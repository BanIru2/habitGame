using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ShopMainIntegrationEditor
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string CanvasPrefabPath = "Assets/Resources/UIPrefabs/Canvas.prefab";
    private const string SlotPrefabPath = "Assets/Resources/Prefabs/ShopItemSlot.prefab";
    private const string FontPath = "Assets/Login/Fonts/NotoSansKR Login SDF.asset";
    private const string CardSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/panel_rpg_card.png";
    private const string CreamButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_cream.png";
    private const string GoldButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_rpg.png";
    private const string MainTabManagerPath = "Assets/Scripts/Core/MainTabManager.cs";
    private const string MenuRoot = "Tools/HabitPVP/UI Renewal/MainScene/";

    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 CreamLight = new Color32(0xFF, 0xF9, 0xEA, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 ReadableBrown = new Color32(0x6E, 0x4B, 0x45, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);

    [MenuItem(MenuRoot + "Validate Shop Main", false, 2250)]
    private static void ValidateMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid) return;

        List<string> layoutErrors = ValidateAppliedLayout(context, false);
        if (layoutErrors.Count == 0)
        {
            Debug.Log("[HabitPVP MainScene] Shop Main 구조 검증 성공. Scene/Prefab은 변경하지 않았습니다.\n" +
                      BuildDiagnostics(context), context.ShopTap);
        }
        else
        {
            foreach (string error in layoutErrors)
                Debug.LogWarning("[HabitPVP MainScene] Shop Main 스타일 미적용/불일치: " + error,
                    context.ShopTap);
        }
    }

    [MenuItem(MenuRoot + "Dry Run Shop Main", false, 2251)]
    private static void DryRunMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid)
        {
            Debug.LogError("[HabitPVP MainScene] Shop Main Dry Run 중단: 구조 오류를 해결해 주세요.");
            return;
        }

        Debug.Log(
            "[HabitPVP MainScene] Shop Main Dry Run (변경 없음)\n" +
            "- ShopTap: 크림 배경, RPG 카드형 헤더, 골드/크림 카테고리 버튼\n" +
            "- ShopScroll: 카테고리 아래 450px부터 TapBar 위 210px까지의 세로 스크롤\n" +
            "- GachaPanel: 같은 본문 안전 영역 안에서 기존 이미지/버튼을 재배치하고 확률 버튼을 하단 중앙으로 복구\n" +
            "- ShopItemSlot.prefab: RPG 상품 카드, NotoSansKR, 230px 터치 영역\n" +
            "- 상품 상세 Popup: ItemIcon을 180px Simple/preserveAspect로 보정\n" +
            "- 상품 상세/구매 실패 Popup: 기존 참조/activeSelf를 유지한 스타일 갱신\n" +
            "- GachaRoot/가챠 결과/확률 Popup: 15-D-6B 범위로 남겨 두며 변경하지 않음\n" +
            "- 보호: ShopUIManager/MainTabManager SerializeField, Button UnityEvent, activeSelf, " +
            "sibling 순서, ScrollRect 참조, TapBar 4버튼 및 런타임 AddListener\n" +
            BuildDiagnostics(context), context.ShopTap);
    }

    [MenuItem(MenuRoot + "Apply Shop Main", false, 2252)]
    private static void ApplyMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid) return;

        ProtectionSnapshot snapshot = ProtectionSnapshot.Capture(context);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Shop Main UI Renewal");
        try
        {
            ApplySceneStyle(context);
            snapshot.AssertUnchanged(context);

            List<string> sceneErrors = ValidateAppliedLayout(context, false);
            if (sceneErrors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", sceneErrors));

            snapshot.AssertUnchanged(context);
            ApplySlotPrefab(context);

            EditorSceneManager.MarkSceneDirty(context.Scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = context.ShopTap;
            Debug.Log(
                "[HabitPVP MainScene] Shop Main 적용 성공. MainScene은 dirty 상태이며 자동 저장하지 않았습니다. " +
                "ShopItemSlot.prefab만 별도 Prefab asset으로 저장했습니다. Canvas.prefab과 가챠 UI는 변경하지 않았습니다.",
                context.ShopTap);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            EditorSceneManager.MarkSceneDirty(context.Scene);
            Debug.LogException(exception);
            Debug.LogError("[HabitPVP MainScene] Shop Main Apply 실패. 이번 실행의 Scene/Prefab 변경을 Undo로 복원했습니다.");
        }
    }

    [MenuItem(MenuRoot + "Apply Shop Main", true)]
    private static bool CanApply()
    {
        Scene scene = SceneManager.GetActiveScene();
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
               scene.IsValid() && scene.isLoaded && scene.path == ScenePath;
    }

    private static Context Inspect(bool log)
    {
        Context context = new Context { Scene = SceneManager.GetActiveScene() };
        if (!context.Scene.IsValid() || !context.Scene.isLoaded)
        {
            context.Errors.Add("활성 Scene이 유효하지 않습니다.");
            return Finish(context, log);
        }
        if (context.Scene.path != ScenePath)
            context.Errors.Add($"MainScene에서만 실행할 수 있습니다. 현재: '{context.Scene.path}'");
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            context.Errors.Add("Play Mode를 종료한 뒤 실행해 주세요.");

        context.Canvas = FindRoot(context.Scene, "Canvas");
        if (context.Canvas == null)
        {
            context.Errors.Add("Scene 루트 Canvas가 없습니다.");
            return Finish(context, log);
        }
        if (PrefabUtility.GetOutermostPrefabInstanceRoot(context.Canvas) != context.Canvas ||
            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(context.Canvas) != CanvasPrefabPath)
            context.Errors.Add("Canvas가 예상한 Canvas.prefab의 최상위 Scene instance가 아닙니다.");

        context.ShopTap = Find(context.Canvas.transform, "ShopTap")?.gameObject;
        context.TopBar = Find(context.ShopTap, "TopBar")?.gameObject;
        context.ShopText = Find(context.TopBar, "ShopText")?.GetComponent<TMP_Text>();
        context.GoldText = Find(context.TopBar, "GoldText")?.GetComponent<TMP_Text>();
        context.TopButtons = Find(context.ShopTap, "TopBottons")?.gameObject;
        context.EquipmentButton = Find(context.TopButtons, "EquipmentButton")?.GetComponent<Button>();
        context.ConsumableButton = Find(context.TopButtons, "ConsumableButton")?.GetComponent<Button>();
        context.ShopScroll = Find(context.ShopTap, "ShopScroll")?.GetComponent<ScrollRect>();
        context.Viewport = context.ShopScroll == null ? null :
            Find(context.ShopScroll.gameObject, "Viewport")?.GetComponent<RectTransform>();
        context.Content = context.Viewport == null ? null :
            Find(context.Viewport.gameObject, "Content")?.GetComponent<RectTransform>();
        context.GachaRoot = Find(context.ShopTap, "GachaRoot")?.gameObject;
        context.GachaPanel = Find(context.GachaRoot, "GachaPanel")?.gameObject;
        context.GachaImage = Find(context.GachaPanel, "GachaImage")?.GetComponent<Image>();
        context.OneGachaButton = Find(context.GachaPanel, "OneGachaButton")?.GetComponent<Button>();
        context.TenGachaButton = Find(context.GachaPanel, "TenGachaButton")?.GetComponent<Button>();
        context.ProbabilityButton = Find(context.GachaPanel, "ProbabilityButton")?.GetComponent<Button>();
        context.ProbabilityPopup = Find(context.GachaPanel, "ProbabilityPopup")?.gameObject;
        context.ResultPopup = Find(context.GachaPanel, "ResultPopup")?.gameObject;
        context.TapBar = Find(context.Canvas.transform, "TapBar")?.gameObject;
        context.HabitTapButton = Find(context.TapBar, "HabitTapButton")?.GetComponent<Button>();
        context.CharacterTapButton = Find(context.TapBar, "CharacterTapButton")?.GetComponent<Button>();
        context.ShopTapButton = Find(context.TapBar, "ShopTapButton")?.GetComponent<Button>();
        context.PvpTapButton = Find(context.TapBar, "PvPTapButton")?.GetComponent<Button>();
        context.ShopManager = FindUnique<ShopUIManager>(context);
        context.TabManager = FindUnique<MainTabManager>(context);
        context.GachaManager = FindUnique<GachaManager>(context);
        context.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        context.CardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardSpritePath);
        context.CreamButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CreamButtonSpritePath);
        context.GoldButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GoldButtonSpritePath);
        context.SlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlotPrefabPath);

        Require(context, context.ShopTap, "Canvas/ShopTap");
        Require(context, context.TopBar, "ShopTap/TopBar");
        Require(context, context.ShopText, "TopBar/ShopText");
        Require(context, context.GoldText, "TopBar/GoldText");
        Require(context, context.TopButtons, "ShopTap/TopBottons");
        Require(context, context.EquipmentButton, "TopBottons/EquipmentButton");
        Require(context, context.ConsumableButton, "TopBottons/ConsumableButton");
        Require(context, context.ShopScroll, "ShopTap/ShopScroll");
        Require(context, context.Viewport, "ShopScroll/Viewport");
        Require(context, context.Content, "ShopScroll/Viewport/Content");
        Require(context, context.GachaRoot, "ShopTap/GachaRoot (검증 전용)");
        Require(context, context.GachaPanel, "GachaRoot/GachaPanel");
        Require(context, context.GachaImage, "GachaPanel/GachaImage");
        Require(context, context.OneGachaButton, "GachaPanel/OneGachaButton");
        Require(context, context.TenGachaButton, "GachaPanel/TenGachaButton");
        Require(context, context.ProbabilityButton, "GachaPanel/ProbabilityButton");
        Require(context, context.ProbabilityPopup, "GachaPanel/ProbabilityPopup (보호 전용)");
        Require(context, context.ResultPopup, "GachaPanel/ResultPopup (보호 전용)");
        Require(context, context.TapBar, "Canvas/TapBar");
        Require(context, context.HabitTapButton, "TapBar/HabitTapButton");
        Require(context, context.CharacterTapButton, "TapBar/CharacterTapButton");
        Require(context, context.ShopTapButton, "TapBar/ShopTapButton");
        Require(context, context.PvpTapButton, "TapBar/PvPTapButton");
        Require(context, context.ShopManager, "ShopUIManager");
        Require(context, context.TabManager, "MainTabManager");
        Require(context, context.GachaManager, "GachaManager");
        Require(context, context.Font, FontPath);
        Require(context, context.CardSprite, CardSpritePath);
        Require(context, context.CreamButtonSprite, CreamButtonSpritePath);
        Require(context, context.GoldButtonSprite, GoldButtonSpritePath);
        Require(context, context.SlotPrefab, SlotPrefabPath);

        if (context.ShopManager != null) ReadAndValidateShopReferences(context);
        if (context.TabManager != null) ValidateTabReferences(context);
        if (context.GachaManager != null) ValidateGachaReferences(context);
        ValidateTapBarListeners(context);
        ValidatePrefabScope(context);
        return Finish(context, log);
    }

    private static void ReadAndValidateShopReferences(Context context)
    {
        SerializedObject serialized = new SerializedObject(context.ShopManager);
        context.ItemDetailPopup = Reference<GameObject>(serialized, "itemDetailPopup");
        context.ItemDetailIcon = Reference<Image>(serialized, "itemDetailIcon");
        context.ItemDetailName = Reference<TMP_Text>(serialized, "itemDetailNameText");
        context.ItemDetailDescription = Reference<TMP_Text>(serialized, "itemDetailDescText");
        context.ItemDetailPrice = Reference<TMP_Text>(serialized, "itemDetailPriceText");
        context.ItemDetailStatus = Reference<TMP_Text>(serialized, "itemDetailPurchaseStatusText");
        context.BuyButton = Reference<Button>(serialized, "buyButton");
        context.CloseButton = Reference<Button>(serialized, "closeButton");
        context.PurchaseFailPopup = Reference<GameObject>(serialized, "purchaseFailPopup");
        context.PurchaseFailText = Reference<TMP_Text>(serialized, "purchaseFailPurchaseStatusText");
        context.PurchaseFailCloseButton = Reference<Button>(serialized, "purchasePopupCloseButton");

        CheckReference(context, serialized, "goldText", context.GoldText);
        CheckReference(context, serialized, "equipmentPanelButton", context.EquipmentButton);
        CheckReference(context, serialized, "consumablePanelButton", context.ConsumableButton);
        CheckReference(context, serialized, "itemSlotParent", context.Content);
        CheckReference(context, serialized, "shopScrollView", context.ShopScroll.gameObject);
        CheckReference(context, serialized, "itemSlotPrefab",
            context.SlotPrefab != null ? context.SlotPrefab.GetComponent<ShopItemSlotUI>() : null);

        Require(context, context.ItemDetailPopup, "ShopUIManager/itemDetailPopup");
        Require(context, context.ItemDetailIcon, "ShopUIManager/itemDetailIcon");
        Require(context, context.ItemDetailName, "ShopUIManager/itemDetailNameText");
        Require(context, context.ItemDetailDescription, "ShopUIManager/itemDetailDescText");
        Require(context, context.ItemDetailPrice, "ShopUIManager/itemDetailPriceText");
        Require(context, context.ItemDetailStatus, "ShopUIManager/itemDetailPurchaseStatusText");
        Require(context, context.BuyButton, "ShopUIManager/buyButton");
        Require(context, context.CloseButton, "ShopUIManager/closeButton");
        Require(context, context.PurchaseFailPopup, "ShopUIManager/purchaseFailPopup");
        Require(context, context.PurchaseFailText, "ShopUIManager/purchaseFailPurchaseStatusText");
        Require(context, context.PurchaseFailCloseButton, "ShopUIManager/purchasePopupCloseButton");
    }

    private static void ValidateTabReferences(Context context)
    {
        SerializedObject serialized = new SerializedObject(context.TabManager);
        CheckReference(context, serialized, "shopTap", context.ShopTap);
        CheckReference(context, serialized, "habitTapButton", context.HabitTapButton);
        CheckReference(context, serialized, "characterTapButton", context.CharacterTapButton);
        CheckReference(context, serialized, "shopTapButton", context.ShopTapButton);
        CheckReference(context, serialized, "battleTapButton", context.PvpTapButton);
    }

    private static void ValidateGachaReferences(Context context)
    {
        SerializedObject serialized = new SerializedObject(context.GachaManager);
        CheckReference(context, serialized, "gachaPanel", context.GachaPanel);
        CheckReference(context, serialized, "gachaMainImage", context.GachaImage.gameObject);
        CheckReference(context, serialized, "oneGachaButton", context.OneGachaButton);
        CheckReference(context, serialized, "tenGachaButton", context.TenGachaButton);
        CheckReference(context, serialized, "probabilityButton", context.ProbabilityButton);
        CheckReference(context, serialized, "probabilityPopup", context.ProbabilityPopup);
    }

    private static void ValidateTapBarListeners(Context context)
    {
        Button[] buttons = context.TapBar != null
            ? context.TapBar.GetComponentsInChildren<Button>(true)
            : Array.Empty<Button>();
        if (buttons.Length != 4)
            context.Errors.Add($"TapBar Button은 정확히 4개여야 합니다. 현재: {buttons.Length}");
        if (!File.Exists(MainTabManagerPath))
        {
            context.Errors.Add("MainTabManager 소스가 없습니다: " + MainTabManagerPath);
            return;
        }
        string source = File.ReadAllText(MainTabManagerPath);
        string[] listeners =
        {
            "habitTapButton.onClick.AddListener(OnClickHabitTap)",
            "characterTapButton.onClick.AddListener(OnClickCharacterTap)",
            "shopTapButton.onClick.AddListener(OnClickShopTap)",
            "battleTapButton.onClick.AddListener(OnClickBattleTap)"
        };
        foreach (string listener in listeners)
            if (!source.Contains(listener))
                context.Errors.Add("MainTabManager 런타임 AddListener가 없습니다: " + listener);
    }

    private static void ValidatePrefabScope(Context context)
    {
        if (context.ShopTap != null &&
            PrefabUtility.GetOutermostPrefabInstanceRoot(context.ShopTap) != context.Canvas)
            context.Errors.Add("ShopTap이 Canvas Prefab instance 밖에 있습니다.");
        if (context.SlotPrefab != null &&
            AssetDatabase.GetAssetPath(context.SlotPrefab) != SlotPrefabPath)
            context.Errors.Add("ShopItemSlot 원본 Prefab 경로가 예상과 다릅니다.");
    }

    private static void ApplySceneStyle(Context context)
    {
        StyleImage(GetOrAdd<Image>(context.ShopTap), null, Cream, Image.Type.Simple);
        SetStretch(context.ShopTap.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        StyleImage(GetOrAdd<Image>(context.TopBar), context.CardSprite, CreamLight, Image.Type.Sliced);
        SetTop(context.TopBar.GetComponent<RectTransform>(), 230f, 35f, 38f);
        StyleText(context.ShopText, "상점", context.Font, 52f, Brown, FontStyles.Bold);
        StyleText(context.GoldText, null, context.Font, 38f, Brown, FontStyles.Bold);
        SetCentered(context.ShopText.rectTransform, new Vector2(520f, 76f), new Vector2(-180f, 12f));
        SetCentered(context.GoldText.rectTransform, new Vector2(360f, 76f), new Vector2(270f, 12f));

        SetTop(context.TopButtons.GetComponent<RectTransform>(), 120f, 285f, 50f);
        StyleCategory(context.EquipmentButton, "장비 가챠", context.GoldButtonSprite, context.Font, -250f);
        StyleCategory(context.ConsumableButton, "소비 아이템", context.CreamButtonSprite, context.Font, 250f);

        StyleImage(GetOrAdd<Image>(context.ShopScroll.gameObject), context.CardSprite, CreamLight,
            Image.Type.Sliced);
        RectTransform scrollRect = context.ShopScroll.GetComponent<RectTransform>();
        SetInsetStretch(scrollRect, 40f, 40f, 450f, 210f);
        Record(context.ShopScroll);
        context.ShopScroll.horizontal = false;
        context.ShopScroll.vertical = true;
        context.ShopScroll.movementType = ScrollRect.MovementType.Elastic;
        context.ShopScroll.scrollSensitivity = 35f;
        SetStretch(context.Viewport, new Vector2(34f, 38f), new Vector2(-34f, -38f));

        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(context.Content.gameObject);
        Record(layout);
        layout.padding = new RectOffset(10, 10, 8, 28);
        layout.spacing = 24f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(context.Content.gameObject);
        Record(fitter);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        Record(context.Content);
        context.Content.anchorMin = new Vector2(0f, 1f);
        context.Content.anchorMax = new Vector2(1f, 1f);
        context.Content.pivot = new Vector2(0.5f, 1f);
        context.Content.anchoredPosition = Vector2.zero;
        context.Content.sizeDelta = Vector2.zero;

        ApplyGachaMainLayout(context);
        ApplyPurchasePopupStyle(context);
    }

    private static void ApplyGachaMainLayout(Context context)
    {
        // 15-D-6A follow-up only: restore the existing main-panel layout. Probability/result
        // popup contents remain untouched for 15-D-6B.
        StyleImage(GetOrAdd<Image>(context.GachaPanel), context.CardSprite, CreamLight, Image.Type.Sliced);
        SetInsetStretch(context.GachaPanel.GetComponent<RectTransform>(), 40f, 40f, 450f, 210f);

        Record(context.GachaImage);
        context.GachaImage.preserveAspect = true;
        // Canvas.prefab currently has no sprite assigned and GachaManager never assigns one.
        // Do not render that empty Image as an opaque white rectangle.
        context.GachaImage.enabled = context.GachaImage.sprite != null;
        SetCentered(context.GachaImage.rectTransform, new Vector2(620f, 570f), new Vector2(0f, 210f));

        SetCentered(context.OneGachaButton.GetComponent<RectTransform>(),
            new Vector2(420f, 125f), new Vector2(-235f, -275f));
        SetCentered(context.TenGachaButton.GetComponent<RectTransform>(),
            new Vector2(420f, 125f), new Vector2(235f, -275f));
        SetCentered(context.ProbabilityButton.GetComponent<RectTransform>(),
            new Vector2(420f, 100f), new Vector2(0f, -430f));
    }

    private static void ApplyPurchasePopupStyle(Context context)
    {
        StyleImage(GetOrAdd<Image>(context.ItemDetailPopup), context.CardSprite, CreamLight, Image.Type.Sliced);
        SetCentered(context.ItemDetailPopup.GetComponent<RectTransform>(), new Vector2(900f, 960f), Vector2.zero);
        Record(context.ItemDetailIcon);
        context.ItemDetailIcon.type = Image.Type.Simple;
        context.ItemDetailIcon.preserveAspect = true;
        context.ItemDetailIcon.color = Color.white;
        RecordPrefabModification(context.ItemDetailIcon);
        SetCentered(context.ItemDetailIcon.rectTransform, new Vector2(180f, 180f), new Vector2(0f, 285f));
        StylePopupText(context.ItemDetailName, context.Font, 46f, Brown, FontStyles.Bold,
            new Vector2(720f, 80f), new Vector2(0f, 95f));
        StylePopupText(context.ItemDetailDescription, context.Font, 31f, ReadableBrown, FontStyles.Normal,
            new Vector2(740f, 190f), new Vector2(0f, -45f));
        StylePopupText(context.ItemDetailPrice, context.Font, 39f, Gold, FontStyles.Bold,
            new Vector2(330f, 76f), new Vector2(0f, -190f));
        StylePopupText(context.ItemDetailStatus, context.Font, 29f, ReadableBrown, FontStyles.Bold,
            new Vector2(740f, 76f), new Vector2(0f, -275f));
        StylePopupButton(context.BuyButton, "구매하기", context.GoldButtonSprite, context.Font,
            new Vector2(-205f, -385f));
        StylePopupButton(context.CloseButton, "닫기", context.CreamButtonSprite, context.Font,
            new Vector2(205f, -385f));
        StyleImage(GetOrAdd<Image>(context.PurchaseFailPopup), context.CardSprite, CreamLight, Image.Type.Sliced);
        SetCentered(context.PurchaseFailPopup.GetComponent<RectTransform>(), new Vector2(820f, 520f), Vector2.zero);
        StylePopupText(context.PurchaseFailText, context.Font, 36f, Brown, FontStyles.Bold,
            new Vector2(680f, 220f), new Vector2(0f, 65f));
        StylePopupButton(context.PurchaseFailCloseButton, "닫기", context.GoldButtonSprite, context.Font,
            new Vector2(0f, -150f));
    }

    private static void ApplySlotPrefab(Context context)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(SlotPrefabPath);
        try
        {
            Image background = root.GetComponent<Image>();
            Button button = root.GetComponent<Button>();
            ShopItemSlotUI slot = root.GetComponent<ShopItemSlotUI>();
            if (background == null || button == null || slot == null)
                throw new InvalidOperationException(
                    "ShopItemSlot Prefab의 Image/Button/ShopItemSlotUI 구성이 올바르지 않습니다.");

            string slotReferences = CaptureObjectReferences(slot);
            string buttonEvent = CaptureEvent(button.onClick);
            string hierarchy = CapturePrefabHierarchy(root.transform);

            StyleImageAsset(background, context.CardSprite, Color.white, Image.Type.Sliced);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(920f, 230f);
            LayoutElement layout = root.GetComponent<LayoutElement>();
            if (layout == null) layout = root.AddComponent<LayoutElement>();
            layout.minHeight = 230f;
            layout.preferredHeight = 230f;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(0xFF, 0xF4, 0xC9, 0xFF);
            colors.pressedColor = new Color32(0xD8, 0xB5, 0x6C, 0xFF);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color32(0xB8, 0xAA, 0x98, 0xFF);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = context.Font;
                bool price = Contains(text.name, "Price");
                bool description = Contains(text.name, "Describe") || Contains(text.name, "Description");
                bool name = Contains(text.name, "Name");
                bool type = Contains(text.name, "Type");
                text.color = price ? Gold : description ? ReadableBrown : Brown;
                text.fontSize = name ? 36f : description ? 27f : price ? 30f : type ? 24f : 27f;
                text.fontStyle = name || price || type ? FontStyles.Bold : FontStyles.Normal;
                text.enableWordWrapping = true;
                text.overflowMode = TextOverflowModes.Ellipsis;
            }
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
                if (image != background) image.preserveAspect = true;

            if (slotReferences != CaptureObjectReferences(slot) ||
                buttonEvent != CaptureEvent(button.onClick) ||
                hierarchy != CapturePrefabHierarchy(root.transform))
                throw new InvalidOperationException(
                    "ShopItemSlot 보호 대상 SerializeField, UnityEvent, activeSelf 또는 sibling 순서가 변경되었습니다.");
            if (background.sprite != context.CardSprite || background.type != Image.Type.Sliced ||
                !Mathf.Approximately(layout.preferredHeight, 230f) ||
                root.GetComponentsInChildren<TMP_Text>(true).Any(text => text.font != context.Font))
                throw new InvalidOperationException("ShopItemSlot 스타일 사전 검증에 실패했습니다.");

            PrefabUtility.SaveAsPrefabAsset(root, SlotPrefabPath, out bool saved);
            if (!saved) throw new InvalidOperationException("ShopItemSlot.prefab 저장에 실패했습니다.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static string CaptureObjectReferences(UnityEngine.Object target)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty iterator = serialized.GetIterator();
        List<string> values = new List<string>();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                values.Add(iterator.propertyPath + "=" +
                           (iterator.objectReferenceValue == null ? "null" :
                               GlobalObjectId.GetGlobalObjectIdSlow(iterator.objectReferenceValue).ToString()));
        }
        return string.Join("|", values);
    }

    private static string CaptureEvent(UnityEventBase value)
    {
        return string.Join(",", Enumerable.Range(0, value.GetPersistentEventCount())
            .Select(index =>
            {
                UnityEngine.Object target = value.GetPersistentTarget(index);
                return (target == null ? "null" : GlobalObjectId.GetGlobalObjectIdSlow(target).ToString()) +
                       ":" + value.GetPersistentMethodName(index);
            }));
    }

    private static string CapturePrefabHierarchy(Transform root)
    {
        return string.Join("|", root.GetComponentsInChildren<Transform>(true)
            .Select(item => item.name + ":" + item.gameObject.activeSelf + ":" + item.GetSiblingIndex()));
    }

    private static List<string> ValidateAppliedLayout(Context context, bool requireSlotStyle)
    {
        List<string> errors = new List<string>();
        if (!context.IsValid) return new List<string>(context.Errors);
        if (context.ShopText.font != context.Font || context.GoldText.font != context.Font)
            errors.Add("헤더에 NotoSansKR TMP Font가 적용되지 않았습니다.");
        if (!context.ShopScroll.vertical || context.ShopScroll.horizontal ||
            context.ShopScroll.viewport != context.Viewport || context.ShopScroll.content != context.Content)
            errors.Add("ShopScroll 세로 스크롤 또는 viewport/content 참조가 올바르지 않습니다.");
        if (context.Content.GetComponent<VerticalLayoutGroup>() == null ||
            context.Content.GetComponent<ContentSizeFitter>() == null)
            errors.Add("상품 Content 자동 세로 Layout 구성이 없습니다.");
        if (!MatchesInsetStretch(context.ShopScroll.GetComponent<RectTransform>(), 40f, 40f, 450f, 210f))
            errors.Add("ShopScroll이 카테고리/TapBar를 피하는 본문 안전 영역에 배치되지 않았습니다.");
        if (!MatchesInsetStretch(context.GachaPanel.GetComponent<RectTransform>(), 40f, 40f, 450f, 210f))
            errors.Add("GachaPanel이 상점 본문 안전 영역에 배치되지 않았습니다.");
        if (context.GachaImage.sprite == null && context.GachaImage.enabled)
            errors.Add("Sprite가 없는 GachaImage가 흰색 빈 박스로 렌더링되고 있습니다.");
        RectTransform probabilityRect = context.ProbabilityButton.GetComponent<RectTransform>();
        if (!Approximately(probabilityRect.anchorMin, new Vector2(0.5f, 0.5f)) ||
            !Approximately(probabilityRect.anchorMax, new Vector2(0.5f, 0.5f)) ||
            !Approximately(probabilityRect.anchoredPosition, new Vector2(0f, -430f)))
            errors.Add("ProbabilityButton이 GachaPanel 하단 중앙 위치에 있지 않습니다.");
        if (context.ItemDetailIcon.type != Image.Type.Simple || !context.ItemDetailIcon.preserveAspect ||
            !Approximately(context.ItemDetailIcon.rectTransform.sizeDelta, new Vector2(180f, 180f)))
            errors.Add("상품 상세 ItemIcon이 180px Simple/preserveAspect 설정이 아닙니다.");
        if (context.TapBar == null || !context.TapBar.activeSelf)
            errors.Add("하단 TapBar가 없거나 비활성화되었습니다.");
        if (requireSlotStyle)
        {
            GameObject slot = AssetDatabase.LoadAssetAtPath<GameObject>(SlotPrefabPath);
            Image image = slot != null ? slot.GetComponent<Image>() : null;
            LayoutElement layout = slot != null ? slot.GetComponent<LayoutElement>() : null;
            if (image == null || image.sprite != context.CardSprite || image.type != Image.Type.Sliced)
                errors.Add("ShopItemSlot RPG 카드 Sprite가 적용되지 않았습니다.");
            if (layout == null || !Mathf.Approximately(layout.preferredHeight, 230f))
                errors.Add("ShopItemSlot 230px LayoutElement가 적용되지 않았습니다.");
            if (slot != null && slot.GetComponentsInChildren<TMP_Text>(true).Any(text => text.font != context.Font))
                errors.Add("ShopItemSlot 일부 텍스트에 NotoSansKR가 적용되지 않았습니다.");
        }
        return errors;
    }

    private static string BuildDiagnostics(Context context)
    {
        return "Hierarchy: Canvas/ShopTap/{TopBar, TopBottons, ShopScroll/Viewport/Content, GachaRoot}\n" +
               "ShopUIManager: Scene root, itemSlotPrefab=" + AssetDatabase.GetAssetPath(context.SlotPrefab) + "\n" +
               "TapBar: " + string.Join(", ", new[] { context.HabitTapButton, context.CharacterTapButton,
                   context.ShopTapButton, context.PvpTapButton }.Select(button => button.name)) + "\n" +
               "GachaRoot activeSelf=" + context.GachaRoot.activeSelf +
               ", ShopScroll activeSelf=" + context.ShopScroll.gameObject.activeSelf;
    }

    private static Context Finish(Context context, bool log)
    {
        if (log)
            foreach (string error in context.Errors)
                Debug.LogError("[HabitPVP MainScene] Shop Main: " + error);
        return context;
    }

    private static void Require(Context context, UnityEngine.Object value, string label)
    {
        if (value == null) context.Errors.Add("필수 대상이 없습니다: " + label);
    }

    private static T FindUnique<T>(Context context) where T : Component
    {
        T[] values = context.Scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        if (values.Length != 1)
            context.Errors.Add(typeof(T).Name + $"가 Scene에 정확히 1개여야 합니다. 현재: {values.Length}");
        return values.Length == 1 ? values[0] : null;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
    }

    private static Transform Find(GameObject root, string path)
    {
        return root == null ? null : root.transform.Find(path);
    }

    private static Transform Find(Transform root, string path)
    {
        return root == null ? null : root.Find(path);
    }

    private static T Reference<T>(SerializedObject serialized, string name) where T : UnityEngine.Object
    {
        SerializedProperty property = serialized.FindProperty(name);
        return property == null ? null : property.objectReferenceValue as T;
    }

    private static void CheckReference(Context context, SerializedObject serialized, string propertyName,
        UnityEngine.Object expected)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null) context.Errors.Add("SerializeField가 없습니다: " + propertyName);
        else if (property.objectReferenceValue != expected)
            context.Errors.Add("SerializeField 참조가 Hierarchy와 다릅니다: " + propertyName);
    }

    private static void StyleCategory(Button button, string label, Sprite sprite, TMP_FontAsset font, float x)
    {
        StyleImage(button.GetComponent<Image>(), sprite, Color.white, Image.Type.Sliced);
        SetCentered(button.GetComponent<RectTransform>(), new Vector2(450f, 108f), new Vector2(x, 0f));
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        StyleText(text, label, font, 31f, Brown, FontStyles.Bold);
        text.enableWordWrapping = false;
        SetStretch(text.rectTransform, new Vector2(18f, 10f), new Vector2(-18f, -10f));
        Record(button);
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(0xFF, 0xF4, 0xC9, 0xFF);
        colors.pressedColor = new Color32(0xD8, 0xB5, 0x6C, 0xFF);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color32(0xD7, 0xCC, 0xBA, 0xFF);
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }

    private static void StylePopupText(TMP_Text text, TMP_FontAsset font, float size, Color color,
        FontStyles style, Vector2 rectSize, Vector2 position)
    {
        StyleText(text, null, font, size, color, style);
        text.enableWordWrapping = true;
        text.alignment = TextAlignmentOptions.Center;
        SetCentered(text.rectTransform, rectSize, position);
    }

    private static void StylePopupButton(Button button, string label, Sprite sprite, TMP_FontAsset font,
        Vector2 position)
    {
        StyleImage(button.GetComponent<Image>(), sprite, Color.white, Image.Type.Sliced);
        SetCentered(button.GetComponent<RectTransform>(), new Vector2(340f, 110f), position);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        StyleText(text, label, font, 31f, Brown, FontStyles.Bold);
        SetStretch(text.rectTransform, new Vector2(18f, 10f), new Vector2(-18f, -10f));
    }

    private static void StyleImage(Image image, Sprite sprite, Color color, Image.Type type)
    {
        Record(image);
        image.sprite = sprite;
        image.color = color;
        image.type = type;
        RecordPrefabModification(image);
    }

    private static void StyleImageAsset(Image image, Sprite sprite, Color color, Image.Type type)
    {
        image.sprite = sprite;
        image.color = color;
        image.type = type;
        EditorUtility.SetDirty(image);
    }

    private static void StyleText(TMP_Text text, string value, TMP_FontAsset font, float size,
        Color color, FontStyles style)
    {
        Record(text);
        if (value != null) text.text = value;
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.overflowMode = TextOverflowModes.Ellipsis;
        RecordPrefabModification(text);
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static void SetCentered(RectTransform rect, Vector2 size, Vector2 position)
    {
        Record(rect);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        RecordPrefabModification(rect);
    }

    private static void SetTop(RectTransform rect, float height, float top, float side)
    {
        Record(rect);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(side, -top - height);
        rect.offsetMax = new Vector2(-side, -top);
        RecordPrefabModification(rect);
    }

    private static void SetStretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        Record(rect);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = min;
        rect.offsetMax = max;
        RecordPrefabModification(rect);
    }

    private static void SetInsetStretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        Record(rect);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        RecordPrefabModification(rect);
    }

    private static bool MatchesInsetStretch(RectTransform rect, float left, float right, float top,
        float bottom)
    {
        return rect != null && Approximately(rect.anchorMin, Vector2.zero) &&
               Approximately(rect.anchorMax, Vector2.one) &&
               Approximately(rect.offsetMin, new Vector2(left, bottom)) &&
               Approximately(rect.offsetMax, new Vector2(-right, -top));
    }

    private static bool Approximately(Vector2 left, Vector2 right)
    {
        return Mathf.Approximately(left.x, right.x) && Mathf.Approximately(left.y, right.y);
    }

    private static void Record(UnityEngine.Object target)
    {
        if (target != null) Undo.RecordObject(target, "Apply Shop Main UI Renewal");
    }

    private static void RecordPrefabModification(UnityEngine.Object target)
    {
        if (target != null && PrefabUtility.IsPartOfPrefabInstance(target))
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        if (target != null) EditorUtility.SetDirty(target);
    }

    private static bool Contains(string value, string part)
    {
        return value.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private sealed class ProtectionSnapshot
    {
        private readonly string shopReferences;
        private readonly string tabReferences;
        private readonly string gachaReferences;
        private readonly string events;
        private readonly string activeStates;
        private readonly string siblingIndices;

        private ProtectionSnapshot(Context context)
        {
            shopReferences = CaptureObjectReferences(context.ShopManager);
            tabReferences = CaptureObjectReferences(context.TabManager);
            gachaReferences = CaptureObjectReferences(context.GachaManager);
            events = CaptureEvents(context);
            activeStates = CaptureActiveStates(context);
            siblingIndices = CaptureSiblingIndices(context);
        }

        public static ProtectionSnapshot Capture(Context context) => new ProtectionSnapshot(context);

        public void AssertUnchanged(Context context)
        {
            ProtectionSnapshot after = new ProtectionSnapshot(context);
            if (shopReferences != after.shopReferences || tabReferences != after.tabReferences ||
                gachaReferences != after.gachaReferences ||
                events != after.events || activeStates != after.activeStates ||
                siblingIndices != after.siblingIndices)
                throw new InvalidOperationException(
                    "보호 대상 SerializeField 참조, UnityEvent, activeSelf 또는 sibling 순서가 변경되었습니다.");
        }

        private static string CaptureObjectReferences(UnityEngine.Object target)
        {
            if (target == null) return string.Empty;
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty iterator = serialized.GetIterator();
            List<string> values = new List<string>();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                    values.Add(iterator.propertyPath + "=" +
                               (iterator.objectReferenceValue == null ? "null" :
                                   iterator.objectReferenceValue.GetInstanceID().ToString()));
            }
            return string.Join("|", values);
        }

        private static string CaptureEvents(Context context)
        {
            IEnumerable<Button> sceneButtons = context.ShopTap.GetComponentsInChildren<Button>(true)
                .Concat(context.TapBar.GetComponentsInChildren<Button>(true));
            Button slotButton = context.SlotPrefab.GetComponent<Button>();
            if (slotButton != null) sceneButtons = sceneButtons.Append(slotButton);
            return string.Join("|", sceneButtons.OrderBy(button => button.name)
                .Select(button => button.name + ":" + CaptureEvent(button.onClick)));
        }

        private static string CaptureEvent(UnityEventBase value)
        {
            return string.Join(",", Enumerable.Range(0, value.GetPersistentEventCount())
                .Select(index => value.GetPersistentTarget(index)?.GetInstanceID() + ":" +
                                 value.GetPersistentMethodName(index)));
        }

        private static string CaptureActiveStates(Context context)
        {
            return string.Join("|", context.ShopTap.GetComponentsInChildren<Transform>(true)
                .Select(item => GetPath(item) + "=" + item.gameObject.activeSelf));
        }

        private static string CaptureSiblingIndices(Context context)
        {
            return string.Join("|", context.ShopTap.GetComponentsInChildren<Transform>(true)
                .Select(item => GetPath(item) + "=" + item.GetSiblingIndex()));
        }

        private static string GetPath(Transform transform)
        {
            List<string> parts = new List<string>();
            while (transform != null)
            {
                parts.Add(transform.name);
                transform = transform.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }
    }

    private sealed class Context
    {
        public Scene Scene;
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
        public GameObject Canvas, ShopTap, TopBar, TopButtons, GachaRoot, GachaPanel,
            ProbabilityPopup, ResultPopup, TapBar;
        public TMP_Text ShopText, GoldText, ItemDetailName, ItemDetailDescription, ItemDetailPrice,
            ItemDetailStatus, PurchaseFailText;
        public Button EquipmentButton, ConsumableButton, HabitTapButton, CharacterTapButton,
            ShopTapButton, PvpTapButton, BuyButton, CloseButton, PurchaseFailCloseButton;
        public Button OneGachaButton, TenGachaButton, ProbabilityButton;
        public ScrollRect ShopScroll;
        public RectTransform Viewport, Content;
        public ShopUIManager ShopManager;
        public MainTabManager TabManager;
        public GachaManager GachaManager;
        public GameObject ItemDetailPopup, PurchaseFailPopup, SlotPrefab;
        public Image ItemDetailIcon;
        public Image GachaImage;
        public TMP_FontAsset Font;
        public Sprite CardSprite, CreamButtonSprite, GoldButtonSprite;
    }
}
