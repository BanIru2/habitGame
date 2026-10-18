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
public static class ShopGachaUiRenewalEditor
{
    private const string ScenePath = "Assets/Scenes/UITestScene.unity";
    private const string CanvasPrefabPath = "Assets/Resources/UIPrefabs/Canvas.prefab";
    private const string FontPath = "Assets/Login/Fonts/NotoSansKR Login SDF.asset";
    private const string CardSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/panel_rpg_card.png";
    private const string CreamButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_cream.png";
    private const string GoldButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_rpg.png";
    private const string PreviewRootName = "ShopGachaPreviewRoot";
    private const string ValidateMenu = "Tools/HabitPVP/UI Renewal/Validate Shop & Gacha Renewal";
    private const string ApplyMenu = "Tools/HabitPVP/UI Renewal/Apply Shop & Gacha Renewal";
    private const string PreviewMenu =
        "Tools/HabitPVP/UI Renewal/Preview Mode/H. Shop & Gacha Renewal";

    private static readonly string[] OtherPreviewRoots =
    {
        "HabitItemPreviewTestRoot", "NumberOverlayPreviewRoot", "PhotoVerificationPreviewRoot",
        "CharacterMainPreviewRoot", "InventoryMainPreviewRoot"
    };

    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 CreamLight = new Color32(0xFF, 0xF9, 0xEA, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x86, 0x68, 0x5D, 0xFF);
    private static readonly Color32 ReadableBrown = new Color32(0x6E, 0x4B, 0x45, 0xFF);
    private static readonly Color32 AlertBrown = new Color32(0x8F, 0x3F, 0x35, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);

    static ShopGachaUiRenewalEditor()
    {
        EditorApplication.update -= RestoreWhenAnotherPreviewIsSelected;
        EditorApplication.update += RestoreWhenAnotherPreviewIsSelected;
        EditorSceneManager.sceneSaving -= RestoreBeforeSceneSave;
        EditorSceneManager.sceneSaving += RestoreBeforeSceneSave;
    }

    [MenuItem(ValidateMenu, false, 2090)]
    private static void Validate()
    {
        Context context = ValidateScene(true);
        if (!context.IsValid)
        {
            return;
        }

        List<string> errors = ValidateAppliedLayout(context, true);
        if (errors.Count == 0)
        {
            Debug.Log("[HabitPVP Shop] 검증 성공. ShopTap RPG 스타일, 스크롤 설정, H Preview와 로컬 격리가 정상입니다.", context.ShopTap);
            return;
        }
        foreach (string error in errors)
        {
            Debug.LogError("[HabitPVP Shop] " + error, context.ShopTap);
        }
    }

    [MenuItem(ApplyMenu, false, 2091)]
    private static void Apply()
    {
        Context context = ValidateScene(true);
        if (!context.IsValid)
        {
            return;
        }

        string managerBefore = EditorJsonUtility.ToJson(context.ShopManager);
        string gachaManagerBefore = EditorJsonUtility.ToJson(context.GachaManager);
        string resultManagerBefore = EditorJsonUtility.ToJson(context.GachaResultManager);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Shop & Gacha UI Renewal");
        try
        {
            ApplyShopStyle(context);
            ApplyGachaStyle(context);
            EnsurePreviewRoot(context);
            if (managerBefore != EditorJsonUtility.ToJson(context.ShopManager))
            {
                throw new InvalidOperationException("ShopUIManager의 SerializeField 또는 UnityEvent 참조가 변경되었습니다.");
            }
            if (gachaManagerBefore != EditorJsonUtility.ToJson(context.GachaManager))
            {
                throw new InvalidOperationException("GachaManager의 SerializeField 또는 UnityEvent 참조가 변경되었습니다.");
            }
            if (resultManagerBefore != EditorJsonUtility.ToJson(context.GachaResultManager))
            {
                throw new InvalidOperationException("GachaResultPopupManager의 SerializeField 참조가 변경되었습니다.");
            }

            List<string> errors = ValidateAppliedLayout(context, true);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join("\n", errors));
            }

