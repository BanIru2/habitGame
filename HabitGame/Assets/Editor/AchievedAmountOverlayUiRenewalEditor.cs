using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AchievedAmountOverlayUiRenewalEditor
{
    private const string ScenePath = "Assets/Scenes/UITestScene.unity";
    private const string CanvasPrefabPath = "Assets/Resources/UIPrefabs/Canvas.prefab";
    private const string OverlayPath = "HabitTap/AchievedAmountOverlay";
    private const string KoreanFontPath = "Assets/Login/Fonts/NotoSansKR Login SDF.asset";
    private const string CardSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/panel_rpg_card.png";
    private const string CreamButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_cream.png";
    private const string GoldButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_rpg.png";
    private const string PreviewRootName = "NumberOverlayPreviewRoot";
    private const string HabitItemPreviewRootName = "HabitItemPreviewTestRoot";
    private const string ValidateMenu = "Tools/HabitPVP/UI Renewal/Validate NUMBER Input Overlay";
    private const string ApplyMenu = "Tools/HabitPVP/UI Renewal/Apply NUMBER Input Overlay";
    private const string PreviewMenu =
        "Tools/HabitPVP/UI Renewal/Preview Mode/D. NUMBER Input Overlay Renewal";

    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x86, 0x68, 0x5D, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);
    private static readonly Color32 Disabled = new Color32(0xB9, 0xAA, 0x91, 0xB8);
    private static readonly Color32 Backdrop = new Color32(0x2A, 0x1B, 0x1D, 0xB8);

    static AchievedAmountOverlayUiRenewalEditor()
    {
        EditorApplication.update -= RestoreWhenAnotherPreviewModeIsSelected;
        EditorApplication.update += RestoreWhenAnotherPreviewModeIsSelected;
    }

    [MenuItem(ValidateMenu, false, 2050)]
    private static void Validate()
    {
        ValidationResult result = ValidateScene(true);
        if (!result.IsValid)
        {
            return;
        }

        List<string> layoutErrors = ValidateLayout(result);
        if (layoutErrors.Count > 0)
        {
            foreach (string error in layoutErrors)
            {
                Debug.LogError("[HabitPVP NUMBER Overlay] " + error, result.Overlay.gameObject);
            }

            return;
        }

        Debug.Log(
            "[HabitPVP NUMBER Overlay] 검증 성공. Canvas Prefab 출처, 6개 SerializeField, " +
            "입력 설정 및 버튼 이벤트가 유지되고 1080x1920 레이아웃 경계가 정상입니다.",
            result.Overlay.gameObject);
    }

    [MenuItem(ApplyMenu, false, 2051)]
    private static void Apply()
    {
        ValidationResult result = ValidateScene(true);
        if (!result.IsValid)
        {
            Debug.LogError("[HabitPVP NUMBER Overlay] Apply 중단: 검증 오류를 먼저 해결해주세요.");
            return;
        }

        ProtectionSnapshot protectedState = ProtectionSnapshot.Capture(result);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply NUMBER Input Overlay UI Renewal");

        try
        {
            ApplyStyle(result);
            EnsurePreviewRoot(result);
            protectedState.AssertUnchanged(result);

            List<string> layoutErrors = ValidateLayout(result);
            if (layoutErrors.Count > 0)
            {
                throw new InvalidOperationException(string.Join("\n", layoutErrors));
            }

            EditorSceneManager.MarkSceneDirty(result.Scene);
            Undo.CollapseUndoOperations(undoGroup);

            if (!EditorSceneManager.SaveScene(result.Scene))
            {
                throw new InvalidOperationException("UITestScene 저장에 실패했습니다.");
            }

            Selection.activeGameObject = result.Overlay.gameObject;
            Debug.Log(
                "[HabitPVP NUMBER Overlay] 적용 성공. UITestScene의 Canvas Prefab Instance Override와 " +
                "D Preview 전용 루트만 저장했습니다. Canvas.prefab, MainScene, HabitGoalUI, HabitItem 및 " +
                "API 코드는 변경하지 않았습니다.",
                result.Overlay.gameObject);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            EditorSceneManager.MarkSceneDirty(result.Scene);
            Debug.LogException(exception);
            Debug.LogError("[HabitPVP NUMBER Overlay] Apply 실패. 이번 실행의 변경을 Undo로 되돌렸습니다.");
        }
    }

    [MenuItem(ApplyMenu, true)]
    private static bool CanApply() => CanUseSceneMenus();

    public static void ApplyFromBatchMode()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Apply();

        ValidationResult result = ValidateScene(false);
        List<string> errors = result.IsValid ? ValidateLayout(result) : result.Errors;
        if (FindRoot(result.Scene, PreviewRootName) == null)
        {
            errors.Add("D Preview 루트가 생성되지 않았습니다.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join("\n", errors));
        }
    }

    [MenuItem(PreviewMenu, false, 2052)]
    private static void ActivatePreview()
    {
        PvpRankingUiRenewalEditor.RestorePvpPreviewState();
        ShopGachaUiRenewalEditor.RestoreShopPreviewState();
        InventoryMainUiRenewalEditor.RestoreInventoryPreviewState();
        CharacterMainUiRenewalEditor.RestoreCharacterPreviewState();
        PhotoVerificationPopupUiRenewalEditor.RestorePhotoPreviewState();
        RestoreNumberPreviewState();
        ValidationResult result = ValidateScene(true);
        if (!result.IsValid)
        {
            return;
        }

        GameObject previewRoot = FindRoot(result.Scene, PreviewRootName);
        if (previewRoot == null)
        {
            Debug.LogError(
                "[HabitPVP NUMBER Overlay] D Preview 루트가 없습니다. " +
                "Apply NUMBER Input Overlay를 먼저 실행해주세요.");
            return;
        }

        HabitGoalFormUiRenewalEditor.RestoreHabitMainPreviewState();
        HabitGoalFormUiRenewalEditor.EnsureHabitCanvasHierarchyActive(result.Scene);
        SetActive(FindRoot(result.Scene, HabitItemPreviewRootName), false, "Hide HabitItem preview for D mode");
        DisableAll<HabitListManager>(result.Scene);
        DisableAll<HabitUIManager>(result.Scene);
        DisableAll<HabitItem>(result.Scene);
        SetAllEnabledInChildren<AchievedAmountOverlayPreviewController>(previewRoot, true);

        SetActive(previewRoot, true, "Activate NUMBER overlay preview");
        SetActive(result.Overlay.gameObject, true, "Activate NUMBER overlay root");
        SetActive(result.Popup, true, "Show NUMBER overlay popup");
        SetPreviewText(result);

        EditorSceneManager.MarkSceneDirty(result.Scene);
        Selection.activeGameObject = result.Popup;
        Debug.Log(
            "[HabitPVP NUMBER Overlay] D 모드: 실제 Overlay Hierarchy를 로컬 데이터로 표시했습니다. " +
            "Play Mode에서는 입력·확인·취소·닫기 상태를 테스트할 수 있으며 HabitItem과 API Service를 " +
            "호출하지 않습니다. A/B/C 메뉴 선택 시 D 상태는 자동으로 복원됩니다.",
            result.Popup);
    }

    [MenuItem(PreviewMenu, true)]
    private static bool CanActivatePreview() => CanUseSceneMenus();

    private static bool CanUseSceneMenus()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
               SceneManager.GetActiveScene().path == ScenePath;
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

        Transform overlayTransform = result.Canvas.transform.Find(OverlayPath);
        if (overlayTransform == null)
        {
            result.Errors.Add($"대상 경로를 찾을 수 없습니다: Canvas/{OverlayPath}");
            return FinishValidation(result, log);
        }

        AchievedAmountOverlay[] overlays = UnityEngine.Object.FindObjectsOfType<AchievedAmountOverlay>(true)
            .Where(item => item.gameObject.scene == result.Scene)
            .ToArray();
        if (overlays.Length != 1 || overlays[0].gameObject != overlayTransform.gameObject)
        {
            result.Errors.Add($"UITestScene의 AchievedAmountOverlay가 대상 경로에 정확히 하나 있어야 합니다. 현재: {overlays.Length}");
            return FinishValidation(result, log);
        }

        result.Overlay = overlays[0];
        result.OverlayRoot = overlayTransform.GetComponent<RectTransform>();
        result.Popup = RequireObject(overlayTransform, "Popup", result);
        result.PopupRect = result.Popup != null ? result.Popup.GetComponent<RectTransform>() : null;
        result.GoalText = Require<TextMeshProUGUI>(overlayTransform, "Popup/Goal Text", result);
        result.AmountInput = Require<TMP_InputField>(overlayTransform, "Popup/AmountInput", result);
        result.UnitText = Require<TextMeshProUGUI>(overlayTransform, "Popup/UnitText", result);
        result.CancelButton = Require<Button>(overlayTransform, "Popup/CancelButton", result);
        result.ConfirmButton = Require<Button>(overlayTransform, "Popup/ConfirmButton", result);
        result.TitleText = Require<TextMeshProUGUI>(overlayTransform, "Popup/TitleText", result);
        result.InputText = Require<TextMeshProUGUI>(overlayTransform, "Popup/AmountInput/Text Area/Text", result);
        result.Placeholder = Require<TextMeshProUGUI>(overlayTransform, "Popup/AmountInput/Text Area/Placeholder", result);

        ValidateSerializedReferences(result);
        ValidateInputConfiguration(result);
        result.Font = LoadAsset<TMP_FontAsset>(KoreanFontPath, result);
        result.CardSprite = LoadAsset<Sprite>(CardSpritePath, result);
        result.CreamButtonSprite = LoadAsset<Sprite>(CreamButtonSpritePath, result);
        result.GoldButtonSprite = LoadAsset<Sprite>(GoldButtonSpritePath, result);
        return FinishValidation(result, log);
    }

    private static void ValidateSerializedReferences(ValidationResult result)
    {
        SerializedObject serialized = new SerializedObject(result.Overlay);
        Dictionary<string, UnityEngine.Object> expected = new Dictionary<string, UnityEngine.Object>
        {
            { "popup", result.Popup },
            { "goalText", result.GoalText },
            { "amountInput", result.AmountInput },
            { "unitText", result.UnitText },
            { "cancelButton", result.CancelButton },
            { "confirmButton", result.ConfirmButton }
        };

        foreach (KeyValuePair<string, UnityEngine.Object> pair in expected)
        {
            SerializedProperty property = serialized.FindProperty(pair.Key);
            if (property == null || property.objectReferenceValue != pair.Value)
            {
                result.Errors.Add($"AchievedAmountOverlay.{pair.Key} SerializeField가 실제 Hierarchy 대상과 다릅니다.");
            }
        }
    }

    private static void ValidateInputConfiguration(ValidationResult result)
    {
        if (result.AmountInput == null)
        {
            return;
        }

        if (result.AmountInput.textComponent != result.InputText)
        {
            result.Errors.Add("AmountInput.textComponent가 기존 Text를 참조하지 않습니다.");
        }

        if (result.AmountInput.placeholder != result.Placeholder)
        {
            result.Errors.Add("AmountInput.placeholder가 기존 Placeholder를 참조하지 않습니다.");
        }

        if (result.AmountInput.contentType != TMP_InputField.ContentType.IntegerNumber &&
            result.AmountInput.characterValidation != TMP_InputField.CharacterValidation.Integer)
        {
            result.Errors.Add("AmountInput이 정수 입력으로 제한되어 있지 않습니다.");
        }
    }

    private static ValidationResult FinishValidation(ValidationResult result, bool log)
    {
        if (log)
        {
            foreach (string error in result.Errors)
            {
                Debug.LogError("[HabitPVP NUMBER Overlay] " + error);
            }
        }

        return result;
    }

    private static void ApplyStyle(ValidationResult result)
    {
        SetRect(result.OverlayRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            "Stretch NUMBER overlay backdrop");
        StyleImage(result.OverlayRoot.GetComponent<Image>(), null, Backdrop, Image.Type.Simple, true);

        SetRect(result.PopupRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 70f), new Vector2(880f, 760f), "Center NUMBER popup");
        StyleImage(result.Popup.GetComponent<Image>(), result.CardSprite, Cream, Image.Type.Sliced, true);

        StyleText(result.TitleText, result.Font, 48f, 56f, Brown, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(result.TitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -78f), new Vector2(720f, 92f), "Layout NUMBER title");

        StyleText(result.GoalText, result.Font, 32f, 42f, Brown, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(result.GoalText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -190f), new Vector2(720f, 96f), "Layout NUMBER goal");

        RectTransform inputRect = result.AmountInput.GetComponent<RectTransform>();
        SetRect(inputRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-80f, 5f), new Vector2(520f, 144f), "Layout NUMBER input");
        StyleImage(result.AmountInput.GetComponent<Image>(), result.CreamButtonSprite, Color.white,
            Image.Type.Sliced, true);
        Record(result.AmountInput, "Style NUMBER input selection");
        result.AmountInput.caretColor = Brown;
        result.AmountInput.selectionColor = new Color32(0xE7, 0xA8, 0x35, 0x70);
        result.AmountInput.caretWidth = 4;
        SavePrefabOverride(result.AmountInput);

        RectTransform textArea = RequireRect(result.AmountInput.transform, "Text Area");
        SetRect(textArea, Vector2.zero, Vector2.one, new Vector2(26f, 18f), new Vector2(-26f, -18f),
            "Pad NUMBER input text");
        StyleText(result.InputText, result.Font, 42f, 54f, Brown, FontStyles.Bold, TextAlignmentOptions.Center);
        StyleText(result.Placeholder, result.Font, 30f, 38f, MutedBrown, FontStyles.Normal,
            TextAlignmentOptions.Center);

        StyleText(result.UnitText, result.Font, 34f, 44f, Brown, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(result.UnitText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(290f, 5f), new Vector2(150f, 110f), "Layout NUMBER unit");

        StyleButton(result.CancelButton, result.CreamButtonSprite, Cream, Brown);
        SetRect(result.CancelButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-190f, 112f), new Vector2(300f, 112f), "Layout NUMBER cancel");
        StyleButton(result.ConfirmButton, result.GoldButtonSprite, Gold, Color.white);
        SetRect(result.ConfirmButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(190f, 112f), new Vector2(300f, 112f), "Layout NUMBER confirm");

        foreach (TMP_Text text in result.Popup.GetComponentsInChildren<TMP_Text>(true))
        {
            Record(text, "Use Korean font in NUMBER overlay");
            text.font = result.Font;
            SavePrefabOverride(text);
        }
    }

    private static void StyleButton(Button button, Sprite sprite, Color32 baseColor, Color textColor)
    {
        Image image = button.GetComponent<Image>();
        StyleImage(image, sprite, baseColor, Image.Type.Sliced, true);
        Record(button, "Style NUMBER overlay button states");
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(0xFF, 0xF4, 0xD2, 0xFF);
        colors.pressedColor = new Color32(0xD5, 0xA1, 0x55, 0xFF);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = Disabled;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        SavePrefabOverride(button);

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            StyleText(label, label.font, 30f, 38f, textColor, FontStyles.Bold, TextAlignmentOptions.Center);
        }
    }

    private static void EnsurePreviewRoot(ValidationResult result)
    {
        GameObject existing = FindRoot(result.Scene, PreviewRootName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        GameObject root = new GameObject(PreviewRootName, typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root, "Create NUMBER overlay preview root");
        SceneManager.MoveGameObjectToScene(root, result.Scene);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 1f;

        GameObject panel = CreateImage(root.transform, "PreviewStatusPanel", result.CardSprite, Cream);
        SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 380f), new Vector2(960f, 320f), "Layout NUMBER preview controls");

        TMP_Text heading = CreateText(panel.transform, "Heading",
            "D. NUMBER Input Overlay Renewal · LOCAL PREVIEW", result.Font, 34f, Brown, FontStyles.Bold);
        SetRect(heading.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(32f, -76f), new Vector2(-32f, -20f), "Layout preview heading");

        TMP_Text habitName = CreateText(panel.transform, "HabitName", "하루 2L 물 마시기",
            result.Font, 33f, Brown, FontStyles.Bold);
        SetRect(habitName.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(44f, -136f), new Vector2(-44f, -84f), "Layout preview habit name");

        TMP_Text status = CreateText(panel.transform, "StatusText",
            "현재 누적 750 / 2,000 mL", result.Font, 34f, Brown, FontStyles.Bold);
        SetRect(status.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(44f, -210f), new Vector2(-44f, -146f), "Layout preview status");

        Button open = CreatePreviewButton(panel.transform, "OpenButton", "다시 열기", result.Font,
            result.CreamButtonSprite, Cream, Brown);
        SetRect(open.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-145f, 68f), new Vector2(260f, 92f), "Layout preview open button");
        Button close = CreatePreviewButton(panel.transform, "CloseButton", "닫기", result.Font,
            result.GoldButtonSprite, Gold, Color.white);
        SetRect(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(145f, 68f), new Vector2(260f, 92f), "Layout preview close button");

        AchievedAmountOverlayPreviewController controller =
            Undo.AddComponent<AchievedAmountOverlayPreviewController>(root);
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("overlay").objectReferenceValue = result.Overlay;
        serialized.FindProperty("openButton").objectReferenceValue = open;
        serialized.FindProperty("closeButton").objectReferenceValue = close;
        serialized.FindProperty("statusText").objectReferenceValue = status;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        root.SetActive(false);
    }

    private static Button CreatePreviewButton(Transform parent, string name, string label, TMP_FontAsset font,
        Sprite sprite, Color32 color, Color textColor)
    {
        GameObject buttonObject = CreateImage(parent, name, sprite, color);
        Button button = Undo.AddComponent<Button>(buttonObject);
        button.targetGraphic = buttonObject.GetComponent<Image>();
        TMP_Text text = CreateText(buttonObject.transform, "Label", label, font, 30f, textColor, FontStyles.Bold);
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 8f), new Vector2(-12f, -8f),
            "Layout preview button label");
        return button;
    }

    private static GameObject CreateImage(Transform parent, string name, Sprite sprite, Color32 color)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(item, "Create NUMBER preview UI");
        item.transform.SetParent(parent, false);
        Image image = item.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        return item;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, TMP_FontAsset font,
        float fontSize, Color color, FontStyles style)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(item, "Create NUMBER preview text");
        item.transform.SetParent(parent, false);
        TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    private static void SetPreviewText(ValidationResult result)
    {
        Record(result.GoalText, "Set NUMBER preview goal");
        Record(result.UnitText, "Set NUMBER preview unit");
        Record(result.AmountInput, "Clear NUMBER preview input");
        result.GoalText.text = "하루 2L 물 마시기";
        result.UnitText.text = "mL";
        result.AmountInput.SetTextWithoutNotify(string.Empty);
        SavePrefabOverride(result.GoalText);
        SavePrefabOverride(result.UnitText);
        SavePrefabOverride(result.AmountInput);
    }

    private static void RestoreWhenAnotherPreviewModeIsSelected()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            return;
        }

        GameObject dRoot = FindRoot(scene, PreviewRootName);
        if (dRoot == null || !dRoot.activeSelf)
        {
            return;
        }

        GameObject aRoot = FindRoot(scene, HabitItemPreviewRootName);
        GameObject canvas = FindRoot(scene, "Canvas");
        Transform addForm = canvas != null ? canvas.transform.Find("HabitTap/LifeAdd_1") : null;
        bool gameModeEnabled = UnityEngine.Object.FindObjectsOfType<HabitListManager>(true)
            .Any(component => component.gameObject.scene == scene && component.enabled) ||
            UnityEngine.Object.FindObjectsOfType<HabitUIManager>(true)
                .Any(component => component.gameObject.scene == scene && component.enabled);

        if ((aRoot != null && aRoot.activeSelf) ||
            (addForm != null && addForm.gameObject.activeSelf) || gameModeEnabled)
        {
            RestoreNumberPreviewState();
        }
    }

    public static void RestoreNumberPreviewState()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            return;
        }

        GameObject dRoot = FindRoot(scene, PreviewRootName);
        if (dRoot == null)
        {
            return;
        }

        SetActive(dRoot, false, "Restore NUMBER preview state");
        AchievedAmountOverlay overlay = UnityEngine.Object.FindObjectsOfType<AchievedAmountOverlay>(true)
            .FirstOrDefault(component => component.gameObject.scene == scene);
        if (overlay != null)
        {
            SerializedObject serialized = new SerializedObject(overlay);
            GameObject popup = serialized.FindProperty("popup")?.objectReferenceValue as GameObject;
            SetActive(popup, false, "Close NUMBER preview popup");
        }

        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void SetAllEnabledInChildren<T>(GameObject root, bool enabled) where T : Behaviour
    {
        foreach (T component in root.GetComponentsInChildren<T>(true))
        {
            if (component.enabled == enabled)
            {
                continue;
            }

            Undo.RecordObject(component, "Enable NUMBER preview component");
            component.enabled = enabled;
            EditorUtility.SetDirty(component);
            SavePrefabOverride(component);
        }
    }

    private static void DisableAll<T>(Scene scene) where T : Behaviour
    {
        foreach (T component in UnityEngine.Object.FindObjectsOfType<T>(true))
        {
            if (component.gameObject.scene != scene || !component.enabled)
            {
                continue;
            }

            Undo.RecordObject(component, "Disable game Habit component for NUMBER preview");
            component.enabled = false;
            EditorUtility.SetDirty(component);
            SavePrefabOverride(component);
        }
    }

    private static List<string> ValidateLayout(ValidationResult result)
    {
        List<string> errors = new List<string>();
        if (result.PopupRect == null)
        {
            errors.Add("Popup RectTransform이 없습니다.");
            return errors;
        }

        if (result.PopupRect.sizeDelta.x > 1000f || result.PopupRect.sizeDelta.y > 1540f)
        {
            errors.Add("Popup이 1080x1920 안전 영역을 벗어납니다.");
        }

        if (result.AmountInput != null && result.AmountInput.GetComponent<RectTransform>().rect.height < 120f)
        {
            errors.Add("수치 입력 터치 영역 높이가 120px 미만입니다.");
        }

        RectTransform cancelRect = result.CancelButton != null
            ? result.CancelButton.GetComponent<RectTransform>()
            : null;
        RectTransform confirmRect = result.ConfirmButton != null
            ? result.ConfirmButton.GetComponent<RectTransform>()
            : null;
        foreach (RectTransform buttonRect in new[] { cancelRect, confirmRect })
        {
            if (buttonRect != null && buttonRect.rect.height < 96f)
            {
                errors.Add($"{buttonRect.name} 터치 영역 높이가 96px 미만입니다.");
            }
        }

        if (cancelRect != null && confirmRect != null)
        {
            if (Mathf.Abs(cancelRect.anchoredPosition.y - confirmRect.anchoredPosition.y) > 0.1f ||
                Mathf.Abs(cancelRect.rect.height - confirmRect.rect.height) > 0.1f)
            {
                errors.Add("Cancel/Confirm 버튼의 높이 또는 수평 정렬이 서로 다릅니다.");
            }

            float cancelBottom = cancelRect.anchoredPosition.y - cancelRect.rect.height * cancelRect.pivot.y;
            float confirmBottom = confirmRect.anchoredPosition.y - confirmRect.rect.height * confirmRect.pivot.y;
            if (cancelBottom < 48f || confirmBottom < 48f)
            {
                errors.Add("Cancel/Confirm 버튼과 Popup 하단 테두리 사이 여백이 48px 미만입니다.");
            }
        }

        GameObject previewRoot = FindRoot(result.Scene, PreviewRootName);
        RectTransform panel = previewRoot != null
            ? previewRoot.transform.Find("PreviewStatusPanel") as RectTransform
            : null;
        if (panel != null)
        {
            Canvas.ForceUpdateCanvases();
            float panelBottom = panel.anchoredPosition.y - panel.rect.height * panel.pivot.y;
            if (panelBottom < 200f)
            {
                errors.Add("D Preview 패널이 하단 내비게이션 안전 여백 200px을 침범합니다.");
            }

            string[] childNames = { "Heading", "HabitName", "StatusText", "OpenButton", "CloseButton" };
            foreach (string childName in childNames)
            {
                RectTransform child = panel.Find(childName) as RectTransform;
                if (child == null || !IsContainedInParent(panel, child))
                {
                    errors.Add($"D Preview {childName}이 패널 내부에 완전히 포함되지 않습니다.");
                }
            }
        }

        return errors;
    }

    private static bool IsContainedInParent(RectTransform parent, RectTransform child)
    {
        Vector3[] corners = new Vector3[4];
        child.GetWorldCorners(corners);
        Rect bounds = parent.rect;
        const float tolerance = 0.5f;
        return corners.All(corner =>
        {
            Vector3 local = parent.InverseTransformPoint(corner);
            return local.x >= bounds.xMin - tolerance && local.x <= bounds.xMax + tolerance &&
                   local.y >= bounds.yMin - tolerance && local.y <= bounds.yMax + tolerance;
        });
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene.GetRootGameObjects().FirstOrDefault(candidate => candidate.name == name);
    }

    private static GameObject RequireObject(Transform root, string path, ValidationResult result)
    {
        Transform target = root.Find(path);
        if (target == null)
        {
            result.Errors.Add($"기존 Overlay 요소를 찾을 수 없습니다: {path}");
            return null;
        }

        return target.gameObject;
    }

    private static T Require<T>(Transform root, string path, ValidationResult result) where T : Component
    {
        GameObject target = RequireObject(root, path, result);
        T component = target != null ? target.GetComponent<T>() : null;
        if (target != null && component == null)
        {
            result.Errors.Add($"{path}에 {typeof(T).Name} 컴포넌트가 없습니다.");
        }

        return component;
    }

    private static RectTransform RequireRect(Transform root, string path)
    {
        Transform target = root.Find(path);
        if (target == null || target.GetComponent<RectTransform>() == null)
        {
            throw new InvalidOperationException($"RectTransform을 찾을 수 없습니다: {path}");
        }

        return target.GetComponent<RectTransform>();
    }

    private static T LoadAsset<T>(string path, ValidationResult result) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            result.Errors.Add($"UI Renewal 에셋을 불러올 수 없습니다: {path}");
        }

        return asset;
    }

    private static void StyleImage(Image image, Sprite sprite, Color color, Image.Type type, bool raycast)
    {
        if (image == null)
        {
            throw new InvalidOperationException("스타일 대상 Image가 없습니다.");
        }

        Record(image, "Style NUMBER overlay image");
        image.sprite = sprite;
        image.color = color;
        image.type = type;
        image.preserveAspect = false;
        image.raycastTarget = raycast;
        SavePrefabOverride(image);
    }

    private static void StyleText(TMP_Text text, TMP_FontAsset font, float minSize, float maxSize,
        Color color, FontStyles style, TextAlignmentOptions alignment)
    {
        if (text == null)
        {
            return;
        }

        Record(text, "Style NUMBER overlay text");
        if (font != null)
        {
            text.font = font;
        }
        text.color = color;
        text.fontStyle = style;
        text.alignment = alignment;
        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.margin = new Vector4(10f, 6f, 10f, 6f);
        SavePrefabOverride(text);
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 sizeOrOffsetMax, string undoName)
    {
        Record(rect, undoName);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        if (anchorMin == anchorMax)
        {
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeOrOffsetMax;
        }
        else
        {
            rect.offsetMin = anchoredPosition;
            rect.offsetMax = sizeOrOffsetMax;
        }
        SavePrefabOverride(rect);
    }

    private static void SetActive(GameObject target, bool active, string undoName)
    {
        if (target == null || target.activeSelf == active)
        {
            return;
        }

        Undo.RecordObject(target, undoName);
        target.SetActive(active);
        EditorUtility.SetDirty(target);
        SavePrefabOverride(target);
    }

    private static void Record(UnityEngine.Object target, string undoName)
    {
        Undo.RecordObject(target, undoName);
        EditorUtility.SetDirty(target);
    }

    private static void SavePrefabOverride(UnityEngine.Object target)
    {
        if (target != null && PrefabUtility.IsPartOfPrefabInstance(target))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
    }

    private sealed class ValidationResult
    {
        public Scene Scene;
        public GameObject Canvas;
        public AchievedAmountOverlay Overlay;
        public RectTransform OverlayRoot;
        public GameObject Popup;
        public RectTransform PopupRect;
        public TextMeshProUGUI GoalText;
        public TMP_InputField AmountInput;
        public TextMeshProUGUI UnitText;
        public Button CancelButton;
        public Button ConfirmButton;
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI InputText;
        public TextMeshProUGUI Placeholder;
        public TMP_FontAsset Font;
        public Sprite CardSprite;
        public Sprite CreamButtonSprite;
        public Sprite GoldButtonSprite;
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }

    private sealed class ProtectionSnapshot
    {
        private readonly int[] references;
        private readonly string inputConfiguration;
        private readonly string cancelEvents;
        private readonly string confirmEvents;
        private readonly string inputEvents;

        private ProtectionSnapshot(ValidationResult result)
        {
            SerializedObject serialized = new SerializedObject(result.Overlay);
            references = new[]
            {
                Id(serialized.FindProperty("popup")?.objectReferenceValue),
                Id(serialized.FindProperty("goalText")?.objectReferenceValue),
                Id(serialized.FindProperty("amountInput")?.objectReferenceValue),
                Id(serialized.FindProperty("unitText")?.objectReferenceValue),
                Id(serialized.FindProperty("cancelButton")?.objectReferenceValue),
                Id(serialized.FindProperty("confirmButton")?.objectReferenceValue)
            };
            inputConfiguration = result.AmountInput.contentType + "|" + result.AmountInput.characterValidation +
                                 "|" + result.AmountInput.lineType + "|" + result.AmountInput.readOnly;
            cancelEvents = EventSignature(result.CancelButton.onClick);
            confirmEvents = EventSignature(result.ConfirmButton.onClick);
            inputEvents = EventSignature(result.AmountInput.onValueChanged) + "|" +
                          EventSignature(result.AmountInput.onEndEdit) + "|" +
                          EventSignature(result.AmountInput.onSubmit);
        }

        public static ProtectionSnapshot Capture(ValidationResult result) => new ProtectionSnapshot(result);

        public void AssertUnchanged(ValidationResult result)
        {
            ProtectionSnapshot after = new ProtectionSnapshot(result);
            if (!references.SequenceEqual(after.references) || inputConfiguration != after.inputConfiguration ||
                cancelEvents != after.cancelEvents || confirmEvents != after.confirmEvents ||
                inputEvents != after.inputEvents)
            {
                throw new InvalidOperationException(
                    "보호 대상 SerializeField, 입력 설정 또는 UnityEvent가 변경되어 Apply를 중단합니다.");
            }
        }

        private static int Id(UnityEngine.Object value) => value != null ? value.GetInstanceID() : 0;

        private static string EventSignature(UnityEngine.Events.UnityEventBase value)
        {
            return string.Join(",", Enumerable.Range(0, value.GetPersistentEventCount())
                .Select(index => value.GetPersistentTarget(index)?.GetInstanceID() + ":" +
                                 value.GetPersistentMethodName(index)));
        }
    }
}
