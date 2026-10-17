using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Interactive, data-free AMI concept prototype. Builds its accessible UI at runtime
/// so the sample scene can be previewed immediately without manual scene wiring.
/// </summary>
public class AMIPrototype : MonoBehaviour
{
    private static readonly Color Navy = Hex("173B68");
    private static readonly Color Blue = Hex("1674D1");
    private static readonly Color Green = Hex("299B69");
    private static readonly Color Orange = Hex("F59A28");
    private static readonly Color Red = Hex("F05252");
    private static readonly Color Purple = Hex("7558B5");
    private static readonly Color PaleBlue = Hex("EAF5FF");
    private static readonly Color PaleGreen = Hex("EAF8F1");
    private static readonly Color PaleOrange = Hex("FFF3E2");
    private static readonly Color PaleRed = Hex("FFF0F0");
    private static readonly Color Ink = Hex("18324F");
    private static readonly Color Muted = Hex("69809A");
    private static readonly Color CanvasColor = Hex("F5F8FC");
    private static Sprite circleSprite;
    private static Sprite roundedSprite;

    private Canvas canvas;
    private RectTransform appFrame;
    private GameObject welcomeScreen;
    private Font font;
    private RectTransform content;
    private Text pageTitle;
    private Text greeting;
    private Text subtitle;
    private RectTransform homeGrid;
    private readonly List<GameObject> homeGridObjects = new List<GameObject>();
    private readonly List<GameObject> pageObjects = new List<GameObject>();
    private readonly List<Button> navButtons = new List<Button>();
    private int activeTab;
    private bool sharedDay = true;
    private bool morningDone;
    private bool vitaminDone;
    private bool pendingResolved;
    private string latestNote = "Desayunó bien. Próxima actividad: llamada con Ana a las 10:30.";
    private float lastGridWidth = -1f;

    private const float DesignWidth = 430f;
    private const float DesignHeight = 932f;
    private const float SideInset = 20f;
    private const float CardGap = 12f;

    private void Start()
    {
        Application.targetFrameRate = 60;
        if (circleSprite == null) circleSprite = CreateCircleSprite();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
        BuildShell();
        ShowTab(0);
        BuildWelcomeScreen();
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(go);
    }

