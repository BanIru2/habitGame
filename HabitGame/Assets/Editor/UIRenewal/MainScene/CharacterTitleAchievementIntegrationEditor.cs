using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Styles the title/achievement popup containers in MainScene and their runtime slot prefabs.
/// Scene changes are never saved automatically. Slot prefab changes require the separate explicit menu command.
/// Runtime managers, serialized references and button listeners are read-only.
/// </summary>
public static class CharacterTitleAchievementIntegrationEditor
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string CanvasPrefabPath = "Assets/Resources/UIPrefabs/Canvas.prefab";
    private const string TitlePrefabPath = "Assets/Resources/Prefabs/TitleSlot.prefab";
    private const string AchievementPrefabPath = "Assets/Resources/Prefabs/AchievementSlot.prefab";
    private const string FontPath = "Assets/Login/Fonts/NotoSansKR Login SDF.asset";
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
    private const string MenuRoot =
        "Tools/HabitPVP/UI Renewal/MainScene/15-D-5B Title Achievement/";
    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 CreamLight = new Color32(0xFF, 0xF9, 0xEA, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x86, 0x68, 0x5D, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);

    [MenuItem(MenuRoot + "Validate Structure", false, 2320)]
    private static void ValidateStructure()
    {
        Context context = BuildContext();
        LogResult("구조 검증", context.Errors, StructureSummary(context));
    }

    [MenuItem(MenuRoot + "Dry Run", false, 2321)]
    private static void DryRun()
    {
        Context context = BuildContext();
        if (context.Errors.Count > 0)
        {
            LogResult("Dry Run 실패", context.Errors, StructureSummary(context));
            return;
        }

        Debug.Log("[HabitPVP 15-D-5B] Dry Run (변경 없음)\n" +
                  StructureSummary(context) + "\n" +
                  "Scene Apply 대상: TitlePopup, AchievementPopup, 각 ScrollRect/Viewport/Content, " +
                  "닫기 버튼과 제목 텍스트, 보상 미리보기 Dim/중앙 Panel/X 버튼\n" +
                  "Scene 저장: 하지 않음. Canvas.prefab 원본: 변경하지 않음.\n" +
                  "Slot Prefab Apply 대상: TitleSlot.prefab, AchievementSlot.prefab (별도 메뉴에서만 저장)\n" +
                  "보존: 모든 Manager 참조, Button listener/UnityEvent, activeSelf, ScrollRect 연결, " +
                  "Content LayoutGroup/ContentSizeFitter, 슬롯 스크립트 필드\n" +
                  "예상 Scene Override: 두 Popup의 시각 속성 및 RectTransform만 생성\n" +
                  "팀원 충돌 주의: Canvas 공용 Prefab을 직접 Apply하지 말고 MainScene의 두 Popup Override만 저장하세요.");
    }

    [MenuItem(MenuRoot + "Apply (No Auto Save)", false, 2322)]
    private static void ApplyScene()
    {
        Context context = BuildContext();
        if (!CanApply(context)) return;

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply 15-D-5B Title Achievement Scene UI");
        try
        {
            ApplyPopup(context.TitlePopup, context.TitleScroll, context.Font, context.CardSprite,
                context.CreamButtonSprite);
            ApplyPopup(context.AchievementPopup, context.AchievementScroll, context.Font,
                context.CardSprite, context.CreamButtonSprite);
            ApplyRewardPreview(context);
            List<string> errors = ValidateSceneDesign(context);
            if (errors.Count > 0) throw new System.InvalidOperationException(string.Join("\n", errors));

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("[HabitPVP 15-D-5B] Scene Apply 완료. MainScene은 자동 저장하지 않았습니다. " +
                      "슬롯은 별도 'Apply Slot Prefabs' 메뉴를 실행하세요.");
        }
        catch (System.Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogError("[HabitPVP 15-D-5B] Scene Apply 실패, 이번 변경을 Undo했습니다.\n" + exception);
        }
    }

    [MenuItem(MenuRoot + "Apply Slot Prefabs", false, 2323)]
    private static void ApplySlotPrefabs()
    {
        Context context = BuildContext();
        if (!CanApply(context)) return;
        if (!EditorUtility.DisplayDialog("15-D-5B Slot Prefabs",
                "TitleSlot.prefab과 AchievementSlot.prefab 원본을 저장합니다.\n" +
                "런타임 생성 슬롯에 디자인을 반영하려면 필요합니다. 계속할까요?", "Apply", "Cancel"))
            return;

        ApplySlotPrefabsAtomically(context);
    }

    [MenuItem(MenuRoot + "Validate Applied Design", false, 2324)]
    private static void ValidateAppliedDesign()
    {
        Context context = BuildContext();
        List<string> errors = new List<string>(context.Errors);
        if (errors.Count == 0)
        {
            errors.AddRange(ValidateSceneDesign(context));
            errors.AddRange(ValidatePrefabDesign(context));
        }
        LogResult("적용 디자인 검증", errors, StructureSummary(context));
    }

    [MenuItem(MenuRoot + "Validate Structure", true)]
    [MenuItem(MenuRoot + "Dry Run", true)]
    [MenuItem(MenuRoot + "Apply (No Auto Save)", true)]
    [MenuItem(MenuRoot + "Apply Slot Prefabs", true)]
    [MenuItem(MenuRoot + "Validate Applied Design", true)]
    private static bool ValidateMenu() => !EditorApplication.isPlayingOrWillChangePlaymode;

    private static Context BuildContext()
    {
        Context context = new Context();
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            context.Errors.Add("활성 Scene이 MainScene이 아닙니다: " + scene.path);
            return context;
        }

        context.TitleManager = FindUnique<TitleManager>(scene, context.Errors);
        context.AchievementManager = FindUnique<AchievementManager>(scene, context.Errors);
        if (context.TitleManager == null || context.AchievementManager == null) return context;

        SerializedObject title = new SerializedObject(context.TitleManager);
        SerializedObject achievement = new SerializedObject(context.AchievementManager);
        context.TitlePopup = ObjectReference<GameObject>(title, "titlePopup", context.Errors);
        context.TitleContent = ObjectReference<Transform>(title, "slotParent", context.Errors);
        context.TitleClose = ObjectReference<Button>(title, "closeButton", context.Errors);
        context.TitleSlot = ObjectReference<TitleSlotUI>(title, "slotPrefab", context.Errors);
        context.PreviewPopup = ObjectReference<GameObject>(title, "previewPopup", context.Errors);
        context.PreviewTitleImage = ObjectReference<Image>(title, "previewTitleImage", context.Errors);
        context.PreviewDimCloser = ObjectReference<DimCloser>(title, "previewDimCloser", context.Errors);
        context.AchievementPopup = ObjectReference<GameObject>(achievement, "achievementPopup", context.Errors);
        context.AchievementContent = ObjectReference<Transform>(achievement, "slotParent", context.Errors);
        context.AchievementClose = ObjectReference<Button>(achievement, "closeButton", context.Errors);
        context.AchievementSlot = ObjectReference<AchievementSlotUI>(achievement, "slotPrefab", context.Errors);

        context.TitleScroll = FindOwningScroll(context.TitleContent, "칭호", context.Errors);
        context.AchievementScroll = FindOwningScroll(context.AchievementContent, "업적", context.Errors);
        ValidatePopup(context.TitlePopup, context.TitleContent, context.TitleClose, context.TitleScroll,
            "칭호", context.Errors);
        ValidatePopup(context.AchievementPopup, context.AchievementContent, context.AchievementClose,
            context.AchievementScroll, "업적", context.Errors);

        context.Font = Load<TMP_FontAsset>(FontPath, context.Errors);
        context.CardSprite = Load<Sprite>(CardSpritePath, context.Errors);
        context.CreamButtonSprite = Load<Sprite>(CreamButtonSpritePath, context.Errors);
        context.GoldButtonSprite = Load<Sprite>(GoldButtonSpritePath, context.Errors);
        context.BarSprite = Load<Sprite>(BarSpritePath, context.Errors);
        context.BarFillSprite = Load<Sprite>(BarFillSpritePath, context.Errors);
        ValidateRewardPreviewApplyRequirements(context, context.Errors);

        if (AssetDatabase.GetAssetPath(context.TitleSlot) != TitlePrefabPath)
            context.Errors.Add("TitleManager.slotPrefab이 예상 Prefab이 아닙니다: " +
                               AssetDatabase.GetAssetPath(context.TitleSlot));
        if (AssetDatabase.GetAssetPath(context.AchievementSlot) != AchievementPrefabPath)
            context.Errors.Add("AchievementManager.slotPrefab이 예상 Prefab이 아닙니다: " +
                               AssetDatabase.GetAssetPath(context.AchievementSlot));
        if (!IsCanvasInstanceChild(context.TitlePopup) || !IsCanvasInstanceChild(context.AchievementPopup))
            context.Errors.Add("두 Popup 모두 MainScene의 Canvas.prefab Instance 하위여야 합니다.");
        context.Errors.AddRange(ValidatePrefabApplyRequirements());

        return context;
    }

    private static void ValidatePopup(GameObject popup, Transform content, Button close, ScrollRect scroll,
        string label, ICollection<string> errors)
    {
        if (popup == null || content == null || close == null || scroll == null) return;
        if (!content.IsChildOf(popup.transform)) errors.Add(label + " Content가 Popup 하위가 아닙니다.");
        if (!close.transform.IsChildOf(popup.transform)) errors.Add(label + " CloseButton이 Popup 하위가 아닙니다.");
        if (scroll.content != content) errors.Add(label + " ScrollRect.content와 Manager.slotParent가 다릅니다.");
        if (!scroll.vertical) errors.Add(label + " ScrollRect.vertical이 비활성화되어 있습니다.");
        if (scroll.viewport == null || !scroll.viewport.IsChildOf(popup.transform))
            errors.Add(label + " ScrollRect.viewport 연결이 유효하지 않습니다.");
        if (scroll.viewport != null && scroll.viewport.GetComponent<Mask>() == null &&
            scroll.viewport.GetComponent<RectMask2D>() == null)
            errors.Add(label + " Viewport에 Mask 또는 RectMask2D가 없습니다.");
        if (content.GetComponent<LayoutGroup>() == null)
            errors.Add(label + " Content에 LayoutGroup이 없습니다.");
        if (content.GetComponent<ContentSizeFitter>() == null)
            errors.Add(label + " Content에 ContentSizeFitter가 없습니다.");
    }

    private static void ApplyPopup(GameObject popup, ScrollRect scroll, TMP_FontAsset font,
        Sprite cardSprite, Sprite buttonSprite)
    {
        Image background = popup.GetComponent<Image>();
        Record(background);
        background.sprite = cardSprite;
        background.type = Image.Type.Sliced;
        background.color = Cream;

        RectTransform rect = popup.GetComponent<RectTransform>();
        Record(rect);
        rect.offsetMin = new Vector2(50f, 200f);
        rect.offsetMax = new Vector2(-50f, -200f);

        Image viewportImage = scroll.viewport.GetComponent<Image>();
        if (viewportImage != null)
        {
            Record(viewportImage);
            viewportImage.color = CreamLight;
            viewportImage.raycastTarget = true;
        }

        foreach (TMP_Text text in popup.GetComponentsInChildren<TMP_Text>(true))
        {
            Record(text);
            text.font = font;
            text.color = Brown;
            text.enableWordWrapping = true;
        }

        foreach (Button button in popup.GetComponentsInChildren<Button>(true))
            StyleButton(button, buttonSprite, font);
        RecordPrefabChanges(popup);
    }

    private static void ApplySlotPrefabsAtomically(Context context)
    {
        byte[] titleBackup = File.ReadAllBytes(TitlePrefabPath);
        byte[] achievementBackup = File.ReadAllBytes(AchievementPrefabPath);
        GameObject titleRoot = null;
        GameObject achievementRoot = null;
        bool saveStarted = false;
        try
        {
            titleRoot = PrefabUtility.LoadPrefabContents(TitlePrefabPath);
            achievementRoot = PrefabUtility.LoadPrefabContents(AchievementPrefabPath);
            ConfigureTitlePrefab(titleRoot, context);
            ConfigureAchievementPrefab(achievementRoot, context);

            List<string> errors = new List<string>();
            ValidateTitleLayout(titleRoot, errors);
            ValidateAchievementLayout(achievementRoot, errors);
            ValidateLayoutHeight(titleRoot, 180f, "TitleSlot", errors);
            ValidateLayoutHeight(achievementRoot, 300f, "AchievementSlot", errors);
            if (errors.Count > 0)
                throw new System.InvalidOperationException("저장 전 레이아웃 검증 실패\n- " +
                                                           string.Join("\n- ", errors));

            saveStarted = true;
            if (PrefabUtility.SaveAsPrefabAsset(titleRoot, TitlePrefabPath) == null)
                throw new System.InvalidOperationException("TitleSlot.prefab 저장 결과가 null입니다.");
            if (PrefabUtility.SaveAsPrefabAsset(achievementRoot, AchievementPrefabPath) == null)
                throw new System.InvalidOperationException("AchievementSlot.prefab 저장 결과가 null입니다.");
            AssetDatabase.SaveAssets();
            Debug.Log("[HabitPVP 15-D-5B] 두 슬롯 Prefab 원본 Apply 완료. " +
                      "Canvas.prefab과 Scene은 저장하지 않았습니다.");
        }
        catch (System.Exception exception)
        {
            if (saveStarted)
            {
                File.WriteAllBytes(TitlePrefabPath, titleBackup);
                File.WriteAllBytes(AchievementPrefabPath, achievementBackup);
                AssetDatabase.ImportAsset(TitlePrefabPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(AchievementPrefabPath, ImportAssetOptions.ForceUpdate);
            }
            Debug.LogError("[HabitPVP 15-D-5B] Slot Prefab Apply 실패. " +
                           (saveStarted ? "두 Prefab을 Apply 직전 상태로 복원했습니다.\n" :
                               "저장 전 실패하여 원본 Prefab은 변경되지 않았습니다.\n") + exception);
        }
        finally
        {
            if (titleRoot != null) PrefabUtility.UnloadPrefabContents(titleRoot);
            if (achievementRoot != null) PrefabUtility.UnloadPrefabContents(achievementRoot);
        }
    }

    private static void ConfigureTitlePrefab(GameObject root, Context context)
    {
        TitleSlotUI slot = root.GetComponent<TitleSlotUI>();
        SerializedObject serialized = new SerializedObject(slot);
        Image badge = serialized.FindProperty("titleBadgeImage").objectReferenceValue as Image;
        TMP_Text nameText = GetOrCreateText(root.transform, "TitleNameText", context.Font);
        TMP_Text statusText = GetOrCreateText(root.transform, "EquippedStatusText", context.Font);
        Image border = serialized.FindProperty("borderImage").objectReferenceValue as Image;
        Button button = serialized.FindProperty("slotButton").objectReferenceValue as Button;
        Image card = root.GetComponent<Image>();

        SetImage(card, context.CardSprite, CreamLight, Image.Type.Sliced);
        SetImage(border, context.GoldButtonSprite, Color.white, Image.Type.Sliced);
        border.raycastTarget = false;
        badge.preserveAspect = true;
        badge.raycastTarget = false;
        SetAnchoredRect(badge.rectTransform, new Vector2(0f, 0.5f),
            new Vector2(105f, 0f), new Vector2(120f, 120f));
        SetText(nameText, context.Font, 34f, Brown, TextAlignmentOptions.Left);
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 26f;
        nameText.fontSizeMax = 34f;
        RectTransform nameRect = nameText.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.offsetMin = new Vector2(190f, 30f);
        nameRect.offsetMax = new Vector2(-175f, -30f);
        SetText(statusText, context.Font, 25f, Gold, TextAlignmentOptions.Center);
        statusText.text = "장착 중";
        statusText.raycastTarget = false;
        SetAnchoredRect(statusText.rectTransform, new Vector2(1f, 0.5f),
            new Vector2(-88f, 0f), new Vector2(140f, 52f));
        serialized.FindProperty("titleNameText").objectReferenceValue = nameText;
        serialized.FindProperty("equippedStatusText").objectReferenceValue = statusText;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        SetRootSizeAndLayout(root, new Vector2(800f, 180f), 180f);
        StyleButton(button, context.CardSprite, context.Font);
    }

    private static void ApplyRewardPreview(Context context)
    {
        RectTransform root = context.PreviewPopup.GetComponent<RectTransform>();
        Image dim = context.PreviewDimCloser.GetComponent<Image>();
        Image panel = GetOrCreateImage(root, "RewardPreviewPanel");
        TMP_Text typeText = GetOrCreateText(context.PreviewPopup.transform, "RewardTypeText", context.Font);
        TMP_Text nameText = GetOrCreateText(context.PreviewPopup.transform, "RewardTitleNameText", context.Font);
        Button closeButton = GetOrCreateButton(root, "RewardPreviewCloseButton");
        TMP_Text closeText = GetOrCreateText(closeButton.transform, "Label", context.Font);

        SetStretchRect(root, Vector2.zero, Vector2.zero);
        SetImage(dim, null, new Color32(0x20, 0x18, 0x18, 0xA6), Image.Type.Simple);
        dim.raycastTarget = true;
        SetImage(panel, context.CardSprite, CreamLight, Image.Type.Sliced);
        panel.raycastTarget = true;
        SetAnchoredRect(panel.rectTransform, new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(720f, 760f));

        SetText(typeText, context.Font, 38f, Brown, TextAlignmentOptions.Center);
        SetText(nameText, context.Font, 40f, Brown, TextAlignmentOptions.Center);
        typeText.text = "칭호 보상";
        nameText.text = "칭호 이름";
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 28f;
        nameText.fontSizeMax = 40f;
        typeText.raycastTarget = false;
        nameText.raycastTarget = false;

        RectTransform imageRect = context.PreviewTitleImage.rectTransform;
        Record(imageRect);
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = new Vector2(0f, 25f);
        imageRect.sizeDelta = new Vector2(300f, 300f);
        context.PreviewTitleImage.preserveAspect = true;
        context.PreviewTitleImage.raycastTarget = false;

        SetAnchoredRect(typeText.rectTransform, new Vector2(0.5f, 0.5f),
            new Vector2(0f, 285f), new Vector2(520f, 72f));
        SetAnchoredRect(nameText.rectTransform, new Vector2(0.5f, 0.5f),
            new Vector2(0f, -175f), new Vector2(580f, 86f));

        StyleButton(closeButton, context.GoldButtonSprite, context.Font);
        SetAnchoredRect(closeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
            new Vector2(300f, 320f), new Vector2(76f, 76f));
        SetText(closeText, context.Font, 34f, Brown, TextAlignmentOptions.Center);
        closeText.text = "X";
        closeText.raycastTarget = false;
        SetStretchRect(closeText.rectTransform, new Vector2(8f, 8f), new Vector2(-8f, -8f));
        EnsureClosePreviewListener(closeButton, context.TitleManager);

        context.PreviewDimCloser.transform.SetSiblingIndex(0);
        panel.transform.SetSiblingIndex(1);
        context.PreviewTitleImage.transform.SetSiblingIndex(2);
        typeText.transform.SetSiblingIndex(3);
        nameText.transform.SetSiblingIndex(4);
        closeButton.transform.SetSiblingIndex(5);

        SerializedObject title = new SerializedObject(context.TitleManager);
        title.FindProperty("previewTitleNameText").objectReferenceValue = nameText;
        title.FindProperty("previewRewardTypeText").objectReferenceValue = typeText;
        title.ApplyModifiedProperties();
        RecordPrefabChanges(context.PreviewPopup);
    }

    private static Image GetOrCreateImage(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        Image image = existing != null ? existing.GetComponent<Image>() : null;
        if (image != null) return image;
        if (existing != null)
            throw new System.InvalidOperationException(objectName + " 오브젝트에 Image가 없습니다.");
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        imageObject.layer = parent.gameObject.layer;
        Undo.RegisterCreatedObjectUndo(imageObject, "Create 15-D-5B Reward Preview Panel");
        imageObject.transform.SetParent(parent, false);
        return imageObject.GetComponent<Image>();
    }

    private static Button GetOrCreateButton(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        Button button = existing != null ? existing.GetComponent<Button>() : null;
        if (button != null && existing.GetComponent<Image>() != null) return button;
        if (existing != null)
            throw new System.InvalidOperationException(objectName + " 오브젝트에 Button/Image가 없습니다.");
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.layer = parent.gameObject.layer;
        Undo.RegisterCreatedObjectUndo(buttonObject, "Create 15-D-5B Reward Preview Close Button");
        buttonObject.transform.SetParent(parent, false);
        return buttonObject.GetComponent<Button>();
    }

    private static void EnsureClosePreviewListener(Button button, TitleManager manager)
    {
        for (int index = 0; index < button.onClick.GetPersistentEventCount(); index++)
            if (button.onClick.GetPersistentTarget(index) == manager &&
                button.onClick.GetPersistentMethodName(index) == nameof(TitleManager.ClosePreview))
                return;
        Record(button);
        UnityEventTools.AddPersistentListener(button.onClick, manager.ClosePreview);
        EditorUtility.SetDirty(button);
    }

    private static TMP_Text GetOrCreateText(Transform parent, string objectName, TMP_FontAsset font)
    {
        Transform existing = parent.Find(objectName);
        TMP_Text text = existing != null ? existing.GetComponent<TMP_Text>() : null;
        if (text != null) return text;
        if (existing != null)
            throw new System.InvalidOperationException(objectName + " 오브젝트에 TMP_Text가 없습니다.");

        GameObject textObject = new GameObject(objectName, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;
        Undo.RegisterCreatedObjectUndo(textObject, "Create 15-D-5B UI Text");
        textObject.transform.SetParent(parent, false);
        text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.raycastTarget = false;
        return text;
    }

    private static void SetAnchoredRect(RectTransform rect, Vector2 anchor, Vector2 position,
        Vector2 size)
    {
        if (rect == null) throw new System.InvalidOperationException("필수 RectTransform이 없습니다.");
        Record(rect);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetStretchRect(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rect == null) throw new System.InvalidOperationException("필수 RectTransform이 없습니다.");
        Record(rect);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void SetRootSizeAndLayout(GameObject root, Vector2 size, float preferredHeight)
    {
        RectTransform rect = root.GetComponent<RectTransform>();
        Record(rect);
        rect.sizeDelta = size;
        LayoutElement layout = root.GetComponent<LayoutElement>();
        if (layout == null) layout = Undo.AddComponent<LayoutElement>(root);
        Record(layout);
        layout.minHeight = preferredHeight;
        layout.preferredHeight = preferredHeight;
        layout.flexibleHeight = 0f;
    }

    private static void ConfigureAchievementPrefab(GameObject root, Context context)
    {
        AchievementSlotUI slot = root.GetComponent<AchievementSlotUI>();
        SerializedObject serialized = new SerializedObject(slot);
        TMP_Text title = serialized.FindProperty("titleText").objectReferenceValue as TMP_Text;
        if (!TryResolveAchievementProgress(serialized, out Image fill, out TMP_Text progress,
                out RectTransform barRoot, out Image barBase, out string progressError))
            throw new System.InvalidOperationException(progressError);
        Button check = serialized.FindProperty("checkRewardButton").objectReferenceValue as Button;
        Button claim = serialized.FindProperty("rewardClaimButton").objectReferenceValue as Button;
        TMP_Text claimText = serialized.FindProperty("rewardClaimText").objectReferenceValue as TMP_Text;
        Image card = root.GetComponent<Image>();

            SetImage(card, context.CardSprite, CreamLight, Image.Type.Sliced);
            SetText(title, context.Font, 30f, Brown, TextAlignmentOptions.Left);
            title.enableAutoSizing = true;
            title.fontSizeMin = 25f;
            title.fontSizeMax = 32f;
            title.raycastTarget = false;
            SetAnchoredRect(title.rectTransform, new Vector2(0.5f, 1f),
                new Vector2(0f, -48f), new Vector2(720f, 62f));
            SetText(progress, context.Font, 26f, MutedBrown, TextAlignmentOptions.Left);
            progress.enableAutoSizing = true;
            progress.fontSizeMin = 22f;
            progress.fontSizeMax = 27f;
            progress.raycastTarget = false;
            SetText(claimText, context.Font, 25f, Brown, TextAlignmentOptions.Center);
            StyleButton(check, context.CreamButtonSprite, context.Font);
            StyleButton(claim, context.GoldButtonSprite, context.Font);
            foreach (TMP_Text buttonText in check.GetComponentsInChildren<TMP_Text>(true)
                         .Concat(claim.GetComponentsInChildren<TMP_Text>(true)))
            {
                SetText(buttonText, context.Font, 25f, Brown, TextAlignmentOptions.Center);
                buttonText.enableAutoSizing = true;
                buttonText.fontSizeMin = 21f;
                buttonText.fontSizeMax = 26f;
                buttonText.raycastTarget = false;
            }
            SetAnchoredRect(check.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
                new Vector2(-180f, 45f), new Vector2(280f, 70f));
            SetAnchoredRect(claim.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
                new Vector2(180f, 45f), new Vector2(280f, 70f));

            SetImage(barBase, context.BarSprite, Color.white, Image.Type.Sliced);
            SetImage(fill, context.BarFillSprite, fill.color, fill.type);
            SetAnchoredRect(barRoot, new Vector2(0.5f, 1f),
                new Vector2(0f, -140f), new Vector2(700f, 82f));
            SetAnchoredRect(progress.rectTransform, new Vector2(0.5f, 1f),
                new Vector2(0f, -20f), new Vector2(700f, 36f));
            SetAnchoredRect(barBase.rectTransform, new Vector2(0.5f, 0f),
                new Vector2(0f, 18f), new Vector2(700f, 36f));
            SetAnchoredRect(fill.rectTransform, new Vector2(0.5f, 0f),
                new Vector2(-342f, 18f), new Vector2(684f, 24f));
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            SetRootSizeAndLayout(root, new Vector2(800f, 300f), 300f);
    }

    private static List<string> ValidateSceneDesign(Context context)
    {
        List<string> errors = new List<string>();
        ValidatePopupStyle(context.TitlePopup, context.TitleScroll, context, "칭호", errors);
        ValidatePopupStyle(context.AchievementPopup, context.AchievementScroll, context, "업적", errors);
        ValidateRewardPreviewDesign(context, errors);
        return errors;
    }

    private static void ValidateRewardPreviewApplyRequirements(Context context,
        ICollection<string> errors)
    {
        if (context.PreviewPopup == null || context.PreviewTitleImage == null ||
            context.PreviewDimCloser == null) return;
        if (context.PreviewPopup.GetComponent<RectTransform>() == null)
            errors.Add("PreviewTitlePopup에 RectTransform이 없습니다.");
        if (context.PreviewDimCloser.GetComponent<Image>() == null)
            errors.Add("PreviewTitlePopup/Dim에 Image가 없습니다.");
        if (context.PreviewDimCloser.transform.parent != context.PreviewPopup.transform)
            errors.Add("previewDimCloser가 PreviewTitlePopup의 직접 자식이 아닙니다: " +
                       PathOf(context.PreviewDimCloser));
        if (context.PreviewTitleImage.transform.parent != context.PreviewPopup.transform)
            errors.Add("previewTitleImage가 PreviewTitlePopup의 직접 자식이 아닙니다: " +
                       PathOf(context.PreviewTitleImage));
        ValidateOptionalComponent<Image>(context.PreviewPopup.transform, "RewardPreviewPanel", errors);
        ValidateOptionalComponent<Button>(context.PreviewPopup.transform, "RewardPreviewCloseButton", errors);
        ValidateOptionalComponent<TMP_Text>(context.PreviewPopup.transform, "RewardTypeText", errors);
        ValidateOptionalComponent<TMP_Text>(context.PreviewPopup.transform, "RewardTitleNameText", errors);
    }

    private static void ValidateOptionalComponent<T>(Transform parent, string objectName,
        ICollection<string> errors) where T : Component
    {
        Transform existing = parent.Find(objectName);
        if (existing != null && existing.GetComponent<T>() == null)
            errors.Add(PathOf(parent) + "/" + objectName + "에 " + typeof(T).Name + "가 없습니다.");
    }

    private static void ValidateRewardPreviewDesign(Context context, ICollection<string> errors)
    {
        RectTransform root = context.PreviewPopup.GetComponent<RectTransform>();
        Image dim = context.PreviewDimCloser.GetComponent<Image>();
        Image panel = root.Find("RewardPreviewPanel")?.GetComponent<Image>();
        Button close = root.Find("RewardPreviewCloseButton")?.GetComponent<Button>();
        TMP_Text typeText = root.Find("RewardTypeText")?.GetComponent<TMP_Text>();
        TMP_Text nameText = root.Find("RewardTitleNameText")?.GetComponent<TMP_Text>();
        if (panel == null) errors.Add("RewardPreviewPanel Image가 없습니다.");
        if (close == null) errors.Add("RewardPreviewCloseButton Button이 없습니다.");
        if (typeText == null) errors.Add("RewardTypeText가 없습니다.");
        if (nameText == null) errors.Add("RewardTitleNameText가 없습니다.");
        if (dim == null || !dim.raycastTarget || dim.color.a < 0.5f)
            errors.Add("보상 미리보기 Dim이 입력 차단 가능한 반투명 Overlay가 아닙니다.");
        if (panel == null || panel.sprite != context.CardSprite || panel.type != Image.Type.Sliced)
            errors.Add("보상 미리보기 중앙 Panel 스타일이 적용되지 않았습니다.");
        if (panel == null || close == null || typeText == null || nameText == null) return;

        RectTransform panelRect = panel.rectTransform;
        ValidateContained(panelRect, context.PreviewTitleImage.rectTransform, "보상 칭호 이미지", errors);
        ValidateContained(panelRect, typeText.rectTransform, "보상 유형 텍스트", errors);
        ValidateContained(panelRect, nameText.rectTransform, "보상 칭호 이름", errors);
        ValidateContained(panelRect, close.GetComponent<RectTransform>(), "보상 미리보기 닫기 버튼", errors);
        if (!HasPersistentListener(close, context.TitleManager, nameof(TitleManager.ClosePreview)))
            errors.Add("보상 미리보기 닫기 버튼에 TitleManager.ClosePreview가 연결되지 않았습니다.");
        if (context.PreviewDimCloser.transform.GetSiblingIndex() >= panel.transform.GetSiblingIndex())
            errors.Add("보상 미리보기 Dim이 중앙 Panel보다 앞에 배치되어 있습니다.");
    }

    private static bool HasPersistentListener(Button button, Object target, string methodName)
    {
        for (int index = 0; index < button.onClick.GetPersistentEventCount(); index++)
            if (button.onClick.GetPersistentTarget(index) == target &&
                button.onClick.GetPersistentMethodName(index) == methodName)
                return true;
        return false;
    }

    private static void ValidatePopupStyle(GameObject popup, ScrollRect scroll, Context context,
        string label, ICollection<string> errors)
    {
        Image image = popup.GetComponent<Image>();
        if (image == null || image.sprite != context.CardSprite || image.type != Image.Type.Sliced)
            errors.Add(label + " Popup 배경 스타일이 적용되지 않았습니다.");
        if (popup.GetComponentsInChildren<TMP_Text>(true).Any(text => text.font != context.Font))
            errors.Add(label + " Popup에 NotoSansKR이 아닌 TMP 텍스트가 있습니다.");
        if (scroll.content.GetComponent<LayoutGroup>() == null ||
            scroll.content.GetComponent<ContentSizeFitter>() == null)
            errors.Add(label + " Content 레이아웃 구성요소가 손상되었습니다.");
    }

    private static List<string> ValidatePrefabDesign(Context context)
    {
        List<string> errors = new List<string>();
        GameObject titleRoot = AssetDatabase.LoadAssetAtPath<GameObject>(TitlePrefabPath);
        GameObject achievementRoot = AssetDatabase.LoadAssetAtPath<GameObject>(AchievementPrefabPath);
        TitleSlotUI titleSlot = titleRoot != null ? titleRoot.GetComponent<TitleSlotUI>() : null;
        AchievementSlotUI achievementSlot = achievementRoot != null
            ? achievementRoot.GetComponent<AchievementSlotUI>() : null;
        if (titleSlot == null) errors.Add("TitleSlot.prefab에 TitleSlotUI가 없습니다.");
        if (achievementSlot == null) errors.Add("AchievementSlot.prefab에 AchievementSlotUI가 없습니다.");
        if (titleRoot != null && titleRoot.GetComponent<Image>()?.sprite != context.CardSprite)
            errors.Add("TitleSlot 카드 Sprite가 적용되지 않았습니다.");
        if (titleSlot != null && new SerializedObject(titleSlot).FindProperty("titleNameText")
                .objectReferenceValue == null)
            errors.Add("TitleSlot.titleNameText 참조가 없습니다.");
        if (titleSlot != null && new SerializedObject(titleSlot).FindProperty("equippedStatusText")
                .objectReferenceValue == null)
            errors.Add("TitleSlot.equippedStatusText 참조가 없습니다.");
        if (achievementRoot != null && achievementRoot.GetComponent<Image>()?.sprite != context.CardSprite)
            errors.Add("AchievementSlot 카드 Sprite가 적용되지 않았습니다.");
        if (achievementRoot != null && achievementRoot.GetComponentsInChildren<TMP_Text>(true)
                .Any(text => text.font != context.Font))
            errors.Add("AchievementSlot에 NotoSansKR이 아닌 TMP 텍스트가 있습니다.");
        if (titleRoot != null) ValidateTitleLayout(titleRoot, errors);
        if (achievementRoot != null) ValidateAchievementLayout(achievementRoot, errors);
        ValidateLayoutHeight(titleRoot, 180f, "TitleSlot", errors);
        ValidateLayoutHeight(achievementRoot, 300f, "AchievementSlot", errors);
        return errors;
    }

    private static List<string> ValidatePrefabApplyRequirements()
    {
        List<string> errors = new List<string>();
        GameObject titleRoot = AssetDatabase.LoadAssetAtPath<GameObject>(TitlePrefabPath);
        GameObject achievementRoot = AssetDatabase.LoadAssetAtPath<GameObject>(AchievementPrefabPath);
        ValidateRequiredReferences<TitleSlotUI>(titleRoot, TitlePrefabPath,
            new[] { "titleBadgeImage", "borderImage", "slotButton" }, errors);
        ValidateRootGraphics(titleRoot, TitlePrefabPath, errors);
        ValidateOptionalTextObject(titleRoot, "TitleNameText", TitlePrefabPath, errors);
        ValidateOptionalTextObject(titleRoot, "EquippedStatusText", TitlePrefabPath, errors);
        AchievementSlotUI achievement = ValidateRequiredReferences<AchievementSlotUI>(achievementRoot,
            AchievementPrefabPath,
            new[]
            {
                "titleText", "progressBarFill", "progressText", "checkRewardButton",
                "rewardClaimButton", "rewardClaimText"
            }, errors);
        ValidateRootGraphics(achievementRoot, AchievementPrefabPath, errors);
        if (achievement != null)
        {
            SerializedObject serialized = new SerializedObject(achievement);
            if (!TryResolveAchievementProgress(serialized, out _, out _, out _, out _,
                    out string progressError))
                errors.Add(AchievementPrefabPath + ": " + progressError);
            foreach (string buttonProperty in new[] { "checkRewardButton", "rewardClaimButton" })
            {
                Button button = serialized.FindProperty(buttonProperty).objectReferenceValue as Button;
                if (button != null && button.GetComponent<RectTransform>() == null)
                    errors.Add(AchievementPrefabPath + ": " + buttonProperty +
                               " 오브젝트에 RectTransform이 없습니다.");
                if (button != null && button.GetComponentsInChildren<TMP_Text>(true).Length == 0)
                    errors.Add(AchievementPrefabPath + ": " + buttonProperty +
                               " 하위에 TMP_Text가 없습니다.");
            }
        }
        return errors;
    }

    private static void ValidateRootGraphics(GameObject root, string assetPath,
        ICollection<string> errors)
    {
        if (root == null) return;
        if (root.GetComponent<RectTransform>() == null)
            errors.Add(assetPath + " Root에 RectTransform이 없습니다.");
        if (root.GetComponent<Image>() == null)
            errors.Add(assetPath + " Root에 카드 Image가 없습니다.");
    }

    private static void ValidateOptionalTextObject(GameObject root, string objectName, string assetPath,
        ICollection<string> errors)
    {
        if (root == null) return;
        Transform existing = root.transform.Find(objectName);
        if (existing != null && existing.GetComponent<TMP_Text>() == null)
            errors.Add(assetPath + ": " + objectName + " 오브젝트에 TMP_Text가 없습니다.");
    }

    private static T ValidateRequiredReferences<T>(GameObject root, string assetPath,
        IEnumerable<string> properties, ICollection<string> errors) where T : Component
    {
        if (root == null)
        {
            errors.Add("Prefab을 찾을 수 없습니다: " + assetPath);
            return null;
        }
        T component = root.GetComponent<T>();
        if (component == null)
        {
            errors.Add(assetPath + " Root에 " + typeof(T).Name + "가 없습니다.");
            return null;
        }
        SerializedObject serialized = new SerializedObject(component);
        foreach (string propertyName in properties)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                errors.Add(assetPath + ": " + typeof(T).Name + "." + propertyName +
                           " SerializeField를 찾을 수 없습니다.");
            else if (property.objectReferenceValue == null)
                errors.Add(assetPath + ": " + typeof(T).Name + "." + propertyName +
                           " 참조가 없습니다.");
        }
        return component;
    }

    private static bool TryResolveAchievementProgress(SerializedObject serialized, out Image fill,
        out TMP_Text progress, out RectTransform barRoot, out Image barBase, out string error)
    {
        fill = serialized.FindProperty("progressBarFill")?.objectReferenceValue as Image;
        progress = serialized.FindProperty("progressText")?.objectReferenceValue as TMP_Text;
        barRoot = fill != null ? fill.transform.parent as RectTransform : null;
        Transform baseTransform = barRoot != null ? barRoot.Find("ProgressBarBase") : null;
        barBase = baseTransform != null ? baseTransform.GetComponent<Image>() : null;
        if (fill == null) error = "AchievementSlotUI.progressBarFill 참조가 없습니다.";
        else if (progress == null) error = "AchievementSlotUI.progressText 참조가 없습니다.";
        else if (barRoot == null || barRoot.name != "ProgressBar")
            error = "progressBarFill의 직접 부모가 ProgressBar RectTransform이 아닙니다.";
        else if (progress.transform.parent != barRoot)
            error = "progressText가 ProgressBar의 직접 자식이 아닙니다: " +
                    PathOf(progress.transform);
        else if (baseTransform == null)
            error = "ProgressBar/ProgressBarBase 경로를 찾을 수 없습니다.";
        else if (barBase == null)
            error = "ProgressBar/ProgressBarBase에 Image가 없습니다.";
        else
        {
            error = null;
            return true;
        }
        return false;
    }

    private static void ValidateLayoutHeight(GameObject root, float expected, string label,
        ICollection<string> errors)
    {
        if (root == null) return;
        LayoutElement layout = root.GetComponent<LayoutElement>();
        if (layout == null || !Mathf.Approximately(layout.minHeight, expected) ||
            !Mathf.Approximately(layout.preferredHeight, expected))
            errors.Add(label + " LayoutElement 높이가 " + expected + "로 동기화되지 않았습니다.");
    }

    private static void ValidateTitleLayout(GameObject root, ICollection<string> errors)
    {
        SerializedObject serialized = new SerializedObject(root.GetComponent<TitleSlotUI>());
        RectTransform icon = (serialized.FindProperty("titleBadgeImage").objectReferenceValue as Image)
            ?.rectTransform;
        RectTransform name = (serialized.FindProperty("titleNameText").objectReferenceValue as TMP_Text)
            ?.rectTransform;
        RectTransform status = (serialized.FindProperty("equippedStatusText").objectReferenceValue as TMP_Text)
            ?.rectTransform;
        ValidateContained(root.transform as RectTransform, icon, "칭호 아이콘", errors);
        ValidateContained(root.transform as RectTransform, name, "칭호 이름", errors);
        ValidateContained(root.transform as RectTransform, status, "칭호 장착 상태", errors);
        if (RectsOverlap(root.transform as RectTransform, icon, name))
            errors.Add("칭호 아이콘과 이름 영역이 겹칩니다.");
        if (RectsOverlap(root.transform as RectTransform, name, status))
            errors.Add("칭호 이름과 장착 상태 영역이 겹칩니다.");
    }

    private static void ValidateAchievementLayout(GameObject root, ICollection<string> errors)
    {
        SerializedObject serialized = new SerializedObject(root.GetComponent<AchievementSlotUI>());
        TMP_Text title = serialized.FindProperty("titleText").objectReferenceValue as TMP_Text;
        TMP_Text progress = serialized.FindProperty("progressText").objectReferenceValue as TMP_Text;
        Image fill = serialized.FindProperty("progressBarFill").objectReferenceValue as Image;
        Button check = serialized.FindProperty("checkRewardButton").objectReferenceValue as Button;
        Button claim = serialized.FindProperty("rewardClaimButton").objectReferenceValue as Button;
        RectTransform card = root.transform as RectTransform;
        foreach (var item in new[]
                 {
                     (Rect: title?.rectTransform, Label: "업적 제목"),
                     (Rect: progress?.rectTransform, Label: "업적 진행률 텍스트"),
                     (Rect: (fill?.transform.parent as RectTransform)?.Find("ProgressBarBase") as RectTransform,
                         Label: "업적 진행률 바"),
                     (Rect: check?.GetComponent<RectTransform>(), Label: "보상 확인 버튼"),
                     (Rect: claim?.GetComponent<RectTransform>(), Label: "보상 수령 버튼")
                 })
            ValidateContained(card, item.Rect, item.Label, errors);
        RectTransform barBase = (fill?.transform.parent as RectTransform)
            ?.Find("ProgressBarBase") as RectTransform;
        if (RectsOverlap(card, progress?.rectTransform, barBase))
            errors.Add("업적 진행률 텍스트와 진행률 바가 겹칩니다.");
        if (RectsOverlap(card, check?.GetComponent<RectTransform>(), claim?.GetComponent<RectTransform>()))
            errors.Add("보상 확인 버튼과 보상 수령 버튼이 겹칩니다.");
    }

    private static void ValidateContained(RectTransform root, RectTransform target, string label,
        ICollection<string> errors)
    {
        if (root == null || target == null)
        {
            errors.Add(label + " RectTransform을 찾을 수 없습니다.");
            return;
        }
        Rect bounds = RelativeRect(root, target);
        Rect card = root.rect;
        const float tolerance = 0.5f;
        if (bounds.xMin < card.xMin - tolerance || bounds.xMax > card.xMax + tolerance ||
            bounds.yMin < card.yMin - tolerance || bounds.yMax > card.yMax + tolerance)
            errors.Add(label + "가 카드 경계를 벗어났습니다: " + bounds);
    }

    private static bool RectsOverlap(RectTransform root, RectTransform first, RectTransform second)
    {
        return root != null && first != null && second != null &&
               RelativeRect(root, first).Overlaps(RelativeRect(root, second));
    }

    private static Rect RelativeRect(RectTransform root, RectTransform target)
    {
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(root, target);
        return Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);
    }

    private static ScrollRect FindOwningScroll(Transform content, string label,
        ICollection<string> errors)
    {
        if (content == null) return null;
        ScrollRect[] candidates = content.GetComponentsInParent<ScrollRect>(true)
            .Where(scroll => scroll.content == content).ToArray();
        if (candidates.Length != 1)
        {
            errors.Add(label + " Content를 소유한 ScrollRect가 정확히 하나가 아닙니다: " + candidates.Length);
            return null;
        }
        return candidates[0];
    }

    private static T FindUnique<T>(Scene scene, ICollection<string> errors) where T : Component
    {
        T[] values = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        if (values.Length != 1)
        {
            errors.Add(typeof(T).Name + " 개수가 1이 아닙니다: " + values.Length);
            return null;
        }
        return values[0];
    }

    private static T ObjectReference<T>(SerializedObject owner, string propertyName,
        ICollection<string> errors) where T : Object
    {
        SerializedProperty property = owner.FindProperty(propertyName);
        T value = property?.objectReferenceValue as T;
        if (value == null) errors.Add(owner.targetObject.GetType().Name + "." + propertyName + " 참조가 없습니다.");
        return value;
    }

    private static T Load<T>(string path, ICollection<string> errors) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) errors.Add("필수 Asset을 찾을 수 없습니다: " + path);
        return asset;
    }

    private static bool IsCanvasInstanceChild(GameObject target)
    {
        if (target == null || !PrefabUtility.IsPartOfPrefabInstance(target)) return false;
        GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(target);
        return PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root) == CanvasPrefabPath;
    }

    private static bool CanApply(Context context)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[HabitPVP 15-D-5B] Edit Mode에서만 Apply할 수 있습니다.");
            return false;
        }
        if (context.Errors.Count == 0) return true;
        LogResult("Apply 중단", context.Errors, StructureSummary(context));
        return false;
    }

    private static void StyleButton(Button button, Sprite sprite, TMP_FontAsset font)
    {
        if (button == null) return;
        Image image = button.GetComponent<Image>();
        if (image != null) SetImage(image, sprite, Color.white, Image.Type.Sliced);
        foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
            SetText(text, font, Mathf.Max(22f, text.fontSize), Brown, TextAlignmentOptions.Center);
    }

    private static void SetImage(Image image, Sprite sprite, Color color, Image.Type type)
    {
        if (image == null) return;
        Record(image);
        image.sprite = sprite;
        image.color = color;
        image.type = type;
    }

    private static void SetText(TMP_Text text, TMP_FontAsset font, float size, Color color,
        TextAlignmentOptions alignment)
    {
        if (text == null) return;
        Record(text);
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
    }

    private static void Record(Object target)
    {
        if (target != null) Undo.RecordObject(target, "Apply 15-D-5B Title Achievement UI");
    }

    private static void RecordPrefabChanges(GameObject root)
    {
        foreach (Component component in root.GetComponentsInChildren<Component>(true))
            if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
    }

    private static string StructureSummary(Context context)
    {
        return "Scene=" + SceneManager.GetActiveScene().path +
               "\nCanvas source=" + CanvasPrefabPath +
               "\nTitle: " + PathOf(context.TitlePopup) + " -> " + PathOf(context.TitleContent) +
               ", runtime prefab=" + AssetDatabase.GetAssetPath(context.TitleSlot) +
               "\nAchievement: " + PathOf(context.AchievementPopup) + " -> " +
               PathOf(context.AchievementContent) + ", runtime prefab=" +
               AssetDatabase.GetAssetPath(context.AchievementSlot) +
               "\nactiveSelf: Title=" + (context.TitlePopup != null && context.TitlePopup.activeSelf) +
               ", Achievement=" + (context.AchievementPopup != null && context.AchievementPopup.activeSelf);
    }

    private static string PathOf(Object value)
    {
        Component component = value as Component;
        Transform transform = component != null ? component.transform : (value as GameObject)?.transform;
        if (transform == null) return "<missing>";
        List<string> parts = new List<string>();
        while (transform != null) { parts.Add(transform.name); transform = transform.parent; }
        parts.Reverse();
        return string.Join("/", parts);
    }

    private static void LogResult(string label, IReadOnlyCollection<string> errors, string details)
    {
        if (errors.Count == 0)
            Debug.Log("[HabitPVP 15-D-5B] " + label + " 성공\n" + details);
        else
            Debug.LogError("[HabitPVP 15-D-5B] " + label + " 실패 (" + errors.Count + ")\n- " +
                           string.Join("\n- ", errors) + "\n" + details);
    }

    private sealed class Context
    {
        public TitleManager TitleManager;
        public AchievementManager AchievementManager;
        public GameObject TitlePopup;
        public Transform TitleContent;
        public Button TitleClose;
        public TitleSlotUI TitleSlot;
        public GameObject PreviewPopup;
        public Image PreviewTitleImage;
        public DimCloser PreviewDimCloser;
        public ScrollRect TitleScroll;
        public GameObject AchievementPopup;
        public Transform AchievementContent;
        public Button AchievementClose;
        public AchievementSlotUI AchievementSlot;
        public ScrollRect AchievementScroll;
        public TMP_FontAsset Font;
        public Sprite CardSprite;
        public Sprite CreamButtonSprite;
        public Sprite GoldButtonSprite;
        public Sprite BarSprite;
        public Sprite BarFillSprite;
        public readonly List<string> Errors = new List<string>();
    }
}
