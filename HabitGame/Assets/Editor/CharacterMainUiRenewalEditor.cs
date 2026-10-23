using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CharacterMainUiRenewalEditor
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
    private const string BarSpritePath =
        "Assets/Skyden_Games/Free_Casual_GUI/Demo/Sprites/Bars/Bar.png";
    private const string BarFillSpritePath =
        "Assets/Skyden_Games/Free_Casual_GUI/Demo/Sprites/Bars/Bar_Fill_02.png";
    private const string TitleSlotPrefabPath = "Assets/Resources/Prefabs/TitleSlot.prefab";
    private const string AchievementSlotPrefabPath = "Assets/Resources/Prefabs/AchievementSlot.prefab";
    private const string PreviewRootName = "CharacterMainPreviewRoot";
    private const string HabitItemPreviewRootName = "HabitItemPreviewTestRoot";
    private const string NumberPreviewRootName = "NumberOverlayPreviewRoot";
    private const string PhotoPreviewRootName = "PhotoVerificationPreviewRoot";
    private const string ValidateMenu = "Tools/HabitPVP/UI Renewal/Validate Character Main Renewal";
    private const string ApplyMenu = "Tools/HabitPVP/UI Renewal/Apply Character Main Renewal";
    private const string PreviewMenu =
        "Tools/HabitPVP/UI Renewal/Preview Mode/F. Character Main Renewal";

    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 CreamLight = new Color32(0xFF, 0xF9, 0xEA, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x86, 0x68, 0x5D, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);
    private static readonly Color32 Fire = new Color32(0xD9, 0x5B, 0x3D, 0xFF);
    private static readonly Color32 Water = new Color32(0x45, 0x91, 0xC9, 0xFF);
    private static readonly Color32 Grass = new Color32(0x65, 0xA8, 0x53, 0xFF);
    private static readonly Color32 Aurora = new Color32(0x9A, 0x68, 0xB5, 0xFF);
    private static readonly string[] ManagerReferenceNames =
    {
        "equippedTitleImage", "nameText", "atkStatText", "defStatText", "hpStatText",
        "spdStatText", "critStatText", "fireExpFill", "waterExpFill", "grassExpFill",
        "auroraExpFill", "fireLevelText", "fireEXPText", "waterLevelText", "waterEXPText",
        "grassLevelText", "grassEXPText", "auroraLevelText", "auroraEXPText", "titleManager"
    };

    static CharacterMainUiRenewalEditor()
    {
        EditorApplication.update -= RestoreWhenAnotherPreviewModeIsSelected;
        EditorApplication.update += RestoreWhenAnotherPreviewModeIsSelected;
        EditorSceneManager.sceneSaving -= RestoreBeforeSceneSave;
        EditorSceneManager.sceneSaving += RestoreBeforeSceneSave;
    }

    [MenuItem(ValidateMenu, false, 2070)]
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
                Debug.LogError("[HabitPVP Character Main] " + error, result.CharacterTap);
            }

            return;
        }

        Debug.Log(
            "[HabitPVP Character Main] 검증 성공. CharacterUIManager 19개 표시 참조, 5개 능력치, " +
            "4개 속성 EXP, 칭호/업적 팝업 연결, 기존 UnityEvent 상태, NotoSansKR, " +
            "1080x1920 배치 및 F Preview 격리가 정상입니다.",
            result.CharacterTap);
    }

    [MenuItem(ApplyMenu, false, 2071)]
    private static void Apply()
    {
        ValidationResult result = ValidateScene(true);
        if (!result.IsValid)
        {
            Debug.LogError("[HabitPVP Character Main] Apply 중단: 구조 검증 오류를 먼저 해결해주세요.");
            return;
        }

        ProtectionSnapshot snapshot = ProtectionSnapshot.Capture(result);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Character Main UI Renewal");

        try
        {
            ApplyCharacterStyle(result);
            ApplyTitlePopupStyle(result);
            ApplyTitleSlotStyle(result);
            ApplyAchievementPopupStyle(result);
            ApplyAchievementSlotStyle(result);
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

            Selection.activeGameObject = result.CharacterTap;
            Debug.Log(
                "[HabitPVP Character Main] 적용 성공. UITestScene의 CharacterTap Prefab Instance Override와 " +
                "칭호/업적 팝업 Override, TitleSlot/AchievementSlot.prefab 스타일 및 F Preview 전용 루트를 저장했습니다. " +
                "Canvas.prefab, MainScene, CharacterUIManager, " +
                "Manager/API/DTO 및 게임 로직은 변경하지 않았습니다.", result.CharacterTap);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            EditorSceneManager.MarkSceneDirty(result.Scene);
            Debug.LogException(exception);
            Debug.LogError("[HabitPVP Character Main] Apply 실패. 이번 실행의 변경을 Undo로 되돌렸습니다.");
        }
    }

    [MenuItem(ApplyMenu, true)]
    private static bool CanApply() => CanUseSceneMenus();

    [MenuItem(PreviewMenu, false, 2072)]
    private static void ActivatePreview()
    {
        SpendingReportUiRenewalEditor.RestoreSpendingPreviewState();
        PvpRankingUiRenewalEditor.RestorePvpPreviewState();
        ShopGachaUiRenewalEditor.RestoreShopPreviewState();
        InventoryMainUiRenewalEditor.RestoreInventoryPreviewState();
        RestoreCharacterPreviewState();
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
                "[HabitPVP Character Main] F Preview 루트가 없습니다. " +
                "Apply Character Main Renewal을 먼저 실행해주세요.");
            return;
        }

        SetActive(FindRoot(result.Scene, HabitItemPreviewRootName), false,
            "Hide HabitItem preview for F mode");
        SetActive(FindRoot(result.Scene, NumberPreviewRootName), false,
            "Hide NUMBER preview for F mode");
        SetActive(FindRoot(result.Scene, PhotoPreviewRootName), false,
            "Hide PHOTO preview for F mode");

        CharacterMainPreviewController controller =
            previewRoot.GetComponent<CharacterMainPreviewController>();
        SetEnabled(controller, true, "Enable character main preview controller");
        IsolateScene(result.Scene, previewRoot, controller);
        SetActive(previewRoot, true, "Activate character main preview");
        EditorSceneManager.MarkSceneDirty(result.Scene);
        Selection.activeGameObject = previewRoot;
        Debug.Log(
            "[HabitPVP Character Main] F 모드: 독립 Character Main UI를 가상 데이터로 표시합니다. " +
            "기본/높은 능력치/칭호 미장착 상태와 칭호 팝업의 장착/해제를 Play Mode에서 확인할 수 있으며 " +
            "CharacterManager, InventoryManager, TitleManager 및 API를 호출하지 않습니다. " +
            "A/B/C/D/E 메뉴 선택 시 격리 상태를 복원합니다.", previewRoot);
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

        Transform characterTap = result.Canvas.transform.Find("CharacterTap");
        if (characterTap == null)
        {
            result.Errors.Add("Canvas/CharacterTap을 찾을 수 없습니다.");
            return FinishValidation(result, log);
        }

        result.CharacterTap = characterTap.gameObject;
        CharacterUIManager[] managers = UnityEngine.Object.FindObjectsOfType<CharacterUIManager>(true)
            .Where(item => item.gameObject.scene == result.Scene)
            .ToArray();
        if (managers.Length != 1)
        {
            result.Errors.Add($"CharacterTap의 CharacterUIManager가 정확히 하나여야 합니다. 현재: {managers.Length}");
            return FinishValidation(result, log);
        }

        result.Manager = managers[0];
        TitleManager[] titleManagers = UnityEngine.Object.FindObjectsOfType<TitleManager>(true)
            .Where(item => item.gameObject.scene == result.Scene)
            .ToArray();
        if (titleManagers.Length != 1)
        {
            result.Errors.Add($"TitleManager가 정확히 하나여야 합니다. 현재: {titleManagers.Length}");
            return FinishValidation(result, log);
        }

        result.TitleManager = titleManagers[0];
        AchievementManager[] achievementManagers = UnityEngine.Object.FindObjectsOfType<AchievementManager>(true)
            .Where(item => item.gameObject.scene == result.Scene)
            .ToArray();
        if (achievementManagers.Length != 1)
        {
            result.Errors.Add($"AchievementManager가 정확히 하나여야 합니다. 현재: {achievementManagers.Length}");
            return FinishValidation(result, log);
        }

        result.AchievementManager = achievementManagers[0];
        result.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        result.CardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardSpritePath);
        result.CreamButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CreamButtonSpritePath);
        result.GoldButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GoldButtonSpritePath);
        result.BarSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BarSpritePath);
        result.BarFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BarFillSpritePath);
        if (result.Font == null || result.CardSprite == null || result.CreamButtonSprite == null ||
            result.GoldButtonSprite == null || result.BarSprite == null || result.BarFillSprite == null)
        {
            result.Errors.Add("Character Main Renewal에 필요한 폰트 또는 Free Casual GUI Sprite가 없습니다.");
        }

        SerializedObject manager = new SerializedObject(result.Manager);
        foreach (string propertyName in ManagerReferenceNames.Where(name => name != "titleManager"))
        {
            SerializedProperty property = manager.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == null)
            {
                result.Errors.Add($"CharacterUIManager 참조가 비어 있습니다: {propertyName}");
            }
        }

        result.NameText = Find<TMP_Text>(characterTap, "NameText", result.Errors);
        result.TitleImage = Find<Image>(characterTap, "TitleImage", result.Errors);
        result.TitleButton = Find<Button>(characterTap, "TitleButton", result.Errors);
        result.AchievementButton = Find<Button>(characterTap, "AchievementButton", result.Errors);
        result.StatusBackground = Find<Image>(characterTap, "StatusBackgroundImage", result.Errors);
        result.StatusBoxes = Find<RectTransform>(characterTap, "StatusBackgroundImage/Content/StatusBoxes", result.Errors);
        result.AttributeBars = Find<RectTransform>(characterTap,
            "StatusBackgroundImage/Content/AttributeLevelBars", result.Errors);
        result.InventoryButton = Find<Button>(characterTap,
            "StatusBackgroundImage/Content/InventoryButton", result.Errors);

        SerializedObject titleManager = new SerializedObject(result.TitleManager);
        result.TitlePopup = GetReference<GameObject>(titleManager, "titlePopup", result.Errors);
        result.TitleCloseButton = GetReference<Button>(titleManager, "closeButton", result.Errors);
        result.TitleSlotParent = GetReference<Transform>(titleManager, "slotParent", result.Errors);
        result.TitleScrollRect = result.TitleSlotParent != null
            ? result.TitleSlotParent.GetComponentInParent<ScrollRect>(true)
            : null;
        if (result.TitleScrollRect == null)
        {
            result.Errors.Add("칭호 목록 ScrollRect를 찾을 수 없습니다.");
        }

        SerializedObject achievementManager = new SerializedObject(result.AchievementManager);
        result.AchievementPopup = GetReference<GameObject>(achievementManager, "achievementPopup", result.Errors);
        result.AchievementCloseButton = GetReference<Button>(achievementManager, "closeButton", result.Errors);
        result.AchievementSlotParent = GetReference<Transform>(achievementManager, "slotParent", result.Errors);
        result.AchievementSlotPrefab = GetReference<AchievementSlotUI>(achievementManager, "slotPrefab", result.Errors);
        result.AchievementScrollRect = result.AchievementSlotParent != null
            ? result.AchievementSlotParent.GetComponentInParent<ScrollRect>(true)
            : null;
        if (result.AchievementScrollRect == null)
        {
            result.Errors.Add("업적 목록 ScrollRect를 찾을 수 없습니다.");
        }

        string[] statBoxes = { "AtkBox", "DefBox", "HpBox", "SpdBox", "CritBox" };
        foreach (string box in statBoxes)
        {
            Image image = Find<Image>(characterTap,
                $"StatusBackgroundImage/Content/StatusBoxes/{box}", result.Errors);
            if (image != null)
            {
                result.StatCards.Add(image);
            }
        }

        string[] attributeRows = { "FireLevelBar", "WaterLevelBar", "GrassLevelBar", "AuroraLevelBar" };
        foreach (string row in attributeRows)
        {
            Image image = Find<Image>(characterTap,
                $"StatusBackgroundImage/Content/AttributeLevelBars/{row}", result.Errors);
            if (image != null)
            {
                result.AttributeCards.Add(image);
            }
        }

        return FinishValidation(result, log);
    }

    private static ValidationResult FinishValidation(ValidationResult result, bool log)
    {
        if (log)
        {
            foreach (string error in result.Errors)
            {
                Debug.LogError("[HabitPVP Character Main] " + error,
                    result.CharacterTap != null ? result.CharacterTap : result.Canvas);
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

    private static T GetReference<T>(SerializedObject serialized, string name,
        ICollection<string> errors) where T : UnityEngine.Object
    {
        T value = serialized.FindProperty(name)?.objectReferenceValue as T;
        if (value == null)
        {
            string owner = serialized.targetObject != null
                ? serialized.targetObject.GetType().Name
                : "Manager";
            errors.Add($"{owner} 참조가 비어 있습니다: {name}");
        }

        return value;
    }

    private static void ApplyCharacterStyle(ValidationResult result)
    {
        Image characterBackground = result.CharacterTap.GetComponent<Image>();
        if (characterBackground != null)
        {
            SetImage(characterBackground, null, Cream, Image.Type.Simple, "Style character background");
        }

        SetStretchRect(result.CharacterTap.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero,
            "Stretch CharacterTap");
        SetText(result.NameText, result.Font, 52f, Brown, TextAlignmentOptions.Center, FontStyles.Bold,
            "Style nickname");
        SetCenteredRect(result.NameText.rectTransform, new Vector2(920f, 72f), new Vector2(0f, 805f),
            "Place nickname");

        SetCenteredRect(result.TitleImage.rectTransform, new Vector2(560f, 92f), new Vector2(0f, 706f),
            "Place equipped title");
        result.TitleImage.preserveAspect = true;
        Dirty(result.TitleImage);

        StyleMainButton(result.TitleButton, result.GoldButtonSprite, result.Font, new Vector2(-260f, 584f),
            "칭호", "Style title button");
        StyleMainButton(result.AchievementButton, result.CreamButtonSprite, result.Font,
            new Vector2(260f, 584f), "업적", "Style achievement button");

        SetImage(result.StatusBackground, result.CardSprite, CreamLight, Image.Type.Sliced,
            "Style status background");
        SetCenteredRect(result.StatusBackground.rectTransform, new Vector2(1000f, 1270f),
            new Vector2(0f, -145f), "Place status background");
        RectTransform content = result.StatusBackground.transform.Find("Content") as RectTransform;
        SetStretchRect(content, new Vector2(36f, 32f), new Vector2(-36f, -32f), "Inset status content");

        TMP_Text statusHeading = content != null ? content.Find("StatusText")?.GetComponent<TMP_Text>() : null;
        TMP_Text attributeHeading = content != null
            ? content.Find("AttributeLevelText")?.GetComponent<TMP_Text>()
            : null;
        SetText(statusHeading, result.Font, 34f, Brown, TextAlignmentOptions.Left, FontStyles.Bold,
            "Style stat heading");
        SetCenteredRect(statusHeading?.rectTransform, new Vector2(880f, 54f), new Vector2(0f, 548f),
            "Place stat heading");
        SetText(attributeHeading, result.Font, 34f, Brown, TextAlignmentOptions.Left, FontStyles.Bold,
            "Style attribute heading");
        SetCenteredRect(attributeHeading?.rectTransform, new Vector2(880f, 54f), new Vector2(0f, 108f),
            "Place attribute heading");

        SetStretchRect(result.StatusBoxes, Vector2.zero, Vector2.zero, "Stretch stat box container");
        Vector2[] statPositions =
        {
            new Vector2(-294f, 426f), new Vector2(0f, 426f), new Vector2(294f, 426f),
            new Vector2(-147f, 262f), new Vector2(147f, 262f)
        };
        for (int index = 0; index < result.StatCards.Count; index++)
        {
            StyleStatCard(result.StatCards[index], result.CardSprite, result.Font, statPositions[index]);
        }

        SetStretchRect(result.AttributeBars, Vector2.zero, Vector2.zero, "Stretch attribute container");
        float[] attributeY = { 25f, -98f, -221f, -344f };
        Color32[] attributeColors = { Fire, Water, Grass, Aurora };
        for (int index = 0; index < result.AttributeCards.Count; index++)
        {
            StyleAttributeCard(result.AttributeCards[index], result.CardSprite, result.BarSprite,
                result.BarFillSprite, result.Font, attributeY[index], attributeColors[index]);
        }

        StyleMainButton(result.InventoryButton, result.GoldButtonSprite, result.Font,
            new Vector2(0f, -512f), "인벤토리", "Style inventory button", new Vector2(520f, 108f));

        foreach (TMP_Text text in GetTargetTexts(result))
        {
            Record(text, "Apply NotoSansKR to CharacterTap");
            text.font = result.Font;
            Dirty(text);
        }
    }

    private static void ApplyTitlePopupStyle(ValidationResult result)
    {
        Image popupImage = result.TitlePopup != null ? result.TitlePopup.GetComponent<Image>() : null;
        SetImage(popupImage, result.CardSprite, Cream, Image.Type.Sliced, "Style title popup");
        SetCenteredRect(result.TitlePopup != null ? result.TitlePopup.GetComponent<RectTransform>() : null,
            new Vector2(980f, 1420f), Vector2.zero, "Place title popup");

        if (result.TitleScrollRect != null)
        {
            Image scrollBackground = result.TitleScrollRect.GetComponent<Image>();
            SetImage(scrollBackground, result.CardSprite, CreamLight, Image.Type.Sliced,
                "Style title scroll background");
            SetCenteredRect(result.TitleScrollRect.GetComponent<RectTransform>(), new Vector2(860f, 1120f),
                new Vector2(0f, 70f), "Place title scroll view");
            result.TitleScrollRect.horizontal = false;
            result.TitleScrollRect.vertical = true;
            result.TitleScrollRect.movementType = ScrollRect.MovementType.Clamped;
            Dirty(result.TitleScrollRect);
        }

        VerticalLayoutGroup vertical = result.TitleSlotParent != null
            ? result.TitleSlotParent.GetComponent<VerticalLayoutGroup>()
            : null;
        if (vertical != null)
        {
            Record(vertical, "Style title list layout");
            vertical.padding = new RectOffset(24, 24, 24, 24);
            vertical.spacing = 20f;
            vertical.childControlWidth = true;
            vertical.childControlHeight = false;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;
            Dirty(vertical);
        }

        StyleMainButton(result.TitleCloseButton, result.GoldButtonSprite, result.Font,
            new Vector2(0f, -620f), "닫기", "Style title popup close button", new Vector2(360f, 96f));
        foreach (TMP_Text text in result.TitlePopup.GetComponentsInChildren<TMP_Text>(true))
        {
            SetText(text, result.Font, Mathf.Max(24f, text.fontSize), Brown, text.alignment,
                text.fontStyle, "Style title popup text");
        }
    }

    private static void ApplyTitleSlotStyle(ValidationResult result)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(TitleSlotPrefabPath);
        try
        {
            TitleSlotUI slot = root.GetComponent<TitleSlotUI>();
            if (slot == null)
            {
                throw new InvalidOperationException("TitleSlot.prefab에서 TitleSlotUI를 찾을 수 없습니다.");
            }

            SerializedObject serialized = new SerializedObject(slot);
            Image badge = serialized.FindProperty("titleBadgeImage")?.objectReferenceValue as Image;
            Image border = serialized.FindProperty("borderImage")?.objectReferenceValue as Image;
            Image card = root.GetComponent<Image>();
            SetImage(card, result.CardSprite, CreamLight, Image.Type.Sliced, "Style title slot card");
            SetCenteredRect(root.GetComponent<RectTransform>(), new Vector2(760f, 170f), Vector2.zero,
                "Size title slot");
            SetCenteredRect(badge != null ? badge.rectTransform : null, new Vector2(650f, 112f), Vector2.zero,
                "Place title badge");
            if (badge != null)
            {
                badge.preserveAspect = true;
                Dirty(badge);
            }
            SetImage(border, result.GoldButtonSprite, Color.white, Image.Type.Sliced,
                "Style equipped title border");
            SetStretchRect(border != null ? border.rectTransform : null, new Vector2(-8f, -8f),
                new Vector2(8f, 8f), "Place equipped title border");
            PrefabUtility.SaveAsPrefabAsset(root, TitleSlotPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ApplyAchievementPopupStyle(ValidationResult result)
    {
        Image popupImage = result.AchievementPopup != null
            ? result.AchievementPopup.GetComponent<Image>()
            : null;
        SetImage(popupImage, result.CardSprite, Cream, Image.Type.Sliced, "Style achievement popup");
        SetCenteredRect(result.AchievementPopup != null
                ? result.AchievementPopup.GetComponent<RectTransform>()
                : null,
            new Vector2(980f, 1420f), Vector2.zero, "Place achievement popup");

        if (result.AchievementScrollRect != null)
        {
            SetImage(result.AchievementScrollRect.GetComponent<Image>(), result.CardSprite, CreamLight,
                Image.Type.Sliced, "Style achievement scroll background");
            SetCenteredRect(result.AchievementScrollRect.GetComponent<RectTransform>(),
                new Vector2(860f, 1120f), new Vector2(0f, 70f), "Place achievement scroll view");
            result.AchievementScrollRect.horizontal = false;
            result.AchievementScrollRect.vertical = true;
            result.AchievementScrollRect.movementType = ScrollRect.MovementType.Clamped;
            Dirty(result.AchievementScrollRect);
        }

        VerticalLayoutGroup vertical = result.AchievementSlotParent != null
            ? result.AchievementSlotParent.GetComponent<VerticalLayoutGroup>()
            : null;
        if (vertical != null)
        {
            Record(vertical, "Style achievement list layout");
            vertical.padding = new RectOffset(24, 24, 24, 24);
            vertical.spacing = 30f;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;
            Dirty(vertical);
        }

        StyleMainButton(result.AchievementCloseButton, result.GoldButtonSprite, result.Font,
            new Vector2(0f, -620f), "닫기", "Style achievement popup close button",
            new Vector2(360f, 96f));
        foreach (TMP_Text text in result.AchievementPopup.GetComponentsInChildren<TMP_Text>(true))
        {
            SetText(text, result.Font, Mathf.Max(24f, text.fontSize), Brown, text.alignment,
                text.fontStyle, "Style achievement popup text");
        }
    }

    private static void ApplyAchievementSlotStyle(ValidationResult result)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(AchievementSlotPrefabPath);
        try
        {
            AchievementSlotUI slot = root.GetComponent<AchievementSlotUI>();
            if (slot == null)
            {
                throw new InvalidOperationException("AchievementSlot.prefab에서 AchievementSlotUI를 찾을 수 없습니다.");
            }

            SerializedObject serialized = new SerializedObject(slot);
            TMP_Text content = serialized.FindProperty("titleText")?.objectReferenceValue as TMP_Text;
            Image progressFill = serialized.FindProperty("progressBarFill")?.objectReferenceValue as Image;
            TMP_Text progressText = serialized.FindProperty("progressText")?.objectReferenceValue as TMP_Text;
            Button checkButton = serialized.FindProperty("checkRewardButton")?.objectReferenceValue as Button;
            Button claimButton = serialized.FindProperty("rewardClaimButton")?.objectReferenceValue as Button;
            TMP_Text claimText = serialized.FindProperty("rewardClaimText")?.objectReferenceValue as TMP_Text;
            Image progressBase = progressFill != null
                ? progressFill.transform.parent.GetComponent<Image>()
                : null;

            SetImage(root.GetComponent<Image>(), result.CardSprite, CreamLight, Image.Type.Sliced,
                "Style achievement slot card");
            SetCenteredRect(root.GetComponent<RectTransform>(), new Vector2(800f, 270f), Vector2.zero,
                "Size achievement slot");
            LayoutElement layout = root.GetComponent<LayoutElement>();
            if (layout != null)
            {
                Record(layout, "Size achievement slot layout");
                layout.minHeight = 270f;
                layout.preferredHeight = 270f;
                Dirty(layout);
            }

            SetText(content, result.Font, 39f, Brown, TextAlignmentOptions.Left, FontStyles.Bold,
                "Style achievement content");
            SetCenteredRect(content?.rectTransform, new Vector2(560f, 100f), new Vector2(-80f, 68f),
                "Place achievement content");
            if (content != null)
            {
                Record(content, "Constrain achievement content");
                content.enableWordWrapping = true;
                content.overflowMode = TextOverflowModes.Ellipsis;
                content.maxVisibleLines = 2;
                content.fontSizeMin = 34f;
                content.fontSizeMax = 39f;
                Dirty(content);
            }
            SetImage(progressBase, result.BarSprite, Color.white, Image.Type.Sliced,
                "Style achievement progress track");
            SetCenteredRect(progressBase?.rectTransform, new Vector2(470f, 44f),
                new Vector2(-120f, -62f), "Place achievement progress track");
            SetImage(progressFill, result.BarFillSprite, Gold, Image.Type.Sliced,
                "Style achievement progress fill");
            SetStretchRect(progressFill?.rectTransform, new Vector2(8f, 7f), new Vector2(-8f, -7f),
                "Inset achievement progress fill");
            SetText(progressText, result.Font, 30f, Brown, TextAlignmentOptions.Center, FontStyles.Bold,
                "Style achievement progress text");
            SetCenteredRect(progressText?.rectTransform, new Vector2(430f, 42f),
                new Vector2(-120f, -62f), "Place achievement progress text");
            StyleMainButton(checkButton, result.CreamButtonSprite, result.Font,
                new Vector2(290f, 62f), "보상 확인", "Style achievement reward preview",
                new Vector2(160f, 64f));
            StyleMainButton(claimButton, result.GoldButtonSprite, result.Font,
                new Vector2(270f, -62f), "보상 수령", "Style achievement claim button",
                new Vector2(200f, 82f));
            SetText(claimText, result.Font, 23f, Brown, TextAlignmentOptions.Center, FontStyles.Bold,
                "Style achievement claim text");
            PrefabUtility.SaveAsPrefabAsset(root, AchievementSlotPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void StyleStatCard(Image card, Sprite sprite, TMP_FontAsset font, Vector2 position)
    {
        SetImage(card, sprite, CreamLight, Image.Type.Sliced, "Style stat card");
        SetCenteredRect(card.rectTransform, new Vector2(270f, 140f), position, "Place stat card");
        TMP_Text[] texts = card.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            bool isValue = text.name.EndsWith("StatusText", StringComparison.Ordinal);
            SetText(text, font, isValue ? 34f : 25f, isValue ? Brown : MutedBrown,
                TextAlignmentOptions.Center, isValue ? FontStyles.Bold : FontStyles.Normal,
                "Style stat card text");
            SetCenteredRect(text.rectTransform, new Vector2(230f, isValue ? 50f : 38f),
                new Vector2(0f, isValue ? -22f : 34f), "Place stat card text");
        }
    }

    private static void StyleAttributeCard(Image card, Sprite cardSprite, Sprite barSprite,
        Sprite fillSprite, TMP_FontAsset font, float y, Color32 accent)
    {
        SetImage(card, cardSprite, CreamLight, Image.Type.Sliced, "Style attribute card");
        SetCenteredRect(card.rectTransform, new Vector2(900f, 108f), new Vector2(0f, y),
            "Place attribute card");

        string prefix = card.name.Replace("LevelBar", string.Empty);
        TMP_Text name = card.transform.Find(prefix + "Text")?.GetComponent<TMP_Text>();
        TMP_Text level = card.transform.Find(prefix + "LevelText")?.GetComponent<TMP_Text>();
        TMP_Text exp = card.transform.Find(prefix + "EXPText")?.GetComponent<TMP_Text>();
        Image bar = card.transform.Find("EXPBarBase")?.GetComponent<Image>();
        Image fill = card.transform.Find(prefix + "EXPBarFill")?.GetComponent<Image>();
        Image icon = card.GetComponentsInChildren<Image>(true)
            .FirstOrDefault(item => item != card && item != bar && item != fill);

        SetText(name, font, 26f, accent, TextAlignmentOptions.Left, FontStyles.Bold,
            "Style attribute name");
        SetCenteredRect(name?.rectTransform, new Vector2(160f, 42f), new Vector2(-294f, 18f),
            "Place attribute name");
        SetText(level, font, 23f, Brown, TextAlignmentOptions.Left, FontStyles.Bold,
            "Style attribute level");
        SetCenteredRect(level?.rectTransform, new Vector2(150f, 36f), new Vector2(-294f, -24f),
            "Place attribute level");
        SetText(exp, font, 21f, Brown, TextAlignmentOptions.Center, FontStyles.Normal,
            "Style attribute exp");
        SetCenteredRect(exp?.rectTransform, new Vector2(460f, 32f), new Vector2(150f, -24f),
            "Place attribute exp");

        if (icon != null)
        {
            SetCenteredRect(icon.rectTransform, new Vector2(64f, 64f), new Vector2(-400f, 0f),
                "Place attribute icon");
            icon.preserveAspect = true;
            Dirty(icon);
        }

        if (bar != null)
        {
            SetImage(bar, barSprite, Color.white, Image.Type.Sliced, "Style attribute exp track");
            SetCenteredRect(bar.rectTransform, new Vector2(500f, 36f), new Vector2(150f, 17f),
                "Place attribute exp track");
        }

        if (fill != null)
        {
            SetImage(fill, fillSprite, accent, Image.Type.Sliced, "Style attribute exp fill");
            SetRect(fill.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(-96f, 0f), new Vector2(482f, 26f), new Vector2(0f, 0.5f),
                "Place attribute exp fill");
        }
    }

    private static void StyleMainButton(Button button, Sprite sprite, TMP_FontAsset font,
        Vector2 position, string fallbackLabel, string label, Vector2? size = null)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        SetImage(image, sprite, Color.white, Image.Type.Sliced, label);
        SetCenteredRect(button.GetComponent<RectTransform>(), size ?? new Vector2(430f, 104f), position, label);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null && string.IsNullOrWhiteSpace(text.text))
        {
            Record(text, label);
            text.text = fallbackLabel;
        }

        SetText(text, font, 31f, Brown, TextAlignmentOptions.Center, FontStyles.Bold, label);
        SetStretchRect(text?.rectTransform, new Vector2(22f, 10f), new Vector2(-22f, -10f), label);
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(0xFF, 0xF5, 0xD5, 0xFF);
        colors.pressedColor = new Color32(0xDD, 0xC7, 0x9A, 0xFF);
        colors.disabledColor = new Color32(0xB9, 0xAA, 0x91, 0xB8);
        Record(button, label);
        button.colors = colors;
        Dirty(button);
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
        Undo.RegisterCreatedObjectUndo(root, "Create Character Main preview root");
        root.transform.SetAsLastSibling();
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 1f;

        Image background = CreateImage(root.transform, "PreviewBackground", null, Cream);
        SetStretchRect(background.rectTransform, Vector2.zero, Vector2.zero, "Stretch preview background");
        TMP_Text heading = CreateText(background.transform, "PreviewHeading",
            "F. Character Main Renewal · LOCAL PREVIEW", result.Font, 28f, MutedBrown, FontStyles.Bold);
        SetCenteredRect(heading.rectTransform, new Vector2(960f, 44f), new Vector2(0f, 882f),
            "Place preview heading");

        Button defaultButton = CreateButton(background.transform, "DefaultStateButton", "기본 상태",
            result.CreamButtonSprite, result.Font, new Vector2(-326f, 808f), new Vector2(290f, 86f));
        Button highButton = CreateButton(background.transform, "HighStatStateButton", "높은 능력치",
            result.CreamButtonSprite, result.Font, new Vector2(0f, 808f), new Vector2(290f, 86f));
        Button noTitleButton = CreateButton(background.transform, "NoTitleStateButton", "칭호 미장착",
            result.CreamButtonSprite, result.Font, new Vector2(326f, 808f), new Vector2(290f, 86f));

        TMP_Text name = CreateText(background.transform, "NameText", "용감한 모험가", result.Font,
            50f, Brown, FontStyles.Bold);
        SetCenteredRect(name.rectTransform, new Vector2(920f, 68f), new Vector2(0f, 710f),
            "Place preview nickname");

        Image titleRoot = CreateImage(background.transform, "EquippedTitle", result.CardSprite, CreamLight);
        SetCenteredRect(titleRoot.rectTransform, new Vector2(560f, 82f), new Vector2(0f, 624f),
            "Place preview title");
        TMP_Text title = CreateText(titleRoot.transform, "TitleText", "새벽을 여는 자", result.Font,
            29f, Gold, FontStyles.Bold);
        SetStretchRect(title.rectTransform, new Vector2(28f, 10f), new Vector2(-28f, -10f),
            "Inset preview title");

        Button titlePopupButton = CreateButton(background.transform, "TitleButton", "칭호", result.GoldButtonSprite, result.Font,
            new Vector2(-260f, 518f), new Vector2(430f, 94f));
        Button achievementPopupButton = CreateButton(background.transform, "AchievementButton", "업적", result.CreamButtonSprite,
            result.Font, new Vector2(260f, 518f), new Vector2(430f, 94f));

        Image panel = CreateImage(background.transform, "CharacterPanel", result.CardSprite, CreamLight);
        SetCenteredRect(panel.rectTransform, new Vector2(1000f, 1010f), new Vector2(0f, -80f),
            "Place preview character panel");
        TMP_Text statHeading = CreateText(panel.transform, "StatHeading", "기본 능력치", result.Font,
            32f, Brown, FontStyles.Bold);
        SetCenteredRect(statHeading.rectTransform, new Vector2(880f, 48f), new Vector2(0f, 438f),
            "Place preview stat heading");

        string[] statNames = { "ATK", "DEF", "HP", "SPD", "CRIT" };
        string[] statValues = { "128", "94", "1,460", "37", "12%" };
        Vector2[] statPositions =
        {
            new Vector2(-294f, 330f), new Vector2(0f, 330f), new Vector2(294f, 330f),
            new Vector2(-147f, 184f), new Vector2(147f, 184f)
        };
        TMP_Text[] valueTexts = new TMP_Text[5];
        for (int index = 0; index < statNames.Length; index++)
        {
            Image card = CreateImage(panel.transform, statNames[index] + "Card", result.CardSprite, CreamLight);
            SetCenteredRect(card.rectTransform, new Vector2(270f, 126f), statPositions[index],
                "Place preview stat card");
            TMP_Text label = CreateText(card.transform, "Label", statNames[index], result.Font, 23f,
                MutedBrown, FontStyles.Bold);
            SetCenteredRect(label.rectTransform, new Vector2(220f, 34f), new Vector2(0f, 30f),
                "Place preview stat label");
            valueTexts[index] = CreateText(card.transform, "Value", statValues[index], result.Font, 32f,
                Brown, FontStyles.Bold);
            SetCenteredRect(valueTexts[index].rectTransform, new Vector2(230f, 44f), new Vector2(0f, -22f),
                "Place preview stat value");
        }

        TMP_Text attributeHeading = CreateText(panel.transform, "AttributeHeading", "속성 레벨 · EXP",
            result.Font, 32f, Brown, FontStyles.Bold);
        SetCenteredRect(attributeHeading.rectTransform, new Vector2(880f, 48f), new Vector2(0f, 85f),
            "Place preview attribute heading");

        string[] attributeNames = { "FIRE", "WATER", "GRASS", "AURORA" };
        Color32[] attributeColors = { Fire, Water, Grass, Aurora };
        int[] levels = { 7, 8, 5, 9 };
        int[] exp = { 700, 1200, 225, 780 };
        int[] maxExp = { 1000, 2000, 500, 3000 };
        float[] rowY = { 15f, -102f, -219f, -336f };
        TMP_Text[] levelTexts = new TMP_Text[4];
        TMP_Text[] expTexts = new TMP_Text[4];
        Image[] fills = new Image[4];
        for (int index = 0; index < attributeNames.Length; index++)
        {
            Image row = CreateImage(panel.transform, attributeNames[index] + "Row", result.CardSprite,
                CreamLight);
            SetCenteredRect(row.rectTransform, new Vector2(900f, 102f), new Vector2(0f, rowY[index]),
                "Place preview attribute row");
            TMP_Text label = CreateText(row.transform, "Name", attributeNames[index], result.Font, 25f,
                attributeColors[index], FontStyles.Bold);
            SetCenteredRect(label.rectTransform, new Vector2(160f, 36f), new Vector2(-340f, 20f),
                "Place preview attribute name");
            levelTexts[index] = CreateText(row.transform, "Level", $"Lv. {levels[index]}", result.Font,
                22f, Brown, FontStyles.Bold);
            SetCenteredRect(levelTexts[index].rectTransform, new Vector2(160f, 32f),
                new Vector2(-340f, -21f), "Place preview attribute level");
            Image track = CreateImage(row.transform, "Track", result.BarSprite, Color.white);
            SetCenteredRect(track.rectTransform, new Vector2(520f, 36f), new Vector2(105f, 18f),
                "Place preview exp track");
            fills[index] = CreateImage(track.transform, "Fill", result.BarFillSprite, attributeColors[index]);
            fills[index].type = Image.Type.Filled;
            fills[index].fillMethod = Image.FillMethod.Horizontal;
            fills[index].fillOrigin = 0;
            fills[index].fillAmount = (float)exp[index] / maxExp[index];
            SetStretchRect(fills[index].rectTransform, new Vector2(8f, 7f), new Vector2(-8f, -7f),
                "Inset preview exp fill");
            expTexts[index] = CreateText(row.transform, "Exp", $"{exp[index]:N0} / {maxExp[index]:N0}",
                result.Font, 20f, Brown, FontStyles.Normal);
            SetCenteredRect(expTexts[index].rectTransform, new Vector2(500f, 30f),
                new Vector2(105f, -22f), "Place preview exp text");
        }

        CreateButton(panel.transform, "InventoryButton", "인벤토리", result.GoldButtonSprite, result.Font,
            new Vector2(0f, -447f), new Vector2(520f, 92f));
        TMP_Text status = CreateText(background.transform, "PreviewStatusText",
            "[가상 테스트 데이터] 기본 상태 · 실제 Manager/API 호출 없음", result.Font, 22f,
            MutedBrown, FontStyles.Normal);
        SetCenteredRect(status.rectTransform, new Vector2(980f, 42f), new Vector2(0f, -895f),
            "Place preview status");

        Image titlePopupDim = CreateImage(root.transform, "TitlePopup", null,
            new Color32(0x36, 0x26, 0x20, 0xB8));
        SetStretchRect(titlePopupDim.rectTransform, Vector2.zero, Vector2.zero, "Stretch title popup dim");
        Image titlePopupPanel = CreateImage(titlePopupDim.transform, "Panel", result.CardSprite, Cream);
        SetCenteredRect(titlePopupPanel.rectTransform, new Vector2(920f, 1440f), Vector2.zero,
            "Place preview title popup");
        TMP_Text popupHeading = CreateText(titlePopupPanel.transform, "Heading", "보유 칭호", result.Font,
            42f, Brown, FontStyles.Bold);
        SetCenteredRect(popupHeading.rectTransform, new Vector2(760f, 64f), new Vector2(0f, 620f),
            "Place preview title heading");
        TMP_Text popupGuide = CreateText(titlePopupPanel.transform, "Guide",
            "칭호를 누르면 장착되며, 장착 중인 칭호를 다시 누르면 해제됩니다.", result.Font,
            22f, MutedBrown, FontStyles.Normal);
        SetCenteredRect(popupGuide.rectTransform, new Vector2(780f, 58f), new Vector2(0f, 555f),
            "Place preview title guide");

        GameObject scrollObject = new GameObject("Scroll View", typeof(RectTransform), typeof(Image),
            typeof(ScrollRect));
        Undo.RegisterCreatedObjectUndo(scrollObject, "Create title preview scroll");
        scrollObject.transform.SetParent(titlePopupPanel.transform, false);
        Image scrollImage = scrollObject.GetComponent<Image>();
        scrollImage.sprite = result.CardSprite;
        scrollImage.color = CreamLight;
        scrollImage.type = Image.Type.Sliced;
        SetCenteredRect(scrollObject.GetComponent<RectTransform>(), new Vector2(790f, 940f),
            new Vector2(0f, 25f), "Place title preview scroll");
        Image viewport = CreateImage(scrollObject.transform, "Viewport", null, Color.white);
        Mask mask = Undo.AddComponent<Mask>(viewport.gameObject);
        mask.showMaskGraphic = false;
        SetStretchRect(viewport.rectTransform, new Vector2(18f, 18f), new Vector2(-18f, -18f),
            "Inset title preview viewport");
        GameObject contentObject = new GameObject("Content", typeof(RectTransform),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        Undo.RegisterCreatedObjectUndo(contentObject, "Create title preview content");
        contentObject.transform.SetParent(viewport.transform, false);
        RectTransform titleContent = contentObject.GetComponent<RectTransform>();
        SetRect(titleContent, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero,
            new Vector2(0f, 0f), new Vector2(0.5f, 1f), "Place title preview content");
        VerticalLayoutGroup titleLayout = contentObject.GetComponent<VerticalLayoutGroup>();
        titleLayout.padding = new RectOffset(16, 16, 16, 16);
        titleLayout.spacing = 18f;
        titleLayout.childControlWidth = true;
        titleLayout.childControlHeight = false;
        titleLayout.childForceExpandWidth = true;
        titleLayout.childForceExpandHeight = false;
        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect titleScroll = scrollObject.GetComponent<ScrollRect>();
        titleScroll.viewport = viewport.rectTransform;
        titleScroll.content = titleContent;
        titleScroll.horizontal = false;
        titleScroll.vertical = true;
        titleScroll.movementType = ScrollRect.MovementType.Clamped;

        string[] previewTitles = { "새벽을 여는 자", "꾸준한 모험가", "습관의 수호자", "불굴의 도전자" };
        Button[] titleButtons = new Button[previewTitles.Length];
        Image[] titleCards = new Image[previewTitles.Length];
        TMP_Text[] titleStates = new TMP_Text[previewTitles.Length];
        for (int index = 0; index < previewTitles.Length; index++)
        {
            titleButtons[index] = CreateButton(titleContent, "TitleOption" + index, previewTitles[index],
                result.CardSprite, result.Font, Vector2.zero, new Vector2(720f, 190f));
            titleCards[index] = titleButtons[index].GetComponent<Image>();
            LayoutElement layout = Undo.AddComponent<LayoutElement>(titleButtons[index].gameObject);
            layout.preferredHeight = 190f;
            TMP_Text label = titleButtons[index].GetComponentInChildren<TMP_Text>();
            SetCenteredRect(label.rectTransform, new Vector2(620f, 60f), new Vector2(0f, 30f),
                "Place preview title name");
            titleStates[index] = CreateText(titleButtons[index].transform, "State", "선택하여 장착",
                result.Font, 21f, MutedBrown, FontStyles.Normal);
            SetCenteredRect(titleStates[index].rectTransform, new Vector2(620f, 40f),
                new Vector2(0f, -38f), "Place preview title state");
        }
        Button titleCloseButton = CreateButton(titlePopupPanel.transform, "CloseButton", "닫기",
            result.GoldButtonSprite, result.Font, new Vector2(0f, -620f), new Vector2(360f, 96f));

        Image achievementPopupDim = CreateImage(root.transform, "AchievementPopup", null,
            new Color32(0x36, 0x26, 0x20, 0xB8));
        SetStretchRect(achievementPopupDim.rectTransform, Vector2.zero, Vector2.zero,
            "Stretch achievement popup dim");
        Image achievementPopupPanel = CreateImage(achievementPopupDim.transform, "Panel",
            result.CardSprite, Cream);
        SetCenteredRect(achievementPopupPanel.rectTransform, new Vector2(920f, 1440f), Vector2.zero,
            "Place preview achievement popup");
        TMP_Text achievementHeading = CreateText(achievementPopupPanel.transform, "Heading", "업적",
            result.Font, 42f, Brown, FontStyles.Bold);
        SetCenteredRect(achievementHeading.rectTransform, new Vector2(760f, 64f),
            new Vector2(0f, 620f), "Place preview achievement heading");
        TMP_Text achievementGuide = CreateText(achievementPopupPanel.transform, "Guide",
            "달성 상태와 진행도를 확인하고 완료한 업적의 보상을 수령할 수 있습니다.", result.Font,
            22f, MutedBrown, FontStyles.Normal);
        SetCenteredRect(achievementGuide.rectTransform, new Vector2(780f, 58f),
            new Vector2(0f, 555f), "Place preview achievement guide");

        GameObject achievementScrollObject = new GameObject("Scroll View", typeof(RectTransform),
            typeof(Image), typeof(ScrollRect));
        Undo.RegisterCreatedObjectUndo(achievementScrollObject, "Create achievement preview scroll");
        achievementScrollObject.transform.SetParent(achievementPopupPanel.transform, false);
        SetImage(achievementScrollObject.GetComponent<Image>(), result.CardSprite, CreamLight,
            Image.Type.Sliced, "Style achievement preview scroll");
        SetCenteredRect(achievementScrollObject.GetComponent<RectTransform>(), new Vector2(790f, 940f),
            new Vector2(0f, 25f), "Place achievement preview scroll");
        Image achievementViewport = CreateImage(achievementScrollObject.transform, "Viewport", null, Color.white);
        Mask achievementMask = Undo.AddComponent<Mask>(achievementViewport.gameObject);
        achievementMask.showMaskGraphic = false;
        SetStretchRect(achievementViewport.rectTransform, new Vector2(18f, 18f),
            new Vector2(-18f, -18f), "Inset achievement preview viewport");
        GameObject achievementContentObject = new GameObject("Content", typeof(RectTransform),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        Undo.RegisterCreatedObjectUndo(achievementContentObject, "Create achievement preview content");
        achievementContentObject.transform.SetParent(achievementViewport.transform, false);
        RectTransform achievementContent = achievementContentObject.GetComponent<RectTransform>();
        SetRect(achievementContent, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero,
            Vector2.zero, new Vector2(0.5f, 1f), "Place achievement preview content");
        VerticalLayoutGroup achievementLayout = achievementContentObject.GetComponent<VerticalLayoutGroup>();
        achievementLayout.padding = new RectOffset(16, 16, 16, 16);
        achievementLayout.spacing = 28f;
        achievementLayout.childControlWidth = true;
        achievementLayout.childControlHeight = true;
        achievementLayout.childForceExpandWidth = true;
        achievementLayout.childForceExpandHeight = false;
        achievementContentObject.GetComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect achievementScroll = achievementScrollObject.GetComponent<ScrollRect>();
        achievementScroll.viewport = achievementViewport.rectTransform;
        achievementScroll.content = achievementContent;
        achievementScroll.horizontal = false;
        achievementScroll.vertical = true;
        achievementScroll.movementType = ScrollRect.MovementType.Clamped;

        string[] achievementContents =
        {
            "불 속성 습관을 10회 인증하기", "아무 속성 레벨 10 달성하기",
            "장비를 5회 강화하기", "PvP에서 10회 승리하기", "습관을 30회 완료하기"
        };
        string[] achievementProgress = { "6 / 10", "10 / 10", "5 / 5", "3 / 10", "30 / 30" };
        string[] achievementStates = { "진행 중", "달성 완료", "달성 완료", "진행 중", "달성 완료" };
        Button[] achievementClaimButtons = new Button[achievementContents.Length];
        Image[] achievementCards = new Image[achievementContents.Length];
        TMP_Text[] achievementStateTexts = new TMP_Text[achievementContents.Length];
        TMP_Text[] achievementClaimTexts = new TMP_Text[achievementContents.Length];
        for (int index = 0; index < achievementContents.Length; index++)
        {
            Image card = CreateImage(achievementContent, "AchievementOption" + index,
                result.CardSprite, index == 1 || index == 2 || index == 4
                    ? new Color32(0xFF, 0xEA, 0xB8, 0xFF)
                    : CreamLight);
            achievementCards[index] = card;
            LayoutElement layout = Undo.AddComponent<LayoutElement>(card.gameObject);
            layout.minHeight = 270f;
            layout.preferredHeight = 270f;
            TMP_Text contentText = CreateText(card.transform, "Content", achievementContents[index],
                result.Font, 39f, Brown, FontStyles.Bold);
            contentText.alignment = TextAlignmentOptions.Left;
            contentText.enableAutoSizing = true;
            contentText.fontSizeMin = 34f;
            contentText.fontSizeMax = 39f;
            contentText.enableWordWrapping = true;
            contentText.overflowMode = TextOverflowModes.Ellipsis;
            contentText.maxVisibleLines = 2;
            SetCenteredRect(contentText.rectTransform, new Vector2(680f, 100f),
                new Vector2(0f, 68f), "Place preview achievement content");
            TMP_Text progress = CreateText(card.transform, "Progress", achievementProgress[index],
                result.Font, 30f, Brown, FontStyles.Bold);
            progress.alignment = TextAlignmentOptions.Left;
            SetCenteredRect(progress.rectTransform, new Vector2(430f, 42f),
                new Vector2(-125f, -34f), "Place preview achievement progress");
            achievementStateTexts[index] = CreateText(card.transform, "State", achievementStates[index],
                result.Font, 30f, index == 1 || index == 2 || index == 4 ? Gold : MutedBrown,
                FontStyles.Bold);
            achievementStateTexts[index].alignment = TextAlignmentOptions.Left;
            SetCenteredRect(achievementStateTexts[index].rectTransform, new Vector2(430f, 42f),
                new Vector2(-125f, -82f), "Place preview achievement state");
            achievementClaimButtons[index] = CreateButton(card.transform, "ClaimButton", "보상 수령",
                result.GoldButtonSprite, result.Font, new Vector2(250f, -58f), new Vector2(190f, 82f));
            achievementClaimButtons[index].interactable = index == 1 || index == 2 || index == 4;
            achievementClaimTexts[index] = achievementClaimButtons[index].GetComponentInChildren<TMP_Text>();
        }
        Button achievementCloseButton = CreateButton(achievementPopupPanel.transform, "CloseButton", "닫기",
            result.GoldButtonSprite, result.Font, new Vector2(0f, -620f), new Vector2(360f, 96f));

        CharacterMainPreviewController controller = Undo.AddComponent<CharacterMainPreviewController>(root);
        SerializedObject serialized = new SerializedObject(controller);
        SetReference(serialized, "nameText", name);
        SetReference(serialized, "equippedTitleRoot", titleRoot.gameObject);
        SetReference(serialized, "titleText", title);
        SetArray(serialized, "statValueTexts", valueTexts);
        SetArray(serialized, "attributeLevelTexts", levelTexts);
        SetArray(serialized, "attributeExpTexts", expTexts);
        SetArray(serialized, "attributeExpFills", fills);
        SetReference(serialized, "previewStatusText", status);
        SetReference(serialized, "defaultStateButton", defaultButton);
        SetReference(serialized, "highStatStateButton", highButton);
        SetReference(serialized, "noTitleStateButton", noTitleButton);
        SetReference(serialized, "titlePopupButton", titlePopupButton);
        SetReference(serialized, "titlePopup", titlePopupDim.gameObject);
        SetReference(serialized, "titlePopupCloseButton", titleCloseButton);
        SetArray(serialized, "titleOptionButtons", titleButtons);
        SetArray(serialized, "titleOptionCards", titleCards);
        SetArray(serialized, "titleOptionStateTexts", titleStates);
        SetReference(serialized, "achievementPopupButton", achievementPopupButton);
        SetReference(serialized, "achievementPopup", achievementPopupDim.gameObject);
        SetReference(serialized, "achievementPopupCloseButton", achievementCloseButton);
        SetArray(serialized, "achievementClaimButtons", achievementClaimButtons);
        SetArray(serialized, "achievementCards", achievementCards);
        SetArray(serialized, "achievementStateTexts", achievementStateTexts);
        SetArray(serialized, "achievementClaimTexts", achievementClaimTexts);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        root.SetActive(false);
        Dirty(root);
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(item, "Create Character preview image");
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
        Undo.RegisterCreatedObjectUndo(item, "Create Character preview text");
        item.transform.SetParent(parent, false);
        TMP_Text text = item.GetComponent<TMP_Text>();
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

    private static Button CreateButton(Transform parent, string name, string label, Sprite sprite,
        TMP_FontAsset font, Vector2 position, Vector2 size)
    {
        Image image = CreateImage(parent, name, sprite, Color.white);
        Button button = Undo.AddComponent<Button>(image.gameObject);
        button.targetGraphic = image;
        SetCenteredRect(image.rectTransform, size, position, "Place Character preview button");
        TMP_Text text = CreateText(image.transform, "Text", label, font, 27f, Brown, FontStyles.Bold);
        SetStretchRect(text.rectTransform, new Vector2(18f, 8f), new Vector2(-18f, -8f),
            "Inset Character preview button text");
        return button;
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

    private static void IsolateScene(Scene scene, GameObject previewRoot,
        CharacterMainPreviewController controller)
    {
        if (controller == null)
        {
            throw new InvalidOperationException("F Preview Controller를 찾을 수 없습니다.");
        }

        HashSet<GameObject> isolatedSet = new HashSet<GameObject>(scene.GetRootGameObjects()
            .Where(root => root != previewRoot && root.activeSelf && root.GetComponent<EventSystem>() == null &&
                           root.GetComponentInChildren<Camera>(true) == null &&
                           root.GetComponentInChildren<AudioListener>(true) == null)
            .ToArray());
        GameObject canvasRoot = FindRoot(scene, "Canvas");
        Transform habitTap = canvasRoot != null ? canvasRoot.transform.Find("HabitTap") : null;
        Transform life = habitTap != null ? habitTap.Find("Life") : null;
        Transform addForm = habitTap != null ? habitTap.Find("LifeAdd_1") : null;
        if (life != null)
        {
            isolatedSet.Add(life.gameObject);
        }
        if (addForm != null)
        {
            isolatedSet.Add(addForm.gameObject);
        }

        GameObject[] isolatedObjects = isolatedSet.OrderBy(root => root.GetInstanceID()).ToArray();
        bool[] objectStates = isolatedObjects.Select(item => item.activeSelf).ToArray();

        string runtimeAssembly = typeof(CharacterUIManager).Assembly.GetName().Name;
        Behaviour[] behaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true)
            .Where(item => item != null && item != controller && item.gameObject.scene == scene &&
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
            if (!behaviour.enabled)
            {
                continue;
            }

            Record(behaviour, "Isolate runtime script for Character preview");
            behaviour.enabled = false;
            SavePrefabOverride(behaviour);
        }

        foreach (GameObject target in isolatedObjects)
        {
            SetActive(target, false, "Isolate scene object for Character preview");
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

    public static void RestoreCharacterPreviewState()
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
        CharacterMainPreviewController controller =
            root != null ? root.GetComponent<CharacterMainPreviewController>() : null;
        if (root == null || controller == null || (!root.activeSelf && !HasIsolationSnapshot(controller)))
        {
            return;
        }

        RestoreBehaviours(controller);
        RestoreObjects(controller);
        ClearIsolationSnapshot(controller);
        SetActive(root, false, "Restore Character preview state");
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void RestoreBehaviours(CharacterMainPreviewController controller)
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
            if (target == null)
            {
                continue;
            }

            target.enabled = states.GetArrayElementAtIndex(index).boolValue;
            Dirty(target);
            SavePrefabOverride(target);
        }
    }

    private static void RestoreObjects(CharacterMainPreviewController controller)
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
            if (target == null)
            {
                continue;
            }

            target.SetActive(states.GetArrayElementAtIndex(index).boolValue);
            Dirty(target);
            SavePrefabOverride(target);
        }
    }

    private static bool HasIsolationSnapshot(CharacterMainPreviewController controller)
    {
        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty behaviours = serialized.FindProperty("isolatedBehaviours");
        SerializedProperty objects = serialized.FindProperty("isolatedGameObjects");
        return (behaviours != null && behaviours.arraySize > 0) ||
               (objects != null && objects.arraySize > 0);
    }

    private static void ClearIsolationSnapshot(CharacterMainPreviewController controller)
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

        GameObject aRoot = FindRoot(scene, HabitItemPreviewRootName);
        GameObject dRoot = FindRoot(scene, NumberPreviewRootName);
        GameObject eRoot = FindRoot(scene, PhotoPreviewRootName);
        GameObject canvas = FindRoot(scene, "Canvas");
        Transform habitTap = canvas != null ? canvas.transform.Find("HabitTap") : null;
        bool otherMode = (aRoot != null && aRoot.activeSelf) || (dRoot != null && dRoot.activeSelf) ||
                         (eRoot != null && eRoot.activeSelf) ||
                         (habitTap != null && habitTap.Find("Life")?.gameObject.activeSelf == true) ||
                         (habitTap != null && habitTap.Find("LifeAdd_1")?.gameObject.activeSelf == true);
        if (otherMode)
        {
            RestoreCharacterPreviewState();
        }
    }

    private static void RestoreBeforeSceneSave(Scene scene, string path)
    {
        if (scene.path == ScenePath && scene == SceneManager.GetActiveScene())
        {
            RestoreCharacterPreviewState();
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

        if (result.StatCards.Count != 5)
        {
            errors.Add($"능력치 카드가 5개가 아닙니다: {result.StatCards.Count}");
        }

        if (result.AttributeCards.Count != 4)
        {
            errors.Add($"속성 EXP 카드가 4개가 아닙니다: {result.AttributeCards.Count}");
        }

        ValidateTouchTarget(result.TitleButton, "TitleButton", errors);
        ValidateTouchTarget(result.AchievementButton, "AchievementButton", errors);
        ValidateTouchTarget(result.InventoryButton, "InventoryButton", errors);
        foreach (TMP_Text text in GetTargetTexts(result))
        {
            if (text.font != result.Font)
            {
                errors.Add($"NotoSansKR이 아닌 CharacterTap 텍스트가 있습니다: {text.name}");
            }
        }

        GameObject preview = FindRoot(result.Scene, PreviewRootName);
        if (requirePreview && (preview == null || preview.GetComponent<CharacterMainPreviewController>() == null))
        {
            errors.Add("F Preview 전용 Root/Controller가 없습니다.");
        }
        else if (requirePreview && preview.transform.Find("TitlePopup") == null)
        {
            errors.Add("F Preview 칭호 팝업이 없습니다.");
        }
        else if (requirePreview && preview.transform.Find("AchievementPopup") == null)
        {
            errors.Add("F Preview 업적 팝업이 없습니다.");
        }

        if (result.TitlePopup == null || result.TitleCloseButton == null || result.TitleSlotParent == null ||
            result.TitleScrollRect == null)
        {
            errors.Add("실제 칭호 팝업의 닫기/스크롤/Content 연결이 불완전합니다.");
        }
        if (result.AchievementPopup == null || result.AchievementCloseButton == null ||
            result.AchievementSlotParent == null || result.AchievementSlotPrefab == null ||
            result.AchievementScrollRect == null)
        {
            errors.Add("실제 업적 팝업의 닫기/스크롤/Content/Slot Prefab 연결이 불완전합니다.");
        }

        return errors;
    }

    private static IEnumerable<TMP_Text> GetTargetTexts(ValidationResult result)
    {
        List<TMP_Text> texts = new List<TMP_Text>();
        if (result.NameText != null)
        {
            texts.Add(result.NameText);
        }

        if (result.TitleButton != null)
        {
            texts.AddRange(result.TitleButton.GetComponentsInChildren<TMP_Text>(true));
        }

        if (result.AchievementButton != null)
        {
            texts.AddRange(result.AchievementButton.GetComponentsInChildren<TMP_Text>(true));
        }

        if (result.StatusBackground != null)
        {
            texts.AddRange(result.StatusBackground.GetComponentsInChildren<TMP_Text>(true));
        }
        if (result.TitlePopup != null)
        {
            texts.AddRange(result.TitlePopup.GetComponentsInChildren<TMP_Text>(true));
        }
        if (result.AchievementPopup != null)
        {
            texts.AddRange(result.AchievementPopup.GetComponentsInChildren<TMP_Text>(true));
        }

        return texts.Where(text => text != null).Distinct();
    }

    private static void ValidateTouchTarget(Button button, string label, ICollection<string> errors)
    {
        RectTransform rect = button != null ? button.GetComponent<RectTransform>() : null;
        if (rect == null || rect.rect.width < 280f || rect.rect.height < 88f)
        {
            errors.Add($"{label} 터치 영역이 280x88px 미만입니다.");
        }
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene.IsValid()
            ? scene.GetRootGameObjects().FirstOrDefault(item => item.name == name)
            : null;
    }

    private static void SetText(TMP_Text text, TMP_FontAsset font, float size, Color color,
        TextAlignmentOptions alignment, FontStyles style, string label)
    {
        if (text == null)
        {
            return;
        }

        Record(text, label);
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = style;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(16f, size * 0.68f);
        text.fontSizeMax = size;
        text.enableWordWrapping = false;
        Dirty(text);
    }

    private static void SetImage(Image image, Sprite sprite, Color color, Image.Type type, string label)
    {
        if (image == null)
        {
            return;
        }

        Record(image, label);
        if (sprite != null)
        {
            image.sprite = sprite;
        }
        image.color = color;
        image.type = type;
        Dirty(image);
    }

    private static void SetCenteredRect(RectTransform rect, Vector2 size, Vector2 position, string label)
    {
        SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size,
            new Vector2(0.5f, 0.5f), label);
    }

    private static void SetStretchRect(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax, string label)
    {
        if (rect == null)
        {
            return;
        }

        Record(rect, label);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        Dirty(rect);
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size, Vector2 pivot, string label)
    {
        if (rect == null)
        {
            return;
        }

        Record(rect, label);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        Dirty(rect);
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
        if (target == null)
        {
            return;
        }

        EditorUtility.SetDirty(target);
        SavePrefabOverride(target);
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
        public GameObject CharacterTap;
        public CharacterUIManager Manager;
        public TitleManager TitleManager;
        public AchievementManager AchievementManager;
        public TMP_FontAsset Font;
        public Sprite CardSprite;
        public Sprite CreamButtonSprite;
        public Sprite GoldButtonSprite;
        public Sprite BarSprite;
        public Sprite BarFillSprite;
        public TMP_Text NameText;
        public Image TitleImage;
        public Button TitleButton;
        public Button AchievementButton;
        public Image StatusBackground;
        public RectTransform StatusBoxes;
        public RectTransform AttributeBars;
        public Button InventoryButton;
        public GameObject TitlePopup;
        public Button TitleCloseButton;
        public Transform TitleSlotParent;
        public ScrollRect TitleScrollRect;
        public GameObject AchievementPopup;
        public Button AchievementCloseButton;
        public Transform AchievementSlotParent;
        public AchievementSlotUI AchievementSlotPrefab;
        public ScrollRect AchievementScrollRect;
        public readonly List<Image> StatCards = new List<Image>();
        public readonly List<Image> AttributeCards = new List<Image>();
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }

    private sealed class ProtectionSnapshot
    {
        private readonly int[] referenceIds;
        private readonly string titleEvent;
        private readonly string achievementEvent;
        private readonly string inventoryEvent;
        private readonly int[] titleReferenceIds;
        private readonly string titleCloseEvent;
        private readonly int[] achievementReferenceIds;
        private readonly string achievementCloseEvent;
        private readonly int[] registeredAchievementIds;
        private readonly bool achievementUsesLocalTestData;

        private ProtectionSnapshot(int[] referenceIds, string titleEvent, string achievementEvent,
            string inventoryEvent, int[] titleReferenceIds, string titleCloseEvent,
            int[] achievementReferenceIds, string achievementCloseEvent,
            int[] registeredAchievementIds, bool achievementUsesLocalTestData)
        {
            this.referenceIds = referenceIds;
            this.titleEvent = titleEvent;
            this.achievementEvent = achievementEvent;
            this.inventoryEvent = inventoryEvent;
            this.titleReferenceIds = titleReferenceIds;
            this.titleCloseEvent = titleCloseEvent;
            this.achievementReferenceIds = achievementReferenceIds;
            this.achievementCloseEvent = achievementCloseEvent;
            this.registeredAchievementIds = registeredAchievementIds;
            this.achievementUsesLocalTestData = achievementUsesLocalTestData;
        }

        public static ProtectionSnapshot Capture(ValidationResult result)
        {
            SerializedObject manager = new SerializedObject(result.Manager);
            int[] ids = ManagerReferenceNames.Select(name =>
            {
                UnityEngine.Object value = manager.FindProperty(name)?.objectReferenceValue;
                return value != null ? value.GetInstanceID() : 0;
            }).ToArray();
            SerializedObject titleManager = new SerializedObject(result.TitleManager);
            string[] titleNames =
            {
                "titleButton", "closeButton", "titlePopup", "slotParent", "previewPopup",
                "previewTitleImage", "previewDimCloser", "slotPrefab", "backendManager"
            };
            int[] titleIds = titleNames.Select(name =>
            {
                UnityEngine.Object value = titleManager.FindProperty(name)?.objectReferenceValue;
                return value != null ? value.GetInstanceID() : 0;
            }).ToArray();
            SerializedObject achievementManager = new SerializedObject(result.AchievementManager);
            string[] achievementNames =
            {
                "titleManager", "achievementButton", "closeButton", "achievementPopup", "slotParent",
                "slotPrefab", "backendManager"
            };
            int[] achievementIds = achievementNames.Select(name =>
            {
                UnityEngine.Object value = achievementManager.FindProperty(name)?.objectReferenceValue;
                return value != null ? value.GetInstanceID() : 0;
            }).ToArray();
            SerializedProperty registeredAchievements = achievementManager.FindProperty("registeredAchievements");
            int[] registeredIds = registeredAchievements != null
                ? Enumerable.Range(0, registeredAchievements.arraySize)
                    .Select(index => registeredAchievements.GetArrayElementAtIndex(index).objectReferenceValue)
                    .Select(value => value != null ? value.GetInstanceID() : 0)
                    .ToArray()
                : Array.Empty<int>();
            bool useLocalTestData = achievementManager.FindProperty("useLocalTestData")?.boolValue ?? false;
            return new ProtectionSnapshot(ids, EventDigest(result.TitleButton),
                EventDigest(result.AchievementButton), EventDigest(result.InventoryButton), titleIds,
                EventDigest(result.TitleCloseButton), achievementIds,
                EventDigest(result.AchievementCloseButton), registeredIds, useLocalTestData);
        }

        public void AssertUnchanged(ValidationResult result)
        {
            ProtectionSnapshot current = Capture(result);
            if (!referenceIds.SequenceEqual(current.referenceIds) || titleEvent != current.titleEvent ||
                achievementEvent != current.achievementEvent || inventoryEvent != current.inventoryEvent ||
                !titleReferenceIds.SequenceEqual(current.titleReferenceIds) ||
                titleCloseEvent != current.titleCloseEvent ||
                !achievementReferenceIds.SequenceEqual(current.achievementReferenceIds) ||
                achievementCloseEvent != current.achievementCloseEvent ||
                !registeredAchievementIds.SequenceEqual(current.registeredAchievementIds) ||
                achievementUsesLocalTestData != current.achievementUsesLocalTestData)
            {
                throw new InvalidOperationException(
                    "CharacterUIManager/TitleManager/AchievementManager SerializeField 또는 버튼 UnityEvent 연결이 변경되어 Apply를 중단합니다.");
            }
        }

        private static string EventDigest(Button button)
        {
            if (button == null)
            {
                return "null";
            }

            StringBuilder digest = new StringBuilder();
            for (int index = 0; index < button.onClick.GetPersistentEventCount(); index++)
            {
                UnityEngine.Object target = button.onClick.GetPersistentTarget(index);
                digest.Append(target != null ? target.GetInstanceID() : 0).Append(':')
                    .Append(button.onClick.GetPersistentMethodName(index)).Append('|');
            }

            return digest.ToString();
        }
    }
}
