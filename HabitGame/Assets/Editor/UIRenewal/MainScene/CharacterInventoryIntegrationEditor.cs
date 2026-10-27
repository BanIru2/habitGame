using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Applies the approved inventory styling to the MainScene Canvas instance or ItemSlot prefab.
/// Scene changes are never saved automatically; prefab changes are saved only from the explicit
/// Apply ItemSlot Prefab menu.
/// </summary>
public static class CharacterInventoryIntegrationEditor
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string CanvasPrefabPath = "Assets/Resources/UIPrefabs/Canvas.prefab";
    private const string ItemSlotPrefabPath = "Assets/Resources/Prefabs/ItemSlot.prefab";
    private const string FontPath = "Assets/Login/Fonts/NotoSansKR Login SDF.asset";
    private const string CardSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/panel_rpg_card.png";
    private const string CreamButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_cream.png";
    private const string GoldButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_rpg.png";
    private const string MenuRoot =
        "Tools/HabitPVP/UI Renewal/MainScene/15-D-5C Inventory/";

    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 CreamLight = new Color32(0xFF, 0xF9, 0xEA, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x86, 0x68, 0x5D, 0xFF);
    private static readonly Color32 DescriptionBrown = new Color32(0x70, 0x50, 0x49, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);
    private static readonly Color32 BadgeGold = new Color32(0xF3, 0xD6, 0x91, 0xFF);
    private static readonly Color32 PopupDescriptionShadow = new Color32(0x3D, 0x24, 0x27, 0x70);
    private static readonly Color32 ExpBlue = new Color32(0x39, 0x9E, 0xE8, 0xFF);
    private static readonly Color32 ExpBlueDark = new Color32(0x20, 0x4F, 0x72, 0xFF);

    private static readonly string[] ManagerReferenceNames =
    {
        "characterPartsManager", "inventoryBackendManager", "itemSlotPrefab", "itemSlotParent",
        "scrollView", "equipButton", "consumableButton", "popupDim", "equipDetail",
        "equipIcon", "equipNameText", "equipDescText", "doEquipButton", "enhanceButton",
        "equipCloseButton", "doEquipButtonText", "consumDetail", "consumIcon",
        "consumNameText", "consumDescText", "consumCloseButton", "funcDetail", "funcIcon",
        "funcNameText", "funcDescText", "funcUseButton", "funcCloseButton", "enhanceTap",
        "enhanceTargetItemNameText", "enhanceTargetItemIcon", "enhanceTargetItemLevelText",
        "enhanceTargetItemExpText", "enhanceTargetItemExpBarFill", "enhanceButtonBar",
        "doEnhanceButton", "enhanceCancelButton"
    };

    [MenuItem(MenuRoot + "Validate Structure", false, 2350)]
    private static void ValidateStructure()
    {
        SceneContext context = BuildSceneContext(true);
        if (!context.IsValid) return;

        Debug.Log("[HabitPVP 15-D-5C] MainScene 구조 검증 성공. " +
                  "InventoryManager 참조, ScrollRect/Mask/Layout, 팝업, 강화 UI 및 5개 MainTabManager " +
                  "바인딩을 확인했습니다. Scene은 변경하지 않았습니다.", context.InventoryTap);
    }

    [MenuItem(MenuRoot + "Dry Run", false, 2351)]
    private static void DryRun()
    {
        SceneContext context = BuildSceneContext(true);
        if (!context.IsValid) return;

        Debug.Log(
            "[HabitPVP 15-D-5C] MainScene Dry Run (파일 변경 없음)\n" +
            "대상: Canvas/InventoryTap과 그 하위 TopBottons, InventoryScroll, PopupDim, " +
            "세 상세 팝업, EnhanceTap, EnhanceButtonBar.\n" +
            "스크롤: 런타임 InventoryManager가 기본 top/bottom=110/180, 강화=610/430을 " +
            "계속 적용할 수 있도록 stretch anchor와 좌우 offset만 디자인 기준으로 설정합니다.\n" +
            "보호: 공용 Canvas.prefab, MainTabManager, InventoryManager, 기존 UnityEvent, " +
            "activeSelf, 기능 오브젝트 이름, 다른 탭, Scene 저장.\n" +
            "주의: 탭 선택 색상은 런타임 코드가 별도 갱신하지 않으므로 기존 로직을 바꾸지 않고 " +
            "Button transition 범위에서만 강조합니다.", context.InventoryTap);
    }

    [MenuItem(MenuRoot + "Apply MainScene (No Auto Save)", false, 2352)]
    private static void ApplyMainScene()
    {
        SceneContext context = BuildSceneContext(true);
        if (!context.IsValid) return;

        SceneProtectionSnapshot snapshot = SceneProtectionSnapshot.Capture(context);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply 15-D-5C Inventory MainScene UI");

        try
        {
            ApplyInventoryStyle(context);
            ApplyPopupStyles(context);
            ApplyEnhanceStyle(context);
            snapshot.AssertUnchanged(context);
            Canvas.ForceUpdateCanvases();

            List<string> errors = ValidateSceneDesign(context);
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", errors));

            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = context.InventoryTap;
            Debug.Log("[HabitPVP 15-D-5C] MainScene 적용 성공. Scene은 저장하지 않았습니다. " +
                      "Canvas.prefab 원본과 게임 로직은 변경하지 않았습니다. 사용자가 Overrides와 " +
                      "Game View를 확인한 뒤 MainScene을 직접 저장하세요.", context.InventoryTap);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogException(exception);
            Debug.LogError("[HabitPVP 15-D-5C] MainScene Apply 실패. 이번 실행 변경을 Undo로 되돌렸습니다.");
        }
    }

    [MenuItem(MenuRoot + "Validate MainScene Design", false, 2353)]
    private static void ValidateMainSceneDesign()
    {
        SceneContext context = BuildSceneContext(true);
        if (!context.IsValid) return;
        LogValidation("MainScene 디자인", ValidateSceneDesign(context), context.InventoryTap);
    }

    [MenuItem(MenuRoot + "Validate ItemSlot Prefab", false, 2354)]
    private static void ValidateItemSlotPrefab()
    {
        WithItemSlotContents(false, context =>
        {
            if (!context.IsValid) return;
            Debug.Log("[HabitPVP 15-D-5C] ItemSlot Prefab 구조 검증 성공. GUID=" +
                      context.Guid + ". Prefab은 변경하지 않았습니다.", context.Root);
        });
    }

    [MenuItem(MenuRoot + "Dry Run ItemSlot", false, 2355)]
    private static void DryRunItemSlot()
    {
        WithItemSlotContents(false, context =>
        {
            if (!context.IsValid) return;
            Debug.Log(
                "[HabitPVP 15-D-5C] ItemSlot Dry Run (파일 변경 없음)\n" +
                "예정: 850x150 목록 카드, 116x116 좌측 아이콘, 중앙 이름/2줄 설명, " +
                "우측 상단 장비 부위, 우측 하단 수량 배지, NotoSansKR 및 RPG 카드 Sprite.\n" +
                "보호: Prefab GUID, ItemSlotUI 전체 SerializeField, Button/클릭 영역, " +
                "아이템 Sprite, normal/equipped/selected 런타임 색상 로직.", context.Root);
        });
    }

    [MenuItem(MenuRoot + "Apply ItemSlot Prefab", false, 2356)]
    private static void ApplyItemSlotPrefab()
    {
        WithItemSlotContents(true, context =>
        {
            if (!context.IsValid) return;

            ItemSlotProtectionSnapshot snapshot = ItemSlotProtectionSnapshot.Capture(context);
            ApplyItemSlotStyle(context);
            snapshot.AssertUnchanged(context);
            Canvas.ForceUpdateCanvases();
            List<string> errors = ValidateItemSlotDesign(context);
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", errors));
        });
    }

    [MenuItem(MenuRoot + "Validate ItemSlot Design", false, 2357)]
    private static void ValidateItemSlotDesignMenu()
    {
        WithItemSlotContents(false, context =>
        {
            if (!context.IsValid) return;
            LogValidation("ItemSlot 디자인", ValidateItemSlotDesign(context), context.Root);
        });
    }

    [MenuItem(MenuRoot + "Validate Structure", true)]
    [MenuItem(MenuRoot + "Dry Run", true)]
    [MenuItem(MenuRoot + "Apply MainScene (No Auto Save)", true)]
    [MenuItem(MenuRoot + "Validate MainScene Design", true)]
    private static bool CanUseSceneMenus() =>
        !EditorApplication.isPlayingOrWillChangePlaymode &&
        SceneManager.GetActiveScene().path == ScenePath;

    [MenuItem(MenuRoot + "Validate ItemSlot Prefab", true)]
    [MenuItem(MenuRoot + "Dry Run ItemSlot", true)]
    [MenuItem(MenuRoot + "Apply ItemSlot Prefab", true)]
    [MenuItem(MenuRoot + "Validate ItemSlot Design", true)]
    private static bool CanUsePrefabMenus() =>
        !EditorApplication.isPlayingOrWillChangePlaymode &&
        AssetDatabase.LoadAssetAtPath<GameObject>(ItemSlotPrefabPath) != null;

    private static SceneContext BuildSceneContext(bool log)
    {
        SceneContext context = new SceneContext { Scene = SceneManager.GetActiveScene() };
        if (!context.Scene.IsValid() || !context.Scene.isLoaded)
            context.Errors.Add("활성 Scene이 유효하지 않거나 로드되지 않았습니다.");
        else if (context.Scene.path != ScenePath)
            context.Errors.Add("MainScene에서만 실행할 수 있습니다. 현재: " + context.Scene.path);
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            context.Errors.Add("Edit Mode에서만 실행할 수 있습니다.");
        if (context.Errors.Count > 0) return Finish(context, log);

        Canvas[] canvases = context.Scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
            .Where(canvas => canvas.transform.parent == null && canvas.name == "Canvas").ToArray();
        if (canvases.Length != 1)
        {
            context.Errors.Add("Scene 루트 Canvas가 정확히 하나여야 합니다. 현재: " + canvases.Length);
            return Finish(context, log);
        }

        context.Canvas = canvases[0].gameObject;
        string canvasPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(context.Canvas);
        if (canvasPath != CanvasPrefabPath)
            context.Errors.Add("Canvas 원본 Prefab이 예상과 다릅니다: " + canvasPath);
        if (!PrefabUtility.IsPartOfPrefabInstance(context.Canvas))
            context.Errors.Add("MainScene Canvas가 Prefab Instance가 아닙니다.");

        Transform inventory = context.Canvas.transform.Find("InventoryTap");
        if (inventory == null)
        {
            context.Errors.Add("Canvas/InventoryTap을 찾을 수 없습니다.");
            return Finish(context, log);
        }
        context.InventoryTap = inventory.gameObject;

        InventoryManager[] managers = context.Scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<InventoryManager>(true)).ToArray();
        if (managers.Length != 1)
        {
            context.Errors.Add("InventoryManager가 정확히 하나여야 합니다. 현재: " + managers.Length);
            return Finish(context, log);
        }
        context.Manager = managers[0];
        context.TopButtons = Find<RectTransform>(inventory, "TopBottons", context.Errors);
        context.EquipmentButton = Find<Button>(inventory, "TopBottons/EquipmentButton", context.Errors);
        context.ConsumableButton = Find<Button>(inventory, "TopBottons/ConsumableButton", context.Errors);
        context.InventoryScroll = Find<ScrollRect>(inventory, "InventoryScroll", context.Errors);
        context.Viewport = Find<RectTransform>(inventory, "InventoryScroll/Viewport", context.Errors);
        context.Content = Find<RectTransform>(inventory, "InventoryScroll/Viewport/Content", context.Errors);
        context.PopupDim = Find<Image>(inventory, "PopupDim", context.Errors);
        ResolvePopup(context, inventory, "EquipItemDetailPopup", true);
        ResolvePopup(context, inventory, "ConsumItemDetailPopup", false);
        ResolvePopup(context, inventory, "FunctionalItemDetailPopup", false);

        SerializedObject manager = new SerializedObject(context.Manager);
        foreach (string name in ManagerReferenceNames)
        {
            SerializedProperty property = manager.FindProperty(name);
            if (property == null || property.objectReferenceValue == null)
                context.Errors.Add("InventoryManager 필수 참조가 비어 있습니다: " + name);
        }
        ValidateReference(manager, "itemSlotParent", context.Content, context.Errors);
        ValidateReference(manager, "scrollView", context.InventoryScroll != null
            ? context.InventoryScroll.GetComponent<RectTransform>() : null, context.Errors);
        ValidateReference(manager, "equipButton", context.EquipmentButton, context.Errors);
        ValidateReference(manager, "consumableButton", context.ConsumableButton, context.Errors);
        ValidateReference(manager, "popupDim", context.PopupDim != null ? context.PopupDim.gameObject : null,
            context.Errors);

        context.EnhanceTap = Reference<GameObject>(manager, "enhanceTap", context.Errors);
        context.EnhanceName = Reference<TMP_Text>(manager, "enhanceTargetItemNameText", context.Errors);
        context.EnhanceIcon = Reference<Image>(manager, "enhanceTargetItemIcon", context.Errors);
        context.EnhanceLevel = Reference<TMP_Text>(manager, "enhanceTargetItemLevelText", context.Errors);
        context.ExpText = Reference<TMP_Text>(manager, "enhanceTargetItemExpText", context.Errors);
        context.ExpFill = Reference<Image>(manager, "enhanceTargetItemExpBarFill", context.Errors);
        context.EnhanceButtonBar = Reference<GameObject>(manager, "enhanceButtonBar", context.Errors);
        context.DoEnhanceButton = Reference<Button>(manager, "doEnhanceButton", context.Errors);
        context.CancelEnhanceButton = Reference<Button>(manager, "enhanceCancelButton", context.Errors);
        if (context.ExpFill != null)
        {
            Transform root = context.ExpFill.transform.parent;
            context.ExpRoot = root as RectTransform;
            context.ExpBase = root != null ? root.Find("ExpBarBase")?.GetComponent<Image>() : null;
            if (root == null || root.name != "TargetItemExpRoot" || context.ExpBase == null)
                context.Errors.Add("TargetItemExpRoot/ExpBarBase/ExpBarFill 구조가 예상과 다릅니다.");
        }

        ValidateInventoryStructure(context);
        ValidateMainTabs(context);
        LoadAssets(context);
        return Finish(context, log);
    }

    private static void ResolvePopup(SceneContext context, Transform inventory, string name,
        bool equipment)
    {
        PopupParts popup = new PopupParts
        {
            Root = Find<Image>(inventory, name, context.Errors),
            Icon = Find<Image>(inventory, name + "/ItemIcon", context.Errors),
            Name = Find<TMP_Text>(inventory, name + "/ItemName", context.Errors),
            Description = Find<TMP_Text>(inventory, name + "/ItemDescription", context.Errors),
            Close = Find<Button>(inventory, name + "/CloseButton", context.Errors)
        };
        if (equipment)
        {
            popup.Primary = Find<Button>(inventory, name + "/EquipButton", context.Errors);
            popup.Secondary = Find<Button>(inventory, name + "/EnhanceButton", context.Errors);
            context.EquipPopup = popup;
        }
        else if (name == "FunctionalItemDetailPopup")
        {
            popup.Primary = Find<Button>(inventory, name + "/UseButton", context.Errors);
            context.FunctionalPopup = popup;
        }
        else
        {
            context.ConsumablePopup = popup;
        }
    }

    private static void ValidateInventoryStructure(SceneContext context)
    {
        if (context.InventoryScroll != null && context.InventoryScroll.viewport != context.Viewport)
            context.Errors.Add("ScrollRect.viewport가 InventoryScroll/Viewport를 가리키지 않습니다.");
        if (context.InventoryScroll != null && context.InventoryScroll.content != context.Content)
            context.Errors.Add("ScrollRect.content가 InventoryScroll/Viewport/Content를 가리키지 않습니다.");
        if (context.Viewport != null && context.Viewport.GetComponent<Mask>() == null &&
            context.Viewport.GetComponent<RectMask2D>() == null)
            context.Errors.Add("InventoryScroll/Viewport에 Mask 또는 RectMask2D가 없습니다.");
        if (context.Content != null && context.Content.GetComponent<VerticalLayoutGroup>() == null)
            context.Errors.Add("InventoryScroll Content에 VerticalLayoutGroup이 없습니다.");
        if (context.Content != null && context.Content.GetComponent<ContentSizeFitter>() == null)
            context.Errors.Add("InventoryScroll Content에 ContentSizeFitter가 없습니다.");
        if (context.EnhanceTap != null && context.EnhanceTap.transform.parent != context.InventoryTap.transform)
            context.Errors.Add("EnhanceTap이 InventoryTap의 직속 자식이 아닙니다.");
        if (context.EnhanceButtonBar != null &&
            context.EnhanceButtonBar.transform.parent != context.InventoryTap.transform)
            context.Errors.Add("EnhanceButtonBar가 InventoryTap의 직속 자식이 아닙니다.");
    }

    private static void ValidateMainTabs(SceneContext context)
    {
        MainTabManager[] managers = context.Scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MainTabManager>(true)).ToArray();
        if (managers.Length != 1)
        {
            context.Errors.Add("MainTabManager가 정확히 하나여야 합니다. 현재: " + managers.Length);
            return;
        }
        context.TabManager = managers[0];
        SerializedObject tab = new SerializedObject(context.TabManager);
        foreach (string name in new[]
                 {
                     "habitTap", "characterTap", "inventoryTap", "shopTap", "battleTap",
                     "habitTapButton", "characterTapButton", "inventoryTapButton", "shopTapButton",
                     "battleTapButton"
                 })
        {
            SerializedProperty property = tab.FindProperty(name);
            if (property == null || property.objectReferenceValue == null)
                context.Errors.Add("MainTabManager 참조가 비어 있습니다: " + name);
        }
        ValidateReference(tab, "inventoryTap", context.InventoryTap, context.Errors);
    }

    private static void LoadAssets(SceneContext context)
    {
        context.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        context.CardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardSpritePath);
        context.CreamButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CreamButtonSpritePath);
        context.GoldButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GoldButtonSpritePath);
        if (context.Font == null || context.CardSprite == null || context.CreamButtonSprite == null ||
            context.GoldButtonSprite == null)
            context.Errors.Add("필요한 NotoSansKR 또는 Free Casual GUI Sprite를 찾지 못했습니다.");
    }

    private static void ApplyInventoryStyle(SceneContext context)
    {
        Image background = context.InventoryTap.GetComponent<Image>();
        if (background == null) background = Undo.AddComponent<Image>(context.InventoryTap);
        SetImage(background, null, Cream, Image.Type.Simple);
        background.raycastTarget = false;
        SetStretch(context.InventoryTap.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        // InventoryManager.SetScrollArea uses normal top=110. Keep both category buttons
        // completely inside that reserved band so the list never overlaps them.
        SetTop(context.TopButtons, 110f, 0f);
        StyleButton(context.EquipmentButton, context.GoldButtonSprite, context.Font,
            new Vector2(-250f, -55f), new Vector2(450f, 96f), "장비");
        StyleButton(context.ConsumableButton, context.CreamButtonSprite, context.Font,
            new Vector2(250f, -55f), new Vector2(450f, 96f), "소비 아이템");

        RectTransform scroll = context.InventoryScroll.GetComponent<RectTransform>();
        Record(scroll);
        scroll.anchorMin = Vector2.zero;
        scroll.anchorMax = Vector2.one;
        scroll.offsetMin = new Vector2(42f, scroll.offsetMin.y);
        scroll.offsetMax = new Vector2(-42f, scroll.offsetMax.y);
        Dirty(scroll);
        Image scrollImage = context.InventoryScroll.GetComponent<Image>();
        if (scrollImage == null) scrollImage = Undo.AddComponent<Image>(context.InventoryScroll.gameObject);
        SetImage(scrollImage, context.CardSprite, CreamLight, Image.Type.Sliced);
        Record(context.InventoryScroll);
        context.InventoryScroll.horizontal = false;
        context.InventoryScroll.vertical = true;
        context.InventoryScroll.movementType = ScrollRect.MovementType.Elastic;
        context.InventoryScroll.scrollSensitivity = 34f;
        Dirty(context.InventoryScroll);

        SetStretch(context.Viewport, new Vector2(28f, 30f), new Vector2(-28f, -30f));
        Image viewportImage = context.Viewport.GetComponent<Image>();
        if (viewportImage != null)
            SetImage(viewportImage, null, new Color32(0xFF, 0xFF, 0xFF, 0x08), Image.Type.Simple);

        VerticalLayoutGroup layout = context.Content.GetComponent<VerticalLayoutGroup>();
        Record(layout);
        layout.padding = new RectOffset(10, 10, 12, 18);
        layout.spacing = 22f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        Dirty(layout);
        ContentSizeFitter fitter = context.Content.GetComponent<ContentSizeFitter>();
        Record(fitter);
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        Dirty(fitter);
    }

    private static void ApplyPopupStyles(SceneContext context)
    {
        SetImage(context.PopupDim, null, new Color32(0x2B, 0x1C, 0x1D, 0xA8), Image.Type.Simple);
        SetStretch(context.PopupDim.rectTransform, Vector2.zero, Vector2.zero);
        StylePopup(context.EquipPopup, context, true);
        StylePopup(context.ConsumablePopup, context, false);
        StylePopup(context.FunctionalPopup, context, false);
    }

    private static void StylePopup(PopupParts popup, SceneContext context, bool equipment)
    {
        SetImage(popup.Root, context.CardSprite, CreamLight, Image.Type.Sliced);
        SetCentered(popup.Root.rectTransform, new Vector2(860f, 1040f), Vector2.zero);
        EnsureIconFrame(popup.Root.transform, "RenewalItemIconFrame", context.GoldButtonSprite,
            new Vector2(300f, 300f), new Vector2(0f, 285f));
        SetCentered(popup.Icon.rectTransform, new Vector2(220f, 220f), new Vector2(0f, 285f));
        Record(popup.Icon);
        popup.Icon.preserveAspect = true;
        popup.Icon.raycastTarget = false;
        Dirty(popup.Icon);
        StyleText(popup.Name, context.Font, 46f, Brown, FontStyles.Bold,
            TextAlignmentOptions.Center, false, TextOverflowModes.Ellipsis);
        SetCentered(popup.Name.rectTransform, new Vector2(720f, 72f), new Vector2(0f, 80f));
        StyleText(popup.Description, context.Font, equipment ? 36f : 30f,
            equipment ? Brown : MutedBrown, equipment ? FontStyles.Bold : FontStyles.Normal,
            equipment ? TextAlignmentOptions.Center : TextAlignmentOptions.TopLeft,
            true, TextOverflowModes.Ellipsis);
        Record(popup.Description);
        popup.Description.lineSpacing = equipment ? 5f : 8f;
        popup.Description.margin = new Vector4(16f, 14f, 16f, 14f);
        SetCentered(popup.Description.rectTransform, new Vector2(720f, equipment ? 210f : 250f),
            new Vector2(0f, equipment ? -75f : -95f));
        if (equipment)
            EnsureTextShadow(popup.Description, PopupDescriptionShadow, new Vector2(1f, -1f));

        if (equipment)
        {
            StyleButton(popup.Primary, context.GoldButtonSprite, context.Font,
                new Vector2(-190f, -280f), new Vector2(340f, 112f), null);
            StyleButton(popup.Secondary, context.GoldButtonSprite, context.Font,
                new Vector2(190f, -280f), new Vector2(340f, 112f), null);
            StyleButton(popup.Close, context.CreamButtonSprite, context.Font,
                new Vector2(0f, -425f), new Vector2(720f, 100f), null);
        }
        else if (popup.Primary != null)
        {
            StyleButton(popup.Primary, context.GoldButtonSprite, context.Font,
                new Vector2(-190f, -360f), new Vector2(340f, 112f), null);
            StyleButton(popup.Close, context.CreamButtonSprite, context.Font,
                new Vector2(190f, -360f), new Vector2(340f, 112f), null);
        }
        else
        {
            StyleButton(popup.Close, context.CreamButtonSprite, context.Font,
                new Vector2(0f, -410f), new Vector2(720f, 100f), null);
        }
    }

    private static void ApplyEnhanceStyle(SceneContext context)
    {
        Image panel = context.EnhanceTap.GetComponent<Image>();
        if (panel == null) panel = Undo.AddComponent<Image>(context.EnhanceTap);
        SetImage(panel, context.CardSprite, CreamLight, Image.Type.Sliced);
        SetTop(context.EnhanceTap.GetComponent<RectTransform>(), 470f, 125f);
        EnsureIconFrame(context.EnhanceTap.transform, "RenewalTargetIconFrame",
            context.GoldButtonSprite, new Vector2(270f, 270f), new Vector2(-300f, 45f));
        SetCentered(context.EnhanceIcon.rectTransform, new Vector2(205f, 205f), new Vector2(-300f, 45f));
        Record(context.EnhanceIcon);
        context.EnhanceIcon.preserveAspect = true;
        context.EnhanceIcon.raycastTarget = false;
        Dirty(context.EnhanceIcon);
        StyleText(context.EnhanceName, context.Font, 43f, Brown, FontStyles.Bold,
            TextAlignmentOptions.Center, false, TextOverflowModes.Ellipsis);
        SetCentered(context.EnhanceName.rectTransform, new Vector2(540f, 70f), new Vector2(155f, 135f));
        StyleText(context.EnhanceLevel, context.Font, 34f, Gold, FontStyles.Bold,
            TextAlignmentOptions.Center, false, TextOverflowModes.Overflow);
        SetCentered(context.EnhanceLevel.rectTransform, new Vector2(500f, 58f), new Vector2(155f, 64f));
        SetCentered(context.ExpRoot, new Vector2(560f, 110f), new Vector2(155f, -65f));
        SetImage(context.ExpBase, null, ExpBlueDark, Image.Type.Simple);
        SetCentered(context.ExpBase.rectTransform, new Vector2(520f, 50f), new Vector2(0f, -17f));
        SetImage(context.ExpFill, null, ExpBlue, Image.Type.Simple);
        RectTransform fill = context.ExpFill.rectTransform;
        float ratio = Mathf.Clamp01(fill.localScale.x);
        Record(fill);
        fill.anchorMin = new Vector2(0.5f, 0.5f);
        fill.anchorMax = new Vector2(0.5f, 0.5f);
        fill.pivot = new Vector2(0f, 0.5f);
        fill.sizeDelta = new Vector2(520f, 50f);
        fill.anchoredPosition = new Vector2(-260f, -17f);
        fill.localScale = new Vector3(ratio, 1f, 1f);
        Dirty(fill);
        StyleText(context.ExpText, context.Font, 27f, Brown, FontStyles.Bold,
            TextAlignmentOptions.Center, false, TextOverflowModes.Overflow);
        SetCentered(context.ExpText.rectTransform, new Vector2(520f, 42f), new Vector2(0f, 36f));

        Image bar = context.EnhanceButtonBar.GetComponent<Image>();
        if (bar == null) bar = Undo.AddComponent<Image>(context.EnhanceButtonBar);
        SetImage(bar, null, Cream, Image.Type.Simple);
        bar.raycastTarget = false;
        SetBottom(context.EnhanceButtonBar.GetComponent<RectTransform>(), 250f, 180f);
        StyleButton(context.DoEnhanceButton, context.GoldButtonSprite, context.Font,
            new Vector2(-235f, 0f), new Vector2(430f, 112f), null);
        StyleButton(context.CancelEnhanceButton, context.CreamButtonSprite, context.Font,
            new Vector2(235f, 0f), new Vector2(430f, 112f), null);
    }

    private static List<string> ValidateSceneDesign(SceneContext context)
    {
        List<string> errors = new List<string>();
        if (!context.IsValid) return context.Errors.ToList();
        foreach (ButtonTarget target in SceneButtons(context)) ValidateTouch(target, errors);
        if (!context.InventoryScroll.vertical || context.InventoryScroll.horizontal)
            errors.Add("InventoryScroll은 세로 전용이어야 합니다.");
        if (context.InventoryScroll.viewport != context.Viewport ||
            context.InventoryScroll.content != context.Content)
            errors.Add("ScrollRect의 Viewport/Content 참조가 변경되었습니다.");
        RectTransform scroll = context.InventoryScroll.GetComponent<RectTransform>();
        if (Mathf.Abs(scroll.offsetMin.x - 42f) > 0.5f || Mathf.Abs(scroll.offsetMax.x + 42f) > 0.5f)
            errors.Add("InventoryScroll 좌우 여백이 42px이 아닙니다.");
        if (Mathf.Abs(scroll.offsetMax.y + 110f) > 0.5f ||
            Mathf.Abs(scroll.offsetMin.y - 180f) > 0.5f)
            errors.Add("기본 InventoryScroll 영역이 런타임 상수 top=110/bottom=180과 다릅니다.");
        RectTransform enhance = context.EnhanceTap.GetComponent<RectTransform>();
        RectTransform enhanceBar = context.EnhanceButtonBar.GetComponent<RectTransform>();
        if (Mathf.Abs(enhance.offsetMin.y + 595f) > 0.5f ||
            Mathf.Abs(enhance.offsetMax.y + 125f) > 0.5f)
            errors.Add("EnhanceTap이 강화 스크롤 top=610 위의 예약 영역 안에 배치되지 않았습니다.");
        if (Mathf.Abs(enhanceBar.offsetMin.y - 180f) > 0.5f ||
            Mathf.Abs(enhanceBar.offsetMax.y - 430f) > 0.5f)
            errors.Add("EnhanceButtonBar가 강화 스크롤 bottom=430 경계와 맞지 않습니다.");
        if (context.Content.GetComponent<VerticalLayoutGroup>().spacing < 18f)
            errors.Add("ItemSlot 사이 간격이 충분하지 않습니다.");
        if (context.PopupDim.color.a < 0.5f || context.PopupDim.color.a > 0.8f)
            errors.Add("PopupDim 투명도가 허용 범위를 벗어났습니다.");
        foreach (PopupParts popup in new[]
                 { context.EquipPopup, context.ConsumablePopup, context.FunctionalPopup })
        {
            if (popup.Root.sprite != context.CardSprite || popup.Root.type != Image.Type.Sliced)
                errors.Add(popup.Root.name + "에 RPG 카드 Sprite가 적용되지 않았습니다.");
            if (popup.Icon.transform.parent.Find("RenewalItemIconFrame") == null)
                errors.Add(popup.Root.name + "의 아이콘 프레임이 없습니다.");
            if (!popup.Icon.preserveAspect) errors.Add(popup.Root.name + " 아이콘 PreserveAspect가 꺼져 있습니다.");
            if (popup.Name.font != context.Font || popup.Description.font != context.Font)
                errors.Add(popup.Root.name + " 텍스트에 NotoSansKR이 적용되지 않았습니다.");
            RectTransform popupRect = popup.Root.rectTransform;
            ValidateRectContained(popupRect, popup.Icon.rectTransform,
                popup.Root.name + "/ItemIcon", errors);
            ValidateRectContained(popupRect, popup.Name.rectTransform,
                popup.Root.name + "/ItemName", errors);
            ValidateRectContained(popupRect, popup.Description.rectTransform,
                popup.Root.name + "/ItemDescription", errors);
            ValidateRectContained(popupRect, popup.Close.GetComponent<RectTransform>(),
                popup.Root.name + "/CloseButton", errors);
            if (Overlaps(popup.Name.rectTransform, popup.Description.rectTransform, popupRect))
                errors.Add(popup.Root.name + "의 이름과 설명 영역이 겹칩니다.");
            foreach (Button action in new[] { popup.Primary, popup.Secondary, popup.Close })
            {
                if (action == null) continue;
                RectTransform actionRect = action.GetComponent<RectTransform>();
                ValidateRectContained(popupRect, actionRect,
                    popup.Root.name + "/" + action.name, errors);
                if (Overlaps(popup.Description.rectTransform, actionRect, popupRect))
                    errors.Add(popup.Root.name + "의 설명과 " + action.name + " 영역이 겹칩니다.");
            }
        }
        if (context.EquipPopup.Description.fontSize < 36f ||
            context.EquipPopup.Description.color != Brown ||
            context.EquipPopup.Description.fontStyle != FontStyles.Bold ||
            context.EquipPopup.Description.alignment != TextAlignmentOptions.Center)
            errors.Add("장비 상세 팝업 설명의 가독성 스타일이 적용되지 않았습니다.");
        Shadow descriptionShadow = context.EquipPopup.Description.GetComponent<Shadow>();
        if (descriptionShadow == null || !descriptionShadow.enabled)
            errors.Add("장비 상세 팝업 설명의 Shadow가 적용되지 않았습니다.");
        if (context.EquipPopup.Description.rectTransform.rect.height < 210f)
            errors.Add("장비 상세 팝업 설명 영역 높이가 210px 미만입니다.");
        if (VerticalGap(context.EquipPopup.Root.rectTransform,
                context.EquipPopup.Icon.rectTransform, context.EquipPopup.Name.rectTransform) < 40f)
            errors.Add("장비 상세 팝업의 아이콘과 이름 사이 간격이 40px 미만입니다.");
        float nameDescriptionGap = VerticalGap(context.EquipPopup.Root.rectTransform,
            context.EquipPopup.Name.rectTransform, context.EquipPopup.Description.rectTransform);
        if (nameDescriptionGap < 12f || nameDescriptionGap > 32f)
            errors.Add("장비 상세 팝업의 이름과 설명 사이 간격이 12~32px 범위를 벗어났습니다.");
        Image enhancePanel = context.EnhanceTap.GetComponent<Image>();
        if (enhancePanel == null || enhancePanel.sprite != context.CardSprite)
            errors.Add("EnhanceTap에 RPG 카드 배경이 적용되지 않았습니다.");
        if (context.ExpFill.color != ExpBlue || context.ExpBase.color != ExpBlueDark)
            errors.Add("강화 EXP 바 스타일이 적용되지 않았습니다.");
        if (context.ExpFill.rectTransform.localScale.x < 0f ||
            context.ExpFill.rectTransform.localScale.x > 1f)
            errors.Add("강화 EXP Fill 비율이 0~1 범위를 벗어났습니다.");
        if (context.EnhanceName.font != context.Font || context.ExpText.font != context.Font)
            errors.Add("강화 UI에 NotoSansKR이 적용되지 않았습니다.");
        return errors;
    }

    private static IEnumerable<ButtonTarget> SceneButtons(SceneContext context)
    {
        yield return new ButtonTarget(context.EquipmentButton, "EquipmentButton");
        yield return new ButtonTarget(context.ConsumableButton, "ConsumableButton");
        yield return new ButtonTarget(context.EquipPopup.Primary, "EquipButton");
        yield return new ButtonTarget(context.EquipPopup.Secondary, "EnhanceButton");
        yield return new ButtonTarget(context.EquipPopup.Close, "Equip CloseButton");
        yield return new ButtonTarget(context.ConsumablePopup.Close, "Consum CloseButton");
        yield return new ButtonTarget(context.FunctionalPopup.Primary, "UseButton");
        yield return new ButtonTarget(context.FunctionalPopup.Close, "Functional CloseButton");
        yield return new ButtonTarget(context.DoEnhanceButton, "Do EnhanceButton");
        yield return new ButtonTarget(context.CancelEnhanceButton, "Enhance CancelButton");
    }

    private static void WithItemSlotContents(bool save, Action<ItemSlotContext> action)
    {
        GameObject root = null;
        string guidBefore = AssetDatabase.AssetPathToGUID(ItemSlotPrefabPath);
        try
        {
            root = PrefabUtility.LoadPrefabContents(ItemSlotPrefabPath);
            ItemSlotContext context = BuildItemSlotContext(root, guidBefore, true);
            action(context);
            if (!save || !context.IsValid) return;

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, ItemSlotPrefabPath);
            if (savedPrefab == null)
                throw new InvalidOperationException("ItemSlot.prefab 저장에 실패했습니다.");
            AssetDatabase.SaveAssets();
            string guidAfter = AssetDatabase.AssetPathToGUID(ItemSlotPrefabPath);
            if (guidAfter != guidBefore)
                throw new InvalidOperationException("ItemSlot.prefab GUID가 변경되었습니다.");
            Debug.Log("[HabitPVP 15-D-5C] ItemSlot Prefab 적용 및 저장 성공. GUID=" + guidAfter +
                      ". ItemSlotUI/버튼 참조와 런타임 상태 색상 값은 유지했습니다.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (save) Debug.LogError("[HabitPVP 15-D-5C] ItemSlot Prefab Apply에 실패했습니다.");
        }
        finally
        {
            if (root != null) PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static ItemSlotContext BuildItemSlotContext(GameObject root, string guid, bool log)
    {
        ItemSlotContext context = new ItemSlotContext { Root = root, Guid = guid };
        if (root == null || root.name != "ItemSlot")
        {
            context.Errors.Add("ItemSlot Prefab 루트가 없거나 이름이 다릅니다.");
            return Finish(context, log);
        }
        context.RootRect = root.GetComponent<RectTransform>();
        context.Background = root.GetComponent<Image>();
        context.Button = root.GetComponent<Button>();
        context.Slot = root.GetComponent<ItemSlotUI>();
        context.Icon = Find<Image>(root.transform, "IconImage", context.Errors);
        context.Name = Find<TMP_Text>(root.transform, "ItemNameText", context.Errors);
        context.Description = Find<TMP_Text>(root.transform, "ItemDescribeText", context.Errors);
        context.QuantityBackground = Find<Image>(root.transform, "QuantityBackground", context.Errors);
        context.Quantity = Find<TMP_Text>(root.transform, "QuantityBackground/QuantityText", context.Errors);
        context.EquipType = Find<TMP_Text>(root.transform, "EquipTypeText", context.Errors);
        context.EquipTypeBadge = root.transform.Find("RenewalEquipTypeBadge")?.GetComponent<Image>();
        if (context.RootRect == null || context.Background == null || context.Button == null || context.Slot == null)
            context.Errors.Add("ItemSlot 루트의 RectTransform/Image/Button/ItemSlotUI가 누락되었습니다.");
        SerializedObject slot = context.Slot != null ? new SerializedObject(context.Slot) : null;
        foreach (string name in new[]
                 {
                     "button", "backgroundImage", "iconImage", "itemNameText", "itemDescribeText",
                     "quantityText", "quantityBackground", "equipTypeText"
                 })
        {
            SerializedProperty property = slot?.FindProperty(name);
            if (property == null || property.objectReferenceValue == null)
                context.Errors.Add("ItemSlotUI 참조가 비어 있습니다: " + name);
        }
        if (slot != null)
        {
            ValidateReference(slot, "button", context.Button, context.Errors);
            ValidateReference(slot, "backgroundImage", context.Background, context.Errors);
            ValidateReference(slot, "iconImage", context.Icon, context.Errors);
            ValidateReference(slot, "itemNameText", context.Name, context.Errors);
            ValidateReference(slot, "itemDescribeText", context.Description, context.Errors);
            ValidateReference(slot, "quantityText", context.Quantity, context.Errors);
            ValidateReference(slot, "quantityBackground", context.QuantityBackground?.gameObject,
                context.Errors);
            ValidateReference(slot, "equipTypeText", context.EquipType, context.Errors);
        }
        context.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        context.CardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardSpritePath);
        context.CreamButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CreamButtonSpritePath);
        context.GoldButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GoldButtonSpritePath);
        if (string.IsNullOrEmpty(guid)) context.Errors.Add("ItemSlot.prefab GUID를 확인할 수 없습니다.");
        if (context.Font == null || context.CardSprite == null || context.CreamButtonSprite == null ||
            context.GoldButtonSprite == null)
            context.Errors.Add("ItemSlot 디자인에 필요한 Font/Sprite를 찾지 못했습니다.");
        return Finish(context, log);
    }

    private static void ApplyItemSlotStyle(ItemSlotContext context)
    {
        context.RootRect.sizeDelta = new Vector2(850f, 150f);
        SetImage(context.Background, context.CardSprite, context.Background.color, Image.Type.Sliced);
        SetCentered(context.Icon.rectTransform, new Vector2(116f, 116f), new Vector2(-350f, 0f));
        context.Icon.preserveAspect = true;
        context.Icon.raycastTarget = false;
        Dirty(context.Icon);
        StyleText(context.Name, context.Font, 31f, Brown, FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft, false, TextOverflowModes.Ellipsis);
        SetCentered(context.Name.rectTransform, new Vector2(515f, 50f), new Vector2(-20f, 39f));
        StyleText(context.Description, context.Font, 24f, DescriptionBrown, FontStyles.Normal,
            TextAlignmentOptions.TopLeft, true, TextOverflowModes.Ellipsis);
        context.Description.lineSpacing = 1f;
        context.Description.margin = new Vector4(0f, 2f, 4f, 2f);
        SetCentered(context.Description.rectTransform, new Vector2(515f, 72f), new Vector2(-20f, -31f));
        SetImage(context.QuantityBackground, context.GoldButtonSprite, Color.white, Image.Type.Sliced);
        SetCentered(context.QuantityBackground.rectTransform, new Vector2(112f, 52f),
            new Vector2(350f, -43f));
        StyleText(context.Quantity, context.Font, 25f, Brown, FontStyles.Bold,
            TextAlignmentOptions.Center, false, TextOverflowModes.Ellipsis);
        SetStretch(context.Quantity.rectTransform, new Vector2(10f, 5f), new Vector2(-10f, -5f));
        StyleText(context.EquipType, context.Font, 27f, Brown, FontStyles.Bold,
            TextAlignmentOptions.Center, false, TextOverflowModes.Ellipsis);
        SetRightCenter(context.EquipType.rectTransform, new Vector2(140f, 50f),
            new Vector2(-19f, 0f));
        context.EquipTypeBadge = EnsureDecorativeImage(context.Root.transform,
            "RenewalEquipTypeBadge", context.CreamButtonSprite, BadgeGold);
        SetRightCenter(context.EquipTypeBadge.rectTransform, new Vector2(154f, 58f),
            new Vector2(-12f, 0f));
        context.EquipTypeBadge.transform.SetAsLastSibling();
        context.EquipType.transform.SetAsLastSibling();
        Outline oldOutline = context.EquipType.GetComponent<Outline>();
        if (oldOutline != null) Undo.DestroyObjectImmediate(oldOutline);
        Dirty(context.RootRect);
    }

    private static List<string> ValidateItemSlotDesign(ItemSlotContext context)
    {
        List<string> errors = new List<string>();
        if (!context.IsValid) return context.Errors.ToList();
        if (Mathf.Abs(context.RootRect.rect.width - 850f) > 0.5f ||
            Mathf.Abs(context.RootRect.rect.height - 150f) > 0.5f)
            errors.Add("ItemSlot 카드 크기가 850x150이 아닙니다.");
        if (context.Background.sprite != context.CardSprite || context.Background.type != Image.Type.Sliced)
            errors.Add("ItemSlot 배경에 RPG 카드 Sprite가 적용되지 않았습니다.");
        if (!context.Icon.preserveAspect || context.Icon.rectTransform.rect.width !=
            context.Icon.rectTransform.rect.height)
            errors.Add("ItemSlot 아이콘이 정사각형 PreserveAspect 영역이 아닙니다.");
        foreach (TMP_Text text in new[] { context.Name, context.Description, context.Quantity, context.EquipType })
            if (text.font != context.Font) errors.Add(text.name + "에 NotoSansKR이 적용되지 않았습니다.");
        if (context.Description.fontSize < 24f || context.Description.color != DescriptionBrown)
            errors.Add("ItemDescribeText의 가독성 스타일이 적용되지 않았습니다.");
        if (context.EquipType.fontSize < 27f || context.EquipType.color != Brown)
            errors.Add("EquipTypeText의 가독성 스타일이 적용되지 않았습니다.");
        if (context.EquipTypeBadge == null ||
            context.EquipTypeBadge.sprite != context.CreamButtonSprite ||
            context.EquipTypeBadge.type != Image.Type.Sliced ||
            context.EquipTypeBadge.color != BadgeGold)
            errors.Add("EquipTypeText의 캡슐형 배지가 적용되지 않았습니다.");
        else if (context.EquipTypeBadge.transform.GetSiblingIndex() >=
                 context.EquipType.transform.GetSiblingIndex())
            errors.Add("EquipTypeBadge가 EquipTypeText 뒤에서 렌더링되지 않습니다.");
        if (!context.Description.enableWordWrapping ||
            context.Description.overflowMode != TextOverflowModes.Ellipsis)
            errors.Add("ItemDescribeText의 Wrapping 또는 Ellipsis 설정이 적용되지 않았습니다.");
        ValidateTwoLineDescription(context.Description, errors);
        Rect card = context.RootRect.rect;
        ValidateContained(context, context.Icon.rectTransform, "IconImage", card, errors);
        ValidateContained(context, context.Name.rectTransform, "ItemNameText", card, errors);
        ValidateContained(context, context.Description.rectTransform, "ItemDescribeText", card, errors);
        ValidateContained(context, context.QuantityBackground.rectTransform, "QuantityBackground", card, errors);
        ValidateContained(context, context.EquipType.rectTransform, "EquipTypeText", card, errors);
        if (context.EquipTypeBadge != null)
        {
            ValidateContained(context, context.EquipTypeBadge.rectTransform,
                "RenewalEquipTypeBadge", card, errors);
            Rect badgeRect = LocalRect(context.RootRect, context.EquipTypeBadge.rectTransform);
            Rect typeRect = LocalRect(context.RootRect, context.EquipType.rectTransform);
            float rightInset = card.xMax - badgeRect.xMax;
            if (Mathf.Abs(badgeRect.center.y - card.center.y) > 0.5f)
                errors.Add("EquipTypeBadge가 카드 높이의 중앙에 정렬되지 않았습니다.");
            if (rightInset < 10f || rightInset > 20f)
                errors.Add("EquipTypeBadge의 오른쪽 내부 여백이 10~20px 범위를 벗어났습니다.");
            if (Vector2.Distance(badgeRect.center, typeRect.center) > 0.5f)
                errors.Add("EquipTypeBadge와 EquipTypeText의 중심이 일치하지 않습니다.");
            if (context.EquipTypeBadge.rectTransform.anchorMin != new Vector2(1f, 0.5f) ||
                context.EquipTypeBadge.rectTransform.anchorMax != new Vector2(1f, 0.5f) ||
                context.EquipTypeBadge.rectTransform.pivot != new Vector2(1f, 0.5f) ||
                context.EquipType.rectTransform.anchorMin != new Vector2(1f, 0.5f) ||
                context.EquipType.rectTransform.anchorMax != new Vector2(1f, 0.5f) ||
                context.EquipType.rectTransform.pivot != new Vector2(1f, 0.5f))
                errors.Add("EquipTypeBadge 또는 EquipTypeText가 우측 중앙 Anchor/Pivot을 사용하지 않습니다.");
            if (Overlaps(context.Name.rectTransform, context.EquipTypeBadge.rectTransform,
                    context.RootRect))
                errors.Add("ItemNameText와 EquipTypeBadge가 겹칩니다.");
            if (Overlaps(context.Description.rectTransform, context.EquipTypeBadge.rectTransform,
                    context.RootRect))
                errors.Add("ItemDescribeText와 EquipTypeBadge가 겹칩니다.");
        }
        if (Overlaps(context.Name.rectTransform, context.Description.rectTransform, context.RootRect))
            errors.Add("ItemNameText와 ItemDescribeText가 겹칩니다.");
        if (Overlaps(context.Description.rectTransform, context.QuantityBackground.rectTransform,
                context.RootRect))
            errors.Add("ItemDescribeText와 QuantityBackground가 겹칩니다.");
        if (Overlaps(context.Description.rectTransform, context.EquipType.rectTransform,
                context.RootRect))
            errors.Add("ItemDescribeText와 EquipTypeText가 겹칩니다.");
        return errors;
    }

    private static void ValidateTwoLineDescription(TMP_Text description,
        ICollection<string> errors)
    {
        RectTransform rect = description.rectTransform;
        float availableWidth = rect.rect.width - description.margin.x - description.margin.z;
        float availableHeight = rect.rect.height - description.margin.y - description.margin.w;
        string originalText = description.text;
        Vector2 twoLineSize;
        Vector2 threeLineSize;

        try
        {
            // TMP 3.0.7 maxVisibleLines is not serialized. Verify the persistent layout instead:
            // the container must fit two natural lines, reject a third, and use Ellipsis overflow.
            twoLineSize = description.GetPreferredValues("가\n가", availableWidth, 10000f);
            threeLineSize = description.GetPreferredValues("가\n가\n가", availableWidth, 10000f);
        }
        finally
        {
            description.text = originalText;
        }

        const float tolerance = 0.5f;
        if (twoLineSize.y > availableHeight + tolerance ||
            threeLineSize.y <= availableHeight + tolerance)
        {
            errors.Add(
                "ItemDescribeText 높이가 정확한 2줄 영역이 아닙니다. " +
                "available=" + availableHeight.ToString("F1") + "px, " +
                "twoLines=" + twoLineSize.y.ToString("F1") + "px, " +
                "threeLines=" + threeLineSize.y.ToString("F1") + "px");
        }
    }

    private static void StyleButton(Button button, Sprite sprite, TMP_FontAsset font,
        Vector2 position, Vector2 size, string fixedLabel)
    {
        SetImage(button.image, sprite, Color.white, Image.Type.Sliced);
        SetCentered(button.GetComponent<RectTransform>(), size, position);
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            if (fixedLabel != null)
            {
                Record(label);
                label.text = fixedLabel;
            }
            StyleText(label, font, 30f, Brown, FontStyles.Bold, TextAlignmentOptions.Center,
                false, TextOverflowModes.Ellipsis);
            SetStretch(label.rectTransform, new Vector2(18f, 10f), new Vector2(-18f, -10f));
        }
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(0xFF, 0xF4, 0xD8, 0xFF);
        colors.pressedColor = new Color32(0xE7, 0xD1, 0xA6, 0xFF);
        colors.selectedColor = new Color32(0xF6, 0xD7, 0x91, 0xFF);
        colors.disabledColor = new Color32(0xA9, 0x9B, 0x88, 0xB8);
        Record(button);
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;
        Dirty(button);
    }

    private static void EnsureIconFrame(Transform parent, string name, Sprite sprite,
        Vector2 size, Vector2 position)
    {
        Transform old = parent.Find(name);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(item, "Create inventory icon frame");
        item.transform.SetParent(parent, false);
        item.transform.SetAsFirstSibling();
        Image image = item.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;
        SetCentered(image.rectTransform, size, position);
        Dirty(item);
    }

    private static Image EnsureDecorativeImage(Transform parent, string name, Sprite sprite,
        Color color)
    {
        Transform existing = parent.Find(name);
        GameObject target;
        if (existing == null)
        {
            target = new GameObject(name, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(target, "Create inventory type badge");
            target.transform.SetParent(parent, false);
        }
        else
        {
            target = existing.gameObject;
        }

        Image image = target.GetComponent<Image>();
        if (image == null) image = Undo.AddComponent<Image>(target);
        SetImage(image, sprite, color, Image.Type.Sliced);
        image.raycastTarget = false;
        Dirty(image);
        return image;
    }

    private static void EnsureTextShadow(TMP_Text text, Color color, Vector2 distance)
    {
        Shadow shadow = text.GetComponent<Shadow>();
        if (shadow == null) shadow = Undo.AddComponent<Shadow>(text.gameObject);
        Record(shadow);
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
        shadow.enabled = true;
        Dirty(shadow);
    }

    private static void StyleText(TMP_Text text, TMP_FontAsset font, float size, Color color,
        FontStyles style, TextAlignmentOptions alignment, bool wrap, TextOverflowModes overflow)
    {
        Record(text);
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = alignment;
        text.enableWordWrapping = wrap;
        text.overflowMode = overflow;
        Dirty(text);
    }

    private static void SetImage(Image image, Sprite sprite, Color color, Image.Type type)
    {
        Record(image);
        image.sprite = sprite;
        image.color = color;
        image.type = type;
        Dirty(image);
    }

    private static void SetCentered(RectTransform rect, Vector2 size, Vector2 position)
    {
        Record(rect);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Dirty(rect);
    }

    private static void SetRightCenter(RectTransform rect, Vector2 size, Vector2 position)
    {
        Record(rect);
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Dirty(rect);
    }

    private static void SetStretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        Record(rect);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = min;
        rect.offsetMax = max;
        Dirty(rect);
    }

    private static void SetTop(RectTransform rect, float height, float top)
    {
        Record(rect);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(0f, -top - height);
        rect.offsetMax = new Vector2(0f, -top);
        Dirty(rect);
    }

    private static void SetBottom(RectTransform rect, float height, float bottom)
    {
        Record(rect);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(0f, bottom);
        rect.offsetMax = new Vector2(0f, bottom + height);
        Dirty(rect);
    }

    private static void ValidateTouch(ButtonTarget target, ICollection<string> errors)
    {
        RectTransform rect = target.Button?.GetComponent<RectTransform>();
        if (rect == null || rect.rect.width < 88f || rect.rect.height < 88f)
            errors.Add(target.Name + " 터치 영역이 88x88 미만입니다.");
    }

    private static void ValidateContained(ItemSlotContext context, RectTransform target, string name,
        Rect bounds, ICollection<string> errors)
    {
        Rect actual = LocalRect(context.RootRect, target);
        const float tolerance = 0.5f;
        if (actual.xMin < bounds.xMin - tolerance || actual.xMax > bounds.xMax + tolerance ||
            actual.yMin < bounds.yMin - tolerance || actual.yMax > bounds.yMax + tolerance)
            errors.Add(name + "이 ItemSlot 카드 경계를 벗어납니다.");
    }

    private static void ValidateRectContained(RectTransform root, RectTransform target, string name,
        ICollection<string> errors)
    {
        Rect bounds = root.rect;
        Rect actual = LocalRect(root, target);
        const float tolerance = 0.5f;
        if (actual.xMin < bounds.xMin - tolerance || actual.xMax > bounds.xMax + tolerance ||
            actual.yMin < bounds.yMin - tolerance || actual.yMax > bounds.yMax + tolerance)
            errors.Add(name + "이 부모 패널 경계를 벗어납니다.");
    }

    private static bool Overlaps(RectTransform a, RectTransform b, RectTransform root) =>
        LocalRect(root, a).Overlaps(LocalRect(root, b));

    private static float VerticalGap(RectTransform root, RectTransform upper, RectTransform lower)
    {
        Rect upperRect = LocalRect(root, upper);
        Rect lowerRect = LocalRect(root, lower);
        return upperRect.yMin - lowerRect.yMax;
    }

    private static Rect LocalRect(RectTransform root, RectTransform target)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector3 first = root.InverseTransformPoint(corners[0]);
        float minX = first.x, maxX = first.x, minY = first.y, maxY = first.y;
        for (int index = 1; index < corners.Length; index++)
        {
            Vector3 point = root.InverseTransformPoint(corners[index]);
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minY = Mathf.Min(minY, point.y);
            maxY = Mathf.Max(maxY, point.y);
        }
        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private static T Find<T>(Transform root, string path, ICollection<string> errors)
        where T : Component
    {
        Transform target = root.Find(path);
        T component = target != null ? target.GetComponent<T>() : null;
        if (component == null) errors.Add("필수 UI를 찾을 수 없습니다: " + path + " (" + typeof(T).Name + ")");
        return component;
    }

    private static T Reference<T>(SerializedObject serialized, string name,
        ICollection<string> errors) where T : UnityEngine.Object
    {
        SerializedProperty property = serialized.FindProperty(name);
        T value = property != null ? property.objectReferenceValue as T : null;
        if (value == null) errors.Add("SerializeField 참조가 비어 있습니다: " + name);
        return value;
    }

    private static void ValidateReference(SerializedObject serialized, string name,
        UnityEngine.Object expected, ICollection<string> errors)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null || property.objectReferenceValue != expected)
            errors.Add(serialized.targetObject.GetType().Name + "." + name + " 참조가 Hierarchy와 다릅니다.");
    }

    private static SceneContext Finish(SceneContext context, bool log)
    {
        if (log) foreach (string error in context.Errors)
            Debug.LogError("[HabitPVP 15-D-5C] " + error, context.InventoryTap ?? context.Canvas);
        return context;
    }

    private static ItemSlotContext Finish(ItemSlotContext context, bool log)
    {
        if (log) foreach (string error in context.Errors)
            Debug.LogError("[HabitPVP 15-D-5C ItemSlot] " + error, context.Root);
        return context;
    }

    private static void LogValidation(string label, List<string> errors, UnityEngine.Object target)
    {
        if (errors.Count == 0)
        {
            Debug.Log("[HabitPVP 15-D-5C] " + label + " 검증 성공.", target);
            return;
        }
        foreach (string error in errors) Debug.LogError("[HabitPVP 15-D-5C] " + error, target);
    }

    private static void Record(UnityEngine.Object target)
    {
        if (target != null) Undo.RecordObject(target, "15-D-5C Inventory UI");
    }

    private static void Dirty(UnityEngine.Object target)
    {
        if (target == null) return;
        EditorUtility.SetDirty(target);
        if (PrefabUtility.IsPartOfPrefabInstance(target))
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private sealed class SceneContext
    {
        public Scene Scene;
        public GameObject Canvas;
        public GameObject InventoryTap;
        public InventoryManager Manager;
        public MainTabManager TabManager;
        public RectTransform TopButtons;
        public Button EquipmentButton;
        public Button ConsumableButton;
        public ScrollRect InventoryScroll;
        public RectTransform Viewport;
        public RectTransform Content;
        public Image PopupDim;
        public PopupParts EquipPopup;
        public PopupParts ConsumablePopup;
        public PopupParts FunctionalPopup;
        public GameObject EnhanceTap;
        public TMP_Text EnhanceName;
        public Image EnhanceIcon;
        public TMP_Text EnhanceLevel;
        public RectTransform ExpRoot;
        public TMP_Text ExpText;
        public Image ExpBase;
        public Image ExpFill;
        public GameObject EnhanceButtonBar;
        public Button DoEnhanceButton;
        public Button CancelEnhanceButton;
        public TMP_FontAsset Font;
        public Sprite CardSprite;
        public Sprite CreamButtonSprite;
        public Sprite GoldButtonSprite;
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }

    private sealed class PopupParts
    {
        public Image Root;
        public Image Icon;
        public TMP_Text Name;
        public TMP_Text Description;
        public Button Primary;
        public Button Secondary;
        public Button Close;
    }

    private sealed class ItemSlotContext
    {
        public GameObject Root;
        public string Guid;
        public RectTransform RootRect;
        public Image Background;
        public Button Button;
        public ItemSlotUI Slot;
        public Image Icon;
        public TMP_Text Name;
        public TMP_Text Description;
        public Image QuantityBackground;
        public TMP_Text Quantity;
        public TMP_Text EquipType;
        public Image EquipTypeBadge;
        public TMP_FontAsset Font;
        public Sprite CardSprite;
        public Sprite CreamButtonSprite;
        public Sprite GoldButtonSprite;
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }

    private readonly struct ButtonTarget
    {
        public readonly Button Button;
        public readonly string Name;
        public ButtonTarget(Button button, string name) { Button = button; Name = name; }
    }

    private sealed class SceneProtectionSnapshot
    {
        private readonly string managerJson;
        private readonly string tabManagerJson;
        private readonly bool[] activeStates;
        private readonly int[] persistentCalls;

        private SceneProtectionSnapshot(string managerJson, string tabManagerJson, bool[] activeStates,
            int[] persistentCalls)
        {
            this.managerJson = managerJson;
            this.tabManagerJson = tabManagerJson;
            this.activeStates = activeStates;
            this.persistentCalls = persistentCalls;
        }

        public static SceneProtectionSnapshot Capture(SceneContext context) => new SceneProtectionSnapshot(
            EditorJsonUtility.ToJson(context.Manager),
            EditorJsonUtility.ToJson(context.TabManager),
            ProtectedObjects(context).Select(item => item.activeSelf).ToArray(),
            SceneButtons(context).Select(item => item.Button.onClick.GetPersistentEventCount()).ToArray());

        public void AssertUnchanged(SceneContext context)
        {
            if (managerJson != EditorJsonUtility.ToJson(context.Manager))
                throw new InvalidOperationException("InventoryManager SerializeField가 Apply 중 변경되었습니다.");
            if (tabManagerJson != EditorJsonUtility.ToJson(context.TabManager))
                throw new InvalidOperationException("MainTabManager SerializeField가 Apply 중 변경되었습니다.");
            if (!activeStates.SequenceEqual(ProtectedObjects(context).Select(item => item.activeSelf)))
                throw new InvalidOperationException("기능 UI의 activeSelf가 Apply 중 변경되었습니다.");
            if (!persistentCalls.SequenceEqual(SceneButtons(context)
                    .Select(item => item.Button.onClick.GetPersistentEventCount())))
                throw new InvalidOperationException("기존 Button UnityEvent가 Apply 중 변경되었습니다.");
        }

        private static IEnumerable<GameObject> ProtectedObjects(SceneContext context)
        {
            yield return context.InventoryTap;
            yield return context.PopupDim.gameObject;
            yield return context.EquipPopup.Root.gameObject;
            yield return context.ConsumablePopup.Root.gameObject;
            yield return context.FunctionalPopup.Root.gameObject;
            yield return context.EnhanceTap;
            yield return context.EnhanceButtonBar;
        }
    }

    private sealed class ItemSlotProtectionSnapshot
    {
        private readonly string slotJson;
        private readonly int persistentCalls;
        private readonly string guid;

        private ItemSlotProtectionSnapshot(string slotJson, int persistentCalls, string guid)
        {
            this.slotJson = slotJson;
            this.persistentCalls = persistentCalls;
            this.guid = guid;
        }

        public static ItemSlotProtectionSnapshot Capture(ItemSlotContext context) =>
            new ItemSlotProtectionSnapshot(EditorJsonUtility.ToJson(context.Slot),
                context.Button.onClick.GetPersistentEventCount(), context.Guid);

        public void AssertUnchanged(ItemSlotContext context)
        {
            if (slotJson != EditorJsonUtility.ToJson(context.Slot))
                throw new InvalidOperationException("ItemSlotUI SerializeField 또는 상태 색상 값이 변경되었습니다.");
            if (persistentCalls != context.Button.onClick.GetPersistentEventCount())
                throw new InvalidOperationException("ItemSlot Button UnityEvent가 변경되었습니다.");
            if (guid != AssetDatabase.AssetPathToGUID(ItemSlotPrefabPath))
                throw new InvalidOperationException("ItemSlot.prefab GUID가 변경되었습니다.");
        }
    }
}