            EditorSceneManager.MarkSceneDirty(context.Scene);
            Undo.CollapseUndoOperations(group);
            if (!EditorSceneManager.SaveScene(context.Scene))
            {
                throw new InvalidOperationException("UITestScene 저장에 실패했습니다.");
            }
            Selection.activeGameObject = context.ShopTap;
            Debug.Log("[HabitPVP Shop] 적용 성공. UITestScene ShopTap Override와 H Preview 루트만 저장했습니다. Canvas.prefab, MainScene, ShopItemSlot.prefab 및 런타임 로직은 변경하지 않았습니다.", context.ShopTap);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(group);
            EditorSceneManager.MarkSceneDirty(context.Scene);
            Debug.LogException(exception);
            Debug.LogError("[HabitPVP Shop] Apply 실패. 이번 실행의 변경을 Undo로 되돌렸습니다.");
        }
    }

    [MenuItem(ApplyMenu, true)]
    private static bool CanApply() => CanUseMenus();

    [MenuItem(PreviewMenu, false, 2092)]
    private static void ActivatePreview()
    {
        PvpRankingUiRenewalEditor.RestorePvpPreviewState();
        RestoreShopPreviewState();
        InventoryMainUiRenewalEditor.RestoreInventoryPreviewState();
        CharacterMainUiRenewalEditor.RestoreCharacterPreviewState();
        PhotoVerificationPopupUiRenewalEditor.RestorePhotoPreviewState();
        AchievedAmountOverlayUiRenewalEditor.RestoreNumberPreviewState();
        HabitGoalFormUiRenewalEditor.RestoreHabitMainPreviewState();

        Context context = ValidateScene(true);
        if (!context.IsValid)
        {
            return;
        }
        GameObject root = FindRoot(context.Scene, PreviewRootName);
        ShopGachaPreviewController controller =
            root != null ? root.GetComponent<ShopGachaPreviewController>() : null;
        if (root == null || controller == null)
        {
            Debug.LogError("[HabitPVP Shop] H Preview가 없습니다. Apply Shop & Gacha Renewal을 먼저 실행해주세요.");
            return;
        }

        foreach (string rootName in OtherPreviewRoots)
        {
            SetActive(FindRoot(context.Scene, rootName), false, "Hide other preview for H mode");
        }
        IsolateScene(context.Scene, root, controller);
        SetActive(root, true, "Activate shop preview");
        EditorSceneManager.MarkSceneDirty(context.Scene);
        Selection.activeGameObject = root;
        Debug.Log("[HabitPVP Shop] H 모드: 로컬 골드/소비 상품과 12-C 장비 가챠·테스트 확률 팝업을 표시합니다. ShopUIManager, GachaManager 및 API는 호출하지 않습니다.", root);
    }

    [MenuItem(PreviewMenu, true)]
    private static bool CanActivatePreview() => CanUseMenus();

    private static bool CanUseMenus()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
               SceneManager.GetActiveScene().path == ScenePath;
    }

    private static Context ValidateScene(bool log)
    {
        Context context = new Context { Scene = SceneManager.GetActiveScene() };
        if (!context.Scene.IsValid() || !context.Scene.isLoaded)
        {
            context.Errors.Add("활성 Scene이 유효하지 않습니다.");
            return Finish(context, log);
        }
        if (context.Scene.path != ScenePath)
        {
            context.Errors.Add($"UITestScene에서만 실행할 수 있습니다. 현재: '{context.Scene.path}'");
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            context.Errors.Add("Play Mode를 종료한 뒤 실행해주세요.");
        }

        context.Canvas = FindRoot(context.Scene, "Canvas");
        if (context.Canvas == null)
        {
            context.Errors.Add("Scene 루트 Canvas를 찾을 수 없습니다.");
            return Finish(context, log);
        }
        string source = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(context.Canvas);
        if (source != CanvasPrefabPath)
        {
            context.Errors.Add($"Canvas 원본 Prefab이 예상과 다릅니다: '{source}'");
        }

        context.ShopTap = Find(context.Canvas.transform, "ShopTap")?.gameObject;
        context.TopBar = Find(context.ShopTap, "TopBar")?.gameObject;
        context.GoldText = Find(context.TopBar, "GoldText")?.GetComponent<TMP_Text>();
        context.ShopText = Find(context.TopBar, "ShopText")?.GetComponent<TMP_Text>();
        context.TopButtons = Find(context.ShopTap, "TopBottons")?.gameObject;
        context.EquipmentButton = Find(context.TopButtons, "EquipmentButton")?.GetComponent<Button>();
        context.ConsumableButton = Find(context.TopButtons, "ConsumableButton")?.GetComponent<Button>();
        context.ShopScroll = Find(context.ShopTap, "ShopScroll")?.GetComponent<ScrollRect>();
        context.Viewport = context.ShopScroll != null ? Find(context.ShopScroll.gameObject, "Viewport")?.gameObject : null;
        context.Content = context.Viewport != null ? Find(context.Viewport, "Content")?.GetComponent<RectTransform>() : null;
        context.ShopManager = FindRoot(context.Scene, "ShopUIManager")?.GetComponent<ShopUIManager>();
        context.GachaRoot = Find(context.ShopTap, "GachaRoot")?.gameObject;
        context.GachaPanel = Find(context.GachaRoot, "GachaPanel")?.gameObject;
        context.GachaManager = context.Scene.GetRootGameObjects()
    .SelectMany(root => root.GetComponentsInChildren<GachaManager>(true))
    .FirstOrDefault();
        context.GachaResultManager = context.Scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<GachaResultPopupManager>(true))
            .FirstOrDefault();
        context.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        context.CardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardSpritePath);
        context.CreamButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CreamButtonSpritePath);
        context.GoldButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GoldButtonSpritePath);

        Require(context, context.ShopTap, "Canvas/ShopTap");
        Require(context, context.TopBar, "ShopTap/TopBar");
        Require(context, context.GoldText, "TopBar/GoldText");
        Require(context, context.ShopText, "TopBar/ShopText");
        Require(context, context.TopButtons, "ShopTap/TopBottons");
        Require(context, context.EquipmentButton, "TopBottons/EquipmentButton");
        Require(context, context.ConsumableButton, "TopBottons/ConsumableButton");
        Require(context, context.ShopScroll, "ShopTap/ShopScroll");
        Require(context, context.Viewport, "ShopScroll/Viewport");
        Require(context, context.Content, "ShopScroll/Viewport/Content");
        Require(context, context.ShopManager, "ShopTap/ShopUIManager");
        Require(context, context.GachaRoot, "ShopTap/GachaRoot");
        Require(context, context.GachaPanel, "GachaRoot/GachaPanel");
        Require(context, context.GachaManager, "GachaRoot/GachaManager");
        Require(context, context.GachaResultManager, "GachaResultPopupManager");
        Require(context, Find(context.GachaPanel, "GachaImage"), "GachaPanel/GachaImage");
        Require(context, Find(context.GachaPanel, "OneGachaButton"), "GachaPanel/OneGachaButton");
        Require(context, Find(context.GachaPanel, "TenGachaButton"), "GachaPanel/TenGachaButton");
        Require(context, Find(context.GachaPanel, "ProbabilityButton"), "GachaPanel/ProbabilityButton");
        Require(context, Find(context.GachaPanel, "ProbabilityPopup"), "GachaPanel/ProbabilityPopup");
        Require(context, context.Font, FontPath);
        Require(context, context.CardSprite, CardSpritePath);
        Require(context, context.CreamButtonSprite, CreamButtonSpritePath);
        Require(context, context.GoldButtonSprite, GoldButtonSpritePath);
        if (context.GachaResultManager != null)
        {
            SerializedObject resultManager = new SerializedObject(context.GachaResultManager);
            context.GachaResultPopup = GetReference<GameObject>(resultManager, "popupRoot");
            context.GachaResultDim = GetReference<GameObject>(resultManager, "dim");
            context.GachaResultContainer = GetReference<RectTransform>(resultManager, "resultContainer");
            context.GachaResultGrid = GetReference<GridLayoutGroup>(resultManager, "resultGridLayout");
            Require(context, context.GachaResultPopup, "GachaResultPopupManager/popupRoot");
            Require(context, context.GachaResultDim, "GachaResultPopupManager/dim");
            Require(context, context.GachaResultContainer, "GachaResultPopupManager/resultContainer");
            Require(context, context.GachaResultGrid, "GachaResultPopupManager/resultGridLayout");
        }
        if (context.ShopManager != null)
        {
            SerializedObject manager = new SerializedObject(context.ShopManager);
            context.ItemDetailPopup = GetReference<GameObject>(manager, "itemDetailPopup");
            context.ItemDetailIcon = GetReference<Image>(manager, "itemDetailIcon");
            context.ItemDetailName = GetReference<TMP_Text>(manager, "itemDetailNameText");
            context.ItemDetailDescription = GetReference<TMP_Text>(manager, "itemDetailDescText");
            context.ItemDetailPrice = GetReference<TMP_Text>(manager, "itemDetailPriceText");
            context.ItemDetailStatus = GetReference<TMP_Text>(manager, "itemDetailPurchaseStatusText");
            context.BuyButton = GetReference<Button>(manager, "buyButton");
            context.CloseButton = GetReference<Button>(manager, "closeButton");
            context.PurchaseFailPopup = GetReference<GameObject>(manager, "purchaseFailPopup");
            context.PurchaseFailText = GetReference<TMP_Text>(manager, "purchaseFailPurchaseStatusText");
            context.PurchaseFailCloseButton = GetReference<Button>(manager, "purchasePopupCloseButton");
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
        return Finish(context, log);
    }

    private static Context Finish(Context context, bool log)
    {
        if (log)
        {
            foreach (string error in context.Errors)
            {
                Debug.LogError("[HabitPVP Shop] " + error);
            }
        }
        return context;
    }

    private static void Require(Context context, UnityEngine.Object value, string label)
    {
        if (value == null)
        {
            context.Errors.Add("필수 대상이 없습니다: " + label);
        }
    }

    private static void ApplyShopStyle(Context context)
    {
        StyleImage(GetOrAdd<Image>(context.ShopTap), null, Cream, Image.Type.Simple);
        SetStretch(context.ShopTap.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

        Image topBarImage = GetOrAdd<Image>(context.TopBar);
        StyleImage(topBarImage, context.CardSprite, CreamLight, Image.Type.Sliced);
        SetTop(context.TopBar.GetComponent<RectTransform>(), 230f, 35f, 38f);
        StyleText(context.ShopText, "상점", context.Font, 52f, Brown, FontStyles.Bold);
        StyleText(context.GoldText, null, context.Font, 38f, Brown, FontStyles.Bold);
        SetCentered(context.ShopText.rectTransform, new Vector2(520f, 76f), new Vector2(-180f, 12f));
        SetCentered(context.GoldText.rectTransform, new Vector2(360f, 76f), new Vector2(270f, 12f));

        SetTop(context.TopButtons.GetComponent<RectTransform>(), 120f, 285f, 50f);
        StyleTab(context.EquipmentButton, "장비 가챠", context.GoldButtonSprite, context.Font, -250f);
        StyleTab(context.ConsumableButton, "소비 아이템", context.CreamButtonSprite, context.Font, 250f);

        Image scrollImage = GetOrAdd<Image>(context.ShopScroll.gameObject);
        StyleImage(scrollImage, context.CardSprite, CreamLight, Image.Type.Sliced);
        RectTransform scrollRect = context.ShopScroll.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.5f, 0f);
        scrollRect.anchorMax = new Vector2(0.5f, 1f);
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.sizeDelta = new Vector2(1000f, -520f);
        scrollRect.anchoredPosition = new Vector2(0f, -45f);
        context.ShopScroll.horizontal = false;
        context.ShopScroll.vertical = true;
        context.ShopScroll.movementType = ScrollRect.MovementType.Elastic;
        context.ShopScroll.scrollSensitivity = 35f;
        SetStretch(context.Viewport.GetComponent<RectTransform>(), new Vector2(34f, 38f), new Vector2(-34f, -38f));

        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(context.Content.gameObject);
        layout.padding = new RectOffset(10, 10, 8, 28);
        layout.spacing = 24f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(context.Content.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        context.Content.anchorMin = new Vector2(0f, 1f);
        context.Content.anchorMax = new Vector2(1f, 1f);
        context.Content.pivot = new Vector2(0.5f, 1f);
        context.Content.anchoredPosition = Vector2.zero;
        context.Content.sizeDelta = Vector2.zero;

        foreach (Transform child in context.Content)
        {
            StyleExistingCard(child.gameObject, context);
        }
        ApplyPopupStyle(context);
        Dirty(context.ShopTap);
    }

    private static void ApplyPopupStyle(Context context)
    {
        StyleImage(GetOrAdd<Image>(context.ItemDetailPopup), context.CardSprite, CreamLight, Image.Type.Sliced);
        SetCentered(context.ItemDetailPopup.GetComponent<RectTransform>(), new Vector2(900f, 960f), Vector2.zero);
        if (context.ItemDetailIcon != null)
        {
            StyleImage(context.ItemDetailIcon, context.GoldButtonSprite, Color.white, Image.Type.Sliced);
            SetCentered(context.ItemDetailIcon.rectTransform, new Vector2(240f, 240f), new Vector2(0f, 265f));
        }
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
        StylePopupText(context.PurchaseFailText, context.Font, 36f, AlertBrown, FontStyles.Bold,
            new Vector2(680f, 220f), new Vector2(0f, 65f));
        if (context.PurchaseFailText != null)
        {
            context.PurchaseFailText.enableWordWrapping = true;
            context.PurchaseFailText.overflowMode = TextOverflowModes.Ellipsis;
        }
        StylePopupButton(context.PurchaseFailCloseButton, "닫기", context.GoldButtonSprite, context.Font,
            new Vector2(0f, -150f));
    }

    private static void ApplyGachaStyle(Context context)
    {
        GameObject panel = context.GachaPanel;
        StyleImage(GetOrAdd<Image>(panel), context.CardSprite, CreamLight, Image.Type.Sliced);
        SetCentered(panel.GetComponent<RectTransform>(), new Vector2(1000f, 1260f), new Vector2(0f, -100f));

        Image gachaImage = Find(panel, "GachaImage")?.GetComponent<Image>();
        if (gachaImage != null)
        {
            SetCentered(gachaImage.rectTransform, new Vector2(620f, 570f), new Vector2(0f, 210f));
            gachaImage.preserveAspect = true;
        }
        StyleGachaButton(Find(panel, "OneGachaButton")?.GetComponent<Button>(), "1회 가챠\n500 G",
            context.GoldButtonSprite, context.Font, new Vector2(-235f, -275f), new Vector2(420f, 125f));
        StyleGachaButton(Find(panel, "TenGachaButton")?.GetComponent<Button>(), "10회 가챠\n5,000 G",
            context.GoldButtonSprite, context.Font, new Vector2(235f, -275f), new Vector2(420f, 125f));
        StyleGachaButton(Find(panel, "ProbabilityButton")?.GetComponent<Button>(), "확률 보기",
            context.CreamButtonSprite, context.Font, new Vector2(0f, -430f), new Vector2(420f, 100f));

        GameObject popup = Find(panel, "ProbabilityPopup")?.gameObject;
        if (popup != null)
        {
            StyleImage(GetOrAdd<Image>(popup), context.CardSprite, CreamLight, Image.Type.Sliced);
            SetCentered(popup.GetComponent<RectTransform>(), new Vector2(900f, 1120f), Vector2.zero);
            TMP_Text heading = Find(popup, "AchievementText")?.GetComponent<TMP_Text>();
            StylePopupText(heading, context.Font, 38f, Brown, FontStyles.Bold,
                new Vector2(760f, 100f), new Vector2(0f, 465f));
            Transform probabilityContent = Find(popup, "Content");
            if (probabilityContent != null)
            {
                VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(probabilityContent.gameObject);
                layout.padding = new RectOffset(18, 18, 18, 18);
                layout.spacing = 16f;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                SetCentered(probabilityContent.GetComponent<RectTransform>(), new Vector2(780f, 760f), new Vector2(0f, 5f));
                foreach (TMP_Text text in probabilityContent.GetComponentsInChildren<TMP_Text>(true))
                {
                    StyleText(text, null, context.Font, 30f, Brown, FontStyles.Bold);
                    text.enableWordWrapping = true;
                    text.overflowMode = TextOverflowModes.Ellipsis;
                }
            }
            StyleGachaButton(Find(popup, "CloseButton")?.GetComponent<Button>(), "닫기",
                context.GoldButtonSprite, context.Font, new Vector2(0f, -470f), new Vector2(340f, 105f));
        }
        if (context.GachaResultPopup != null && context.GachaResultGrid != null)
        {
            StyleImage(GetOrAdd<Image>(context.GachaResultPopup), context.CardSprite, CreamLight, Image.Type.Sliced);
            SetCentered(context.GachaResultPopup.GetComponent<RectTransform>(), new Vector2(960f, 1180f), Vector2.zero);
            context.GachaResultGrid.padding = new RectOffset(64, 64, 155, 155);
            context.GachaResultGrid.spacing = new Vector2(24f, 24f);
            context.GachaResultGrid.cellSize = new Vector2(190f, 240f);
            context.GachaResultGrid.childAlignment = TextAnchor.MiddleCenter;
            context.GachaResultGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            context.GachaResultGrid.constraintCount = 4;
            Image dimImage = context.GachaResultDim.GetComponent<Image>();
            if (dimImage != null) dimImage.color = new Color32(45, 28, 24, 180);
            Dirty(context.GachaResultPopup);
            Dirty(context.GachaResultDim);
        }
        Dirty(context.GachaRoot);
    }

    private static void StyleGachaButton(Button button, string label, Sprite sprite, TMP_FontAsset font,
        Vector2 position, Vector2 size)
    {
        if (button == null) return;
        StyleImage(button.GetComponent<Image>(), sprite, Color.white, Image.Type.Sliced);
        SetCentered(button.GetComponent<RectTransform>(), size, position);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        StyleText(text, label, font, 32f, Brown, FontStyles.Bold);
        if (text != null) SetStretch(text.rectTransform, new Vector2(18f, 8f), new Vector2(-18f, -8f));
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
        if (text == null) return;
        text.enableWordWrapping = true;
        text.alignment = TextAlignmentOptions.Center;
        SetCentered(text.rectTransform, rectSize, position);
    }

    private static void StylePopupButton(Button button, string label, Sprite sprite, TMP_FontAsset font,
        Vector2 position)
    {
        if (button == null) return;
        StyleImage(button.GetComponent<Image>(), sprite, Color.white, Image.Type.Sliced);
        SetCentered(button.GetComponent<RectTransform>(), new Vector2(340f, 110f), position);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        StyleText(text, label, font, 31f, Brown, FontStyles.Bold);
        if (text != null) SetStretch(text.rectTransform, new Vector2(18f, 10f), new Vector2(-18f, -10f));
    }

    private static void StyleExistingCard(GameObject card, Context context)
    {
        Image image = card.GetComponent<Image>();
        if (image != null)
        {
            StyleImage(image, context.CardSprite, Color.white, Image.Type.Sliced);
        }
        LayoutElement element = GetOrAdd<LayoutElement>(card);
        element.minHeight = 230f;
        element.preferredHeight = 230f;
        foreach (TMP_Text text in card.GetComponentsInChildren<TMP_Text>(true))
        {
            text.font = context.Font;
            bool isPrice = text.name.IndexOf("Price", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isDescription = text.name.IndexOf("Description", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isName = text.name.IndexOf("Name", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isStatus = text.name.IndexOf("Status", StringComparison.OrdinalIgnoreCase) >= 0;
            text.color = isPrice ? Gold : isDescription ? ReadableBrown : Brown;
            if (isName) text.fontSize = 36f;
            else if (isDescription) text.fontSize = 27f;
            else if (isPrice) text.fontSize = 30f;
            else if (isStatus) text.fontSize = 24f;
            text.fontStyle = isName || isPrice || isStatus ? FontStyles.Bold : FontStyles.Normal;
            if (isDescription) text.lineSpacing = 4f;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }
    }

    private static void EnsurePreviewRoot(Context context)
    {
        GameObject existing = FindRoot(context.Scene, PreviewRootName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        GameObject root = new GameObject(PreviewRootName, typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root, "Create Shop preview root");
        root.transform.SetAsLastSibling();
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 530;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 1f;

        Image background = CreateImage(root.transform, "PreviewBackground", null, Cream);
        SetStretch(background.rectTransform, Vector2.zero, Vector2.zero);
        TMP_Text heading = CreateText(background.transform, "PreviewHeading",
            "H. SHOP & GACHA RENEWAL · LOCAL PREVIEW", context.Font, 27f, MutedBrown, FontStyles.Bold);
        SetCentered(heading.rectTransform, new Vector2(980f, 44f), new Vector2(0f, 885f));

        Image topBar = CreateImage(background.transform, "TopBar", context.CardSprite, CreamLight);
        SetCentered(topBar.rectTransform, new Vector2(1000f, 190f), new Vector2(0f, 745f));
        TMP_Text title = CreateText(topBar.transform, "ShopText", "상점", context.Font, 52f, Brown, FontStyles.Bold);
        SetCentered(title.rectTransform, new Vector2(520f, 76f), new Vector2(-180f, 10f));
        TMP_Text gold = CreateText(topBar.transform, "GoldText", "12,450 G", context.Font, 38f, Brown, FontStyles.Bold);
        SetCentered(gold.rectTransform, new Vector2(360f, 76f), new Vector2(270f, 10f));
        Button lowBalance = CreateButton(topBar.transform, "LowBalanceTestButton", "저잔액 테스트",
            context.CreamButtonSprite, context.Font, new Vector2(355f, -58f), new Vector2(210f, 64f));
        lowBalance.GetComponentInChildren<TMP_Text>().fontSize = 23f;

        Button equipment = CreateButton(background.transform, "EquipmentButton", "장비 가챠",
            context.CreamButtonSprite, context.Font, new Vector2(-250f, 600f), new Vector2(450f, 108f));
        Button consumable = CreateButton(background.transform, "ConsumableButton", "소비 아이템",
            context.GoldButtonSprite, context.Font, new Vector2(250f, 600f), new Vector2(450f, 108f));

        Image scrollPanel = CreateImage(background.transform, "ConsumablePanel", context.CardSprite, CreamLight);
        SetCentered(scrollPanel.rectTransform, new Vector2(1000f, 1260f), new Vector2(0f, -100f));
        ScrollRect scroll = Undo.AddComponent<ScrollRect>(scrollPanel.gameObject);
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 35f;
        Image viewport = CreateImage(scrollPanel.transform, "Viewport", null, new Color32(255, 255, 255, 8));
        SetStretch(viewport.rectTransform, new Vector2(34f, 38f), new Vector2(-34f, -38f));
        Mask mask = Undo.AddComponent<Mask>(viewport.gameObject);
        mask.showMaskGraphic = false;
        RectTransform content = CreateContent(viewport.transform);
        scroll.viewport = viewport.rectTransform;
        scroll.content = content;

        string[,] products =
        {
            { "작은 회복 물약", "HP를 조금 회복하는 기본 물약", "120 G", "회" },
            { "집중의 차", "전투 준비 중 집중력을 높여주는 차", "180 G", "집" },
            { "불꽃 영약", "불 속성의 힘을 일시적으로 높인다", "350 G", "불" },
            { "맑은 샘물", "지친 모험가의 기운을 맑게 한다", "220 G", "샘" },
            { "행운 쿠키", "작은 행운이 담긴 바삭한 간식", "280 G", "행" },
            { "방어 비약", "전투 준비 중 방어 능력을 보조한다", "320 G", "방" },
            { "신속의 깃털", "가벼운 움직임을 돕는 마법 깃털", "400 G", "신" },
            { "별가루 병", "반짝이는 별가루를 담은 작은 병", "500 G", "별" }
        };
        List<Button> productButtons = new List<Button>();
        List<TMP_Text> productStatusTexts = new List<TMP_Text>();
        for (int index = 0; index < products.GetLength(0); index++)
        {
            Button productButton = CreateProductCard(content, products[index, 0], products[index, 1], products[index, 2],
                products[index, 3], context);
            productButtons.Add(productButton);
            productStatusTexts.Add(productButton.transform.Find("PurchaseStatus").GetComponent<TMP_Text>());
        }

        Image gacha = CreateImage(background.transform, "GachaPanel", context.CardSprite, CreamLight);
        SetCentered(gacha.rectTransform, new Vector2(1000f, 1260f), new Vector2(0f, -100f));
        TMP_Text gachaTitle = CreateText(gacha.transform, "Title", "장비 가챠", context.Font, 52f, Brown, FontStyles.Bold);
        SetCentered(gachaTitle.rectTransform, new Vector2(760f, 80f), new Vector2(0f, 505f));
        Image gachaImage = CreateImage(gacha.transform, "GachaImage", context.GoldButtonSprite, Color.white);
        SetCentered(gachaImage.rectTransform, new Vector2(600f, 480f), new Vector2(0f, 205f));
        TMP_Text gachaNotice = CreateText(gacha.transform, "Notice",
            "Preview 전용 가상 화면 · 실제 가챠/API 호출 없음", context.Font, 27f, ReadableBrown, FontStyles.Bold);
        gachaNotice.enableWordWrapping = true;
        gachaNotice.overflowMode = TextOverflowModes.Ellipsis;
        SetCentered(gachaNotice.rectTransform, new Vector2(760f, 70f), new Vector2(0f, -80f));
        Button oneGacha = CreateButton(gacha.transform, "OneGachaButton", "1회 가챠\n500 G",
            context.GoldButtonSprite, context.Font, new Vector2(-235f, -235f), new Vector2(420f, 125f));
        Button tenGacha = CreateButton(gacha.transform, "TenGachaButton", "10회 가챠\n5,000 G",
            context.GoldButtonSprite, context.Font, new Vector2(235f, -235f), new Vector2(420f, 125f));
        Button probability = CreateButton(gacha.transform, "ProbabilityButton", "확률 보기",
            context.CreamButtonSprite, context.Font, new Vector2(0f, -380f), new Vector2(420f, 100f));
        StyleGachaButton(oneGacha, "1회 가챠\n500 G", context.GoldButtonSprite, context.Font,
            new Vector2(-235f, -235f), new Vector2(420f, 125f));
        StyleGachaButton(tenGacha, "10회 가챠\n5,000 G", context.GoldButtonSprite, context.Font,
            new Vector2(235f, -235f), new Vector2(420f, 125f));
        StyleGachaButton(probability, "확률 보기", context.CreamButtonSprite, context.Font,
            new Vector2(0f, -380f), new Vector2(420f, 100f));
        TMP_Text gachaStatus = CreateText(gacha.transform, "InputStatus", "버튼을 눌러 로컬 입력을 확인하세요.",
            context.Font, 27f, Brown, FontStyles.Bold);
        SetCentered(gachaStatus.rectTransform, new Vector2(850f, 68f), new Vector2(0f, -485f));

        Image probabilityDim = CreateImage(background.transform, "ProbabilityPreviewDim", null, new Color32(45, 28, 24, 150));
        SetStretch(probabilityDim.rectTransform, Vector2.zero, Vector2.zero);
        Image probabilityPopup = CreateImage(probabilityDim.transform, "ProbabilityPopup", context.CardSprite, CreamLight);
        SetCentered(probabilityPopup.rectTransform, new Vector2(900f, 1120f), Vector2.zero);
        TMP_Text probabilityTitle = CreateText(probabilityPopup.transform, "AchievementText", "장비 가챠 확률 안내",
            context.Font, 42f, Brown, FontStyles.Bold);
        SetCentered(probabilityTitle.rectTransform, new Vector2(760f, 80f), new Vector2(0f, 475f));
        TMP_Text testNotice = CreateText(probabilityPopup.transform, "TestNotice",
            "아래 수치는 UI 확인용 테스트 데이터이며 실제 확률이 아닙니다.", context.Font, 27f, ReadableBrown, FontStyles.Bold);
        SetCentered(testNotice.rectTransform, new Vector2(760f, 90f), new Vector2(0f, 390f));
        Image probabilityContent = CreateImage(probabilityPopup.transform, "Content", null, new Color32(255, 255, 255, 0));
        SetCentered(probabilityContent.rectTransform, new Vector2(780f, 600f), new Vector2(0f, 5f));
        string[] previewProbabilities = { "전설 · 테스트 1%", "영웅 · 테스트 9%", "희귀 · 테스트 30%", "일반 · 테스트 60%" };
        for (int index = 0; index < previewProbabilities.Length; index++)
        {
            Image slot = CreateImage(probabilityContent.transform, "ProbSlot_" + (index + 1), context.CardSprite, Color.white);
            SetCentered(slot.rectTransform, new Vector2(740f, 130f), new Vector2(0f, 220f - index * 145f));
            TMP_Text slotText = CreateText(slot.transform, "Label", previewProbabilities[index], context.Font, 31f, Brown, FontStyles.Bold);
            SetStretch(slotText.rectTransform, new Vector2(24f, 12f), new Vector2(-24f, -12f));
        }
        Button probabilityClose = CreateButton(probabilityPopup.transform, "CloseButton", "닫기",
            context.GoldButtonSprite, context.Font, new Vector2(0f, -475f), new Vector2(340f, 105f));
        StyleGachaButton(probabilityClose, "닫기", context.GoldButtonSprite, context.Font,
            new Vector2(0f, -475f), new Vector2(340f, 105f));
        probabilityDim.gameObject.SetActive(false);
        gacha.gameObject.SetActive(false);

        Image resultDim = CreateImage(background.transform, "GachaResultPreviewDim", null, new Color32(45, 28, 24, 180));
        SetStretch(resultDim.rectTransform, Vector2.zero, Vector2.zero);
        Button resultDimButton = Undo.AddComponent<Button>(resultDim.gameObject);
        resultDimButton.targetGraphic = resultDim;
        resultDimButton.transition = Selectable.Transition.None;
        Image resultPopup = CreateImage(resultDim.transform, "ResultPopup", context.CardSprite, CreamLight);
        SetCentered(resultPopup.rectTransform, new Vector2(980f, 1250f), Vector2.zero);
        Button resultPopupInputBlocker = Undo.AddComponent<Button>(resultPopup.gameObject);
        resultPopupInputBlocker.targetGraphic = resultPopup;
        resultPopupInputBlocker.transition = Selectable.Transition.None;
        TMP_Text resultTitle = CreateText(resultPopup.transform, "Title", "가챠 결과", context.Font, 50f, Brown, FontStyles.Bold);
        SetCentered(resultTitle.rectTransform, new Vector2(780f, 80f), new Vector2(0f, 520f));
        TMP_Text resultNotice = CreateText(resultPopup.transform, "Notice", "Preview 전용 가상 장비 · 실제 지급/확률과 무관", context.Font, 26f, ReadableBrown, FontStyles.Bold);
        SetCentered(resultNotice.rectTransform, new Vector2(820f, 60f), new Vector2(0f, 455f));
        Image resultContent = CreateImage(resultPopup.transform, "ResultContent", null, new Color32(255, 255, 255, 0));
        SetCentered(resultContent.rectTransform, new Vector2(884f, 720f), new Vector2(0f, 35f));
        GridLayoutGroup resultGrid = Undo.AddComponent<GridLayoutGroup>(resultContent.gameObject);
        resultGrid.padding = new RectOffset(0, 0, 0, 0);
        resultGrid.spacing = new Vector2(16f, 22f);
        resultGrid.cellSize = new Vector2(164f, 330f);
        resultGrid.childAlignment = TextAnchor.MiddleCenter;
        resultGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        resultGrid.constraintCount = 5;
        GameObject[] resultCards = new GameObject[10];
        TMP_Text[] resultIcons = new TMP_Text[10];
        TMP_Text[] resultNames = new TMP_Text[10];
        TMP_Text[] resultTiers = new TMP_Text[10];
        for (int index = 0; index < resultCards.Length; index++)
        {
            Image card = CreateImage(resultContent.transform, "ResultCard_" + (index + 1), context.GoldButtonSprite, Color.white);
            LayoutElement cardLayout = Undo.AddComponent<LayoutElement>(card.gameObject);
            cardLayout.preferredWidth = 164f;
            cardLayout.preferredHeight = 330f;
            Image iconFrame = CreateImage(card.transform, "EquipmentImageFrame", context.CardSprite, Cream);
            SetCentered(iconFrame.rectTransform, new Vector2(128f, 145f), new Vector2(0f, 62f));
            TMP_Text icon = CreateText(iconFrame.transform, "IconPlaceholder", "검", context.Font, 54f, Brown, FontStyles.Bold);
            SetStretch(icon.rectTransform, new Vector2(10f, 10f), new Vector2(-10f, -10f));
            TMP_Text itemName = CreateText(card.transform, "EquipmentName", "훈련용 목검", context.Font, 23f, Brown, FontStyles.Bold);
            itemName.enableWordWrapping = true;
            itemName.overflowMode = TextOverflowModes.Ellipsis;
            SetCentered(itemName.rectTransform, new Vector2(142f, 72f), new Vector2(0f, -58f));
            TMP_Text tier = CreateText(card.transform, "Tier", "일반 등급", context.Font, 22f, ReadableBrown, FontStyles.Bold);
            SetCentered(tier.rectTransform, new Vector2(142f, 44f), new Vector2(0f, -118f));
            resultCards[index] = card.gameObject;
            resultIcons[index] = icon;
            resultNames[index] = itemName;
            resultTiers[index] = tier;
        }
        Button resultClose = CreateButton(resultPopup.transform, "CloseButton", "결과 닫기",
            context.GoldButtonSprite, context.Font, new Vector2(0f, -530f), new Vector2(360f, 105f));
        StyleGachaButton(resultClose, "결과 닫기", context.GoldButtonSprite, context.Font,
            new Vector2(0f, -530f), new Vector2(360f, 105f));
        resultDim.gameObject.SetActive(false);

        Image detailDim = CreateImage(background.transform, "ShopItemDetailDim", null, new Color32(45, 28, 24, 150));
        SetStretch(detailDim.rectTransform, Vector2.zero, Vector2.zero);
        Image detail = CreateImage(detailDim.transform, "ShopItemDetailPopup", context.CardSprite, CreamLight);
        SetCentered(detail.rectTransform, new Vector2(900f, 960f), Vector2.zero);
        Image detailIconFrame = CreateImage(detail.transform, "ItemIconFrame", context.GoldButtonSprite, Color.white);
        SetCentered(detailIconFrame.rectTransform, new Vector2(240f, 240f), new Vector2(0f, 265f));
        TMP_Text detailIcon = CreateText(detailIconFrame.transform, "IconText", "회", context.Font, 72f, Brown, FontStyles.Bold);
        detailIcon.overflowMode = TextOverflowModes.Ellipsis;
        SetStretch(detailIcon.rectTransform, new Vector2(20f, 20f), new Vector2(-20f, -20f));
        TMP_Text detailName = CreateText(detail.transform, "ItemName", "작은 회복 물약", context.Font, 46f, Brown, FontStyles.Bold);
        SetCentered(detailName.rectTransform, new Vector2(720f, 80f), new Vector2(0f, 95f));
        TMP_Text detailDescription = CreateText(detail.transform, "Description", "HP를 조금 회복하는 기본 물약", context.Font, 31f, ReadableBrown, FontStyles.Normal);
        detailDescription.enableWordWrapping = true;
        detailDescription.overflowMode = TextOverflowModes.Ellipsis;
        detailDescription.lineSpacing = 4f;
        SetCentered(detailDescription.rectTransform, new Vector2(740f, 190f), new Vector2(0f, -45f));
        TMP_Text detailPrice = CreateText(detail.transform, "Price", "120 G", context.Font, 39f, Gold, FontStyles.Bold);
        SetCentered(detailPrice.rectTransform, new Vector2(330f, 76f), new Vector2(0f, -190f));
        TMP_Text detailStatus = CreateText(detail.transform, "PurchaseStatus", "구매 가능 · Preview 로컬 테스트", context.Font, 29f, ReadableBrown, FontStyles.Bold);
        SetCentered(detailStatus.rectTransform, new Vector2(740f, 76f), new Vector2(0f, -275f));
        Button buy = CreateButton(detail.transform, "BuyButton", "구매하기", context.GoldButtonSprite, context.Font,
            new Vector2(-205f, -385f), new Vector2(340f, 110f));
        Button detailClose = CreateButton(detail.transform, "CloseButton", "닫기", context.CreamButtonSprite, context.Font,
            new Vector2(205f, -385f), new Vector2(340f, 110f));
        detailDim.gameObject.SetActive(false);

        Image failDim = CreateImage(background.transform, "PurchaseFailDim", null, new Color32(45, 28, 24, 150));
        SetStretch(failDim.rectTransform, Vector2.zero, Vector2.zero);
        Image fail = CreateImage(failDim.transform, "PurchaseFailPopup", context.CardSprite, CreamLight);
        SetCentered(fail.rectTransform, new Vector2(820f, 520f), Vector2.zero);
        TMP_Text failTitle = CreateText(fail.transform, "Title", "구매할 수 없습니다", context.Font, 40f, Brown, FontStyles.Bold);
        SetCentered(failTitle.rectTransform, new Vector2(680f, 72f), new Vector2(0f, 145f));
        TMP_Text failMessage = CreateText(fail.transform, "Message", "보유 골드가 부족합니다!\n다른 상품을 선택하거나 골드를 확인해주세요.", context.Font, 33f, AlertBrown, FontStyles.Bold);
        failMessage.enableWordWrapping = true;
        failMessage.overflowMode = TextOverflowModes.Ellipsis;
        SetCentered(failMessage.rectTransform, new Vector2(680f, 160f), new Vector2(0f, 25f));
        Button failClose = CreateButton(fail.transform, "CloseButton", "닫기", context.GoldButtonSprite, context.Font,
            new Vector2(0f, -150f), new Vector2(340f, 110f));
        failDim.gameObject.SetActive(false);

        ShopGachaPreviewController controller = Undo.AddComponent<ShopGachaPreviewController>(root);
        SerializedObject serialized = new SerializedObject(controller);
        SetReference(serialized, "consumableButton", consumable);
        SetReference(serialized, "equipmentGachaButton", equipment);
        SetReference(serialized, "consumableButtonImage", consumable.GetComponent<Image>());
        SetReference(serialized, "equipmentGachaButtonImage", equipment.GetComponent<Image>());
        SetReference(serialized, "consumableButtonText", consumable.GetComponentInChildren<TMP_Text>());
        SetReference(serialized, "equipmentGachaButtonText", equipment.GetComponentInChildren<TMP_Text>());
        SetReference(serialized, "consumablePanel", scrollPanel.gameObject);
        SetReference(serialized, "gachaPanel", gacha.gameObject);
        SetReference(serialized, "oneGachaButton", oneGacha);
        SetReference(serialized, "tenGachaButton", tenGacha);
        SetReference(serialized, "probabilityButton", probability);
        SetReference(serialized, "probabilityPopup", probabilityDim.gameObject);
        SetReference(serialized, "probabilityCloseButton", probabilityClose);
        SetReference(serialized, "gachaInputStatusText", gachaStatus);
        SetReference(serialized, "gachaResultPopup", resultDim.gameObject);
        SetReference(serialized, "gachaResultDimButton", resultDimButton);
        SetReference(serialized, "gachaResultCloseButton", resultClose);
        SetReference(serialized, "gachaResultGrid", resultGrid);
        SetArray(serialized, "gachaResultCards", resultCards);
        SetArray(serialized, "gachaResultIconTexts", resultIcons);
        SetArray(serialized, "gachaResultNameTexts", resultNames);
        SetArray(serialized, "gachaResultTierTexts", resultTiers);
        SetReference(serialized, "consumableScroll", scroll);
        SetReference(serialized, "goldText", gold);
        SetReference(serialized, "lowBalanceButton", lowBalance);
        SetArray(serialized, "productButtons", productButtons.ToArray());
        SetArray(serialized, "productStatusTexts", productStatusTexts.ToArray());
        SetReference(serialized, "detailPopup", detailDim.gameObject);
        SetReference(serialized, "detailIconText", detailIcon);
        SetReference(serialized, "detailNameText", detailName);
        SetReference(serialized, "detailDescriptionText", detailDescription);
        SetReference(serialized, "detailPriceText", detailPrice);
        SetReference(serialized, "detailPurchaseStatusText", detailStatus);
        SetReference(serialized, "buyButton", buy);
        SetReference(serialized, "detailCloseButton", detailClose);
        SetReference(serialized, "purchaseFailPopup", failDim.gameObject);
        SetReference(serialized, "purchaseFailCloseButton", failClose);
        SetReference(serialized, "selectedTabSprite", context.GoldButtonSprite);
        SetReference(serialized, "normalTabSprite", context.CreamButtonSprite);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        // H Preview가 활성 상태일 때 카드 목록의 실제 레이아웃 높이를 확정한다.
        // 비활성화 후에는 Unity 레이아웃 계산이 생략될 수 있다.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        float preferredContentHeight = LayoutUtility.GetPreferredHeight(content);
        if (preferredContentHeight > 0f)
        {
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredContentHeight);
        }

        root.SetActive(false);
        Dirty(root);
    }

    private static Button CreateProductCard(Transform parent, string itemName, string description,
        string price, string icon, Context context)
    {
        GameObject cardObject = new GameObject("ProductCard", typeof(RectTransform), typeof(Image),
            typeof(Button), typeof(LayoutElement));
        Undo.RegisterCreatedObjectUndo(cardObject, "Create Shop preview product card");
        cardObject.transform.SetParent(parent, false);
        Image card = cardObject.GetComponent<Image>();
        card.sprite = context.CardSprite;
        card.color = Color.white;
        card.type = Image.Type.Sliced;
        Button button = cardObject.GetComponent<Button>();
        button.targetGraphic = card;
        LayoutElement element = cardObject.GetComponent<LayoutElement>();
        element.minHeight = 230f;
        element.preferredHeight = 230f;
        Image iconFrame = CreateImage(card.transform, "IconFrame", context.GoldButtonSprite, Color.white);
        SetCentered(iconFrame.rectTransform, new Vector2(160f, 160f), new Vector2(-365f, 0f));
        TMP_Text iconText = CreateText(iconFrame.transform, "IconText", icon, context.Font, 54f, Brown, FontStyles.Bold);
        iconText.overflowMode = TextOverflowModes.Ellipsis;
        SetStretch(iconText.rectTransform, new Vector2(16f, 16f), new Vector2(-16f, -16f));
        TMP_Text name = CreateText(card.transform, "ItemName", itemName, context.Font, 36f, Brown, FontStyles.Bold);
        name.alignment = TextAlignmentOptions.Left;
        name.enableWordWrapping = false;
        name.overflowMode = TextOverflowModes.Ellipsis;
        SetCentered(name.rectTransform, new Vector2(520f, 56f), new Vector2(0f, 52f));
        TMP_Text desc = CreateText(card.transform, "Description", description, context.Font, 27f, ReadableBrown, FontStyles.Normal);
        desc.alignment = TextAlignmentOptions.TopLeft;
        desc.enableWordWrapping = true;
        desc.overflowMode = TextOverflowModes.Ellipsis;
        desc.maxVisibleLines = 2;
        desc.lineSpacing = 4f;
        SetCentered(desc.rectTransform, new Vector2(520f, 90f), new Vector2(0f, -32f));
        Image badge = CreateImage(card.transform, "PriceBadge", context.GoldButtonSprite, Color.white);
        SetCentered(badge.rectTransform, new Vector2(190f, 92f), new Vector2(355f, 0f));
        TMP_Text priceText = CreateText(badge.transform, "PriceText", price, context.Font, 30f, Brown, FontStyles.Bold);
        priceText.enableWordWrapping = false;
        priceText.overflowMode = TextOverflowModes.Ellipsis;
        SetStretch(priceText.rectTransform, new Vector2(12f, 8f), new Vector2(-12f, -8f));
        TMP_Text status = CreateText(card.transform, "PurchaseStatus", string.Empty, context.Font, 24f, Gold, FontStyles.Bold);
        status.enableWordWrapping = false;
        status.overflowMode = TextOverflowModes.Ellipsis;
        SetCentered(status.rectTransform, new Vector2(190f, 38f), new Vector2(355f, -72f));
        return button;
    }

    private static RectTransform CreateContent(Transform parent)
    {
        GameObject item = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        Undo.RegisterCreatedObjectUndo(item, "Create Shop preview content");
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = item.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 28);
        layout.spacing = 24f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = item.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rect;
    }

    private static List<string> ValidateAppliedLayout(Context context, bool requirePreview)
    {
        List<string> errors = new List<string>();
        if (!context.IsValid)
        {
            errors.AddRange(context.Errors);
            return errors;
        }
        if (context.GoldText.font != context.Font || context.ShopText.font != context.Font)
        {
            errors.Add("ShopTap 상단 텍스트에 NotoSansKR TMP Font가 적용되지 않았습니다.");
        }
        if (!context.ShopScroll.vertical || context.ShopScroll.horizontal)
        {
            errors.Add("ShopScroll이 세로 전용 ScrollRect가 아닙니다.");
        }
        GameObject preview = FindRoot(context.Scene, PreviewRootName);
        if (requirePreview && (preview == null || preview.GetComponent<ShopGachaPreviewController>() == null))
        {
            errors.Add("H Preview Root/Controller가 없습니다.");
        }
        if (preview != null)
        {
            RectTransform content = preview.transform.Find("PreviewBackground/ConsumablePanel/Viewport/Content") as RectTransform;
            if (content == null || content.childCount < 8)
            {
                errors.Add("H Preview 소비 상품 카드가 8개가 아닙니다.");
            }
            else
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                if (content.rect.height <= 1200f)
                {
                    errors.Add("H Preview Content 높이가 Viewport보다 작아 실제 스크롤 범위가 없습니다.");
                }
                foreach (TMP_Text text in content.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.font != context.Font || text.overflowMode != TextOverflowModes.Ellipsis)
                    {
                        errors.Add("H Preview 카드 텍스트 Font/Overflow 설정이 올바르지 않습니다: " + text.name);
                    }
                }
                Button[] productButtons = content.Cast<Transform>()
                    .Select(card => card.GetComponent<Button>()).ToArray();
                if (productButtons.Length != 8 || productButtons.Any(button => button == null))
                {
                    errors.Add("H Preview 소비 상품 카드 Button이 8개 모두 생성되지 않았습니다.");
                }

                ShopGachaPreviewController controller = preview.GetComponent<ShopGachaPreviewController>();
                SerializedProperty serializedButtons = controller != null
                    ? new SerializedObject(controller).FindProperty("productButtons")
                    : null;
                if (serializedButtons == null || serializedButtons.arraySize != 8 ||
                    Enumerable.Range(0, serializedButtons != null ? serializedButtons.arraySize : 0)
                        .Any(index => serializedButtons.GetArrayElementAtIndex(index).objectReferenceValue == null))
                {
                    errors.Add("H Preview Controller에 상품 카드 Button 8개가 연결되지 않았습니다.");
                }
            }
            Transform detail = preview.transform.Find("PreviewBackground/ShopItemDetailDim/ShopItemDetailPopup");
            Transform fail = preview.transform.Find("PreviewBackground/PurchaseFailDim/PurchaseFailPopup");
            Transform gacha = preview.transform.Find("PreviewBackground/GachaPanel");
            Transform probability = preview.transform.Find("PreviewBackground/ProbabilityPreviewDim/ProbabilityPopup");
            Transform result = preview.transform.Find("PreviewBackground/GachaResultPreviewDim/ResultPopup");
            if (detail == null || detail.Find("BuyButton") == null || detail.Find("CloseButton") == null)
            {
                errors.Add("H Preview 상품 상세 팝업 구조가 누락되었습니다.");
            }
            if (fail == null || fail.Find("Message") == null || fail.Find("CloseButton") == null)
            {
                errors.Add("H Preview 구매 실패 팝업 구조가 누락되었습니다.");
            }
            if (gacha == null || gacha.Find("OneGachaButton") == null || gacha.Find("TenGachaButton") == null ||
                gacha.Find("ProbabilityButton") == null)
            {
                errors.Add("H Preview 장비 가챠 버튼 구조가 누락되었습니다.");
            }
            if (probability == null || probability.Find("Content/ProbSlot_1") == null ||
                probability.Find("Content/ProbSlot_4") == null ||
                probability.Find("CloseButton") == null)
            {
                errors.Add("H Preview 테스트 확률 팝업 구조가 누락되었습니다.");
            }
            if (result == null || result.Find("ResultContent") == null ||
                result.Find("ResultContent").childCount != 10 || result.Find("CloseButton") == null)
            {
                errors.Add("H Preview 가챠 결과 팝업 또는 결과 카드 10개가 누락되었습니다.");
            }
        }
        return errors;
    }

    private static void IsolateScene(Scene scene, GameObject previewRoot,
        ShopGachaPreviewController controller)
    {
        GameObject[] objects = scene.GetRootGameObjects()
            .Where(root => root != previewRoot && !IsPreviewRoot(root) && root.activeSelf &&
                           root.GetComponent<EventSystem>() == null &&
                           root.GetComponentInChildren<Camera>(true) == null &&
                           root.GetComponentInChildren<AudioListener>(true) == null)
            .OrderBy(root => root.GetInstanceID()).ToArray();
        bool[] objectStates = objects.Select(item => item.activeSelf).ToArray();
        Behaviour[] behaviours = Array.Empty<Behaviour>();
        bool[] behaviourStates = Array.Empty<bool>();
        SerializedObject serialized = new SerializedObject(controller);
        SetArray(serialized, "isolatedGameObjects", objects);
        SetBoolArray(serialized, "isolatedGameObjectActiveStates", objectStates);
        SetArray(serialized, "isolatedBehaviours", behaviours);
        SetBoolArray(serialized, "isolatedBehaviourEnabledStates", behaviourStates);
        serialized.ApplyModifiedProperties();
        Dirty(controller);
        foreach (GameObject target in objects)
        {
            SetActive(target, false, "Isolate scene object for Shop preview");
        }
    }

    public static void RestoreShopPreviewState()
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
        ShopGachaPreviewController controller =
            root != null ? root.GetComponent<ShopGachaPreviewController>() : null;
        if (root == null || controller == null || (!root.activeSelf && !HasSnapshot(controller)))
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty targets = serialized.FindProperty("isolatedGameObjects");
        SerializedProperty states = serialized.FindProperty("isolatedGameObjectActiveStates");
        if (targets != null && states != null && targets.arraySize == states.arraySize)
        {
            for (int index = 0; index < targets.arraySize; index++)
            {
                GameObject target = targets.GetArrayElementAtIndex(index).objectReferenceValue as GameObject;
                if (target != null)
                {
                    SetActive(target, states.GetArrayElementAtIndex(index).boolValue,
                        "Restore Shop preview state");
                }
            }
        }
        ClearSnapshot(controller);
        SetActive(root, false, "Restore Shop preview root");
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static bool HasSnapshot(ShopGachaPreviewController controller)
    {
        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty objects = serialized.FindProperty("isolatedGameObjects");
        return objects != null && objects.arraySize > 0;
    }

    private static void ClearSnapshot(ShopGachaPreviewController controller)
    {
        SerializedObject serialized = new SerializedObject(controller);
        foreach (string name in new[] { "isolatedBehaviours", "isolatedBehaviourEnabledStates", "isolatedGameObjects", "isolatedGameObjectActiveStates" })
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

    private static void RestoreWhenAnotherPreviewIsSelected()
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
        bool otherActive = OtherPreviewRoots.Any(name =>
        {
            GameObject other = FindRoot(scene, name);
            return other != null && other.activeSelf;
        });
        if (otherActive)
        {
            RestoreShopPreviewState();
        }
    }

    private static void RestoreBeforeSceneSave(Scene scene, string path)
    {
        if (scene.path == ScenePath && scene == SceneManager.GetActiveScene())
        {
            RestoreShopPreviewState();
        }
    }

    private static void StyleTab(Button button, string label, Sprite sprite, TMP_FontAsset font, float x)
    {
        if (button == null)
        {
            return;
        }
        StyleImage(button.GetComponent<Image>(), sprite, Color.white, Image.Type.Sliced);
        SetCentered(button.GetComponent<RectTransform>(), new Vector2(450f, 108f), new Vector2(x, 0f));
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        StyleText(text, label, font, 31f, Brown, FontStyles.Bold);
        if (text != null)
        {
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            SetStretch(text.rectTransform, new Vector2(18f, 10f), new Vector2(-18f, -10f));
        }
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(item, "Create Shop preview image");
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
        Undo.RegisterCreatedObjectUndo(item, "Create Shop preview text");
        item.transform.SetParent(parent, false);
        TMP_Text text = item.GetComponent<TMP_Text>();
        text.text = value;
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, Sprite sprite,
        TMP_FontAsset font, Vector2 position, Vector2 size)
    {
        Image image = CreateImage(parent, name, sprite, Color.white);
        Button button = Undo.AddComponent<Button>(image.gameObject);
        SetCentered(image.rectTransform, size, position);
        TMP_Text text = CreateText(image.transform, "Label", label, font, 31f, Brown, FontStyles.Bold);
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        SetStretch(text.rectTransform, new Vector2(18f, 10f), new Vector2(-18f, -10f));
        return button;
    }

    private static void StyleImage(Image image, Sprite sprite, Color color, Image.Type type)
    {
        if (image == null)
        {
            return;
        }
        Undo.RecordObject(image, "Style Shop image");
        image.sprite = sprite;
        image.color = color;
        image.type = type;
        Dirty(image);
    }

    private static void StyleText(TMP_Text text, string value, TMP_FontAsset font, float size,
        Color color, FontStyles style)
    {
        if (text == null)
        {
            return;
        }
        Undo.RecordObject(text, "Style Shop text");
        if (value != null)
        {
            text.text = value;
        }
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.overflowMode = TextOverflowModes.Ellipsis;
        Dirty(text);
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static void SetCentered(RectTransform rect, Vector2 size, Vector2 position)
    {
        if (rect == null) return;
        Undo.RecordObject(rect, "Place Shop UI");
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Dirty(rect);
    }

    private static void SetTop(RectTransform rect, float height, float top, float side)
    {
        if (rect == null) return;
        Undo.RecordObject(rect, "Place Shop UI");
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(side, -top - height);
        rect.offsetMax = new Vector2(-side, -top);
        Dirty(rect);
    }

    private static void SetStretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        if (rect == null) return;
        Undo.RecordObject(rect, "Place Shop UI");
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = min;
        rect.offsetMax = max;
        Dirty(rect);
    }

    private static void SetReference(SerializedObject serialized, string name, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null) property.objectReferenceValue = value;
    }

    private static T GetReference<T>(SerializedObject serialized, string name) where T : UnityEngine.Object
    {
        SerializedProperty property = serialized.FindProperty(name);
        return property != null ? property.objectReferenceValue as T : null;
    }

    private static void SetArray<T>(SerializedObject serialized, string name, T[] values)
        where T : UnityEngine.Object
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null) return;
        property.arraySize = values.Length;
        for (int index = 0; index < values.Length; index++)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
    }

    private static void SetBoolArray(SerializedObject serialized, string name, bool[] values)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null) return;
        property.arraySize = values.Length;
        for (int index = 0; index < values.Length; index++)
        {
            property.GetArrayElementAtIndex(index).boolValue = values[index];
        }
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene.IsValid() ? scene.GetRootGameObjects().FirstOrDefault(item => item.name == name) : null;
    }

    private static Transform Find(GameObject root, string name)
    {
        return root != null ? root.transform.Find(name) : null;
    }

    private static Transform Find(Transform root, string name)
    {
        return root != null ? root.Find(name) : null;
    }

    private static bool IsPreviewRoot(GameObject root)
    {
        return root != null && (root.name == PreviewRootName || OtherPreviewRoots.Contains(root.name));
    }

    private static void SetActive(GameObject target, bool active, string label)
    {
        if (target == null || target.activeSelf == active) return;
        Undo.RecordObject(target, label);
        target.SetActive(active);
        Dirty(target);
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private static void Dirty(UnityEngine.Object target)
    {
        if (target != null) EditorUtility.SetDirty(target);
    }

    private sealed class Context
    {
        public Scene Scene;
        public GameObject Canvas;
        public GameObject ShopTap;
        public GameObject TopBar;
        public TMP_Text GoldText;
        public TMP_Text ShopText;
        public GameObject TopButtons;
        public Button EquipmentButton;
        public Button ConsumableButton;
        public ScrollRect ShopScroll;
        public GameObject Viewport;
        public RectTransform Content;
        public ShopUIManager ShopManager;
        public GameObject GachaRoot;
        public GameObject GachaPanel;
        public GachaManager GachaManager;
        public GachaResultPopupManager GachaResultManager;
        public GameObject GachaResultPopup;
        public GameObject GachaResultDim;
        public RectTransform GachaResultContainer;
        public GridLayoutGroup GachaResultGrid;
        public GameObject ItemDetailPopup;
        public Image ItemDetailIcon;
        public TMP_Text ItemDetailName;
        public TMP_Text ItemDetailDescription;
        public TMP_Text ItemDetailPrice;
        public TMP_Text ItemDetailStatus;
        public Button BuyButton;
        public Button CloseButton;
        public GameObject PurchaseFailPopup;
        public TMP_Text PurchaseFailText;
        public Button PurchaseFailCloseButton;
        public TMP_FontAsset Font;
        public Sprite CardSprite;
        public Sprite CreamButtonSprite;
        public Sprite GoldButtonSprite;
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }
}
