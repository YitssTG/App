using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class AMIPrototype
{
    private static readonly Color Navy = Hex("173B68"), Blue = Hex("1674D1"), Green = Hex("23805A"), Ink = Hex("18324F"), Muted = Hex("526B83"), PaleBlue = Hex("EAF5FF"), PaleGreen = Hex("EAF8F1"), PaleOrange = Hex("FFF3E2"), PaleRed = Hex("FFF0F0"), Background = Hex("F5F8FC");
    private static Sprite rounded;
    private Font font;
    private RectTransform frame, content, modal, modalContent;
    private ScrollRect scroll;
    private Text pageTitle, patientLabel, toastText;
    private GameObject toastObject;
    private AMIStore store;
    private AMIVoice voice;
    private readonly List<Button> nav = new List<Button>();
    private int tab;
    private string dayKey, formOriginal;
    private Func<string> formSnapshot;
    private GameObject discardPanel;
    private bool allowSystemSpeech;
    private float toastUntil;
    private string historyFilter = "Todos", historySearch = "";
    private readonly CultureInfo spanish = new CultureInfo("es-PE");
    private AMIPatient Patient { get { return store.Patient; } }

    private void Start()
    {
        Application.targetFrameRate = 60;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        store = new AMIStore(Application.persistentDataPath);
        voice = gameObject.AddComponent<AMIVoice>();
        if (FindFirstObjectByType<EventSystem>() == null) new GameObject("AMI EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        BuildShell(); dayKey = AMIStore.Day(DateTime.Now); ShowPage(0);
        if (!string.IsNullOrEmpty(store.LoadWarning)) { OpenForm("Datos locales"); Copy(modalContent, store.LoadWarning); }
        else if (string.IsNullOrWhiteSpace(store.Data.author)) EditAccount(true);
    }
    private void Update()
    {
        if (toastObject != null && Time.unscaledTime > toastUntil) toastObject.SetActive(false);
        if (dayKey != AMIStore.Day(DateTime.Now)) { dayKey = AMIStore.Day(DateTime.Now); if (modal == null) ShowPage(tab); }
        if (Input.GetKeyDown(KeyCode.Escape)) { if (modal != null) RequestClose(); else if (tab != 0) ShowPage(0); }
    }
    private void BuildShell()
    {
        var root = new GameObject("AMI App", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(430, 932); scaler.matchWidthOrHeight = 1;
        Fill(Box("Fondo", root.transform, Background));
        frame = Box("AMI Mobile Frame", root.transform, Color.white); frame.gameObject.AddComponent<AMIResponsiveFrame>().Configure(430);
        var header = Box("Cabecera", frame, Color.white); Bounds(header, new Vector2(0, 1), Vector2.one, new Vector2(20, -85), new Vector2(-20, -14));
        var logo = TextAt("♥  AMI", header, 27, Navy, FontStyle.Bold); Bounds(logo.rectTransform, Vector2.zero, Vector2.one, new Vector2(0, 26), new Vector2(-60, 0));
        pageTitle = TextAt("", header, 14, Muted); Bounds(pageTitle.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-55, -44));
        var settings = ButtonAt("Mi perfil", header, () => EditAccount(false), PaleBlue, Navy, 16); Bounds(settings.GetComponent<RectTransform>(), new Vector2(1, 0), Vector2.one, new Vector2(-100, 10), new Vector2(0, -12));
        var selected = ButtonAt("", frame, () => ShowPage(3), PaleBlue, Navy); Bounds(selected.GetComponent<RectTransform>(), new Vector2(0, 1), Vector2.one, new Vector2(20, -150), new Vector2(-20, -94));
        patientLabel = TextAt("", selected.transform, 16, Navy, FontStyle.Bold); Fill(patientLabel.rectTransform, 14, 8);
        content = Scroller(frame, new Vector2(18, 83), new Vector2(-18, -162), out scroll);
        var navRect = Box("Navegación", frame, Color.white); Bounds(navRect, Vector2.zero, new Vector2(1, 0), new Vector2(12, 9), new Vector2(-12, 76));
        var layout = navRect.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 5; layout.childControlWidth = true; layout.childForceExpandWidth = true; layout.childControlHeight = true; layout.childForceExpandHeight = true;
        string[] labels = { "Inicio", "Rutinas", "Notas", "Pacientes" };
        for (int i = 0; i < labels.Length; i++) { int index = i; var b = ButtonAt(labels[i], navRect, () => ShowPage(index), Color.white, Muted, 16); var le = b.gameObject.AddComponent<LayoutElement>(); le.flexibleWidth = 1; le.minWidth = 0; nav.Add(b); }
        toastObject = Box("Mensaje", frame, Navy).gameObject; Bounds(toastObject.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1, 0), new Vector2(20, 90), new Vector2(-20, 175));
        toastText = TextAt("", toastObject.transform, 15, Color.white); Fill(toastText.rectTransform, 14, 8); toastObject.SetActive(false);
    }
    private void ShowPage(int index)
    {
        if (modal != null) return;
        tab = index; Clear(content);
        patientLabel.text = Patient == null ? "+  Registra tu primer paciente" : Patient.name + "  ·  Cambiar";
        string[] labels = { "Cuidado diario", "Rutinas", "Notas", "Pacientes" }; pageTitle.text = labels[tab];
        for (int i = 0; i < nav.Count; i++) { nav[i].GetComponent<Image>().color = i == tab ? PaleBlue : Color.white; nav[i].GetComponentInChildren<Text>().color = i == tab ? Blue : Muted; }
        if (tab == 3) BuildPatients(); else if (Patient == null) EmptyPatient(); else if (tab == 0) BuildHome(); else if (tab == 1) BuildRoutines(); else BuildHistory();
        Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(content); scroll.StopMovement(); scroll.verticalNormalizedPosition = 1;
    }
    private void EmptyPatient()
    {
        Heading(content, "Bienvenido a AMI"); Copy(content, "Agrega a la persona que cuidas para empezar.");
        ActionButton(content, "+  Registrar paciente", () => EditPatient(null));
        Copy(content, "Tus datos se guardan en este teléfono.", 16, Muted);
    }
    private void BuildHome()
    {
        Heading(content, "Cuidados de hoy"); Copy(content, DateTime.Now.ToString("dddd, d 'de' MMMM", spanish), 16, Muted);
        var today = AMIStore.Today(Patient, DateTime.Now);
        if (today.Count > 0) Copy(content, today.Count(r => r.IsDone(DateTime.Now)) + " de " + today.Count + " realizados", 18, Green, true);
        var next = today.FirstOrDefault(r => !r.IsDone(DateTime.Now));
        if (next != null) { Section(content, "Próximo cuidado"); RoutineCard(content, next); }
        else InfoCard(content, today.Count == 0 ? "Sin rutinas para hoy" : "Todo listo por hoy", today.Count == 0 ? "Agrega una actividad, medicamento o cita." : "Completaste los cuidados de hoy.", PaleGreen);
        ActionButton(content, today.Count == 0 ? "+  Agregar rutina" : "Ver todas las rutinas", () => { if (today.Count == 0) EditRoutine(null); else ShowPage(1); }, PaleBlue, Blue);
        Section(content, "¿Cómo está hoy?");
        ActionButton(content, "+  Añadir nota", () => EditNote(null, false));
        Copy(content, "Escribe o usa tu voz.", 16, Muted);
    }
    private void BuildPatients()
    {
        Heading(content, "Pacientes"); ActionButton(content, "+  Registrar paciente", () => EditPatient(null));
        foreach (var patient in store.Data.patients)
        {
            var item = patient; var card = Card(content, Patient?.id == item.id ? PaleGreen : PaleBlue); Copy(card, item.name, 20, Navy, true); Copy(card, item.routines.Count + " rutinas · " + item.notes.Count + " registros", 14, Muted);
            ActionButton(card, Patient?.id == item.id ? "Ver ficha" : "Seleccionar paciente", () => { if (Save(d => d.selectedPatient = item.id)) { ShowPage(0); OpenPatientDetails(); } }, Color.white, Blue);
        }
    }
    private void BuildRoutines()
    {
        Heading(content, "Rutinas de hoy"); ActionButton(content, "+  Agregar rutina", () => EditRoutine(null));
        var today = AMIStore.Today(Patient, DateTime.Now);
        if (today.Count == 0) Copy(content, "Aquí verás los cuidados y sus horarios.");
        foreach (var r in today) RoutineCard(content, r);
        var other = Patient.routines.Where(r => !r.IsDue(DateTime.Now)).OrderBy(r => r.date).ThenBy(r => r.time).ToList();
        if (other.Count > 0) Section(content, "OTRAS FECHAS"); foreach (var r in other) RoutineCard(content, r);
        Copy(content, "Consulta aquí tus horarios. No se envían alarmas.", 16, Muted);
    }
    private void RoutineCard(Transform parent, AMIRoutine routine)
    {
        var day = DateTime.Now; bool due = routine.IsDue(day), done = routine.IsDone(day); var card = Card(parent, done && due ? PaleGreen : PaleBlue);
        Copy(card, routine.time + "  ·  " + routine.category, 18, Blue, true); Copy(card, routine.title, 20, Navy, true);
        if (!string.IsNullOrEmpty(routine.detail)) Copy(card, routine.detail, 17); if (!due) Copy(card, "Fecha: " + routine.date, 16, Muted);
        if (due) ActionButton(card, done ? "Realizado · Deshacer" : "Marcar realizado", () =>
        {
            string pid = Patient.id;
            if (Save(d => { var r = d.patients.Find(p => p.id == pid).routines.Find(x => x.id == routine.id); if (done) r.completions.RemoveAll(c => c.day == AMIStore.Day(day)); else if (!r.IsDone(day)) r.completions.Add(new AMICompletion { day = AMIStore.Day(day), at = DateTimeOffset.Now.ToString("o"), author = d.author }); })) { ShowPage(tab); Toast(done ? "Registro deshecho." : "Cuidado registrado para hoy."); }
        }, done ? Color.white : Blue, done ? Green : Color.white);
        if (tab == 1) ActionButton(card, "Editar rutina", () => EditRoutine(routine), Color.white, Blue);
    }
    private void BuildHistory()
    {
        Heading(content, "Notas del cuidado"); ActionButton(content, "+  Añadir nota", () => EditNote(null, false));
        var filters = Disclosure(content, "Buscar notas", historyFilter != "Todos" || historySearch != "");
        var search = Field(filters, "Buscar", historySearch, "Palabra o frase", 56, 120);
        var filter = Choice(filters, "Categoría", new[] { "Todos", "General", "Alimentación", "Medicación", "Ánimo", "Actividad", "Síntomas" }, historyFilter);
        ActionButton(filters, "Buscar", () => { historySearch = search.text.Trim(); historyFilter = filter(); ShowPage(2); }, PaleBlue, Blue);
        ActionButton(filters, "Ver todas las notas", () => { historySearch = ""; historyFilter = "Todos"; ShowPage(2); }, Color.white, Blue);
        var entries = Patient.notes.Where(n => (historyFilter == "Todos" || n.category == historyFilter) && (string.IsNullOrWhiteSpace(historySearch) || n.text.IndexOf(historySearch, StringComparison.OrdinalIgnoreCase) >= 0)).OrderByDescending(n => n.at).ToList();
        foreach (var n in entries) NoteCard(content, n);
        if (entries.Count == 0) Copy(content, Patient.notes.Count == 0 ? "Añade una nota para recordar cómo estuvo el día." : "No se encontraron notas. Cambia la búsqueda.");
        var completed = Disclosure(content, "Cuidados realizados");
        var completions = Patient.routines.SelectMany(r => r.completions.Select(c => new { Routine = r, Completion = c })).OrderByDescending(x => x.Completion.at).ToList();
        foreach (var e in completions) InfoCard(completed, e.Routine.title, FormatDate(e.Completion.at) + " · " + e.Completion.author, PaleGreen);
        if (completions.Count == 0) Copy(completed, "Aún no hay cuidados realizados.");
        ActionButton(content, "Compartir resumen", PreviewShare, PaleBlue, Blue);
    }
    private void NoteCard(Transform parent, AMINote note)
    {
        var card = Card(parent, PaleBlue); Copy(card, note.category + " · " + FormatDate(note.at), 16, Blue, true); Copy(card, note.text, 18);
        Copy(card, note.author, 16, Muted); ActionButton(card, "Editar nota", () => EditNote(note, false), Color.white, Blue);
    }
    private bool Save(Action<AMIData> change) { if (store.Change(change, out var error)) return true; Toast(error); return false; }
    private static string FormatDate(string text) { return DateTimeOffset.TryParse(text, out var date) ? date.LocalDateTime.ToString("dd/MM/yyyy HH:mm") : text; }
    private void OpenForm(string title)
    {
        CloseForm(); modal = Box(title, frame, Color.white); Fill(modal);
        var heading = TextAt(title, modal, 21, Navy, FontStyle.Bold); Bounds(heading.rectTransform, new Vector2(0, 1), Vector2.one, new Vector2(20, -68), new Vector2(-70, -14));
        var close = ButtonAt("×", modal, RequestClose, PaleBlue, Navy, 26); Bounds(close.GetComponent<RectTransform>(), Vector2.one, Vector2.one, new Vector2(-62, -62), new Vector2(-14, -14));
        modalContent = Scroller(modal, new Vector2(20, 18), new Vector2(-20, -82), out var modalScroll); modalContent.gameObject.AddComponent<AMIKeyboardInset>().Configure(modalScroll.GetComponent<RectTransform>(), frame);
    }
    private void Track(Func<string> snapshot) { formSnapshot = snapshot; formOriginal = snapshot(); }
    private void RequestClose()
    {
        voice.Cancel();
        if (discardPanel != null) { Destroy(discardPanel); discardPanel = null; return; }
        if (formSnapshot != null && formSnapshot() != formOriginal)
        {
            var panel = Box("Confirmar salida", modal, Color.white); Fill(panel, 8, 8); discardPanel = panel.gameObject;
            var stack = Stack(panel, 18); Fill(stack, 18, 100); Heading(stack, "¿Descartar cambios?"); Copy(stack, "Hay información sin guardar.");
            ActionButton(stack, "Seguir editando", () => { Destroy(discardPanel); discardPanel = null; }); ActionButton(stack, "Descartar y cerrar", CloseForm, PaleRed, Navy); return;
        }
        CloseForm();
    }
    private void CloseForm()
    {
        if (voice != null) voice.Cancel(); formSnapshot = null; formOriginal = null; discardPanel = null;
        if (modal != null) { modal.gameObject.SetActive(false); Destroy(modal.gameObject); } modal = null; modalContent = null;
    }
    private void Confirm(string title, string message, Action yes, Action no) { OpenForm(title); Copy(modalContent, message); ActionButton(modalContent, "Cancelar", no); ActionButton(modalContent, "Sí, eliminar", yes, PaleRed, Navy); }
    private void Toast(string message) { toastText.text = message; toastObject.transform.SetAsLastSibling(); toastObject.SetActive(true); toastUntil = Time.unscaledTime + 6; }
    private RectTransform Card(Transform parent, Color color) { var r = Box("Tarjeta", parent, color); Round(r.GetComponent<Image>()); Layout(r, 14, 10); return r; }
    private void InfoCard(Transform parent, string title, string body, Color color) { var card = Card(parent, color); Copy(card, title, 18, Navy, true); Copy(card, body, 15); }
    private void Heading(Transform parent, string value) { Copy(parent, value, 24, Navy, true); }
    private void Section(Transform parent, string value) { Copy(parent, value, 18, Navy, true).gameObject.GetComponent<LayoutElement>().minHeight = 32; }
    private Text Copy(Transform parent, string value, int size = 16, Color? color = null, bool bold = false)
    {
        var text = TextAt(value, parent, size, color ?? Ink, bold ? FontStyle.Bold : FontStyle.Normal); var le = text.gameObject.AddComponent<LayoutElement>(); le.minWidth = 0; le.flexibleWidth = 1; le.minHeight = size + 8; return text;
    }
    private Button ActionButton(Transform parent, string caption, Action action, Color? bg = null, Color? fg = null)
    {
        var b = ButtonAt(caption, parent, action, bg ?? Blue, fg ?? Color.white, 18); var le = b.gameObject.AddComponent<LayoutElement>(); le.minHeight = 56; le.preferredHeight = 56; le.flexibleWidth = 1; le.minWidth = 0; return b;
    }
    private Button ButtonAt(string caption, Transform parent, Action action, Color bg, Color fg, int size = 16)
    {
        var r = Box(caption, parent, bg); Round(r.GetComponent<Image>()); var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = r.GetComponent<Image>(); b.onClick.AddListener(() => action());
        var t = TextAt(caption, r, size, fg, FontStyle.Bold); t.alignment = TextAnchor.MiddleCenter; Fill(t.rectTransform, 10, 6); return b;
    }
    private InputField Field(Transform parent, string label, string value, string placeholder, float height, int limit)
    {
        Copy(parent, label, 16, Navy, true); var r = Box(label, parent, Background); Round(r.GetComponent<Image>()); var le = r.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = height; le.minHeight = height; le.flexibleWidth = 1; le.minWidth = 0;
        var input = r.gameObject.AddComponent<InputField>(); var text = TextAt("", r, 16, Ink); Fill(text.rectTransform, 12, 10); text.alignment = TextAnchor.UpperLeft;
        var hint = TextAt(placeholder, r, 15, Muted); Fill(hint.rectTransform, 12, 10); hint.alignment = TextAnchor.UpperLeft;
        input.textComponent = text; input.placeholder = hint; input.targetGraphic = r.GetComponent<Image>(); input.lineType = height > 60 ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;
        input.characterLimit = limit; input.text = value; input.shouldHideMobileInput = false; return input;
    }
    private RectTransform Disclosure(Transform parent, string label, bool expanded = false)
    {
        RectTransform body = null; Button toggle = null;
        toggle = ActionButton(parent, label, () =>
        {
            bool open = !body.gameObject.activeSelf; body.gameObject.SetActive(open);
            toggle.GetComponentInChildren<Text>().text = (open ? "−  " : "+  ") + label;
        }, Background, Navy);
        toggle.GetComponentInChildren<Text>().text = (expanded ? "−  " : "+  ") + label;
        body = Stack(parent, 10); body.gameObject.SetActive(expanded); return body;
    }
    private Func<string> Choice(Transform parent, string label, string[] options, string current, Action<string> changed = null)
    {
        Copy(parent, label, 16, Navy, true); int index = Math.Max(0, Array.IndexOf(options, current));
        RectTransform choices = null;
        var toggle = ActionButton(parent, options[index] + "  ▾", () => choices.gameObject.SetActive(!choices.gameObject.activeSelf), PaleBlue, Navy);
        choices = Stack(parent, 6);
        for (int i = 0; i < options.Length; i++)
        {
            int selected = i;
            ActionButton(choices, options[i], () => { index = selected; toggle.GetComponentInChildren<Text>().text = options[index] + "  ▾"; choices.gameObject.SetActive(false); changed?.Invoke(options[index]); }, Background, Navy);
        }
        choices.gameObject.SetActive(false); return () => options[index];
    }
    private Text TextAt(string value, Transform parent, int size, Color color, FontStyle style = FontStyle.Normal)
    {
        var go = new GameObject("Texto", typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false); var t = go.GetComponent<Text>(); t.font = font; t.text = value; t.fontSize = size; t.color = color; t.fontStyle = style; t.alignment = TextAnchor.MiddleLeft; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate; t.supportRichText = false; t.raycastTarget = false; return t;
    }
    private static RectTransform Box(string name, Transform parent, Color color) { var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false); go.GetComponent<Image>().color = color; return go.GetComponent<RectTransform>(); }
    private static RectTransform Stack(Transform parent, int gap) { var go = new GameObject("Contenido", typeof(RectTransform)); go.transform.SetParent(parent, false); var r = go.GetComponent<RectTransform>(); Layout(r, 0, gap); return r; }
    private static void Layout(RectTransform r, int padding, float gap) { var g = r.gameObject.AddComponent<VerticalLayoutGroup>(); g.padding = new RectOffset(padding, padding, padding, padding); g.spacing = gap; g.childControlWidth = true; g.childControlHeight = true; g.childForceExpandWidth = true; g.childForceExpandHeight = false; }
    private static RectTransform Scroller(Transform parent, Vector2 min, Vector2 max, out ScrollRect sc)
    {
        var body = Box("Lista", parent, Color.white); Bounds(body, Vector2.zero, Vector2.one, min, max); sc = body.gameObject.AddComponent<ScrollRect>(); sc.horizontal = false; sc.movementType = ScrollRect.MovementType.Clamped; sc.scrollSensitivity = 50;
        var viewport = Box("Área visible", body, Color.white); Fill(viewport); viewport.gameObject.AddComponent<RectMask2D>(); var stack = Stack(viewport, 12); stack.anchorMin = new Vector2(0, 1); stack.anchorMax = Vector2.one; stack.pivot = new Vector2(0.5f, 1); stack.sizeDelta = Vector2.zero;
        stack.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(2, 2, 6, 22); stack.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize; sc.viewport = viewport; sc.content = stack; return stack;
    }
    private static void Bounds(RectTransform r, Vector2 min, Vector2 max, Vector2 a, Vector2 b) { r.anchorMin = min; r.anchorMax = max; r.offsetMin = a; r.offsetMax = b; }
    private static void Fill(RectTransform r, float x = 0, float y = 0) { Bounds(r, Vector2.zero, Vector2.one, new Vector2(x, y), new Vector2(-x, -y)); }
    private static void Clear(RectTransform r) { foreach (Transform child in r) { child.gameObject.SetActive(false); Destroy(child.gameObject); } }
    private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var color); return color; }
    private static void Round(Image image)
    {
        if (rounded == null)
        {
            const int size = 48; const float radius = 12; var texture = new Texture2D(size, size, TextureFormat.RGBA32, false); texture.filterMode = FilterMode.Bilinear; texture.wrapMode = TextureWrapMode.Clamp; var pixels = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) { float dx = Mathf.Max(Mathf.Abs(x + 0.5f - size / 2f) - (size / 2f - radius), 0); float dy = Mathf.Max(Mathf.Abs(y + 0.5f - size / 2f) - (size / 2f - radius), 0); pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy))); }
            texture.SetPixels(pixels); texture.Apply(false, true); rounded = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }
        image.sprite = rounded; image.type = Image.Type.Sliced;
    }
}
public sealed class AMIOnDestroy : MonoBehaviour { public Action Callback; private void OnDestroy() { Callback?.Invoke(); } }
public sealed class AMIKeyboardInset : MonoBehaviour
{
    private RectTransform body, frame;
    public void Configure(RectTransform area, RectTransform appFrame) { body = area; frame = appFrame; }
    private void LateUpdate() { if (body == null || frame == null) return; float inset = 18; if (TouchScreenKeyboard.visible && Screen.height > 0) inset = Mathf.Clamp(TouchScreenKeyboard.area.height / frame.GetComponentInParent<Canvas>().scaleFactor, 18, frame.rect.height * 0.55f); body.offsetMin = new Vector2(body.offsetMin.x, inset); }
}
[DisallowMultipleComponent] public sealed class AMIResponsiveFrame : MonoBehaviour
{
    private RectTransform rect; private float maxWidth = 430;
    public void Configure(float width) { maxWidth = width; rect = GetComponent<RectTransform>(); Apply(); }
    private void LateUpdate() { Apply(); }
    private void Apply()
    {
        if (rect == null || Screen.width <= 0 || Screen.height <= 0) return; var parent = rect.parent as RectTransform; if (parent == null) return; Rect safe = Screen.safeArea; float left = safe.xMin / Screen.width, right = safe.xMax / Screen.width;
        rect.anchorMin = new Vector2((left + right) / 2, safe.yMin / Screen.height); rect.anchorMax = new Vector2((left + right) / 2, safe.yMax / Screen.height); rect.pivot = new Vector2(0.5f, 0.5f); rect.sizeDelta = new Vector2(Mathf.Min(maxWidth, parent.rect.width * (right - left)), 0); rect.anchoredPosition = Vector2.zero;
    }
}
