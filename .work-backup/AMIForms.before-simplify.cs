using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public partial class AMIPrototype
{
    private void OpenPatientDetails()
    {
        if (Patient == null) return; var patient = Patient; OpenForm("Ficha del paciente"); Heading(modalContent, patient.name);
        Copy(modalContent, string.IsNullOrEmpty(patient.birthDate) ? "Fecha de nacimiento no registrada" : "Nacimiento: " + patient.birthDate);
        InfoCard(modalContent, "Necesidades y cuidados", string.IsNullOrWhiteSpace(patient.needs) ? "Sin información registrada" : patient.needs, PaleBlue);
        InfoCard(modalContent, "Alergias / precauciones", string.IsNullOrWhiteSpace(patient.allergies) ? "Sin información registrada" : patient.allergies, PaleOrange);
        ActionButton(modalContent, "Editar ficha", () => EditPatient(patient)); Section(modalContent, "CONTACTOS DE APOYO");
        foreach (var contact in patient.contacts)
        {
            var item = contact; var card = Card(modalContent, PaleGreen); Copy(card, item.name + " · " + item.relation, 17, Navy, true); Copy(card, item.phone, 16);
            ActionButton(card, "Abrir marcador", () => Dial(item.phone), Color.white, Blue); ActionButton(card, "Editar contacto", () => EditContact(item), Color.white, Blue);
        }
        ActionButton(modalContent, "+  Agregar contacto", () => EditContact(null), PaleBlue, Blue);
        ActionButton(modalContent, "Eliminar paciente…", () => Confirm("Eliminar a " + patient.name, "Se borrarán de este teléfono su ficha, notas, contactos y rutinas. Esta acción no se puede deshacer.", () =>
        {
            if (Save(d => { d.patients.RemoveAll(p => p.id == patient.id); d.selectedPatient = d.patients.FirstOrDefault()?.id ?? ""; })) { CloseForm(); ShowPage(3); }
        }, OpenPatientDetails), PaleRed, Navy);
    }
    private void EditAccount(bool onboarding)
    {
        OpenForm(onboarding ? "Empecemos contigo" : "Mi perfil y privacidad"); Copy(modalContent, "Tu nombre aparecerá como autor de los registros guardados en este teléfono.");
        var author = Field(modalContent, "Tu nombre", store.Data.author, "Ej.: Ana López", 52, 80); var role = Choice(modalContent, "Acompaño como", new[] { "Cuidador", "Familiar" }, store.Data.role); Track(() => author.text + role());
        ActionButton(modalContent, "Guardar perfil", () =>
        {
            if (string.IsNullOrWhiteSpace(author.text)) { Toast("Escribe tu nombre."); return; }
            if (Save(d => { d.author = author.text.Trim(); d.role = role(); })) { CloseForm(); ShowPage(0); }
        });
        InfoCard(modalContent, "Datos locales", "La información se guarda en el almacenamiento privado de esta app. No se sincroniza con otros celulares. Desinstalar AMI borra sus datos. Protege tu teléfono con bloqueo de pantalla.", PaleBlue);
        InfoCard(modalContent, "Voz y privacidad", "AMI guarda el texto que revisas, no una grabación de audio. El reconocimiento local se usa cuando está disponible. Si autorizas el servicio de voz del sistema, este puede procesar audio en sus servidores según su configuración.", PaleBlue);
        InfoCard(modalContent, "Compartir", "El resumen solo se envía cuando eliges compartirlo y seleccionas una app y un destinatario. Cuidador y familiar son etiquetas del perfil local, no cuentas con permisos separados.", PaleBlue);
    }
    private void EditPatient(AMIPatient existing)
    {
        OpenForm(existing == null ? "Registrar paciente" : "Editar paciente"); Copy(modalContent, "Registra solo la información que necesitas para acompañar su cuidado.");
        var name = Field(modalContent, "Nombre *", existing?.name ?? "", "Nombre de la persona", 52, 100);
        var birth = Field(modalContent, "Nacimiento (opcional)", existing?.birthDate ?? "", "AAAA-MM-DD · ej.: 1948-05-23", 52, 10);
        var needs = Field(modalContent, "Necesidades y cuidados", existing?.needs ?? "", "Apoyo que necesita, indicaciones existentes…", 115, 2000); VoiceControls(modalContent, needs, null);
        var allergies = Field(modalContent, "Alergias y precauciones", existing?.allergies ?? "", "Si no se conocen, indícalo", 85, 1000);
        Track(() => name.text + "|" + birth.text + "|" + needs.text + "|" + allergies.text);
        ActionButton(modalContent, "Guardar paciente", () =>
        {
            DateTime dob;
            if (string.IsNullOrWhiteSpace(name.text)) { Toast("El nombre del paciente es obligatorio."); return; }
            if (!string.IsNullOrWhiteSpace(birth.text) && (!AMIStore.ValidDate(birth.text.Trim(), out dob) || dob.Date > DateTime.Today)) { Toast("Revisa la fecha: usa AAAA-MM-DD, sin fechas futuras."); return; }
            if (Save(d =>
            {
                var p = existing == null ? new AMIPatient() : d.patients.Find(x => x.id == existing.id); p.name = name.text.Trim(); p.birthDate = birth.text.Trim(); p.needs = needs.text.Trim(); p.allergies = allergies.text.Trim(); if (existing == null) d.patients.Add(p); d.selectedPatient = p.id;
            })) { CloseForm(); ShowPage(0); Toast("Ficha guardada en este teléfono."); }
        });
    }
    private void EditRoutine(AMIRoutine existing)
    {
        string patientId = Patient.id; OpenForm(existing == null ? "Nueva rutina" : "Editar rutina"); Copy(modalContent, "Paciente: " + Patient.name, 16, Blue, true);
        var category = Choice(modalContent, "Tipo", new[] { "Actividad", "Medicamento", "Cita" }, existing?.category ?? "Actividad");
        var title = Field(modalContent, "Qué hay que hacer *", existing?.title ?? "", "Ej.: paseo acompañado", 64, 140);
        var detail = Field(modalContent, "Indicaciones", existing?.detail ?? "", "Detalles del cuidado o indicación prescrita", 100, 1500); VoiceControls(modalContent, detail, null);
        var time = Field(modalContent, "Hora (24 horas) *", existing?.time ?? "08:00", "HH:mm · ej.: 08:30", 52, 5);
        var repeat = Choice(modalContent, "Repetición", new[] { "Todos los días", "Una fecha" }, existing == null || existing.daily ? "Todos los días" : "Una fecha");
        var date = Field(modalContent, "Fecha (solo para una fecha)", string.IsNullOrEmpty(existing?.date) ? AMIStore.Day(DateTime.Now) : existing.date, "AAAA-MM-DD", 52, 10);
        Copy(modalContent, "Para medicamentos, copia únicamente las indicaciones del profesional. AMI no calcula dosis ni propone tratamientos.", 13, Muted);
        Track(() => category() + title.text + detail.text + time.text + repeat() + date.text);
        ActionButton(modalContent, "Guardar rutina", () =>
        {
            DateTime parsed;
            if (string.IsNullOrWhiteSpace(title.text)) { Toast("Escribe el nombre de la rutina."); return; }
            if (!AMIStore.ValidTime(time.text.Trim())) { Toast("Usa una hora válida, por ejemplo 08:30 o 17:00."); return; }
            if (repeat() == "Una fecha" && !AMIStore.ValidDate(date.text.Trim(), out parsed)) { Toast("La fecha debe tener el formato AAAA-MM-DD."); return; }
            if (Save(d =>
            {
                var p = d.patients.Find(x => x.id == patientId); var r = existing == null ? new AMIRoutine() : p.routines.Find(x => x.id == existing.id);
                r.title = title.text.Trim(); r.detail = detail.text.Trim(); r.category = category(); r.time = time.text.Trim(); r.daily = repeat() == "Todos los días"; r.date = date.text.Trim(); if (existing == null) p.routines.Add(r);
            })) { CloseForm(); ShowPage(1); Toast("Rutina guardada."); }
        });
        if (existing != null) ActionButton(modalContent, "Eliminar rutina…", () => Confirm("Eliminar rutina", "También se eliminará su historial de realizaciones. Las notas del paciente se conservarán.", () =>
        {
            if (Save(d => d.patients.Find(p => p.id == patientId).routines.RemoveAll(r => r.id == existing.id))) { CloseForm(); ShowPage(1); }
        }, () => EditRoutine(existing)), PaleRed, Navy);
    }
    private void EditNote(AMINote existing, bool voiceFirst)
    {
        string patientId = Patient.id; bool dictated = existing != null && existing.dictated; OpenForm(existing == null ? "Registrar cuidado" : "Corregir registro");
        Copy(modalContent, "Para: " + Patient.name, 18, Blue, true);
        Copy(modalContent, "Dicta lo que observaste, revisa el texto y luego guarda. No se crearán medicamentos ni se cambiarán dosis automáticamente.", 14, Muted);
        var category = Choice(modalContent, "Categoría", new[] { "General", "Alimentación", "Medicación", "Ánimo", "Actividad", "Síntomas" }, existing?.category ?? "General");
        var text = Field(modalContent, "Registro *", existing?.text ?? "", "Ej.: hoy almorzó bien y caminó diez minutos acompañado.", 185, 6000); VoiceControls(modalContent, text, () => dictated = true);
        if (voiceFirst) Copy(modalContent, "Pulsa Iniciar dictado para activar el micrófono. Puedes dictar varias frases y se agregarán al texto.", 14, Blue);
        Track(() => text.text + category());
        ActionButton(modalContent, "Revisado · guardar registro", () =>
        {
            if (voice.Busy) { Toast("Termina el dictado y revisa el texto antes de guardar."); return; }
            if (string.IsNullOrWhiteSpace(text.text)) { Toast("Dicta o escribe el registro antes de guardarlo."); return; }
            if (string.IsNullOrWhiteSpace(store.Data.author)) { Toast("Primero guarda tu nombre en Mi perfil."); return; }
            if (Save(d =>
            {
                var p = d.patients.Find(x => x.id == patientId); var n = existing == null ? new AMINote { author = d.author, role = d.role } : p.notes.Find(x => x.id == existing.id);
                n.text = text.text.Trim(); n.category = category(); n.dictated = dictated; if (existing == null) p.notes.Add(n);
            })) { CloseForm(); historyFilter = "Todos"; historySearch = ""; ShowPage(2); Toast("Registro guardado para " + Patient.name + "."); }
        });
        if (existing != null) ActionButton(modalContent, "Eliminar registro…", () => Confirm("Eliminar registro", "La nota se borrará del historial de este paciente. No se puede deshacer.", () =>
        {
            if (Save(d => d.patients.Find(p => p.id == patientId).notes.RemoveAll(n => n.id == existing.id))) { CloseForm(); ShowPage(2); }
        }, () => EditNote(existing, false)), PaleRed, Navy);
    }
    private void EditContact(AMIContact existing)
    {
        string patientId = Patient.id; OpenForm(existing == null ? "Agregar contacto" : "Editar contacto");
        var name = Field(modalContent, "Nombre *", existing?.name ?? "", "Nombre del contacto", 52, 100); var relation = Field(modalContent, "Relación", existing?.relation ?? "", "Ej.: hija, cuidador, familiar", 52, 80);
        var phone = Field(modalContent, "Teléfono *", existing?.phone ?? "", "Ej.: +51 999 999 999", 52, 25); phone.keyboardType = TouchScreenKeyboardType.PhonePad; Track(() => name.text + relation.text + phone.text);
        ActionButton(modalContent, "Guardar contacto", () =>
        {
            string number = new string(phone.text.Where(c => char.IsDigit(c) || c == '+').ToArray());
            if (string.IsNullOrWhiteSpace(name.text) || number.Count(char.IsDigit) < 6 || number.Count(char.IsDigit) > 15 || number.Count(c => c == '+') > 1 || number.IndexOf('+') > 0) { Toast("Revisa el nombre y el número de teléfono."); return; }
            if (Save(d =>
            {
                var p = d.patients.Find(x => x.id == patientId); var c = existing == null ? new AMIContact() : p.contacts.Find(x => x.id == existing.id); c.name = name.text.Trim(); c.relation = relation.text.Trim(); c.phone = number; if (existing == null) p.contacts.Add(c);
            })) { CloseForm(); ShowPage(tab); OpenPatientDetails(); }
        });
        if (existing != null) ActionButton(modalContent, "Eliminar contacto…", () => Confirm("Eliminar contacto", "Se quitará de la ficha de este paciente.", () =>
        {
            if (Save(d => d.patients.Find(p => p.id == patientId).contacts.RemoveAll(c => c.id == existing.id))) { CloseForm(); OpenPatientDetails(); }
        }, () => EditContact(existing)), PaleRed, Navy);
    }
    private void VoiceControls(Transform parent, InputField field, Action dictated)
    {
        var card = Card(parent, PaleGreen); var status = Copy(card, "Dicta una frase y revisa el texto antes de guardar.", 14, Ink);
        var options = Stack(card, 8);
        var network = Choice(options, "Reconocimiento de voz", new[] { "Solo en el dispositivo", "Permitir servicio de Android" }, allowSystemSpeech ? "Permitir servicio de Android" : "Solo en el dispositivo");
        Copy(options, "El servicio de Android puede enviar audio a su proveedor. La opción local requiere un dispositivo e idioma compatibles.", 12, Muted);
        options.gameObject.SetActive(false);
        Action<string> handler = message => { if (status != null) status.text = message; }; voice.Status += handler;
        card.gameObject.AddComponent<AMIOnDestroy>().Callback = () => { if (voice != null) voice.Status -= handler; };
        var start = ActionButton(card, "●  Iniciar dictado", () =>
        {
            if (voice.Busy) { status.text = "Ya hay un dictado en curso. Pulsa Terminar o Cancelar."; return; }
            allowSystemSpeech = network() == "Permitir servicio de Android";
            voice.Begin(allowSystemSpeech, value =>
            {
                if (field == null) return; string combined = field.text + (string.IsNullOrWhiteSpace(field.text) ? "" : "\n") + value;
                if (combined.Length > field.characterLimit) { Toast("No se agregó el dictado: se alcanzó el límite del campo. Guarda y crea otra nota."); return; }
                field.text = combined; dictated?.Invoke();
            });
        }, Green, Color.white);
        start.transform.SetAsFirstSibling();
        var row = new GameObject("Controles de escucha", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(card, false); row.GetComponent<LayoutElement>().preferredHeight = 48;
        var group = row.GetComponent<HorizontalLayoutGroup>(); group.spacing = 8; group.childControlWidth = true; group.childControlHeight = true; group.childForceExpandWidth = true;
        ActionButton(row.transform, "Terminar", () => { if (voice.Busy) voice.Stop(); }, Color.white, Green);
        ActionButton(row.transform, "Cancelar", () => { voice.Cancel(); status.text = "Escucha cancelada. Tu texto anterior se conserva."; }, Color.white, Muted);
        ActionButton(card, "Opciones de voz", () => options.gameObject.SetActive(!options.gameObject.activeSelf), Color.white, Muted);
        options.SetAsLastSibling();
    }
    private void PreviewShare()
    {
        var sb = new StringBuilder(); sb.AppendLine("AMI · Resumen de " + Patient.name); sb.AppendLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm")); sb.AppendLine("\nCuidados de hoy:");
        foreach (var r in AMIStore.Today(Patient, DateTime.Now)) sb.AppendLine(r.time + " · " + r.title + (r.IsDone(DateTime.Now) ? " · realizado" : " · pendiente"));
        sb.AppendLine("\nRegistros de hoy:");
        foreach (var n in Patient.notes.Where(n => DateTimeOffset.TryParse(n.at, out var at) && at.LocalDateTime.Date == DateTime.Today).OrderBy(n => n.at)) sb.AppendLine(n.category + ": " + n.text + " — " + n.author);
        OpenForm("Revisar antes de compartir"); Copy(modalContent, "Este resumen contiene información del paciente. Compártelo solo con la persona adecuada.", 15, Navy);
        var draft = Field(modalContent, "Puedes quitar información", sb.ToString(), "Resumen", 360, 20000);
        ActionButton(modalContent, "Elegir app para compartir…", () => Share(draft.text)); Copy(modalContent, "Compartir un resumen no sincroniza las fichas entre teléfonos.", 13, Muted);
    }
    private void Share(string text)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND"))
            using (var api = new AndroidJavaClass("android.content.Intent"))
            {
                intent.Call<AndroidJavaObject>("setType", "text/plain").Dispose(); intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT", text).Dispose();
                using (var chooser = api.CallStatic<AndroidJavaObject>("createChooser", intent, "Compartir resumen")) activity.Call("startActivity", chooser);
            }
        }
        catch { Toast("No se pudo abrir el menú para compartir."); }
#else
        GUIUtility.systemCopyBuffer = text; Toast("Resumen copiado. En Android se abrirá el menú para compartir.");
#endif
    }
    private void Dial(string phone)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Application.OpenURL("tel:" + phone);
#else
        Toast("En Android se abrirá el marcador con " + phone + ".");
#endif
    }
}
