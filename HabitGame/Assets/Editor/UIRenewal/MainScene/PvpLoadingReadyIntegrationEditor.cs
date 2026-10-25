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

    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 CreamLight = new Color32(0xFF, 0xF9, 0xEA, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x86, 0x68, 0x5D, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);

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

        if (context.BattleUiManager != null) ReadBattleUiReferences(context);
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
    }

    private static void StylePanel(RectTransform panel, Sprite sprite, Vector2 size, float y)
    {
        StyleImage(GetOrAdd<Image>(panel.gameObject), sprite, Color.white, Image.Type.Sliced);
        SetTop(panel, size, new Vector2(0f, y));
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
            AttributePanel, ConsumablePanel, EquipmentPanel;
        public TMP_Text LoadingTitle, ParticipantCount, MyNameText, OpponentNameText,
            MyReadyText, OpponentReadyText;
        public TMP_Text[] OpponentAttributeTexts = Array.Empty<TMP_Text>();
        public Button CancelButton, CheckOpponentAttributeButton, ReadyButton;
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
    }
}
