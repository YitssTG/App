using System;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if UNITY_ANDROID
using UnityEditor.Android;
#endif

public static class AMIBuild
{
    [MenuItem("AMI/Validar datos %&v")]
    public static void ValidateData()
    {
        string directory = Path.GetFullPath("Library/AMIValidation/" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var store = new AMIStore(directory);
        string error;
        int count = 0;
        Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception("AMI validation: " + name); count++; };
        check(store.Data.patients.Count == 0, "First run has no fake patient");
        check(store.Change(d => { d.author = "Prueba local"; d.patients.Add(new AMIPatient { name = "Paciente A" }); d.patients.Add(new AMIPatient { name = "Paciente B" }); d.selectedPatient = d.patients[0].id; }, out error), "First write");
        string patientId = store.Patient.id;
        DateTime today = new DateTime(2026, 10, 9);
        check(store.Change(d =>
        {
            var p = d.patients[0];
            p.notes.Add(new AMINote { text = "Registro dictado y revisado", dictated = true, author = d.author });
            var daily = new AMIRoutine { title = "Paseo", time = "17:00", daily = true };
            daily.completions.Add(new AMICompletion { day = AMIStore.Day(today), author = d.author, at = today.ToString("o") });
            p.routines.Add(daily);
            p.routines.Add(new AMIRoutine { title = "Cita", time = "08:00", daily = false, date = AMIStore.Day(today.AddDays(1)) });
        }, out error), "Nested records write");
        var reload = new AMIStore(directory);
        check(reload.Patient.id == patientId && reload.Patient.notes.Count == 1 && reload.Patient.notes[0].dictated, "Persistence and selected patient");
        check(reload.Data.patients[1].notes.Count == 0, "Patient isolation");
        check(AMIStore.Today(reload.Patient, today).Count == 1, "One-off date filtering");
        check(AMIStore.Today(reload.Patient, today.AddDays(1)).First().title == "Cita", "Sort and scheduled date");
        check(reload.Patient.routines[0].IsDone(today) && !reload.Patient.routines[0].IsDone(today.AddDays(1)), "Completion resets each date");
        check(AMIStore.ValidTime("23:59") && !AMIStore.ValidTime("24:00") && !AMIStore.ValidTime("9:90"), "Time validation");
        DateTime parsed;
        check(AMIStore.ValidDate("2024-02-29", out parsed) && !AMIStore.ValidDate("2026-02-29", out parsed), "Date validation");
        check(reload.Change(d => d.patients[0].notes[0].text = "Corregido", out error), "Edit record");
        check(new AMIStore(directory).Patient.notes[0].text == "Corregido", "Edit persists");
        File.WriteAllText(Path.Combine(directory, "ami-care-v1.json"), "broken");
        var recovered = new AMIStore(directory);
        check(!recovered.ReadOnly && recovered.Patient.notes[0].text == "Registro dictado y revisado" && recovered.LoadWarning != null, "Recovery from backup");
        check(recovered.Change(d => d.author = "Recuperado", out error), "Save after recovery");
        check(new AMIStore(directory).Data.author == "Recuperado", "Recovered save readable");
        check(recovered.Change(d => { d.patients.RemoveAll(p => p.id == patientId); d.selectedPatient = d.patients[0].id; }, out error), "Delete patient transaction");
        check(new AMIStore(directory).Data.patients.Count == 1 && new AMIStore(directory).Patient.name == "Paciente B", "Delete isolated to selected patient");
        var corrupt = Path.Combine(directory, "corrupt"); Directory.CreateDirectory(corrupt); File.WriteAllText(Path.Combine(corrupt, "ami-care-v1.json"), "broken");
        var locked = new AMIStore(corrupt);
        check(locked.ReadOnly && !locked.Change(d => d.author = "Do not overwrite", out error), "Corrupt data preserved");
        var invalid = Path.Combine(directory, "not-a-directory"); File.WriteAllText(invalid, "blocked"); var failing = new AMIStore(invalid);
        check(!failing.Change(d => d.author = "Unsaved", out error) && failing.Data.author == "", "Failed save does not mutate memory");
        Directory.CreateDirectory("Builds"); File.WriteAllText("Builds/validation.txt", count + " data checks passed\n" + DateTimeOffset.Now.ToString("o"));
        Debug.Log("AMI: " + count + " validaciones de datos correctas.");
    }

    [MenuItem("AMI/Crear APK Android %&b")]
    public static void BuildAndroid()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("Detén Play antes de crear el APK."); return; }
        ValidateData();
        PlayerSettings.companyName = "AMI"; PlayerSettings.productName = "AMI Cuidado";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.ami.cuidado");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        EditorUserBuildSettings.buildAppBundle = false;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/SampleScene.unity" }, locationPathName = "Builds/AMI-Cuidado.apk", target = BuildTarget.Android, options = BuildOptions.None });
        File.WriteAllText("Builds/build-result.txt", report.summary.result + "\nErrors: " + report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings + "\n" + report.summary.totalTime);
        if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("No se pudo crear el APK. Revisa la consola.");
        Debug.Log("AMI: APK creado en Builds/AMI-Cuidado.apk");
    }
}

#if UNITY_ANDROID
public sealed class AMIAndroidManifest : IPostGenerateGradleAndroidProject
{
    public int callbackOrder { get { return 100; } }
    public void OnPostGenerateGradleAndroidProject(string path)
    {
        Patch(Path.Combine(path, "src/main/AndroidManifest.xml"), true);
        string launcher = Path.GetFullPath(Path.Combine(path, "../launcher/src/main/AndroidManifest.xml"));
        if (File.Exists(launcher)) Patch(launcher, false);
    }
    private static void Patch(string path, bool permission)
    {
        const string ns = "http://schemas.android.com/apk/res/android";
        var doc = new XmlDocument(); doc.Load(path); var root = doc.DocumentElement;
        if (permission)
        {
            if (!root.ChildNodes.Cast<XmlNode>().Any(n => n.Name == "uses-permission" && n.Attributes?["name", ns]?.Value == "android.permission.RECORD_AUDIO"))
            { var mic = doc.CreateElement("uses-permission"); mic.SetAttribute("name", ns, "android.permission.RECORD_AUDIO"); root.AppendChild(mic); }
            var feature = doc.CreateElement("uses-feature"); feature.SetAttribute("name", ns, "android.hardware.microphone"); feature.SetAttribute("required", ns, "false"); root.AppendChild(feature);
            var queries = root.SelectSingleNode("queries") ?? root.AppendChild(doc.CreateElement("queries"));
            var intent = doc.CreateElement("intent"); var action = doc.CreateElement("action"); action.SetAttribute("name", ns, "android.speech.RecognitionService"); intent.AppendChild(action); queries.AppendChild(intent);
        }
        var app = root.SelectSingleNode("application") as XmlElement;
        if (app != null) { app.SetAttribute("allowBackup", ns, "false"); app.SetAttribute("fullBackupContent", ns, "false"); }
        doc.Save(path);
    }
}
#endif