    private void BuildWelcomeScreen()
    {
        var overlay = new GameObject("Bienvenida AMI", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(appFrame, false);
        RectTransform panel = overlay.GetComponent<RectTransform>();
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = Color.white;
        welcomeScreen = overlay;

        var stackObject = new GameObject("Contenido de bienvenida", typeof(RectTransform), typeof(VerticalLayoutGroup));
        stackObject.transform.SetParent(panel, false);
        RectTransform stackRect = stackObject.GetComponent<RectTransform>();
        stackRect.anchorMin = Vector2.zero;
        stackRect.anchorMax = Vector2.one;
        stackRect.offsetMin = new Vector2(26, 28);
        stackRect.offsetMax = new Vector2(-26, -32);
        VerticalLayoutGroup stack = stackObject.GetComponent<VerticalLayoutGroup>();
        stack.padding = new RectOffset(0, 0, 0, 0);
        stack.spacing = 14;
        stack.childAlignment = TextAnchor.MiddleCenter;
        stack.childControlWidth = true;
        stack.childControlHeight = true;
        stack.childForceExpandWidth = true;
        stack.childForceExpandHeight = false;

        WelcomeLabel("♥  AMI", 34, Navy, FontStyle.Bold, 56);
        WelcomeLabel("SIEMPRE CERCA DE LO QUE IMPORTA", 11, Green, FontStyle.Bold, 25);
        WelcomeLabel("Cuidar también\nes amar", 34, Navy, FontStyle.Bold, 92);
        WelcomeLabel("Una ayuda sencilla para organizar el cuidado\ny sentir a la familia más cerca.", 16, Muted, FontStyle.Normal, 58);

        var spacer = new GameObject("Espacio", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(stackObject.transform, false);
        spacer.GetComponent<LayoutElement>().preferredHeight = 22;
        spacer.GetComponent<LayoutElement>().minHeight = 22;

        WelcomeFeature("◷", "Rutinas claras", "Medicamentos y actividades en un solo lugar", PaleBlue, Blue);
        WelcomeFeature("♥", "Familia conectada", "Un resumen compartido para acompañar a María", PaleGreen, Green);
        WelcomeFeature("✓", "Cuidado organizado", "Pendientes visibles y fáciles de confirmar", PaleOrange, Orange);

        var buttonSlot = new GameObject("Acción principal", typeof(RectTransform), typeof(LayoutElement));
        buttonSlot.transform.SetParent(stackObject.transform, false);
        buttonSlot.GetComponent<LayoutElement>().preferredHeight = 58;
        buttonSlot.GetComponent<LayoutElement>().minHeight = 52;
        MakeButton("Entrar a AMI", buttonSlot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Blue, Color.white, 18, EnterApp);

        WelcomeLabel("Prototipo de demostración · datos ficticios", 11, Muted, FontStyle.Normal, 22);
        Canvas.ForceUpdateCanvases();
    }

    private Text WelcomeLabel(string value, int size, Color color, FontStyle style, float height)
    {
        var go = new GameObject("Bienvenida · " + value, typeof(RectTransform), typeof(LayoutElement), typeof(Text));
        go.transform.SetParent(welcomeScreen.transform.GetChild(0), false);
        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.minHeight = height;
        layout.flexibleWidth = 1;
        layout.minWidth = 0;
        Text text = go.GetComponent<Text>();
        ConfigureText(text, value, size, color, style, TextAnchor.MiddleCenter);
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private void WelcomeFeature(string icon, string title, string detail, Color background, Color tint)
    {
        var row = new GameObject("Beneficio · " + title, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        row.transform.SetParent(welcomeScreen.transform.GetChild(0), false);
        LayoutElement layout = row.GetComponent<LayoutElement>();
        layout.preferredHeight = 66;
        layout.minHeight = 58;
        layout.flexibleWidth = 1;
        layout.minWidth = 0;
        Image card = row.GetComponent<Image>();
        card.color = background;
        Round(card, 14);
        RectTransform rect = row.GetComponent<RectTransform>();

        Text badge = Label(icon, rect, 23, tint, FontStyle.Bold, TextAnchor.MiddleCenter);
        badge.rectTransform.anchorMin = new Vector2(0, 0.5f);
        badge.rectTransform.anchorMax = new Vector2(0, 0.5f);
        badge.rectTransform.pivot = new Vector2(0, 0.5f);
        badge.rectTransform.anchoredPosition = new Vector2(16, 0);
        badge.rectTransform.sizeDelta = new Vector2(40, 40);

        Text heading = Label(title, rect, 15, Navy, FontStyle.Bold, TextAnchor.MiddleLeft);
        heading.rectTransform.anchorMin = new Vector2(0, 0.5f);
        heading.rectTransform.anchorMax = new Vector2(1, 0.5f);
        heading.rectTransform.pivot = new Vector2(0, 0.5f);
        heading.rectTransform.offsetMin = new Vector2(68, 8);
        heading.rectTransform.offsetMax = new Vector2(-12, 8);
        heading.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 22);

        Text description = Label(detail, rect, 12, Muted, FontStyle.Normal, TextAnchor.MiddleLeft);
        description.rectTransform.anchorMin = new Vector2(0, 0.5f);
        description.rectTransform.anchorMax = new Vector2(1, 0.5f);
        description.rectTransform.pivot = new Vector2(0, 0.5f);
        description.rectTransform.offsetMin = new Vector2(68, -15);
        description.rectTransform.offsetMax = new Vector2(-12, -15);
        description.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 22);
        description.resizeTextForBestFit = true;
        description.resizeTextMinSize = 10;
        description.resizeTextMaxSize = 12;
    }

    private void EnterApp()
    {
        if (welcomeScreen != null) welcomeScreen.SetActive(false);
        ShowTab(0);
    }

    private void BuildShell()
    {
        var root = new GameObject("AMI Prototype", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(DesignWidth, DesignHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
        RectTransform rootRect = root.GetComponent<RectTransform>();

        var background = Rect("Background", rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image bgImage = background.gameObject.AddComponent<Image>(); bgImage.color = CanvasColor;
        background.SetAsFirstSibling();

        var frameObject = new GameObject("AMI Mobile Frame", typeof(RectTransform), typeof(Image), typeof(AMIResponsiveFrame));
        frameObject.transform.SetParent(root.transform, false);
        appFrame = frameObject.GetComponent<RectTransform>();
        appFrame.anchorMin = new Vector2(0.5f, 0f);
        appFrame.anchorMax = new Vector2(0.5f, 1f);
        appFrame.pivot = new Vector2(0.5f, 0.5f);
        appFrame.sizeDelta = new Vector2(DesignWidth, 0f);
        frameObject.GetComponent<Image>().color = Color.white;
        frameObject.GetComponent<AMIResponsiveFrame>().Configure(DesignWidth);

        var header = Rect("Header", appFrame, new Vector2(0, 1), new Vector2(1, 1), new Vector2(SideInset, -102), new Vector2(-SideInset, -10));
        var heart = Label("♥", header, 24, Green, FontStyle.Bold, TextAnchor.MiddleLeft);
        heart.rectTransform.anchorMin = new Vector2(0, 1); heart.rectTransform.anchorMax = new Vector2(0, 1); heart.rectTransform.pivot = new Vector2(0, 1); heart.rectTransform.anchoredPosition = Vector2.zero; heart.rectTransform.sizeDelta = new Vector2(29, 35);
        var logo = Label("AMI", header, 29, Navy, FontStyle.Bold, TextAnchor.MiddleLeft);
        logo.rectTransform.anchorMin = new Vector2(0, 1); logo.rectTransform.anchorMax = new Vector2(0, 1);
        logo.rectTransform.pivot = new Vector2(0, 1); logo.rectTransform.anchoredPosition = new Vector2(30, 0); logo.rectTransform.sizeDelta = new Vector2(100, 38);
        var brand = Label("Siempre cerca de lo que importa", header, 11, Muted, FontStyle.Normal, TextAnchor.MiddleLeft);
        brand.rectTransform.anchorMin = new Vector2(0, 1); brand.rectTransform.anchorMax = new Vector2(1, 1); brand.rectTransform.pivot = new Vector2(0, 1); brand.rectTransform.anchoredPosition = new Vector2(1, -40); brand.rectTransform.sizeDelta = new Vector2(-64, 19);
        var settings = MakeButton("⚙", header, new Vector2(1, 1), new Vector2(1, 1), new Vector2(0, -1), new Vector2(42, 42), PaleBlue, Navy, 19, () => Toast("Configuración de accesibilidad"));
        settings.GetComponent<RectTransform>().pivot = new Vector2(1, 1);

        pageTitle = Label("", appFrame, 13, Blue, FontStyle.Bold, TextAnchor.MiddleLeft);
        pageTitle.rectTransform.anchorMin = new Vector2(0, 1); pageTitle.rectTransform.anchorMax = new Vector2(1, 1); pageTitle.rectTransform.pivot = new Vector2(0.5f, 1); pageTitle.rectTransform.anchoredPosition = new Vector2(0, -112); pageTitle.rectTransform.sizeDelta = new Vector2(-SideInset * 2f, 23);
        greeting = Label("", appFrame, 23, Navy, FontStyle.Bold, TextAnchor.MiddleLeft);
        greeting.rectTransform.anchorMin = new Vector2(0, 1); greeting.rectTransform.anchorMax = new Vector2(1, 1); greeting.rectTransform.pivot = new Vector2(0.5f, 1); greeting.rectTransform.anchoredPosition = new Vector2(0, -137); greeting.rectTransform.sizeDelta = new Vector2(-SideInset * 2f, 33);
        subtitle = Label("", appFrame, 14, Muted, FontStyle.Normal, TextAnchor.MiddleLeft);
        subtitle.rectTransform.anchorMin = new Vector2(0, 1); subtitle.rectTransform.anchorMax = new Vector2(1, 1); subtitle.rectTransform.pivot = new Vector2(0.5f, 1); subtitle.rectTransform.anchoredPosition = new Vector2(0, -169); subtitle.rectTransform.sizeDelta = new Vector2(-SideInset * 2f, 24);

        var body = Rect("Scrollable content", appFrame, new Vector2(0, 0), new Vector2(1, 1), new Vector2(SideInset, 84), new Vector2(-SideInset, -198));
        var scroll = body.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 50;
        var viewport = Rect("Viewport", body, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var mask = viewport.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var viewportImage = viewport.gameObject.AddComponent<Image>(); viewportImage.color = Color.white;
        content = Rect("Content", viewport, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        content.pivot = new Vector2(0.5f, 1);
        var verticalLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        verticalLayout.padding = new RectOffset(0, 0, 6, 18);
        verticalLayout.spacing = 10;
        verticalLayout.childAlignment = TextAnchor.UpperCenter;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = true;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;
        var contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var homeGridObject = new GameObject("Accesos rápidos", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
        homeGridObject.transform.SetParent(content, false);
        var homeGridLayoutElement = homeGridObject.GetComponent<LayoutElement>();
        homeGridLayoutElement.flexibleWidth = 1;
        homeGridLayoutElement.minWidth = 0;
        homeGrid = homeGridObject.GetComponent<RectTransform>();
        homeGrid.anchorMin = new Vector2(0, 1); homeGrid.anchorMax = new Vector2(1, 1); homeGrid.pivot = new Vector2(0.5f, 1);
        homeGrid.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);
        var gridLayout = homeGridObject.GetComponent<GridLayoutGroup>();
        gridLayout.padding = new RectOffset(0, 0, 0, 0);
        gridLayout.spacing = new Vector2(CardGap, CardGap);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 2;
        gridLayout.cellSize = new Vector2(170, 142);
        scroll.viewport = viewport; scroll.content = content;

        var nav = Rect("Bottom navigation", appFrame, new Vector2(0, 0), new Vector2(1, 0), new Vector2(10, 8), new Vector2(-10, 72));
        nav.pivot = new Vector2(0.5f, 0);
        Image navBg = nav.gameObject.AddComponent<Image>(); navBg.color = Color.white;
        var navLayout = nav.gameObject.AddComponent<HorizontalLayoutGroup>();
        navLayout.padding = new RectOffset(3, 3, 4, 4); navLayout.spacing = 0; navLayout.childAlignment = TextAnchor.MiddleCenter; navLayout.childControlWidth = true; navLayout.childControlHeight = true; navLayout.childForceExpandWidth = true; navLayout.childForceExpandHeight = true;
        AddNav(nav, "⌂", "Inicio", 0); AddNav(nav, "◷", "Mi día", 1); AddNav(nav, "♡", "Familia", 4);
    }

    private void AddNav(RectTransform parent, string icon, string label, int tab)
    {
        var button = MakeButton(icon + "\n" + label, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white, Muted, 13, () => ShowTab(tab));
        var layout = button.gameObject.AddComponent<LayoutElement>(); layout.flexibleWidth = 1; layout.minHeight = 58;
        navButtons.Add(button);
    }

    private void ShowTab(int tab)
    {
        activeTab = tab;
        foreach (var go in pageObjects)
        {
            go.transform.SetParent(null, false);
            Destroy(go);
        }
        pageObjects.Clear();
        foreach (var go in homeGridObjects)
        {
            go.transform.SetParent(null, false);
            Destroy(go);
        }
        homeGridObjects.Clear();
        homeGrid.gameObject.SetActive(tab == 0);
        ConfigureHomeGridWidth();
        for (int i = 0; i < navButtons.Count; i++)
        {
            var t = navButtons[i].GetComponentInChildren<Text>();
            bool selected = (i == 0 && tab == 0) || (i == 1 && tab >= 1 && tab <= 3) || (i == 2 && tab == 4);
            t.color = selected ? Blue : Muted;
            t.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
        }
        string[] titles = { "Inicio", "Mi día", "Medicamentos", "Avisos", "Familia", "Cuidador" };
        string[] greetings = { "Hola, María 👋", "Tu día, paso a paso", "Mis medicamentos", "Avisos importantes", "Cuidamos en equipo", "Vista del cuidador" };
        string[] subtitles = { "Qué bueno verte hoy. Todo está más claro.", "Viernes, 9 de octubre · rutina de hoy", "Horarios y dosis de hoy", "Pendientes que necesitan atención", "La información compartida da tranquilidad.", "Un resumen sencillo del cuidado de hoy." };
        pageTitle.text = titles[tab];
        greeting.text = greetings[tab];
        subtitle.text = subtitles[tab];
        if (tab == 0) BuildHome();
        if (tab == 1) BuildDay();
        if (tab == 2) BuildMedicines();
        if (tab == 3) BuildAlerts();
        if (tab == 4) BuildFamily();
        if (tab == 5) BuildCaregiver();
        Canvas.ForceUpdateCanvases();
        if (tab == 0)
        {
            ConfigureHomeGridWidth();
            LayoutRebuilder.ForceRebuildLayoutImmediate(homeGrid);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }
        content.anchoredPosition = Vector2.zero;
        Canvas.ForceUpdateCanvases();
        var scroll = content.GetComponentInParent<ScrollRect>();
        if (scroll != null) scroll.verticalNormalizedPosition = 1f;
    }

    private void BuildHome()
    {
        Section("TU RESUMEN DE HOY", "Lo importante, de un vistazo");
        GridCard(homeGrid, "▤", "Resumen", "Cómo va todo hoy", PaleBlue, Blue, () => ShowTab(1));
        GridCard(homeGrid, "✓", "Pendientes", pendingResolved ? "Todo al día" : "1 por revisar", PaleOrange, Orange, () => ShowTab(3));
        GridCard(homeGrid, "◉", "Medicamentos", (morningDone ? 1 : 0) + (vitaminDone ? 1 : 0) + " de 2 confirmados", PaleGreen, Green, () => ShowTab(2));
        GridCard(homeGrid, "♥", "Alertas", pendingResolved ? "Sin avisos nuevos" : "1 aviso importante", PaleRed, Red, () => ShowTab(3));
        GridCard(homeGrid, "♧", "Cuidador", "Apoyo y contacto", Hex("F1EEFF"), Purple, () => ShowTab(5));
        GridCard(homeGrid, "?", "Ayuda", "Resuelve tus dudas", PaleBlue, Blue, () => Toast("Estamos aquí para ayudarte"));
        Section("MEMORIA COMPARTIDA", "Un registro sencillo para estar cerca");
        Card("Turno de hoy", latestNote, PaleBlue, 88);
        PrimaryButton("+  Registrar cuidado", "", OpenRecord);
        // VerticalLayoutGroup follows sibling order, so explicitly place the grid
        // between the introduction and the shared-memory section.
        homeGrid.SetSiblingIndex(1);
        ConfigureHomeGridWidth();
    }

    private void ConfigureHomeGridWidth()
    {
        RectTransform viewport = content.parent.GetComponent<RectTransform>();
        float available = viewport.rect.width;
        if (available <= 0) available = DesignWidth - SideInset * 2f;
        available = Mathf.Min(available, DesignWidth - SideInset * 2f);
        GridLayoutGroup layout = homeGrid.GetComponent<GridLayoutGroup>();
        int columns = available < 300f ? 1 : 2;
        layout.constraintCount = columns;
        float cellWidth = columns == 1 ? available : Mathf.Max(116f, (available - CardGap) * 0.5f);
        float cellHeight = 142f;
        layout.cellSize = new Vector2(cellWidth, cellHeight);
        homeGrid.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);
        LayoutElement gridElement = homeGrid.GetComponent<LayoutElement>();
        gridElement.preferredWidth = available;
        gridElement.minWidth = available;
        float rows = Mathf.Ceil(homeGrid.childCount / (float)columns);
        float gridHeight = rows * layout.cellSize.y + Mathf.Max(0, rows - 1) * layout.spacing.y;
        gridElement.preferredHeight = gridHeight;
        gridElement.minHeight = gridHeight;
        homeGrid.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, gridHeight);
        lastGridWidth = available;
    }

    private void LateUpdate()
    {
        if (homeGrid == null || activeTab != 0) return;
        float width = homeGrid.parent.GetComponent<RectTransform>().rect.width;
        if (Mathf.Abs(width - lastGridWidth) < 1f) return;
        ConfigureHomeGridWidth();
        LayoutRebuilder.ForceRebuildLayoutImmediate(homeGrid);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    private void BuildDay()
    {
        bool medicinePending = !morningDone;
        Section("MEDICAMENTOS", "Horarios de hoy");
        MedicineCard("8:00", "Presión arterial", "1 tableta · después del desayuno", morningDone, () => { morningDone = true; UpdateMedicationStatus(); Toast("Toma registrada. ¡Gracias!"); ShowTab(activeTab); });
        MedicineCard("13:00", "Vitamina D", "1 cápsula · con el almuerzo", vitaminDone, () => { vitaminDone = true; UpdateMedicationStatus(); Toast("Toma registrada. ¡Gracias!"); ShowTab(activeTab); });
        Section("ACTIVIDADES", "Pequeños recordatorios del día");
        ActivityRow("◷", "10:30", "Llamar a Ana", "Conversar un momento", PaleBlue, Blue);
        ActivityRow("○", "17:00", "Paseo suave", "Si el clima acompaña", Hex("F1EEFF"), Purple);
        Section("REGISTRO DEL CUIDADO", "Comparte cómo va el día");
        Card("Nota para la familia", "Puedes dictar una nota o escribir un breve resumen del turno.", PaleGreen, 82);
        PrimaryButton("+  Agregar nota del turno", "", OpenRecord);
        if (medicinePending) RowCard("!", "Aviso pendiente", "La toma de las 8:00 aún no se confirma", PaleRed, Red, () => { morningDone = true; UpdateMedicationStatus(); Toast("Toma marcada como realizada"); ShowTab(1); });
    }

    private void BuildMedicines()
    {
        Section("TRATAMIENTO DE HOY", "Medicamentos registrados");
        MedicineCard("8:00", "Presión arterial", "1 tableta · después del desayuno", morningDone, () => { morningDone = true; UpdateMedicationStatus(); Toast("Toma registrada. ¡Gracias!"); ShowTab(2); });
        MedicineCard("13:00", "Vitamina D", "1 cápsula · con el almuerzo", vitaminDone, () => { vitaminDone = true; UpdateMedicationStatus(); Toast("Toma registrada. ¡Gracias!"); ShowTab(2); });
        Section("PRÓXIMA DOSIS", "El siguiente horario aparece aquí");
        Card("Vitamina D · 1:00 p. m.", "Recuerda tomarla junto con el almuerzo. Puedes confirmar la toma cuando esté lista.", PaleBlue, 92);
        PrimaryButton("+  Agregar medicamento", "", () => Toast("Formulario de ejemplo: agregar medicamento"));
        Text note = AddLabel("Los medicamentos son datos de demostración. Sigue siempre las indicaciones del profesional de salud.", 12, Muted, FontStyle.Italic, 48); note.alignment = TextAnchor.MiddleCenter;
    }

    private void BuildAlerts()
    {
        Section("PENDIENTES", pendingResolved ? "Todo está al día" : "Hay un aviso por revisar");
        if (!pendingResolved)
            RowCard("!", "Medicamento pendiente", "La toma de las 8:00 aún no se confirma", PaleRed, Red, () => { pendingResolved = true; morningDone = true; UpdateMedicationStatus(); Toast("Marcado como realizado en la demostración"); ShowTab(3); });
        else
            RowCard("✓", "Sin pendientes", "No hay avisos por revisar ahora", PaleGreen, Green, () => Toast("¡Todo en orden!"));
        Section("PRÓXIMOS RECORDATORIOS", "Actividades de hoy");
        ActivityRow("◷", "10:30", "Llamar a Ana", "Recordatorio de actividad", PaleBlue, Blue);
        RowCard("▣", "Mañana · Cita médica", "10:30 a. m. · Centro médico", PaleOrange, Hex("D88316"), () => Toast("Cita de ejemplo"));
        Card("Avisos tranquilos", "AMI reúne aquí los pendientes para que sea más fácil saber qué necesita atención.", PaleBlue, 84);
    }

    private void BuildCaregiver()
    {
        Section("RESUMEN DEL TURNO", "Viernes, 9 de octubre");
        Card("María López · En casa", latestNote + "\nMedicamentos: " + (morningDone ? "mañana confirmada" : "mañana pendiente"), PaleBlue, 108);
        Section("ACTIVIDAD REGISTRADA", "Información compartida con la familia");
        RowCard("✓", "Rutina de la mañana", "Desayuno registrado · 8:20 a. m.", PaleGreen, Green, () => Toast("Detalle del registro de ejemplo"));
        ActivityRow("◷", "10:30", "Llamada con Ana", "Próxima actividad", PaleOrange, Orange);
        Section("CIERRE DEL TURNO", "Deja una nota para el siguiente cuidador");
        Card("Memoria compartida", "Un resumen breve ayuda a que todos sepan qué pasó durante el día.", PaleGreen, 82);
        PrimaryButton("●  Registrar nota del turno", "", OpenRecord);
        PrimaryButton("Compartir resumen con la familia", "", () => { sharedDay = true; Toast("Resumen compartido en esta demostración"); });
    }

    private void BuildFamily()
    {
        Section("PERSONA ACOMPAÑADA", "Perfil de ejemplo");
        Card("María López · Mamá", "Estado de hoy: " + (morningDone ? "medicación de la mañana confirmada" : "medicación de la mañana pendiente") + ". " + (sharedDay ? "Resumen compartido con la familia." : "Resumen privado por ahora."), PaleBlue, 94);
        Section("CONTACTOS CONECTADOS", "Personas que acompañan el cuidado");
        RowCard("A", "Ana López", "Hija · Disponible", PaleGreen, Green, () => Toast("Llamada de ejemplo a Ana"));
        RowCard("C", "Carlos López", "Hijo · Última visita: hoy", PaleBlue, Blue, () => Toast("Contacto de ejemplo"));
        Section("RESUMEN COMPARTIDO", "Actualizado hoy, 9:15 a. m.");
        Card("El día de María", latestNote + "\n" + (pendingResolved ? "No hay pendientes." : "La toma de la mañana está pendiente de confirmar."), PaleOrange, 112);
        ToggleRow("Compartir resumen del día", "La familia puede consultar el registro", sharedDay, value => { sharedDay = value; Toast(value ? "Resumen compartido" : "Resumen no compartido"); });
        PrimaryButton("☎  Contactar a Ana", "", () => Toast("Acción de contacto de ejemplo"));
        Text small = AddLabel("Los nombres y registros son datos ficticios para este prototipo.", 12, Muted, FontStyle.Italic, 42); small.alignment = TextAnchor.MiddleCenter;
    }

    private void UpdateMedicationStatus()
    {
        pendingResolved = morningDone;
    }

    private void ActivityRow(string icon, string time, string title, string detail, Color background, Color tint)
    {
        var row = NewCard(88, background);
        var bubble = Rect("Icono", row, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
        bubble.pivot = new Vector2(0, 0.5f);
        bubble.anchoredPosition = new Vector2(16, 0);
        bubble.sizeDelta = new Vector2(48, 48);
        Image image = bubble.gameObject.AddComponent<Image>(); image.color = Color.white; Circle(image);
        Text iconText = Label(icon, bubble, 20, tint, FontStyle.Bold, TextAnchor.MiddleCenter); iconText.rectTransform.offsetMin = Vector2.zero; iconText.rectTransform.offsetMax = Vector2.zero;
        Text timeLabel = Label(time, row, 13, tint, FontStyle.Bold, TextAnchor.MiddleLeft);
        timeLabel.rectTransform.anchorMin = new Vector2(0, 0.5f); timeLabel.rectTransform.anchorMax = new Vector2(0.35f, 0.5f); timeLabel.rectTransform.pivot = new Vector2(0, 0.5f); timeLabel.rectTransform.offsetMin = new Vector2(74, 0); timeLabel.rectTransform.offsetMax = new Vector2(-4, 0); timeLabel.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 22);
        Text titleLabel = Label(title, row, 15, Navy, FontStyle.Bold, TextAnchor.MiddleLeft);
        titleLabel.rectTransform.anchorMin = new Vector2(0.35f, 0.5f); titleLabel.rectTransform.anchorMax = new Vector2(1, 0.5f); titleLabel.rectTransform.pivot = new Vector2(0, 0.5f); titleLabel.rectTransform.offsetMin = new Vector2(2, 14); titleLabel.rectTransform.offsetMax = new Vector2(-14, 14); titleLabel.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 22);
        Text detailLabel = Label(detail, row, 12, Muted, FontStyle.Normal, TextAnchor.MiddleLeft);
        detailLabel.rectTransform.anchorMin = new Vector2(0.35f, 0.5f); detailLabel.rectTransform.anchorMax = new Vector2(1, 0.5f); detailLabel.rectTransform.pivot = new Vector2(0, 0.5f); detailLabel.rectTransform.offsetMin = new Vector2(2, -14); detailLabel.rectTransform.offsetMax = new Vector2(-14, -14); detailLabel.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 22);
    }

    private void OpenRecord()
    {
        var overlay = new GameObject("Registro del turno", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(appFrame, false); pageObjects.Add(overlay);
        RectTransform r = overlay.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0.04f, 0.11f, 0.2f, 0.55f);
        var sheet = Rect("Registro", r, new Vector2(0, 0), new Vector2(1, 0.78f), new Vector2(SideInset, 12), new Vector2(-SideInset, 0));
        sheet.pivot = new Vector2(0.5f, 0); var bg = sheet.gameObject.AddComponent<Image>(); bg.color = Color.white; Round(bg, 22);
        MakeButton("×", sheet, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -12), new Vector2(36, 36), PaleBlue, Navy, 22, () => CloseOverlay(overlay));
        Label("REGISTRO DEL TURNO", sheet, 13, Blue, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(0, -24), new Vector2(-44, 28));
        Label("¿Cómo va el cuidado?", sheet, 25, Navy, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(0, -58), new Vector2(-44, 38));
        Label("Agrega una nota para que la familia sepa cómo estuvo el día.", sheet, 15, Muted, FontStyle.Normal, TextAnchor.UpperLeft, new Vector2(0, -104), new Vector2(-44, 46));
        var inputGo = new GameObject("Escribe una nota", typeof(RectTransform), typeof(Image), typeof(InputField)); inputGo.transform.SetParent(sheet, false);
        RectTransform ir = inputGo.GetComponent<RectTransform>(); ir.anchorMin = new Vector2(0, 1); ir.anchorMax = new Vector2(1, 1); ir.pivot = new Vector2(0.5f, 1); ir.anchoredPosition = new Vector2(0, -158); ir.sizeDelta = new Vector2(-SideInset * 2f, 88);
        Image inputBg = inputGo.GetComponent<Image>(); inputBg.color = CanvasColor; Round(inputBg, 12);
        var placeholder = Label("Ej.: almorzó bien, salió a caminar…", inputGo.transform as RectTransform, 15, Muted, FontStyle.Italic, TextAnchor.UpperLeft); placeholder.rectTransform.offsetMin = new Vector2(14, 12); placeholder.rectTransform.offsetMax = new Vector2(-14, -12);
        var inputText = Label("", inputGo.transform as RectTransform, 16, Ink, FontStyle.Normal, TextAnchor.UpperLeft); inputText.rectTransform.offsetMin = new Vector2(14, 12); inputText.rectTransform.offsetMax = new Vector2(-14, -12);
        InputField input = inputGo.GetComponent<InputField>(); input.textComponent = inputText; input.placeholder = placeholder; input.lineType = InputField.LineType.MultiLineNewline;
        MakeButton("●   Mantén para registrar por voz", sheet, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -272), new Vector2(-44, 56), PaleBlue, Blue, 16, () => Toast("La grabación por voz estará disponible en una siguiente versión."));
        Label("En este prototipo puedes escribir la nota.\nNo se envía información real.", sheet, 13, Muted, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0, -340), new Vector2(-44, 44));
        MakeButton("Guardar nota", sheet, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 16), new Vector2(-44, 58), Blue, Color.white, 17, () =>
        {
            string note = input.text.Trim();
            if (!string.IsNullOrEmpty(note)) latestNote = note;
            CloseOverlay(overlay);
            Toast(string.IsNullOrEmpty(note) ? "Registro guardado en la demostración" : "Nota agregada al resumen");
            ShowTab(activeTab);
        });
    }

