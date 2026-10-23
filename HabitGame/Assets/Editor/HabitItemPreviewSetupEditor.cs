using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class HabitItemPreviewSetupEditor
{
    private const string ScenePath = "Assets/Scenes/UITestScene.unity";
    private const string RenewalPrefabPath = "Assets/Resources/Prefabs/HabitItem_UIRenewal.prefab";
    private const string KoreanFontPath = "Assets/Login/Fonts/NotoSansKR Login SDF.asset";
    private const string MenuPath = "Tools/HabitPVP/UI Renewal/Setup HabitItem Preview Test";
    private const string HabitItemModeMenu =
        "Tools/HabitPVP/UI Renewal/Preview Mode/A. HabitItem Independent";
    private const string HabitMainModeMenu =
        "Tools/HabitPVP/UI Renewal/Preview Mode/B. Habit Main Renewal";
    private const string RootName = "HabitItemPreviewTestRoot";

    private static readonly Color32 Background = new Color32(0xF7, 0xEF, 0xDD, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 ButtonColor = new Color32(0xE9, 0xD2, 0xAC, 0xFF);

    [MenuItem(MenuPath, false, 2023)]
    private static void SetupPreviewTest()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            Debug.LogError($"[HabitPVP Preview] UITestScene에서만 실행할 수 있습니다.\nActive: {scene.path}");
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[HabitPVP Preview] Play Mode를 종료한 뒤 실행해주세요.");
            return;
        }

        GameObject renewalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RenewalPrefabPath);
        if (renewalPrefab == null)
        {
            Debug.LogError($"[HabitPVP Preview] Renewal Prefab을 찾을 수 없습니다: {RenewalPrefabPath}");
            return;
        }

        TMP_FontAsset koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        if (koreanFont == null)
        {
            Debug.LogError($"[HabitPVP Preview] 한글 TMP Font Asset을 찾을 수 없습니다: {KoreanFontPath}");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup HabitItem Preview Test");

        try
        {
            RemoveExistingRoot(scene);
            List<string> disabledComponents = DisableGameHabitComponents(scene);
            GameObject root = CreateRoot(scene);
            CreateHeading(root.transform, koreanFont);
            HabitItemPreview[] previews = CreatePreviewItems(root.transform, renewalPrefab, koreanFont);
            CreateControls(root.transform, previews, koreanFont);
            EnsureEventSystem(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = root;

            Debug.Log("[HabitPVP Preview] 독립 Preview 테스트 환경을 구성했습니다.\n" +
                      "- Renewal Prefab 외형을 복제하고 HabitItem 컴포넌트 및 기존 UI 이벤트를 제거했습니다.\n" +
                      "- Preview Mode/A는 독립 카드, Preview Mode/B는 습관 메인 화면을 표시합니다.\n" +
                      "- Scene은 저장하지 않았습니다. Hierarchy와 Inspector 확인 후 직접 저장해주세요.\n" +
                      "- UITestScene에서만 비활성화: " +
                      (disabledComponents.Count == 0 ? "없음" : string.Join(", ", disabledComponents)));
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogError("[HabitPVP Preview] 구성 실패. Undo로 변경을 되돌렸습니다.\n" + exception);
        }
    }

    [MenuItem(MenuPath, true)]
    private static bool CanSetupPreviewTest()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
               SceneManager.GetActiveScene().path == ScenePath;
    }

    [MenuItem(HabitItemModeMenu, false, 2030)]
    private static void ActivateHabitItemMode()
    {
        SpendingReportUiRenewalEditor.RestoreSpendingPreviewState();
        PvpRankingUiRenewalEditor.RestorePvpPreviewState();
        ShopGachaUiRenewalEditor.RestoreShopPreviewState();
        InventoryMainUiRenewalEditor.RestoreInventoryPreviewState();
        CharacterMainUiRenewalEditor.RestoreCharacterPreviewState();
        if (!TryGetPreviewRoot(out Scene scene, out GameObject root))
        {
            return;
        }

        PhotoVerificationPopupUiRenewalEditor.RestorePhotoPreviewState();
        AchievedAmountOverlayUiRenewalEditor.RestoreNumberPreviewState();
        HabitGoalFormUiRenewalEditor.RestoreHabitMainPreviewState();
        Undo.RecordObject(root, "Activate HabitItem preview mode");
        root.SetActive(true);
        SetAllEnabledInChildren<HabitItemPreviewController>(root, true);
        SetAllEnabledInChildren<HabitItemPreview>(root, true);
        List<string> disabledComponents = DisableGameHabitComponents(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        Debug.Log(
            "[HabitPVP Preview] A 모드: HabitItem 독립 Preview를 표시합니다. " +
            "습관 메인 런타임 컴포넌트는 UITestScene에서만 비활성화했습니다.\n- " +
            (disabledComponents.Count == 0 ? "추가 비활성화 없음" :
                string.Join("\n- ", disabledComponents)));
    }

    [MenuItem(HabitItemModeMenu, true)]
    private static bool CanActivateHabitItemMode() => CanSwitchPreviewMode();

    [MenuItem(HabitMainModeMenu, false, 2031)]
    private static void ActivateHabitMainMode()
    {
        SpendingReportUiRenewalEditor.RestoreSpendingPreviewState();
        PvpRankingUiRenewalEditor.RestorePvpPreviewState();
        ShopGachaUiRenewalEditor.RestoreShopPreviewState();
        InventoryMainUiRenewalEditor.RestoreInventoryPreviewState();
        CharacterMainUiRenewalEditor.RestoreCharacterPreviewState();
        if (!TryGetPreviewRoot(out Scene scene, out GameObject root))
        {
            return;
        }

        PhotoVerificationPopupUiRenewalEditor.RestorePhotoPreviewState();
        AchievedAmountOverlayUiRenewalEditor.RestoreNumberPreviewState();
        HabitGoalFormUiRenewalEditor.RestoreHabitMainPreviewState();
        HabitGoalFormUiRenewalEditor.EnsureHabitCanvasHierarchyActive(scene);
        Undo.RecordObject(root, "Activate Habit main preview mode");
        root.SetActive(false);
        List<string> enabledComponents = EnableGameHabitComponents(scene);
        EditorSceneManager.MarkSceneDirty(scene);

        GameObject canvas = scene.GetRootGameObjects().FirstOrDefault(item => item.name == "Canvas");
        Selection.activeGameObject = canvas;
        Debug.Log(
            "[HabitPVP Preview] B 모드: HabitItem 독립 Preview를 숨기고 습관 메인 화면을 표시합니다. " +
            "UI Renewal의 Validate/Apply 메뉴를 실행해 확인하세요.\n- " +
            (enabledComponents.Count == 0 ? "추가 활성화 없음" :
                string.Join("\n- ", enabledComponents)));
    }

    [MenuItem(HabitMainModeMenu, true)]
    private static bool CanActivateHabitMainMode() => CanSwitchPreviewMode();

    private static bool CanSwitchPreviewMode()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
               SceneManager.GetActiveScene().path == ScenePath &&
               FindRoot(SceneManager.GetActiveScene()) != null;
    }

    private static bool TryGetPreviewRoot(out Scene scene, out GameObject root)
    {
        scene = SceneManager.GetActiveScene();
        root = null;
        if (scene.path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[HabitPVP Preview] Edit Mode의 UITestScene에서만 전환할 수 있습니다.");
            return false;
        }

        root = FindRoot(scene);
        if (root == null)
        {
            Debug.LogError(
                "[HabitPVP Preview] HabitItemPreviewTestRoot가 없습니다. " +
                "Setup HabitItem Preview Test를 먼저 실행해주세요.");
            return false;
        }
        return true;
    }

    private static GameObject FindRoot(Scene scene)
    {
        return scene.GetRootGameObjects().FirstOrDefault(candidate => candidate.name == RootName);
    }

    private static void RemoveExistingRoot(Scene scene)
    {
        foreach (GameObject candidate in scene.GetRootGameObjects())
        {
            if (candidate.name == RootName)
            {
                Undo.DestroyObjectImmediate(candidate);
                return;
            }
        }
    }

    private static List<string> DisableGameHabitComponents(Scene scene)
    {
        List<string> disabled = new List<string>();
        DisableAll<HabitListManager>(scene, disabled);
        DisableAll<HabitUIManager>(scene, disabled);
        DisableAll<HabitItem>(scene, disabled);
        return disabled;
    }

    private static List<string> EnableGameHabitComponents(Scene scene)
    {
        List<string> enabled = new List<string>();
        EnableAll<HabitListManager>(scene, enabled);
        EnableAll<HabitUIManager>(scene, enabled);
        EnableAll<HabitItem>(scene, enabled);
        return enabled;
    }

    private static void DisableAll<T>(Scene scene, ICollection<string> disabled) where T : Behaviour
    {
        foreach (T component in UnityEngine.Object.FindObjectsOfType<T>(true))
        {
            if (component.gameObject.scene != scene || !component.enabled)
            {
                continue;
            }

            Undo.RecordObject(component, "Disable game Habit component in UITestScene");
            component.enabled = false;
            EditorUtility.SetDirty(component);
            disabled.Add(component.GetType().Name + " (" + GetHierarchyPath(component.transform) + ")");
        }
    }

    private static void EnableAll<T>(Scene scene, ICollection<string> enabled) where T : Behaviour
    {
        foreach (T component in UnityEngine.Object.FindObjectsOfType<T>(true))
        {
            if (component.gameObject.scene != scene || component.enabled)
            {
                continue;
            }

            Undo.RecordObject(component, "Enable game Habit component in UITestScene");
            component.enabled = true;
            EditorUtility.SetDirty(component);
            enabled.Add(component.GetType().Name + " (" + GetHierarchyPath(component.transform) + ")");
        }
    }

    private static void SetAllEnabledInChildren<T>(GameObject root, bool enabled) where T : Behaviour
    {
        foreach (T component in root.GetComponentsInChildren<T>(true))
        {
            if (component.enabled == enabled)
            {
                continue;
            }

            Undo.RecordObject(component, "Enable HabitItem preview component");
            component.enabled = enabled;
            EditorUtility.SetDirty(component);
        }
    }

    private static GameObject CreateRoot(Scene scene)
    {
        GameObject root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
        Undo.RegisterCreatedObjectUndo(root, "Create HabitItem Preview root");
        SceneManager.MoveGameObjectToScene(root, scene);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 1f;

        root.GetComponent<Image>().color = Background;

        return root;
    }

    private static void CreateHeading(Transform root, TMP_FontAsset font)
    {
        GameObject heading = CreateText(root, "PreviewHeading", "HabitItem Renewal · LOCAL UI PREVIEW", 38f, Brown, font);
        SetAnchoredRect(heading.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(40f, -118f), new Vector2(-40f, -50f));
    }

    private static HabitItemPreview[] CreatePreviewItems(Transform root, GameObject prefab, TMP_FontAsset font)
    {
        GameObject scrollObject = new GameObject("PreviewScroll", typeof(RectTransform), typeof(ScrollRect));
        Undo.RegisterCreatedObjectUndo(scrollObject, "Create Preview scroll");
        scrollObject.transform.SetParent(root, false);
        SetAnchoredRect(scrollObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
            new Vector2(40f, 292f), new Vector2(-40f, -130f));

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        Undo.RegisterCreatedObjectUndo(viewport, "Create Preview viewport");
        viewport.transform.SetParent(scrollObject.transform, false);
        SetAnchoredRect(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewport.GetComponent<Image>().color = new Color32(0xFF, 0xFF, 0xFF, 0x01);

        GameObject items = new GameObject("PreviewItems", typeof(RectTransform), typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        Undo.RegisterCreatedObjectUndo(items, "Create Preview items");
        items.transform.SetParent(viewport.transform, false);
        RectTransform itemsRect = items.GetComponent<RectTransform>();
        itemsRect.anchorMin = new Vector2(0f, 1f);
        itemsRect.anchorMax = Vector2.one;
        itemsRect.pivot = new Vector2(0.5f, 1f);
        itemsRect.offsetMin = Vector2.zero;
        itemsRect.offsetMax = Vector2.zero;
        VerticalLayoutGroup layout = items.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        items.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = itemsRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 45f;

        PreviewDefinition[] definitions =
        {
            new PreviewDefinition(HabitItemPreview.PreviewKind.Check, "물 마시기", 0, 1, ""),
            new PreviewDefinition(HabitItemPreview.PreviewKind.Number, "하루 2L 물 마시기", 500, 2000, "mL"),
            new PreviewDefinition(HabitItemPreview.PreviewKind.Photo, "운동 인증하기", 0, 1, ""),
            new PreviewDefinition(HabitItemPreview.PreviewKind.LongTitle,
                "매일 아침 기상 후 스트레칭 및 가벼운 유산소 운동하기", 0, 1, ""),
            new PreviewDefinition(HabitItemPreview.PreviewKind.Weekly, "일주일에 3번 운동하기", 1, 3, "")
        };

        HabitItemPreview[] previews = new HabitItemPreview[definitions.Length];
        for (int index = 0; index < definitions.Length; index++)
        {
            previews[index] = CreatePreviewItem(items.transform, prefab, definitions[index], font);
        }

        return previews;
    }

    private static HabitItemPreview CreatePreviewItem(
        Transform parent,
        GameObject prefab,
        PreviewDefinition definition,
        TMP_FontAsset font)
    {
        GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(item, "Create HabitItem Preview");
        PrefabUtility.UnpackPrefabInstance(item, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        item.name = "Preview_" + definition.Kind;

        HabitItem gameHabitItem = item.GetComponent<HabitItem>();
        if (gameHabitItem == null)
        {
            throw new InvalidOperationException("Renewal Prefab 루트에서 HabitItem 컴포넌트를 찾지 못했습니다.");
        }
        Undo.DestroyObjectImmediate(gameHabitItem);

        Toggle toggle = item.GetComponentInChildren<Toggle>(true);
        TextMeshProUGUI label = FindNamedComponent<TextMeshProUGUI>(item.transform, "Label");
        Slider slider = FindNamedComponent<Slider>(item.transform, "ProgressBar");
        TextMeshProUGUI progress = FindNamedComponent<TextMeshProUGUI>(item.transform, "ProgressText");
        TextMeshProUGUI streak = FindNamedComponent<TextMeshProUGUI>(item.transform, "StreakText");
        GameObject completionBadge = FindNamedObject(item.transform, "CompletionBadge");
        Button labelButton = FindNamedComponent<Button>(item.transform, "Label");

        if (toggle == null || label == null || slider == null || progress == null || streak == null ||
            completionBadge == null)
        {
            throw new InvalidOperationException(
                "Renewal Prefab의 Toggle/Label/ProgressBar/ProgressText/StreakText/CompletionBadge 구조가 예상과 다릅니다.");
        }

        foreach (TextMeshProUGUI text in item.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            text.font = font;
            EditorUtility.SetDirty(text);
        }

        toggle.onValueChanged = new Toggle.ToggleEvent();
        slider.onValueChanged = new Slider.SliderEvent();
        if (labelButton != null)
        {
            labelButton.onClick = new Button.ButtonClickedEvent();
            labelButton.interactable = false;
        }

        HabitItemPreview preview = Undo.AddComponent<HabitItemPreview>(item);
        preview.Configure(definition.Kind, definition.Title, definition.Initial, definition.Target,
            definition.Unit, toggle, label, slider, progress, streak, completionBadge);
        label.fontSizeMin = 28f;
        label.fontSizeMax = 40f;
        label.maxVisibleLines = 2;
        label.overflowMode = TextOverflowModes.Ellipsis;
        EditorUtility.SetDirty(preview);
        AddLayoutElement(item, 196f);
        return preview;
    }

    private static void CreateControls(Transform root, HabitItemPreview[] previews, TMP_FontAsset font)
    {
        GameObject panel = new GameObject("PreviewControls", typeof(RectTransform), typeof(Image),
            typeof(GridLayoutGroup), typeof(LayoutElement));
        Undo.RegisterCreatedObjectUndo(panel, "Create Preview controls");
        panel.transform.SetParent(root, false);
        SetAnchoredRect(panel.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(40f, 40f), new Vector2(-40f, 276f));
        panel.GetComponent<Image>().color = new Color32(0xEF, 0xE1, 0xC8, 0xFF);
        GridLayoutGroup grid = panel.GetComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(20, 20, 20, 20);
        grid.spacing = new Vector2(12f, 12f);
        grid.cellSize = new Vector2(312f, 92f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        Button check = CreateButton(panel.transform, "CHECK 완료 / 초기화", font);
        Button numberAdd = CreateButton(panel.transform, "NUMBER +500", font);
        Button numberReset = CreateButton(panel.transform, "NUMBER 초기화", font);
        Button photo = CreateButton(panel.transform, "PHOTO 성공 / 초기화", font);
        Button weekly = CreateButton(panel.transform, "WEEKLY Streak +1", font);
        Button resetAll = CreateButton(panel.transform, "전체 초기화", font);

        HabitItemPreviewController controller = Undo.AddComponent<HabitItemPreviewController>(panel);
        controller.Configure(previews, check, numberAdd, numberReset, photo, weekly, resetAll);
        EditorUtility.SetDirty(controller);
    }

    private static Button CreateButton(Transform parent, string text, TMP_FontAsset font)
    {
        GameObject buttonObject = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(buttonObject, "Create Preview button");
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<Image>().color = ButtonColor;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.onClick = new Button.ButtonClickedEvent();

        GameObject label = CreateText(buttonObject.transform, "Label", text, 25f, Brown, font);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(8f, 4f);
        rect.offsetMax = new Vector2(-8f, -4f);
        return button;
    }

    private static GameObject CreateText(
        Transform parent,
        string name,
        string text,
        float size,
        Color color,
        TMP_FontAsset font)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(textObject, "Create Preview text");
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.font = font;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return textObject;
    }

    private static void SetAnchoredRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void AddLayoutElement(GameObject target, float height)
    {
        LayoutElement element = target.GetComponent<LayoutElement>();
        if (element == null)
        {
            element = Undo.AddComponent<LayoutElement>(target);
        }
        element.preferredHeight = height;
        element.minHeight = height;
    }

    private static void EnsureEventSystem(Scene scene)
    {
        foreach (EventSystem eventSystem in UnityEngine.Object.FindObjectsOfType<EventSystem>(true))
        {
            if (eventSystem.gameObject.scene == scene)
            {
                return;
            }
        }

        GameObject eventSystemObject = new GameObject("PreviewEventSystem", typeof(EventSystem),
            typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create Preview EventSystem");
        SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
    }

    private static T FindNamedComponent<T>(Transform root, string objectName) where T : Component
    {
        foreach (T component in root.GetComponentsInChildren<T>(true))
        {
            if (component.gameObject.name == objectName)
            {
                return component;
            }
        }
        return null;
    }

    private static GameObject FindNamedObject(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
            {
                return child.gameObject;
            }
        }
        return null;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }

    private readonly struct PreviewDefinition
    {
        public readonly HabitItemPreview.PreviewKind Kind;
        public readonly string Title;
        public readonly int Initial;
        public readonly int Target;
        public readonly string Unit;

        public PreviewDefinition(HabitItemPreview.PreviewKind kind, string title, int initial, int target, string unit)
        {
            Kind = kind;
            Title = title;
            Initial = initial;
            Target = target;
            Unit = unit;
        }
    }
}
