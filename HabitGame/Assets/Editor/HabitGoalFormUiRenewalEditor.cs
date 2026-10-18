using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class HabitGoalFormUiRenewalEditor
{
    private const string ScenePath = "Assets/Scenes/UITestScene.unity";
    private const string CanvasPrefabPath = "Assets/Resources/UIPrefabs/Canvas.prefab";
    private const string KoreanFontPath = "Assets/Login/Fonts/NotoSansKR Login SDF.asset";
    private const string CardSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/panel_rpg_card.png";
    private const string CreamButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_cream.png";
    private const string GoldButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_rpg.png";
    private const string FormPath = "HabitTap/LifeAdd_1";
    private const string ValidateMenu = "Tools/HabitPVP/UI Renewal/Validate Habit Goal Form";
    private const string ApplyMenu = "Tools/HabitPVP/UI Renewal/Apply Habit Goal Form";
    private const string PreviewMenu =
        "Tools/HabitPVP/UI Renewal/Preview Mode/C. Habit Add Form Renewal";
    private const string HabitItemPreviewRootName = "HabitItemPreviewTestRoot";
    private const float ScrollHorizontalInset = 18f;
    private const float ScrollEdgeGap = 18f;
    private const float ScrollSensitivity = 55f;
    private const float HeaderBackPadding = 48f;

    private static RectTransform previewContent;
    private static GameObject previewDurationPanel;
    private static ScrollRect previewScrollRect;
    private static bool previewDurationWasActive;
    private static bool previewLayoutInitialized;

    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x86, 0x68, 0x5D, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);
    private static readonly Color32 SoftGold = new Color32(0xF4, 0xD5, 0x8B, 0xFF);

    private static readonly string[] RequiredSectionPaths =
    {
        "HabitScroll View/Viewport/Content/Name",
        "HabitScroll View/Viewport/Content/Category",
        "HabitScroll View/Viewport/Content/RecordTypePanel",
        "HabitScroll View/Viewport/Content/Repeat",
        "HabitScroll View/Viewport/Content/DurationPanel",
        "HabitScroll View/Viewport/Content/Amount",
        "HabitScroll View/Viewport/Content/Reward",
        "HabitScroll View/Viewport/Content/Save"
    };

    static HabitGoalFormUiRenewalEditor()
    {
        EditorApplication.update -= RefreshPreviewLayoutWhenDurationChanges;
        EditorApplication.update += RefreshPreviewLayoutWhenDurationChanges;
        EditorApplication.playModeStateChanged -= ResetPreviewLayoutWatcher;
        EditorApplication.playModeStateChanged += ResetPreviewLayoutWatcher;
    }

    [MenuItem(ValidateMenu, false, 2040)]
    private static void Validate()
    {
        ValidationResult result = ValidateScene(true);
        if (result.IsValid)
        {
            List<string> layoutErrors = ValidateLayoutBounds(result);
            if (layoutErrors.Count > 0)
            {
                foreach (string error in layoutErrors)
                {
                    Debug.LogError("[HabitPVP Habit Form] " + error, result.FormRoot);
                }

                return;
            }

            Debug.Log(
                "[HabitPVP Habit Form] 검증 성공. 기존 생성 폼의 필드, 참조, 이벤트와 " +
                "Dropdown 매핑이 유지되고 섹션 제목/콘텐츠 경계도 정상입니다.\n" +
                BuildScrollDiagnostics(result),
                result.FormRoot);
        }
    }

    [MenuItem(ApplyMenu, false, 2041)]
    private static void Apply()
    {
        ValidationResult result = ValidateScene(true);
        if (!result.IsValid)
        {
            Debug.LogError("[HabitPVP Habit Form] Apply 중단: 검증 오류를 먼저 해결해주세요.");
            return;
        }

        ProtectionSnapshot protectedState = ProtectionSnapshot.Capture(result);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Habit Goal Form UI Renewal");

        try
        {
            ApplyStyle(result);
            protectedState.AssertUnchanged(result);
            List<string> layoutErrors = ValidateLayoutBounds(result);
            if (layoutErrors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Habit Add Form 레이아웃 경계 검증에 실패했습니다.\n" +
                    string.Join("\n", layoutErrors));
            }

            EditorSceneManager.MarkSceneDirty(result.Scene);
            Undo.CollapseUndoOperations(undoGroup);

            if (!EditorSceneManager.SaveScene(result.Scene))
            {
                throw new InvalidOperationException("UITestScene 저장에 실패했습니다.");
            }

            Selection.activeGameObject = result.FormRoot;
            Debug.Log(
                "[HabitPVP Habit Form] 적용 성공. LifeAdd_1의 Scene Prefab Instance Override만 저장했습니다.\n" +
                "- Canvas.prefab, MainScene, HabitGoalUI 및 API 코드는 변경하지 않았습니다.\n" +
                "- C Preview는 Edit Mode 시각 검증용이며 테스트 데이터를 생성하거나 전송하지 않습니다.",
                result.FormRoot);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            EditorSceneManager.MarkSceneDirty(result.Scene);
            Debug.LogException(exception);
            Debug.LogError("[HabitPVP Habit Form] 적용 실패. 이번 실행의 변경을 Undo로 되돌렸습니다.");
        }
    }

    [MenuItem(ApplyMenu, true)]
    private static bool CanApply()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
               SceneManager.GetActiveScene().path == ScenePath;
    }

    [MenuItem(PreviewMenu, false, 2042)]
    private static void ActivateHabitGoalPreview()
    {
        PvpRankingUiRenewalEditor.RestorePvpPreviewState();
        ShopGachaUiRenewalEditor.RestoreShopPreviewState();
        InventoryMainUiRenewalEditor.RestoreInventoryPreviewState();
        CharacterMainUiRenewalEditor.RestoreCharacterPreviewState();
        PhotoVerificationPopupUiRenewalEditor.RestorePhotoPreviewState();
        AchievedAmountOverlayUiRenewalEditor.RestoreNumberPreviewState();
        ValidationResult result = ValidateScene(true);
        if (!result.IsValid)
        {
            return;
        }

        GameObject independentPreview = result.Scene.GetRootGameObjects()
            .FirstOrDefault(candidate => candidate.name == HabitItemPreviewRootName);
        SetActiveWithUndo(independentPreview, false, "Hide HabitItem preview for Habit form preview");
        EnsureHabitCanvasHierarchyActive(result.Scene);
        SetHabitPanelMode(result.Scene, true);
        SetAllEnabled<HabitUIManager>(result.Scene, false);
        SetAllEnabled<HabitListManager>(result.Scene, false);
        SetAllEnabled<HabitItem>(result.Scene, false);
        RebuildScrollLayout(result.FormRoot.transform);

        EditorSceneManager.MarkSceneDirty(result.Scene);
        Selection.activeGameObject = result.FormRoot;
        Debug.Log(
            "[HabitPVP Habit Form] C 모드: 습관 추가 폼을 표시했습니다. " +
            "Edit Mode 전용 시각 Preview이므로 API 요청이나 테스트 데이터 전송은 실행되지 않습니다. " +
            "A/B 메뉴로 전환하면 기존 Preview 상태가 복원됩니다.",
            result.FormRoot);
    }

    [MenuItem(PreviewMenu, true)]
    private static bool CanActivateHabitGoalPreview() => CanApply();

    public static void RestoreHabitMainPreviewState()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path == ScenePath && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SetHabitPanelMode(scene, false);
        }
    }

    public static void EnsureHabitCanvasHierarchyActive(Scene scene)
    {
        Canvas canvas = UnityEngine.Object.FindObjectsOfType<Canvas>(true)
            .FirstOrDefault(item => item.gameObject.scene == scene && item.transform.parent == null &&
                                    item.gameObject.name == "Canvas");
        if (canvas == null)
        {
            return;
        }

        SetActiveWithUndo(canvas.gameObject, true, "Activate Canvas for Habit preview");
        Transform habitTap = canvas.transform.Find("HabitTap");
        SetActiveWithUndo(habitTap != null ? habitTap.gameObject : null, true,
            "Activate HabitTap for Habit preview");
    }

    private static ValidationResult ValidateScene(bool log)
    {
        ValidationResult result = new ValidationResult { Scene = SceneManager.GetActiveScene() };
        if (!result.Scene.IsValid() || !result.Scene.isLoaded)
        {
            result.Errors.Add("활성 Scene이 유효하지 않거나 로드되지 않았습니다.");
            return FinishValidation(result, log);
        }

        if (result.Scene.path != ScenePath)
        {
            result.Errors.Add($"UITestScene에서만 실행할 수 있습니다. 현재: '{result.Scene.path}'");
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            result.Errors.Add("Play Mode를 종료한 뒤 실행해주세요.");
        }

        Canvas[] canvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true)
            .Where(canvas => canvas.gameObject.scene == result.Scene && canvas.transform.parent == null &&
                             canvas.gameObject.name == "Canvas")
            .ToArray();
        if (canvases.Length != 1)
        {
            result.Errors.Add($"Scene 루트 Canvas가 정확히 하나여야 합니다. 현재: {canvases.Length}");
            return FinishValidation(result, log);
        }

        result.Canvas = canvases[0].gameObject;
        string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(result.Canvas);
        if (sourcePath != CanvasPrefabPath)
        {
            result.Errors.Add($"Canvas 원본 Prefab이 예상과 다릅니다: '{sourcePath}'");
        }

        Transform form = result.Canvas.transform.Find(FormPath);
        if (form == null)
        {
            result.Errors.Add($"대상 경로를 찾을 수 없습니다: Canvas/{FormPath}");
            return FinishValidation(result, log);
        }

        result.FormRoot = form.gameObject;
        if (!PrefabUtility.IsPartOfPrefabInstance(result.FormRoot))
        {
            result.Errors.Add("LifeAdd_1이 Canvas Prefab 인스턴스에 속하지 않습니다.");
        }

        HabitGoalUI[] forms = UnityEngine.Object.FindObjectsOfType<HabitGoalUI>(true)
            .Where(item => item.gameObject.scene == result.Scene)
            .ToArray();
        if (forms.Length != 1 || forms[0].gameObject != result.FormRoot)
        {
            result.Errors.Add($"UITestScene의 HabitGoalUI가 LifeAdd_1에 정확히 하나 있어야 합니다. 현재: {forms.Length}");
            return FinishValidation(result, log);
        }

        result.GoalUi = forms[0];
        ValidateGoalReferences(result);
        ValidateFunctionalBindings(result);
        foreach (string path in RequiredSectionPaths)
        {
            if (result.FormRoot.transform.Find(path) == null)
            {
                result.Errors.Add($"기존 입력 섹션을 찾을 수 없습니다: LifeAdd_1/{path}");
            }
        }

        result.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        result.CardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardSpritePath);
        result.CreamButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CreamButtonSpritePath);
        result.GoldButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GoldButtonSpritePath);
        ValidateAsset(result.Font, KoreanFontPath, result);
        ValidateAsset(result.CardSprite, CardSpritePath, result);
        ValidateAsset(result.CreamButtonSprite, CreamButtonSpritePath, result);
        ValidateAsset(result.GoldButtonSprite, GoldButtonSpritePath, result);

        return FinishValidation(result, log);
    }

    private static void ValidateGoalReferences(ValidationResult result)
    {
        SerializedObject serialized = new SerializedObject(result.GoalUi);
        string[] references =
        {
            "habitNameInput", "amountPanel", "amountText", "unitDropdown",
            "physicalButton", "rhythmButton", "ecoButton", "growthButton",
            "completeButton", "valueButton", "dailyButton", "weeklyButton",
            "longTermButton", "durationPanel", "durationInput"
        };

        foreach (string name in references)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null || property.objectReferenceValue == null)
            {
                result.Errors.Add($"HabitGoalUI.{name} 참조가 비어 있거나 없습니다.");
            }
        }

        if (result.GoalUi.unitDropdown != null && result.GoalUi.unitDropdown.options.Count == 0)
        {
            result.Errors.Add("Unit Dropdown 옵션이 비어 있습니다.");
        }
    }

    private static void ValidateFunctionalBindings(ValidationResult result)
    {
        HabitGoalUI goal = result.GoalUi;
        ValidateButtonBinding(goal.physicalButton, "SelectCategory", result);
        ValidateButtonBinding(goal.rhythmButton, "SelectCategory", result);
        ValidateButtonBinding(goal.ecoButton, "SelectCategory", result);
        ValidateButtonBinding(goal.growthButton, "SelectCategory", result);
        ValidateButtonBinding(goal.completeButton, "CompleteMode", result);
        ValidateButtonBinding(goal.valueButton, "ValueMode", result);
        ValidateButtonBinding(goal.dailyButton, "SelectPeriod", result);
        ValidateButtonBinding(goal.weeklyButton, "SelectPeriod", result);
        ValidateButtonBinding(goal.longTermButton, "SelectPeriod", result);

        Transform root = result.FormRoot.transform;
        ValidateButtonBinding(
            FindComponent<Button>(root, "HabitScroll View/Viewport/Content/Amount/InputGroup/MinusButton"),
            "DecreaseAmount", result);
        ValidateButtonBinding(
            FindComponent<Button>(root, "HabitScroll View/Viewport/Content/Amount/InputGroup/PlusButton"),
            "IncreaseAmount", result);
        ValidateButtonBinding(
            FindComponent<Button>(root, "HabitScroll View/Viewport/Content/Save/SaveButton"),
            "SaveGoal", result);
        ValidateButtonBinding(FindComponent<Button>(root, "Header/BackButton"), "BackToLife", result);

        Transform addButton = result.Canvas.transform.Find("HabitTap/Life/HabitListCard/HabitListHeader/AddButton");
        ValidateButtonBinding(addButton != null ? addButton.GetComponent<Button>() : null,
            "OpenLifeAddPanel", result);
    }

    private static T FindComponent<T>(Transform root, string path) where T : Component
    {
        Transform target = root.Find(path);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static void ValidateButtonBinding(Button button, string expectedMethod, ValidationResult result)
    {
        if (button == null)
        {
            result.Errors.Add($"{expectedMethod} 이벤트 대상 Button을 찾을 수 없습니다.");
            return;
        }

        bool found = false;
        for (int index = 0; index < button.onClick.GetPersistentEventCount(); index++)
        {
            if (button.onClick.GetPersistentMethodName(index) == expectedMethod)
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            result.Errors.Add($"{HierarchyPath(result.Canvas.transform, button.transform)}의 " +
                              $"기존 {expectedMethod} 이벤트를 찾을 수 없습니다.");
        }
    }

    private static void ValidateAsset(UnityEngine.Object asset, string path, ValidationResult result)
    {
        if (asset == null)
        {
            result.Errors.Add($"UI Renewal 에셋을 불러올 수 없습니다: {path}");
        }
    }

    private static ValidationResult FinishValidation(ValidationResult result, bool log)
    {
        if (log)
        {
            foreach (string error in result.Errors)
            {
                Debug.LogError("[HabitPVP Habit Form] " + error);
            }
        }

        return result;
    }

    private static void ApplyStyle(ValidationResult result)
    {
        Transform root = result.FormRoot.transform;
        StyleImage(GetOrAdd<Image>(result.FormRoot), result.CardSprite, Cream, false);

        RectTransform header = Require<RectTransform>(root, "Header");
        Record(header, "Style Habit form header");
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.anchoredPosition = Vector2.zero;
        header.sizeDelta = new Vector2(0f, 160f);
        StyleImage(GetOrAdd<Image>(header.gameObject), result.CardSprite, SoftGold, false);

        RectTransform scrollArea = Require<RectTransform>(root, "HabitScroll View");
        Record(scrollArea, "Layout Habit form scroll area");
        ConfigureScrollArea(result, header, scrollArea);
        StyleImage(GetOrAdd<Image>(scrollArea.gameObject), result.CardSprite, Cream, true);

        RectTransform viewport = Require<RectTransform>(root, "HabitScroll View/Viewport");
        ScrollRect scrolling = Require<ScrollRect>(root, "HabitScroll View");
        Record(scrolling, "Configure Habit form vertical scrolling");
        scrolling.content = Require<RectTransform>(root, "HabitScroll View/Viewport/Content");
        scrolling.viewport = viewport;
        scrolling.horizontal = false;
        scrolling.vertical = true;
        scrolling.movementType = ScrollRect.MovementType.Clamped;
        scrolling.inertia = true;
        scrolling.scrollSensitivity = ScrollSensitivity;
        scrolling.enabled = true;

        RectTransform content = Require<RectTransform>(root, "HabitScroll View/Viewport/Content");
        Record(content, "Anchor Habit form content");
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, content.sizeDelta.y);

        VerticalLayoutGroup vertical = GetOrAdd<VerticalLayoutGroup>(content.gameObject);
        Record(vertical, "Layout Habit form content");
        vertical.padding = new RectOffset(24, 24, 24, 32);
        vertical.spacing = 18f;
        vertical.childAlignment = TextAnchor.UpperCenter;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;

        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(content.gameObject);
        Record(fitter, "Fit Habit form content");
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        StyleSection(root, "HabitScroll View/Viewport/Content/Name", 188f, result.CardSprite);
        StyleSection(root, "HabitScroll View/Viewport/Content/Category", 282f, result.CardSprite);
        StyleSection(root, "HabitScroll View/Viewport/Content/RecordTypePanel", 216f, result.CardSprite);
        StyleSection(root, "HabitScroll View/Viewport/Content/Repeat", 216f, result.CardSprite);
        StyleSection(root, "HabitScroll View/Viewport/Content/DurationPanel", 188f, result.CardSprite);
        StyleSection(root, "HabitScroll View/Viewport/Content/Amount", 232f, result.CardSprite);
        StyleSection(root, "HabitScroll View/Viewport/Content/Reward", 124f, result.CardSprite);
        StyleSection(root, "HabitScroll View/Viewport/Content/Save", 136f, result.CardSprite);

        foreach (TMP_Text text in result.FormRoot.GetComponentsInChildren<TMP_Text>(true))
        {
            Record(text, "Style Habit form text");
            text.font = result.Font;
            text.color = Brown;
            text.enableAutoSizing = true;
            text.fontSizeMin = 26f;
            text.fontSizeMax = 40f;
            text.margin = new Vector4(10f, 4f, 10f, 4f);
        }

        StyleTitle(root, "Header/title", result.Font, 50f, Color.white);
        ConfigureHeader(root);
        foreach (string section in RequiredSectionPaths.Take(6))
        {
            Transform title = root.Find(section + "/Title");
            if (title != null)
            {
                TMP_Text text = title.GetComponent<TMP_Text>();
                if (text != null)
                {
                    StyleHeading(text, result.Font);
                }
            }
        }

        StyleInput(result.GoalUi.habitNameInput, result.CreamButtonSprite, result.Font);
        StyleInput(result.GoalUi.durationInput, result.CreamButtonSprite, result.Font);
        StyleDropdown(result.GoalUi.unitDropdown, result.CreamButtonSprite, result.Font);
        StyleReward(root, result.Font, result.CardSprite);

        Button[] choiceButtons =
        {
            result.GoalUi.physicalButton, result.GoalUi.rhythmButton,
            result.GoalUi.ecoButton, result.GoalUi.growthButton,
            result.GoalUi.completeButton, result.GoalUi.valueButton,
            result.GoalUi.dailyButton, result.GoalUi.weeklyButton, result.GoalUi.longTermButton
        };
        foreach (Button button in choiceButtons)
        {
            StyleButton(button, result.CreamButtonSprite, Color.white, Brown, result.Font, 34f);
        }

        StyleButton(Require<Button>(root, "HabitScroll View/Viewport/Content/Amount/InputGroup/MinusButton"),
            result.CreamButtonSprite, Color.white, Brown, result.Font, 42f);
        StyleButton(Require<Button>(root, "HabitScroll View/Viewport/Content/Amount/InputGroup/PlusButton"),
            result.CreamButtonSprite, Color.white, Brown, result.Font, 42f);
        StyleButton(Require<Button>(root, "Header/BackButton"), result.CreamButtonSprite,
            new Color32(0xEA, 0xD7, 0xB5, 0xFF), MutedBrown, result.Font, 30f);
        StyleButton(Require<Button>(root, "HabitScroll View/Viewport/Content/Save/SaveButton"),
            result.GoldButtonSprite, Color.white, Brown, result.Font, 42f);

        ConfigureSectionTitle(root, "HabitScroll View/Viewport/Content/Category/Title");
        ConfigureSectionTitle(root, "HabitScroll View/Viewport/Content/RecordTypePanel/Title");
        ConfigureSectionTitle(root, "HabitScroll View/Viewport/Content/Repeat/Title");
        ConfigureSectionTitle(root, "HabitScroll View/Viewport/Content/Amount/Title");
        ConfigureNameSection(root);
        ConfigureDurationPanel(root, result.Font);
        ConfigureSaveSection(root);
        ConfigureGrid(root, "HabitScroll View/Viewport/Content/Category/CategoryGrid",
            new Vector2(430f, 84f), new Vector2(18f, 12f));
        ConfigureSegmentRow(root, "HabitScroll View/Viewport/Content/RecordTypePanel/ButtonGroup", 20f);
        ConfigureSegmentRow(root, "HabitScroll View/Viewport/Content/Repeat/ButtonGroup", 16f);
        ConfigureAmountRow(root, "HabitScroll View/Viewport/Content/Amount/InputGroup", 12f);

        TMP_Text amountText = result.GoalUi.amountText;
        if (amountText != null)
        {
            Record(amountText, "Style Habit target amount");
            amountText.font = result.Font;
            amountText.fontStyle = FontStyles.Bold;
            amountText.fontSize = 46f;
            amountText.color = Brown;
            amountText.alignment = TextAlignmentOptions.Center;
        }

        foreach (Component component in result.FormRoot.GetComponentsInChildren<Component>(true))
        {
            if (component == null)
            {
                continue;
            }

            EditorUtility.SetDirty(component);
            if (PrefabUtility.IsPartOfPrefabInstance(component))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
        }

        RebuildScrollLayout(root);
    }

    private static void StyleSection(Transform root, string path, float height, Sprite sprite)
    {
        Transform section = RequireTransform(root, path);
        StyleImage(GetOrAdd<Image>(section.gameObject), sprite, Color.white, false);
        LayoutElement layout = GetOrAdd<LayoutElement>(section.gameObject);
        Record(layout, "Size Habit form section");
        layout.minHeight = height;
        layout.preferredHeight = height;
        layout.flexibleHeight = 0f;
    }

    private static void StyleInput(TMP_InputField input, Sprite sprite, TMP_FontAsset font)
    {
        if (input == null)
        {
            return;
        }

        StyleImage(GetOrAdd<Image>(input.gameObject), sprite, Color.white, true);
        Record(input, "Style Habit form input");
        ColorBlock colors = input.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(0xFF, 0xF7, 0xE8, 0xFF);
        colors.selectedColor = new Color32(0xFF, 0xF2, 0xD2, 0xFF);
        colors.pressedColor = new Color32(0xE8, 0xD1, 0xA9, 0xFF);
        input.colors = colors;
        input.selectionColor = new Color32(0xE7, 0xA8, 0x35, 0x66);
        SetPreferredHeight(input.gameObject, 88f);

        if (input.textComponent != null)
        {
            Record(input.textComponent, "Style Habit input text");
            input.textComponent.font = font;
            input.textComponent.fontStyle = FontStyles.Bold;
            input.textComponent.enableAutoSizing = true;
            input.textComponent.fontSizeMin = 28f;
            input.textComponent.fontSizeMax = 36f;
            input.textComponent.color = Brown;
        }

        if (input.placeholder is TMP_Text placeholder)
        {
            Record(placeholder, "Style Habit input placeholder");
            placeholder.font = font;
            placeholder.enableAutoSizing = true;
            placeholder.fontSizeMin = 27f;
            placeholder.fontSizeMax = 33f;
            placeholder.color = new Color32(0x78, 0x5A, 0x50, 0xE0);
        }
    }

    private static void StyleDropdown(TMP_Dropdown dropdown, Sprite sprite, TMP_FontAsset font)
    {
        if (dropdown == null)
        {
            return;
        }

        StyleImage(GetOrAdd<Image>(dropdown.gameObject), sprite, Color.white, true);
        Record(dropdown, "Style Habit unit dropdown");
        ColorBlock colors = dropdown.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(0xFF, 0xF7, 0xE8, 0xFF);
        colors.selectedColor = new Color32(0xFF, 0xF2, 0xD2, 0xFF);
        colors.pressedColor = new Color32(0xE8, 0xD1, 0xA9, 0xFF);
        dropdown.colors = colors;
        SetPreferredHeight(dropdown.gameObject, 88f);

        if (dropdown.captionText != null)
        {
            Record(dropdown.captionText, "Style Habit unit caption");
            dropdown.captionText.font = font;
            dropdown.captionText.color = Brown;
            dropdown.captionText.enableAutoSizing = true;
            dropdown.captionText.fontStyle = FontStyles.Bold;
            dropdown.captionText.fontSizeMin = 27f;
            dropdown.captionText.fontSizeMax = 34f;
        }

        if (dropdown.itemText != null)
        {
            Record(dropdown.itemText, "Style Habit unit option");
            dropdown.itemText.font = font;
            dropdown.itemText.color = Brown;
            dropdown.itemText.enableAutoSizing = true;
            dropdown.itemText.fontSizeMin = 25f;
            dropdown.itemText.fontSizeMax = 32f;
        }
    }

    private static void StyleReward(Transform root, TMP_FontAsset font, Sprite sprite)
    {
        Transform reward = RequireTransform(root, "HabitScroll View/Viewport/Content/Reward");
        StyleImage(GetOrAdd<Image>(reward.gameObject), sprite,
            new Color32(0xFF, 0xF2, 0xCE, 0xFF), false);

        TMP_Text[] labels = reward.GetComponentsInChildren<TMP_Text>(true);
        for (int index = 0; index < labels.Length; index++)
        {
            TMP_Text label = labels[index];
            Record(label, "Emphasize Habit expected reward");
            label.font = font;
            label.fontStyle = FontStyles.Bold;
            label.color = index == 0 ? Brown : new Color32(0x9A, 0x61, 0x16, 0xFF);
            label.enableAutoSizing = true;
            label.fontSizeMin = index == 0 ? 28f : 32f;
            label.fontSizeMax = index == 0 ? 36f : 42f;
        }
    }

    private static void StyleButton(
        Button button, Sprite sprite, Color imageColor, Color textColor, TMP_FontAsset font, float fontSize)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.image != null ? button.image : GetOrAdd<Image>(button.gameObject);
        StyleImage(image, sprite, imageColor, true);
        Record(button, "Style Habit form button");
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(0xFF, 0xF5, 0xDB, 0xFF);
        colors.selectedColor = new Color32(0xFF, 0xE9, 0xB2, 0xFF);
        colors.pressedColor = new Color32(0xD9, 0xB7, 0x72, 0xFF);
        colors.disabledColor = new Color32(0x8D, 0x82, 0x76, 0x72);
        colors.colorMultiplier = 1f;
        button.colors = colors;
        SetPreferredHeight(button.gameObject, 84f);

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            Record(text, "Style Habit button label");
            text.font = font;
            text.fontStyle = FontStyles.Bold;
            text.color = textColor;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 22f;
            text.fontSizeMax = fontSize;
            text.margin = new Vector4(14f, 6f, 14f, 8f);
        }
    }

    private static void StyleImage(Image image, Sprite sprite, Color color, bool raycast)
    {
        if (image == null)
        {
            return;
        }

        Record(image, "Style Habit form image");
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = raycast;
    }

    private static void StyleTitle(
        Transform root, string path, TMP_FontAsset font, float size, Color color)
    {
        TMP_Text text = Require<TMP_Text>(root, path);
        Record(text, "Style Habit form title");
        text.font = font;
        text.fontStyle = FontStyles.Bold;
        text.fontSize = size;
        text.fontSizeMax = size;
        text.fontSizeMin = 30f;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
    }

    private static void StyleHeading(TMP_Text text, TMP_FontAsset font)
    {
        Record(text, "Style Habit form section heading");
        text.font = font;
        text.fontStyle = FontStyles.Bold;
        text.fontSize = 36f;
        text.fontSizeMax = 36f;
        text.fontSizeMin = 26f;
        text.color = Brown;
        text.alignment = TextAlignmentOptions.Left;
    }

    private static void ConfigureGrid(Transform root, string path, Vector2 cellSize, Vector2 spacing)
    {
        RectTransform gridRect = Require<RectTransform>(root, path);
        Record(gridRect, "Inset Habit category grid");
        gridRect.anchorMin = new Vector2(0f, 1f);
        gridRect.anchorMax = new Vector2(1f, 1f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.anchoredPosition = new Vector2(0f, -172f);
        gridRect.sizeDelta = new Vector2(-40f, 180f);

        GridLayoutGroup grid = GetOrAdd<GridLayoutGroup>(gridRect.gameObject);
        Record(grid, "Layout Habit category buttons");
        grid.cellSize = cellSize;
        grid.spacing = spacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.MiddleCenter;
    }

    private static void ConfigureSectionTitle(Transform root, string path)
    {
        RectTransform title = Require<RectTransform>(root, path);
        Record(title, "Layout Habit form section title");
        title.anchorMin = new Vector2(0f, 1f);
        title.anchorMax = new Vector2(1f, 1f);
        title.pivot = new Vector2(0.5f, 1f);
        title.anchoredPosition = new Vector2(0f, -18f);
        title.sizeDelta = new Vector2(-40f, 48f);
    }

    private static void ConfigureHeader(Transform root)
    {
        RectTransform backButton = Require<RectTransform>(root, "Header/BackButton");
        Record(backButton, "Inset Habit form back button");
        backButton.anchorMin = new Vector2(0f, 0.5f);
        backButton.anchorMax = new Vector2(0f, 0.5f);
        backButton.pivot = new Vector2(0f, 0.5f);
        backButton.anchoredPosition = new Vector2(HeaderBackPadding, 0f);
    }

    private static void ConfigureScrollArea(
        ValidationResult result, RectTransform header, RectTransform scrollArea)
    {
        RectTransform form = result.FormRoot.GetComponent<RectTransform>();
        RectTransform tapBar = result.Canvas.transform.Find("TapBar") as RectTransform;
        if (form == null || tapBar == null)
        {
            throw new InvalidOperationException("LifeAdd_1 또는 Canvas/TapBar RectTransform을 찾을 수 없습니다.");
        }

        Canvas.ForceUpdateCanvases();
        Rect headerBounds = RelativeRect(form, header);
        Rect tapBarBounds = RelativeRect(form, tapBar);
        float topInset = Mathf.Max(0f, form.rect.yMax - headerBounds.yMin) + ScrollEdgeGap;
        float bottomInset = Mathf.Max(0f, tapBarBounds.yMax - form.rect.yMin) + ScrollEdgeGap;

        scrollArea.anchorMin = Vector2.zero;
        scrollArea.anchorMax = Vector2.one;
        scrollArea.pivot = new Vector2(0.5f, 0.5f);
        scrollArea.offsetMin = new Vector2(ScrollHorizontalInset, bottomInset);
        scrollArea.offsetMax = new Vector2(-ScrollHorizontalInset, -topInset);
    }

    private static void ConfigureAmountRow(Transform root, string path, float spacing)
    {
        RectTransform row = Require<RectTransform>(root, path);
        Record(row, "Inset Habit amount controls");
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0.5f, 0.5f);
        row.anchoredPosition = new Vector2(0f, -138f);
        row.sizeDelta = new Vector2(-40f, 112f);

        HorizontalLayoutGroup group = GetOrAdd<HorizontalLayoutGroup>(row.gameObject);
        Record(group, "Layout Habit amount controls");
        group.padding = new RectOffset(12, 12, 12, 12);
        group.spacing = spacing;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;
    }

    private static void ConfigureNameSection(Transform root)
    {
        const string namePath = "HabitScroll View/Viewport/Content/Name/";
        RectTransform title = Require<RectTransform>(root, namePath + "Title");
        RectTransform input = Require<RectTransform>(root, namePath + "HabitInput");

        ConfigureTopRow(title, 18f, 48f, 40f, "Layout Habit name title");
        ConfigureTopRow(input, 78f, 88f, 40f, "Layout Habit name input");
    }

    private static void ConfigureDurationPanel(Transform root, TMP_FontAsset font)
    {
        const string durationPath = "HabitScroll View/Viewport/Content/DurationPanel/";
        RectTransform section = Require<RectTransform>(
            root, "HabitScroll View/Viewport/Content/DurationPanel");
        RectTransform label = Require<RectTransform>(root, durationPath + "DurationLabel");
        RectTransform input = Require<RectTransform>(root, durationPath + "DurationInput");
        RectTransform days = Require<RectTransform>(root, durationPath + "DaysText");

        Record(section, "Size inactive Habit duration section");
        section.sizeDelta = new Vector2(section.sizeDelta.x, 188f);

        ConfigureTopRow(label, 18f, 48f, 40f, "Layout Habit duration title");
        StyleHeading(Require<TMP_Text>(root, durationPath + "DurationLabel"), font);

        Record(input, "Layout Habit duration input");
        input.anchorMin = new Vector2(0.5f, 1f);
        input.anchorMax = new Vector2(0.5f, 1f);
        input.pivot = new Vector2(0.5f, 0.5f);
        input.anchoredPosition = new Vector2(-98f, -126f);
        input.sizeDelta = new Vector2(360f, 88f);

        Record(days, "Layout Habit duration unit");
        days.anchorMin = new Vector2(0.5f, 1f);
        days.anchorMax = new Vector2(0.5f, 1f);
        days.pivot = new Vector2(0.5f, 0.5f);
        days.anchoredPosition = new Vector2(188f, -126f);
        days.sizeDelta = new Vector2(180f, 60f);

        TMP_Text daysText = Require<TMP_Text>(root, durationPath + "DaysText");
        Record(daysText, "Align Habit duration unit");
        daysText.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private static void ConfigureSaveSection(Transform root)
    {
        RectTransform button = Require<RectTransform>(
            root, "HabitScroll View/Viewport/Content/Save/SaveButton");
        Record(button, "Inset Habit save button");
        button.anchorMin = Vector2.zero;
        button.anchorMax = Vector2.one;
        button.pivot = new Vector2(0.5f, 0.5f);
        button.anchoredPosition = Vector2.zero;
        button.sizeDelta = new Vector2(-40f, -32f);
    }

    private static void ConfigureTopRow(
        RectTransform target, float topInset, float height, float horizontalInset, string label)
    {
        Record(target, label);
        target.anchorMin = new Vector2(0f, 1f);
        target.anchorMax = new Vector2(1f, 1f);
        target.pivot = new Vector2(0.5f, 1f);
        target.anchoredPosition = new Vector2(0f, -topInset);
        target.sizeDelta = new Vector2(-horizontalInset, height);
    }

    private static void ConfigureSegmentRow(Transform root, string path, float spacing)
    {
        RectTransform row = Require<RectTransform>(root, path);
        Record(row, "Inset Habit form segmented control");
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0.5f, 0.5f);
        row.anchoredPosition = new Vector2(0f, -136f);
        row.sizeDelta = new Vector2(-40f, 108f);

        HorizontalLayoutGroup group = GetOrAdd<HorizontalLayoutGroup>(row.gameObject);
        Record(group, "Layout Habit form segmented control");
        group.padding = new RectOffset(14, 14, 12, 12);
        group.spacing = spacing;
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;
    }

    private static List<string> ValidateLayoutBounds(ValidationResult result)
    {
        List<string> errors = new List<string>();
        Transform root = result.FormRoot.transform;
        RectTransform content = Require<RectTransform>(root, "HabitScroll View/Viewport/Content");
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();

        string[][] sections =
        {
            new[] { "Name", "HabitInput" },
            new[] { "Category", "CategoryGrid" },
            new[] { "RecordTypePanel", "ButtonGroup" },
            new[] { "Repeat", "ButtonGroup" },
            new[] { "Amount", "InputGroup" }
        };

        foreach (string[] item in sections)
        {
            string sectionPath = "HabitScroll View/Viewport/Content/" + item[0];
            RectTransform section = Require<RectTransform>(root, sectionPath);
            if (!ParticipatesInLayout(content, section))
            {
                continue;
            }

            RectTransform title = Require<RectTransform>(root, sectionPath + "/Title");
            RectTransform controls = Require<RectTransform>(root, sectionPath + "/" + item[1]);
            Rect titleBounds = RelativeRect(section, title);
            Rect controlBounds = RelativeRect(section, controls);

            ValidateContained(section.rect, titleBounds, item[0] + " 제목", errors);
            ValidateContained(section.rect, controlBounds, item[0] + " 콘텐츠", errors);
            if (titleBounds.yMin < controlBounds.yMax + 12f)
            {
                errors.Add(item[0] + " 제목과 콘텐츠 사이의 세로 여백이 12px 미만입니다.");
            }

            foreach (Transform child in controls)
            {
                if (child is RectTransform childRect && child.gameObject.activeSelf)
                {
                    ValidateContained(section.rect, RelativeRect(section, childRect),
                        item[0] + "/" + child.name, errors);
                }
            }
        }

        RectTransform durationSection = Require<RectTransform>(
            root, "HabitScroll View/Viewport/Content/DurationPanel");
        // Duration은 기본 상태에서 비활성이므로 활성 여부와 관계없이 LongTerm 배치를 검증한다.
        {
            RectTransform durationLabel = Require<RectTransform>(
                root, "HabitScroll View/Viewport/Content/DurationPanel/DurationLabel");
            RectTransform durationInput = Require<RectTransform>(
                root, "HabitScroll View/Viewport/Content/DurationPanel/DurationInput");
            RectTransform durationDays = Require<RectTransform>(
                root, "HabitScroll View/Viewport/Content/DurationPanel/DaysText");
            Rect labelBounds = RelativeRect(durationSection, durationLabel);
            Rect inputBounds = RelativeRect(durationSection, durationInput);
            Rect daysBounds = RelativeRect(durationSection, durationDays);

            ValidateContained(durationSection.rect, labelBounds, "DurationPanel 제목", errors);
            ValidateContained(durationSection.rect, inputBounds, "DurationPanel 입력", errors);
            ValidateContained(durationSection.rect, daysBounds, "DurationPanel 단위", errors);
            float controlsTop = Mathf.Max(inputBounds.yMax, daysBounds.yMax);
            if (labelBounds.yMin < controlsTop + 12f)
            {
                errors.Add("DurationPanel 제목과 입력 컨트롤 사이의 세로 여백이 12px 미만입니다.");
            }

            if (Mathf.Abs(inputBounds.center.y - daysBounds.center.y) > 0.5f)
            {
                errors.Add("DurationPanel 숫자 입력과 Days 텍스트의 세로 중심이 정렬되지 않았습니다.");
            }

            if (daysBounds.xMin < inputBounds.xMax + 12f)
            {
                errors.Add("DurationPanel 숫자 입력과 Days 텍스트 사이의 가로 여백이 12px 미만입니다.");
            }
        }

        RectTransform scroll = Require<RectTransform>(root, "HabitScroll View");
        ScrollRect scrollRect = scroll.GetComponent<ScrollRect>();
        RectTransform viewport = Require<RectTransform>(root, "HabitScroll View/Viewport");
        if (scrollRect == null || !scrollRect.enabled || !scrollRect.vertical || scrollRect.horizontal ||
            scrollRect.content != content || scrollRect.viewport != viewport)
        {
            errors.Add("HabitScroll View의 세로 전용 ScrollRect 또는 Viewport/Content 연결이 유효하지 않습니다.");
        }

        if (scrollRect != null && scrollRect.movementType != ScrollRect.MovementType.Clamped)
        {
            errors.Add("HabitScroll View가 Clamped가 아니어서 드래그 해제 시 Elastic 복원이 발생할 수 있습니다.");
        }

        if (scrollRect != null && scrollRect.scrollSensitivity < 10f)
        {
            errors.Add($"HabitScroll View의 scrollSensitivity({scrollRect.scrollSensitivity:F1})가 너무 낮아 " +
                       "마우스 휠 이동을 확인하기 어렵습니다.");
        }

        Image scrollInputGraphic = scroll.GetComponent<Image>();
        if (scrollInputGraphic == null || !scrollInputGraphic.enabled || !scrollInputGraphic.raycastTarget)
        {
            errors.Add("HabitScroll View의 입력 표면 Image가 Raycast Target이 아니어서 빈 영역에서 휠/드래그를 받을 수 없습니다.");
        }

        Mask viewportMask = viewport.GetComponent<Mask>();
        RectMask2D viewportRectMask = viewport.GetComponent<RectMask2D>();
        if ((viewportMask == null || !viewportMask.enabled) &&
            (viewportRectMask == null || !viewportRectMask.enabled))
        {
            errors.Add("Viewport에 활성 Mask 또는 RectMask2D가 없습니다.");
        }

        GraphicRaycaster raycaster = result.Canvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null || !raycaster.enabled)
        {
            errors.Add("Canvas의 GraphicRaycaster가 없어 ScrollRect 포인터 입력을 받을 수 없습니다.");
        }

        foreach (CanvasGroup group in GetAncestorCanvasGroups(scroll))
        {
            if (group.isActiveAndEnabled && (!group.blocksRaycasts || !group.interactable))
            {
                errors.Add($"{HierarchyPath(result.Canvas.transform, group.transform)} CanvasGroup이 " +
                           $"입력을 차단합니다(blocksRaycasts={group.blocksRaycasts}, " +
                           $"interactable={group.interactable}, ignoreParentGroups={group.ignoreParentGroups}).");
            }
        }

        if (EditorApplication.isPlaying && (EventSystem.current == null || !EventSystem.current.enabled))
        {
            errors.Add("Play Mode의 활성 EventSystem을 찾을 수 없어 휠/드래그 이벤트를 전달할 수 없습니다.");
        }

        VerticalLayoutGroup sectionLayout = content.GetComponent<VerticalLayoutGroup>();
        if (sectionLayout == null || !sectionLayout.enabled)
        {
            errors.Add("Content의 활성 VerticalLayoutGroup을 찾을 수 없습니다.");
        }

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null || !fitter.enabled ||
            fitter.verticalFit != ContentSizeFitter.FitMode.PreferredSize)
        {
            errors.Add("Content의 활성 ContentSizeFitter가 세로 PreferredSize로 설정되어 있지 않습니다.");
        }

        if (!Mathf.Approximately(content.anchorMin.y, 1f) ||
            !Mathf.Approximately(content.anchorMax.y, 1f) ||
            !Mathf.Approximately(content.pivot.y, 1f))
        {
            errors.Add("Content가 Viewport 상단 기준으로 고정되어 있지 않아 높이 증가 시 스크롤 범위가 어긋날 수 있습니다.");
        }

        Rect previousBounds = default;
        RectTransform previousSection = null;
        RectTransform[] orderedSections = RequiredSectionPaths
            .Select(path => Require<RectTransform>(root, path))
            .OrderBy(section => section.GetSiblingIndex())
            .ToArray();
        foreach (RectTransform section in orderedSections)
        {
            if (section.parent != content)
            {
                errors.Add(section.name + " 섹션이 Content의 직접 자식이 아닙니다. " +
                           DescribeSection(content, section));
                continue;
            }

            LayoutElement layout = section.GetComponent<LayoutElement>();
            if (layout == null || layout.preferredHeight <= 0f)
            {
                errors.Add(section.name + " 섹션의 LayoutElement preferredHeight가 유효하지 않습니다. " +
                           DescribeSection(content, section));
            }

            if (!ParticipatesInLayout(content, section))
            {
                continue;
            }

            Rect bounds = RelativeRect(content, section);
            if (previousSection != null)
            {
                float gap = previousBounds.yMin - bounds.yMax;
                if (gap < -0.5f)
                {
                    errors.Add(
                        $"{previousSection.name} 섹션과 {section.name} 섹션이 {-gap:F1}px 겹칩니다. " +
                        DescribeSection(content, previousSection) + " / " +
                        DescribeSection(content, section));
                }
                else if (sectionLayout != null && gap < sectionLayout.spacing - 0.5f)
                {
                    errors.Add(
                        $"{previousSection.name} 섹션과 {section.name} 섹션 사이 간격이 " +
                        $"{gap:F1}px로 VerticalLayoutGroup 설정 {sectionLayout.spacing:F1}px보다 작습니다. " +
                        DescribeSection(content, previousSection) + " / " +
                        DescribeSection(content, section));
                }
            }

            previousBounds = bounds;
            previousSection = section;
        }

        RectTransform saveSection = Require<RectTransform>(
            root, "HabitScroll View/Viewport/Content/Save");
        RectTransform saveButton = Require<RectTransform>(
            root, "HabitScroll View/Viewport/Content/Save/SaveButton");
        ValidateContained(saveSection.rect, RelativeRect(saveSection, saveButton), "Save 버튼", errors);

        RectTransform header = Require<RectTransform>(root, "Header");
        RectTransform backButton = Require<RectTransform>(root, "Header/BackButton");
        RectTransform headerTitle = Require<RectTransform>(root, "Header/title");
        Rect backBounds = RelativeRect(header, backButton);
        Rect headerTitleBounds = RelativeRect(header, headerTitle);
        ValidateContained(header.rect, backBounds, "Back 버튼", errors);
        float backLeftPadding = backBounds.xMin - header.rect.xMin;
        if (backLeftPadding + 0.5f < HeaderBackPadding)
        {
            errors.Add($"Back 버튼의 헤더 내부 왼쪽 여백이 {backLeftPadding:F1}px로 " +
                       $"최소 {HeaderBackPadding:F1}px보다 작습니다.");
        }
        if (backBounds.xMax > headerTitleBounds.xMin)
        {
            errors.Add("Back 버튼과 헤더 제목이 겹칩니다.");
        }

        RectTransform tapBar = result.Canvas.transform.Find("TapBar") as RectTransform;
        if (tapBar == null)
        {
            errors.Add("Canvas/TapBar RectTransform을 찾을 수 없어 하단 네비게이션 겹침을 검사할 수 없습니다.");
        }
        else
        {
            RectTransform formRect = result.FormRoot.GetComponent<RectTransform>();
            Rect viewportBoundsInForm = RelativeRect(formRect, viewport);
            Rect tapBarBoundsInForm = RelativeRect(formRect, tapBar);
            if (viewportBoundsInForm.Overlaps(tapBarBoundsInForm))
            {
                errors.Add("Viewport가 하단 네비게이션 TapBar와 겹쳐 Save 표시와 포인터 입력을 방해합니다. " +
                           $"Viewport={FormatRect(viewportBoundsInForm)}, " +
                           $"TapBar={FormatRect(tapBarBoundsInForm)}");
            }
        }

        if (!CanFullyShowAtBottom(viewport, content, saveButton, out Rect saveAtBottom))
        {
            errors.Add("최대 스크롤 위치에서도 Save 버튼 전체를 Viewport 안에 표시할 수 없습니다. " +
                       $"Viewport={FormatRect(viewport.rect)}, Save@Bottom={FormatRect(saveAtBottom)}");
        }

        float expectedCurrentHeight = CalculateContentPreferredHeight(
            root, content, durationSection.gameObject.activeSelf);
        if (result.FormRoot.activeInHierarchy && content.rect.height + 0.5f < expectedCurrentHeight)
        {
            errors.Add(
                $"Content 실제 높이({content.rect.height:F1}px)가 활성 섹션의 예상 높이" +
                $"({expectedCurrentHeight:F1}px)보다 작아 하단 콘텐츠가 잘릴 수 있습니다.");
        }

        if (errors.Count > 0)
        {
            errors.Add("전체 섹션 레이아웃 진단:\n" +
                       BuildSectionDiagnostics(content, orderedSections) + "\n" +
                       BuildScrollDiagnostics(result));
        }

        return errors;
    }

    private static Rect RelativeRect(RectTransform relativeTo, RectTransform target)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        float xMin = float.PositiveInfinity;
        float xMax = float.NegativeInfinity;
        float yMin = float.PositiveInfinity;
        float yMax = float.NegativeInfinity;
        foreach (Vector3 corner in corners)
        {
            Vector3 local = relativeTo.InverseTransformPoint(corner);
            xMin = Mathf.Min(xMin, local.x);
            xMax = Mathf.Max(xMax, local.x);
            yMin = Mathf.Min(yMin, local.y);
            yMax = Mathf.Max(yMax, local.y);
        }

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static IEnumerable<CanvasGroup> GetAncestorCanvasGroups(Transform target)
    {
        Transform current = target;
        while (current != null)
        {
            foreach (CanvasGroup group in current.GetComponents<CanvasGroup>())
            {
                yield return group;
            }

            current = current.parent;
        }
    }

    private static bool CanFullyShowAtBottom(
        RectTransform viewport, RectTransform content, RectTransform target, out Rect targetAtBottom)
    {
        Rect contentBounds = RelativeRect(viewport, content);
        Rect targetBounds = RelativeRect(viewport, target);
        float bottomAlignmentOffset = viewport.rect.yMin - contentBounds.yMin;
        targetAtBottom = new Rect(
            targetBounds.x, targetBounds.y + bottomAlignmentOffset,
            targetBounds.width, targetBounds.height);
        return viewport.rect.Contains(targetAtBottom.min) && viewport.rect.Contains(targetAtBottom.max);
    }

    private static void ValidateContained(Rect parent, Rect child, string label, List<string> errors)
    {
        const float tolerance = 0.5f;
        List<string> overflow = new List<string>();
        if (child.xMin < parent.xMin - tolerance)
        {
            overflow.Add($"왼쪽 {parent.xMin - child.xMin:F1}px");
        }

        if (child.xMax > parent.xMax + tolerance)
        {
            overflow.Add($"오른쪽 {child.xMax - parent.xMax:F1}px");
        }

        if (child.yMin < parent.yMin - tolerance)
        {
            overflow.Add($"아래쪽 {parent.yMin - child.yMin:F1}px");
        }

        if (child.yMax > parent.yMax + tolerance)
        {
            overflow.Add($"위쪽 {child.yMax - parent.yMax:F1}px");
        }

        if (overflow.Count > 0)
        {
            errors.Add(
                $"{label} RectTransform이 부모 섹션 경계를 벗어납니다. " +
                $"부모 Bounds={FormatRect(parent)}, 콘텐츠 Bounds={FormatRect(child)}, " +
                $"초과={string.Join(", ", overflow)}");
        }
    }

    private static string FormatRect(Rect rect)
    {
        return $"[xMin={rect.xMin:F1}, xMax={rect.xMax:F1}, " +
               $"yMin={rect.yMin:F1}, yMax={rect.yMax:F1}]";
    }

    private static bool ParticipatesInLayout(RectTransform content, RectTransform section)
    {
        if (section.parent != content || !section.gameObject.activeSelf)
        {
            return false;
        }

        foreach (Component component in section.GetComponents<Component>())
        {
            if (component is ILayoutIgnorer ignorer && ignorer.ignoreLayout &&
                (!(component is Behaviour behaviour) || behaviour.enabled))
            {
                return false;
            }
        }

        return true;
    }

    private static string DescribeSection(RectTransform content, RectTransform section)
    {
        LayoutElement layout = section.GetComponent<LayoutElement>();
        LayoutGroup parentLayout = content.GetComponent<LayoutGroup>();
        string preferredHeight = layout != null ? layout.preferredHeight.ToString("F1") : "없음";
        string parentLayoutName = parentLayout != null
            ? parentLayout.GetType().Name + "(active=" + parentLayout.isActiveAndEnabled + ")"
            : "없음";

        return $"{section.name}[sibling={section.GetSiblingIndex()}, " +
               $"activeSelf={section.gameObject.activeSelf}, " +
               $"activeInHierarchy={section.gameObject.activeInHierarchy}, " +
               $"parent={section.parent?.name ?? "없음"}, parentLayout={parentLayoutName}, " +
               $"Bounds={FormatRect(RelativeRect(content, section))}, " +
               $"preferredHeight={preferredHeight}, " +
               $"layoutParticipant={ParticipatesInLayout(content, section)}]";
    }

    private static string BuildSectionDiagnostics(
        RectTransform content, IEnumerable<RectTransform> orderedSections)
    {
        List<string> diagnostics = new List<string>();
        Rect previousBounds = default;
        bool hasPreviousLayoutSection = false;
        foreach (RectTransform section in orderedSections)
        {
            bool participates = ParticipatesInLayout(content, section);
            Rect bounds = RelativeRect(content, section);
            string gap = participates && hasPreviousLayoutSection
                ? (previousBounds.yMin - bounds.yMax).ToString("F1") + "px"
                : participates ? "첫 활성 섹션" : "레이아웃 제외";
            diagnostics.Add(DescribeSection(content, section) + ", previousGap=" + gap);

            if (participates)
            {
                previousBounds = bounds;
                hasPreviousLayoutSection = true;
            }
        }

        return string.Join("\n", diagnostics);
    }

    private static string BuildScrollDiagnostics(ValidationResult result)
    {
        Transform root = result.FormRoot.transform;
        RectTransform content = Require<RectTransform>(root, "HabitScroll View/Viewport/Content");
        RectTransform viewport = Require<RectTransform>(root, "HabitScroll View/Viewport");
        RectTransform duration = Require<RectTransform>(
            root, "HabitScroll View/Viewport/Content/DurationPanel");
        RectTransform saveButton = Require<RectTransform>(
            root, "HabitScroll View/Viewport/Content/Save/SaveButton");
        ScrollRect scrollRect = Require<ScrollRect>(root, "HabitScroll View");
        Rect saveBounds = RelativeRect(content, saveButton);
        Rect contentBoundsInViewport = RelativeRect(viewport, content);
        float actualMaxScroll = Mathf.Max(0f, contentBoundsInViewport.height - viewport.rect.height);
        bool saveVisibleAtBottom = CanFullyShowAtBottom(
            viewport, content, saveButton, out Rect saveBoundsAtBottom);
        float inactiveHeight = CalculateContentPreferredHeight(root, content, false);
        float activeHeight = CalculateContentPreferredHeight(root, content, true);
        float viewportHeight = viewport.rect.height;
        Canvas canvas = result.FormRoot.GetComponentInParent<Canvas>();
        RectTransform tapBar = canvas != null ? canvas.transform.Find("TapBar") as RectTransform : null;
        RectTransform formRect = result.FormRoot.GetComponent<RectTransform>();
        bool viewportOverlapsTapBar = tapBar != null &&
                                      RelativeRect(formRect, viewport).Overlaps(RelativeRect(formRect, tapBar));
        Image inputGraphic = scrollRect.GetComponent<Image>();
        string canvasGroups = string.Join(", ", GetAncestorCanvasGroups(scrollRect.transform)
            .Select(group => $"{group.name}(enabled={group.enabled}, blocks={group.blocksRaycasts}, " +
                             $"interactable={group.interactable}, ignoreParent={group.ignoreParentGroups})"));
        if (string.IsNullOrEmpty(canvasGroups))
        {
            canvasGroups = "없음";
        }
        string activeSections = string.Join(", ", RequiredSectionPaths
            .Select(path => Require<RectTransform>(root, path))
            .Where(section => ParticipatesInLayout(content, section))
            .Select(section => section.name));

        return "스크롤 진단: " +
               $"활성 섹션=[{activeSections}], " +
               $"Content 실제 높이={content.rect.height:F1}px, " +
               $"Viewport 높이={viewportHeight:F1}px, " +
               $"실제 최대 스크롤 거리={actualMaxScroll:F1}px, " +
               $"ScrollRect enabled={scrollRect.enabled}, vertical={scrollRect.vertical}, " +
               $"horizontal={scrollRect.horizontal}, MovementType={scrollRect.movementType}, " +
               $"Inertia={scrollRect.inertia}, scrollSensitivity={scrollRect.scrollSensitivity:F1}, " +
               $"입력 Image RaycastTarget={(inputGraphic != null && inputGraphic.raycastTarget)}, " +
               $"CanvasGroups=[{canvasGroups}], " +
               $"Content anchoredPosition={content.anchoredPosition}, " +
               $"verticalNormalizedPosition={scrollRect.verticalNormalizedPosition:F3}, " +
               $"Save 버튼 하단={saveBounds.yMin:F1}px" +
               $"(Content 하단 여유={saveBounds.yMin - content.rect.yMin:F1}px), " +
               $"최대 스크롤 Save Bounds={FormatRect(saveBoundsAtBottom)}/" +
               $"전체 표시 가능={saveVisibleAtBottom}, " +
               $"Viewport/TapBar 겹침={viewportOverlapsTapBar}, " +
               $"LongTerm 비활성 예상 높이={inactiveHeight:F1}px/스크롤 범위=" +
               $"{Mathf.Max(0f, inactiveHeight - viewportHeight):F1}px, " +
               $"LongTerm 활성 예상 높이={activeHeight:F1}px/스크롤 범위=" +
               $"{Mathf.Max(0f, activeHeight - viewportHeight):F1}px, " +
               $"현재 LongTerm={(duration.gameObject.activeSelf ? "활성" : "비활성")}";
    }

    private static float CalculateContentPreferredHeight(
        Transform root, RectTransform content, bool durationActive)
    {
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        float height = layout != null ? layout.padding.top + layout.padding.bottom : 0f;
        int includedCount = 0;
        foreach (string path in RequiredSectionPaths)
        {
            RectTransform section = Require<RectTransform>(root, path);
            bool isDuration = section.name == "DurationPanel";
            bool include = isDuration
                ? durationActive && CanParticipateWhenActive(content, section)
                : ParticipatesInLayout(content, section);
            if (!include)
            {
                continue;
            }

            height += Mathf.Max(LayoutUtility.GetPreferredHeight(section), section.rect.height);
            includedCount++;
        }

        if (layout != null && includedCount > 1)
        {
            height += layout.spacing * (includedCount - 1);
        }

        return height;
    }

    private static bool CanParticipateWhenActive(RectTransform content, RectTransform section)
    {
        if (section.parent != content)
        {
            return false;
        }

        foreach (Component component in section.GetComponents<Component>())
        {
            if (component is ILayoutIgnorer ignorer && ignorer.ignoreLayout &&
                (!(component is Behaviour behaviour) || behaviour.enabled))
            {
                return false;
            }
        }

        return true;
    }

    private static void RebuildScrollLayout(Transform root)
    {
        RectTransform content = Require<RectTransform>(root, "HabitScroll View/Viewport/Content");
        LayoutRebuilder.MarkLayoutForRebuild(content);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();
    }

    private static void ResetPreviewLayoutWatcher(PlayModeStateChange change)
    {
        previewContent = null;
        previewDurationPanel = null;
        previewScrollRect = null;
        previewLayoutInitialized = false;
    }

    private static void RefreshPreviewLayoutWhenDurationChanges()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().path != ScenePath)
        {
            previewLayoutInitialized = false;
            return;
        }

        if (previewContent == null || previewDurationPanel == null || previewScrollRect == null)
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject canvas = scene.GetRootGameObjects()
                .FirstOrDefault(candidate => candidate.name == "Canvas");
            Transform form = canvas != null ? canvas.transform.Find(FormPath) : null;
            if (form == null || !form.gameObject.activeInHierarchy)
            {
                previewLayoutInitialized = false;
                return;
            }

            previewContent = form.Find("HabitScroll View/Viewport/Content") as RectTransform;
            Transform duration = form.Find("HabitScroll View/Viewport/Content/DurationPanel");
            previewDurationPanel = duration != null ? duration.gameObject : null;
            previewScrollRect = form.Find("HabitScroll View")?.GetComponent<ScrollRect>();
            if (previewContent == null || previewDurationPanel == null || previewScrollRect == null)
            {
                previewLayoutInitialized = false;
                return;
            }
        }

        bool durationActive = previewDurationPanel.activeSelf;
        if (previewLayoutInitialized && durationActive == previewDurationWasActive)
        {
            return;
        }

        float normalizedPosition = previewScrollRect.verticalNormalizedPosition;
        RebuildScrollLayout(previewContent.parent.parent.parent);
        previewScrollRect.StopMovement();
        previewScrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
        Canvas.ForceUpdateCanvases();
        previewDurationWasActive = durationActive;
        previewLayoutInitialized = true;

        GameObject formRoot = previewScrollRect.transform.parent.gameObject;
        Debug.Log(
            "[HabitPVP Habit Form] Preview 활성 콘텐츠 변경 후 ScrollRect 레이아웃을 갱신했습니다.\n" +
            BuildScrollDiagnostics(new ValidationResult { FormRoot = formRoot }),
            formRoot);
    }

    private static void SetPreferredHeight(GameObject target, float height)
    {
        LayoutElement layout = GetOrAdd<LayoutElement>(target);
        Record(layout, "Size Habit form control");
        layout.minHeight = height;
        layout.preferredHeight = height;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static T Require<T>(Transform root, string path) where T : Component
    {
        Transform target = RequireTransform(root, path);
        T component = target.GetComponent<T>();
        if (component == null)
        {
            throw new InvalidOperationException($"{typeof(T).Name} 컴포넌트가 없습니다: {path}");
        }

        return component;
    }

    private static Transform RequireTransform(Transform root, string path)
    {
        Transform target = root.Find(path);
        if (target == null)
        {
            throw new InvalidOperationException("대상 Transform이 없습니다: " + path);
        }

        return target;
    }

    private static void Record(UnityEngine.Object target, string label)
    {
        if (target == null)
        {
            return;
        }

        Undo.RecordObject(target, label);
        EditorUtility.SetDirty(target);
    }

    private static void SetHabitPanelMode(Scene scene, bool showAddForm)
    {
        Canvas canvas = UnityEngine.Object.FindObjectsOfType<Canvas>(true)
            .FirstOrDefault(item => item.gameObject.scene == scene && item.transform.parent == null &&
                                    item.gameObject.name == "Canvas");
        Transform habitTap = canvas != null ? canvas.transform.Find("HabitTap") : null;
        if (habitTap == null)
        {
            Debug.LogError("[HabitPVP Habit Form] Canvas/HabitTap을 찾을 수 없습니다.");
            return;
        }

        Dictionary<string, bool> state = new Dictionary<string, bool>
        {
            { "TopTab", !showAddForm },
            { "Life", !showAddForm },
            { "Spend", false },
            { "LifeAdd_1", showAddForm },
            { "SpendAdd_1", false },
            { "BudgetSetting", false },
            { "SpendHistory", false },
            { "WeeklyReportPanel", false },
            { "HabitDetail", false }
        };

        foreach (KeyValuePair<string, bool> item in state)
        {
            Transform target = habitTap.Find(item.Key);
            if (target != null)
            {
                SetActiveWithUndo(target.gameObject, item.Value, "Switch Habit UI preview mode");
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void SetActiveWithUndo(GameObject target, bool active, string label)
    {
        if (target == null || target.activeSelf == active)
        {
            return;
        }

        Undo.RecordObject(target, label);
        target.SetActive(active);
        EditorUtility.SetDirty(target);
        if (PrefabUtility.IsPartOfPrefabInstance(target))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }

    private static void SetAllEnabled<T>(Scene scene, bool enabled) where T : Behaviour
    {
        foreach (T component in UnityEngine.Object.FindObjectsOfType<T>(true))
        {
            if (component.gameObject.scene != scene || component.enabled == enabled)
            {
                continue;
            }

            Undo.RecordObject(component, "Switch Habit preview component state");
            component.enabled = enabled;
            EditorUtility.SetDirty(component);
            if (PrefabUtility.IsPartOfPrefabInstance(component))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
        }
    }

    private static string HierarchyPath(Transform root, Transform target)
    {
        List<string> parts = new List<string>();
        Transform current = target;
        while (current != null && current != root)
        {
            parts.Add(current.name);
            current = current.parent;
        }

        parts.Add(root.name);
        parts.Reverse();
        return string.Join("/", parts);
    }

    private sealed class ValidationResult
    {
        public Scene Scene;
        public GameObject Canvas;
        public GameObject FormRoot;
        public HabitGoalUI GoalUi;
        public TMP_FontAsset Font;
        public Sprite CardSprite;
        public Sprite CreamButtonSprite;
        public Sprite GoldButtonSprite;
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }

    private sealed class ProtectionSnapshot
    {
        private string goalUiJson;
        private string[] eventSignatures;
        private string[] dropdownOptions;
        private int dropdownValue;
        private Dictionary<string, bool> activeStates;
        private Dictionary<string, CanvasGroupState> canvasGroups;

        public static ProtectionSnapshot Capture(ValidationResult result)
        {
            return new ProtectionSnapshot
            {
                goalUiJson = EditorJsonUtility.ToJson(result.GoalUi),
                eventSignatures = CaptureEvents(result.FormRoot),
                dropdownOptions = result.GoalUi.unitDropdown.options.Select(option => option.text).ToArray(),
                dropdownValue = result.GoalUi.unitDropdown.value,
                activeStates = result.FormRoot.GetComponentsInChildren<Transform>(true)
                    .ToDictionary(
                        transform => HierarchyPath(result.FormRoot.transform, transform),
                        transform => transform.gameObject.activeSelf),
                canvasGroups = result.FormRoot.GetComponentsInChildren<CanvasGroup>(true)
                    .ToDictionary(
                        group => HierarchyPath(result.FormRoot.transform, group.transform),
                        CanvasGroupState.Capture)
            };
        }

        public void AssertUnchanged(ValidationResult result)
        {
            if (goalUiJson != EditorJsonUtility.ToJson(result.GoalUi))
            {
                throw new InvalidOperationException("HabitGoalUI SerializeField 참조가 변경되었습니다.");
            }

            if (!eventSignatures.SequenceEqual(CaptureEvents(result.FormRoot)))
            {
                throw new InvalidOperationException("Button/InputField/Dropdown UnityEvent가 변경되었습니다.");
            }

            if (!dropdownOptions.SequenceEqual(result.GoalUi.unitDropdown.options.Select(option => option.text)) ||
                dropdownValue != result.GoalUi.unitDropdown.value)
            {
                throw new InvalidOperationException("Unit Dropdown 옵션 또는 내부 value가 변경되었습니다.");
            }

            foreach (KeyValuePair<string, bool> item in activeStates)
            {
                Transform target = FindBySnapshotPath(result.FormRoot.transform, item.Key);
                if (target == null || target.gameObject.activeSelf != item.Value)
                {
                    throw new InvalidOperationException("입력 조건에 사용되는 활성 상태가 변경되었습니다: " + item.Key);
                }
            }

            foreach (KeyValuePair<string, CanvasGroupState> item in canvasGroups)
            {
                Transform target = FindBySnapshotPath(result.FormRoot.transform, item.Key);
                CanvasGroup group = target != null ? target.GetComponent<CanvasGroup>() : null;
                if (group == null || !item.Value.Equals(group))
                {
                    throw new InvalidOperationException("CanvasGroup 활성화 조건이 변경되었습니다: " + item.Key);
                }
            }
        }

        private static string[] CaptureEvents(GameObject root)
        {
            List<string> signatures = new List<string>();
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                AddEvent(signatures, root, button.transform, "Button.onClick", button.onClick);
            }

            foreach (TMP_Dropdown dropdown in root.GetComponentsInChildren<TMP_Dropdown>(true))
            {
                AddEvent(signatures, root, dropdown.transform, "Dropdown.onValueChanged", dropdown.onValueChanged);
            }

            foreach (TMP_InputField input in root.GetComponentsInChildren<TMP_InputField>(true))
            {
                AddEvent(signatures, root, input.transform, "Input.onValueChanged", input.onValueChanged);
                AddEvent(signatures, root, input.transform, "Input.onEndEdit", input.onEndEdit);
                AddEvent(signatures, root, input.transform, "Input.onSubmit", input.onSubmit);
            }

            signatures.Sort(StringComparer.Ordinal);
            return signatures.ToArray();
        }

        private static void AddEvent(
            ICollection<string> output, GameObject root, Transform owner, string label, UnityEventBase unityEvent)
        {
            string path = HierarchyPath(root.transform, owner);
            output.Add(path + "|" + label + "|count=" + unityEvent.GetPersistentEventCount());
            for (int index = 0; index < unityEvent.GetPersistentEventCount(); index++)
            {
                UnityEngine.Object target = unityEvent.GetPersistentTarget(index);
                output.Add(path + "|" + label + "|" + index + "|" +
                           (target != null ? target.GetInstanceID().ToString() : "null") + "|" +
                           unityEvent.GetPersistentMethodName(index));
            }
        }

        private static Transform FindBySnapshotPath(Transform root, string path)
        {
            if (path == root.name)
            {
                return root;
            }

            string prefix = root.name + "/";
            return path.StartsWith(prefix, StringComparison.Ordinal)
                ? root.Find(path.Substring(prefix.Length))
                : null;
        }
    }

    private readonly struct CanvasGroupState
    {
        private readonly float alpha;
        private readonly bool interactable;
        private readonly bool blocksRaycasts;

        private CanvasGroupState(float alpha, bool interactable, bool blocksRaycasts)
        {
            this.alpha = alpha;
            this.interactable = interactable;
            this.blocksRaycasts = blocksRaycasts;
        }

        public static CanvasGroupState Capture(CanvasGroup group)
        {
            return new CanvasGroupState(group.alpha, group.interactable, group.blocksRaycasts);
        }

        public bool Equals(CanvasGroup group)
        {
            return Mathf.Approximately(alpha, group.alpha) &&
                   interactable == group.interactable && blocksRaycasts == group.blocksRaycasts;
        }
    }
}