    private void CloseOverlay(GameObject overlay)
    {
        pageObjects.Remove(overlay); Destroy(overlay);
    }

    private void ToggleRow(string title, string description, bool value, Action<bool> changed)
    {
        var row = NewCard(92, PaleGreen);
        Text t = Label(title, row, 15, Ink, FontStyle.Bold, TextAnchor.MiddleLeft); t.rectTransform.anchorMin = new Vector2(0, 1); t.rectTransform.anchorMax = new Vector2(1, 1); t.rectTransform.pivot = new Vector2(0.5f, 1); t.rectTransform.anchoredPosition = new Vector2(-32, -8); t.rectTransform.sizeDelta = new Vector2(-96, 38);
        Text d = Label(description, row, 12, Muted, FontStyle.Normal, TextAnchor.MiddleLeft); d.rectTransform.anchorMin = new Vector2(0, 0); d.rectTransform.anchorMax = new Vector2(1, 0); d.rectTransform.pivot = new Vector2(0.5f, 0); d.rectTransform.anchoredPosition = new Vector2(-32, 8); d.rectTransform.sizeDelta = new Vector2(-96, 34);
        var toggleGo = new GameObject("Compartir", typeof(RectTransform), typeof(Toggle)); toggleGo.transform.SetParent(row, false);
        RectTransform tr = toggleGo.GetComponent<RectTransform>(); tr.anchorMin = new Vector2(1, 0.5f); tr.anchorMax = new Vector2(1, 0.5f); tr.pivot = new Vector2(1, 0.5f); tr.anchoredPosition = new Vector2(-16, 0); tr.sizeDelta = new Vector2(50, 32);
        var trackGo = new GameObject("Track", typeof(RectTransform), typeof(Image)); trackGo.transform.SetParent(toggleGo.transform, false); RectTransform rr = trackGo.GetComponent<RectTransform>(); rr.anchorMin = Vector2.zero; rr.anchorMax = Vector2.one; rr.offsetMin = Vector2.zero; rr.offsetMax = Vector2.zero; Image track = trackGo.GetComponent<Image>(); track.color = value ? Green : Hex("B9C5D1"); Circle(track);
        var knobGo = new GameObject("Knob", typeof(RectTransform), typeof(Image)); knobGo.transform.SetParent(toggleGo.transform, false); RectTransform kr = knobGo.GetComponent<RectTransform>(); kr.anchorMin = new Vector2(value ? 1 : 0, 0.5f); kr.anchorMax = kr.anchorMin; kr.pivot = new Vector2(0.5f, 0.5f); kr.anchoredPosition = new Vector2(value ? -16 : 16, 0); kr.sizeDelta = new Vector2(24, 24); Image knob = knobGo.GetComponent<Image>(); knob.color = Color.white; Circle(knob);
        Toggle toggle = toggleGo.GetComponent<Toggle>(); toggle.targetGraphic = track; toggle.graphic = knob; toggle.isOn = value; toggle.onValueChanged.AddListener(v => { track.color = v ? Green : Hex("B9C5D1"); kr.anchorMin = kr.anchorMax = new Vector2(v ? 1 : 0, 0.5f); kr.anchoredPosition = new Vector2(v ? -16 : 16, 0); changed(v); });
    }

