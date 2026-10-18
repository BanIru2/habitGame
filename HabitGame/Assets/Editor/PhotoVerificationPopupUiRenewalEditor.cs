using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PhotoVerificationPopupUiRenewalEditor
{
    private const string ScenePath = "Assets/Scenes/UITestScene.unity";
    private const string CanvasPrefabPath = "Assets/Resources/UIPrefabs/Canvas.prefab";
    private const string PopupRootPath = "HabitTap/PhotoVerificationPopupRoot";
    private const string MainPopupName = "PhotoVerificationPopup";
    private const string NoticePopupName = "PhotoVerificationNoticePopup";
    private const string ResultPopupName = "PhotoVerificationResultPopup";
    private const string KoreanFontPath = "Assets/Login/Fonts/NotoSansKR Login SDF.asset";
    private const string CardSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/panel_rpg_card.png";
    private const string CreamButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_cream.png";
    private const string GoldButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_rpg.png";
    private const string PreviewRootName = "PhotoVerificationPreviewRoot";
    private const string HabitItemPreviewRootName = "HabitItemPreviewTestRoot";
    private const string NumberPreviewRootName = "NumberOverlayPreviewRoot";
    private const string ValidateMenu = "Tools/HabitPVP/UI Renewal/Validate PHOTO Verification Popup";
    private const string ApplyMenu = "Tools/HabitPVP/UI Renewal/Apply PHOTO Verification Popup";
    private const string PreviewMenu =
        "Tools/HabitPVP/UI Renewal/Preview Mode/E. PHOTO Verification Popup Renewal";

    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 CreamLight = new Color32(0xFF, 0xF9, 0xEA, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x86, 0x68, 0x5D, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);
    private static readonly Color32 Backdrop = new Color32(0x3A, 0x29, 0x2A, 0xFF);
    private static readonly Color32 RewardBrown = new Color32(0x8A, 0x55, 0x16, 0xFF);
    private static readonly Color32 Disabled = new Color32(0xB9, 0xAA, 0x91, 0xB8);

    static PhotoVerificationPopupUiRenewalEditor()
    {
        EditorApplication.update -= RestoreWhenAnotherPreviewModeIsSelected;
        EditorApplication.update += RestoreWhenAnotherPreviewModeIsSelected;
        EditorSceneManager.sceneSaving -= RestoreBeforeSceneSave;
        EditorSceneManager.sceneSaving += RestoreBeforeSceneSave;
    }

    [MenuItem(ValidateMenu, false, 2060)]
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
                Debug.LogError("[HabitPVP PHOTO Popup] " + error, result.PopupRoot);
            }

            return;
        }

        Debug.Log(
            "[HabitPVP PHOTO Popup] 검증 성공. Canvas Prefab 출처, PhotoVerificationManager 14개 참조, " +
            "4개 Button 이벤트, 1080x1920 레이아웃, E Preview 로컬 분리가 정상입니다.",
            result.PopupRoot);
    }

    [MenuItem(ApplyMenu, false, 2061)]
    private static void Apply()
    {
        ValidationResult result = ValidateScene(true);
        if (!result.IsValid)
        {
            Debug.LogError("[HabitPVP PHOTO Popup] Apply 중단: 구조 검증 오류를 먼저 해결해주세요.");
            return;
        }

        ProtectionSnapshot snapshot = ProtectionSnapshot.Capture(result);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply PHOTO Verification Popup UI Renewal");

        try
        {
            ApplyStyle(result);
            EnsurePreviewRoot(result);
            snapshot.AssertUnchanged(result);

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

            Selection.activeGameObject = result.PopupRoot;
            Debug.Log(
                "[HabitPVP PHOTO Popup] 적용 성공. UITestScene의 Canvas Prefab Instance Override와 " +
                "E Preview 전용 루트만 저장했습니다. Canvas.prefab, MainScene, PhotoVerificationManager, " +
                "HabitItem 및 API 코드는 변경하지 않았습니다.",
                result.PopupRoot);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            EditorSceneManager.MarkSceneDirty(result.Scene);
            Debug.LogException(exception);
            Debug.LogError("[HabitPVP PHOTO Popup] Apply 실패. 이번 실행의 변경을 Undo로 되돌렸습니다.");
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
            errors.Add("E Preview 루트가 생성되지 않았습니다.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join("\n", errors));
        }
    }

    [MenuItem(PreviewMenu, false, 2062)]
    private static void ActivatePreview()
    {
        PvpRankingUiRenewalEditor.RestorePvpPreviewState();
        ShopGachaUiRenewalEditor.RestoreShopPreviewState();
        InventoryMainUiRenewalEditor.RestoreInventoryPreviewState();
        CharacterMainUiRenewalEditor.RestoreCharacterPreviewState();
        RestorePhotoPreviewState();
        AchievedAmountOverlayUiRenewalEditor.RestoreNumberPreviewState();
        ValidationResult result = ValidateScene(true);
        if (!result.IsValid)
        {
            return;
        }

        GameObject previewRoot = FindRoot(result.Scene, PreviewRootName);
        if (previewRoot == null)
        {
            Debug.LogError(
                "[HabitPVP PHOTO Popup] E Preview 루트가 없습니다. " +
                "Apply PHOTO Verification Popup을 먼저 실행해주세요.");
            return;
        }

        HabitGoalFormUiRenewalEditor.RestoreHabitMainPreviewState();
        HabitGoalFormUiRenewalEditor.EnsureHabitCanvasHierarchyActive(result.Scene);
        SetActive(FindRoot(result.Scene, HabitItemPreviewRootName), false, "Hide HabitItem preview for E mode");
        SetActive(FindRoot(result.Scene, NumberPreviewRootName), false, "Hide NUMBER preview for E mode");

        PhotoVerificationPopupPreviewController previewController =
            previewRoot.GetComponent<PhotoVerificationPopupPreviewController>();
        SetEnabled(previewController, true, "Enable PHOTO preview controller");
        IsolateSceneObjects(result.Scene, result.Canvas, result.PopupRoot, previewRoot, previewController,
            result.PhotoPopup, result.NoticePopup, result.ResultPopup);
        DisableRuntimeGameScripts(result.Scene, previewController);
        SetActive(previewRoot, true, "Activate PHOTO preview controls");
        SetActive(result.PopupRoot, true, "Activate PHOTO popup preview");
        SetActive(result.PhotoPopup, true, "Show PHOTO input popup");
        SetActive(result.NoticePopup, false, "Hide PHOTO notice popup");
        SetActive(result.ResultPopup, false, "Hide PHOTO result popup");
        result.VerifyButton.interactable = false;
        SavePrefabOverride(result.VerifyButton);

        EditorSceneManager.MarkSceneDirty(result.Scene);
        Selection.activeGameObject = result.PhotoPopup;
        Debug.Log(
            "[HabitPVP PHOTO Popup] E 모드: 실제 PHOTO Hierarchy를 가상 테스트 데이터로 표시했습니다. " +
            "Play Mode에서 사진 선택/로딩/성공/실패/초기화를 확인할 수 있으며 NativeCamera, " +
            "HabitItem, JWT 및 API Service를 호출하지 않습니다. A/B/C/D 메뉴 선택 시 E 상태는 복원됩니다.",
            result.PhotoPopup);
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

        Transform popupRoot = result.Canvas.transform.Find(PopupRootPath);
        if (popupRoot == null)
        {
            result.Errors.Add($"대상 경로를 찾을 수 없습니다: Canvas/{PopupRootPath}");
            return FinishValidation(result, log);
        }

        result.PopupRoot = popupRoot.gameObject;
        result.Manager = popupRoot.GetComponent<PhotoVerificationManager>();
        if (result.Manager == null)
        {
            result.Errors.Add("PhotoVerificationPopupRoot에 PhotoVerificationManager가 없습니다.");
            return FinishValidation(result, log);
        }

        SerializedObject manager = new SerializedObject(result.Manager);
        result.PhotoPopup = ReadReference<GameObject>(manager, "photoVerificationPopup", result.Errors);
        result.PhotoView = ReadReference<RawImage>(manager, "photoView", result.Errors);
        result.PhotoButton = ReadReference<Button>(manager, "photoButton", result.Errors);
        result.VerifyButton = ReadReference<Button>(manager, "verifyButton", result.Errors);
        result.CancelButton = ReadReference<Button>(manager, "cancelButton", result.Errors);
        result.NoticePopup = ReadReference<GameObject>(manager, "noticePopup", result.Errors);
        result.NoticeText = ReadReference<TextMeshProUGUI>(manager, "noticeText", result.Errors);
        result.ResultPopup = ReadReference<GameObject>(manager, "resultPopup", result.Errors);
        result.ResultText = ReadReference<TextMeshProUGUI>(manager, "resultText", result.Errors);
        result.ReasonText = ReadReference<TextMeshProUGUI>(manager, "reasonText", result.Errors);
        result.DetectedActionText = ReadReference<TextMeshProUGUI>(manager, "detectedActionText", result.Errors);
        result.AttrText = ReadReference<TextMeshProUGUI>(manager, "attrText", result.Errors);
        result.ExpText = ReadReference<TextMeshProUGUI>(manager, "expText", result.Errors);
        result.StreakText = ReadReference<TextMeshProUGUI>(manager, "streakText", result.Errors);
        result.ResultConfirmButton = ReadReference<Button>(manager, "checkVerificationResultButton", result.Errors);

        foreach (UnityEngine.Object reference in result.AllReferences())
        {
            Component component = reference as Component;
            GameObject gameObject = reference as GameObject;
            Transform target = component != null ? component.transform : gameObject != null ? gameObject.transform : null;
            if (target != null && !target.IsChildOf(popupRoot))
            {
                result.Errors.Add($"Manager 참조가 PHOTO 루트 밖을 가리킵니다: {reference.name}");
            }
        }

        result.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        result.CardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardSpritePath);
        result.CreamButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CreamButtonSpritePath);
        result.GoldButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GoldButtonSpritePath);
        if (result.Font == null) result.Errors.Add("NotoSansKR TMP Font Asset을 찾을 수 없습니다.");
        if (result.CardSprite == null) result.Errors.Add("RPG 카드 Sprite를 찾을 수 없습니다.");
        if (result.CreamButtonSprite == null) result.Errors.Add("크림 Button Sprite를 찾을 수 없습니다.");
        if (result.GoldButtonSprite == null) result.Errors.Add("골드 Button Sprite를 찾을 수 없습니다.");

        return FinishValidation(result, log);
    }

    private static ValidationResult FinishValidation(ValidationResult result, bool log)
    {
        if (log && !result.IsValid)
        {
            foreach (string error in result.Errors)
            {
                Debug.LogError("[HabitPVP PHOTO Popup] " + error, result.PopupRoot);
            }
        }

        return result;
    }

    private static T ReadReference<T>(SerializedObject owner, string propertyName, ICollection<string> errors)
        where T : UnityEngine.Object
    {
        SerializedProperty property = owner.FindProperty(propertyName);
        T value = property != null ? property.objectReferenceValue as T : null;
        if (value == null)
        {
            errors.Add($"PhotoVerificationManager.{propertyName} 참조가 비어 있거나 타입이 다릅니다.");
        }

        return value;
    }

    private static void ApplyStyle(ValidationResult result)
    {
        RectTransform rootRect = result.PopupRoot.GetComponent<RectTransform>();
        SetRect(rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, "Stretch PHOTO popup root");
        Image backdrop = GetOrAdd<Image>(result.PopupRoot);
        Record(backdrop, "Style PHOTO backdrop");
        backdrop.sprite = null;
        backdrop.type = Image.Type.Simple;
        backdrop.color = Backdrop;
        backdrop.raycastTarget = true;
        SavePrefabOverride(backdrop);

        StyleMainPopup(result);
        StyleNoticePopup(result);
        StyleResultPopup(result);
    }

    private static void StyleMainPopup(ValidationResult result)
    {
        Image panel = GetOrAdd<Image>(result.PhotoPopup);
        StylePanel(panel, result.CardSprite);
        SetCenteredRect(result.PhotoPopup.GetComponent<RectTransform>(), new Vector2(940f, 1360f),
            new Vector2(0f, -105f), "Layout PHOTO input panel");

        EnsurePresentationalText(result.PhotoPopup.transform, "RenewalTitle", "사진으로 습관 인증",
            result.Font, 54f, Brown, FontStyles.Bold, new Vector2(0f, 560f), new Vector2(820f, 90f));
        EnsurePresentationalText(result.PhotoPopup.transform, "RenewalDescription",
            "사진을 선택한 뒤 인증 버튼을 눌러주세요.", result.Font, 31f, MutedBrown,
            FontStyles.Normal, new Vector2(0f, 490f), new Vector2(820f, 64f));

        RectTransform photoRect = result.PhotoView.GetComponent<RectTransform>();
        SetCenteredRect(photoRect, new Vector2(780f, 780f), new Vector2(0f, 65f),
            "Layout PHOTO preview area");
        Record(result.PhotoView, "Style PHOTO preview area");
        result.PhotoView.color = Color.white;
        result.PhotoView.raycastTarget = false;
        SavePrefabOverride(result.PhotoView);
        Outline outline = GetOrAdd<Outline>(result.PhotoView.gameObject);
        Record(outline, "Style PHOTO preview border");
        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(5f, -5f);
        outline.useGraphicAlpha = false;
        SavePrefabOverride(outline);

        StyleButton(result.PhotoButton, result.CreamButtonSprite, Cream, Brown, result.Font);
        StyleButton(result.VerifyButton, result.GoldButtonSprite, Gold, Color.white, result.Font);
        StyleButton(result.CancelButton, result.CreamButtonSprite, Cream, Brown, result.Font);
        SetCenteredRect(result.PhotoButton.GetComponent<RectTransform>(), new Vector2(264f, 120f),
            new Vector2(-290f, -555f), "Layout PHOTO select button");
        SetCenteredRect(result.VerifyButton.GetComponent<RectTransform>(), new Vector2(264f, 120f),
            new Vector2(0f, -555f), "Layout PHOTO verify button");
        SetCenteredRect(result.CancelButton.GetComponent<RectTransform>(), new Vector2(264f, 120f),
            new Vector2(290f, -555f), "Layout PHOTO cancel button");
    }

    private static void StyleNoticePopup(ValidationResult result)
    {
        Image panel = GetOrAdd<Image>(result.NoticePopup);
        StylePanel(panel, result.CardSprite);
        SetCenteredRect(result.NoticePopup.GetComponent<RectTransform>(), new Vector2(800f, 340f),
            new Vector2(0f, 30f), "Layout PHOTO loading panel");
        StyleText(result.NoticeText, result.Font, 34f, 42f, Brown, FontStyles.Bold,
            TextAlignmentOptions.Center);
        SetStretchRect(result.NoticeText.rectTransform, new Vector2(72f, 64f), new Vector2(-72f, -64f),
            "Layout PHOTO loading text");
    }

    private static void StyleResultPopup(ValidationResult result)
    {
        Image panel = GetOrAdd<Image>(result.ResultPopup);
        StylePanel(panel, result.CardSprite);
        SetCenteredRect(result.ResultPopup.GetComponent<RectTransform>(), new Vector2(900f, 1340f),
            new Vector2(0f, -100f), "Layout PHOTO result panel");

        StyleAndPlace(result.ResultText, result.Font, 54f, 64f, Brown, FontStyles.Bold,
            new Vector2(0f, 520f), new Vector2(780f, 180f));
        StyleAndPlace(result.ReasonText, result.Font, 34f, 44f, Brown, FontStyles.Normal,
            new Vector2(0f, 342f), new Vector2(760f, 150f));
        StyleAndPlace(result.DetectedActionText, result.Font, 38f, 48f, Brown, FontStyles.Bold,
            new Vector2(0f, 172f), new Vector2(760f, 150f));
        StyleAndPlace(result.AttrText, result.Font, 36f, 46f, RewardBrown, FontStyles.Bold,
            new Vector2(0f, 12f), new Vector2(760f, 100f));
        StyleAndPlace(result.ExpText, result.Font, 42f, 52f, RewardBrown, FontStyles.Bold,
            new Vector2(0f, -112f), new Vector2(760f, 110f));
        StyleAndPlace(result.StreakText, result.Font, 36f, 46f, Brown, FontStyles.Bold,
            new Vector2(0f, -242f), new Vector2(760f, 100f));

        StyleButton(result.ResultConfirmButton, result.GoldButtonSprite, Gold, Color.white, result.Font);
        SetCenteredRect(result.ResultConfirmButton.GetComponent<RectTransform>(), new Vector2(520f, 120f),
            new Vector2(0f, -535f), "Layout PHOTO result confirm button");
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
        Undo.RegisterCreatedObjectUndo(root, "Create PHOTO preview root");
        SceneManager.MoveGameObjectToScene(root, result.Scene);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1100;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 1f;

        GameObject panel = CreateImage(root.transform, "PreviewStatusPanel", result.CardSprite, Cream);
        SetCenteredRect(panel.GetComponent<RectTransform>(), new Vector2(1000f, 278f),
            new Vector2(0f, 795f), "Layout PHOTO preview controls");
        TMP_Text heading = CreateText(panel.transform, "Heading",
            "E. PHOTO Verification Popup Renewal · LOCAL PREVIEW", result.Font, 30f, Brown,
            FontStyles.Bold);
        SetRect(heading.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(32f, -64f), new Vector2(-32f, -16f), "Layout PHOTO preview heading");
        TMP_Text status = CreateText(panel.transform, "StatusText",
            "[가상 테스트 데이터] 초기 상태 · 실제 카메라/API 호출 없음", result.Font, 25f,
            MutedBrown, FontStyles.Bold);
        SetRect(status.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(32f, -112f), new Vector2(-32f, -68f), "Layout PHOTO preview status");

        string[] labels = { "사진 선택", "로딩", "성공", "실패", "초기화" };
        string[] names = { "SelectStateButton", "LoadingStateButton", "SuccessStateButton",
            "FailureStateButton", "ResetStateButton" };
        Button[] controls = new Button[labels.Length];
        for (int index = 0; index < labels.Length; index++)
        {
            controls[index] = CreatePreviewButton(panel.transform, names[index], labels[index], result.Font,
                index == 2 ? result.GoldButtonSprite : result.CreamButtonSprite,
                index == 2 ? Gold : CreamLight, index == 2 ? Color.white : Brown);
            float x = -388f + index * 194f;
            SetCenteredRect(controls[index].GetComponent<RectTransform>(), new Vector2(178f, 96f),
                new Vector2(x, -72f), "Layout PHOTO preview state button");
        }

        PhotoVerificationPopupPreviewController controller =
            Undo.AddComponent<PhotoVerificationPopupPreviewController>(root);
        SerializedObject serialized = new SerializedObject(controller);
        Assign(serialized, "popupRoot", result.PopupRoot);
        Assign(serialized, "photoPopup", result.PhotoPopup);
        Assign(serialized, "photoView", result.PhotoView);
        Assign(serialized, "photoButton", result.PhotoButton);
        Assign(serialized, "verifyButton", result.VerifyButton);
        Assign(serialized, "cancelButton", result.CancelButton);
        Assign(serialized, "noticePopup", result.NoticePopup);
        Assign(serialized, "noticeText", result.NoticeText);
        Assign(serialized, "resultPopup", result.ResultPopup);
        Assign(serialized, "resultText", result.ResultText);
        Assign(serialized, "reasonText", result.ReasonText);
        Assign(serialized, "detectedActionText", result.DetectedActionText);
        Assign(serialized, "attrText", result.AttrText);
        Assign(serialized, "expText", result.ExpText);
        Assign(serialized, "streakText", result.StreakText);
        Assign(serialized, "resultConfirmButton", result.ResultConfirmButton);
        Assign(serialized, "selectStateButton", controls[0]);
        Assign(serialized, "loadingStateButton", controls[1]);
        Assign(serialized, "successStateButton", controls[2]);
        Assign(serialized, "failureStateButton", controls[3]);
        Assign(serialized, "resetStateButton", controls[4]);
        Assign(serialized, "previewStatusText", status);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        root.SetActive(false);
    }

    private static void Assign(SerializedObject serialized, string propertyName, UnityEngine.Object value)
    {
        serialized.FindProperty(propertyName).objectReferenceValue = value;
    }

    private static void StylePanel(Image image, Sprite sprite)
    {
        Record(image, "Style PHOTO RPG panel");
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = Cream;
        image.raycastTarget = true;
        SavePrefabOverride(image);
    }

    private static void StyleButton(Button button, Sprite sprite, Color32 color, Color textColor,
        TMP_FontAsset font)
    {
        Image image = button.targetGraphic as Image ?? button.GetComponent<Image>();
        Record(image, "Style PHOTO button image");
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        SavePrefabOverride(image);

        Record(button, "Style PHOTO button states");
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
            StyleText(label, font, 30f, 38f, textColor, FontStyles.Bold, TextAlignmentOptions.Center);
        }
    }

    private static void StyleAndPlace(TMP_Text text, TMP_FontAsset font, float size, float lineSpacing,
        Color color, FontStyles style, Vector2 position, Vector2 dimensions)
    {
        StyleText(text, font, size, lineSpacing, color, style, TextAlignmentOptions.Center);
        SetCenteredRect(text.rectTransform, dimensions, position, "Layout PHOTO result text");
    }

    private static void StyleText(TMP_Text text, TMP_FontAsset font, float size, float lineSpacing,
        Color color, FontStyles style, TextAlignmentOptions alignment)
    {
        Record(text, "Style PHOTO text");
        text.font = font;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(22f, size - 10f);
        text.fontSizeMax = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.lineSpacing = lineSpacing - size;
        text.margin = new Vector4(14f, 8f, 14f, 8f);
        text.raycastTarget = false;
        SavePrefabOverride(text);
    }

    private static TMP_Text EnsurePresentationalText(Transform parent, string name, string value,
        TMP_FontAsset font, float size, Color color, FontStyles style, Vector2 position, Vector2 dimensions)
    {
        Transform existing = parent.Find(name);
        TextMeshProUGUI text;
        if (existing == null)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(item, "Create PHOTO presentation text");
            item.transform.SetParent(parent, false);
            text = item.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            text = existing.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = Undo.AddComponent<TextMeshProUGUI>(existing.gameObject);
            }
        }

        Record(text, "Update PHOTO presentation text");
        text.text = value;
        StyleText(text, font, size, size + 8f, color, style, TextAlignmentOptions.Center);
        SetCenteredRect(text.rectTransform, dimensions, position, "Layout PHOTO presentation text");
        return text;
    }

    private static Button CreatePreviewButton(Transform parent, string name, string label, TMP_FontAsset font,
        Sprite sprite, Color32 color, Color textColor)
    {
        GameObject buttonObject = CreateImage(parent, name, sprite, color);
        Button button = Undo.AddComponent<Button>(buttonObject);
        button.targetGraphic = buttonObject.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(0xFF, 0xF4, 0xD2, 0xFF);
        colors.pressedColor = new Color32(0xD5, 0xA1, 0x55, 0xFF);
        colors.disabledColor = Disabled;
        button.colors = colors;
        TMP_Text text = CreateText(buttonObject.transform, "Label", label, font, 26f, textColor,
            FontStyles.Bold);
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 6f),
            new Vector2(-10f, -6f), "Layout PHOTO preview button label");
        return button;
    }

    private static GameObject CreateImage(Transform parent, string name, Sprite sprite, Color32 color)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(item, "Create PHOTO preview UI");
        item.transform.SetParent(parent, false);
        Image image = item.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        return item;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, TMP_FontAsset font,
        float size, Color color, FontStyles style)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(item, "Create PHOTO preview text");
        item.transform.SetParent(parent, false);
        TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
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

        GameObject eRoot = FindRoot(scene, PreviewRootName);
        if (eRoot == null || !eRoot.activeSelf)
        {
            return;
        }

        GameObject aRoot = FindRoot(scene, HabitItemPreviewRootName);
        GameObject dRoot = FindRoot(scene, NumberPreviewRootName);
        GameObject canvas = FindRoot(scene, "Canvas");
        Transform habitTap = canvas != null ? canvas.transform.Find("HabitTap") : null;
        Transform addForm = habitTap != null ? habitTap.Find("LifeAdd_1") : null;
        Transform life = habitTap != null ? habitTap.Find("Life") : null;
        bool gameModeEnabled = UnityEngine.Object.FindObjectsOfType<HabitListManager>(true)
            .Any(component => component.gameObject.scene == scene && component.enabled) ||
            UnityEngine.Object.FindObjectsOfType<HabitUIManager>(true)
                .Any(component => component.gameObject.scene == scene && component.enabled);

        if ((aRoot != null && aRoot.activeSelf) || (dRoot != null && dRoot.activeSelf) ||
            (addForm != null && addForm.gameObject.activeSelf) ||
            (life != null && life.gameObject.activeSelf) || gameModeEnabled)
        {
            RestorePhotoPreviewState();
        }
    }

    public static void RestorePhotoPreviewState()
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

        GameObject eRoot = FindRoot(scene, PreviewRootName);
        if (eRoot == null)
        {
            return;
        }

        PhotoVerificationPopupPreviewController previewController =
            eRoot.GetComponent<PhotoVerificationPopupPreviewController>();
        bool hadSnapshot = HasIsolationSnapshot(previewController);
        if (!eRoot.activeSelf && !hadSnapshot)
        {
            return;
        }

        RestoreRuntimeGameScripts(previewController);
        RestoreSceneObjects(previewController);
        ClosePhotoPopupHierarchy(scene);
        ClearIsolationSnapshot(previewController);
        SetActive(eRoot, false, "Restore PHOTO preview state");
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void ClosePhotoPopupHierarchy(Scene scene)
    {
        GameObject canvas = FindRoot(scene, "Canvas");
        Transform popupRoot = canvas != null ? canvas.transform.Find(PopupRootPath) : null;
        if (popupRoot == null)
        {
            return;
        }

        SetActive(popupRoot.Find(MainPopupName)?.gameObject, false, "Close PHOTO input popup");
        SetActive(popupRoot.Find(NoticePopupName)?.gameObject, false, "Close PHOTO notice popup");
        SetActive(popupRoot.Find(ResultPopupName)?.gameObject, false, "Close PHOTO result popup");
        SetActive(popupRoot.gameObject, false, "Close PHOTO popup root");
    }

    private static void RestoreBeforeSceneSave(Scene scene, string path)
    {
        if (scene.path == ScenePath && scene == SceneManager.GetActiveScene())
        {
            RestorePhotoPreviewState();
        }
    }

    private static void IsolateSceneObjects(Scene scene, GameObject canvas, GameObject popupRoot,
        GameObject previewRoot, PhotoVerificationPopupPreviewController previewController,
        params GameObject[] previewStateObjects)
    {
        if (previewController == null)
        {
            throw new InvalidOperationException("E Preview Controller를 찾을 수 없습니다.");
        }

        HashSet<GameObject> isolated = new HashSet<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root == canvas || root == previewRoot || root.GetComponent<EventSystem>() != null ||
                root.GetComponentInChildren<Camera>(true) != null ||
                root.GetComponentInChildren<AudioListener>(true) != null ||
                !root.activeSelf)
            {
                continue;
            }

            isolated.Add(root);
        }

        Transform habitTap = canvas != null ? canvas.transform.Find("HabitTap") : null;
        if (canvas != null)
        {
            foreach (Transform child in canvas.transform)
            {
                if (child != habitTap && child.gameObject.activeSelf &&
                    child.GetComponentInChildren<Camera>(true) == null &&
                    child.GetComponentInChildren<AudioListener>(true) == null)
                {
                    isolated.Add(child.gameObject);
                }
            }
        }

        if (habitTap != null)
        {
            foreach (Transform child in habitTap)
            {
                if (child.gameObject != popupRoot && child.gameObject.activeSelf &&
                    child.GetComponentInChildren<Camera>(true) == null &&
                    child.GetComponentInChildren<AudioListener>(true) == null)
                {
                    isolated.Add(child.gameObject);
                }
            }
        }

        SerializedObject serialized = new SerializedObject(previewController);
        SerializedProperty property = serialized.FindProperty("isolatedGameObjects");
        SerializedProperty stateProperty = serialized.FindProperty("isolatedGameObjectActiveStates");
        HashSet<GameObject> snapshotTargets = new HashSet<GameObject>(isolated);
        snapshotTargets.Add(popupRoot);
        foreach (GameObject target in previewStateObjects)
        {
            snapshotTargets.Add(target);
        }

        GameObject[] ordered = snapshotTargets
            .Where(target => target != null && target.scene == scene)
            .OrderBy(target => target.GetInstanceID())
            .ToArray();
        property.arraySize = ordered.Length;
        stateProperty.arraySize = ordered.Length;
        for (int index = 0; index < ordered.Length; index++)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue = ordered[index];
            stateProperty.GetArrayElementAtIndex(index).boolValue = ordered[index].activeSelf;
        }

        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(previewController);

        foreach (GameObject target in isolated)
        {
            SetActive(target, false, "Isolate scene object for PHOTO preview");
        }
    }

    private static void RestoreSceneObjects(PhotoVerificationPopupPreviewController previewController)
    {
        if (previewController == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(previewController);
        SerializedProperty property = serialized.FindProperty("isolatedGameObjects");
        SerializedProperty stateProperty = serialized.FindProperty("isolatedGameObjectActiveStates");
        if (property == null || stateProperty == null || property.arraySize != stateProperty.arraySize)
        {
            return;
        }

        for (int index = 0; index < property.arraySize; index++)
        {
            GameObject target = property.GetArrayElementAtIndex(index).objectReferenceValue as GameObject;
            if (target == null)
            {
                continue;
            }

            target.SetActive(stateProperty.GetArrayElementAtIndex(index).boolValue);
            EditorUtility.SetDirty(target);
            SavePrefabOverride(target);
        }
    }

    private static void DisableRuntimeGameScripts(
        Scene scene, PhotoVerificationPopupPreviewController previewController)
    {
        if (previewController == null)
        {
            throw new InvalidOperationException("E Preview Controller를 찾을 수 없습니다.");
        }

        string runtimeAssemblyName = typeof(PhotoVerificationManager).Assembly.GetName().Name;
        List<Behaviour> isolated = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true)
            .Where(component => component != null && component.gameObject.scene == scene &&
                                component != previewController &&
                                component.GetComponentInChildren<Camera>(true) == null &&
                                component.GetComponentInChildren<AudioListener>(true) == null &&
                                component.GetType().Assembly.GetName().Name == runtimeAssemblyName)
            .Cast<Behaviour>()
            .ToList();
        bool[] enabledStates = isolated.Select(component => component.enabled).ToArray();

        foreach (Behaviour component in isolated)
        {
            if (component.enabled)
            {
                Record(component, "Isolate runtime script for PHOTO preview");
                component.enabled = false;
                SavePrefabOverride(component);
            }
        }

        SerializedObject serialized = new SerializedObject(previewController);
        SerializedProperty property = serialized.FindProperty("isolatedBehaviours");
        SerializedProperty stateProperty = serialized.FindProperty("isolatedBehaviourEnabledStates");
        property.arraySize = isolated.Count;
        stateProperty.arraySize = isolated.Count;
        for (int index = 0; index < isolated.Count; index++)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue = isolated[index];
            stateProperty.GetArrayElementAtIndex(index).boolValue = enabledStates[index];
        }

        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(previewController);
    }

    private static void RestoreRuntimeGameScripts(PhotoVerificationPopupPreviewController previewController)
    {
        if (previewController == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(previewController);
        SerializedProperty property = serialized.FindProperty("isolatedBehaviours");
        SerializedProperty stateProperty = serialized.FindProperty("isolatedBehaviourEnabledStates");
        if (property == null || stateProperty == null || property.arraySize != stateProperty.arraySize)
        {
            return;
        }

        for (int index = 0; index < property.arraySize; index++)
        {
            Behaviour component = property.GetArrayElementAtIndex(index).objectReferenceValue as Behaviour;
            if (component == null)
            {
                continue;
            }

            component.enabled = stateProperty.GetArrayElementAtIndex(index).boolValue;
            EditorUtility.SetDirty(component);
            SavePrefabOverride(component);
        }
    }

    private static bool HasIsolationSnapshot(PhotoVerificationPopupPreviewController previewController)
    {
        if (previewController == null)
        {
            return false;
        }

        SerializedObject serialized = new SerializedObject(previewController);
        SerializedProperty behaviours = serialized.FindProperty("isolatedBehaviours");
        SerializedProperty gameObjects = serialized.FindProperty("isolatedGameObjects");
        return (behaviours != null && behaviours.arraySize > 0) ||
               (gameObjects != null && gameObjects.arraySize > 0);
    }

    private static void ClearIsolationSnapshot(PhotoVerificationPopupPreviewController previewController)
    {
        if (previewController == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(previewController);
        string[] propertyNames =
        {
            "isolatedBehaviours", "isolatedBehaviourEnabledStates",
            "isolatedGameObjects", "isolatedGameObjectActiveStates"
        };
        foreach (string propertyName in propertyNames)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.arraySize = 0;
            }
        }

        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(previewController);
    }

    private static void SetHabitPanelChildren(Transform habitTap, bool active)
    {
        if (habitTap == null)
        {
            return;
        }

        string[] names =
        {
            "TopTab", "Life", "Spend", "LifeAdd_1", "SpendAdd_1", "BudgetSetting", "SpendHistory",
            "WeeklyReportPanel", "HabitDetail", "AchievedAmountOverlay"
        };
        foreach (string name in names)
        {
            Transform child = habitTap.Find(name);
            if (child != null)
            {
                SetActive(child.gameObject, active, "Switch to PHOTO preview mode");
            }
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

            Record(component, "Disable game component for PHOTO preview");
            component.enabled = false;
            SavePrefabOverride(component);
        }
    }

    private static List<string> ValidateLayout(ValidationResult result)
    {
        List<string> errors = new List<string>();
        if (!result.IsValid)
        {
            errors.AddRange(result.Errors);
            return errors;
        }

        ValidatePanelBounds(result.PhotoPopup, 1000f, 1500f, "입력 Popup", errors);
        ValidatePanelBounds(result.NoticePopup, 900f, 500f, "로딩 Popup", errors);
        ValidatePanelBounds(result.ResultPopup, 1000f, 1500f, "결과 Popup", errors);
        ValidateTouchTarget(result.PhotoButton, errors);
        ValidateTouchTarget(result.VerifyButton, errors);
        ValidateTouchTarget(result.CancelButton, errors);
        ValidateTouchTarget(result.ResultConfirmButton, errors);

        RectTransform[] inputButtons =
        {
            result.PhotoButton.GetComponent<RectTransform>(),
            result.VerifyButton.GetComponent<RectTransform>(),
            result.CancelButton.GetComponent<RectTransform>()
        };
        for (int left = 0; left < inputButtons.Length; left++)
        {
            for (int right = left + 1; right < inputButtons.Length; right++)
            {
                if (LocalRect(inputButtons[left]).Overlaps(LocalRect(inputButtons[right])))
                {
                    errors.Add($"입력 버튼이 겹칩니다: {inputButtons[left].name}, {inputButtons[right].name}");
                }
            }
        }

        GameObject previewRoot = FindRoot(result.Scene, PreviewRootName);
        RectTransform panel = previewRoot != null
            ? previewRoot.transform.Find("PreviewStatusPanel") as RectTransform
            : null;
        if (panel != null)
        {
            float top = panel.anchoredPosition.y + panel.rect.height * (1f - panel.pivot.y);
            if (top > 960f || top - panel.rect.height < 650f)
            {
                errors.Add("E Preview 컨트롤이 1080x1920 상단 안전 영역을 벗어납니다.");
            }

            foreach (RectTransform button in panel.GetComponentsInChildren<Button>(true)
                         .Select(item => item.GetComponent<RectTransform>()))
            {
                if (button.rect.height < 88f || !IsContainedInParent(panel, button))
                {
                    errors.Add($"E Preview 버튼 크기/경계가 올바르지 않습니다: {button.name}");
                }
            }
        }

        return errors;
    }

    private static void ValidatePanelBounds(GameObject panel, float maxWidth, float maxHeight, string label,
        ICollection<string> errors)
    {
        RectTransform rect = panel != null ? panel.GetComponent<RectTransform>() : null;
        if (rect == null || rect.rect.width > maxWidth || rect.rect.height > maxHeight)
        {
            errors.Add($"{label}이 1080x1920 안전 영역을 벗어나거나 RectTransform이 없습니다.");
        }
    }

    private static void ValidateTouchTarget(Button button, ICollection<string> errors)
    {
        RectTransform rect = button != null ? button.GetComponent<RectTransform>() : null;
        if (rect == null || rect.rect.width < 160f || rect.rect.height < 96f)
        {
            errors.Add($"버튼 터치 영역이 160x96px 미만입니다: {(button != null ? button.name : "null")}");
        }
    }

    private static Rect LocalRect(RectTransform rect)
    {
        Vector2 size = rect.rect.size;
        Vector2 position = rect.anchoredPosition;
        return new Rect(position.x - size.x * rect.pivot.x, position.y - size.y * rect.pivot.y,
            size.x, size.y);
    }

    private static bool IsContainedInParent(RectTransform parent, RectTransform child)
    {
        Vector3[] parentCorners = new Vector3[4];
        Vector3[] childCorners = new Vector3[4];
        parent.GetWorldCorners(parentCorners);
        child.GetWorldCorners(childCorners);
        return childCorners.All(corner => corner.x >= parentCorners[0].x - 0.5f &&
                                          corner.x <= parentCorners[2].x + 0.5f &&
                                          corner.y >= parentCorners[0].y - 0.5f &&
                                          corner.y <= parentCorners[2].y + 0.5f);
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene.IsValid() ? scene.GetRootGameObjects().FirstOrDefault(item => item.name == name) : null;
    }

    private static void SetActive(GameObject target, bool active, string label)
    {
        if (target == null || target.activeSelf == active)
        {
            return;
        }

        Undo.RecordObject(target, label);
        target.SetActive(active);
        EditorUtility.SetDirty(target);
        SavePrefabOverride(target);
    }

    private static void SetEnabled(Behaviour target, bool enabled, string label)
    {
        if (target == null || target.enabled == enabled)
        {
            return;
        }

        Record(target, label);
        target.enabled = enabled;
        SavePrefabOverride(target);
    }

    private static void SetCenteredRect(RectTransform rect, Vector2 size, Vector2 position, string label)
    {
        SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size, label);
    }

    private static void SetStretchRect(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax, string label)
    {
        SetRect(rect, Vector2.zero, Vector2.one, offsetMin, offsetMax, label);
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 valueA,
        Vector2 valueB, string label)
    {
        Record(rect, label);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        if (anchorMin == anchorMax)
        {
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = valueA;
            rect.sizeDelta = valueB;
        }
        else
        {
            rect.offsetMin = valueA;
            rect.offsetMax = valueB;
        }

        SavePrefabOverride(rect);
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
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
        public GameObject PopupRoot;
        public PhotoVerificationManager Manager;
        public GameObject PhotoPopup;
        public RawImage PhotoView;
        public Button PhotoButton;
        public Button VerifyButton;
        public Button CancelButton;
        public GameObject NoticePopup;
        public TextMeshProUGUI NoticeText;
        public GameObject ResultPopup;
        public TextMeshProUGUI ResultText;
        public TextMeshProUGUI ReasonText;
        public TextMeshProUGUI DetectedActionText;
        public TextMeshProUGUI AttrText;
        public TextMeshProUGUI ExpText;
        public TextMeshProUGUI StreakText;
        public Button ResultConfirmButton;
        public TMP_FontAsset Font;
        public Sprite CardSprite;
        public Sprite CreamButtonSprite;
        public Sprite GoldButtonSprite;
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;

        public IEnumerable<UnityEngine.Object> AllReferences()
        {
            return new UnityEngine.Object[]
            {
                PhotoPopup, PhotoView, PhotoButton, VerifyButton, CancelButton, NoticePopup, NoticeText,
                ResultPopup, ResultText, ReasonText, DetectedActionText, AttrText, ExpText, StreakText,
                ResultConfirmButton
            }.Where(item => item != null);
        }
    }

    private sealed class ProtectionSnapshot
    {
        private string managerJson;
        private string[] eventSignatures;

        public static ProtectionSnapshot Capture(ValidationResult result)
        {
            return new ProtectionSnapshot
            {
                managerJson = EditorJsonUtility.ToJson(result.Manager),
                eventSignatures = CaptureEvents(result.PopupRoot)
            };
        }

        public void AssertUnchanged(ValidationResult result)
        {
            if (managerJson != EditorJsonUtility.ToJson(result.Manager))
            {
                throw new InvalidOperationException("PhotoVerificationManager SerializeField 참조가 변경되었습니다.");
            }

            if (!eventSignatures.SequenceEqual(CaptureEvents(result.PopupRoot)))
            {
                throw new InvalidOperationException("PHOTO Popup Button UnityEvent가 변경되었습니다.");
            }
        }

        private static string[] CaptureEvents(GameObject root)
        {
            List<string> signatures = new List<string>();
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                string path = HierarchyPath(root.transform, button.transform);
                signatures.Add(path + "|count=" + button.onClick.GetPersistentEventCount());
                for (int index = 0; index < button.onClick.GetPersistentEventCount(); index++)
                {
                    UnityEngine.Object target = button.onClick.GetPersistentTarget(index);
                    signatures.Add(path + "|" + index + "|" +
                                   (target != null ? target.GetInstanceID().ToString() : "null") + "|" +
                                   button.onClick.GetPersistentMethodName(index));
                }
            }

            signatures.Sort(StringComparer.Ordinal);
            return signatures.ToArray();
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
    }
}
