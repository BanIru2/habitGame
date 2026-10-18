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
public static class PvpRankingUiRenewalEditor
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
    private const string PreviewRootName = "PvpRankingPreviewRoot";
    private const string ValidateMenu =
        "Tools/HabitPVP/UI Renewal/Validate PvP Lobby & Ranking Renewal";
    private const string ApplyMenu =
        "Tools/HabitPVP/UI Renewal/Apply PvP Lobby & Ranking Renewal";
    private const string PreviewMenu =
        "Tools/HabitPVP/UI Renewal/Preview Mode/I. PvP Lobby & Ranking Renewal";

    private static readonly string[] OtherPreviewRoots =
    {
        "HabitItemPreviewTestRoot", "NumberOverlayPreviewRoot", "PhotoVerificationPreviewRoot",
        "CharacterMainPreviewRoot", "InventoryMainPreviewRoot", "ShopGachaPreviewRoot"
    };

    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 CreamLight = new Color32(0xFF, 0xF9, 0xEA, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x86, 0x68, 0x5D, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);
    private static readonly Color32 Alert = new Color32(0xA8, 0x3F, 0x35, 0xFF);
    private static readonly Color32 Dim = new Color32(0x2D, 0x1C, 0x18, 0xA0);

    static PvpRankingUiRenewalEditor()
    {
        EditorApplication.update -= RestoreWhenAnotherPreviewIsSelected;
        EditorApplication.update += RestoreWhenAnotherPreviewIsSelected;
        EditorSceneManager.sceneSaving -= RestoreBeforeSceneSave;
        EditorSceneManager.sceneSaving += RestoreBeforeSceneSave;
    }

    [MenuItem(ValidateMenu, false, 2100)]
    private static void Validate()
    {
        Context context = ValidateScene(true);
        if (!context.IsValid) return;

        List<string> errors = ValidateAppliedLayout(context, true);
        if (errors.Count == 0)
        {
            Debug.Log("[HabitPVP PvP] 검증 성공. UITestScene PvP Lobby/Ranking/Loading/Ready/Battle HUD와 " +
                      "장비 선택/상세 팝업 스타일, I Preview 로컬 흐름이 정상입니다.", context.Lobby);
            return;
        }

        foreach (string error in errors)
        {
            Debug.LogError("[HabitPVP PvP] " + error, context.Lobby);
        }
    }

    [MenuItem(ApplyMenu, false, 2101)]
    private static void Apply()
    {
        Context context = ValidateScene(true);
        if (!context.IsValid) return;

        RestorePvpPreviewState();
        Dictionary<MonoBehaviour, string> protectedState = CaptureProtectedManagers();
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply PvP Lobby & Ranking UI Renewal");

        try
        {
            ApplyLobbyStyle(context);
            ApplyLoadingStyle(context);
            ApplyReadyStyle(context);
            ApplyEquipmentPopupStyle(context);
            ApplyBattleStyle(context);
            EnsurePreviewRoot(context);
            AssertProtectedManagersUnchanged(protectedState);

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

            Debug.Log("[HabitPVP PvP] 적용 성공. UITestScene PvPTap/Lobby/Loading/Ready/Battle HUD Override와 " +
                      "I Preview Root만 저장했습니다. Canvas.prefab, MainScene, RankingRow.prefab 및 " +
                      "PvP 런타임 로직은 변경하지 않았습니다.", context.Lobby);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(group);
            Debug.LogError("[HabitPVP PvP] 적용 실패. 이번 변경을 Undo로 되돌렸습니다.\n" + exception);
        }
    }

    [MenuItem(PreviewMenu, false, 2102)]
    private static void ActivatePreview()
    {
        Context context = ValidateScene(false);
        if (!context.IsValid) return;

        RestoreOtherPreviewStates();
        GameObject root = FindRoot(context.Scene, PreviewRootName);
        PvpRankingPreviewController controller =
            root != null ? root.GetComponent<PvpRankingPreviewController>() : null;
        if (root == null || controller == null)
        {
            Debug.LogError("[HabitPVP PvP] I Preview가 없습니다. Apply PvP Lobby & Ranking Renewal을 먼저 실행해주세요.");
            return;
        }

        foreach (string rootName in OtherPreviewRoots)
        {
            SetActive(FindRoot(context.Scene, rootName), false, "Hide other preview for I mode");
        }

        IsolateScene(context.Scene, root, controller);
        SetActive(root, true, "Activate PvP ranking preview");
        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(context.Scene);
        Debug.Log("[HabitPVP PvP] I 모드: 가상 랭킹 20명, 잔여 횟수 0, 제한 팝업과 " +
                  "로컬 Lobby→Loading→Ready→장비 목록→상세→Battle HUD/HP/턴/타이머 흐름을 표시합니다. " +
                  "Photon/PvP Manager/API는 호출하지 않습니다.", root);
    }

    [MenuItem(ValidateMenu, true)]
    [MenuItem(ApplyMenu, true)]
    [MenuItem(PreviewMenu, true)]
    private static bool CanUseMenus()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
               SceneManager.GetActiveScene().path == ScenePath;
    }

    private static Context ValidateScene(bool logErrors)
    {
        Context context = new Context { Scene = SceneManager.GetActiveScene() };
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            context.Errors.Add("Play Mode를 종료한 뒤 실행해주세요.");
        }
        if (context.Scene.path != ScenePath)
        {
            context.Errors.Add($"UITestScene에서만 실행할 수 있습니다. 현재: '{context.Scene.path}'");
        }

        GameObject[] canvases = context.Scene.IsValid()
            ? context.Scene.GetRootGameObjects().Where(item => item.name == "Canvas").ToArray()
            : Array.Empty<GameObject>();
        if (canvases.Length != 1)
        {
            context.Errors.Add($"Scene 루트 Canvas가 정확히 하나여야 합니다. 현재: {canvases.Length}");
        }
        else
        {
            context.Canvas = canvases[0];
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(context.Canvas);
            if (prefabPath != CanvasPrefabPath)
            {
                context.Errors.Add($"Canvas 원본 Prefab 경로가 다릅니다: '{prefabPath}'");
            }
        }

        context.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        context.CardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardSpritePath);
        context.CreamButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CreamButtonSpritePath);
        context.GoldButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GoldButtonSpritePath);
        if (context.Font == null) context.Errors.Add("NotoSansKR TMP Font Asset을 찾을 수 없습니다.");
        if (context.CardSprite == null) context.Errors.Add("RPG 카드 Sprite를 찾을 수 없습니다.");
        if (context.CreamButtonSprite == null) context.Errors.Add("크림 버튼 Sprite를 찾을 수 없습니다.");
        if (context.GoldButtonSprite == null) context.Errors.Add("골드 버튼 Sprite를 찾을 수 없습니다.");

        if (context.Canvas != null)
        {
            context.Lobby = Find(context.Canvas.transform, "PvPTap/Lobby")?.gameObject;
            context.RankingTitle = Get<TMP_Text>(context.Canvas.transform,
                "PvPTap/Lobby/RankingTitleText", context.Errors);
            context.RankingList = Get<ScrollRect>(context.Canvas.transform,
                "PvPTap/Lobby/RankingList", context.Errors);
            context.RankingContent = Find(context.Canvas.transform,
                "PvPTap/Lobby/RankingList/Viewport/Content") as RectTransform;
            context.MyInfo = Find(context.Canvas.transform,
                "PvPTap/Lobby/MyRankingInfoBox") as RectTransform;
            context.MatchButton = Get<Button>(context.Canvas.transform,
                "PvPTap/Lobby/MatchButton", context.Errors);
            context.LimitPopup = Find(context.Canvas.transform,
                "PvPTap/Lobby/DailyPvpLimitReachedPopup")?.gameObject;
            context.Loading = Find(context.Canvas.transform, "PvPTap/Loading")?.gameObject;
            context.LoadingSpinner = Find(context.Canvas.transform,
                "PvPTap/Loading/LoadingSpinner") as RectTransform;
            context.LoadingTitle = Get<TMP_Text>(context.Canvas.transform,
                "PvPTap/Loading/MatchText", context.Errors);
            context.ParticipantCount = Get<TMP_Text>(context.Canvas.transform,
                "PvPTap/Loading/ParticipantsCountText/CurrentParticipantsCount", context.Errors);
            context.CancelButton = Get<Button>(context.Canvas.transform,
                "PvPTap/Loading/CancelButton", context.Errors);
            context.Ready = Find(context.Canvas.transform, "PvPTap/Ready")?.gameObject;
            context.Battle = Find(context.Canvas.transform, "PvPTap/Battle")?.gameObject;
            BattleUIManager battleUi = UnityEngine.Object.FindObjectOfType<BattleUIManager>(true);
            if (battleUi != null)
            {
                SerializedObject battleSerialized = new SerializedObject(battleUi);
                context.CheckOpponentAttributeButton = GetReference<Button>(battleSerialized,
                    "checkOppAttrButton");
                context.OpponentAttributeTexts = GetReferenceArray<TMP_Text>(battleSerialized,
                    "oppAttrText");
                context.AttributeButtons = GetReferenceArray<Button>(battleSerialized, "attrButtons");
                context.ConsumableButtons = GetReferenceArray<Button>(battleSerialized,
                    "consumableItemButtons");
                context.EquipmentButtons = GetReferenceArray<Button>(battleSerialized,
                    "equipItemButtons");
                context.ReadyButton = GetReference<Button>(battleSerialized, "readyButton");
                context.MyNameText = GetReference<TMP_Text>(battleSerialized, "myNameText");
                context.OpponentNameText = GetReference<TMP_Text>(battleSerialized, "oppNameText");
                context.MyReadyText = GetReference<TMP_Text>(battleSerialized, "isMeReadyText");
                context.OpponentReadyText = GetReference<TMP_Text>(battleSerialized, "isOppReadyText");
                context.EquipSelectPanel = GetReference<GameObject>(battleSerialized, "equipSelectPanel");
                context.EquipItemDetailPopup = GetReference<GameObject>(battleSerialized, "equipDetailPopup");
                context.EquipTypeText = GetReference<TMP_Text>(battleSerialized, "equipTypeText");
                context.EquipItemSlotParent = GetReference<Transform>(battleSerialized, "itemSlotParent");
                context.EquipSelectCloseButton = GetReference<Button>(battleSerialized, "equipSelectCloseButton");
                context.EquipIcon = GetReference<Image>(battleSerialized, "equipIcon");
                context.EquipNameText = GetReference<TMP_Text>(battleSerialized, "equipNameText");
                context.EquipDescriptionText = GetReference<TMP_Text>(battleSerialized, "equipDescText");
                context.EquipActionButton = GetReference<Button>(battleSerialized, "equipActionButton");
                context.EquipActionButtonText = GetReference<TMP_Text>(battleSerialized, "equipActionButtonText");
                context.EquipDetailCloseButton = GetReference<Button>(battleSerialized, "equipDetailCloseButton");
                context.BattleMyName = GetReference<TMP_Text>(battleSerialized, "myName");
                context.BattleOpponentName = GetReference<TMP_Text>(battleSerialized, "oppName");
                context.MyBattleTitleImage = GetReference<Image>(battleSerialized, "myBattleTitleImage");
                context.OpponentBattleTitleImage = GetReference<Image>(battleSerialized, "oppBattleTitleImage");
                context.MyBattleTitleVisual = context.MyBattleTitleImage != null
                    ? context.MyBattleTitleImage
                    : FindDescendant(context.Battle?.transform, "MyTitleImage")?.GetComponent<Image>();
                context.OpponentBattleTitleVisual = context.OpponentBattleTitleImage != null
                    ? context.OpponentBattleTitleImage
                    : FindDescendant(context.Battle?.transform, "OppTitleImage")?.GetComponent<Image>();
                context.TimerText = GetReference<TMP_Text>(battleSerialized, "timerText");
                context.MyHpBar = GetReference<Image>(battleSerialized, "myHpBar");
                context.OpponentHpBar = GetReference<Image>(battleSerialized, "oppHpBar");
                context.MyHpText = GetReference<TMP_Text>(battleSerialized, "myHpText");
                context.OpponentHpText = GetReference<TMP_Text>(battleSerialized, "oppHpText");
                context.MyCharacterImage = GetReference<Image>(battleSerialized, "myCharacterImage");
                context.OpponentCharacterImage = GetReference<Image>(battleSerialized, "oppCharacterImage");
                context.TurnCountText = GetReference<TMP_Text>(battleSerialized, "turnCountText");
            }
            if (context.Lobby == null) context.Errors.Add("Canvas/PvPTap/Lobby를 찾을 수 없습니다.");
            if (context.RankingContent == null) context.Errors.Add("RankingList Content를 찾을 수 없습니다.");
            if (context.MyInfo == null) context.Errors.Add("MyRankingInfoBox를 찾을 수 없습니다.");
            if (context.LimitPopup == null) context.Errors.Add("DailyPvpLimitReachedPopup을 찾을 수 없습니다.");
            if (context.Loading == null) context.Errors.Add("Canvas/PvPTap/Loading을 찾을 수 없습니다.");
            if (context.LoadingSpinner == null) context.Errors.Add("LoadingSpinner를 찾을 수 없습니다.");
            if (context.Ready == null) context.Errors.Add("Canvas/PvPTap/Ready를 찾을 수 없습니다.");
            if (context.Battle == null) context.Errors.Add("Canvas/PvPTap/Battle을 찾을 수 없습니다.");
            else if (Find(context.Ready.transform, "IsReadyPanel") == null ||
                     Find(context.Ready.transform, "OppInfoPanel") == null ||
                     Find(context.Ready.transform, "AttributeButtons") == null ||
                     Find(context.Ready.transform, "ConsumableItemButtons") == null ||
                     Find(context.Ready.transform, "EquipItemButtons") == null)
                context.Errors.Add("Ready의 기존 상태/속성/소비품/장비 컨테이너를 찾을 수 없습니다.");
            if (battleUi == null) context.Errors.Add("BattleUIManager를 찾을 수 없습니다.");
            if (context.AttributeButtons.Length != 4) context.Errors.Add("Ready 속성 버튼 배열이 4개가 아닙니다.");
            if (context.ConsumableButtons.Length != 3) context.Errors.Add("Ready 소비 아이템 버튼 배열이 3개가 아닙니다.");
            if (context.EquipmentButtons.Length != 4) context.Errors.Add("Ready 장비 버튼 배열이 4개가 아닙니다.");
            if (context.ReadyButton == null || context.MyNameText == null || context.OpponentNameText == null ||
                context.MyReadyText == null || context.OpponentReadyText == null)
                context.Errors.Add("Ready 필수 이름/상태/ReadyButton 직렬화 참조가 없습니다.");
            if (context.EquipSelectPanel == null || context.EquipItemDetailPopup == null)
                context.Errors.Add("EquipSelectPanel/EquipItemDetailPopup 보호 참조가 없습니다.");
            if (context.EquipTypeText == null || context.EquipItemSlotParent == null ||
                context.EquipSelectCloseButton == null || context.EquipIcon == null ||
                context.EquipNameText == null || context.EquipDescriptionText == null ||
                context.EquipActionButton == null || context.EquipActionButtonText == null ||
                context.EquipDetailCloseButton == null)
                context.Errors.Add("장비 선택/상세 팝업의 기존 직렬화 참조가 완전하지 않습니다.");
            if (context.BattleMyName == null || context.BattleOpponentName == null ||
                context.TimerText == null || context.MyHpBar == null || context.OpponentHpBar == null ||
                context.MyHpText == null || context.OpponentHpText == null ||
                context.MyCharacterImage == null || context.OpponentCharacterImage == null ||
                context.TurnCountText == null)
                context.Errors.Add("Battle HUD의 기존 이름/캐릭터/HP/턴/타이머 참조가 완전하지 않습니다.");
        }

        if (logErrors)
        {
            foreach (string error in context.Errors) Debug.LogError("[HabitPVP PvP] " + error);
        }
        return context;
    }

    private static void ApplyLobbyStyle(Context context)
    {
        RectTransform lobbyRect = context.Lobby.GetComponent<RectTransform>();
        Record(lobbyRect, "Layout PvP lobby");
        SetStretch(lobbyRect, Vector2.zero, Vector2.zero);
        Image lobbyImage = GetOrAdd<Image>(context.Lobby);
        StyleImage(lobbyImage, null, Cream, Image.Type.Simple);

        StyleText(context.RankingTitle, "모험가 PvP 랭킹", context.Font, 50f, Brown, FontStyles.Bold);
        SetTop(context.RankingTitle.rectTransform, new Vector2(920f, 90f), new Vector2(0f, -72f));

        RectTransform listRect = context.RankingList.GetComponent<RectTransform>();
        SetTop(listRect, new Vector2(940f, 980f), new Vector2(0f, -210f));
        StyleImage(GetOrAdd<Image>(context.RankingList.gameObject), context.CardSprite, Color.white, Image.Type.Sliced);
        context.RankingList.horizontal = false;
        context.RankingList.vertical = true;
        context.RankingList.movementType = ScrollRect.MovementType.Clamped;
        context.RankingList.scrollSensitivity = 45f;
        Dirty(context.RankingList);

        RectTransform viewport = Find(context.RankingList.transform, "Viewport") as RectTransform;
        if (viewport != null) SetStretch(viewport, new Vector2(28f, 76f), new Vector2(-28f, -28f));

        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(context.RankingContent.gameObject);
        Record(layout, "Configure PvP ranking layout");
        layout.padding = new RectOffset(12, 12, 8, 18);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(context.RankingContent.gameObject);
        Record(fitter, "Configure PvP ranking content fitter");
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        SetTopStretch(context.RankingContent, 0f);

        EnsureRankingHeader(context);
        StyleMyInfo(context);
        StyleButton(context.MatchButton, "매칭 시작", context.GoldButtonSprite, context.Font, 38f);
        SetBottom(context.MatchButton.GetComponent<RectTransform>(), new Vector2(620f, 118f), new Vector2(0f, 58f));
        StyleLimitPopup(context);
    }

    private static void EnsureRankingHeader(Context context)
    {
        Transform existing = Find(context.RankingList.transform, "RankingColumnHeader");
        GameObject header = existing != null
            ? existing.gameObject
            : CreateImage(context.RankingList.transform, "RankingColumnHeader", context.CreamButtonSprite, CreamLight).gameObject;
        RectTransform rect = header.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -12f);
        rect.sizeDelta = new Vector2(-56f, 58f);
        TMP_Text text = header.GetComponentInChildren<TMP_Text>(true);
        if (text == null) text = CreateText(header.transform, "Columns", "순위     칭호 · 모험가                         점수", context.Font, 25f, MutedBrown, FontStyles.Bold);
        StyleText(text, "순위     칭호 · 모험가                         점수", context.Font, 25f, MutedBrown, FontStyles.Bold);
        SetStretch(text.rectTransform, new Vector2(24f, 4f), new Vector2(-24f, -4f));
    }

    private static void ApplyLoadingStyle(Context context)
    {
        RectTransform loadingRect = context.Loading.GetComponent<RectTransform>();
        SetStretch(loadingRect, Vector2.zero, Vector2.zero);
        StyleImage(GetOrAdd<Image>(context.Loading), null, Cream, Image.Type.Simple);

        Transform existingPanel = Find(context.Loading.transform, "MatchingProgressPanel");
        Image panel = existingPanel != null
            ? GetOrAdd<Image>(existingPanel.gameObject)
            : CreateImage(context.Loading.transform, "MatchingProgressPanel", context.CardSprite, Color.white);
        StyleImage(panel, context.CardSprite, Color.white, Image.Type.Sliced);
        SetCentered(panel.rectTransform, new Vector2(900f, 920f), Vector2.zero);
        panel.raycastTarget = false;
        panel.transform.SetAsFirstSibling();

        StyleText(context.LoadingTitle, "상대를 찾는 중...", context.Font, 48f, Brown, FontStyles.Bold);
        SetCentered(context.LoadingTitle.rectTransform, new Vector2(760f, 90f), new Vector2(0f, 350f));

        TMP_Text guide = panel.transform.Find("GuideText")?.GetComponent<TMP_Text>();
        if (guide == null)
        {
            guide = CreateText(panel.transform, "GuideText",
                "비슷한 실력의 모험가를 찾고 있습니다.\n잠시만 기다려주세요!",
                context.Font, 31f, MutedBrown, FontStyles.Normal);
        }
        StyleText(guide, "비슷한 실력의 모험가를 찾고 있습니다.\n잠시만 기다려주세요!",
            context.Font, 31f, MutedBrown, FontStyles.Normal);
        guide.enableWordWrapping = true;
        SetCentered(guide.rectTransform, new Vector2(720f, 130f), new Vector2(0f, 235f));

        // BattleUIManager가 회전시키는 기존 RectTransform/Image를 교체하지 않는다.
        SetCentered(context.LoadingSpinner, new Vector2(180f, 180f), new Vector2(0f, 40f));
        Image spinnerImage = context.LoadingSpinner.GetComponent<Image>();
        if (spinnerImage != null)
        {
            Record(spinnerImage, "Style existing PvP loading spinner");
            spinnerImage.color = Gold;
            Dirty(spinnerImage);
            PrefabUtility.RecordPrefabInstancePropertyModifications(spinnerImage);
        }

        RectTransform participantParent = Find(context.Loading.transform,
            "ParticipantsCountText") as RectTransform;
        if (participantParent != null)
        {
            SetCentered(participantParent, new Vector2(620f, 100f), new Vector2(0f, -150f));
            Image countCard = GetOrAdd<Image>(participantParent.gameObject);
            StyleImage(countCard, context.CreamButtonSprite, CreamLight, Image.Type.Sliced);
            foreach (TMP_Text text in participantParent.GetComponentsInChildren<TMP_Text>(true))
            {
                StyleText(text, null, context.Font, text == context.ParticipantCount ? 38f : 30f,
                    text == context.ParticipantCount ? Brown : MutedBrown,
                    text == context.ParticipantCount ? FontStyles.Bold : FontStyles.Normal);
                text.enableWordWrapping = false;
            }
        }

        StyleButton(context.CancelButton, "매칭 취소", context.GoldButtonSprite, context.Font, 36f);
        SetCentered(context.CancelButton.GetComponent<RectTransform>(),
            new Vector2(520f, 116f), new Vector2(0f, -350f));
    }

    private static void StyleMyInfo(Context context)
    {
        StyleImage(GetOrAdd<Image>(context.MyInfo.gameObject), context.CardSprite, Color.white, Image.Type.Sliced);
        SetBottom(context.MyInfo, new Vector2(940f, 220f), new Vector2(0f, 205f));

        RectTransform ranking = Find(context.MyInfo, "RankingTextParent") as RectTransform;
        RectTransform score = Find(context.MyInfo, "ScoreTextParent") as RectTransform;
        RectTransform remain = Find(context.MyInfo, "RemainBox") as RectTransform;
        SetInfoColumn(ranking, -300f);
        SetInfoColumn(score, 0f);
        SetInfoColumn(remain, 300f);
        foreach (TMP_Text text in context.MyInfo.GetComponentsInChildren<TMP_Text>(true))
        {
            bool value = text.name.IndexOf("Current", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         text.name.IndexOf("Point", StringComparison.OrdinalIgnoreCase) >= 0;
            StyleText(text, null, context.Font, value ? 34f : 25f,
                value ? Brown : MutedBrown, value ? FontStyles.Bold : FontStyles.Normal);
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }
    }

    private static void StyleLimitPopup(Context context)
    {
        RectTransform popupRect = context.LimitPopup.GetComponent<RectTransform>();
        SetStretch(popupRect, Vector2.zero, Vector2.zero);
        Transform background = Find(context.LimitPopup.transform, "BackgroundImage");
        if (background != null)
        {
            StyleImage(GetOrAdd<Image>(background.gameObject), context.CardSprite, Color.white, Image.Type.Sliced);
            SetCentered(background as RectTransform, new Vector2(820f, 520f), Vector2.zero);
            foreach (TMP_Text text in background.GetComponentsInChildren<TMP_Text>(true))
            {
                StyleText(text, null, context.Font, 34f, Alert, FontStyles.Bold);
                text.enableWordWrapping = true;
                text.overflowMode = TextOverflowModes.Ellipsis;
            }
        }
        Button close = context.LimitPopup.GetComponentInChildren<Button>(true);
        if (close != null) StyleButton(close, "확인", context.GoldButtonSprite, context.Font, 32f);
    }

    private static void ApplyReadyStyle(Context context)
    {
        RectTransform readyRect = context.Ready.GetComponent<RectTransform>();
        SetStretch(readyRect, Vector2.zero, Vector2.zero);
        StyleImage(GetOrAdd<Image>(context.Ready), null, Cream, Image.Type.Simple);

        RectTransform readyStatusPanel = Find(context.Ready.transform, "IsReadyPanel") as RectTransform;
        RectTransform opponentInfoPanel = Find(context.Ready.transform, "OppInfoPanel") as RectTransform;
        RectTransform attributePanel = Find(context.Ready.transform, "AttributeButtons") as RectTransform;
        RectTransform consumablePanel = Find(context.Ready.transform, "ConsumableItemButtons") as RectTransform;
        RectTransform equipmentPanel = Find(context.Ready.transform, "EquipItemButtons") as RectTransform;
        StyleImage(GetOrAdd<Image>(readyStatusPanel.gameObject), context.CardSprite, Color.white, Image.Type.Sliced);
        StyleImage(GetOrAdd<Image>(opponentInfoPanel.gameObject), context.CardSprite, Color.white, Image.Type.Sliced);
        StyleImage(GetOrAdd<Image>(attributePanel.gameObject), context.CardSprite, Color.white, Image.Type.Sliced);
        StyleImage(GetOrAdd<Image>(consumablePanel.gameObject), context.CardSprite, Color.white, Image.Type.Sliced);
        StyleImage(GetOrAdd<Image>(equipmentPanel.gameObject), context.CardSprite, Color.white, Image.Type.Sliced);
        SetTop(readyStatusPanel, new Vector2(960f, 220f), new Vector2(0f, -95f));
        SetTop(opponentInfoPanel, new Vector2(960f, 190f), new Vector2(0f, -335f));
        SetTop(attributePanel, new Vector2(960f, 250f), new Vector2(0f, -545f));
        SetTop(consumablePanel, new Vector2(960f, 275f), new Vector2(0f, -815f));
        SetTop(equipmentPanel, new Vector2(960f, 295f), new Vector2(0f, -1110f));
        SetStretch(Find(readyStatusPanel, "NameText") as RectTransform, Vector2.zero, Vector2.zero);
        SetStretch(Find(readyStatusPanel, "IsReadyText") as RectTransform, Vector2.zero, Vector2.zero);
        SetStretch(Find(opponentInfoPanel, "Text") as RectTransform,
            new Vector2(410f, 0f), Vector2.zero);
        StyleReadyGroupText(readyStatusPanel, context.Font);
        StyleReadyGroupText(opponentInfoPanel, context.Font);
        StyleReadyGroupText(attributePanel, context.Font);
        StyleReadyGroupText(consumablePanel, context.Font);
        StyleReadyGroupText(equipmentPanel, context.Font);

        StyleText(context.MyNameText, null, context.Font, 36f, Brown, FontStyles.Bold);
        StyleText(context.OpponentNameText, null, context.Font, 36f, Brown, FontStyles.Bold);
        StyleText(context.MyReadyText, null, context.Font, 29f, MutedBrown, FontStyles.Bold);
        StyleText(context.OpponentReadyText, null, context.Font, 29f, MutedBrown, FontStyles.Bold);
        SetCentered(context.MyNameText.rectTransform, new Vector2(390f, 58f), new Vector2(-230f, 35f));
        SetCentered(context.OpponentNameText.rectTransform, new Vector2(390f, 58f), new Vector2(230f, 35f));
        SetCentered(context.MyReadyText.rectTransform, new Vector2(390f, 50f), new Vector2(-230f, -40f));
        SetCentered(context.OpponentReadyText.rectTransform, new Vector2(390f, 50f), new Vector2(230f, -40f));

        StyleButton(context.CheckOpponentAttributeButton, null, context.CreamButtonSprite,
            context.Font, 28f);
        if (context.CheckOpponentAttributeButton != null)
            SetCentered(context.CheckOpponentAttributeButton.GetComponent<RectTransform>(),
                new Vector2(340f, 82f), new Vector2(-270f, 0f));
        for (int index = 0; index < context.OpponentAttributeTexts.Length; index++)
        {
            TMP_Text text = context.OpponentAttributeTexts[index];
            StyleText(text, null, context.Font, 25f, Brown, FontStyles.Bold);
            SetCentered(text.rectTransform, new Vector2(125f, 70f),
                new Vector2(-200f + index * 135f, 0f));
        }

        LayoutReadyButtons(context.AttributeButtons, context.CreamButtonSprite, context.Font,
            4, 205f, -10f, new Vector2(185f, 105f));
        LayoutReadyButtons(context.ConsumableButtons, context.CreamButtonSprite, context.Font,
            3, 270f, -10f, new Vector2(230f, 145f));
        LayoutReadyButtons(context.EquipmentButtons, context.CreamButtonSprite, context.Font,
            4, 205f, -10f, new Vector2(185f, 155f));
        StyleButton(context.ReadyButton, null, context.GoldButtonSprite, context.Font, 38f);
        ConfigureSelectionColors(context.ReadyButton);
        SetBottom(context.ReadyButton.GetComponent<RectTransform>(), new Vector2(620f, 118f),
            new Vector2(0f, 72f));
    }

    private static void LayoutReadyButtons(Button[] buttons, Sprite sprite, TMP_FontAsset font,
        int count, float spacing, float topY, Vector2 size)
    {
        if (buttons == null) return;
        float start = -spacing * (count - 1) * 0.5f;
        for (int index = 0; index < buttons.Length; index++)
        {
            Button button = buttons[index];
            StyleButton(button, null, sprite, font, 25f);
            ConfigureSelectionColors(button);
            if (button != null)
                SetCentered(button.GetComponent<RectTransform>(), size,
                    new Vector2(start + spacing * index, topY));
        }
    }

    private static void ConfigureSelectionColors(Button button)
    {
        if (button == null) return;
        Record(button, "Configure Ready button states");
        ColorBlock colors = button.colors;
        colors.normalColor = CreamLight;
        colors.highlightedColor = new Color32(0xFF, 0xE7, 0xA8, 0xFF);
        colors.pressedColor = new Color32(0xE7, 0xA8, 0x35, 0xFF);
        colors.selectedColor = new Color32(0xFF, 0xD8, 0x79, 0xFF);
        colors.disabledColor = new Color32(0xC9, 0xBE, 0xAA, 0xB8);
        button.colors = colors;
        Dirty(button);
        PrefabUtility.RecordPrefabInstancePropertyModifications(button);
    }

    private static void StyleReadyGroupText(Transform group, TMP_FontAsset font)
    {
        if (group == null) return;
        foreach (TMP_Text text in group.GetComponentsInChildren<TMP_Text>(true))
        {
            float size = Mathf.Clamp(text.fontSize, 23f, 36f);
            StyleText(text, null, font, size, Brown,
                text.fontStyle == FontStyles.Normal ? FontStyles.Bold : text.fontStyle);
        }
    }

    private static void ApplyEquipmentPopupStyle(Context context)
    {
        RectTransform select = context.EquipSelectPanel.GetComponent<RectTransform>();
        SetCentered(select, new Vector2(940f, 1320f), Vector2.zero);
        StyleImage(GetOrAdd<Image>(context.EquipSelectPanel), context.CardSprite, Color.white, Image.Type.Sliced);
        StyleText(context.EquipTypeText, null, context.Font, 42f, Brown, FontStyles.Bold);
        SetTop(context.EquipTypeText.rectTransform, new Vector2(700f, 76f), new Vector2(-70f, -42f));
        StyleButton(context.EquipSelectCloseButton, "Ready로 돌아가기", context.CreamButtonSprite,
            context.Font, 27f);
        SetBottom(context.EquipSelectCloseButton.GetComponent<RectTransform>(),
            new Vector2(360f, 92f), new Vector2(0f, 42f));

        RectTransform content = context.EquipItemSlotParent as RectTransform;
        if (content != null)
        {
            LayoutGroup existingLayout = content.GetComponent<LayoutGroup>();
            GridLayoutGroup grid = existingLayout as GridLayoutGroup;
            if (existingLayout == null) grid = Undo.AddComponent<GridLayoutGroup>(content.gameObject);
            if (grid != null)
            {
                Record(grid, "Layout PvP equipment cards");
                grid.padding = new RectOffset(24, 24, 24, 24);
                grid.cellSize = new Vector2(380f, 190f);
                grid.spacing = new Vector2(24f, 24f);
                grid.childAlignment = TextAnchor.UpperCenter;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 2;
            }
            ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(content.gameObject);
            Record(fitter, "Fit PvP equipment card content");
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect scroll = content.GetComponentInParent<ScrollRect>(true);
            if (scroll != null)
            {
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 45f;
                StyleImage(GetOrAdd<Image>(scroll.gameObject), context.CreamButtonSprite,
                    CreamLight, Image.Type.Sliced);
            }
        }

        RectTransform detail = context.EquipItemDetailPopup.GetComponent<RectTransform>();
        SetCentered(detail, new Vector2(820f, 940f), Vector2.zero);
        StyleImage(GetOrAdd<Image>(context.EquipItemDetailPopup), context.CardSprite,
            Color.white, Image.Type.Sliced);
        Record(context.EquipIcon, "Style PvP equipment icon");
        context.EquipIcon.color = Color.white;
        context.EquipIcon.preserveAspect = true;
        context.EquipIcon.raycastTarget = false;
        Dirty(context.EquipIcon);
        SetTop(context.EquipIcon.rectTransform, new Vector2(250f, 250f), new Vector2(0f, -95f));
        StyleText(context.EquipNameText, null, context.Font, 38f, Brown, FontStyles.Bold);
        SetTop(context.EquipNameText.rectTransform, new Vector2(680f, 72f), new Vector2(0f, -370f));
        StyleText(context.EquipDescriptionText, null, context.Font, 28f, MutedBrown, FontStyles.Normal);
        context.EquipDescriptionText.enableWordWrapping = true;
        context.EquipDescriptionText.overflowMode = TextOverflowModes.Ellipsis;
        SetTop(context.EquipDescriptionText.rectTransform, new Vector2(680f, 220f), new Vector2(0f, -455f));
        StyleButton(context.EquipActionButton, null, context.GoldButtonSprite, context.Font, 31f);
        SetBottom(context.EquipActionButton.GetComponent<RectTransform>(),
            new Vector2(360f, 100f), new Vector2(-185f, 55f));
        StyleButton(context.EquipDetailCloseButton, "뒤로", context.CreamButtonSprite, context.Font, 31f);
        SetBottom(context.EquipDetailCloseButton.GetComponent<RectTransform>(),
            new Vector2(260f, 100f), new Vector2(235f, 55f));
    }

    private static void ApplyBattleStyle(Context context)
    {
        Image battleBackground = GetOrAdd<Image>(context.Battle);
        StyleImage(battleBackground, null, Cream, Image.Type.Simple);
        battleBackground.raycastTarget = false;

        StyleText(context.BattleMyName, null, context.Font, 36f, Brown, FontStyles.Bold);
        StyleText(context.BattleOpponentName, null, context.Font, 36f, Brown, FontStyles.Bold);
        StyleText(context.MyHpText, null, context.Font, 27f, CreamLight, FontStyles.Bold);
        StyleText(context.OpponentHpText, null, context.Font, 27f, CreamLight, FontStyles.Bold);
        StyleText(context.TimerText, null, context.Font, 46f, Brown, FontStyles.Bold);
        StyleText(context.TurnCountText, null, context.Font, 34f, Gold, FontStyles.Bold);

        StyleBattleInfoBackground(context.BattleMyName, context);
        StyleBattleInfoBackground(context.BattleOpponentName, context);
        StyleBattleInfoBackground(context.MyHpBar, context);
        StyleBattleInfoBackground(context.OpponentHpBar, context);

        StyleBattleHpFill(context.MyHpBar, new Color32(0x58, 0xAD, 0x62, 0xFF));
        StyleBattleHpFill(context.OpponentHpBar, new Color32(0xD9, 0x62, 0x55, 0xFF));
        PreserveBattleImage(context.MyCharacterImage);
        PreserveBattleImage(context.OpponentCharacterImage);
        PreserveBattleImage(context.MyBattleTitleVisual);
        PreserveBattleImage(context.OpponentBattleTitleVisual);
    }

    private static void StyleBattleInfoBackground(Component child, Context context)
    {
        Image background = child != null && child.transform.parent != null
            ? child.transform.parent.GetComponent<Image>() : null;
        if (background != null) StyleImage(background, context.CardSprite, Color.white, Image.Type.Sliced);
    }

    private static void StyleBattleHpFill(Image bar, Color color)
    {
        if (bar == null) return;
        Record(bar, "Style PvP battle HP fill");
        bar.color = color;
        Dirty(bar);
        PrefabUtility.RecordPrefabInstancePropertyModifications(bar);
    }

    private static void PreserveBattleImage(Image image)
    {
        if (image == null) return;
        Record(image, "Preserve PvP battle image presentation");
        image.preserveAspect = true;
        Dirty(image);
        PrefabUtility.RecordPrefabInstancePropertyModifications(image);
    }

    private static void EnsurePreviewRoot(Context context)
    {
        GameObject existing = FindRoot(context.Scene, PreviewRootName);
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        GameObject root = new GameObject(PreviewRootName, typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root, "Create PvP ranking preview root");
        SceneManager.MoveGameObjectToScene(root, context.Scene);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Image background = CreateImage(root.transform, "PreviewBackground", null, Cream);
        SetStretch(background.rectTransform, Vector2.zero, Vector2.zero);
        TMP_Text heading = CreateText(background.transform, "PreviewHeading",
            "I. PvP LOBBY & RANKING · LOCAL PREVIEW", context.Font, 27f, MutedBrown, FontStyles.Bold);
        SetTop(heading.rectTransform, new Vector2(980f, 54f), new Vector2(0f, -22f));
        TMP_Text title = CreateText(background.transform, "RankingTitle", "모험가 PvP 랭킹",
            context.Font, 52f, Brown, FontStyles.Bold);
        SetTop(title.rectTransform, new Vector2(920f, 86f), new Vector2(0f, -84f));

        Image listCard = CreateImage(background.transform, "RankingCard", context.CardSprite, Color.white);
        SetTop(listCard.rectTransform, new Vector2(940f, 1050f), new Vector2(0f, -184f));
        TMP_Text columns = CreateText(listCard.transform, "RankingColumns",
            "순위     칭호 · 모험가                              점수",
            context.Font, 27f, MutedBrown, FontStyles.Bold);
        SetTop(columns.rectTransform, new Vector2(860f, 60f), new Vector2(0f, -24f));

        ScrollRect scroll = Undo.AddComponent<ScrollRect>(listCard.gameObject);
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 48f;
        Image viewport = CreateImage(listCard.transform, "Viewport", null, new Color32(0, 0, 0, 0));
        SetStretch(viewport.rectTransform, new Vector2(30f, 94f), new Vector2(-30f, -30f));
        Undo.AddComponent<RectMask2D>(viewport.gameObject);
        GameObject contentObject = new GameObject("Content", typeof(RectTransform),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        Undo.RegisterCreatedObjectUndo(contentObject, "Create PvP preview ranking content");
        contentObject.transform.SetParent(viewport.transform, false);
        RectTransform content = contentObject.GetComponent<RectTransform>();
        SetTopStretch(content, 0f);
        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 4, 16);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport.rectTransform;
        scroll.content = content;

        string[] names =
        {
            "황금검 루나", "불꽃기사 하람", "새벽의 미르", "숲지기 다온", "파도술사 유나",
            "별빛 로아", "철벽 태오", "바람궁수 시온", "은월 나래", "용맹한 준",
            "연금술사 세아", "수호자 리안", "폭풍의 건", "달빛 아린", "붉은별 현",
            "푸른잎 솔", "검은고양이 봄", "빛의 수", "작은거인 온", "여행자 담"
        };
        for (int index = 0; index < names.Length; index++)
        {
            CreatePreviewRankingRow(content, index + 1, names[index], 2460 - index * 73, context);
        }

        Image info = CreateImage(background.transform, "MyPvpInfoCard", context.CardSprite, Color.white);
        SetBottom(info.rectTransform, new Vector2(940f, 250f), new Vector2(0f, 260f));
        CreateInfoColumn(info.transform, "내 순위", "12위", -300f, context, out _);
        CreateInfoColumn(info.transform, "랭킹 점수", "1,657 P", 0f, context, out _);
        CreateInfoColumn(info.transform, "남은 PvP", "3 / 5", 300f, context, out TMP_Text remainText);
        Image remainCard = Find(info.transform, "RemainColumn")?.GetComponent<Image>();

        TMP_Text status = CreateText(background.transform, "LocalStatus",
            "로컬 Preview · 실제 매칭과 서버 요청은 실행되지 않습니다.",
            context.Font, 25f, MutedBrown, FontStyles.Normal);
        SetBottom(status.rectTransform, new Vector2(920f, 62f), new Vector2(0f, 170f));
        Button match = CreateButton(background.transform, "MatchButton", "매칭 시작",
            context.GoldButtonSprite, context.Font, new Vector2(-170f, 65f), new Vector2(560f, 118f));
        SetBottom(match.GetComponent<RectTransform>(), new Vector2(560f, 118f), new Vector2(-170f, 48f));
        Button zero = CreateButton(background.transform, "ZeroRemainTestButton", "잔여 0 테스트",
            context.CreamButtonSprite, context.Font, new Vector2(300f, 48f), new Vector2(270f, 92f));
        SetBottom(zero.GetComponent<RectTransform>(), new Vector2(270f, 92f), new Vector2(300f, 61f));

        Image loadingPanel = CreateImage(background.transform, "LoadingPanel", null, Cream);
        SetStretch(loadingPanel.rectTransform, Vector2.zero, Vector2.zero);
        TMP_Text loadingHeading = CreateText(loadingPanel.transform, "PreviewHeading",
            "I. PvP MATCHING · LOCAL PREVIEW", context.Font, 27f, MutedBrown, FontStyles.Bold);
        SetTop(loadingHeading.rectTransform, new Vector2(980f, 54f), new Vector2(0f, -22f));
        Image progressCard = CreateImage(loadingPanel.transform, "MatchingProgressCard",
            context.CardSprite, Color.white);
        SetCentered(progressCard.rectTransform, new Vector2(900f, 960f), Vector2.zero);
        TMP_Text loadingTitle = CreateText(progressCard.transform, "Title", "상대를 찾는 중...",
            context.Font, 49f, Brown, FontStyles.Bold);
        SetCentered(loadingTitle.rectTransform, new Vector2(760f, 90f), new Vector2(0f, 350f));
        TMP_Text loadingGuide = CreateText(progressCard.transform, "Guide",
            "비슷한 실력의 모험가를 찾고 있습니다.\n잠시만 기다려주세요!",
            context.Font, 31f, MutedBrown, FontStyles.Normal);
        loadingGuide.enableWordWrapping = true;
        SetCentered(loadingGuide.rectTransform, new Vector2(720f, 130f), new Vector2(0f, 220f));
        Image spinner = CreateImage(progressCard.transform, "LoadingSpinner",
            context.CreamButtonSprite, CreamLight);
        SetCentered(spinner.rectTransform, new Vector2(190f, 190f), new Vector2(0f, 35f));
        TMP_Text spinnerText = CreateText(spinner.transform, "SpinnerIcon", "⌛",
            context.Font, 72f, Gold, FontStyles.Bold);
        SetStretch(spinnerText.rectTransform, Vector2.zero, Vector2.zero);
        Image participantCard = CreateImage(progressCard.transform, "ParticipantCard",
            context.CreamButtonSprite, CreamLight);
        SetCentered(participantCard.rectTransform, new Vector2(620f, 118f), new Vector2(0f, -155f));
        TMP_Text participantLabel = CreateText(participantCard.transform, "Label", "참가 모험가",
            context.Font, 27f, MutedBrown, FontStyles.Bold);
        SetLeft(participantLabel.rectTransform, new Vector2(270f, 80f), new Vector2(35f, 0f));
        TMP_Text participantCount = CreateText(participantCard.transform, "Count", "1 / 2",
            context.Font, 39f, Brown, FontStyles.Bold);
        SetRight(participantCount.rectTransform, new Vector2(220f, 80f), new Vector2(-38f, 0f));
        Button participantChange = CreateButton(progressCard.transform, "ParticipantChangeButton",
            "참가자 수 변경", context.CreamButtonSprite, context.Font,
            new Vector2(-245f, -335f), new Vector2(260f, 104f));
        Button cancelMatch = CreateButton(progressCard.transform, "CancelButton", "매칭 취소",
            context.CreamButtonSprite, context.Font,
            new Vector2(0f, -335f), new Vector2(200f, 104f));
        Button matchComplete = CreateButton(progressCard.transform, "MatchCompleteTestButton",
            "매칭 완료 테스트", context.GoldButtonSprite, context.Font,
            new Vector2(245f, -335f), new Vector2(260f, 104f));
        loadingPanel.gameObject.SetActive(false);

        Image readyPanel = CreateImage(background.transform, "ReadyPanel", null, Cream);
        SetStretch(readyPanel.rectTransform, Vector2.zero, Vector2.zero);
        TMP_Text readyHeading = CreateText(readyPanel.transform, "PreviewHeading",
            "I. PvP READY · LOCAL PREVIEW", context.Font, 27f, MutedBrown, FontStyles.Bold);
        SetTop(readyHeading.rectTransform, new Vector2(980f, 54f), new Vector2(0f, -22f));

        Image myCard = CreateImage(readyPanel.transform, "MyPlayerCard", context.CardSprite, Color.white);
        SetTop(myCard.rectTransform, new Vector2(455f, 205f), new Vector2(-245f, -85f));
        TMP_Text myName = CreateText(myCard.transform, "Name", "황금검 루나", context.Font,
            35f, Brown, FontStyles.Bold);
        SetCentered(myName.rectTransform, new Vector2(390f, 58f), new Vector2(0f, 35f));
        TMP_Text myReady = CreateText(myCard.transform, "ReadyState", "준비 중", context.Font,
            29f, MutedBrown, FontStyles.Bold);
        SetCentered(myReady.rectTransform, new Vector2(390f, 52f), new Vector2(0f, -38f));

        Image opponentCard = CreateImage(readyPanel.transform, "OpponentPlayerCard",
            context.CardSprite, Color.white);
        SetTop(opponentCard.rectTransform, new Vector2(455f, 205f), new Vector2(245f, -85f));
        TMP_Text opponentName = CreateText(opponentCard.transform, "Name", "불꽃기사 하람",
            context.Font, 35f, Brown, FontStyles.Bold);
        SetCentered(opponentName.rectTransform, new Vector2(390f, 58f), new Vector2(0f, 35f));
        TMP_Text opponentReady = CreateText(opponentCard.transform, "ReadyState", "준비 완료",
            context.Font, 29f, Gold, FontStyles.Bold);
        SetCentered(opponentReady.rectTransform, new Vector2(390f, 52f), new Vector2(0f, -38f));

        Image opponentAttribute = CreateImage(readyPanel.transform, "OpponentAttributeCard",
            context.CardSprite, Color.white);
        SetTop(opponentAttribute.rectTransform, new Vector2(940f, 155f), new Vector2(0f, -310f));
        TMP_Text opponentAttributeText = CreateText(opponentAttribute.transform, "Text",
            "상대 속성  ·  FIRE", context.Font, 31f, Alert, FontStyles.Bold);
        SetStretch(opponentAttributeText.rectTransform, new Vector2(35f, 18f), new Vector2(-35f, -18f));

        Image attributeCard = CreateImage(readyPanel.transform, "AttributeCard",
            context.CardSprite, Color.white);
        SetTop(attributeCard.rectTransform, new Vector2(940f, 240f), new Vector2(0f, -485f));
        TMP_Text attributeTitle = CreateText(attributeCard.transform, "Title", "전투 속성 선택",
            context.Font, 29f, Brown, FontStyles.Bold);
        SetTop(attributeTitle.rectTransform, new Vector2(820f, 48f), new Vector2(0f, -18f));
        string[] attributeNames = { "FIRE", "WATER", "GRASS", "AURORA" };
        Button[] attributeButtons = new Button[attributeNames.Length];
        for (int index = 0; index < attributeButtons.Length; index++)
            attributeButtons[index] = CreateButton(attributeCard.transform, "Attribute_" + index,
                attributeNames[index], context.CreamButtonSprite, context.Font,
                new Vector2(-315f + index * 210f, -45f), new Vector2(185f, 105f));

        Image consumableCard = CreateImage(readyPanel.transform, "ConsumableCard",
            context.CardSprite, Color.white);
        SetTop(consumableCard.rectTransform, new Vector2(940f, 265f), new Vector2(0f, -745f));
        TMP_Text consumableTitle = CreateText(consumableCard.transform, "Title", "소비 아이템",
            context.Font, 29f, Brown, FontStyles.Bold);
        SetTop(consumableTitle.rectTransform, new Vector2(820f, 48f), new Vector2(0f, -18f));
        RectTransform consumableRow = CreatePreviewButtonRow(consumableCard.transform,
            "ButtonRow", new Vector2(820f, 150f), new Vector2(0f, -38f), 30f);
        string[] consumableNames = { "회복 물약", "수호 부적", "집중 캔디" };
        Button[] consumableButtons = new Button[consumableNames.Length];
        for (int index = 0; index < consumableButtons.Length; index++)
            consumableButtons[index] = CreateReadyPreviewSlot(consumableRow,
                "Consumable_" + index, consumableNames[index], index + 1,
                Vector2.zero, new Vector2(235f, 138f), context);

        Image equipmentCard = CreateImage(readyPanel.transform, "EquipmentCard",
            context.CardSprite, Color.white);
        SetTop(equipmentCard.rectTransform, new Vector2(940f, 275f), new Vector2(0f, -1030f));
        TMP_Text equipmentTitle = CreateText(equipmentCard.transform, "Title", "전투 장비",
            context.Font, 29f, Brown, FontStyles.Bold);
        SetTop(equipmentTitle.rectTransform, new Vector2(820f, 48f), new Vector2(0f, -18f));
        RectTransform equipmentRow = CreatePreviewButtonRow(equipmentCard.transform,
            "ButtonRow", new Vector2(840f, 160f), new Vector2(0f, -42f), 18f);
        string[] equipmentNames = { "무기", "의류", "모자", "신발" };
        Button[] equipmentButtons = new Button[equipmentNames.Length];
        for (int index = 0; index < equipmentButtons.Length; index++)
            equipmentButtons[index] = CreateReadyPreviewSlot(equipmentRow,
                "Equipment_" + index, equipmentNames[index], index + 1,
                Vector2.zero, new Vector2(185f, 148f), context);
        LayoutRebuilder.ForceRebuildLayoutImmediate(consumableRow);
        LayoutRebuilder.ForceRebuildLayoutImmediate(equipmentRow);

        Image equipmentSelectDim = CreateImage(background.transform, "EquipmentSelectDim", null, Dim);
        SetStretch(equipmentSelectDim.rectTransform, Vector2.zero, Vector2.zero);
        Image equipmentSelectPanel = CreateImage(equipmentSelectDim.transform, "EquipmentSelectPanel",
            context.CardSprite, Color.white);
        SetCentered(equipmentSelectPanel.rectTransform, new Vector2(940f, 1320f), Vector2.zero);
        TMP_Text equipmentType = CreateText(equipmentSelectPanel.transform, "EquipmentTypeText",
            "무기 선택", context.Font, 42f, Brown, FontStyles.Bold);
        SetTop(equipmentType.rectTransform, new Vector2(700f, 76f), new Vector2(-70f, -42f));
        Button equipmentSelectClose = CreateButton(equipmentSelectPanel.transform, "CloseButton",
            "Ready로 돌아가기", context.CreamButtonSprite, context.Font,
            Vector2.zero, new Vector2(360f, 92f));
        SetBottom(equipmentSelectClose.GetComponent<RectTransform>(),
            new Vector2(360f, 92f), new Vector2(0f, 42f));

        GameObject listObject = new GameObject("EquipmentList", typeof(RectTransform), typeof(Image),
            typeof(ScrollRect));
        Undo.RegisterCreatedObjectUndo(listObject, "Create equipment preview list");
        listObject.transform.SetParent(equipmentSelectPanel.transform, false);
        RectTransform listRect = listObject.GetComponent<RectTransform>();
        SetCentered(listRect, new Vector2(820f, 1020f), new Vector2(0f, 25f));
        StyleImage(listObject.GetComponent<Image>(), context.CreamButtonSprite, CreamLight, Image.Type.Sliced);
        Image listViewport = CreateImage(listObject.transform, "Viewport", null, Color.white);
        SetStretch(listViewport.rectTransform, new Vector2(22f, 22f), new Vector2(-22f, -22f));
        Mask listMask = Undo.AddComponent<Mask>(listViewport.gameObject);
        listMask.showMaskGraphic = false;
        GameObject listContentObject = new GameObject("Content", typeof(RectTransform),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        Undo.RegisterCreatedObjectUndo(listContentObject, "Create equipment preview content");
        listContentObject.transform.SetParent(listViewport.transform, false);
        RectTransform listContent = listContentObject.GetComponent<RectTransform>();
        SetTopStretch(listContent, 0f);
        VerticalLayoutGroup equipmentListLayout = listContentObject.GetComponent<VerticalLayoutGroup>();
        equipmentListLayout.padding = new RectOffset(18, 18, 18, 18);
        equipmentListLayout.spacing = 18f;
        equipmentListLayout.childAlignment = TextAnchor.UpperCenter;
        equipmentListLayout.childControlWidth = true;
        equipmentListLayout.childControlHeight = false;
        equipmentListLayout.childForceExpandWidth = true;
        equipmentListLayout.childForceExpandHeight = false;
        ContentSizeFitter equipmentListFitter = listContentObject.GetComponent<ContentSizeFitter>();
        equipmentListFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        equipmentListFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect equipmentScroll = listObject.GetComponent<ScrollRect>();
        equipmentScroll.viewport = listViewport.rectTransform;
        equipmentScroll.content = listContent;
        equipmentScroll.horizontal = false;
        equipmentScroll.vertical = true;
        equipmentScroll.movementType = ScrollRect.MovementType.Clamped;
        equipmentScroll.scrollSensitivity = 45f;

        Button[] equipmentItemButtons = new Button[5];
        TMP_Text[] equipmentItemNames = new TMP_Text[5];
        TMP_Text[] equipmentItemStats = new TMP_Text[5];
        TMP_Text[] equipmentItemStates = new TMP_Text[5];
        for (int index = 0; index < equipmentItemButtons.Length; index++)
        {
            equipmentItemButtons[index] = CreateButton(listContent, "EquipmentItem_" + index, "",
                context.CreamButtonSprite, context.Font, Vector2.zero, new Vector2(740f, 245f));
            LayoutElement itemLayout = Undo.AddComponent<LayoutElement>(
                equipmentItemButtons[index].gameObject);
            itemLayout.preferredHeight = 245f;
            itemLayout.minHeight = 245f;
            Image iconFrame = CreateImage(equipmentItemButtons[index].transform, "IconFrame",
                context.GoldButtonSprite, Color.white);
            SetLeft(iconFrame.rectTransform, new Vector2(150f, 150f), new Vector2(28f, 0f));
            TMP_Text iconText = CreateText(iconFrame.transform, "Placeholder", "장비 " + (index + 1),
                context.Font, 22f, Gold, FontStyles.Bold);
            SetStretch(iconText.rectTransform, new Vector2(8f, 8f), new Vector2(-8f, -8f));
            equipmentItemNames[index] = CreateText(equipmentItemButtons[index].transform, "Name",
                "장비 이름", context.Font, 30f, Brown, FontStyles.Bold);
            SetLeft(equipmentItemNames[index].rectTransform, new Vector2(400f, 58f), new Vector2(205f, 48f));
            equipmentItemStats[index] = CreateText(equipmentItemButtons[index].transform, "Stats",
                "능력치", context.Font, 24f, MutedBrown, FontStyles.Normal);
            SetLeft(equipmentItemStats[index].rectTransform, new Vector2(460f, 54f), new Vector2(205f, -10f));
            equipmentItemStates[index] = CreateText(equipmentItemButtons[index].transform, "State",
                "상세 보기", context.Font, 23f, Gold, FontStyles.Bold);
            SetRight(equipmentItemStates[index].rectTransform, new Vector2(170f, 48f), new Vector2(-22f, -78f));
        }
        float equipmentContentHeight = GetVerticalLayoutPreferredHeight(listContent);
        listContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, equipmentContentHeight);

        Image equipmentDetail = CreateImage(equipmentSelectDim.transform, "EquipmentDetailPopup",
            context.CardSprite, Color.white);
        SetCentered(equipmentDetail.rectTransform, new Vector2(820f, 940f), Vector2.zero);
        Image detailIcon = CreateImage(equipmentDetail.transform, "IconFrame",
            context.GoldButtonSprite, Color.white);
        SetTop(detailIcon.rectTransform, new Vector2(250f, 250f), new Vector2(0f, -80f));
        TMP_Text detailIconText = CreateText(detailIcon.transform, "Placeholder", "장비 이미지",
            context.Font, 24f, Gold, FontStyles.Bold);
        SetStretch(detailIconText.rectTransform, new Vector2(12f, 12f), new Vector2(-12f, -12f));
        TMP_Text detailName = CreateText(equipmentDetail.transform, "Name", "장비 이름",
            context.Font, 38f, Brown, FontStyles.Bold);
        SetTop(detailName.rectTransform, new Vector2(680f, 72f), new Vector2(0f, -355f));
        TMP_Text detailStat = CreateText(equipmentDetail.transform, "Stats", "능력치",
            context.Font, 29f, Gold, FontStyles.Bold);
        SetTop(detailStat.rectTransform, new Vector2(680f, 64f), new Vector2(0f, -430f));
        TMP_Text detailDescription = CreateText(equipmentDetail.transform, "Description", "장비 설명",
            context.Font, 27f, MutedBrown, FontStyles.Normal);
        detailDescription.enableWordWrapping = true;
        detailDescription.overflowMode = TextOverflowModes.Ellipsis;
        SetTop(detailDescription.rectTransform, new Vector2(680f, 180f), new Vector2(0f, -510f));
        Button equipmentAction = CreateButton(equipmentDetail.transform, "ActionButton", "장착하기",
            context.GoldButtonSprite, context.Font, Vector2.zero, new Vector2(360f, 100f));
        SetBottom(equipmentAction.GetComponent<RectTransform>(), new Vector2(360f, 100f), new Vector2(-185f, 55f));
        TMP_Text equipmentActionText = equipmentAction.GetComponentInChildren<TMP_Text>(true);
        Button equipmentDetailClose = CreateButton(equipmentDetail.transform, "CloseButton", "뒤로",
            context.CreamButtonSprite, context.Font, Vector2.zero, new Vector2(260f, 100f));
        SetBottom(equipmentDetailClose.GetComponent<RectTransform>(), new Vector2(260f, 100f), new Vector2(235f, 55f));
        equipmentDetail.gameObject.SetActive(false);
        equipmentSelectDim.gameObject.SetActive(false);

        TMP_Text readyGuide = CreateText(readyPanel.transform, "ReadyGuide",
            "속성을 선택하면 준비 완료 버튼이 활성화됩니다.", context.Font,
            25f, MutedBrown, FontStyles.Bold);
        SetBottom(readyGuide.rectTransform, new Vector2(920f, 58f), new Vector2(0f, 225f));
        Button ready = CreateButton(readyPanel.transform, "ReadyButton", "준비 완료",
            context.GoldButtonSprite, context.Font, new Vector2(-285f, -820f), new Vector2(330f, 112f));
        SetBottom(ready.GetComponent<RectTransform>(), new Vector2(330f, 112f), new Vector2(-285f, 82f));
        Button battleTest = CreateButton(readyPanel.transform, "BattleTestButton", "전투 화면 테스트",
            context.GoldButtonSprite, context.Font, Vector2.zero, new Vector2(350f, 112f));
        SetBottom(battleTest.GetComponent<RectTransform>(), new Vector2(350f, 112f), new Vector2(65f, 82f));
        Button returnLobby = CreateButton(readyPanel.transform, "ReturnLobbyTestButton", "로비 복귀",
            context.CreamButtonSprite, context.Font, new Vector2(360f, -820f), new Vector2(190f, 100f));
        SetBottom(returnLobby.GetComponent<RectTransform>(), new Vector2(190f, 100f), new Vector2(360f, 88f));
        readyPanel.gameObject.SetActive(false);

        Image battlePanel = CreateImage(background.transform, "BattlePanel", null, Cream);
        SetStretch(battlePanel.rectTransform, Vector2.zero, Vector2.zero);
        TMP_Text battleHeading = CreateText(battlePanel.transform, "PreviewHeading",
            "I. PvP BATTLE HUD · LOCAL PREVIEW", context.Font, 27f, MutedBrown, FontStyles.Bold);
        SetTop(battleHeading.rectTransform, new Vector2(980f, 54f), new Vector2(0f, -22f));

        Image turnCard = CreateImage(battlePanel.transform, "TurnCard", context.CardSprite, Color.white);
        SetTop(turnCard.rectTransform, new Vector2(350f, 120f), new Vector2(-205f, -92f));
        TMP_Text battleTurn = CreateText(turnCard.transform, "TurnCountText", "TURN 1",
            context.Font, 36f, Gold, FontStyles.Bold);
        SetStretch(battleTurn.rectTransform, new Vector2(25f, 15f), new Vector2(-25f, -15f));
        Image timerCard = CreateImage(battlePanel.transform, "TimerCard", context.CardSprite, Color.white);
        SetTop(timerCard.rectTransform, new Vector2(260f, 120f), new Vector2(230f, -92f));
        TMP_Text battleTimer = CreateText(timerCard.transform, "TimerText", "60",
            context.Font, 50f, Brown, FontStyles.Bold);
        SetStretch(battleTimer.rectTransform, new Vector2(25f, 10f), new Vector2(-25f, -10f));

        CreateBattlePreviewPlayerCard(battlePanel.transform, "MyBattleCard",
            "나 · 황금검 루나", "용맹한 도전자", new Vector2(-245f, -245f),
            new Color32(0x58, 0xAD, 0x62, 0xFF), context,
            out Image myBattleHpBar, out TMP_Text myBattleHpText);
        CreateBattlePreviewPlayerCard(battlePanel.transform, "OpponentBattleCard",
            "상대 · 불꽃기사 하람", "불굴의 결투가", new Vector2(245f, -245f),
            new Color32(0xD9, 0x62, 0x55, 0xFF), context,
            out Image opponentBattleHpBar, out TMP_Text opponentBattleHpText);

        TMP_Text battleGuide = CreateText(battlePanel.transform, "Guide",
            "로컬 HUD 표시만 갱신합니다 · 실제 전투 계산/Photon/API 미호출",
            context.Font, 25f, MutedBrown, FontStyles.Bold);
        SetBottom(battleGuide.rectTransform, new Vector2(940f, 58f), new Vector2(0f, 245f));
        Button hpTest = CreateButton(battlePanel.transform, "HpTestButton", "HP 100→70→30%",
            context.CreamButtonSprite, context.Font, Vector2.zero, new Vector2(300f, 100f));
        SetBottom(hpTest.GetComponent<RectTransform>(), new Vector2(300f, 100f), new Vector2(-315f, 130f));
        Button turnTest = CreateButton(battlePanel.transform, "TurnTestButton", "턴 +1",
            context.CreamButtonSprite, context.Font, Vector2.zero, new Vector2(200f, 100f));
        SetBottom(turnTest.GetComponent<RectTransform>(), new Vector2(200f, 100f), new Vector2(-45f, 130f));
        Button timerTest = CreateButton(battlePanel.transform, "TimerTestButton", "60→30→10초",
            context.CreamButtonSprite, context.Font, Vector2.zero, new Vector2(250f, 100f));
        SetBottom(timerTest.GetComponent<RectTransform>(), new Vector2(250f, 100f), new Vector2(205f, 130f));
        Button battleReturnLobby = CreateButton(battlePanel.transform, "ReturnLobbyTestButton", "로비 복귀",
            context.GoldButtonSprite, context.Font, Vector2.zero, new Vector2(760f, 104f));
        SetBottom(battleReturnLobby.GetComponent<RectTransform>(), new Vector2(760f, 104f), new Vector2(0f, 35f));
        battlePanel.gameObject.SetActive(false);

        Image dim = CreateImage(background.transform, "DailyLimitPopupDim", null, Dim);
        SetStretch(dim.rectTransform, Vector2.zero, Vector2.zero);
        Image popup = CreateImage(dim.transform, "DailyLimitPopup", context.CardSprite, Color.white);
        SetCentered(popup.rectTransform, new Vector2(820f, 540f), Vector2.zero);
        TMP_Text popupTitle = CreateText(popup.transform, "Title", "오늘의 PvP 도전 완료",
            context.Font, 43f, Brown, FontStyles.Bold);
        SetCentered(popupTitle.rectTransform, new Vector2(700f, 80f), new Vector2(0f, 145f));
        TMP_Text popupMessage = CreateText(popup.transform, "Message",
            "일일 PvP 횟수를 모두 사용했습니다.\n내일 다시 멋진 승부를 시작해보세요!",
            context.Font, 32f, Alert, FontStyles.Bold);
        popupMessage.enableWordWrapping = true;
        popupMessage.overflowMode = TextOverflowModes.Ellipsis;
        SetCentered(popupMessage.rectTransform, new Vector2(690f, 160f), new Vector2(0f, 25f));
        Button popupClose = CreateButton(popup.transform, "CloseButton", "확인",
            context.GoldButtonSprite, context.Font, new Vector2(0f, -160f), new Vector2(340f, 108f));
        dim.gameObject.SetActive(false);
        equipmentSelectDim.transform.SetAsLastSibling();

        PvpRankingPreviewController controller = Undo.AddComponent<PvpRankingPreviewController>(root);
        SerializedObject serialized = new SerializedObject(controller);
        SetReference(serialized, "matchButton", match);
        SetReference(serialized, "zeroRemainButton", zero);
        SetReference(serialized, "popupCloseButton", popupClose);
        SetReference(serialized, "limitPopup", dim.gameObject);
        SetReference(serialized, "remainText", remainText);
        SetReference(serialized, "localStatusText", status);
        SetReference(serialized, "remainCardImage", remainCard);
        SetReference(serialized, "matchButtonImage", match.GetComponent<Image>());
        SetReference(serialized, "normalMatchSprite", context.GoldButtonSprite);
        SetReference(serialized, "disabledMatchSprite", context.CreamButtonSprite);
        SetReference(serialized, "rankingScroll", scroll);
        SetArray(serialized, "lobbyObjects", new[]
        {
            heading.gameObject, title.gameObject, listCard.gameObject, info.gameObject,
            status.gameObject, match.gameObject, zero.gameObject
        });
        SetReference(serialized, "loadingPanel", loadingPanel.gameObject);
        SetReference(serialized, "participantChangeButton", participantChange);
        SetReference(serialized, "cancelMatchButton", cancelMatch);
        SetReference(serialized, "matchCompleteButton", matchComplete);
        SetReference(serialized, "participantCountText", participantCount);
        SetReference(serialized, "readyPanel", readyPanel.gameObject);
        SetArray(serialized, "attributeButtons", attributeButtons);
        SetArray(serialized, "consumableButtons", consumableButtons);
        SetArray(serialized, "equipmentButtons", equipmentButtons);
        SetReference(serialized, "equipmentSelectPanel", equipmentSelectDim.gameObject);
        SetReference(serialized, "equipmentTypeText", equipmentType);
        SetArray(serialized, "equipmentItemButtons", equipmentItemButtons);
        SetArray(serialized, "equipmentItemNameTexts", equipmentItemNames);
        SetArray(serialized, "equipmentItemStatTexts", equipmentItemStats);
        SetArray(serialized, "equipmentItemStateTexts", equipmentItemStates);
        SetReference(serialized, "equipmentSelectCloseButton", equipmentSelectClose);
        SetReference(serialized, "equipmentDetailPopup", equipmentDetail.gameObject);
        SetReference(serialized, "equipmentDetailNameText", detailName);
        SetReference(serialized, "equipmentDetailStatText", detailStat);
        SetReference(serialized, "equipmentDetailDescriptionText", detailDescription);
        SetReference(serialized, "equipmentActionButton", equipmentAction);
        SetReference(serialized, "equipmentActionButtonText", equipmentActionText);
        SetReference(serialized, "equipmentDetailCloseButton", equipmentDetailClose);
        SetReference(serialized, "readyButton", ready);
        SetReference(serialized, "battleTestButton", battleTest);
        SetReference(serialized, "returnLobbyButton", returnLobby);
        SetReference(serialized, "myReadyText", myReady);
        SetReference(serialized, "opponentReadyText", opponentReady);
        SetReference(serialized, "readyGuideText", readyGuide);
        SetReference(serialized, "battlePanel", battlePanel.gameObject);
        SetReference(serialized, "battleReturnLobbyButton", battleReturnLobby);
        SetReference(serialized, "hpTestButton", hpTest);
        SetReference(serialized, "turnTestButton", turnTest);
        SetReference(serialized, "timerTestButton", timerTest);
        SetReference(serialized, "myBattleHpBar", myBattleHpBar);
        SetReference(serialized, "opponentBattleHpBar", opponentBattleHpBar);
        SetReference(serialized, "myBattleHpText", myBattleHpText);
        SetReference(serialized, "opponentBattleHpText", opponentBattleHpText);
        SetReference(serialized, "battleTurnText", battleTurn);
        SetReference(serialized, "battleTimerText", battleTimer);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        float preferredHeight = LayoutUtility.GetPreferredHeight(content);
        if (preferredHeight > 0f) content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
        root.SetActive(false);
        Dirty(root);
    }

    private static void CreatePreviewRankingRow(Transform parent, int rank, string playerName,
        int score, Context context)
    {
        GameObject row = new GameObject($"RankingRow_{rank:00}", typeof(RectTransform),
            typeof(Image), typeof(LayoutElement));
        Undo.RegisterCreatedObjectUndo(row, "Create PvP preview ranking row");
        row.transform.SetParent(parent, false);
        Image image = row.GetComponent<Image>();
        image.sprite = context.CreamButtonSprite;
        image.type = Image.Type.Sliced;
        image.color = rank <= 3 ? new Color32(0xFF, 0xF0, 0xC9, 0xFF) : CreamLight;
        LayoutElement element = row.GetComponent<LayoutElement>();
        element.preferredHeight = 112f;
        element.minHeight = 112f;

        TMP_Text rankText = CreateText(row.transform, "Rank", rank.ToString(), context.Font,
            rank <= 3 ? 34f : 30f, rank <= 3 ? Gold : Brown, FontStyles.Bold);
        SetLeft(rankText.rectTransform, new Vector2(100f, 80f), new Vector2(20f, 0f));
        Image titleBadge = CreateImage(row.transform, "TitleImage", null,
            rank % 3 == 0 ? new Color32(0xD9, 0xA4, 0x42, 0xFF) : new Color32(0xC9, 0xA8, 0x74, 0xFF));
        SetLeft(titleBadge.rectTransform, new Vector2(64f, 64f), new Vector2(128f, 0f));
        TMP_Text badgeText = CreateText(titleBadge.transform, "BadgeText", rank % 2 == 0 ? "별" : "검",
            context.Font, 22f, CreamLight, FontStyles.Bold);
        SetStretch(badgeText.rectTransform, Vector2.zero, Vector2.zero);
        TMP_Text name = CreateText(row.transform, "PlayerName", playerName, context.Font,
            29f, Brown, FontStyles.Bold);
        name.overflowMode = TextOverflowModes.Ellipsis;
        name.enableWordWrapping = false;
        SetLeft(name.rectTransform, new Vector2(450f, 78f), new Vector2(215f, 0f));
        TMP_Text scoreText = CreateText(row.transform, "Score", score.ToString("N0") + " P",
            context.Font, 29f, Brown, FontStyles.Bold);
        SetRight(scoreText.rectTransform, new Vector2(190f, 78f), new Vector2(-24f, 0f));
    }

    private static Image CreateBattlePreviewPlayerCard(Transform parent, string name,
        string playerName, string title, Vector2 position, Color hpColor, Context context,
        out Image hpBar, out TMP_Text hpText)
    {
        Image card = CreateImage(parent, name, context.CardSprite, Color.white);
        SetTop(card.rectTransform, new Vector2(455f, 1180f), position);
        TMP_Text nameText = CreateText(card.transform, "NameText", playerName,
            context.Font, 34f, Brown, FontStyles.Bold);
        SetTop(nameText.rectTransform, new Vector2(390f, 64f), new Vector2(0f, -42f));

        Image titleFrame = CreateImage(card.transform, "TitleImage", context.GoldButtonSprite, Color.white);
        SetTop(titleFrame.rectTransform, new Vector2(330f, 92f), new Vector2(0f, -120f));
        TMP_Text titleText = CreateText(titleFrame.transform, "TitleText", title,
            context.Font, 25f, Brown, FontStyles.Bold);
        SetStretch(titleText.rectTransform, new Vector2(18f, 12f), new Vector2(-18f, -12f));

        Image characterImage = CreateImage(card.transform, "CharacterImage", null, CreamLight);
        SetTop(characterImage.rectTransform, new Vector2(350f, 650f), new Vector2(0f, -235f));
        TMP_Text characterLabel = CreateText(characterImage.transform, "Placeholder",
            "캐릭터 Image\n(기존 Sprite 영역)", context.Font, 27f, MutedBrown, FontStyles.Bold);
        SetStretch(characterLabel.rectTransform, new Vector2(20f, 20f), new Vector2(-20f, -20f));

        Image hpBackground = CreateImage(card.transform, "HpBarBackground",
            context.CreamButtonSprite, Brown);
        SetBottom(hpBackground.rectTransform, new Vector2(370f, 92f), new Vector2(0f, 125f));
        hpBar = CreateImage(hpBackground.transform, "HpBar", context.CreamButtonSprite, hpColor);
        SetStretch(hpBar.rectTransform, new Vector2(14f, 14f), new Vector2(-14f, -14f));
        hpBar.type = Image.Type.Filled;
        hpBar.fillMethod = Image.FillMethod.Horizontal;
        hpBar.fillOrigin = 0;
        hpBar.fillAmount = 1f;
        hpText = CreateText(hpBackground.transform, "HpText", "1000 / 1000",
            context.Font, 27f, CreamLight, FontStyles.Bold);
        SetStretch(hpText.rectTransform, new Vector2(18f, 8f), new Vector2(-18f, -8f));
        return card;
    }

    private static RectTransform CreatePreviewButtonRow(Transform parent, string name,
        Vector2 size, Vector2 position, float spacing)
    {
        GameObject rowObject = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        Undo.RegisterCreatedObjectUndo(rowObject, "Create PvP preview button row");
        rowObject.transform.SetParent(parent, false);
        RectTransform row = rowObject.GetComponent<RectTransform>();
        SetCentered(row, size, position);
        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return row;
    }

    private static Button CreateReadyPreviewSlot(Transform parent, string name, string label,
        int placeholderIndex, Vector2 position, Vector2 size, Context context)
    {
        Button button = CreateButton(parent, name, label, context.CreamButtonSprite,
            context.Font, position, size);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.fontSize = 23f;
            text.rectTransform.offsetMin = new Vector2(10f, 8f);
            text.rectTransform.offsetMax = new Vector2(-10f, -58f);
        }
        TMP_Text icon = CreateText(button.transform, "PlaceholderIcon", "◆ " + placeholderIndex,
            context.Font, 25f, Gold, FontStyles.Bold);
        SetBottom(icon.rectTransform, new Vector2(size.x - 24f, 48f), new Vector2(0f, 10f));
        return button;
    }

    private static void CreateInfoColumn(Transform parent, string label, string value, float x,
        Context context, out TMP_Text valueText)
    {
        Image column = CreateImage(parent, label == "남은 PvP" ? "RemainColumn" : label,
            context.CreamButtonSprite, CreamLight);
        SetCentered(column.rectTransform, new Vector2(270f, 170f), new Vector2(x, 0f));
        TMP_Text labelText = CreateText(column.transform, "Label", label, context.Font,
            25f, MutedBrown, FontStyles.Bold);
        SetCentered(labelText.rectTransform, new Vector2(230f, 52f), new Vector2(0f, 42f));
        valueText = CreateText(column.transform, "Value", value, context.Font,
            35f, Brown, FontStyles.Bold);
        SetCentered(valueText.rectTransform, new Vector2(230f, 62f), new Vector2(0f, -35f));
    }

    private static List<string> ValidateAppliedLayout(Context context, bool requirePreview)
    {
        List<string> errors = new List<string>(context.Errors);
        if (!context.IsValid) return errors;
        if (context.RankingTitle.font != context.Font || context.RankingTitle.overflowMode != TextOverflowModes.Ellipsis)
            errors.Add("랭킹 제목 Font/Overflow 설정이 올바르지 않습니다.");
        if (!context.RankingList.vertical || context.RankingList.horizontal)
            errors.Add("RankingList가 세로 전용 ScrollRect가 아닙니다.");
        if (context.RankingContent.GetComponent<VerticalLayoutGroup>() == null ||
            context.RankingContent.GetComponent<ContentSizeFitter>() == null)
            errors.Add("Ranking Content에 VerticalLayoutGroup/ContentSizeFitter가 없습니다.");
        if (context.LoadingTitle.font != context.Font ||
            context.LoadingTitle.overflowMode != TextOverflowModes.Ellipsis)
            errors.Add("Loading 제목 Font/Overflow 설정이 올바르지 않습니다.");
        if (context.Loading.transform.Find("MatchingProgressPanel") == null)
            errors.Add("Loading 중앙 MatchingProgressPanel이 없습니다.");
        if (context.LoadingSpinner == null || context.CancelButton == null)
            errors.Add("기존 LoadingSpinner/CancelButton 참조 대상을 찾을 수 없습니다.");
        if (context.Ready.GetComponent<Image>() == null ||
            Find(context.Ready.transform, "IsReadyPanel")?.GetComponent<Image>() == null ||
            Find(context.Ready.transform, "OppInfoPanel")?.GetComponent<Image>() == null ||
            Find(context.Ready.transform, "AttributeButtons")?.GetComponent<Image>() == null ||
            Find(context.Ready.transform, "ConsumableItemButtons")?.GetComponent<Image>() == null ||
            Find(context.Ready.transform, "EquipItemButtons")?.GetComponent<Image>() == null)
            errors.Add("Ready RPG 카드 레이아웃이 적용되지 않았습니다.");
        if (context.AttributeButtons.Length != 4 || context.ConsumableButtons.Length != 3 ||
            context.EquipmentButtons.Length != 4)
            errors.Add("Ready 기존 버튼 배열 수(4/3/4)가 보존되지 않았습니다.");
        if (context.EquipSelectPanel.GetComponent<Image>() == null ||
            context.EquipItemDetailPopup.GetComponent<Image>() == null ||
            context.EquipItemSlotParent.GetComponent<LayoutGroup>() == null)
            errors.Add("실제 장비 선택/상세 팝업 RPG 카드 또는 목록 Layout이 적용되지 않았습니다.");
        if (context.Battle.GetComponent<Image>() == null ||
            context.BattleMyName.font != context.Font || context.BattleOpponentName.font != context.Font ||
            context.TimerText.font != context.Font || context.TurnCountText.font != context.Font ||
            context.MyHpText.font != context.Font || context.OpponentHpText.font != context.Font)
            errors.Add("Battle HUD 크림 배경 또는 NotoSansKR 텍스트 스타일이 적용되지 않았습니다.");

        GameObject preview = FindRoot(context.Scene, PreviewRootName);
        PvpRankingPreviewController controller = preview != null
            ? preview.GetComponent<PvpRankingPreviewController>() : null;
        if (requirePreview && (preview == null || controller == null))
            errors.Add("I Preview Root/Controller가 없습니다.");
        if (preview != null)
        {
            Transform content = Find(preview.transform,
                "PreviewBackground/RankingCard/Viewport/Content");
            if (content == null || content.childCount != 20)
                errors.Add("I Preview 가상 랭킹 Row가 20개가 아닙니다.");
            else
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(content as RectTransform);
                if ((content as RectTransform).rect.height <= 920f)
                    errors.Add("I Preview Content 높이가 Viewport보다 작아 스크롤 범위가 없습니다.");
                foreach (TMP_Text text in content.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.font != context.Font || text.overflowMode != TextOverflowModes.Ellipsis)
                        errors.Add("I Preview Row Font/Overflow 설정 오류: " + text.name);
                }
            }
            if (Find(preview.transform, "PreviewBackground/DailyLimitPopupDim/DailyLimitPopup/CloseButton") == null ||
                Find(preview.transform, "PreviewBackground/ZeroRemainTestButton") == null)
                errors.Add("I Preview 잔여 0 테스트 또는 제한 팝업 구조가 없습니다.");
            Transform loading = Find(preview.transform, "PreviewBackground/LoadingPanel");
            if (loading == null || loading.Find("MatchingProgressCard/ParticipantChangeButton") == null ||
                loading.Find("MatchingProgressCard/CancelButton") == null ||
                loading.Find("MatchingProgressCard/MatchCompleteTestButton") == null ||
                loading.Find("MatchingProgressCard/ParticipantCard/Count") == null)
                errors.Add("I Preview 로컬 Loading/참가자 변경/취소 구조가 없습니다.");
            Transform ready = Find(preview.transform, "PreviewBackground/ReadyPanel");
            if (ready == null || ready.Find("AttributeCard") == null ||
                ready.Find("ConsumableCard") == null || ready.Find("EquipmentCard") == null ||
                ready.Find("ReadyButton") == null || ready.Find("ReturnLobbyTestButton") == null)
                errors.Add("I Preview 로컬 Ready 선택/준비/로비 복귀 구조가 없습니다.");
            else
            {
                RectTransform consumableCard = ready.Find("ConsumableCard") as RectTransform;
                RectTransform equipmentCard = ready.Find("EquipmentCard") as RectTransform;
                ValidatePreviewButtonRow(consumableCard,
                    consumableCard.Find("ButtonRow") as RectTransform, 3, "소비 아이템", errors);
                ValidatePreviewButtonRow(equipmentCard,
                    equipmentCard.Find("ButtonRow") as RectTransform, 4, "전투 장비", errors);
            }
            Transform battle = Find(preview.transform, "PreviewBackground/BattlePanel");
            if (battle == null || battle.Find("MyBattleCard/CharacterImage") == null ||
                battle.Find("OpponentBattleCard/CharacterImage") == null ||
                battle.Find("MyBattleCard/HpBarBackground/HpBar") == null ||
                battle.Find("OpponentBattleCard/HpBarBackground/HpBar") == null ||
                battle.Find("TurnCard/TurnCountText") == null || battle.Find("TimerCard/TimerText") == null ||
                battle.Find("HpTestButton") == null || battle.Find("TurnTestButton") == null ||
                battle.Find("TimerTestButton") == null || battle.Find("ReturnLobbyTestButton") == null)
                errors.Add("I Preview Battle HUD/HP/턴/타이머/로비 복귀 테스트 구조가 없습니다.");
            else
            {
                Image myPreviewHp = battle.Find("MyBattleCard/HpBarBackground/HpBar").GetComponent<Image>();
                Image opponentPreviewHp = battle.Find(
                    "OpponentBattleCard/HpBarBackground/HpBar").GetComponent<Image>();
                if (!IsValidPreviewHpFill(myPreviewHp) || !IsValidPreviewHpFill(opponentPreviewHp))
                    errors.Add("I Preview HP Fill은 Sprite가 있는 Horizontal Filled Image여야 합니다.");
            }
            Transform equipmentSelect = Find(preview.transform,
                "PreviewBackground/EquipmentSelectDim/EquipmentSelectPanel");
            Transform equipmentListContent = Find(equipmentSelect,
                "EquipmentList/Viewport/Content");
            if (equipmentSelect == null || equipmentListContent == null ||
                equipmentListContent.childCount != 5 ||
                Find(preview.transform, "PreviewBackground/EquipmentSelectDim/EquipmentDetailPopup") == null)
                errors.Add("I Preview 장비 선택 목록(5개)/상세 팝업 구조가 없습니다.");
            else
            {
                RectTransform contentRect = equipmentListContent as RectTransform;
                RectTransform viewportRect = equipmentSelect.Find(
                    "EquipmentList/Viewport") as RectTransform;
                ScrollRect equipmentScroll = equipmentSelect.Find("EquipmentList")?
                    .GetComponent<ScrollRect>();
                contentRect.ForceUpdateRectTransforms();
                viewportRect?.ForceUpdateRectTransforms();
                float requiredHeight = GetVerticalLayoutPreferredHeight(contentRect);
                float viewportHeight = viewportRect != null ? viewportRect.rect.height : 0f;
                bool requiresScroll = requiredHeight > viewportHeight + 0.5f;
                if (viewportRect == null || viewportHeight <= 0f)
                    errors.Add("I Preview 장비 목록 Viewport의 실제 높이를 확인할 수 없습니다.");
                else if (Mathf.Abs(contentRect.rect.height - requiredHeight) > 1f)
                    errors.Add($"I Preview 장비 목록 Content 높이가 Layout 요구값과 다릅니다. " +
                               $"현재 {contentRect.rect.height:F1}, 필요 {requiredHeight:F1}");
                else if (equipmentScroll == null || equipmentScroll.content != contentRect ||
                         equipmentScroll.viewport != viewportRect || !equipmentScroll.vertical)
                    errors.Add("I Preview 장비 목록 ScrollRect의 Content/Viewport 연결이 올바르지 않습니다.");
                else if (requiresScroll && contentRect.rect.height <= viewportHeight)
                    errors.Add("I Preview 장비 목록이 Viewport보다 길지만 스크롤 범위가 없습니다.");
            }
        }
        return errors;
    }

    private static void ValidatePreviewButtonRow(RectTransform card, RectTransform row,
        int expectedChildren, string label, List<string> errors)
    {
        if (card == null || row == null || row.GetComponent<HorizontalLayoutGroup>() == null)
        {
            errors.Add($"I Preview {label} ButtonRow/HorizontalLayoutGroup이 없습니다.");
            return;
        }
        if (row.childCount != expectedChildren)
            errors.Add($"I Preview {label} ButtonRow 버튼 수가 {expectedChildren}개가 아닙니다.");

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(row);
        Bounds rowBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(card, row);
        if (rowBounds.min.y < card.rect.yMin + 12f || rowBounds.max.y > card.rect.yMax - 62f ||
            rowBounds.min.x < card.rect.xMin + 24f || rowBounds.max.x > card.rect.xMax - 24f)
            errors.Add($"I Preview {label} ButtonRow가 제목 영역 또는 카드 경계를 침범합니다.");

        foreach (RectTransform child in row.Cast<Transform>().OfType<RectTransform>())
        {
            Bounds childBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(row, child);
            if (childBounds.min.x < row.rect.xMin || childBounds.max.x > row.rect.xMax ||
                childBounds.min.y < row.rect.yMin || childBounds.max.y > row.rect.yMax)
                errors.Add($"I Preview {label} 버튼 '{child.name}'이 ButtonRow 경계를 벗어납니다.");
        }
    }

    private static bool IsValidPreviewHpFill(Image image)
    {
        return image != null && image.sprite != null && image.type == Image.Type.Filled &&
               image.fillMethod == Image.FillMethod.Horizontal && image.fillOrigin == 0;
    }

    private static float GetVerticalLayoutPreferredHeight(RectTransform content)
    {
        VerticalLayoutGroup layout = content != null
            ? content.GetComponent<VerticalLayoutGroup>() : null;
        if (layout == null) return content != null ? content.rect.height : 0f;

        float height = layout.padding.top + layout.padding.bottom;
        int includedChildren = 0;
        foreach (RectTransform child in content.Cast<Transform>().OfType<RectTransform>())
        {
            if (!child.gameObject.activeSelf) continue;
            LayoutElement element = child.GetComponent<LayoutElement>();
            float childHeight = element != null
                ? Mathf.Max(element.minHeight, element.preferredHeight)
                : LayoutUtility.GetPreferredHeight(child);
            height += Mathf.Max(0f, childHeight);
            includedChildren++;
        }
        if (includedChildren > 1) height += layout.spacing * (includedChildren - 1);
        return height;
    }

    private static void IsolateScene(Scene scene, GameObject previewRoot,
        PvpRankingPreviewController controller)
    {
        GameObject[] objects = scene.GetRootGameObjects()
            .Where(root => root != previewRoot && !IsPreviewRoot(root) && root.activeSelf &&
                           root.GetComponent<EventSystem>() == null &&
                           root.GetComponentInChildren<Camera>(true) == null &&
                           root.GetComponentInChildren<AudioListener>(true) == null)
            .OrderBy(root => root.GetInstanceID()).ToArray();
        bool[] states = objects.Select(item => item.activeSelf).ToArray();
        SerializedObject serialized = new SerializedObject(controller);
        SetArray(serialized, "isolatedGameObjects", objects);
        SetBoolArray(serialized, "isolatedGameObjectActiveStates", states);
        SetArray(serialized, "isolatedBehaviours", Array.Empty<Behaviour>());
        SetBoolArray(serialized, "isolatedBehaviourEnabledStates", Array.Empty<bool>());
        serialized.ApplyModifiedProperties();
        Dirty(controller);
        foreach (GameObject target in objects)
            SetActive(target, false, "Isolate scene object for PvP preview");
    }

    public static void RestorePvpPreviewState()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) return;
        GameObject root = FindRoot(scene, PreviewRootName);
        PvpRankingPreviewController controller =
            root != null ? root.GetComponent<PvpRankingPreviewController>() : null;
        if (root == null || controller == null || (!root.activeSelf && !HasSnapshot(controller))) return;

        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty targets = serialized.FindProperty("isolatedGameObjects");
        SerializedProperty states = serialized.FindProperty("isolatedGameObjectActiveStates");
        if (targets != null && states != null && targets.arraySize == states.arraySize)
        {
            for (int index = 0; index < targets.arraySize; index++)
            {
                GameObject target = targets.GetArrayElementAtIndex(index).objectReferenceValue as GameObject;
                if (target != null)
                    SetActive(target, states.GetArrayElementAtIndex(index).boolValue,
                        "Restore PvP preview state");
            }
        }
        ClearSnapshot(controller);
        SetActive(root, false, "Restore PvP preview root");
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void RestoreOtherPreviewStates()
    {
        ShopGachaUiRenewalEditor.RestoreShopPreviewState();
        InventoryMainUiRenewalEditor.RestoreInventoryPreviewState();
        CharacterMainUiRenewalEditor.RestoreCharacterPreviewState();
        PhotoVerificationPopupUiRenewalEditor.RestorePhotoPreviewState();
        AchievedAmountOverlayUiRenewalEditor.RestoreNumberPreviewState();
        HabitGoalFormUiRenewalEditor.RestoreHabitMainPreviewState();
    }

    private static bool HasSnapshot(PvpRankingPreviewController controller)
    {
        SerializedProperty objects = new SerializedObject(controller).FindProperty("isolatedGameObjects");
        return objects != null && objects.arraySize > 0;
    }

    private static void ClearSnapshot(PvpRankingPreviewController controller)
    {
        SerializedObject serialized = new SerializedObject(controller);
        foreach (string name in new[]
                 {
                     "isolatedBehaviours", "isolatedBehaviourEnabledStates",
                     "isolatedGameObjects", "isolatedGameObjectActiveStates"
                 })
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null) property.arraySize = 0;
        }
        serialized.ApplyModifiedProperties();
        Dirty(controller);
    }

    private static void RestoreWhenAnotherPreviewIsSelected()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        GameObject root = scene.path == ScenePath ? FindRoot(scene, PreviewRootName) : null;
        if (root == null || !root.activeSelf) return;
        if (OtherPreviewRoots.Any(name =>
            {
                GameObject other = FindRoot(scene, name);
                return other != null && other.activeSelf;
            }))
            RestorePvpPreviewState();
    }

    private static void RestoreBeforeSceneSave(Scene scene, string path)
    {
        if (scene.path == ScenePath && scene == SceneManager.GetActiveScene())
            RestorePvpPreviewState();
    }

    private static Dictionary<MonoBehaviour, string> CaptureProtectedManagers()
    {
        string[] names = { "BattleUIManager", "RankingboardManager", "MainTabManager" };
        return UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true)
            .Where(item => names.Contains(item.GetType().Name))
            .ToDictionary(item => item, item => EditorJsonUtility.ToJson(item));
    }

    private static void AssertProtectedManagersUnchanged(Dictionary<MonoBehaviour, string> states)
    {
        foreach (KeyValuePair<MonoBehaviour, string> pair in states)
        {
            if (pair.Key != null && pair.Value != EditorJsonUtility.ToJson(pair.Key))
                throw new InvalidOperationException(pair.Key.GetType().Name +
                    "의 SerializeField 또는 UnityEvent 참조가 변경되었습니다.");
        }
    }

    private static T Get<T>(Transform root, string path, List<string> errors) where T : Component
    {
        Transform target = Find(root, path);
        T component = target != null ? target.GetComponent<T>() : null;
        if (component == null) errors.Add($"{path}의 {typeof(T).Name}을 찾을 수 없습니다.");
        return component;
    }

    private static T GetReference<T>(SerializedObject serialized, string name)
        where T : UnityEngine.Object
    {
        SerializedProperty property = serialized.FindProperty(name);
        return property != null ? property.objectReferenceValue as T : null;
    }

    private static T[] GetReferenceArray<T>(SerializedObject serialized, string name)
        where T : UnityEngine.Object
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null || !property.isArray) return Array.Empty<T>();
        T[] values = new T[property.arraySize];
        for (int index = 0; index < property.arraySize; index++)
            values[index] = property.GetArrayElementAtIndex(index).objectReferenceValue as T;
        return values;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(item, "Create PvP renewal image");
        item.transform.SetParent(parent, false);
        Image image = item.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        return image;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value,
        TMP_FontAsset font, float size, Color color, FontStyles style)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(item, "Create PvP renewal text");
        item.transform.SetParent(parent, false);
        TMP_Text text = item.GetComponent<TMP_Text>();
        StyleText(text, value, font, size, color, style);
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, Sprite sprite,
        TMP_FontAsset font, Vector2 position, Vector2 size)
    {
        Image image = CreateImage(parent, name, sprite, Color.white);
        Button button = Undo.AddComponent<Button>(image.gameObject);
        button.targetGraphic = image;
        SetCentered(image.rectTransform, size, position);
        TMP_Text text = CreateText(image.transform, "Text", label, font, 31f, Brown, FontStyles.Bold);
        SetStretch(text.rectTransform, new Vector2(18f, 10f), new Vector2(-18f, -10f));
        return button;
    }

    private static void StyleButton(Button button, string label, Sprite sprite,
        TMP_FontAsset font, float size)
    {
        if (button == null) return;
        StyleImage(button.GetComponent<Image>(), sprite, Color.white, Image.Type.Sliced);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        StyleText(text, label, font, size, Brown, FontStyles.Bold);
        if (text != null)
        {
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }
    }

    private static void StyleImage(Image image, Sprite sprite, Color color, Image.Type type)
    {
        if (image == null) return;
        Record(image, "Style PvP renewal image");
        image.sprite = sprite;
        image.color = color;
        image.type = type;
        image.preserveAspect = false;
        Dirty(image);
        PrefabUtility.RecordPrefabInstancePropertyModifications(image);
    }

    private static void StyleText(TMP_Text text, string value, TMP_FontAsset font,
        float size, Color color, FontStyles style)
    {
        if (text == null) return;
        Record(text, "Style PvP renewal text");
        if (value != null) text.text = value;
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        Dirty(text);
        PrefabUtility.RecordPrefabInstancePropertyModifications(text);
    }

    private static void SetInfoColumn(RectTransform rect, float x)
    {
        if (rect == null) return;
        SetCentered(rect, new Vector2(280f, 170f), new Vector2(x, 0f));
    }

    private static void SetStretch(RectTransform rect, Vector2 minOffset, Vector2 maxOffset)
    {
        if (rect == null) return;
        Record(rect, "Layout PvP renewal rect");
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = minOffset;
        rect.offsetMax = maxOffset;
        Dirty(rect);
        PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
    }

    private static void SetTopStretch(RectTransform rect, float top)
    {
        if (rect == null) return;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -top);
        rect.sizeDelta = new Vector2(0f, 0f);
    }

    private static void SetTop(RectTransform rect, Vector2 size, Vector2 position)
    {
        if (rect == null) return;
        Record(rect, "Layout PvP renewal top rect");
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Dirty(rect);
        PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
    }

    private static void SetBottom(RectTransform rect, Vector2 size, Vector2 position)
    {
        if (rect == null) return;
        Record(rect, "Layout PvP renewal bottom rect");
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Dirty(rect);
        PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
    }

    private static void SetCentered(RectTransform rect, Vector2 size, Vector2 position)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void SetLeft(RectTransform rect, Vector2 size, Vector2 position)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void SetRight(RectTransform rect, Vector2 size, Vector2 position)
    {
        if (rect == null) return;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void SetReference(SerializedObject serialized, string name, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null) property.objectReferenceValue = value;
    }

    private static void SetArray<T>(SerializedObject serialized, string name, T[] values)
        where T : UnityEngine.Object
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null) return;
        property.arraySize = values.Length;
        for (int index = 0; index < values.Length; index++)
            property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
    }

    private static void SetBoolArray(SerializedObject serialized, string name, bool[] values)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null) return;
        property.arraySize = values.Length;
        for (int index = 0; index < values.Length; index++)
            property.GetArrayElementAtIndex(index).boolValue = values[index];
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene.IsValid()
            ? scene.GetRootGameObjects().FirstOrDefault(item => item.name == name)
            : null;
    }

    private static Transform Find(Transform root, string path)
    {
        return root != null ? root.Find(path) : null;
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        if (root == null) return null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
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

    private static void Record(UnityEngine.Object target, string label)
    {
        if (target != null) Undo.RecordObject(target, label);
    }

    private static void Dirty(UnityEngine.Object target)
    {
        if (target != null) EditorUtility.SetDirty(target);
    }

    private sealed class Context
    {
        public Scene Scene;
        public GameObject Canvas;
        public GameObject Lobby;
        public TMP_FontAsset Font;
        public Sprite CardSprite;
        public Sprite CreamButtonSprite;
        public Sprite GoldButtonSprite;
        public TMP_Text RankingTitle;
        public ScrollRect RankingList;
        public RectTransform RankingContent;
        public RectTransform MyInfo;
        public Button MatchButton;
        public GameObject LimitPopup;
        public GameObject Loading;
        public RectTransform LoadingSpinner;
        public TMP_Text LoadingTitle;
        public TMP_Text ParticipantCount;
        public Button CancelButton;
        public GameObject Ready;
        public GameObject Battle;
        public Button CheckOpponentAttributeButton;
        public TMP_Text[] OpponentAttributeTexts = Array.Empty<TMP_Text>();
        public Button[] AttributeButtons = Array.Empty<Button>();
        public Button[] ConsumableButtons = Array.Empty<Button>();
        public Button[] EquipmentButtons = Array.Empty<Button>();
        public Button ReadyButton;
        public TMP_Text MyNameText;
        public TMP_Text OpponentNameText;
        public TMP_Text MyReadyText;
        public TMP_Text OpponentReadyText;
        public GameObject EquipSelectPanel;
        public GameObject EquipItemDetailPopup;
        public TMP_Text EquipTypeText;
        public Transform EquipItemSlotParent;
        public Button EquipSelectCloseButton;
        public Image EquipIcon;
        public TMP_Text EquipNameText;
        public TMP_Text EquipDescriptionText;
        public Button EquipActionButton;
        public TMP_Text EquipActionButtonText;
        public Button EquipDetailCloseButton;
        public TMP_Text BattleMyName;
        public TMP_Text BattleOpponentName;
        public Image MyBattleTitleImage;
        public Image OpponentBattleTitleImage;
        public Image MyBattleTitleVisual;
        public Image OpponentBattleTitleVisual;
        public TMP_Text TimerText;
        public Image MyHpBar;
        public Image OpponentHpBar;
        public TMP_Text MyHpText;
        public TMP_Text OpponentHpText;
        public Image MyCharacterImage;
        public Image OpponentCharacterImage;
        public TMP_Text TurnCountText;
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }
}
