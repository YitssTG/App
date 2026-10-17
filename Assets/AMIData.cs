using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable] public sealed class AMIData
{
    public int version = 1;
    public string author = "";
    public string role = "Cuidador";
    public string selectedPatient = "";
    public List<AMIPatient> patients = new List<AMIPatient>();
}

[Serializable] public sealed class AMIPatient
{
    public string id = Guid.NewGuid().ToString("N");
    public string name = "";
    public string birthDate = "";
    public string needs = "";
    public string allergies = "";
    public List<AMIRoutine> routines = new List<AMIRoutine>();
    public List<AMINote> notes = new List<AMINote>();
    public List<AMIContact> contacts = new List<AMIContact>();
}

[Serializable] public sealed class AMIRoutine
{
    public string id = Guid.NewGuid().ToString("N");
    public string title = "";
    public string category = "Actividad";
    public string detail = "";
    public string time = "08:00";
    public string date = "";
    public bool daily = true;
    public List<AMICompletion> completions = new List<AMICompletion>();
    public bool IsDue(DateTime day) { return daily || date == AMIStore.Day(day); }
    public bool IsDone(DateTime day) { return completions.Any(c => c.day == AMIStore.Day(day)); }
}

[Serializable] public sealed class AMICompletion
{
    public string day;
    public string at;
    public string author;
}

[Serializable] public sealed class AMINote
{
    public string id = Guid.NewGuid().ToString("N");
    public string at = DateTimeOffset.Now.ToString("o");
    public string category = "General";
    public string text = "";
    public string author = "";
    public string role = "";
    public bool dictated;
}

[Serializable] public sealed class AMIContact
{
    public string id = Guid.NewGuid().ToString("N");
    public string name = "";
    public string relation = "";
    public string phone = "";
}

/// <summary>One local household, isolated patient records and atomic file replacement.</summary>
public sealed class AMIStore
{
    public AMIData Data { get; private set; }
    public string LoadWarning { get; private set; }
    public bool ReadOnly { get; private set; }
    private readonly string path;
    public static string Day(DateTime day) { return day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }
    public AMIPatient Patient { get { return Data.patients.Find(p => p.id == Data.selectedPatient); } }

    public AMIStore(string directory)
    {
        path = Path.Combine(directory, "ami-care-v1.json");
        Data = new AMIData();
        if (!File.Exists(path) && !File.Exists(path + ".bak")) return;
        try { Data = Read(path); }
        catch
        {
            try { Data = Read(path + ".bak"); LoadWarning = "Se recuperó la copia local anterior. Revisa tus últimos registros."; }
            catch { ReadOnly = true; LoadWarning = "No se pudieron leer los datos. Se conservaron los archivos originales. No registres datos nuevos hasta recuperar la copia local."; }
        }
        if (Data.patients.All(p => p.id != Data.selectedPatient)) Data.selectedPatient = Data.patients.FirstOrDefault()?.id ?? "";
    }

    private static AMIData Read(string filename)
    {
        var json = File.ReadAllText(filename);
        if (!json.TrimStart().StartsWith("{") || !json.Contains("\"version\"")) throw new InvalidDataException();
        var data = JsonUtility.FromJson<AMIData>(json);
        if (data == null || data.version != 1 || data.patients == null) throw new InvalidDataException();
        foreach (var patient in data.patients)
        {
            if (patient == null || string.IsNullOrEmpty(patient.id)) throw new InvalidDataException();
            patient.routines = patient.routines ?? new List<AMIRoutine>();
            patient.notes = patient.notes ?? new List<AMINote>();
            patient.contacts = patient.contacts ?? new List<AMIContact>();
            foreach (var routine in patient.routines) routine.completions = routine.completions ?? new List<AMICompletion>();
        }
        return data;
    }

    public bool Change(Action<AMIData> change, out string error)
    {
        error = null;
        if (ReadOnly) { error = LoadWarning; return false; }
        var next = JsonUtility.FromJson<AMIData>(JsonUtility.ToJson(Data));
        try
        {
            change(next);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path + ".tmp", JsonUtility.ToJson(next, true));
            if (File.Exists(path))
            {
                // Keep a known valid backup when recovering from a damaged primary file.
                bool primaryValid = false;
                try { Read(path); primaryValid = true; } catch { }
                if (primaryValid) File.Copy(path, path + ".bak", true);
                File.Replace(path + ".tmp", path, null);
            }
            else File.Move(path + ".tmp", path);
            Data = next;
            return true;
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            error = "No se pudo guardar. Revisa el espacio disponible; tus cambios siguen en el formulario.";
            return false;
        }
    }

    public static bool ValidDate(string value, out DateTime day)
    {
        return DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day);
    }
    public static bool ValidTime(string value)
    {
        DateTime parsed;
        return DateTime.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
    }
    public static List<AMIRoutine> Today(AMIPatient patient, DateTime day)
    {
        return patient.routines.Where(r => r.IsDue(day)).OrderBy(r => r.time, StringComparer.Ordinal).ToList();
    }
}
