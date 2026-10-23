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

[InitializeOnLoad]
public static class InventoryMainUiRenewalEditor
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
    private const string PreviewRootName = "InventoryMainPreviewRoot";
    private const string HabitItemPreviewRootName = "HabitItemPreviewTestRoot";
    private const string NumberPreviewRootName = "NumberOverlayPreviewRoot";
    private const string PhotoPreviewRootName = "PhotoVerificationPreviewRoot";
    private const string CharacterPreviewRootName = "CharacterMainPreviewRoot";
    private const string ValidateMenu = "Tools/HabitPVP/UI Renewal/Validate Inventory Main Renewal";
    private const string ApplyMenu = "Tools/HabitPVP/UI Renewal/Apply Inventory Main Renewal";
    private const string PreviewMenu =
        "Tools/HabitPVP/UI Renewal/Preview Mode/G. Inventory Main Renewal";

    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 CreamLight = new Color32(0xFF, 0xF9, 0xEA, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x86, 0x68, 0x5D, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);
    private static readonly Color32 EquippedGreen = new Color32(0x6E, 0x9B, 0x58, 0xFF);
    private static readonly Color32 ExpBlue = new Color32(0x39, 0x9E, 0xE8, 0xFF);
    private static readonly Color32 ExpBlueDark = new Color32(0x20, 0x4F, 0x72, 0xFF);

    static InventoryMainUiRenewalEditor()
    {
        EditorApplication.update -= RestoreWhenAnotherPreviewModeIsSelected;
        EditorApplication.update += RestoreWhenAnotherPreviewModeIsSelected;
        EditorSceneManager.sceneSaving -= RestoreBeforeSceneSave;
        EditorSceneManager.sceneSaving += RestoreBeforeSceneSave;
    }

    [MenuItem(ValidateMenu, false, 2080)]
    private static void Validate()
    {
        ValidationResult result = ValidateScene(true);
        if (!result.IsValid)
        {
            return;
        }

        List<string> errors = ValidateRenewalLayout(result, true);
        if (errors.Count > 0)
        {
            foreach (string error in errors)
            {
                Debug.LogError("[HabitPVP Inventory Main] " + error, result.InventoryTap);
            }
            return;
        }

        Debug.Log(
            "[HabitPVP Inventory Main] 검증 성공. 기존 InventoryManager 참조/UnityEvent를 유지하고 " +
            "장비·소비 탭, 강화 이름/레벨/EXP, RPG 스타일, 1080x1920 및 G Preview 격리가 정상입니다.",
            result.InventoryTap);
    }

    [MenuItem(ApplyMenu, false, 2081)]
    private static void Apply()
    {
        ValidationResult result = ValidateScene(true);
        if (!result.IsValid)
        {
            Debug.LogError("[HabitPVP Inventory Main] Apply 중단: 구조 검증 오류를 먼저 해결해주세요.");
            return;
        }

        ProtectionSnapshot snapshot = ProtectionSnapshot.Capture(result);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Inventory Main UI Renewal");

        try
        {
            ApplyInventoryStyle(result);
            ApplyEquipmentDetailStyle(result);
            ApplyConsumableDetailStyles(result);
            ApplyEnhanceStyle(result);
            EnsurePreviewRoot(result);
            snapshot.AssertUnchanged(result);

            List<string> errors = ValidateRenewalLayout(result, true);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join("\n", errors));
            }

            EditorSceneManager.MarkSceneDirty(result.Scene);
            Undo.CollapseUndoOperations(undoGroup);
            if (!EditorSceneManager.SaveScene(result.Scene))
            {
                throw new InvalidOperationException("UITestScene 저장에 실패했습니다.");
            }

            Selection.activeGameObject = result.InventoryTap;
            Debug.Log(
                "[HabitPVP Inventory Main] 적용 성공. UITestScene의 InventoryTap 표시 Override와 " +
                "G Preview 전용 루트만 저장했습니다. MainScene, Canvas.prefab, InventoryManager, " +
                "CharacterManager, ItemSlotUI 및 API 코드는 변경하지 않았습니다.", result.InventoryTap);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            EditorSceneManager.MarkSceneDirty(result.Scene);
            Debug.LogException(exception);
            Debug.LogError("[HabitPVP Inventory Main] Apply 실패. 이번 실행의 변경을 Undo로 되돌렸습니다.");
        }
    }

    [MenuItem(ApplyMenu, true)]
    private static bool CanApply() => CanUseSceneMenus();

    [MenuItem(PreviewMenu, false, 2082)]
    private static void ActivatePreview()
    {
        SpendingReportUiRenewalEditor.RestoreSpendingPreviewState();
        PvpRankingUiRenewalEditor.RestorePvpPreviewState();
        ShopGachaUiRenewalEditor.RestoreShopPreviewState();
        RestoreInventoryPreviewState();
        CharacterMainUiRenewalEditor.RestoreCharacterPreviewState();
        PhotoVerificationPopupUiRenewalEditor.RestorePhotoPreviewState();
        AchievedAmountOverlayUiRenewalEditor.RestoreNumberPreviewState();
        HabitGoalFormUiRenewalEditor.RestoreHabitMainPreviewState();

        ValidationResult result = ValidateScene(true);
        if (!result.IsValid)
        {
            return;
        }

        GameObject previewRoot = FindRoot(result.Scene, PreviewRootName);
        if (previewRoot == null)
        {
            Debug.LogError(
                "[HabitPVP Inventory Main] G Preview 루트가 없습니다. " +
                "Apply Inventory Main Renewal을 먼저 실행해주세요.");
            return;
        }

        SetActive(FindRoot(result.Scene, HabitItemPreviewRootName), false, "Hide HabitItem preview for G mode");
        SetActive(FindRoot(result.Scene, NumberPreviewRootName), false, "Hide NUMBER preview for G mode");
        SetActive(FindRoot(result.Scene, PhotoPreviewRootName), false, "Hide PHOTO preview for G mode");
        SetActive(FindRoot(result.Scene, CharacterPreviewRootName), false, "Hide Character preview for G mode");

        InventoryMainPreviewController controller =
            previewRoot.GetComponent<InventoryMainPreviewController>();
        SetEnabled(controller, true, "Enable inventory main preview controller");
        IsolateScene(result.Scene, previewRoot, controller);
        SetActive(previewRoot, true, "Activate inventory main preview");
        EditorSceneManager.MarkSceneDirty(result.Scene);
        Selection.activeGameObject = previewRoot;
        Debug.Log(
            "[HabitPVP Inventory Main] G 모드: 로컬 가상 장비/소비 아이템 목록을 표시합니다. " +
            "상세→강화→로컬 수치 변경→복귀를 확인할 수 있으며 InventoryManager, " +
            "CharacterManager 및 API는 호출하지 않습니다. A~F 메뉴 선택 시 격리 상태를 복원합니다.",
            previewRoot);
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
            .Where(item => item.gameObject.scene == result.Scene && item.transform.parent == null &&
                           item.gameObject.name == "Canvas")
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

        Transform inventoryTap = result.Canvas.transform.Find("InventoryTap");
        if (inventoryTap == null)
        {
            result.Errors.Add("Canvas/InventoryTap을 찾을 수 없습니다.");
            return FinishValidation(result, log);
        }

        result.InventoryTap = inventoryTap.gameObject;
        InventoryManager[] managers = UnityEngine.Object.FindObjectsOfType<InventoryManager>(true)
            .Where(item => item.gameObject.scene == result.Scene)
            .ToArray();
        if (managers.Length != 1)
        {
            result.Errors.Add($"InventoryManager가 정확히 하나여야 합니다. 현재: {managers.Length}");
            return FinishValidation(result, log);
        }
        result.Manager = managers[0];

        result.TopButtons = Find<RectTransform>(inventoryTap, "TopBottons", result.Errors);
        result.EquipmentButton = Find<Button>(inventoryTap, "TopBottons/EquipmentButton", result.Errors);
        result.ConsumableButton = Find<Button>(inventoryTap, "TopBottons/ConsumableButton", result.Errors);
        result.InventoryScroll = Find<ScrollRect>(inventoryTap, "InventoryScroll", result.Errors);
        result.Viewport = Find<RectTransform>(inventoryTap, "InventoryScroll/Viewport", result.Errors);
        result.Content = Find<RectTransform>(inventoryTap, "InventoryScroll/Viewport/Content", result.Errors);
        result.PopupDim = Find<Image>(inventoryTap, "PopupDim", result.Errors);
        result.EquipDetail = Find<Image>(inventoryTap, "EquipItemDetailPopup", result.Errors);
        result.EquipIcon = Find<Image>(inventoryTap, "EquipItemDetailPopup/ItemIcon", result.Errors);
        result.EquipName = Find<TMP_Text>(inventoryTap, "EquipItemDetailPopup/ItemName", result.Errors);
        result.EquipDescription = Find<TMP_Text>(inventoryTap,
            "EquipItemDetailPopup/ItemDescription", result.Errors);
        result.DoEquipButton = Find<Button>(inventoryTap,
            "EquipItemDetailPopup/EquipButton", result.Errors);
        result.EnhanceButton = Find<Button>(inventoryTap,
            "EquipItemDetailPopup/EnhanceButton", result.Errors);
        result.EquipCloseButton = Find<Button>(inventoryTap,
            "EquipItemDetailPopup/CloseButton", result.Errors);
        result.ConsumDetail = Find<Image>(inventoryTap, "ConsumItemDetailPopup", result.Errors);
        result.ConsumIcon = Find<Image>(inventoryTap, "ConsumItemDetailPopup/ItemIcon", result.Errors);
        result.ConsumName = Find<TMP_Text>(inventoryTap, "ConsumItemDetailPopup/ItemName", result.Errors);
        result.ConsumDescription = Find<TMP_Text>(inventoryTap,
            "ConsumItemDetailPopup/ItemDescription", result.Errors);
        result.ConsumCloseButton = Find<Button>(inventoryTap,
            "ConsumItemDetailPopup/CloseButton", result.Errors);
        result.FuncDetail = Find<Image>(inventoryTap, "FunctionalItemDetailPopup", result.Errors);
        result.FuncIcon = Find<Image>(inventoryTap, "FunctionalItemDetailPopup/ItemIcon", result.Errors);
        result.FuncName = Find<TMP_Text>(inventoryTap, "FunctionalItemDetailPopup/ItemName", result.Errors);
        result.FuncDescription = Find<TMP_Text>(inventoryTap,
            "FunctionalItemDetailPopup/ItemDescription", result.Errors);
        result.FuncUseButton = Find<Button>(inventoryTap,
            "FunctionalItemDetailPopup/UseButton", result.Errors);
        result.FuncCloseButton = Find<Button>(inventoryTap,
            "FunctionalItemDetailPopup/CloseButton", result.Errors);

        result.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        result.CardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardSpritePath);
        result.CreamButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CreamButtonSpritePath);
        result.GoldButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GoldButtonSpritePath);
        if (result.Font == null || result.CardSprite == null || result.CreamButtonSprite == null ||
            result.GoldButtonSprite == null)
        {
            result.Errors.Add("Inventory Main Renewal에 필요한 NotoSansKR 또는 Free Casual GUI Sprite가 없습니다.");
        }

        SerializedObject manager = new SerializedObject(result.Manager);
        string[] requiredManagerReferences =
        {
            "itemSlotPrefab", "itemSlotParent", "scrollView", "equipButton", "consumableButton"
        };
        foreach (string propertyName in requiredManagerReferences)
        {
            SerializedProperty property = manager.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == null)
            {
                result.Errors.Add($"InventoryManager 필수 참조가 비어 있습니다: {propertyName}");
            }
        }

        ValidateManagerReference(manager, "popupDim", result.PopupDim != null ? result.PopupDim.gameObject : null,
            result.Errors);
        ValidateManagerReference(manager, "equipDetail",
            result.EquipDetail != null ? result.EquipDetail.gameObject : null, result.Errors);
        ValidateManagerReference(manager, "equipIcon", result.EquipIcon, result.Errors);
        ValidateManagerReference(manager, "equipNameText", result.EquipName, result.Errors);
        ValidateManagerReference(manager, "equipDescText", result.EquipDescription, result.Errors);
        ValidateManagerReference(manager, "doEquipButton", result.DoEquipButton, result.Errors);
        ValidateManagerReference(manager, "equipCloseButton", result.EquipCloseButton, result.Errors);
        ValidateManagerReference(manager, "consumDetail",
            result.ConsumDetail != null ? result.ConsumDetail.gameObject : null, result.Errors);
        ValidateManagerReference(manager, "consumIcon", result.ConsumIcon, result.Errors);
        ValidateManagerReference(manager, "consumNameText", result.ConsumName, result.Errors);
        ValidateManagerReference(manager, "consumDescText", result.ConsumDescription, result.Errors);
        ValidateManagerReference(manager, "consumCloseButton", result.ConsumCloseButton, result.Errors);
        ValidateManagerReference(manager, "funcDetail",
            result.FuncDetail != null ? result.FuncDetail.gameObject : null, result.Errors);
        ValidateManagerReference(manager, "funcIcon", result.FuncIcon, result.Errors);
        ValidateManagerReference(manager, "funcNameText", result.FuncName, result.Errors);
        ValidateManagerReference(manager, "funcDescText", result.FuncDescription, result.Errors);
        ValidateManagerReference(manager, "funcUseButton", result.FuncUseButton, result.Errors);
        ValidateManagerReference(manager, "funcCloseButton", result.FuncCloseButton, result.Errors);

        result.EnhanceTap = GetManagerReference<GameObject>(manager, "enhanceTap", result.Errors);
        result.EnhanceTargetName = GetManagerReference<TMP_Text>(manager,
            "enhanceTargetItemNameText", result.Errors);
        result.EnhanceTargetIcon = GetManagerReference<Image>(manager,
            "enhanceTargetItemIcon", result.Errors);
        result.EnhanceTargetLevel = GetManagerReference<TMP_Text>(manager,
            "enhanceTargetItemLevelText", result.Errors);
        result.EnhanceExpText = GetManagerReference<TMP_Text>(manager,
            "enhanceTargetItemExpText", result.Errors);
        result.EnhanceExpFill = GetManagerReference<Image>(manager,
            "enhanceTargetItemExpBarFill", result.Errors);
        result.EnhanceButtonBar = GetManagerReference<GameObject>(manager,
            "enhanceButtonBar", result.Errors);
        result.DoEnhanceButton = GetManagerReference<Button>(manager,
            "doEnhanceButton", result.Errors);
        result.EnhanceCancelButton = GetManagerReference<Button>(manager,
            "enhanceCancelButton", result.Errors);
        if (result.EnhanceExpFill != null)
        {
            Transform expRoot = result.EnhanceExpFill.transform.parent;
            result.EnhanceExpRoot = expRoot != null ? expRoot.GetComponent<RectTransform>() : null;
            Transform expBase = expRoot != null ? expRoot.Find("ExpBarBase") : null;
            result.EnhanceExpBase = expBase != null ? expBase.GetComponent<Image>() : null;
            if (result.EnhanceExpRoot == null || result.EnhanceExpBase == null ||
                expRoot.gameObject.name != "TargetItemExpRoot")
            {
                result.Errors.Add("기존 TargetItemExpRoot/ExpBarBase/ExpBarFill 구조가 예상과 다릅니다.");
            }
        }
        if (result.EnhanceTap != null &&
            (result.EnhanceTap.name != "EnhanceTap" || result.EnhanceTap.transform.parent != inventoryTap))
        {
            result.Errors.Add("기존 InventoryTap/EnhanceTap 계층이 예상과 다릅니다.");
        }
        if (result.EnhanceTap != null && result.EnhanceTargetName != null && result.EnhanceTargetIcon != null &&
            result.EnhanceTargetLevel != null &&
            (result.EnhanceTargetName.transform.parent != result.EnhanceTap.transform ||
             result.EnhanceTargetIcon.transform.parent != result.EnhanceTap.transform ||
             result.EnhanceTargetLevel.transform.parent != result.EnhanceTap.transform))
        {
            result.Errors.Add("강화 대상 이름/아이콘/레벨이 EnhanceTap 직속 UI가 아닙니다.");
        }
        if (result.EnhanceButtonBar != null && result.DoEnhanceButton != null &&
            result.EnhanceCancelButton != null &&
            (result.EnhanceButtonBar.name != "EnhanceButtonBar" ||
             result.EnhanceButtonBar.transform.parent != inventoryTap ||
             result.DoEnhanceButton.transform.parent != result.EnhanceButtonBar.transform ||
             result.EnhanceCancelButton.transform.parent != result.EnhanceButtonBar.transform))
        {
            result.Errors.Add("기존 EnhanceButtonBar/EnhanceButton/CancelButton 계층이 예상과 다릅니다.");
        }

        SerializedProperty enhanceReference = manager.FindProperty("enhanceButton");
        if (log && enhanceReference != null && enhanceReference.objectReferenceValue == null)
        {
            Debug.LogWarning(
                "[HabitPVP Inventory Detail] 기존 확인 사항: InventoryManager.enhanceButton이 null입니다. " +
                "11-C-3도 게임 로직/SerializeField를 변경하거나 참조를 임의 연결하지 않습니다.",
                result.Manager);
        }

        return FinishValidation(result, log);
    }

    private static void ValidateManagerReference(SerializedObject manager, string propertyName,
        UnityEngine.Object expected, ICollection<string> errors)
    {
        SerializedProperty property = manager.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue != expected)
        {
            errors.Add($"InventoryManager.{propertyName}이 현재 팝업 Hierarchy와 일치하지 않습니다.");
        }
    }

    private static T GetManagerReference<T>(SerializedObject manager, string propertyName,
        ICollection<string> errors) where T : UnityEngine.Object
    {
        SerializedProperty property = manager.FindProperty(propertyName);
        T value = property != null ? property.objectReferenceValue as T : null;
        if (value == null)
        {
            errors.Add($"InventoryManager 필수 강화 UI 참조가 비어 있습니다: {propertyName}");
        }
        return value;
    }

    private static ValidationResult FinishValidation(ValidationResult result, bool log)
    {
        if (log)
        {
            foreach (string error in result.Errors)
            {
                Debug.LogError("[HabitPVP Inventory Main] " + error,
                    result.InventoryTap != null ? result.InventoryTap : result.Canvas);
            }
        }
        return result;
    }

    private static T Find<T>(Transform root, string path, ICollection<string> errors) where T : Component
    {
        Transform target = root.Find(path);
        T component = target != null ? target.GetComponent<T>() : null;
        if (component == null)
        {
            errors.Add($"필수 UI를 찾을 수 없습니다: {path} ({typeof(T).Name})");
        }
        return component;
    }

    private static void ApplyInventoryStyle(ValidationResult result)
    {
        Image background = result.InventoryTap.GetComponent<Image>();
        if (background == null)
        {
            background = Undo.AddComponent<Image>(result.InventoryTap);
        }
        SetImage(background, null, Cream, Image.Type.Simple, "Style inventory background");
        background.raycastTarget = false;
        Dirty(background);

        SetStretchRect(result.InventoryTap.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero,
            "Stretch InventoryTap");
        SetTopRect(result.TopButtons, 150f, 32f, "Place inventory category tabs");
        StyleTab(result.EquipmentButton, result.GoldButtonSprite, result.Font, -250f, "장비");
        StyleTab(result.ConsumableButton, result.CreamButtonSprite, result.Font, 250f, "소비 아이템");

        RectTransform scrollRect = result.InventoryScroll.GetComponent<RectTransform>();
        SetStretchRect(scrollRect, new Vector2(42f, 205f), new Vector2(-42f, -205f),
            "Place inventory scroll above bottom navigation");
        Image scrollImage = result.InventoryScroll.GetComponent<Image>();
        if (scrollImage == null)
        {
            scrollImage = Undo.AddComponent<Image>(result.InventoryScroll.gameObject);
        }
        SetImage(scrollImage, result.CardSprite, CreamLight, Image.Type.Sliced, "Style inventory scroll panel");
        result.InventoryScroll.horizontal = false;
        result.InventoryScroll.vertical = true;
        result.InventoryScroll.movementType = ScrollRect.MovementType.Elastic;
        result.InventoryScroll.scrollSensitivity = 34f;
        Dirty(result.InventoryScroll);

        SetStretchRect(result.Viewport, new Vector2(28f, 30f), new Vector2(-28f, -30f),
            "Inset inventory viewport");
        Image viewportImage = result.Viewport.GetComponent<Image>();
        if (viewportImage != null)
        {
            SetImage(viewportImage, null, new Color32(0xFF, 0xFF, 0xFF, 0x08), Image.Type.Simple,
                "Keep viewport mask transparent");
        }

        VerticalLayoutGroup layout = result.Content.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            Record(layout, "Style inventory item list layout");
            layout.padding = new RectOffset(10, 10, 12, 18);
            layout.spacing = 30f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            Dirty(layout);
        }

        ContentSizeFitter fitter = result.Content.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            Record(fitter, "Keep inventory content vertical fit");
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Dirty(fitter);
        }

    }

    private static void StyleTab(Button button, Sprite sprite, TMP_FontAsset font, float x, string label)
    {
        Image image = button.GetComponent<Image>();
        SetImage(image, sprite, Color.white, Image.Type.Sliced, "Style inventory category button");
        SetCenteredRect(button.GetComponent<RectTransform>(), new Vector2(450f, 112f),
            new Vector2(x, -75f), "Place inventory category button");
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            SetText(text, label, font, 32f, Brown, FontStyles.Bold);
            SetStretchRect(text.rectTransform, new Vector2(22f, 12f), new Vector2(-22f, -12f),
                "Inset inventory category label");
        }
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(0xFF, 0xF4, 0xD8, 0xFF);
        colors.pressedColor = new Color32(0xE7, 0xD1, 0xA6, 0xFF);
        colors.selectedColor = Color.white;
        Record(button, "Style inventory category button states");
        button.colors = colors;
        Dirty(button);
    }

    private static void ApplyEquipmentDetailStyle(ValidationResult result)
    {
        SetImage(result.PopupDim, null, new Color32(0x2B, 0x1C, 0x1D, 0xA8), Image.Type.Simple,
            "Style equipment detail dim");
        SetStretchRect(result.PopupDim.rectTransform, Vector2.zero, Vector2.zero,
            "Stretch equipment detail dim");

        SetImage(result.EquipDetail, result.CardSprite, CreamLight, Image.Type.Sliced,
            "Style equipment detail panel");
        SetCenteredRect(result.EquipDetail.rectTransform, new Vector2(860f, 1040f), Vector2.zero,
            "Place equipment detail panel");

        Transform oldFrame = result.EquipDetail.transform.Find("RenewalItemIconFrame");
        if (oldFrame != null)
        {
            Undo.DestroyObjectImmediate(oldFrame.gameObject);
        }
        Image iconFrame = CreateImage(result.EquipDetail.transform, "RenewalItemIconFrame",
            result.GoldButtonSprite, Color.white);
        iconFrame.transform.SetAsFirstSibling();
        SetCenteredRect(iconFrame.rectTransform, new Vector2(300f, 300f), new Vector2(0f, 285f),
            "Place equipment detail icon frame");

        SetCenteredRect(result.EquipIcon.rectTransform, new Vector2(220f, 220f), new Vector2(0f, 285f),
            "Place equipment detail icon");
        Record(result.EquipIcon, "Keep equipment icon aspect");
        result.EquipIcon.preserveAspect = true;
        result.EquipIcon.raycastTarget = false;
        Dirty(result.EquipIcon);

        SetText(result.EquipName, result.EquipName.text, result.Font, 46f, Brown, FontStyles.Bold);
        result.EquipName.alignment = TextAlignmentOptions.Center;
        result.EquipName.enableWordWrapping = false;
        result.EquipName.overflowMode = TextOverflowModes.Ellipsis;
        SetCenteredRect(result.EquipName.rectTransform, new Vector2(720f, 72f), new Vector2(0f, 80f),
            "Place equipment detail item name");

        SetText(result.EquipDescription, result.EquipDescription.text, result.Font, 30f, MutedBrown,
            FontStyles.Normal);
        result.EquipDescription.alignment = TextAlignmentOptions.TopLeft;
        result.EquipDescription.enableWordWrapping = true;
        result.EquipDescription.overflowMode = TextOverflowModes.Ellipsis;
        result.EquipDescription.lineSpacing = 8f;
        result.EquipDescription.margin = new Vector4(16f, 14f, 16f, 14f);
        SetCenteredRect(result.EquipDescription.rectTransform, new Vector2(720f, 190f),
            new Vector2(0f, -65f), "Place equipment detail description");

        StyleDetailButton(result.DoEquipButton, result.GoldButtonSprite, result.Font,
            new Vector2(-190f, -280f), new Vector2(340f, 112f), "장착하기");
        StyleDetailButton(result.EnhanceButton, result.GoldButtonSprite, result.Font,
            new Vector2(190f, -280f), new Vector2(340f, 112f), "강화하기");
        StyleDetailButton(result.EquipCloseButton, result.CreamButtonSprite, result.Font,
            new Vector2(0f, -425f), new Vector2(720f, 100f), "닫기");
    }

    private static void ApplyConsumableDetailStyles(ValidationResult result)
    {
        ApplyConsumablePanelStyle(result.ConsumDetail, result.ConsumIcon, result.ConsumName,
            result.ConsumDescription, result, "RenewalItemIconFrame");
        StyleDetailButton(result.ConsumCloseButton, result.CreamButtonSprite, result.Font,
            new Vector2(0f, -410f), new Vector2(720f, 100f), "닫기");

        ApplyConsumablePanelStyle(result.FuncDetail, result.FuncIcon, result.FuncName,
            result.FuncDescription, result, "RenewalItemIconFrame");
        StyleDetailButton(result.FuncUseButton, result.GoldButtonSprite, result.Font,
            new Vector2(-190f, -360f), new Vector2(340f, 112f), "사용하기");
        StyleDetailButton(result.FuncCloseButton, result.CreamButtonSprite, result.Font,
            new Vector2(190f, -360f), new Vector2(340f, 112f), "닫기");
    }

    private static void ApplyConsumablePanelStyle(Image panel, Image icon, TMP_Text itemName,
        TMP_Text description, ValidationResult result, string frameName)
    {
        SetImage(panel, result.CardSprite, CreamLight, Image.Type.Sliced,
            "Style consumable detail panel");
        SetCenteredRect(panel.rectTransform, new Vector2(860f, 1040f), Vector2.zero,
            "Place consumable detail panel");

        Transform oldFrame = panel.transform.Find(frameName);
        if (oldFrame != null)
        {
            Undo.DestroyObjectImmediate(oldFrame.gameObject);
        }
        Image iconFrame = CreateImage(panel.transform, frameName, result.GoldButtonSprite, Color.white);
        iconFrame.transform.SetAsFirstSibling();
        SetCenteredRect(iconFrame.rectTransform, new Vector2(300f, 300f), new Vector2(0f, 285f),
            "Place consumable detail icon frame");

        SetCenteredRect(icon.rectTransform, new Vector2(220f, 220f), new Vector2(0f, 285f),
            "Place consumable detail icon");
        Record(icon, "Keep consumable icon aspect");
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        Dirty(icon);

        SetText(itemName, itemName.text, result.Font, 46f, Brown, FontStyles.Bold);
        itemName.alignment = TextAlignmentOptions.Center;
        itemName.enableWordWrapping = false;
        itemName.overflowMode = TextOverflowModes.Ellipsis;
        SetCenteredRect(itemName.rectTransform, new Vector2(720f, 72f), new Vector2(0f, 80f),
            "Place consumable detail item name");

        SetText(description, description.text, result.Font, 30f, MutedBrown, FontStyles.Normal);
        description.alignment = TextAlignmentOptions.TopLeft;
        description.enableWordWrapping = true;
        description.overflowMode = TextOverflowModes.Ellipsis;
        description.lineSpacing = 8f;
        description.margin = new Vector4(16f, 14f, 16f, 14f);
        SetCenteredRect(description.rectTransform, new Vector2(720f, 250f), new Vector2(0f, -95f),
            "Place consumable detail description");
    }

    private static void StyleDetailButton(Button button, Sprite sprite, TMP_FontAsset font,
        Vector2 position, Vector2 size, string fallbackLabel)
    {
        if (button == null)
        {
            return;
        }

        SetImage(button.image, sprite, Color.white, Image.Type.Sliced,
            "Style equipment detail button");
        SetCenteredRect(button.GetComponent<RectTransform>(), size, position,
            "Place equipment detail button");
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            string value = string.IsNullOrWhiteSpace(label.text) ? fallbackLabel : label.text;
            SetText(label, value, font, 30f, Brown, FontStyles.Bold);
            SetStretchRect(label.rectTransform, new Vector2(18f, 10f), new Vector2(-18f, -10f),
                "Inset equipment detail button label");
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(0xFF, 0xF4, 0xD8, 0xFF);
        colors.pressedColor = new Color32(0xE7, 0xD1, 0xA6, 0xFF);
        colors.selectedColor = new Color32(0xF6, 0xD7, 0x91, 0xFF);
        colors.disabledColor = new Color32(0xA9, 0x9B, 0x88, 0xB8);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.12f;
        Record(button, "Style equipment detail button states");
        button.colors = colors;
        Dirty(button);
    }

    private static void ApplyEnhanceStyle(ValidationResult result)
    {
        Image enhancePanel = result.EnhanceTap.GetComponent<Image>();
        if (enhancePanel == null)
        {
            enhancePanel = Undo.AddComponent<Image>(result.EnhanceTap);
        }
        SetImage(enhancePanel, result.CardSprite, CreamLight, Image.Type.Sliced,
            "Style enhance target panel");
        SetTopRect(result.EnhanceTap.GetComponent<RectTransform>(), 470f, 125f,
            "Place enhance target panel");

        Transform oldFrame = result.EnhanceTap.transform.Find("RenewalTargetIconFrame");
        if (oldFrame != null)
        {
            Undo.DestroyObjectImmediate(oldFrame.gameObject);
        }
        Image iconFrame = CreateImage(result.EnhanceTap.transform, "RenewalTargetIconFrame",
            result.GoldButtonSprite, Color.white);
        iconFrame.transform.SetAsFirstSibling();
        SetCenteredRect(iconFrame.rectTransform, new Vector2(270f, 270f), new Vector2(-300f, 45f),
            "Place enhance target icon frame");

        SetCenteredRect(result.EnhanceTargetIcon.rectTransform, new Vector2(205f, 205f),
            new Vector2(-300f, 45f), "Place enhance target icon");
        Record(result.EnhanceTargetIcon, "Keep enhance target icon aspect");
        result.EnhanceTargetIcon.preserveAspect = true;
        result.EnhanceTargetIcon.raycastTarget = false;
        Dirty(result.EnhanceTargetIcon);

        SetText(result.EnhanceTargetName, result.EnhanceTargetName.text, result.Font, 43f, Brown,
            FontStyles.Bold);
        result.EnhanceTargetName.alignment = TextAlignmentOptions.Center;
        result.EnhanceTargetName.overflowMode = TextOverflowModes.Ellipsis;
        SetCenteredRect(result.EnhanceTargetName.rectTransform, new Vector2(540f, 70f),
            new Vector2(155f, 135f), "Place enhance target name");

        SetText(result.EnhanceTargetLevel, result.EnhanceTargetLevel.text, result.Font, 34f, Gold,
            FontStyles.Bold);
        result.EnhanceTargetLevel.alignment = TextAlignmentOptions.Center;
        SetCenteredRect(result.EnhanceTargetLevel.rectTransform, new Vector2(500f, 58f),
            new Vector2(155f, 64f), "Place enhance target level");

        SetCenteredRect(result.EnhanceExpRoot, new Vector2(560f, 110f), new Vector2(155f, -65f),
            "Place enhance EXP group");
        SetImage(result.EnhanceExpBase, null, ExpBlueDark, Image.Type.Simple,
            "Style enhance EXP base");
        SetCenteredRect(result.EnhanceExpBase.rectTransform, new Vector2(520f, 50f),
            new Vector2(0f, -17f), "Place enhance EXP base");
        SetImage(result.EnhanceExpFill, null, ExpBlue, Image.Type.Simple,
            "Style enhance EXP fill");
        Record(result.EnhanceExpFill.rectTransform, "Place enhance EXP fill");
        RectTransform fillRect = result.EnhanceExpFill.rectTransform;
        float ratio = Mathf.Clamp01(fillRect.localScale.x);
        fillRect.anchorMin = new Vector2(0.5f, 0.5f);
        fillRect.anchorMax = new Vector2(0.5f, 0.5f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.sizeDelta = new Vector2(520f, 50f);
        fillRect.anchoredPosition = new Vector2(-260f, -17f);
        fillRect.localScale = new Vector3(ratio, 1f, 1f);
        Dirty(fillRect);

        SetText(result.EnhanceExpText, result.EnhanceExpText.text, result.Font, 27f, Brown,
            FontStyles.Bold);
        result.EnhanceExpText.alignment = TextAlignmentOptions.Center;
        SetCenteredRect(result.EnhanceExpText.rectTransform, new Vector2(520f, 42f),
            new Vector2(0f, 36f), "Place enhance EXP text");

        Image buttonBarImage = result.EnhanceButtonBar.GetComponent<Image>();
        if (buttonBarImage == null)
        {
            buttonBarImage = Undo.AddComponent<Image>(result.EnhanceButtonBar);
        }
        SetImage(buttonBarImage, null, Cream, Image.Type.Simple, "Style enhance button bar");
        buttonBarImage.raycastTarget = false;
        SetBottomRect(result.EnhanceButtonBar.GetComponent<RectTransform>(), 250f, 190f,
            "Place enhance button bar");
        StyleDetailButton(result.DoEnhanceButton, result.GoldButtonSprite, result.Font,
            new Vector2(-235f, 0f), new Vector2(430f, 112f), "강화하기");
        StyleDetailButton(result.EnhanceCancelButton, result.CreamButtonSprite, result.Font,
            new Vector2(235f, 0f), new Vector2(430f, 112f), "취소");
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
        Undo.RegisterCreatedObjectUndo(root, "Create Inventory Main preview root");
        root.transform.SetAsLastSibling();
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 510;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 1f;

        Image background = CreateImage(root.transform, "PreviewBackground", null, Cream);
        SetStretchRect(background.rectTransform, Vector2.zero, Vector2.zero, "Stretch inventory preview background");
        TMP_Text heading = CreateText(background.transform, "PreviewHeading",
            "G. INVENTORY MAIN RENEWAL · LOCAL PREVIEW", result.Font, 27f, MutedBrown, FontStyles.Bold);
        SetCenteredRect(heading.rectTransform, new Vector2(980f, 44f), new Vector2(0f, 885f),
            "Place inventory preview heading");

        TMP_Text title = CreateText(background.transform, "Title", "인벤토리", result.Font, 52f,
            Brown, FontStyles.Bold);
        SetCenteredRect(title.rectTransform, new Vector2(920f, 70f), new Vector2(0f, 820f),
            "Place inventory title");

        Button equipmentButton = CreateButton(background.transform, "EquipmentButton", "장비",
            result.GoldButtonSprite, result.Font, new Vector2(-250f, 728f), new Vector2(450f, 105f));
        Button consumableButton = CreateButton(background.transform, "ConsumableButton", "소비 아이템",
            result.CreamButtonSprite, result.Font, new Vector2(250f, 728f), new Vector2(450f, 105f));

        Image panel = CreateImage(background.transform, "InventoryScroll", result.CardSprite, CreamLight);
        SetCenteredRect(panel.rectTransform, new Vector2(1000f, 1370f), new Vector2(0f, -25f),
            "Place inventory preview scroll panel");
        ScrollRect scroll = Undo.AddComponent<ScrollRect>(panel.gameObject);
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 34f;

        Image viewport = CreateImage(panel.transform, "Viewport", null, new Color32(0xFF, 0xFF, 0xFF, 0x08));
        SetStretchRect(viewport.rectTransform, new Vector2(34f, 42f), new Vector2(-34f, -42f),
            "Inset inventory preview viewport");
        Mask mask = Undo.AddComponent<Mask>(viewport.gameObject);
        mask.showMaskGraphic = false;

        RectTransform content = CreateContent(viewport.transform, "Content");
        GameObject equipmentRoot = CreateCategoryRoot(content, "EquipmentItems");
        GameObject consumableRoot = CreateCategoryRoot(content, "ConsumableItems");

        string[,] equipmentItems =
        {
            { "새벽의 검", "ATK +18 · 모험의 시작을 함께한 검", "무기", "장착 중", "검" },
            { "숲지기 모자", "HP +120 · 잎사귀의 기운이 깃든 모자", "모자", "", "모" },
            { "용기의 갑옷", "DEF +24 · 단단한 골드 테두리 갑옷", "옷", "장착 중", "갑" },
            { "바람걸음 신발", "SPD +9 · 가벼운 발걸음을 돕는 신발", "신발", "", "신" },
            { "불꽃 장검", "ATK +31 · 불 속성 숙련자를 위한 무기", "무기", "", "불" },
            { "별빛 후드", "CRIT +6% · 밤하늘 빛을 머금은 모자", "모자", "", "별" },
            { "수호자의 제복", "DEF +42 · 오랜 습관을 증명하는 옷", "옷", "", "수" },
            { "구름 발자국", "SPD +14 · 구름처럼 가벼운 신발", "신발", "", "구" }
        };
        string[,] consumableItems =
        {
            { "작은 회복 물약", "HP를 조금 회복하는 기본 물약", "x12", "", "회" },
            { "집중의 차", "전투 준비 중 집중력을 높여주는 차", "x5", "", "집" },
            { "불꽃 영약", "불 속성의 힘을 일시적으로 높인다", "x3", "", "불" },
            { "맑은 샘물", "지친 모험가의 기운을 맑게 한다", "x18", "", "샘" },
            { "행운 쿠키", "작은 행운이 담긴 바삭한 간식", "x7", "", "행" },
            { "방어 비약", "전투 준비 중 방어 능력을 보조한다", "x4", "", "방" },
            { "신속의 깃털", "가벼운 움직임을 돕는 마법 깃털", "x9", "", "신" },
            { "오로라 사탕", "오로라 속성의 달콤한 사탕", "x21", "", "오" },
            { "튼튼한 도시락", "모험 전에 든든하게 먹는 도시락", "x2", "", "도" },
            { "별가루 병", "반짝이는 별가루를 담은 작은 병", "x15", "", "별" }
        };

        Button[] equipmentItemButtons = CreateCards(equipmentRoot.transform, equipmentItems, result);
        Button[] consumableItemButtons = CreateCards(consumableRoot.transform, consumableItems, result);
        UnityEngine.Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(equipmentRoot.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(consumableRoot.GetComponent<RectTransform>());
        UnityEngine.Canvas.ForceUpdateCanvases();
        consumableRoot.SetActive(false);

        scroll.viewport = viewport.rectTransform;
        scroll.content = equipmentRoot.GetComponent<RectTransform>();
        TMP_Text status = CreateText(background.transform, "CategoryStatus",
            "장비 8개 · 장착 상태와 레벨 표시 · LOCAL DATA", result.Font, 22f, MutedBrown,
            FontStyles.Normal);
        SetCenteredRect(status.rectTransform, new Vector2(980f, 40f), new Vector2(0f, -885f),
            "Place inventory preview status");

        Image detailDim = CreateImage(background.transform, "EquipmentDetailDim", null,
            new Color32(0x2B, 0x1C, 0x1D, 0xA8));
        SetStretchRect(detailDim.rectTransform, Vector2.zero, Vector2.zero,
            "Stretch preview equipment detail dim");
        Image detailPanel = CreateImage(detailDim.transform, "EquipItemDetailPopup", result.CardSprite,
            CreamLight);
        SetCenteredRect(detailPanel.rectTransform, new Vector2(860f, 1040f), Vector2.zero,
            "Place preview equipment detail panel");

        Image detailIconFrame = CreateImage(detailPanel.transform, "ItemIconFrame",
            result.GoldButtonSprite, Color.white);
        SetCenteredRect(detailIconFrame.rectTransform, new Vector2(300f, 300f), new Vector2(0f, 285f),
            "Place preview equipment icon frame");
        TMP_Text detailIconText = CreateText(detailIconFrame.transform, "ItemIconPlaceholder", "검",
            result.Font, 82f, Brown, FontStyles.Bold);
        SetStretchRect(detailIconText.rectTransform, new Vector2(28f, 28f), new Vector2(-28f, -28f),
            "Inset preview equipment icon placeholder");

        TMP_Text detailName = CreateText(detailPanel.transform, "ItemName", "새벽의 검", result.Font,
            46f, Brown, FontStyles.Bold);
        detailName.overflowMode = TextOverflowModes.Ellipsis;
        SetCenteredRect(detailName.rectTransform, new Vector2(720f, 72f), new Vector2(0f, 80f),
            "Place preview equipment name");
        TMP_Text detailType = CreateText(detailPanel.transform, "EquipmentType", "장비 유형 · 무기",
            result.Font, 25f, Gold, FontStyles.Bold);
        SetCenteredRect(detailType.rectTransform, new Vector2(720f, 48f), new Vector2(0f, 20f),
            "Place preview equipment type");
        TMP_Text detailDescription = CreateText(detailPanel.transform, "ItemDescription",
            "ATK +18 · 모험의 시작을 함께한 검", result.Font, 30f, MutedBrown, FontStyles.Normal);
        detailDescription.alignment = TextAlignmentOptions.TopLeft;
        detailDescription.enableWordWrapping = true;
        detailDescription.overflowMode = TextOverflowModes.Ellipsis;
        detailDescription.lineSpacing = 8f;
        detailDescription.margin = new Vector4(16f, 14f, 16f, 14f);
        SetCenteredRect(detailDescription.rectTransform, new Vector2(720f, 170f),
            new Vector2(0f, -90f), "Place preview equipment description");

        Button detailEquipButton = CreateButton(detailPanel.transform, "EquipButton", "해제하기",
            result.GoldButtonSprite, result.Font, new Vector2(-190f, -280f), new Vector2(340f, 112f));
        Button detailEnhanceButton = CreateButton(detailPanel.transform, "EnhanceButton", "강화하기",
            result.GoldButtonSprite, result.Font, new Vector2(190f, -280f), new Vector2(340f, 112f));
        Button detailCloseButton = CreateButton(detailPanel.transform, "CloseButton", "닫기",
            result.CreamButtonSprite, result.Font, new Vector2(0f, -425f), new Vector2(720f, 100f));
        detailDim.gameObject.SetActive(false);

        Image consumableDetailDim = CreateImage(background.transform, "ConsumableDetailDim", null,
            new Color32(0x2B, 0x1C, 0x1D, 0xA8));
        SetStretchRect(consumableDetailDim.rectTransform, Vector2.zero, Vector2.zero,
            "Stretch preview consumable detail dim");
        Image consumableDetailPanel = CreateImage(consumableDetailDim.transform,
            "ConsumItemDetailPopup", result.CardSprite, CreamLight);
        SetCenteredRect(consumableDetailPanel.rectTransform, new Vector2(860f, 1040f), Vector2.zero,
            "Place preview consumable detail panel");
        Image consumableIconFrame = CreateImage(consumableDetailPanel.transform, "ItemIconFrame",
            result.GoldButtonSprite, Color.white);
        SetCenteredRect(consumableIconFrame.rectTransform, new Vector2(300f, 300f),
            new Vector2(0f, 285f), "Place preview consumable icon frame");
        TMP_Text consumableDetailIcon = CreateText(consumableIconFrame.transform,
            "ItemIconPlaceholder", "집", result.Font, 82f, Brown, FontStyles.Bold);
        SetStretchRect(consumableDetailIcon.rectTransform, new Vector2(28f, 28f),
            new Vector2(-28f, -28f), "Inset preview consumable icon");
        TMP_Text consumableDetailName = CreateText(consumableDetailPanel.transform, "ItemName",
            "집중의 차", result.Font, 46f, Brown, FontStyles.Bold);
        consumableDetailName.overflowMode = TextOverflowModes.Ellipsis;
        SetCenteredRect(consumableDetailName.rectTransform, new Vector2(720f, 72f),
            new Vector2(0f, 80f), "Place preview consumable name");
        TMP_Text consumableDetailDescription = CreateText(consumableDetailPanel.transform,
            "ItemDescription", "전투 준비 중 집중력을 높여주는 차", result.Font, 30f,
            MutedBrown, FontStyles.Normal);
        StylePreviewDetailDescription(consumableDetailDescription);
        Button consumableDetailCloseButton = CreateButton(consumableDetailPanel.transform,
            "CloseButton", "닫기", result.CreamButtonSprite, result.Font,
            new Vector2(0f, -410f), new Vector2(720f, 100f));

        Image functionalDetailPanel = CreateImage(consumableDetailDim.transform,
            "FunctionalItemDetailPopup", result.CardSprite, CreamLight);
        SetCenteredRect(functionalDetailPanel.rectTransform, new Vector2(860f, 1040f), Vector2.zero,
            "Place preview functional detail panel");
        Image functionalIconFrame = CreateImage(functionalDetailPanel.transform, "ItemIconFrame",
            result.GoldButtonSprite, Color.white);
        SetCenteredRect(functionalIconFrame.rectTransform, new Vector2(300f, 300f),
            new Vector2(0f, 285f), "Place preview functional icon frame");
        TMP_Text functionalDetailIcon = CreateText(functionalIconFrame.transform,
            "ItemIconPlaceholder", "불", result.Font, 82f, Brown, FontStyles.Bold);
        SetStretchRect(functionalDetailIcon.rectTransform, new Vector2(28f, 28f),
            new Vector2(-28f, -28f), "Inset preview functional icon");
        TMP_Text functionalDetailName = CreateText(functionalDetailPanel.transform, "ItemName",
            "불꽃 영약", result.Font, 46f, Brown, FontStyles.Bold);
        functionalDetailName.overflowMode = TextOverflowModes.Ellipsis;
        SetCenteredRect(functionalDetailName.rectTransform, new Vector2(720f, 72f),
            new Vector2(0f, 80f), "Place preview functional name");
        TMP_Text functionalDetailDescription = CreateText(functionalDetailPanel.transform,
            "ItemDescription", "불 속성의 힘을 일시적으로 높인다", result.Font, 30f,
            MutedBrown, FontStyles.Normal);
        StylePreviewDetailDescription(functionalDetailDescription);
        Button functionalDetailUseButton = CreateButton(functionalDetailPanel.transform,
            "UseButton", "사용하기", result.GoldButtonSprite, result.Font,
            new Vector2(-190f, -360f), new Vector2(340f, 112f));
        Button functionalDetailCloseButton = CreateButton(functionalDetailPanel.transform,
            "CloseButton", "닫기", result.CreamButtonSprite, result.Font,
            new Vector2(190f, -360f), new Vector2(340f, 112f));
        consumableDetailPanel.gameObject.SetActive(false);
        functionalDetailPanel.gameObject.SetActive(false);
        consumableDetailDim.gameObject.SetActive(false);

        Image enhanceDim = CreateImage(background.transform, "EnhancePreviewDim", null,
            new Color32(0x2B, 0x1C, 0x1D, 0xA8));
        SetStretchRect(enhanceDim.rectTransform, Vector2.zero, Vector2.zero,
            "Stretch preview enhance dim");
        Image enhancePanel = CreateImage(enhanceDim.transform, "EnhanceTap", result.CardSprite,
            CreamLight);
        SetCenteredRect(enhancePanel.rectTransform, new Vector2(900f, 1180f), Vector2.zero,
            "Place preview enhance panel");
        TMP_Text enhanceHeading = CreateText(enhancePanel.transform, "Heading", "장비 강화",
            result.Font, 48f, Brown, FontStyles.Bold);
        SetCenteredRect(enhanceHeading.rectTransform, new Vector2(760f, 70f), new Vector2(0f, 490f),
            "Place preview enhance heading");
        TMP_Text enhanceNotice = CreateText(enhancePanel.transform, "PreviewNotice",
            "PREVIEW 전용 수치 시뮬레이션 · 실제 서버 규칙과 무관", result.Font, 22f,
            MutedBrown, FontStyles.Bold);
        SetCenteredRect(enhanceNotice.rectTransform, new Vector2(780f, 46f), new Vector2(0f, 430f),
            "Place preview enhance notice");

        Image enhanceIconFrame = CreateImage(enhancePanel.transform, "TargetItemIconFrame",
            result.GoldButtonSprite, Color.white);
        SetCenteredRect(enhanceIconFrame.rectTransform, new Vector2(300f, 300f),
            new Vector2(0f, 230f), "Place preview enhance icon frame");
        TMP_Text enhanceIconText = CreateText(enhanceIconFrame.transform, "TargetItemIcon", "검",
            result.Font, 82f, Brown, FontStyles.Bold);
        SetStretchRect(enhanceIconText.rectTransform, new Vector2(30f, 30f), new Vector2(-30f, -30f),
            "Inset preview enhance icon");
        TMP_Text enhanceName = CreateText(enhancePanel.transform, "TargetItemNameText", "새벽의 검",
            result.Font, 43f, Brown, FontStyles.Bold);
        enhanceName.overflowMode = TextOverflowModes.Ellipsis;
        SetCenteredRect(enhanceName.rectTransform, new Vector2(720f, 70f), new Vector2(0f, 35f),
            "Place preview enhance name");
        TMP_Text enhanceLevel = CreateText(enhancePanel.transform, "TargetItemLevelText", "Lv. 3",
            result.Font, 34f, Gold, FontStyles.Bold);
        SetCenteredRect(enhanceLevel.rectTransform, new Vector2(620f, 56f), new Vector2(0f, -30f),
            "Place preview enhance level");

        Image expFrame = CreateImage(enhancePanel.transform, "TargetItemExpRoot",
            result.GoldButtonSprite, Color.white);
        SetCenteredRect(expFrame.rectTransform, new Vector2(740f, 92f), new Vector2(0f, -145f),
            "Place preview EXP frame");
        Image expBase = CreateImage(expFrame.transform, "ExpBarBase", null, ExpBlueDark);
        SetCenteredRect(expBase.rectTransform, new Vector2(690f, 52f), Vector2.zero,
            "Place preview EXP base");
        Image expFill = CreateImage(expBase.transform, "ExpBarFill", null, ExpBlue);
        RectTransform previewFillRect = expFill.rectTransform;
        previewFillRect.anchorMin = new Vector2(0f, 0f);
        previewFillRect.anchorMax = new Vector2(0f, 1f);
        previewFillRect.pivot = new Vector2(0f, 0.5f);
        previewFillRect.offsetMin = Vector2.zero;
        previewFillRect.offsetMax = new Vector2(690f, 0f);
        previewFillRect.localScale = new Vector3(0.35f, 1f, 1f);
        TMP_Text enhanceExpText = CreateText(expFrame.transform, "ExpText", "35 / 100",
            result.Font, 27f, Color.white, FontStyles.Bold);
        SetStretchRect(enhanceExpText.rectTransform, new Vector2(25f, 18f), new Vector2(-25f, -18f),
            "Inset preview EXP text");

        Button enhanceTestButton = CreateButton(enhancePanel.transform, "EnhanceTestButton",
            "강화 테스트", result.GoldButtonSprite, result.Font, new Vector2(0f, -330f),
            new Vector2(720f, 112f));
        Button enhanceCancelButton = CreateButton(enhancePanel.transform, "CancelButton", "돌아가기",
            result.CreamButtonSprite, result.Font, new Vector2(0f, -470f), new Vector2(720f, 100f));
        enhanceDim.gameObject.SetActive(false);

        InventoryMainPreviewController controller = Undo.AddComponent<InventoryMainPreviewController>(root);
        SerializedObject serialized = new SerializedObject(controller);
        SetReference(serialized, "equipmentButton", equipmentButton);
        SetReference(serialized, "consumableButton", consumableButton);
        SetReference(serialized, "equipmentButtonImage", equipmentButton.GetComponent<Image>());
        SetReference(serialized, "consumableButtonImage", consumableButton.GetComponent<Image>());
        SetReference(serialized, "equipmentButtonText", equipmentButton.GetComponentInChildren<TMP_Text>());
        SetReference(serialized, "consumableButtonText", consumableButton.GetComponentInChildren<TMP_Text>());
        SetReference(serialized, "equipmentItemsRoot", equipmentRoot);
        SetReference(serialized, "consumableItemsRoot", consumableRoot);
        SetArray(serialized, "equipmentItemButtons", equipmentItemButtons);
        SetArray(serialized, "consumableItemButtons", consumableItemButtons);
        SetReference(serialized, "scrollRect", scroll);
        SetReference(serialized, "categoryStatusText", status);
        SetReference(serialized, "detailPopupRoot", detailDim.gameObject);
        SetReference(serialized, "detailIconText", detailIconText);
        SetReference(serialized, "detailNameText", detailName);
        SetReference(serialized, "detailDescriptionText", detailDescription);
        SetReference(serialized, "detailTypeText", detailType);
        SetReference(serialized, "detailEquipButton", detailEquipButton);
        SetReference(serialized, "detailEquipButtonImage", detailEquipButton.GetComponent<Image>());
        SetReference(serialized, "detailEquipButtonText", detailEquipButton.GetComponentInChildren<TMP_Text>());
        SetReference(serialized, "detailEnhanceButton", detailEnhanceButton);
        SetReference(serialized, "detailCloseButton", detailCloseButton);
        SetReference(serialized, "enhancePopupRoot", enhanceDim.gameObject);
        SetReference(serialized, "enhanceIconText", enhanceIconText);
        SetReference(serialized, "enhanceNameText", enhanceName);
        SetReference(serialized, "enhanceLevelText", enhanceLevel);
        SetReference(serialized, "enhanceExpText", enhanceExpText);
        SetReference(serialized, "enhanceExpFill", expFill);
        SetReference(serialized, "enhanceTestButton", enhanceTestButton);
        SetReference(serialized, "enhanceCancelButton", enhanceCancelButton);
        SetReference(serialized, "consumableDetailDim", consumableDetailDim.gameObject);
        SetReference(serialized, "consumableDetailPopup", consumableDetailPanel.gameObject);
        SetReference(serialized, "consumableDetailIconText", consumableDetailIcon);
        SetReference(serialized, "consumableDetailNameText", consumableDetailName);
        SetReference(serialized, "consumableDetailDescriptionText", consumableDetailDescription);
        SetReference(serialized, "consumableDetailCloseButton", consumableDetailCloseButton);
        SetReference(serialized, "functionalDetailPopup", functionalDetailPanel.gameObject);
        SetReference(serialized, "functionalDetailIconText", functionalDetailIcon);
        SetReference(serialized, "functionalDetailNameText", functionalDetailName);
        SetReference(serialized, "functionalDetailDescriptionText", functionalDetailDescription);
        SetReference(serialized, "functionalDetailUseButton", functionalDetailUseButton);
        SetReference(serialized, "functionalDetailCloseButton", functionalDetailCloseButton);
        SetReference(serialized, "selectedTabSprite", result.GoldButtonSprite);
        SetReference(serialized, "normalTabSprite", result.CreamButtonSprite);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        root.SetActive(false);
        Dirty(root);
    }

    private static RectTransform CreateContent(Transform parent, string name)
    {
        GameObject item = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(item, "Create inventory preview content");
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        return rect;
    }

    private static GameObject CreateCategoryRoot(RectTransform content, string name)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        Undo.RegisterCreatedObjectUndo(root, "Create inventory preview item group");
        root.transform.SetParent(content, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 18);
        layout.spacing = 30f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        root.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return root;
    }

    private static Button[] CreateCards(Transform parent, string[,] items, ValidationResult result)
    {
        List<Button> buttons = new List<Button>();
        for (int index = 0; index < items.GetLength(0); index++)
        {
            Image card = CreateImage(parent, "ItemSlot_" + (index + 1), result.CardSprite, CreamLight);
            Button cardButton = Undo.AddComponent<Button>(card.gameObject);
            cardButton.targetGraphic = card;
            buttons.Add(cardButton);
            LayoutElement cardLayout = Undo.AddComponent<LayoutElement>(card.gameObject);
            cardLayout.preferredHeight = 160f;
            cardLayout.minHeight = 160f;

            RectTransform textGroup = CreateRectGroup(card.transform, "TextGroup",
                new Vector2(690f, 136f), new Vector2(-88f, 0f));
            TMP_Text name = CreateText(textGroup, "ItemNameText", items[index, 0], result.Font,
                34f, Brown, FontStyles.Bold);
            name.alignment = TextAlignmentOptions.TopLeft;
            name.overflowMode = TextOverflowModes.Ellipsis;
            SetCenteredRect(name.rectTransform, new Vector2(690f, 48f), new Vector2(0f, 36f),
                "Place inventory preview item name");
            TMP_Text description = CreateText(textGroup, "ItemDescribeText", items[index, 1],
                result.Font, 24f, MutedBrown, FontStyles.Normal);
            description.alignment = TextAlignmentOptions.TopLeft;
            description.overflowMode = TextOverflowModes.Ellipsis;
            SetCenteredRect(description.rectTransform, new Vector2(690f, 54f), new Vector2(0f, -25f),
                "Place inventory preview item description");

            Image badge = CreateImage(card.transform, "QuantityOrTypeBackground", result.GoldButtonSprite,
                Color.white);
            SetCenteredRect(badge.rectTransform, new Vector2(150f, 64f), new Vector2(365f, 0f),
                "Place inventory preview item badge");
            TMP_Text badgeText = CreateText(badge.transform, "QuantityOrTypeText", items[index, 2],
                result.Font, 23f, Brown, FontStyles.Bold);
            SetStretchRect(badgeText.rectTransform, new Vector2(12f, 8f), new Vector2(-12f, -8f),
                "Inset inventory preview item badge");

            TMP_Text equipped = CreateText(card.transform, "EquippedText", items[index, 3], result.Font,
                19f, EquippedGreen, FontStyles.Bold);
            equipped.alignment = TextAlignmentOptions.Right;
            SetCenteredRect(equipped.rectTransform, new Vector2(190f, 30f), new Vector2(310f, -52f),
                "Place equipped state");
        }
        return buttons.ToArray();
    }

    private static void StylePreviewDetailDescription(TMP_Text description)
    {
        description.alignment = TextAlignmentOptions.TopLeft;
        description.enableWordWrapping = true;
        description.overflowMode = TextOverflowModes.Ellipsis;
        description.lineSpacing = 8f;
        description.margin = new Vector4(16f, 14f, 16f, 14f);
        SetCenteredRect(description.rectTransform, new Vector2(720f, 250f),
            new Vector2(0f, -95f), "Place preview consumable description");
    }

    private static RectTransform CreateRectGroup(Transform parent, string name, Vector2 size,
        Vector2 position)
    {
        GameObject item = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(item, "Create inventory preview text group");
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        SetCenteredRect(rect, size, position, "Place inventory preview text group");
        return rect;
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(item, "Create inventory preview image");
        item.transform.SetParent(parent, false);
        Image image = item.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        return image;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, TMP_FontAsset font,
        float size, Color color, FontStyles style)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(item, "Create inventory preview text");
        item.transform.SetParent(parent, false);
        TMP_Text text = item.GetComponent<TMP_Text>();
        SetText(text, value, font, size, color, style);
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, Sprite sprite,
        TMP_FontAsset font, Vector2 position, Vector2 size)
    {
        Image image = CreateImage(parent, name, sprite, Color.white);
        Button button = Undo.AddComponent<Button>(image.gameObject);
        button.targetGraphic = image;
        SetCenteredRect(image.rectTransform, size, position, "Place inventory preview button");
        TMP_Text text = CreateText(image.transform, "Text", label, font, 30f, Brown, FontStyles.Bold);
        SetStretchRect(text.rectTransform, new Vector2(18f, 8f), new Vector2(-18f, -8f),
            "Inset inventory preview button text");
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(0xFF, 0xF4, 0xD8, 0xFF);
        colors.pressedColor = new Color32(0xE7, 0xD1, 0xA6, 0xFF);
        colors.selectedColor = new Color32(0xF6, 0xD7, 0x91, 0xFF);
        colors.disabledColor = new Color32(0xA9, 0x9B, 0x88, 0xB8);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.12f;
        button.colors = colors;
        return button;
    }

    private static void IsolateScene(Scene scene, GameObject previewRoot,
        InventoryMainPreviewController controller)
    {
        if (controller == null)
        {
            throw new InvalidOperationException("G Preview Controller를 찾을 수 없습니다.");
        }

        GameObject[] isolatedObjects = scene.GetRootGameObjects()
            .Where(root => root != previewRoot && !IsPreviewInfrastructure(root) && root.activeSelf &&
                           root.GetComponent<EventSystem>() == null &&
                           root.GetComponentInChildren<Camera>(true) == null &&
                           root.GetComponentInChildren<AudioListener>(true) == null)
            .OrderBy(root => root.GetInstanceID())
            .ToArray();
        bool[] objectStates = isolatedObjects.Select(item => item.activeSelf).ToArray();

        string runtimeAssembly = typeof(InventoryManager).Assembly.GetName().Name;
        Behaviour[] behaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true)
            .Where(item => item != null && item != controller && item.gameObject.scene == scene &&
                           !IsPreviewRoot(item.transform.root.gameObject) &&
                           item.GetType().Assembly.GetName().Name == runtimeAssembly)
            .Cast<Behaviour>()
            .OrderBy(item => item.GetInstanceID())
            .ToArray();
        bool[] behaviourStates = behaviours.Select(item => item.enabled).ToArray();

        SerializedObject serialized = new SerializedObject(controller);
        SetArray(serialized, "isolatedGameObjects", isolatedObjects);
        SetBoolArray(serialized, "isolatedGameObjectActiveStates", objectStates);
        SetArray(serialized, "isolatedBehaviours", behaviours);
        SetBoolArray(serialized, "isolatedBehaviourEnabledStates", behaviourStates);
        serialized.ApplyModifiedProperties();
        Dirty(controller);

        foreach (Behaviour behaviour in behaviours)
        {
            if (behaviour.enabled)
            {
                Record(behaviour, "Isolate runtime script for Inventory preview");
                behaviour.enabled = false;
                SavePrefabOverride(behaviour);
            }
        }
        foreach (GameObject target in isolatedObjects)
        {
            SetActive(target, false, "Isolate scene object for Inventory preview");
        }
    }

    private static bool IsPreviewInfrastructure(GameObject root)
    {
        if (root == null)
        {
            return false;
        }

        return root.name == "Canvas" || IsPreviewRoot(root);
    }

    private static bool IsPreviewRoot(GameObject root)
    {
        if (root == null)
        {
            return false;
        }

        return root.name == HabitItemPreviewRootName || root.name == NumberPreviewRootName ||
               root.name == PhotoPreviewRootName || root.name == CharacterPreviewRootName ||
               root.name == PreviewRootName;
    }

    public static void RestoreInventoryPreviewState()
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

        GameObject root = FindRoot(scene, PreviewRootName);
        InventoryMainPreviewController controller =
            root != null ? root.GetComponent<InventoryMainPreviewController>() : null;
        if (root == null || controller == null || (!root.activeSelf && !HasIsolationSnapshot(controller)))
        {
            return;
        }

        RestoreBehaviours(controller);
        RestoreObjects(controller);
        ClearIsolationSnapshot(controller);
        SetActive(root, false, "Restore Inventory preview state");
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void RestoreBehaviours(InventoryMainPreviewController controller)
    {
        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty targets = serialized.FindProperty("isolatedBehaviours");
        SerializedProperty states = serialized.FindProperty("isolatedBehaviourEnabledStates");
        if (targets == null || states == null || targets.arraySize != states.arraySize)
        {
            return;
        }
        for (int index = 0; index < targets.arraySize; index++)
        {
            Behaviour target = targets.GetArrayElementAtIndex(index).objectReferenceValue as Behaviour;
            if (target != null)
            {
                target.enabled = states.GetArrayElementAtIndex(index).boolValue;
                Dirty(target);
                SavePrefabOverride(target);
            }
        }
    }

    private static void RestoreObjects(InventoryMainPreviewController controller)
    {
        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty targets = serialized.FindProperty("isolatedGameObjects");
        SerializedProperty states = serialized.FindProperty("isolatedGameObjectActiveStates");
        if (targets == null || states == null || targets.arraySize != states.arraySize)
        {
            return;
        }
        for (int index = 0; index < targets.arraySize; index++)
        {
            GameObject target = targets.GetArrayElementAtIndex(index).objectReferenceValue as GameObject;
            if (target != null)
            {
                target.SetActive(states.GetArrayElementAtIndex(index).boolValue);
                Dirty(target);
                SavePrefabOverride(target);
            }
        }
    }

    private static bool HasIsolationSnapshot(InventoryMainPreviewController controller)
    {
        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty behaviours = serialized.FindProperty("isolatedBehaviours");
        SerializedProperty objects = serialized.FindProperty("isolatedGameObjects");
        return (behaviours != null && behaviours.arraySize > 0) ||
               (objects != null && objects.arraySize > 0);
    }

    private static void ClearIsolationSnapshot(InventoryMainPreviewController controller)
    {
        SerializedObject serialized = new SerializedObject(controller);
        string[] names =
        {
            "isolatedBehaviours", "isolatedBehaviourEnabledStates",
            "isolatedGameObjects", "isolatedGameObjectActiveStates"
        };
        foreach (string name in names)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null)
            {
                property.arraySize = 0;
            }
        }
        serialized.ApplyModifiedProperties();
        Dirty(controller);
    }

    private static void RestoreWhenAnotherPreviewModeIsSelected()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        GameObject root = scene.path == ScenePath ? FindRoot(scene, PreviewRootName) : null;
        if (root == null || !root.activeSelf)
        {
            return;
        }

        bool otherMode = IsActive(scene, HabitItemPreviewRootName) || IsActive(scene, NumberPreviewRootName) ||
                         IsActive(scene, PhotoPreviewRootName) || IsActive(scene, CharacterPreviewRootName);
        if (otherMode)
        {
            RestoreInventoryPreviewState();
        }
    }

    private static bool IsActive(Scene scene, string rootName)
    {
        GameObject root = FindRoot(scene, rootName);
        return root != null && root.activeSelf;
    }

    private static void RestoreBeforeSceneSave(Scene scene, string path)
    {
        if (scene.path == ScenePath && scene == SceneManager.GetActiveScene())
        {
            RestoreInventoryPreviewState();
        }
    }

    private static List<string> ValidateRenewalLayout(ValidationResult result, bool requirePreview)
    {
        List<string> errors = new List<string>();
        if (!result.IsValid)
        {
            errors.AddRange(result.Errors);
            return errors;
        }
        ValidateTouchTarget(result.EquipmentButton, "EquipmentButton", errors);
        ValidateTouchTarget(result.ConsumableButton, "ConsumableButton", errors);
        ValidateTouchTarget(result.DoEquipButton, "EquipItemDetailPopup/EquipButton", errors);
        ValidateTouchTarget(result.EnhanceButton, "EquipItemDetailPopup/EnhanceButton", errors);
        ValidateTouchTarget(result.EquipCloseButton, "EquipItemDetailPopup/CloseButton", errors);
        ValidateTouchTarget(result.ConsumCloseButton, "ConsumItemDetailPopup/CloseButton", errors);
        ValidateTouchTarget(result.FuncUseButton, "FunctionalItemDetailPopup/UseButton", errors);
        ValidateTouchTarget(result.FuncCloseButton, "FunctionalItemDetailPopup/CloseButton", errors);
        ValidateTouchTarget(result.DoEnhanceButton, "EnhanceButtonBar/EnhanceButton", errors);
        ValidateTouchTarget(result.EnhanceCancelButton, "EnhanceButtonBar/CancelButton", errors);
        if (!result.InventoryScroll.vertical || result.InventoryScroll.horizontal)
        {
            errors.Add("InventoryScroll은 세로 전용 ScrollRect여야 합니다.");
        }
        if (result.Content.parent != result.Viewport)
        {
            errors.Add("InventoryScroll/Viewport/Content 계층이 변경되었습니다.");
        }
        TMP_Text[] tabTexts =
        {
            result.EquipmentButton.GetComponentInChildren<TMP_Text>(true),
            result.ConsumableButton.GetComponentInChildren<TMP_Text>(true)
        };
        foreach (TMP_Text text in tabTexts)
        {
            if (text == null)
            {
                errors.Add("장비/소비 아이템 탭 레이블을 찾을 수 없습니다.");
                continue;
            }
            if (text.font != result.Font)
            {
                errors.Add($"인벤토리 탭 레이블에 NotoSansKR이 적용되지 않았습니다: {text.name}");
            }
        }
        if (result.EquipDetail.sprite != result.CardSprite || result.EquipDetail.type != Image.Type.Sliced)
        {
            errors.Add("장비 상세 팝업에 RPG 카드 Sprite가 적용되지 않았습니다.");
        }
        if (result.EquipName.font != result.Font || result.EquipDescription.font != result.Font)
        {
            errors.Add("장비 상세 팝업 텍스트에 NotoSansKR이 적용되지 않았습니다.");
        }
        if (result.EquipName.fontStyle != FontStyles.Bold)
        {
            errors.Add("장비 상세 팝업 아이템 이름이 Bold가 아닙니다.");
        }
        if (result.ConsumDetail.sprite != result.CardSprite ||
            result.FuncDetail.sprite != result.CardSprite ||
            result.ConsumDetail.type != Image.Type.Sliced || result.FuncDetail.type != Image.Type.Sliced)
        {
            errors.Add("소비/기능성 상세 팝업에 RPG 카드 Sprite가 적용되지 않았습니다.");
        }
        if (result.ConsumName.font != result.Font || result.ConsumDescription.font != result.Font ||
            result.FuncName.font != result.Font || result.FuncDescription.font != result.Font)
        {
            errors.Add("소비/기능성 상세 팝업 텍스트에 NotoSansKR이 적용되지 않았습니다.");
        }
        if (result.ConsumName.fontStyle != FontStyles.Bold || result.FuncName.fontStyle != FontStyles.Bold)
        {
            errors.Add("소비/기능성 상세 팝업 아이템 이름이 Bold가 아닙니다.");
        }
        if (result.PopupDim.color.a < 0.5f || result.PopupDim.color.a > 0.8f)
        {
            errors.Add("장비 상세 팝업 배경 Dim 투명도가 허용 범위를 벗어났습니다.");
        }
        if (result.EnhanceTargetName.font != result.Font || result.EnhanceTargetLevel.font != result.Font ||
            result.EnhanceExpText.font != result.Font)
        {
            errors.Add("강화 대상 이름/레벨/EXP에 NotoSansKR이 적용되지 않았습니다.");
        }
        if (result.EnhanceTap.GetComponent<Image>() == null ||
            result.EnhanceTap.GetComponent<Image>().sprite != result.CardSprite)
        {
            errors.Add("EnhanceTap에 RPG 카드 배경이 적용되지 않았습니다.");
        }
        if (result.EnhanceExpFill.color != ExpBlue || result.EnhanceExpBase.color != ExpBlueDark)
        {
            errors.Add("강화 EXP 게이지에 파란색 RPG 스타일이 적용되지 않았습니다.");
        }
        if (result.EnhanceExpFill.rectTransform.localScale.x < 0f ||
            result.EnhanceExpFill.rectTransform.localScale.x > 1f)
        {
            errors.Add("강화 EXP 게이지 비율이 0~1 범위를 벗어났습니다.");
        }

        if (!requirePreview)
        {
            return errors;
        }
        GameObject root = FindRoot(result.Scene, PreviewRootName);
        InventoryMainPreviewController previewController =
            root != null ? root.GetComponent<InventoryMainPreviewController>() : null;
        if (root == null || previewController == null)
        {
            errors.Add("G Preview 루트 또는 Controller가 없습니다.");
            return errors;
        }
        SerializedObject previewSerialized = new SerializedObject(previewController);
        string[] previewEnhanceReferences =
        {
            "detailEnhanceButton", "enhancePopupRoot", "enhanceIconText", "enhanceNameText",
            "enhanceLevelText", "enhanceExpText", "enhanceExpFill", "enhanceTestButton",
            "enhanceCancelButton"
        };
        foreach (string propertyName in previewEnhanceReferences)
        {
            SerializedProperty property = previewSerialized.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == null)
            {
                errors.Add($"G Preview 강화 바인딩이 비어 있습니다: {propertyName}");
            }
        }
        string[] previewConsumableReferences =
        {
            "consumableItemButtons", "consumableDetailDim", "consumableDetailPopup",
            "consumableDetailIconText", "consumableDetailNameText",
            "consumableDetailDescriptionText", "consumableDetailCloseButton",
            "functionalDetailPopup", "functionalDetailIconText", "functionalDetailNameText",
            "functionalDetailDescriptionText", "functionalDetailUseButton",
            "functionalDetailCloseButton"
        };
        foreach (string propertyName in previewConsumableReferences)
        {
            SerializedProperty property = previewSerialized.FindProperty(propertyName);
            bool missing = property == null ||
                           (property.isArray ? property.arraySize == 0 : property.objectReferenceValue == null);
            if (missing)
            {
                errors.Add($"G Preview 소비 상세 바인딩이 비어 있습니다: {propertyName}");
            }
        }
        Transform equipment = root.transform.Find("PreviewBackground/InventoryScroll/Viewport/Content/EquipmentItems");
        Transform consumables = root.transform.Find("PreviewBackground/InventoryScroll/Viewport/Content/ConsumableItems");
        if (equipment == null || equipment.childCount < 8)
        {
            errors.Add("G Preview 장비 카드가 8개 이상 구성되지 않았습니다.");
        }
        if (consumables == null || consumables.childCount < 10)
        {
            errors.Add("G Preview 소비 아이템 카드가 10개 이상 구성되지 않았습니다.");
        }
        Transform detailPopup = root.transform.Find(
            "PreviewBackground/EquipmentDetailDim/EquipItemDetailPopup");
        if (detailPopup == null || detailPopup.Find("ItemName") == null ||
            detailPopup.Find("ItemDescription") == null || detailPopup.Find("EquipmentType") == null ||
            detailPopup.Find("EquipButton") == null || detailPopup.Find("EnhanceButton") == null ||
            detailPopup.Find("CloseButton") == null)
        {
            errors.Add("G Preview 장비 상세 팝업 구조가 누락되었습니다.");
        }
        Transform enhancePopup = root.transform.Find("PreviewBackground/EnhancePreviewDim/EnhanceTap");
        if (enhancePopup == null || enhancePopup.Find("TargetItemIconFrame/TargetItemIcon") == null ||
            enhancePopup.Find("TargetItemNameText") == null ||
            enhancePopup.Find("TargetItemLevelText") == null ||
            enhancePopup.Find("TargetItemExpRoot/ExpBarBase/ExpBarFill") == null ||
            enhancePopup.Find("TargetItemExpRoot/ExpText") == null ||
            enhancePopup.Find("EnhanceTestButton") == null || enhancePopup.Find("CancelButton") == null)
        {
            errors.Add("G Preview 로컬 강화 화면 구조가 누락되었습니다.");
        }
        Transform consumableDetail = root.transform.Find(
            "PreviewBackground/ConsumableDetailDim/ConsumItemDetailPopup");
        Transform functionalDetail = root.transform.Find(
            "PreviewBackground/ConsumableDetailDim/FunctionalItemDetailPopup");
        if (consumableDetail == null || consumableDetail.Find("ItemIconFrame/ItemIconPlaceholder") == null ||
            consumableDetail.Find("ItemName") == null || consumableDetail.Find("ItemDescription") == null ||
            consumableDetail.Find("CloseButton") == null)
        {
            errors.Add("G Preview 전투 준비 소비 아이템 상세 팝업 구조가 누락되었습니다.");
        }
        if (functionalDetail == null || functionalDetail.Find("ItemIconFrame/ItemIconPlaceholder") == null ||
            functionalDetail.Find("ItemName") == null || functionalDetail.Find("ItemDescription") == null ||
            functionalDetail.Find("UseButton") == null || functionalDetail.Find("CloseButton") == null)
        {
            errors.Add("G Preview 기능성 아이템 상세 팝업 구조가 누락되었습니다.");
        }
        Transform detailDim = root.transform.Find("PreviewBackground/EquipmentDetailDim");
        if (detailDim != null && detailDim.gameObject.activeSelf)
        {
            errors.Add("저장 시 G Preview 장비 상세 팝업은 비활성 상태여야 합니다.");
        }
        Transform enhanceDim = root.transform.Find("PreviewBackground/EnhancePreviewDim");
        if (enhanceDim != null && enhanceDim.gameObject.activeSelf)
        {
            errors.Add("저장 시 G Preview 강화 화면은 비활성 상태여야 합니다.");
        }
        Transform consumableDim = root.transform.Find("PreviewBackground/ConsumableDetailDim");
        if (consumableDim != null && consumableDim.gameObject.activeSelf)
        {
            errors.Add("저장 시 G Preview 소비 상세 팝업은 비활성 상태여야 합니다.");
        }
        ValidateCardTextBounds(equipment, "장비", errors);
        ValidateCardTextBounds(consumables, "소비 아이템", errors);
        if (root.activeSelf)
        {
            errors.Add("저장 시 G Preview 루트는 비활성 상태여야 합니다.");
        }
        return errors;
    }

    private static void ValidateCardTextBounds(Transform category, string categoryName,
        ICollection<string> errors)
    {
        if (category == null)
        {
            return;
        }

        RectTransform categoryRect = category as RectTransform;
        VerticalLayoutGroup categoryLayout = category.GetComponent<VerticalLayoutGroup>();
        ContentSizeFitter categoryFitter = category.GetComponent<ContentSizeFitter>();
        float preferredContentHeight = CalculatePreferredContentHeight(category, categoryLayout);
        float actualContentHeight = categoryRect != null ? categoryRect.rect.height : 0f;
        float contentHeightDelta = Mathf.Abs(actualContentHeight - preferredContentHeight);
        bool validLayout = categoryRect != null && categoryLayout != null && categoryLayout.enabled &&
                           categoryLayout.childControlHeight && categoryFitter != null &&
                           categoryFitter.enabled &&
                           categoryFitter.verticalFit == ContentSizeFitter.FitMode.PreferredSize;
        if (!validLayout || contentHeightDelta > 0.5f)
        {
            errors.Add(
                $"G Preview {categoryName} Content 높이가 레이아웃 계산과 다릅니다: " +
                $"content={category.name}, layoutEnabled={categoryLayout != null && categoryLayout.enabled}, " +
                $"childControlHeight={categoryLayout != null && categoryLayout.childControlHeight}, " +
                $"fitterEnabled={categoryFitter != null && categoryFitter.enabled}, " +
                $"activeSelf={category.gameObject.activeSelf}, activeInHierarchy={category.gameObject.activeInHierarchy}, " +
                $"cards={category.childCount}, actual={actualContentHeight:F1}px, " +
                $"preferred={preferredContentHeight:F1}px, delta={contentHeightDelta:F1}px");
        }

        foreach (Transform card in category)
        {
            RectTransform cardRect = card as RectTransform;
            LayoutElement cardLayout = card.GetComponent<LayoutElement>();
            RectTransform textGroup = card.Find("TextGroup") as RectTransform;
            RectTransform name = card.Find("TextGroup/ItemNameText") as RectTransform;
            RectTransform description = card.Find("TextGroup/ItemDescribeText") as RectTransform;
            RectTransform badge = card.Find("QuantityOrTypeBackground") as RectTransform;
            if (cardRect == null || textGroup == null || name == null || description == null || badge == null)
            {
                errors.Add($"G Preview {categoryName} 카드 텍스트 구조가 누락되었습니다: {card.name}");
                continue;
            }

            float preferredCardHeight = cardLayout != null ? cardLayout.preferredHeight : 0f;
            if (cardLayout == null || Mathf.Abs(cardRect.rect.height - preferredCardHeight) > 0.5f)
            {
                errors.Add(
                    $"G Preview {categoryName} 카드 실제 높이와 LayoutElement가 다릅니다: {card.name}, " +
                    $"actual={cardRect.rect.height:F1}px, preferred={preferredCardHeight:F1}px, " +
                    $"delta={Mathf.Abs(cardRect.rect.height - preferredCardHeight):F1}px");
            }

            Rect cardBounds = cardRect.rect;
            Rect groupBounds = GetRectInLocalSpace(cardRect, textGroup);
            Rect nameBounds = GetRectInLocalSpace(cardRect, name);
            Rect descriptionBounds = GetRectInLocalSpace(cardRect, description);
            Rect badgeBounds = GetRectInLocalSpace(cardRect, badge);
            ValidateContained(card.name, categoryName, "ItemNameText", cardBounds, nameBounds, errors);
            ValidateContained(card.name, categoryName, "ItemDescribeText", cardBounds,
                descriptionBounds, errors);
            if (groupBounds.xMax > badgeBounds.xMin)
            {
                float overlap = groupBounds.xMax - badgeBounds.xMin;
                errors.Add(
                    $"G Preview {categoryName} 카드 텍스트와 오른쪽 태그가 겹칩니다: {card.name}, " +
                    $"TextGroup={FormatRect(groupBounds)}, Tag={FormatRect(badgeBounds)}, " +
                    $"overlap={overlap:F1}px");
            }
        }
    }

    private static float CalculatePreferredContentHeight(Transform category, VerticalLayoutGroup layout)
    {
        if (category == null || layout == null)
        {
            return 0f;
        }

        float height = layout.padding.vertical;
        int activeCardCount = 0;
        foreach (Transform card in category)
        {
            if (!card.gameObject.activeSelf)
            {
                continue;
            }

            LayoutElement cardLayout = card.GetComponent<LayoutElement>();
            RectTransform cardRect = card as RectTransform;
            float cardHeight = cardLayout != null && cardLayout.preferredHeight >= 0f
                ? cardLayout.preferredHeight
                : cardRect != null ? cardRect.rect.height : 0f;
            height += cardHeight;
            activeCardCount++;
        }

        if (activeCardCount > 1)
        {
            height += layout.spacing * (activeCardCount - 1);
        }
        return height;
    }

    private static Rect GetRectInLocalSpace(RectTransform root, RectTransform target)
    {
        Vector3[] worldCorners = new Vector3[4];
        target.GetWorldCorners(worldCorners);
        Vector3 first = root.InverseTransformPoint(worldCorners[0]);
        float xMin = first.x;
        float xMax = first.x;
        float yMin = first.y;
        float yMax = first.y;
        for (int index = 1; index < worldCorners.Length; index++)
        {
            Vector3 point = root.InverseTransformPoint(worldCorners[index]);
            xMin = Mathf.Min(xMin, point.x);
            xMax = Mathf.Max(xMax, point.x);
            yMin = Mathf.Min(yMin, point.y);
            yMax = Mathf.Max(yMax, point.y);
        }
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static void ValidateContained(string cardName, string categoryName, string textName,
        Rect cardBounds, Rect textBounds, ICollection<string> errors)
    {
        const float tolerance = 0.5f;
        float left = Mathf.Max(0f, cardBounds.xMin - textBounds.xMin);
        float right = Mathf.Max(0f, textBounds.xMax - cardBounds.xMax);
        float bottom = Mathf.Max(0f, cardBounds.yMin - textBounds.yMin);
        float top = Mathf.Max(0f, textBounds.yMax - cardBounds.yMax);
        if (left <= tolerance && right <= tolerance && bottom <= tolerance && top <= tolerance)
        {
            return;
        }

        errors.Add(
            $"G Preview {categoryName} 카드 텍스트가 카드 Bounds를 벗어납니다: {cardName}, " +
            $"text={textName}, Card={FormatRect(cardBounds)}, Text={FormatRect(textBounds)}, " +
            $"overflow(L/R/B/T)=({left:F1}/{right:F1}/{bottom:F1}/{top:F1})px");
    }

    private static string FormatRect(Rect rect)
    {
        return $"({rect.xMin:F1}, {rect.yMin:F1})-({rect.xMax:F1}, {rect.yMax:F1})";
    }

    private static void ValidateTouchTarget(Button button, string name, ICollection<string> errors)
    {
        RectTransform rect = button != null ? button.GetComponent<RectTransform>() : null;
        if (rect == null || rect.rect.width < 88f || rect.rect.height < 88f)
        {
            errors.Add($"{name} 터치 영역이 88x88 미만입니다.");
        }
    }

    private static void SetImage(Image image, Sprite sprite, Color color, Image.Type type, string label)
    {
        if (image == null)
        {
            return;
        }
        Record(image, label);
        image.sprite = sprite;
        image.color = color;
        image.type = type;
        Dirty(image);
    }

    private static void SetText(TMP_Text text, string value, TMP_FontAsset font, float size, Color color,
        FontStyles style)
    {
        if (text == null)
        {
            return;
        }
        Record(text, "Style inventory text");
        text.text = value;
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        Dirty(text);
    }

    private static void SetCenteredRect(RectTransform rect, Vector2 size, Vector2 position, string label)
    {
        if (rect == null)
        {
            return;
        }
        Record(rect, label);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Dirty(rect);
    }

    private static void SetTopRect(RectTransform rect, float height, float top, string label)
    {
        if (rect == null)
        {
            return;
        }
        Record(rect, label);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(0f, -top - height);
        rect.offsetMax = new Vector2(0f, -top);
        Dirty(rect);
    }

    private static void SetBottomRect(RectTransform rect, float height, float bottom, string label)
    {
        if (rect == null)
        {
            return;
        }
        Record(rect, label);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(0f, bottom);
        rect.offsetMax = new Vector2(0f, bottom + height);
        Dirty(rect);
    }

    private static void SetStretchRect(RectTransform rect, Vector2 minOffset, Vector2 maxOffset,
        string label)
    {
        if (rect == null)
        {
            return;
        }
        Record(rect, label);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = minOffset;
        rect.offsetMax = maxOffset;
        Dirty(rect);
    }

    private static void SetReference(SerializedObject serialized, string name, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetArray<T>(SerializedObject serialized, string name, T[] values)
        where T : UnityEngine.Object
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null)
        {
            return;
        }
        property.arraySize = values.Length;
        for (int index = 0; index < values.Length; index++)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
    }

    private static void SetBoolArray(SerializedObject serialized, string name, bool[] values)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null)
        {
            return;
        }
        property.arraySize = values.Length;
        for (int index = 0; index < values.Length; index++)
        {
            property.GetArrayElementAtIndex(index).boolValue = values[index];
        }
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene.IsValid()
            ? scene.GetRootGameObjects().FirstOrDefault(candidate => candidate.name == name)
            : null;
    }

    private static void SetActive(GameObject target, bool active, string label)
    {
        if (target == null || target.activeSelf == active)
        {
            return;
        }
        Undo.RecordObject(target, label);
        target.SetActive(active);
        Dirty(target);
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

    private static void Record(UnityEngine.Object target, string label)
    {
        if (target != null)
        {
            Undo.RecordObject(target, label);
        }
    }

    private static void Dirty(UnityEngine.Object target)
    {
        if (target != null)
        {
            EditorUtility.SetDirty(target);
        }
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
        public GameObject InventoryTap;
        public InventoryManager Manager;
        public RectTransform TopButtons;
        public Button EquipmentButton;
        public Button ConsumableButton;
        public ScrollRect InventoryScroll;
        public RectTransform Viewport;
        public RectTransform Content;
        public Image PopupDim;
        public Image EquipDetail;
        public Image EquipIcon;
        public TMP_Text EquipName;
        public TMP_Text EquipDescription;
        public Button DoEquipButton;
        public Button EnhanceButton;
        public Button EquipCloseButton;
        public Image ConsumDetail;
        public Image ConsumIcon;
        public TMP_Text ConsumName;
        public TMP_Text ConsumDescription;
        public Button ConsumCloseButton;
        public Image FuncDetail;
        public Image FuncIcon;
        public TMP_Text FuncName;
        public TMP_Text FuncDescription;
        public Button FuncUseButton;
        public Button FuncCloseButton;
        public GameObject EnhanceTap;
        public TMP_Text EnhanceTargetName;
        public Image EnhanceTargetIcon;
        public TMP_Text EnhanceTargetLevel;
        public RectTransform EnhanceExpRoot;
        public TMP_Text EnhanceExpText;
        public Image EnhanceExpBase;
        public Image EnhanceExpFill;
        public GameObject EnhanceButtonBar;
        public Button DoEnhanceButton;
        public Button EnhanceCancelButton;
        public TMP_FontAsset Font;
        public Sprite CardSprite;
        public Sprite CreamButtonSprite;
        public Sprite GoldButtonSprite;
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }

    private sealed class ProtectionSnapshot
    {
        private readonly string managerJson;
        private readonly int equipmentPersistentCalls;
        private readonly int consumablePersistentCalls;
        private readonly int[] detailPersistentCalls;

        private ProtectionSnapshot(string managerJson, int equipmentPersistentCalls,
            int consumablePersistentCalls, int[] detailPersistentCalls)
        {
            this.managerJson = managerJson;
            this.equipmentPersistentCalls = equipmentPersistentCalls;
            this.consumablePersistentCalls = consumablePersistentCalls;
            this.detailPersistentCalls = detailPersistentCalls;
        }

        public static ProtectionSnapshot Capture(ValidationResult result)
        {
            return new ProtectionSnapshot(
                EditorJsonUtility.ToJson(result.Manager),
                result.EquipmentButton.onClick.GetPersistentEventCount(),
                result.ConsumableButton.onClick.GetPersistentEventCount(),
                new[]
                {
                    result.DoEquipButton.onClick.GetPersistentEventCount(),
                    result.EnhanceButton.onClick.GetPersistentEventCount(),
                    result.EquipCloseButton.onClick.GetPersistentEventCount(),
                    result.ConsumCloseButton.onClick.GetPersistentEventCount(),
                    result.FuncUseButton.onClick.GetPersistentEventCount(),
                    result.FuncCloseButton.onClick.GetPersistentEventCount(),
                    result.DoEnhanceButton.onClick.GetPersistentEventCount(),
                    result.EnhanceCancelButton.onClick.GetPersistentEventCount()
                });
        }

        public void AssertUnchanged(ValidationResult result)
        {
            if (managerJson != EditorJsonUtility.ToJson(result.Manager))
            {
                throw new InvalidOperationException("InventoryManager SerializeField가 Apply 중 변경되었습니다.");
            }
            if (equipmentPersistentCalls != result.EquipmentButton.onClick.GetPersistentEventCount() ||
                consumablePersistentCalls != result.ConsumableButton.onClick.GetPersistentEventCount())
            {
                throw new InvalidOperationException("인벤토리 탭 버튼 UnityEvent가 Apply 중 변경되었습니다.");
            }
            int[] currentDetailCalls =
            {
                result.DoEquipButton.onClick.GetPersistentEventCount(),
                result.EnhanceButton.onClick.GetPersistentEventCount(),
                result.EquipCloseButton.onClick.GetPersistentEventCount(),
                result.ConsumCloseButton.onClick.GetPersistentEventCount(),
                result.FuncUseButton.onClick.GetPersistentEventCount(),
                result.FuncCloseButton.onClick.GetPersistentEventCount(),
                result.DoEnhanceButton.onClick.GetPersistentEventCount(),
                result.EnhanceCancelButton.onClick.GetPersistentEventCount()
            };
            if (!detailPersistentCalls.SequenceEqual(currentDetailCalls))
            {
                throw new InvalidOperationException("상세 팝업 UnityEvent가 Apply 중 변경되었습니다.");
            }
        }
    }
}
