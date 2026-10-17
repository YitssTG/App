using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Exercises the same form buttons as the user, with an isolated temporary store.</summary>
public static class AMIFlowValidation
{
    private static AMIPrototype app;
    private static AMIStore original;
    private static IEnumerator steps;
    private static double next;
    private static int passed;
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    [MenuItem("AMI/Probar formularios en Play %&t")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Inicia Play para probar los formularios.");
        app = UnityEngine.Object.FindFirstObjectByType<AMIPrototype>();
        if (app == null) throw new Exception("AMI no está en la escena.");
        original = (AMIStore)typeof(AMIPrototype).GetField("store", Flags).GetValue(app);
        passed = 0; steps = Flow(); next = 0; EditorApplication.update += Tick;
    }
    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 0.25;
        try { if (steps.MoveNext()) return; Finish(null); }
        catch (Exception error) { Finish(error); }
    }
    private static void Finish(Exception error)
    {
        EditorApplication.update -= Tick;
        Invoke("CloseForm"); typeof(AMIPrototype).GetField("store", Flags).SetValue(app, original); Invoke("ShowPage", 0);
        Directory.CreateDirectory("Builds");
        File.WriteAllText("Builds/flow-validation.txt", error == null ? passed + " UI flow checks passed" : error.ToString());
        if (error != null) Debug.LogException(error); else Debug.Log("AMI: formularios, persistencia y aislamiento verificados (" + passed + ").");
    }
    private static void Invoke(string method, params object[] args) { typeof(AMIPrototype).GetMethod(method, Flags).Invoke(app, args); }
    private static AMIStore Store { get { return (AMIStore)typeof(AMIPrototype).GetField("store", Flags).GetValue(app); } }
    private static void Check(bool ok, string label) { if (!ok) throw new Exception("AMI flow: " + label); passed++; }
    private static void Fill(string label, string value) { UnityEngine.Object.FindObjectsByType<InputField>(FindObjectsSortMode.None).Single(f => f.gameObject.activeInHierarchy && f.name == label).text = value; }
    private static void Click(string caption)
    {
        var buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b => b.gameObject.activeInHierarchy && b.name == caption).ToArray();
        if (buttons.Length != 1) throw new Exception("Button ambiguous: " + caption + " (" + buttons.Length + ")");
        buttons[0].onClick.Invoke();
    }
    private static IEnumerator Flow()
    {
        string dir = Path.GetFullPath("Library/AMIFlowValidation/" + Guid.NewGuid().ToString("N"));
        typeof(AMIPrototype).GetField("store", Flags).SetValue(app, new AMIStore(dir));
        Invoke("CloseForm"); Invoke("EditAccount", true); yield return null;
        Fill("Tu nombre", "Cuidadora de prueba"); Click("Guardar perfil"); yield return null;
        Check(Store.Data.author == "Cuidadora de prueba", "Profile saved through form");
        Invoke("EditPatient", new object[] { null }); yield return null;
        Click("Guardar paciente"); Check(Store.Data.patients.Count == 0, "Blank name rejected");
        Fill("Nombre *", "Paciente de prueba A"); Click("Datos del cuidado (opcional)"); yield return null;
        Fill("Nacimiento (opcional)", "1948-05-23"); Fill("Necesidades y cuidados", "Apoyo al caminar"); Fill("Alergias y precauciones", "Por confirmar"); Click("Guardar paciente"); yield return null;
        Check(Store.Patient.name == "Paciente de prueba A", "Patient form saved");
        string first = Store.Patient.id;
        Invoke("EditRoutine", new object[] { null }); yield return null;
        Fill("Qué hay que hacer *", "Paseo acompañado"); Click("Indicaciones (opcional)"); yield return null;
        Fill("Indicaciones", "En el jardín"); Fill("Hora (24 horas) *", "25:90"); Click("Guardar rutina"); Check(Store.Patient.routines.Count == 0, "Invalid time rejected");
        Fill("Hora (24 horas) *", "17:00"); Click("Guardar rutina"); yield return null;
        Check(Store.Patient.routines.Count == 1, "Routine form saved");
        Invoke("EditRoutine", Store.Patient.routines[0]); yield return null;
        Check(!UnityEngine.Object.FindObjectsByType<InputField>(FindObjectsSortMode.None).Any(f => f.gameObject.activeInHierarchy && f.name == "Fecha"), "Daily routine hides date");
        Click("Todos los días  ▾"); yield return null; Click("Una fecha"); yield return null;
        Fill("Fecha", AMIStore.Day(DateTime.Today)); Click("Guardar rutina"); yield return null;
        Check(!Store.Patient.routines[0].daily && Store.Patient.routines[0].date == AMIStore.Day(DateTime.Today), "Explicit repeat choice and date saved");
        Invoke("ShowPage", 0); yield return null;
        ((GameObject)typeof(AMIPrototype).GetField("toastObject", Flags).GetValue(app)).SetActive(false);
        Directory.CreateDirectory("Builds/UIReview");
        ScreenCapture.CaptureScreenshot("Builds/UIReview/inicio.png"); yield return null;
        Invoke("ShowPage", 1); yield return null;
        Click("Paseo acompañado  ›"); yield return null;
        Click("Marcar realizado"); yield return null;
        Check(Store.Patient.routines[0].IsDone(DateTime.Today), "Completion button persisted");
        Click("✓  Paseo acompañado  ›"); yield return null; Click("Realizado · Deshacer"); yield return null;
        Check(!Store.Patient.routines[0].IsDone(DateTime.Today), "Completed section allows undo");
        Click("Paseo acompañado  ›"); yield return null; Click("Marcar realizado"); yield return null;
        Invoke("ShowPage", 0); yield return null;
        Check(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.gameObject.activeInHierarchy && t.text == "★  ¡Cuidados de hoy completos!"), "Daily completion achievement visible");
        Invoke("EditNote", null, true); yield return null;
        ScreenCapture.CaptureScreenshot("Builds/UIReview/nota.png"); yield return null;
        Click("Guardar nota"); Check(Store.Patient.notes.Count == 0, "Blank note rejected");
        Fill("¿Cómo estuvo? *", "Comió bien. Registro escrito para validar el flujo."); Click("Guardar nota"); yield return null;
        Check(Store.Patient.notes.Count == 1 && Store.Patient.notes[0].author == "Cuidadora de prueba", "Note attributed and saved");
        Click("Buscar notas"); yield return null;
        Fill("Buscar", "no coincide"); Click("Buscar"); yield return null;
        Check(!UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b => b.gameObject.activeInHierarchy && b.name == "Editar nota"), "Search hides nonmatching notes");
        Click("Ver todas las notas"); yield return null;
        Check(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b => b.gameObject.activeInHierarchy && b.name == "Editar nota"), "Reset search restores notes");
        Invoke("EditNote", Store.Patient.notes[0], false); yield return null;
        Fill("¿Cómo estuvo? *", "Corrección pendiente"); Invoke("RequestClose"); yield return null;
        Click("Seguir editando"); yield return null;
        Check(UnityEngine.Object.FindObjectsByType<InputField>(FindObjectsSortMode.None).Any(f => f.gameObject.activeInHierarchy && f.text == "Corrección pendiente"), "Discard dialog preserves input");
        Invoke("RequestClose"); yield return null; Click("Descartar y cerrar"); yield return null;
        Check(Store.Patient.notes[0].text.StartsWith("Comió"), "Discard leaves saved data intact");
        Invoke("EditContact", new object[] { null }); yield return null;
        Fill("Nombre *", "Contacto de prueba"); Fill("Relación", "Familiar"); Fill("Teléfono *", "+51 999 999 999"); Click("Guardar contacto"); yield return null;
        Check(Store.Patient.contacts.Count == 1 && Store.Patient.contacts[0].phone == "+51999999999", "Contact saved and normalized");
        Invoke("CloseForm"); Invoke("ShowPage", 0); yield return null;
        Click("Medicamentos"); yield return null;
        Click("+  Agregar medicamento"); yield return null;
        Check(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b => b.gameObject.activeInHierarchy && b.name == "Medicamento  ▾"), "Medication entry presets correct category");
        Fill("Qué hay que hacer *", "Medicación de prueba"); Click("Guardar rutina"); yield return null;
        Invoke("ShowPage", 4); yield return null;
        Check(!UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.gameObject.activeInHierarchy && t.text == "Paseo acompañado"), "Medicines screen excludes activities");
        ScreenCapture.CaptureScreenshot("Builds/UIReview/medicamentos.png"); yield return null;
        Invoke("ShowPage", 5); yield return null;
        Check(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.gameObject.activeInHierarchy && t.text == "Medicación de prueba"), "Pending screen includes incomplete medication");
        Click("Marcar realizado"); yield return null;
        Check(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.gameObject.activeInHierarchy && t.text == "Sin cuidados pendientes"), "Pending screen updates after completion");
        Invoke("ShowPage", 6); yield return null;
        Check(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b => b.gameObject.activeInHierarchy && b.name == "Llamar a Contacto de prueba"), "Family shows saved contact");
        ((GameObject)typeof(AMIPrototype).GetField("toastObject", Flags).GetValue(app)).SetActive(false);
        ScreenCapture.CaptureScreenshot("Builds/UIReview/familia.png"); yield return null;
        Invoke("ShowPage", 7); yield return null;
        Check(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.gameObject.activeInHierarchy && t.text.StartsWith("Comió bien.")), "Care summary includes saved note");
        Invoke("EditPatient", new object[] { null }); yield return null;
        Fill("Nombre *", "Paciente de prueba B"); Click("Guardar paciente"); yield return null;
        Check(Store.Patient.id != first && Store.Patient.notes.Count == 0 && Store.Patient.routines.Count == 0, "Second patient isolated");
        var reload = new AMIStore(dir); Check(reload.Data.patients.Count == 2 && reload.Data.patients[0].notes.Count == 1 && reload.Data.patients[0].contacts.Count == 1, "Restart retains all records");
        Canvas.ForceUpdateCanvases();
        var controls = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b => b.gameObject.activeInHierarchy && b.GetComponent<LayoutElement>() != null);
        Check(controls.All(b => b.GetComponent<RectTransform>().rect.height >= 48), "Accessible button height");
    }
}