    private void MedicineCard(string time, string medicine, string detail, bool done, Action action)
    {
        var card = NewCard(156, PaleBlue);
        var accent = Rect("Estado", card, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(5, 0));
        Image accentImage = accent.gameObject.AddComponent<Image>();
        accentImage.color = done ? Green : Blue;
        Text timeText = Label(time, card, 13, Blue, FontStyle.Bold, TextAnchor.MiddleLeft); timeText.rectTransform.anchorMin = new Vector2(0, 1); timeText.rectTransform.anchorMax = new Vector2(1, 1); timeText.rectTransform.pivot = new Vector2(0, 1); timeText.rectTransform.anchoredPosition = new Vector2(18, -10); timeText.rectTransform.sizeDelta = new Vector2(-36, 20);
        Label(medicine, card, 17, Ink, FontStyle.Bold, TextAnchor.MiddleLeft, new Vector2(0, -34), new Vector2(-36, 24));
        Label(detail, card, 13, Muted, FontStyle.Normal, TextAnchor.MiddleLeft, new Vector2(0, -61), new Vector2(-36, 23));
        MakeButton(done ? "✓  Toma confirmada" : "Confirmar toma", card, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 12), new Vector2(-28, 44), done ? PaleGreen : Blue, done ? Green : Color.white, 14, action);
    }

    private void Section(string eyebrow, string title)
    {
        var go = new GameObject("Sección " + eyebrow, typeof(RectTransform), typeof(LayoutElement)); go.transform.SetParent(content, false); pageObjects.Add(go);
        LayoutElement sectionLayout = go.GetComponent<LayoutElement>(); sectionLayout.preferredHeight = 60; sectionLayout.minHeight = 60; sectionLayout.flexibleWidth = 1;
        RectTransform r = go.GetComponent<RectTransform>(); r.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 65);
        Text e = Label(eyebrow, r, 11, Blue, FontStyle.Bold, TextAnchor.MiddleLeft); e.rectTransform.anchorMin = new Vector2(0, 1); e.rectTransform.anchorMax = new Vector2(1, 1); e.rectTransform.pivot = new Vector2(0, 1); e.rectTransform.anchoredPosition = new Vector2(2, -2); e.rectTransform.sizeDelta = new Vector2(-4, 20);
        Text t = Label(title, r, 15, Muted, FontStyle.Normal, TextAnchor.MiddleLeft); t.rectTransform.anchorMin = new Vector2(0, 1); t.rectTransform.anchorMax = new Vector2(1, 1); t.rectTransform.pivot = new Vector2(0, 1); t.rectTransform.anchoredPosition = new Vector2(2, -23); t.rectTransform.sizeDelta = new Vector2(-4, 25);
    }

    private void GridCard(Transform parent, string icon, string title, string detail, Color bg, Color tint, Action action)
    {
        var go = new GameObject("Acceso " + title, typeof(RectTransform), typeof(Image), typeof(LayoutElement)); go.transform.SetParent(parent, false); homeGridObjects.Add(go);
        RectTransform card = go.GetComponent<RectTransform>();
        LayoutElement element = go.GetComponent<LayoutElement>(); element.minWidth = 0; element.flexibleWidth = 1; element.preferredHeight = 142;
        Image cardImage = go.GetComponent<Image>(); cardImage.color = bg; Round(cardImage, 18);
        var iconBubble = Rect("Icono", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
        iconBubble.pivot = new Vector2(0.5f, 1);
        iconBubble.anchoredPosition = new Vector2(0, -12);
        iconBubble.sizeDelta = new Vector2(50, 50);
        Image bubbleImage = iconBubble.gameObject.AddComponent<Image>(); bubbleImage.color = Color.white; Circle(bubbleImage);
        Text i = Label(icon, iconBubble, 21, tint, FontStyle.Bold, TextAnchor.MiddleCenter); i.rectTransform.anchorMin = Vector2.zero; i.rectTransform.anchorMax = Vector2.one; i.rectTransform.offsetMin = Vector2.zero; i.rectTransform.offsetMax = Vector2.zero;
        Text titleText = Label(title, card, 16, Navy, FontStyle.Bold, TextAnchor.MiddleCenter); titleText.rectTransform.anchorMin = new Vector2(0, 1); titleText.rectTransform.anchorMax = new Vector2(1, 1); titleText.rectTransform.pivot = new Vector2(0.5f, 1); titleText.rectTransform.anchoredPosition = new Vector2(0, -69); titleText.rectTransform.sizeDelta = new Vector2(-12, 25);
        Text detailText = Label(detail, card, 13, Muted, FontStyle.Normal, TextAnchor.MiddleCenter); detailText.rectTransform.anchorMin = new Vector2(0, 1); detailText.rectTransform.anchorMax = new Vector2(1, 1); detailText.rectTransform.pivot = new Vector2(0.5f, 1); detailText.rectTransform.anchoredPosition = new Vector2(0, -97); detailText.rectTransform.sizeDelta = new Vector2(-14, 34);
        detailText.resizeTextForBestFit = true;
        detailText.resizeTextMinSize = 10;
        detailText.resizeTextMaxSize = 13;
        var hit = MakeButton("", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1, 1, 1, 0.01f), Color.clear, 1, action); hit.transform.SetAsLastSibling();
    }

    private void RowCard(string icon, string title, string detail, Color bg, Color tint, Action action)
    {
        var card = NewCard(86, bg);
        var bubble = Rect("Icono", card, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero); bubble.pivot = new Vector2(0, 0.5f); bubble.anchoredPosition = new Vector2(16, 0); bubble.sizeDelta = new Vector2(48, 48); Image bi = bubble.gameObject.AddComponent<Image>(); bi.color = Color.white; Circle(bi);
        Text iconText = Label(icon, bubble, 21, tint, FontStyle.Bold, TextAnchor.MiddleCenter); iconText.rectTransform.offsetMin = Vector2.zero; iconText.rectTransform.offsetMax = Vector2.zero;
        Text t = Label(title, card, 16, Ink, FontStyle.Bold, TextAnchor.MiddleLeft);
        t.rectTransform.anchorMin = new Vector2(0, 0.5f); t.rectTransform.anchorMax = new Vector2(1, 0.5f); t.rectTransform.pivot = new Vector2(0, 0.5f); t.rectTransform.offsetMin = new Vector2(77, 13); t.rectTransform.offsetMax = new Vector2(-16, 13); t.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 24);
        Text d = Label(detail, card, 13, Muted, FontStyle.Normal, TextAnchor.MiddleLeft);
        d.rectTransform.anchorMin = new Vector2(0, 0.5f); d.rectTransform.anchorMax = new Vector2(1, 0.5f); d.rectTransform.pivot = new Vector2(0, 0.5f); d.rectTransform.offsetMin = new Vector2(77, -13); d.rectTransform.offsetMax = new Vector2(-16, -13); d.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 23);
        var hit = MakeButton("", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1, 1, 1, 0.01f), Color.clear, 1, action); hit.transform.SetAsLastSibling();
    }

    private void Card(string title, string detail, Color bg, float height)
    {
        var card = NewCard(Mathf.Max(height, 138f), bg);
        Text titleText = Label(title, card, 16, Ink, FontStyle.Bold, TextAnchor.MiddleLeft);
        titleText.rectTransform.anchorMin = new Vector2(0, 1); titleText.rectTransform.anchorMax = new Vector2(1, 1); titleText.rectTransform.pivot = new Vector2(0, 1); titleText.rectTransform.offsetMin = new Vector2(16, -39); titleText.rectTransform.offsetMax = new Vector2(-16, -12); 
        Text d = Label(detail, card, 14, Muted, FontStyle.Normal, TextAnchor.UpperLeft);
        d.rectTransform.anchorMin = new Vector2(0, 0); d.rectTransform.anchorMax = new Vector2(1, 1); d.rectTransform.pivot = new Vector2(0, 1); d.rectTransform.offsetMin = new Vector2(16, 12); d.rectTransform.offsetMax = new Vector2(-16, -43); 
        titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        d.horizontalOverflow = HorizontalWrapMode.Wrap;
        d.verticalOverflow = VerticalWrapMode.Truncate;
    }

    private void PrimaryButton(string text, string unusedIcon, Action action)
    {
        var holder = NewCard(66, Color.clear); holder.GetComponent<Image>().enabled = false;
        MakeButton(text, holder, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Blue, Color.white, 16, action);
    }

    private RectTransform NewCard(float height, Color color)
    {
        var go = new GameObject("Tarjeta", typeof(RectTransform), typeof(Image), typeof(LayoutElement)); go.transform.SetParent(content, false); pageObjects.Add(go);
        var le = go.GetComponent<LayoutElement>(); le.preferredHeight = height; le.minHeight = height; le.flexibleWidth = 1; le.minWidth = 0;
        RectTransform r = go.GetComponent<RectTransform>(); r.sizeDelta = new Vector2(0, height); Image image = go.GetComponent<Image>(); image.color = color; Round(image, 16);
        var layout = go.GetComponent<LayoutElement>(); layout.minWidth = 0; layout.flexibleWidth = 1;
        return r;
    }

    private Text AddLabel(string text, int size, Color color, FontStyle style, float height)
    {
        var go = new GameObject("Texto", typeof(RectTransform), typeof(LayoutElement), typeof(Text)); go.transform.SetParent(content, false); pageObjects.Add(go);
        LayoutElement layout = go.GetComponent<LayoutElement>(); layout.preferredHeight = height; layout.minHeight = height; layout.flexibleWidth = 1; layout.minWidth = 0;
        Text t = go.GetComponent<Text>(); ConfigureText(t, text, size, color, style, TextAnchor.MiddleCenter); return t;
    }

    private Text Label(string text, Transform parent, int size, Color color, FontStyle style, TextAnchor anchor = TextAnchor.MiddleLeft, Vector2 position = default(Vector2), Vector2 sizeDelta = default(Vector2))
    {
        var go = new GameObject(string.IsNullOrEmpty(text) ? "Texto" : text, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>(); ConfigureText(t, text, size, color, style, anchor);
        t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one; t.rectTransform.offsetMin = Vector2.zero; t.rectTransform.offsetMax = Vector2.zero;
        if (sizeDelta != Vector2.zero) { t.rectTransform.anchorMin = new Vector2(0, 1); t.rectTransform.anchorMax = new Vector2(1, 1); t.rectTransform.pivot = new Vector2(0.5f, 1); t.rectTransform.anchoredPosition = position; t.rectTransform.sizeDelta = sizeDelta; }
        return t;
    }

    private void ConfigureText(Text t, string value, int size, Color color, FontStyle style, TextAnchor anchor)
    {
        t.text = value; t.font = font; t.fontSize = size; t.color = color; t.fontStyle = style; t.alignment = anchor; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate; t.lineSpacing = 1.1f; t.raycastTarget = false;
    }

    private Button MakeButton(string caption, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color bg, Color fg, int fontSize, Action action)
    {
        var go = new GameObject(string.IsNullOrEmpty(caption) ? "Acción" : caption, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
        RectTransform r = go.GetComponent<RectTransform>(); r.anchorMin = anchorMin; r.anchorMax = anchorMax; r.pivot = new Vector2((anchorMin.x + anchorMax.x) * 0.5f, (anchorMin.y + anchorMax.y) * 0.5f); r.anchoredPosition = position; r.sizeDelta = size;
        Image image = go.GetComponent<Image>(); image.color = bg; Round(image, 14);
        Button button = go.GetComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => action());
        var colors = button.colors; colors.highlightedColor = Color.Lerp(bg, Color.white, 0.13f); colors.pressedColor = Color.Lerp(bg, Navy, 0.12f); colors.selectedColor = colors.highlightedColor; button.colors = colors;
        if (!string.IsNullOrEmpty(caption))
        {
            Text label = Label(caption, go.transform, fontSize, fg, FontStyle.Bold, TextAnchor.MiddleCenter); label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one; label.rectTransform.offsetMin = new Vector2(8, 5); label.rectTransform.offsetMax = new Vector2(-8, -5); label.raycastTarget = false;
        }
        return button;
    }

    private RectTransform Rect(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); RectTransform r = go.GetComponent<RectTransform>(); r.anchorMin = anchorMin; r.anchorMax = anchorMax; r.offsetMin = offsetMin; r.offsetMax = offsetMax; return r;
    }

    private void Toast(string message)
    {
        var go = new GameObject("Mensaje", typeof(RectTransform), typeof(Image)); go.transform.SetParent(canvas.transform, false);
        RectTransform r = go.GetComponent<RectTransform>(); r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0); r.pivot = new Vector2(0.5f, 0); r.offsetMin = new Vector2(SideInset, 108); r.offsetMax = new Vector2(-SideInset, 166); r.sizeDelta = Vector2.zero;
        Image image = go.GetComponent<Image>(); image.color = Navy; Round(image, 15);
        Text label = Label(message, go.transform, 14, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter); label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one; label.rectTransform.offsetMin = new Vector2(12, 6); label.rectTransform.offsetMax = new Vector2(-12, -6);
        Destroy(go, 2.4f);
    }

    private static void Round(Graphic graphic, float radius)
    {
        var image = graphic as Image;
        if (image == null) return;
        if (roundedSprite == null)
        {
            const int size = 64;
            const float corner = 16f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "AMI Rounded Card";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - size / 2f) - (size / 2f - corner), 0);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - size / 2f) - (size / 2f - corner), 0);
                    pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(corner - Mathf.Sqrt(dx * dx + dy * dy)));
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            roundedSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(corner, corner, corner, corner));
        }
        image.sprite = roundedSprite;
        image.type = Image.Type.Sliced;
    }

    private static void Circle(Image image)
    {
        if (image == null || circleSprite == null) return;
        image.sprite = circleSprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
    }

    private static Sprite CreateCircleSprite()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "AMI Circle UI Sprite";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[size * size];
        float center = size * 0.5f;
        float radius = center - 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - center;
                float dy = y + 0.5f - center;
                byte alpha = dx * dx + dy * dy <= radius * radius ? (byte)255 : (byte)0;
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
    }

    private static Color Hex(string html)
    {
        Color c; ColorUtility.TryParseHtmlString("#" + html, out c); return c;
    }
}

