using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PvpLoadingReadyIntegrationEditor
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string CanvasPrefabPath = "Assets/Resources/UIPrefabs/Canvas.prefab";
    private const string FontPath = "Assets/Login/Fonts/NotoSansKR Login SDF.asset";
    private const string CardSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/panel_rpg_card.png";
    private const string CreamButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_cream.png";
    private const string GoldButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_rpg.png";
    private const string MenuRoot = "Tools/HabitPVP/UI Renewal/MainScene/";
    private const string ReadyMenuRoot = MenuRoot + "15-D-7B Ready Final/";
    private const string MyReadyCardName = "__15D7B_MyReadyCard";
    private const string OpponentReadyCardName = "__15D7B_OpponentReadyCard";

    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 CreamLight = new Color32(0xFF, 0xF9, 0xEA, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x86, 0x68, 0x5D, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);
    private static readonly Color32 SelectedOutlineGreen = new Color32(0x82, 0xAD, 0x68, 0xFF);

    [MenuItem(ReadyMenuRoot + "Validate Ready Structure", false, 2400)]
    private static void ValidateReadyStructureMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid) return;

        Debug.Log(
            "[HabitPVP 15-D-7B] Ready 구조 검증 성공. Scene/Prefab을 변경하지 않았습니다.\n" +
            BuildReadyDiagnostics(context), context.Ready);
    }

    [MenuItem(ReadyMenuRoot + "Dry Run Ready", false, 2401)]
    private static void DryRunReadyMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid) return;

        Debug.Log(
            "[HabitPVP 15-D-7B] Ready Dry Run (변경/Dirty/저장 없음)\n" +
            "- 상단 카드: 기존 TMP를 유지하고 IsReadyPanel 내부에 동일 크기의 좌우 독립 카드 2개 생성\n" +
            "- 상단 텍스트: 기존 HorizontalLayoutGroup의 위치 재계산에서 TMP를 제외하고 " +
            "닉네임/상태를 위·아래 독립 영역에 고정\n" +
            "- 정찰: 기존 CheckOppAttrButton을 OppInfoPanel 중앙에 배치\n" +
            "- 특성: 기존 4개 버튼과 HorizontalLayoutGroup의 패딩/간격을 카드 내부 폭에 맞춤\n" +
            "- 아이콘: 특성 버튼 자식 Image의 Preserve Aspect 보장\n" +
            "- 보호: SerializeField, Button UnityEvent, activeSelf, Photon/전투 로직, Canvas.prefab 원본\n" +
            "- 장비: 기존 4개 버튼에 밝은 녹색 선택 상태용 Outline만 Scene Override로 추가\n" +
            "- 상태 원본: BattleUIManager가 InventoryItemResponse.IsEquipped를 읽어 색상을 갱신\n" +
            BuildReadyDiagnostics(context), context.Ready);
    }

    [MenuItem(ReadyMenuRoot + "Apply Ready (No Auto Save)", false, 2402)]
    private static void ApplyReadyLayoutMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid) return;

        ProtectionSnapshot snapshot = ProtectionSnapshot.Capture(context);
        Hash128 prefabHash = AssetDatabase.GetAssetDependencyHash(CanvasPrefabPath);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply 15-D-7B PvP Ready Final");

        try
        {
            ApplyReadyLayoutFix(context);
            snapshot.AssertUnchanged(context);

            if (AssetDatabase.GetAssetDependencyHash(CanvasPrefabPath) != prefabHash)
                throw new InvalidOperationException("Canvas.prefab 원본의 dependency hash가 변경되었습니다.");

            List<string> errors = ValidateReadyLayout(context);
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", errors));

            EditorSceneManager.MarkSceneDirty(context.Scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = context.Ready;
            Debug.Log(
                "[HabitPVP 15-D-7B] Ready 배치 적용 성공. MainScene은 dirty 상태이며 자동 저장하지 않았습니다. " +
                "Canvas.prefab 원본과 PvP 런타임 참조는 변경하지 않았습니다.", context.Ready);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(group);
            Debug.LogException(exception);
            Debug.LogError("[HabitPVP 15-D-7B] Ready Apply 실패. 이번 Scene 변경을 Undo로 복원했습니다.");
        }
    }

    [MenuItem(ReadyMenuRoot + "Validate Ready Design", false, 2403)]
    private static void ValidateReadyDesignMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid) return;

        List<string> errors = ValidateReadyLayout(context);
        if (errors.Count == 0)
        {
            Debug.Log(
                "[HabitPVP 15-D-7B] Ready 적용 디자인 검증 성공. 파일을 변경하지 않았습니다.\n" +
                BuildReadyDiagnostics(context), context.Ready);
            return;
        }

        foreach (string error in errors)
            Debug.LogError("[HabitPVP 15-D-7B] Ready 적용 디자인 오류: " + error, context.Ready);
    }

    [MenuItem(ReadyMenuRoot + "Validate Ready Structure", true)]
    [MenuItem(ReadyMenuRoot + "Dry Run Ready", true)]
    [MenuItem(ReadyMenuRoot + "Apply Ready (No Auto Save)", true)]
    [MenuItem(ReadyMenuRoot + "Validate Ready Design", true)]
    private static bool CanUseReadyMenus() => CanApply();

    [MenuItem(MenuRoot + "Validate PvP Loading Ready", false, 2280)]
    private static void ValidateMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid) return;

        List<string> styleErrors = ValidateAppliedStyle(context);
        Debug.Log(styleErrors.Count == 0
                ? "[HabitPVP MainScene] PvP Loading·Ready 구조/디자인 검증 성공. 변경하지 않았습니다.\n" +
                  BuildDiagnostics(context)
                : "[HabitPVP MainScene] PvP Loading·Ready 적용 전 구조 검증 성공. 변경하지 않았습니다.\n" +
                  BuildDiagnostics(context), context.Loading);
        foreach (string error in styleErrors)
            Debug.LogWarning("[HabitPVP MainScene] PvP Loading·Ready 스타일 미적용/불일치: " + error,
                context.Loading);
    }

    [MenuItem(MenuRoot + "Dry Run PvP Loading Ready", false, 2281)]
    private static void DryRunMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid)
        {
            Debug.LogError("[HabitPVP MainScene] PvP Loading·Ready Dry Run 중단: 구조 오류를 해결해 주세요.");
            return;
        }

        Debug.Log(
            "[HabitPVP MainScene] PvP Loading·Ready Dry Run (변경 없음)\n" +
            "- Loading: 크림 배경, RPG 진행 카드, 기존 MatchText/Spinner/참가자 수/취소 버튼 재배치\n" +
            "- Ready: 기존 준비 상태·상대 정보·속성·소비품·장비 패널과 Ready 버튼을 RPG 카드로 정렬\n" +
            "- 실제 Ready 전용 타이머와 캐릭터 표시 영역은 현재 Hierarchy/Manager에 없어 추가하지 않음\n" +
            "- 보호: BattleUIManager/PhotonManager/전투 Manager 참조, Button UnityEvent, 기존 activeSelf, " +
            "sibling 상대 순서, TapBar, Canvas.prefab, Lobby/Battle/Result\n" +
            "- 런타임 TapBar 정책은 BattleUIManager의 기존 숨김/복원을 그대로 사용\n" +
            BuildDiagnostics(context), context.Loading);
    }

    [MenuItem(MenuRoot + "Apply PvP Loading Ready", false, 2282)]
    private static void ApplyMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid) return;

        ProtectionSnapshot snapshot = ProtectionSnapshot.Capture(context);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply PvP Loading Ready UI Renewal");

        try
        {
            ApplyLoading(context);
            ApplyReady(context);
            snapshot.AssertUnchanged(context);

            List<string> errors = ValidateAppliedStyle(context);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));

            EditorSceneManager.MarkSceneDirty(context.Scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = context.Loading;
            Debug.Log(
                "[HabitPVP MainScene] PvP Loading·Ready 적용 성공. MainScene은 dirty 상태이며 자동 저장하지 않았습니다. " +
                "Canvas.prefab, Lobby, Battle, Result 및 PvP 런타임 로직은 변경하지 않았습니다.",
                context.Loading);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(group);
            EditorSceneManager.MarkSceneDirty(context.Scene);
            Debug.LogException(exception);
            Debug.LogError("[HabitPVP MainScene] PvP Loading·Ready Apply 실패. 이번 Scene 변경을 Undo로 복원했습니다.");
        }
    }

    [MenuItem(MenuRoot + "Apply PvP Loading Ready", true)]
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

        context.PvpTap = Find(context.Canvas, "PvPTap")?.gameObject;
        context.Lobby = Find(context.PvpTap, "Lobby")?.gameObject;
        context.Loading = Find(context.PvpTap, "Loading")?.gameObject;
        context.Ready = Find(context.PvpTap, "Ready")?.gameObject;
        context.Battle = Find(context.PvpTap, "Battle")?.gameObject;
        context.Result = Find(context.PvpTap, "Result")?.gameObject;
        context.TapBar = Find(context.Canvas, "TapBar")?.gameObject;
        context.LoadingTitle = Find(context.Loading, "MatchText")?.GetComponent<TMP_Text>();
        context.Spinner = Find(context.Loading, "LoadingSpinner") as RectTransform;
        context.ParticipantPanel = Find(context.Loading, "ParticipantsCountText") as RectTransform;
        context.ParticipantCount = Find(context.Loading,
            "ParticipantsCountText/CurrentParticipantsCount")?.GetComponent<TMP_Text>();
        context.CancelButton = Find(context.Loading, "CancelButton")?.GetComponent<Button>();
        context.ReadyStatusPanel = Find(context.Ready, "IsReadyPanel") as RectTransform;
        context.OpponentInfoPanel = Find(context.Ready, "OppInfoPanel") as RectTransform;
        context.AttributePanel = Find(context.Ready, "AttributeButtons") as RectTransform;
        context.ConsumablePanel = Find(context.Ready, "ConsumableItemButtons") as RectTransform;
        context.EquipmentPanel = Find(context.Ready, "EquipItemButtons") as RectTransform;
        context.BattleUiManager = FindUnique<BattleUIManager>(context);
        context.PhotonManager = FindUnique<PhotonManager>(context);
        context.BattleManager = FindUnique<BattleManager>(context);
        context.BattleBackendManager = FindUnique<BattleBackendManager>(context);
        context.MainTabManager = FindUnique<MainTabManager>(context);
        context.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        context.CardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardSpritePath);
        context.CreamButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CreamButtonSpritePath);
        context.GoldButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GoldButtonSpritePath);
        context.CanvasScaler = context.Canvas != null ? context.Canvas.GetComponent<CanvasScaler>() : null;

        Require(context, context.PvpTap, "Canvas/PvPTap");
        Require(context, context.Lobby, "Canvas/PvPTap/Lobby");
        Require(context, context.Loading, "Canvas/PvPTap/Loading");
        Require(context, context.Ready, "Canvas/PvPTap/Ready");
        Require(context, context.Battle, "Canvas/PvPTap/Battle");
        Require(context, context.TapBar, "Canvas/TapBar");
        Require(context, context.LoadingTitle, "Loading/MatchText");
        Require(context, context.Spinner, "Loading/LoadingSpinner");
        Require(context, context.ParticipantPanel, "Loading/ParticipantsCountText");
        Require(context, context.ParticipantCount, "ParticipantsCountText/CurrentParticipantsCount");
        Require(context, context.CancelButton, "Loading/CancelButton");
        Require(context, context.ReadyStatusPanel, "Ready/IsReadyPanel");
        Require(context, context.OpponentInfoPanel, "Ready/OppInfoPanel");
        Require(context, context.AttributePanel, "Ready/AttributeButtons");
        Require(context, context.ConsumablePanel, "Ready/ConsumableItemButtons");
        Require(context, context.EquipmentPanel, "Ready/EquipItemButtons");
        Require(context, context.Font, FontPath);
        Require(context, context.CardSprite, CardSpritePath);
        Require(context, context.CreamButtonSprite, CreamButtonSpritePath);
        Require(context, context.GoldButtonSprite, GoldButtonSpritePath);
        Require(context, context.CanvasScaler, "Canvas/CanvasScaler");

        if (context.BattleUiManager != null) ReadBattleUiReferences(context);
        context.ReadyNameGroup = Find(context.Ready, "IsReadyPanel/NameText") as RectTransform;
        context.ReadyStateGroup = Find(context.Ready, "IsReadyPanel/IsReadyText") as RectTransform;
        context.AttributeLayout = context.AttributePanel != null
            ? context.AttributePanel.GetComponent<HorizontalLayoutGroup>()
            : null;
        context.ReadyNameLayout = context.ReadyNameGroup != null
            ? context.ReadyNameGroup.GetComponent<HorizontalLayoutGroup>()
            : null;
        context.ReadyStateLayout = context.ReadyStateGroup != null
            ? context.ReadyStateGroup.GetComponent<HorizontalLayoutGroup>()
            : null;
        context.MyReadyCard = Find(context.ReadyStatusPanel?.gameObject, MyReadyCardName) as RectTransform;
        context.OpponentReadyCard = Find(context.ReadyStatusPanel?.gameObject,
            OpponentReadyCardName) as RectTransform;
        context.WeaponButton = context.EquipmentButtons.FirstOrDefault(button =>
            button != null && button.GetComponent<EquipButtonInfo>()?.type == EquipmentType.Weapon);
        Require(context, context.ReadyNameGroup, "Ready/IsReadyPanel/NameText");
        Require(context, context.ReadyStateGroup, "Ready/IsReadyPanel/IsReadyText");
        Require(context, context.AttributeLayout, "Ready/AttributeButtons/HorizontalLayoutGroup");
        Require(context, context.ReadyNameLayout, "Ready/IsReadyPanel/NameText/HorizontalLayoutGroup");
        Require(context, context.ReadyStateLayout, "Ready/IsReadyPanel/IsReadyText/HorizontalLayoutGroup");
        Require(context, context.WeaponButton, "BattleUIManager.equipItemButtons Weapon 매핑");
        if (context.EquipmentButtons.Any(button => button == null ||
                                                  button.GetComponent<EquipButtonInfo>() == null))
            context.Errors.Add("장비 버튼에 EquipButtonInfo가 없는 항목이 있습니다.");
        EquipmentType[] equipmentTypes = context.EquipmentButtons
            .Where(button => button != null && button.GetComponent<EquipButtonInfo>() != null)
            .Select(button => button.GetComponent<EquipButtonInfo>().type).ToArray();
        if (equipmentTypes.Length != context.EquipmentButtons.Length ||
            equipmentTypes.Distinct().Count() != context.EquipmentButtons.Length)
            context.Errors.Add("장비 버튼의 EquipmentType 매핑이 중복되었습니다.");
        if (context.ReadyNameGroup != null && context.ReadyNameGroup.parent != context.ReadyStatusPanel)
            context.Errors.Add("Ready 닉네임 래퍼가 IsReadyPanel의 직접 자식이 아닙니다.");
        if (context.ReadyStateGroup != null && context.ReadyStateGroup.parent != context.ReadyStatusPanel)
            context.Errors.Add("Ready 준비 상태 래퍼가 IsReadyPanel의 직접 자식이 아닙니다.");
        ValidateRuntimeBindings(context);
        return Finish(context, log);
    }

    private static void ReadBattleUiReferences(Context context)
    {
        SerializedObject serialized = new SerializedObject(context.BattleUiManager);
        context.CheckOpponentAttributeButton = Reference<Button>(serialized, "checkOppAttrButton");
        context.OpponentAttributeTexts = ReferenceArray<TMP_Text>(serialized, "oppAttrText");
        context.AttributeButtons = ReferenceArray<Button>(serialized, "attrButtons");
        context.ConsumableButtons = ReferenceArray<Button>(serialized, "consumableItemButtons");
        context.EquipmentButtons = ReferenceArray<Button>(serialized, "equipItemButtons");
        context.ReadyButton = Reference<Button>(serialized, "readyButton");
        context.MyNameText = Reference<TMP_Text>(serialized, "myNameText");
        context.OpponentNameText = Reference<TMP_Text>(serialized, "oppNameText");
        context.MyReadyText = Reference<TMP_Text>(serialized, "isMeReadyText");
        context.OpponentReadyText = Reference<TMP_Text>(serialized, "isOppReadyText");

        CheckReference(context, serialized, "lobbyPanel", context.Lobby);
        CheckReference(context, serialized, "loadingPanel", context.Loading);
        CheckReference(context, serialized, "readyPanel", context.Ready);
        CheckReference(context, serialized, "battlePanel", context.Battle);
        CheckReference(context, serialized, "tapBar", context.TapBar);
        CheckReference(context, serialized, "loadingSpinner", context.Spinner);
        CheckReference(context, serialized, "matchCancelButton", context.CancelButton);
        CheckReference(context, serialized, "currentParticipantsCount", context.ParticipantCount);

        Require(context, context.CheckOpponentAttributeButton, "BattleUIManager.checkOppAttrButton");
        Require(context, context.ReadyButton, "BattleUIManager.readyButton");
        Require(context, context.MyNameText, "BattleUIManager.myNameText");
        Require(context, context.OpponentNameText, "BattleUIManager.oppNameText");
        Require(context, context.MyReadyText, "BattleUIManager.isMeReadyText");
        Require(context, context.OpponentReadyText, "BattleUIManager.isOppReadyText");
        if (context.OpponentAttributeTexts.Length == 0)
            context.Errors.Add("BattleUIManager.oppAttrText 배열이 비어 있습니다.");
        if (context.AttributeButtons.Length != 4)
            context.Errors.Add("BattleUIManager.attrButtons 배열이 4개가 아닙니다.");
        if (context.ConsumableButtons.Length != 3)
            context.Errors.Add("BattleUIManager.consumableItemButtons 배열이 3개가 아닙니다.");
        if (context.EquipmentButtons.Length != 4)
            context.Errors.Add("BattleUIManager.equipItemButtons 배열이 4개가 아닙니다.");
    }

    private static void ValidateRuntimeBindings(Context context)
    {
        if (context.BattleUiManager == null || context.PhotonManager == null ||
            context.BattleManager == null || context.BattleBackendManager == null ||
            context.MainTabManager == null || context.Lobby == null || context.Loading == null ||
            context.Ready == null || context.Battle == null || context.TapBar == null ||
            context.Spinner == null || context.ReadyButton == null) return;

        if (context.Loading.activeSelf || context.Ready.activeSelf || context.Battle.activeSelf)
            context.Errors.Add("Loading/Ready/Battle의 초기 activeSelf는 false여야 합니다.");
        if (!context.Lobby.activeSelf || !context.TapBar.activeSelf)
            context.Errors.Add("Lobby와 TapBar의 초기 activeSelf는 true여야 합니다.");
        if (!IsChildOf(context.Spinner, context.Loading.transform) ||
            !IsChildOf(context.ReadyButton.transform, context.Ready.transform))
            context.Errors.Add("BattleUIManager의 Loading/Ready 참조가 실제 패널 내부를 가리키지 않습니다.");
    }

    private static void ApplyLoading(Context context)
    {
        SetStretch(context.Loading.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        StyleImage(GetOrAdd<Image>(context.Loading), null, Cream, Image.Type.Simple);

        Transform panelTransform = Find(context.Loading, "MatchingProgressPanel");
        Image panel;
        if (panelTransform == null)
        {
            GameObject item = new GameObject("MatchingProgressPanel", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(item, "Create PvP matching progress panel");
            item.transform.SetParent(context.Loading.transform, false);
            item.transform.SetAsFirstSibling();
            panel = item.GetComponent<Image>();
        }
        else panel = panelTransform.GetComponent<Image>() ?? Undo.AddComponent<Image>(panelTransform.gameObject);
        StyleImage(panel, context.CardSprite, Color.white, Image.Type.Sliced);
        SetCentered(panel.rectTransform, new Vector2(900f, 920f), Vector2.zero);
        Record(panel);
        panel.raycastTarget = false;

        TMP_Text guide = Find(panel.gameObject, "GuideText")?.GetComponent<TMP_Text>();
        if (guide == null)
        {
            GameObject item = new GameObject("GuideText", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(item, "Create PvP matching guide");
            item.transform.SetParent(panel.transform, false);
            guide = item.GetComponent<TMP_Text>();
        }
        StyleText(guide, "비슷한 실력의 모험가를 찾고 있습니다.\n잠시만 기다려주세요!",
            context.Font, 31f, MutedBrown, FontStyles.Normal, true);
        SetCentered(guide.rectTransform, new Vector2(720f, 130f), new Vector2(0f, 235f));

        StyleText(context.LoadingTitle, "상대를 찾는 중...", context.Font, 48f, Brown,
            FontStyles.Bold, false);
        SetCentered(context.LoadingTitle.rectTransform, new Vector2(760f, 90f), new Vector2(0f, 350f));
        SetCentered(context.Spinner, new Vector2(180f, 180f), new Vector2(0f, 40f));
        Image spinnerImage = context.Spinner.GetComponent<Image>();
        if (spinnerImage != null)
        {
            Record(spinnerImage);
            spinnerImage.color = Gold;
            Dirty(spinnerImage);
        }

        SetCentered(context.ParticipantPanel, new Vector2(620f, 100f), new Vector2(0f, -150f));
        StyleImage(GetOrAdd<Image>(context.ParticipantPanel.gameObject), context.CreamButtonSprite,
            CreamLight, Image.Type.Sliced);
        foreach (TMP_Text text in context.ParticipantPanel.GetComponentsInChildren<TMP_Text>(true))
            StyleText(text, null, context.Font, text == context.ParticipantCount ? 38f : 30f,
                text == context.ParticipantCount ? Brown : MutedBrown,
                text == context.ParticipantCount ? FontStyles.Bold : FontStyles.Normal, false);

        StyleButton(context.CancelButton, "매칭 취소", context.GoldButtonSprite, context.Font, 36f);
        SetCentered(context.CancelButton.GetComponent<RectTransform>(), new Vector2(520f, 116f),
            new Vector2(0f, -350f));
    }

    private static void ApplyReady(Context context)
    {
        SetStretch(context.Ready.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        StyleImage(GetOrAdd<Image>(context.Ready), null, Cream, Image.Type.Simple);

        StylePanel(context.ReadyStatusPanel, context.CardSprite, new Vector2(960f, 220f), -95f);
        StylePanel(context.OpponentInfoPanel, context.CardSprite, new Vector2(960f, 190f), -335f);
        StylePanel(context.AttributePanel, context.CardSprite, new Vector2(960f, 250f), -545f);
        StylePanel(context.ConsumablePanel, context.CardSprite, new Vector2(960f, 275f), -815f);
        StylePanel(context.EquipmentPanel, context.CardSprite, new Vector2(960f, 295f), -1110f);

        foreach (RectTransform panel in new[] { context.ReadyStatusPanel, context.OpponentInfoPanel,
                     context.AttributePanel, context.ConsumablePanel, context.EquipmentPanel })
            foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(true))
                StyleText(text, null, context.Font, Mathf.Clamp(text.fontSize, 23f, 36f), Brown,
                    text.fontStyle == FontStyles.Normal ? FontStyles.Bold : text.fontStyle, false);

        StyleText(context.MyNameText, null, context.Font, 36f, Brown, FontStyles.Bold, false);
        StyleText(context.OpponentNameText, null, context.Font, 36f, Brown, FontStyles.Bold, false);
        StyleText(context.MyReadyText, null, context.Font, 29f, MutedBrown, FontStyles.Bold, false);
        StyleText(context.OpponentReadyText, null, context.Font, 29f, MutedBrown, FontStyles.Bold, false);
        SetCentered(context.MyNameText.rectTransform, new Vector2(390f, 58f), new Vector2(-230f, 35f));
        SetCentered(context.OpponentNameText.rectTransform, new Vector2(390f, 58f), new Vector2(230f, 35f));
        SetCentered(context.MyReadyText.rectTransform, new Vector2(390f, 50f), new Vector2(-230f, -40f));
        SetCentered(context.OpponentReadyText.rectTransform, new Vector2(390f, 50f), new Vector2(230f, -40f));

        StyleButton(context.CheckOpponentAttributeButton, null, context.CreamButtonSprite,
            context.Font, 28f);
        SetCentered(context.CheckOpponentAttributeButton.GetComponent<RectTransform>(),
            new Vector2(340f, 82f), new Vector2(-270f, 0f));
        for (int index = 0; index < context.OpponentAttributeTexts.Length; index++)
        {
            TMP_Text text = context.OpponentAttributeTexts[index];
            StyleText(text, null, context.Font, 25f, Brown, FontStyles.Bold, false);
            SetCentered(text.rectTransform, new Vector2(125f, 70f),
                new Vector2(-200f + index * 135f, 0f));
        }

        LayoutButtons(context.AttributeButtons, context.CreamButtonSprite, context.Font,
            205f, new Vector2(185f, 105f));
        LayoutButtons(context.ConsumableButtons, context.CreamButtonSprite, context.Font,
            270f, new Vector2(230f, 145f));
        LayoutButtons(context.EquipmentButtons, context.CreamButtonSprite, context.Font,
            205f, new Vector2(185f, 155f));
        StyleButton(context.ReadyButton, null, context.GoldButtonSprite, context.Font, 38f);
        SetBottom(context.ReadyButton.GetComponent<RectTransform>(), new Vector2(620f, 118f),
            new Vector2(0f, 72f));
        ApplyReadyLayoutFix(context);
    }

    private static void ApplyReadyLayoutFix(Context context)
    {
        Image statusPanelImage = context.ReadyStatusPanel.GetComponent<Image>();
        Record(statusPanelImage);
        statusPanelImage.enabled = false;
        Dirty(statusPanelImage);

        context.MyReadyCard = GetOrCreateReadyCard(context.ReadyStatusPanel, MyReadyCardName,
            context.CardSprite);
        context.OpponentReadyCard = GetOrCreateReadyCard(context.ReadyStatusPanel,
            OpponentReadyCardName, context.CardSprite);
        SetCentered(context.MyReadyCard, new Vector2(452f, 184f), new Vector2(-238f, 0f));
        SetCentered(context.OpponentReadyCard, new Vector2(452f, 184f), new Vector2(238f, 0f));

        SetStretch(context.ReadyNameGroup, new Vector2(24f, 18f), new Vector2(-24f, -18f));
        SetStretch(context.ReadyStateGroup, new Vector2(24f, 18f), new Vector2(-24f, -18f));
        ConfigureIgnoredLayoutGroup(context.ReadyNameLayout);
        ConfigureIgnoredLayoutGroup(context.ReadyStateLayout);

        foreach (TMP_Text text in new[]
                 {
                     context.MyNameText, context.OpponentNameText,
                     context.MyReadyText, context.OpponentReadyText
                 })
            IgnoreParentLayout(text.rectTransform);

        StyleText(context.MyNameText, null, context.Font, 36f, Brown, FontStyles.Bold, false);
        StyleText(context.OpponentNameText, null, context.Font, 36f, Brown, FontStyles.Bold, false);
        foreach (TMP_Text name in new[] { context.MyNameText, context.OpponentNameText })
        {
            Record(name);
            name.enableAutoSizing = true;
            name.fontSizeMin = 24f;
            name.fontSizeMax = 36f;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.raycastTarget = false;
            Dirty(name);
        }

        StyleText(context.MyReadyText, null, context.Font, 29f, MutedBrown, FontStyles.Bold, false);
        StyleText(context.OpponentReadyText, null, context.Font, 29f, MutedBrown, FontStyles.Bold, false);
        SetCentered(context.MyNameText.rectTransform, new Vector2(380f, 58f), new Vector2(-238f, 44f));
        SetCentered(context.OpponentNameText.rectTransform, new Vector2(380f, 58f), new Vector2(238f, 44f));
        SetCentered(context.MyReadyText.rectTransform, new Vector2(380f, 44f), new Vector2(-238f, -46f));
        SetCentered(context.OpponentReadyText.rectTransform, new Vector2(380f, 44f), new Vector2(238f, -46f));

        SetCentered(context.CheckOpponentAttributeButton.GetComponent<RectTransform>(),
            new Vector2(340f, 72f), new Vector2(0f, 34f));
        for (int index = 0; index < context.OpponentAttributeTexts.Length; index++)
            SetCentered(context.OpponentAttributeTexts[index].rectTransform, new Vector2(125f, 50f),
                new Vector2(-202.5f + index * 135f, -48f));

        Record(context.AttributeLayout);
        context.AttributeLayout.padding = new RectOffset(60, 60, 32, 32);
        context.AttributeLayout.spacing = 20f;
        context.AttributeLayout.childAlignment = TextAnchor.MiddleCenter;
        context.AttributeLayout.childControlWidth = false;
        context.AttributeLayout.childControlHeight = false;
        context.AttributeLayout.childForceExpandWidth = false;
        context.AttributeLayout.childForceExpandHeight = false;
        Dirty(context.AttributeLayout);

        foreach (Button button in context.AttributeButtons)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            Record(rect);
            rect.sizeDelta = new Vector2(185f, 105f);
            Dirty(rect);
            foreach (Image icon in button.GetComponentsInChildren<Image>(true))
            {
                if (icon == button.image) continue;
                Record(icon);
                icon.preserveAspect = true;
                Dirty(icon);
            }
        }

        foreach (Button button in context.EquipmentButtons)
        {
            Outline outline = GetOrAdd<Outline>(button.gameObject);
            Record(outline);
            outline.effectColor = SelectedOutlineGreen;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = false;
            outline.enabled = false;
            Dirty(outline);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(context.AttributePanel);
    }

    private static List<string> ValidateReadyLayout(Context context)
    {
        List<string> errors = new List<string>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(context.AttributePanel);

        int myCardCount = context.ReadyStatusPanel.Cast<Transform>()
            .Count(child => child.name == MyReadyCardName);
        int opponentCardCount = context.ReadyStatusPanel.Cast<Transform>()
            .Count(child => child.name == OpponentReadyCardName);
        if (myCardCount != 1 || opponentCardCount != 1)
            errors.Add($"Ready 정보 카드 개수가 올바르지 않습니다: My={myCardCount}, Opp={opponentCardCount}");

        if (context.ReadyNameGroup.parent != context.ReadyStatusPanel ||
            context.ReadyStateGroup.parent != context.ReadyStatusPanel)
            errors.Add("닉네임/준비 상태 래퍼가 IsReadyPanel의 직접 자식이 아닙니다.");
        if (context.MyReadyCard == null || context.OpponentReadyCard == null)
        {
            errors.Add("Ready 좌우 독립 정보 카드 2개가 없습니다.");
        }
        else
        {
            if (context.MyReadyCard.rect.size != context.OpponentReadyCard.rect.size)
                errors.Add("Ready 좌우 카드의 크기가 서로 다릅니다.");
            if (Overlaps(context.MyReadyCard, context.OpponentReadyCard))
                errors.Add("Ready 좌우 카드가 서로 겹칩니다.");
            float cardGap = HorizontalGap(context.MyReadyCard, context.OpponentReadyCard);
            if (cardGap < 16f)
                errors.Add("Ready 좌우 카드 사이 간격이 부족합니다: " + cardGap);
            foreach (RectTransform rect in new[]
                     {
                         context.MyNameText.rectTransform, context.MyReadyText.rectTransform
                     })
                if (!Contains(context.MyReadyCard, rect))
                    errors.Add("내 정보 TMP가 왼쪽 카드 밖으로 벗어납니다: " + GetPath(rect));
            foreach (RectTransform rect in new[]
                     {
                         context.OpponentNameText.rectTransform, context.OpponentReadyText.rectTransform
                     })
                if (!Contains(context.OpponentReadyCard, rect))
                    errors.Add("상대 정보 TMP가 오른쪽 카드 밖으로 벗어납니다: " + GetPath(rect));
            ValidateReadyCard(errors, context.MyReadyCard, context.CardSprite);
            ValidateReadyCard(errors, context.OpponentReadyCard, context.CardSprite);
        }
        if (context.ReadyStatusPanel.GetComponent<Image>().enabled)
            errors.Add("기존 IsReadyPanel 배경이 활성화되어 이중 테두리가 표시됩니다.");
        if (context.ReadyNameLayout.childControlWidth || context.ReadyNameLayout.childControlHeight ||
            context.ReadyStateLayout.childControlWidth || context.ReadyStateLayout.childControlHeight)
            errors.Add("상단 HorizontalLayoutGroup이 네 TMP의 수동 좌우/상하 배치를 덮어쓰고 있습니다.");
        foreach (TMP_Text text in new[]
                 {
                     context.MyNameText, context.OpponentNameText,
                     context.MyReadyText, context.OpponentReadyText
                 })
        {
            LayoutElement layoutElement = text.GetComponent<LayoutElement>();
            if (layoutElement == null || !layoutElement.ignoreLayout)
                errors.Add("상단 TMP가 부모 LayoutGroup의 위치 재계산에서 제외되지 않았습니다: " +
                           GetPath(text.transform));
        }
        foreach (RectTransform group in new[] { context.ReadyNameGroup, context.ReadyStateGroup })
        {
            ContentSizeFitter fitter = group.GetComponent<ContentSizeFitter>();
            if (fitter != null && fitter.enabled)
                errors.Add("상단 TMP 래퍼의 ContentSizeFitter가 독립 영역 크기를 덮어쓸 수 있습니다: " +
                           GetPath(group));
        }
        foreach (TMP_Text text in new[]
                 {
                     context.MyNameText, context.OpponentNameText,
                     context.MyReadyText, context.OpponentReadyText
                 })
            if (!Contains(context.ReadyStatusPanel, text.rectTransform))
                errors.Add("상단 카드 밖으로 벗어난 텍스트가 있습니다: " + GetPath(text.transform));
        if (Overlaps(context.MyNameText.rectTransform, context.MyReadyText.rectTransform) ||
            Overlaps(context.OpponentNameText.rectTransform, context.OpponentReadyText.rectTransform))
            errors.Add("닉네임과 준비 상태가 겹칩니다.");
        if (VerticalGap(context.MyNameText.rectTransform, context.MyReadyText.rectTransform) < 20f ||
            VerticalGap(context.OpponentNameText.rectTransform,
                context.OpponentReadyText.rectTransform) < 20f)
            errors.Add("닉네임과 준비 상태 사이의 세로 간격이 부족합니다.");
        if (Overlaps(context.MyNameText.rectTransform, context.OpponentNameText.rectTransform) ||
            Overlaps(context.MyReadyText.rectTransform, context.OpponentReadyText.rectTransform))
            errors.Add("내 정보와 상대 정보의 좌우 영역이 겹칩니다.");
        foreach (TMP_Text name in new[] { context.MyNameText, context.OpponentNameText })
        {
            if (!name.enableAutoSizing || name.fontSizeMin < 24f || name.fontSizeMax > 36f)
                errors.Add("긴 닉네임용 Auto Size 범위가 올바르지 않습니다: " + GetPath(name.transform));
            if (name.enableWordWrapping || name.overflowMode != TextOverflowModes.Ellipsis)
                errors.Add("닉네임은 한 줄 Ellipsis여야 합니다: " + GetPath(name.transform));
        }
        if (Mathf.Abs(context.CheckOpponentAttributeButton.GetComponent<RectTransform>()
                .anchoredPosition.x) > 0.1f)
            errors.Add("정찰하기 버튼이 OppInfoPanel 중앙에 정렬되지 않았습니다.");
        foreach (Button button in context.AttributeButtons)
        {
            if (!Contains(context.AttributePanel, button.GetComponent<RectTransform>()))
                errors.Add("특성 버튼이 카드 밖으로 벗어납니다: " + GetPath(button.transform));
            foreach (Image icon in button.GetComponentsInChildren<Image>(true))
                if (icon != button.image && !icon.preserveAspect)
                    errors.Add("특성 아이콘 PreserveAspect가 꺼져 있습니다: " + GetPath(icon.transform));
        }
        if (context.AttributeLayout.padding.left < 40 || context.AttributeLayout.padding.right < 40)
            errors.Add("특성 버튼 그룹의 좌우 내부 여백이 부족합니다.");
        foreach (Button button in context.EquipmentButtons)
        {
            Outline outline = button.GetComponent<Outline>();
            if (outline == null || outline.effectColor != (Color)SelectedOutlineGreen ||
                outline.effectDistance != new Vector2(3f, -3f) || outline.useGraphicAlpha)
                errors.Add("장비 선택 상태용 밝은 녹색 Outline 설정이 올바르지 않습니다: " +
                           GetPath(button.transform));
            if (button.transition != Selectable.Transition.ColorTint)
                errors.Add("장비 버튼 Transition이 ColorTint가 아닙니다: " + GetPath(button.transform));
        }
        return errors;
    }

    private static bool Contains(RectTransform outer, RectTransform inner)
    {
        Vector3[] outerCorners = new Vector3[4];
        Vector3[] innerCorners = new Vector3[4];
        outer.GetWorldCorners(outerCorners);
        inner.GetWorldCorners(innerCorners);
        const float tolerance = 0.5f;
        return innerCorners[0].x >= outerCorners[0].x - tolerance &&
               innerCorners[0].y >= outerCorners[0].y - tolerance &&
               innerCorners[2].x <= outerCorners[2].x + tolerance &&
               innerCorners[2].y <= outerCorners[2].y + tolerance;
    }

    private static bool Overlaps(RectTransform first, RectTransform second)
    {
        Vector3[] firstCorners = new Vector3[4];
        Vector3[] secondCorners = new Vector3[4];
        first.GetWorldCorners(firstCorners);
        second.GetWorldCorners(secondCorners);
        Rect a = Rect.MinMaxRect(firstCorners[0].x, firstCorners[0].y,
            firstCorners[2].x, firstCorners[2].y);
        Rect b = Rect.MinMaxRect(secondCorners[0].x, secondCorners[0].y,
            secondCorners[2].x, secondCorners[2].y);
        return a.Overlaps(b);
    }

    private static float HorizontalGap(RectTransform left, RectTransform right)
    {
        float leftEdgeOfRight = right.anchoredPosition.x - right.rect.width * 0.5f;
        float rightEdgeOfLeft = left.anchoredPosition.x + left.rect.width * 0.5f;
        return leftEdgeOfRight - rightEdgeOfLeft;
    }

    private static float VerticalGap(RectTransform upper, RectTransform lower)
    {
        Vector3[] upperCorners = new Vector3[4];
        Vector3[] lowerCorners = new Vector3[4];
        upper.GetWorldCorners(upperCorners);
        lower.GetWorldCorners(lowerCorners);
        return upperCorners[0].y - lowerCorners[2].y;
    }

    private static void ValidateReadyCard(List<string> errors, RectTransform card, Sprite sprite)
    {
        Image image = card.GetComponent<Image>();
        if (image == null || image.sprite != sprite || image.type != Image.Type.Sliced ||
            image.raycastTarget)
            errors.Add("Ready 정보 카드 Image 설정이 올바르지 않습니다: " + GetPath(card));
    }

    private static string BuildReadyDiagnostics(Context context)
    {
        float attributeWidth = context.AttributeButtons.Sum(button =>
                                   button.GetComponent<RectTransform>().rect.width) +
                               context.AttributeLayout.spacing * (context.AttributeButtons.Length - 1) +
                               context.AttributeLayout.padding.horizontal;
        return "대상: Canvas/PvPTap/Ready (activeSelf=" + context.Ready.activeSelf + ")\n" +
               "CanvasScaler: reference=" + context.CanvasScaler.referenceResolution +
               ", match=" + context.CanvasScaler.matchWidthOrHeight +
               " / Windows PlayerSettings=" + PlayerSettings.defaultScreenWidth + "x" +
               PlayerSettings.defaultScreenHeight + ", resizable=" + PlayerSettings.resizableWindow + "\n" +
               "상단 카드 크기: " + context.ReadyStatusPanel.rect.size +
               ", LayoutGroup childControl(Name/State)=" +
               context.ReadyNameLayout.childControlWidth + "/" +
               context.ReadyStateLayout.childControlWidth + "\n" +
               "독립 카드: My=" + (context.MyReadyCard == null ? "없음" : context.MyReadyCard.rect.size.ToString()) +
               ", Opp=" + (context.OpponentReadyCard == null ? "없음" :
                   context.OpponentReadyCard.rect.size.ToString()) + "\n" +
               "상단 정보: " + GetPath(context.ReadyNameGroup) + ", " +
               GetPath(context.ReadyStateGroup) + "\n" +
               "정찰 버튼: " + GetPath(context.CheckOpponentAttributeButton.transform) + "\n" +
               "특성 Layout 총 요구 폭: " + attributeWidth + " / 카드 폭 " +
               context.AttributePanel.rect.width + "\n" +
               "장비 버튼: " + context.EquipmentButtons.Length +
               "개 (밝은 녹색 팔레트, 상태 원본=InventoryItemResponse.IsEquipped)";
    }

    private static void StylePanel(RectTransform panel, Sprite sprite, Vector2 size, float y)
    {
        StyleImage(GetOrAdd<Image>(panel.gameObject), sprite, Color.white, Image.Type.Sliced);
        SetTop(panel, size, new Vector2(0f, y));
    }

    private static RectTransform GetOrCreateReadyCard(RectTransform parent, string name, Sprite sprite)
    {
        Transform existing = parent.Find(name);
        Image image;
        if (existing == null)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(item, "Create 15-D-7B Ready player card");
            item.transform.SetParent(parent, false);
            item.transform.SetAsFirstSibling();
            image = item.GetComponent<Image>();
        }
        else
        {
            image = existing.GetComponent<Image>() ?? Undo.AddComponent<Image>(existing.gameObject);
        }

        StyleImage(image, sprite, Color.white, Image.Type.Sliced);
        Record(image);
        image.raycastTarget = false;
        Dirty(image);
        return image.rectTransform;
    }

    private static void IgnoreParentLayout(RectTransform rect)
    {
        LayoutElement layoutElement = GetOrAdd<LayoutElement>(rect.gameObject);
        Record(layoutElement);
        layoutElement.ignoreLayout = true;
        Dirty(layoutElement);
    }

    private static void ConfigureIgnoredLayoutGroup(HorizontalLayoutGroup layout)
    {
        Record(layout);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        Dirty(layout);
    }

    private static void LayoutButtons(Button[] buttons, Sprite sprite, TMP_FontAsset font,
        float spacing, Vector2 size)
    {
        float start = -spacing * (buttons.Length - 1) * 0.5f;
        for (int index = 0; index < buttons.Length; index++)
        {
            StyleButton(buttons[index], null, sprite, font, 25f);
            SetCentered(buttons[index].GetComponent<RectTransform>(), size,
                new Vector2(start + spacing * index, -10f));
        }
    }

    private static List<string> ValidateAppliedStyle(Context context)
    {
        List<string> errors = new List<string>();
        if (context.Loading.GetComponent<Image>()?.color != (Color)Cream)
            errors.Add("Loading 크림 배경이 적용되지 않았습니다.");
        Image progress = Find(context.Loading, "MatchingProgressPanel")?.GetComponent<Image>();
        if (progress == null || progress.sprite != context.CardSprite)
            errors.Add("Loading RPG 진행 카드가 적용되지 않았습니다.");
        if (context.Spinner.sizeDelta.x < 170f || context.Spinner.GetComponent<Image>()?.color != (Color)Gold)
            errors.Add("기존 LoadingSpinner의 크기/골드 색상이 적용되지 않았습니다.");
        if (context.CancelButton.GetComponent<Image>()?.sprite != context.GoldButtonSprite)
            errors.Add("기존 매칭 취소 버튼의 RPG 스타일이 적용되지 않았습니다.");
        if (context.Ready.GetComponent<Image>()?.color != (Color)Cream)
            errors.Add("Ready 크림 배경이 적용되지 않았습니다.");
        foreach (RectTransform panel in new[] { context.ReadyStatusPanel, context.OpponentInfoPanel,
                     context.AttributePanel, context.ConsumablePanel, context.EquipmentPanel })
            if (panel.GetComponent<Image>()?.sprite != context.CardSprite)
                errors.Add("Ready RPG 카드가 적용되지 않았습니다: " + GetPath(panel));
        if (context.ReadyButton.GetComponent<Image>()?.sprite != context.GoldButtonSprite)
            errors.Add("기존 Ready 버튼의 RPG 스타일이 적용되지 않았습니다.");
        return errors;
    }

    private static string BuildDiagnostics(Context context)
    {
        return "대상 Scene: " + ScenePath + "\n" +
               "Hierarchy: Canvas/PvPTap/{Lobby, Loading, Ready, Battle" +
               (context.Result == null ? "" : ", Result") + "}, Canvas/TapBar\n" +
               "Manager: " + GetPath(context.BattleUiManager.transform) + ", " +
               GetPath(context.PhotonManager.transform) + ", " + GetPath(context.BattleManager.transform) + "\n" +
               "초기 상태: Lobby=" + context.Lobby.activeSelf + ", Loading=" + context.Loading.activeSelf +
               ", Ready=" + context.Ready.activeSelf + ", Battle=" + context.Battle.activeSelf +
               ", TapBar=" + context.TapBar.activeSelf + "\n" +
               "Ready 전용 타이머/캐릭터 RawImage: 실제 Ready Hierarchy 및 BattleUIManager 참조에 없음; " +
               "Battle RawImage/RenderTexture는 비변경.";
    }

    private static void StyleButton(Button button, string label, Sprite sprite, TMP_FontAsset font,
        float fontSize)
    {
        StyleImage(button.GetComponent<Image>(), sprite, Color.white, Image.Type.Sliced);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null) StyleText(text, label, font, fontSize, Brown, FontStyles.Bold, false);
        Record(button);
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(0xFF, 0xF4, 0xC9, 0xFF);
        colors.pressedColor = new Color32(0xD8, 0xB5, 0x6C, 0xFF);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        Dirty(button);
    }

    private static void StyleImage(Image image, Sprite sprite, Color color, Image.Type type)
    {
        Record(image);
        image.sprite = sprite;
        image.color = color;
        image.type = type;
        Dirty(image);
    }

    private static void StyleText(TMP_Text text, string value, TMP_FontAsset font, float size,
        Color color, FontStyles style, bool wrap)
    {
        Record(text);
        if (value != null) text.text = value;
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        text.enableWordWrapping = wrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        Dirty(text);
    }

    private static void SetStretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        Record(rect);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = min;
        rect.offsetMax = max;
        Dirty(rect);
    }

    private static void SetCentered(RectTransform rect, Vector2 size, Vector2 position)
    {
        Record(rect);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Dirty(rect);
    }

    private static void SetTop(RectTransform rect, Vector2 size, Vector2 position)
    {
        Record(rect);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Dirty(rect);
    }

    private static void SetBottom(RectTransform rect, Vector2 size, Vector2 position)
    {
        Record(rect);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Dirty(rect);
    }

    private static Context Finish(Context context, bool log)
    {
        if (log)
            foreach (string error in context.Errors)
                Debug.LogError("[HabitPVP MainScene] PvP Loading·Ready: " + error);
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

    private static bool IsChildOf(Transform child, Transform parent)
    {
        return child != null && parent != null && child.IsChildOf(parent);
    }

    private static T Reference<T>(SerializedObject serialized, string name) where T : UnityEngine.Object
    {
        return serialized.FindProperty(name)?.objectReferenceValue as T;
    }

    private static T[] ReferenceArray<T>(SerializedObject serialized, string name)
        where T : UnityEngine.Object
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null || !property.isArray) return Array.Empty<T>();
        return Enumerable.Range(0, property.arraySize)
            .Select(index => property.GetArrayElementAtIndex(index).objectReferenceValue as T)
            .Where(value => value != null).ToArray();
    }

    private static void CheckReference(Context context, SerializedObject serialized, string name,
        UnityEngine.Object expected)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null) context.Errors.Add("SerializeField가 없습니다: " + name);
        else if (property.objectReferenceValue != expected)
            context.Errors.Add("SerializeField 참조가 Hierarchy와 다릅니다: " + name);
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T value = target.GetComponent<T>();
        return value != null ? value : Undo.AddComponent<T>(target);
    }

    private static void Record(UnityEngine.Object target)
    {
        if (target != null) Undo.RecordObject(target, "Apply PvP Loading Ready UI Renewal");
    }

    private static void Dirty(UnityEngine.Object target)
    {
        if (target != null && PrefabUtility.IsPartOfPrefabInstance(target))
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        if (target != null) EditorUtility.SetDirty(target);
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

    private sealed class ProtectionSnapshot
    {
        private readonly Dictionary<int, string> managerReferences;
        private readonly Dictionary<int, string> buttonEvents;
        private readonly Dictionary<int, bool> activeStates;
        private readonly Dictionary<int, int> siblingOrder;
        private readonly HashSet<int> protectedTransformIds;

        private ProtectionSnapshot(Context context)
        {
            Component[] managers =
            {
                context.MainTabManager, context.PhotonManager, context.BattleManager,
                context.BattleUiManager, context.BattleBackendManager
            };
            managerReferences = managers.ToDictionary(item => item.GetInstanceID(), CaptureObjectReferences);
            buttonEvents = context.Canvas.GetComponentsInChildren<Button>(true)
                .ToDictionary(button => button.GetInstanceID(), button => CaptureEvent(button.onClick));
            Transform[] transforms = context.PvpTap.GetComponentsInChildren<Transform>(true)
                .Concat(context.TapBar.GetComponentsInChildren<Transform>(true)).Distinct().ToArray();
            activeStates = transforms.ToDictionary(item => item.GetInstanceID(), item => item.gameObject.activeSelf);
            protectedTransformIds = transforms.Select(item => item.GetInstanceID()).ToHashSet();
            siblingOrder = transforms.ToDictionary(item => item.GetInstanceID(), RelativeSiblingOrder);
        }

        public static ProtectionSnapshot Capture(Context context) => new ProtectionSnapshot(context);

        public void AssertUnchanged(Context context)
        {
            foreach (Component manager in new Component[]
                     {
                         context.MainTabManager, context.PhotonManager, context.BattleManager,
                         context.BattleUiManager, context.BattleBackendManager
                     })
                if (managerReferences[manager.GetInstanceID()] != CaptureObjectReferences(manager))
                    throw new InvalidOperationException(manager.GetType().Name + " SerializeField 참조가 변경되었습니다.");
            foreach (Button button in context.Canvas.GetComponentsInChildren<Button>(true))
                if (buttonEvents.TryGetValue(button.GetInstanceID(), out string before) &&
                    before != CaptureEvent(button.onClick))
                    throw new InvalidOperationException("기존 Button UnityEvent가 변경되었습니다: " +
                                                        GetPath(button.transform));
            foreach (Transform item in context.PvpTap.GetComponentsInChildren<Transform>(true)
                         .Concat(context.TapBar.GetComponentsInChildren<Transform>(true)).Distinct())
            {
                int id = item.GetInstanceID();
                if (!protectedTransformIds.Contains(id)) continue;
                if (activeStates[id] != item.gameObject.activeSelf)
                    throw new InvalidOperationException("기존 activeSelf가 변경되었습니다: " + GetPath(item));
                if (siblingOrder[id] != RelativeSiblingOrder(item))
                    throw new InvalidOperationException("기존 sibling 상대 순서가 변경되었습니다: " + GetPath(item));
            }
        }

        private int RelativeSiblingOrder(Transform item)
        {
            if (item.parent == null) return 0;
            int order = 0;
            foreach (Transform sibling in item.parent)
            {
                if (sibling == item) break;
                if (protectedTransformIds.Contains(sibling.GetInstanceID())) order++;
            }
            return order;
        }

        private static string CaptureObjectReferences(Component target)
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
                    return (target == null ? "null" :
                               GlobalObjectId.GetGlobalObjectIdSlow(target).ToString()) +
                           ":" + value.GetPersistentMethodName(index);
                }));
        }
    }

    private sealed class Context
    {
        public Scene Scene;
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
        public GameObject Canvas, PvpTap, Lobby, Loading, Ready, Battle, Result, TapBar;
        public RectTransform Spinner, ParticipantPanel, ReadyStatusPanel, OpponentInfoPanel,
            AttributePanel, ConsumablePanel, EquipmentPanel, ReadyNameGroup, ReadyStateGroup,
            MyReadyCard, OpponentReadyCard;
        public TMP_Text LoadingTitle, ParticipantCount, MyNameText, OpponentNameText,
            MyReadyText, OpponentReadyText;
        public TMP_Text[] OpponentAttributeTexts = Array.Empty<TMP_Text>();
        public Button CancelButton, CheckOpponentAttributeButton, ReadyButton, WeaponButton;
        public Button[] AttributeButtons = Array.Empty<Button>();
        public Button[] ConsumableButtons = Array.Empty<Button>();
        public Button[] EquipmentButtons = Array.Empty<Button>();
        public BattleUIManager BattleUiManager;
        public PhotonManager PhotonManager;
        public BattleManager BattleManager;
        public BattleBackendManager BattleBackendManager;
        public MainTabManager MainTabManager;
        public TMP_FontAsset Font;
        public Sprite CardSprite, CreamButtonSprite, GoldButtonSprite;
        public CanvasScaler CanvasScaler;
        public HorizontalLayoutGroup AttributeLayout, ReadyNameLayout, ReadyStateLayout;
    }
}