[DisallowMultipleComponent]
public sealed class AMIResponsiveFrame : MonoBehaviour
{
    private RectTransform rect;
    private float maxWidth = 430f;
    private float lastWidth = -1f;
    private float lastHeight = -1f;
    private Rect lastSafeArea;

    public void Configure(float widthLimit)
    {
        maxWidth = Mathf.Max(280f, widthLimit);
        rect = GetComponent<RectTransform>();
        ApplySafeAreaAndWidth();
    }

    private void LateUpdate()
    {
        if (rect == null) rect = GetComponent<RectTransform>();
        if (Screen.width == lastWidth && Screen.height == lastHeight && Screen.safeArea == lastSafeArea) return;
        ApplySafeAreaAndWidth();
    }

    private void ApplySafeAreaAndWidth()
    {
        if (rect == null || rect.parent == null || Screen.width <= 0 || Screen.height <= 0) return;
        Rect safe = Screen.safeArea;
        RectTransform parent = rect.parent as RectTransform;
        if (parent == null) return;

        float xMin = Mathf.Clamp01(safe.xMin / Screen.width);
        float xMax = Mathf.Clamp01(safe.xMax / Screen.width);
        float yMin = Mathf.Clamp01(safe.yMin / Screen.height);
        float yMax = Mathf.Clamp01(safe.yMax / Screen.height);
        float centerX = (xMin + xMax) * 0.5f;
        rect.anchorMin = new Vector2(centerX, yMin);
        rect.anchorMax = new Vector2(centerX, yMax);
        rect.pivot = new Vector2(0.5f, 0.5f);

        float availableWidth = parent.rect.width * Mathf.Max(0f, xMax - xMin);
        float width = Mathf.Min(maxWidth, availableWidth);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        rect.anchoredPosition = Vector2.zero;
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        lastSafeArea = safe;
    }
}
