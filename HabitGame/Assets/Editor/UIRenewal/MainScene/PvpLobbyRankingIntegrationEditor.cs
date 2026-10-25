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

public static class PvpLobbyRankingIntegrationEditor
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string CanvasPrefabPath = "Assets/Resources/UIPrefabs/Canvas.prefab";
    private const string RankingRowPrefabPath = "Assets/Resources/Prefabs/RankingRow.prefab";
    private const string EmptyRankingTextName = "EmptyRankingText";
    private const string FontPath = "Assets/Login/Fonts/NotoSansKR Login SDF.asset";
    private const string CardSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/panel_rpg_card.png";
    private const string GoldButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_rpg.png";
    private const string MenuRoot = "Tools/HabitPVP/UI Renewal/MainScene/";

    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x86, 0x68, 0x5D, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);

    [MenuItem(MenuRoot + "Validate PvP Lobby Ranking", false, 2270)]
    private static void ValidateMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid) return;

        List<string> styleErrors = ValidateAppliedStyle(context, false);
        Debug.Log(styleErrors.Count == 0
                ? "[HabitPVP MainScene] PvP 로비·랭킹 구조/디자인 검증 성공. 변경하지 않았습니다.\n" +
                  BuildDiagnostics(context)
                : "[HabitPVP MainScene] PvP 로비·랭킹 적용 전 구조 검증 성공. 변경하지 않았습니다.\n" +
                  BuildDiagnostics(context),
            context.Lobby);
        foreach (string error in styleErrors)
            Debug.LogWarning("[HabitPVP MainScene] PvP 로비·랭킹 스타일 미적용/불일치: " + error,
                context.Lobby);
    }

    [MenuItem(MenuRoot + "Dry Run PvP Lobby Ranking", false, 2271)]
    private static void DryRunMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid)
        {
            Debug.LogError("[HabitPVP MainScene] PvP 로비·랭킹 Dry Run 중단: 구조 오류를 해결해 주세요.");
            return;
        }

        Debug.Log(
            "[HabitPVP MainScene] PvP 로비·랭킹 Dry Run (변경 없음)\n" +
            "- 실제 구조: Canvas/PvPTap/Lobby 안의 랭킹 목록·내 정보·매칭 버튼\n" +
            "- Lobby: 크림 배경, NotoSansKR 제목, 하단 180px TapBar 안전 영역 확보\n" +
            "- RankingList: RPG 카드, 세로 전용 ScrollRect, 기존 Viewport/Content 참조 유지\n" +
            "- MyRankingInfoBox: 음수 spacing 충돌 제거, 3열 정보와 잔여 횟수 카드 정렬\n" +
            "- RankingRow.prefab: 기존 4개 데이터 SerializeField와 이름/점수 표시 영역 검증\n" +
            "- 빈 랭킹: 미사용 캐시 Row를 숨겨 고정 점수 문구가 남지 않도록 처리\n" +
            "- 제외: Loading, Ready, Battle HUD, Result 및 모든 PvP/API 로직\n" +
            "- 보호: Manager 참조, Button UnityEvent, activeSelf, sibling 순서, TapBar, Canvas.prefab\n" +
            BuildDiagnostics(context), context.Lobby);
    }

    [MenuItem(MenuRoot + "Apply PvP Lobby Ranking", false, 2272)]
    private static void ApplyMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid) return;

        ProtectionSnapshot snapshot = ProtectionSnapshot.Capture(context);
        byte[] prefabBackup = File.ReadAllBytes(RankingRowPrefabPath);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply PvP Lobby Ranking UI Renewal");

        try
        {
            ApplyLobbyStyle(context);
            snapshot.AssertUnchanged(context);
            ApplyRankingRowPrefab(context);
            snapshot.AssertUnchanged(context);

            List<string> errors = ValidateAppliedStyle(context, true);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));

            EditorSceneManager.MarkSceneDirty(context.Scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = context.Lobby;
            Debug.Log(
                "[HabitPVP MainScene] PvP 로비·랭킹 적용 성공. MainScene은 dirty 상태이며 자동 저장하지 않았습니다. " +
                "RankingRow.prefab만 별도 Prefab asset으로 저장했습니다. Canvas.prefab과 전투 화면은 변경하지 않았습니다.",
                context.Lobby);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            File.WriteAllBytes(RankingRowPrefabPath, prefabBackup);
            AssetDatabase.ImportAsset(RankingRowPrefabPath, ImportAssetOptions.ForceUpdate);
            EditorSceneManager.MarkSceneDirty(context.Scene);
            Debug.LogException(exception);
            Debug.LogError("[HabitPVP MainScene] PvP 로비·랭킹 Apply 실패. Scene 변경과 RankingRow.prefab을 복원했습니다.");
        }
    }

    [MenuItem(MenuRoot + "Apply PvP Lobby Ranking", true)]
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
        context.RankingTitle = Find(context.Lobby, "RankingTitleText")?.GetComponent<TMP_Text>();
        context.RankingList = Find(context.Lobby, "RankingList")?.GetComponent<ScrollRect>();
        context.Viewport = Find(context.RankingList != null ? context.RankingList.gameObject : null,
            "Viewport") as RectTransform;
        context.Content = Find(context.RankingList != null ? context.RankingList.gameObject : null,
            "Viewport/Content") as RectTransform;
        context.EmptyRankingText = Find(context.RankingList != null ? context.RankingList.gameObject : null,
            EmptyRankingTextName)?.GetComponent<TMP_Text>();
        context.MyInfo = Find(context.Lobby, "MyRankingInfoBox") as RectTransform;
        context.RankingInfo = Find(context.MyInfo != null ? context.MyInfo.gameObject : null,
            "RankingTextParent") as RectTransform;
        context.ScoreInfo = Find(context.MyInfo != null ? context.MyInfo.gameObject : null,
            "ScoreTextParent") as RectTransform;
        context.RemainInfo = Find(context.MyInfo != null ? context.MyInfo.gameObject : null,
            "RemainBox") as RectTransform;
        context.MatchButton = Find(context.Lobby, "MatchButton")?.GetComponent<Button>();
        context.LimitPopup = Find(context.Lobby, "DailyPvpLimitReachedPopup")?.gameObject;
        context.TapBar = Find(context.Canvas, "TapBar")?.gameObject;
        context.MainTabManager = FindUnique<MainTabManager>(context);
        context.RankingManager = FindUnique<RankingboardManager>(context);
        context.BattleManager = FindUnique<BattleManager>(context);
        context.BattleUiManager = FindUnique<BattleUIManager>(context);
        context.BattleBackendManager = FindUnique<BattleBackendManager>(context);
        context.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        context.CardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardSpritePath);
        context.GoldButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GoldButtonSpritePath);
        context.RankingRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RankingRowPrefabPath);

        Require(context, context.PvpTap, "Canvas/PvPTap");
        Require(context, context.Lobby, "Canvas/PvPTap/Lobby");
        Require(context, context.RankingTitle, "Lobby/RankingTitleText");
        Require(context, context.RankingList, "Lobby/RankingList");
        Require(context, context.Viewport, "RankingList/Viewport");
        Require(context, context.Content, "RankingList/Viewport/Content");
        Require(context, context.MyInfo, "Lobby/MyRankingInfoBox");
        Require(context, context.RankingInfo, "MyRankingInfoBox/RankingTextParent");
        Require(context, context.ScoreInfo, "MyRankingInfoBox/ScoreTextParent");
        Require(context, context.RemainInfo, "MyRankingInfoBox/RemainBox");
        Require(context, context.MatchButton, "Lobby/MatchButton");
        Require(context, context.LimitPopup, "Lobby/DailyPvpLimitReachedPopup");
        Require(context, context.TapBar, "Canvas/TapBar");
        Require(context, context.MainTabManager, "MainTabManager");
        Require(context, context.RankingManager, "RankingboardManager");
        Require(context, context.BattleManager, "BattleManager");
        Require(context, context.BattleUiManager, "BattleUIManager");
        Require(context, context.BattleBackendManager, "BattleBackendManager");
        Require(context, context.Font, FontPath);
        Require(context, context.CardSprite, CardSpritePath);
        Require(context, context.GoldButtonSprite, GoldButtonSpritePath);
        Require(context, context.RankingRowPrefab, RankingRowPrefabPath);

        if (context.RankingManager != null) ReadRankingReferences(context);
        if (context.MainTabManager != null) ReadTabReferences(context);
        ValidateTapBar(context);
        return Finish(context, log);
    }

    private static void ReadRankingReferences(Context context)
    {
        SerializedObject serialized = new SerializedObject(context.RankingManager);
        RankingRowUI rowPrefab = Reference<RankingRowUI>(serialized, "rowPrefab");
        Transform contentParent = Reference<Transform>(serialized, "contentParent");
        context.MyRankingText = Reference<TMP_Text>(serialized, "myRankingText");
        context.MyScoreText = Reference<TMP_Text>(serialized, "myScoreText");
        context.RemainCountText = Reference<TMP_Text>(serialized, "remainCountText");

        if (rowPrefab == null || AssetDatabase.GetAssetPath(rowPrefab.gameObject) != RankingRowPrefabPath)
            context.Errors.Add("RankingboardManager/rowPrefab이 예상 RankingRow.prefab과 다릅니다.");
        if (contentParent != context.Content)
            context.Errors.Add("RankingboardManager/contentParent가 실제 RankingList Content와 다릅니다.");
        Require(context, context.MyRankingText, "RankingboardManager/myRankingText");
        Require(context, context.MyScoreText, "RankingboardManager/myScoreText");
        Require(context, context.RemainCountText, "RankingboardManager/remainCountText");
        if (context.MyRankingText != null &&
            !context.MyRankingText.transform.IsChildOf(context.RankingInfo))
            context.Errors.Add("myRankingText가 RankingTextParent 내부 값 Text가 아닙니다.");
        if (context.MyScoreText != null &&
            !context.MyScoreText.transform.IsChildOf(context.ScoreInfo))
            context.Errors.Add("myScoreText가 ScoreTextParent 내부 값 Text가 아닙니다.");
        if (context.RemainCountText != null &&
            (!context.RemainCountText.transform.IsChildOf(context.RemainInfo) ||
             context.RemainCountText.transform.parent == null ||
             context.RemainCountText.transform.parent.name != "CountParent"))
            context.Errors.Add("remainCountText가 RemainBox/CountParent 내부 값 Text가 아닙니다.");
    }

    private static void ReadTabReferences(Context context)
    {
        SerializedObject serialized = new SerializedObject(context.MainTabManager);
        CheckReference(context, serialized, "battleTap", context.PvpTap);
        CheckReference(context, serialized, "battleTapButton",
            Find(context.TapBar, "PvPTapButton")?.GetComponent<Button>());
        CheckReference(context, serialized, "rankingboardManager", context.RankingManager);
    }

    private static void ValidateTapBar(Context context)
    {
        if (context.TapBar == null) return;
        foreach (string name in new[] { "HabitTapButton", "CharacterTapButton", "ShopTapButton", "PvPTapButton" })
            if (Find(context.TapBar, name)?.GetComponent<Button>() == null)
                context.Errors.Add("TapBar 필수 버튼이 없습니다: " + name);
    }

    private static void ApplyLobbyStyle(Context context)
    {
        RectTransform lobbyRect = context.Lobby.GetComponent<RectTransform>();
        SetStretch(lobbyRect, Vector2.zero, Vector2.zero);
        StyleImage(GetOrAdd<Image>(context.Lobby), null, Cream, Image.Type.Simple);

        StyleText(context.RankingTitle, "모험가 PvP 랭킹", context.Font, 48f, Brown, FontStyles.Bold);
        SetTop(context.RankingTitle.rectTransform, new Vector2(920f, 90f), new Vector2(0f, -45f));

        RectTransform listRect = context.RankingList.GetComponent<RectTransform>();
        SetTop(listRect, new Vector2(940f, 1050f), new Vector2(0f, -155f));
        StyleImage(GetOrAdd<Image>(context.RankingList.gameObject), context.CardSprite, Color.white,
            Image.Type.Sliced);
        Record(context.RankingList);
        context.RankingList.horizontal = false;
        context.RankingList.vertical = true;
        context.RankingList.movementType = ScrollRect.MovementType.Clamped;
        context.RankingList.scrollSensitivity = 45f;
        RecordPrefabModification(context.RankingList);
        SetStretch(context.Viewport, new Vector2(26f, 26f), new Vector2(-26f, -26f));

        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(context.Content.gameObject);
        Record(layout);
        layout.padding = new RectOffset(12, 12, 12, 18);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        RecordPrefabModification(layout);
        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(context.Content.gameObject);
        Record(fitter);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        RecordPrefabModification(fitter);
        SetTopStretch(context.Content);

        StyleImage(GetOrAdd<Image>(context.MyInfo.gameObject), context.CardSprite, Color.white,
            Image.Type.Sliced);
        SetBottom(context.MyInfo, new Vector2(940f, 210f), new Vector2(0f, 385f));
        StyleInfoColumn(context.RankingInfo, -300f, "현재 순위", context.MyRankingText, context);
        StyleInfoColumn(context.ScoreInfo, 0f, "PvP 점수", context.MyScoreText, context);
        StyleInfoColumn(context.RemainInfo, 300f, "남은 횟수", context.RemainCountText, context);
        Image remainBackground = context.RemainInfo.GetComponent<Image>();
        StyleImage(remainBackground, context.CardSprite,
            new Color32(0xFF, 0xF9, 0xEA, 0xFF), Image.Type.Sliced);
        Record(remainBackground);
        remainBackground.raycastTarget = false;
        RecordPrefabModification(remainBackground);

        ApplyEmptyRankingText(context);

        StyleButton(context.MatchButton, "매칭 시작", context.GoldButtonSprite, context.Font, 36f);
        SetBottom(context.MatchButton.GetComponent<RectTransform>(), new Vector2(620f, 112f),
            new Vector2(0f, 205f));
    }

    private static void ApplyRankingRowPrefab(Context context)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(RankingRowPrefabPath);
        try
        {
            RankingRowUI row = root.GetComponent<RankingRowUI>();
            if (row == null) throw new InvalidOperationException("RankingRow.prefab 루트에 RankingRowUI가 없습니다.");
            SerializedObject serialized = new SerializedObject(row);
            TMP_Text rank = Reference<TMP_Text>(serialized, "rankText");
            Image title = Reference<Image>(serialized, "titleImage");
            TMP_Text playerName = Reference<TMP_Text>(serialized, "nameText");
            TMP_Text score = Reference<TMP_Text>(serialized, "scoreText");
            if (rank == null || title == null || playerName == null || score == null)
                throw new InvalidOperationException("RankingRowUI의 기존 4개 데이터 참조가 완전하지 않습니다.");

            Image background = root.GetComponent<Image>() ?? root.AddComponent<Image>();
            background.sprite = context.CardSprite;
            background.type = Image.Type.Sliced;
            background.color = Color.white;
            background.raycastTarget = false;
            LayoutElement element = root.GetComponent<LayoutElement>() ?? root.AddComponent<LayoutElement>();
            element.minHeight = 112f;
            element.preferredHeight = 112f;
            element.flexibleHeight = 0f;

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.sizeDelta = new Vector2(0f, 112f);
            SetPrefabRect(rank.rectTransform, new Vector2(100f, 82f),
                new Vector2(70f, 0f), new Vector2(0f, 0.5f));
            SetPrefabRect(title.rectTransform, new Vector2(190f, 40f),
                new Vector2(330f, 25f), new Vector2(0f, 0.5f));
            SetPrefabRect(playerName.rectTransform, new Vector2(440f, 48f),
                new Vector2(470f, -24f), new Vector2(0f, 0.5f));

            RectTransform scoreParent = score.rectTransform.parent as RectTransform;
            if (scoreParent == null)
                throw new InvalidOperationException("RankingRow/ScoreText의 부모 RectTransform이 없습니다.");
            SetPrefabRect(scoreParent, new Vector2(230f, 82f),
                new Vector2(-28f, 0f), new Vector2(1f, 0.5f));
            HorizontalLayoutGroup scoreLayout = scoreParent.GetComponent<HorizontalLayoutGroup>();
            if (scoreLayout == null)
                throw new InvalidOperationException("RankingRow/ScoreTextParent HorizontalLayoutGroup이 없습니다.");
            scoreLayout.padding = new RectOffset(8, 8, 0, 0);
            scoreLayout.spacing = 8f;
            scoreLayout.childAlignment = TextAnchor.MiddleRight;
            scoreLayout.childControlWidth = true;
            scoreLayout.childControlHeight = true;
            scoreLayout.childForceExpandWidth = false;
            scoreLayout.childForceExpandHeight = true;
            TMP_Text scoreLabel = scoreParent.Find("ScoreIsText")?.GetComponent<TMP_Text>();
            if (scoreLabel == null)
                throw new InvalidOperationException("RankingRow/ScoreTextParent/ScoreIsText가 없습니다.");
            scoreLabel.text = "점수";
            StylePrefabText(scoreLabel, context.Font, 24f, MutedBrown, FontStyles.Normal);
            SetPrefabPreferredWidth(scoreLabel.gameObject, 72f);
            SetPrefabPreferredWidth(score.gameObject, 125f);
            StylePrefabText(rank, context.Font, 31f, Brown, FontStyles.Bold);
            title.preserveAspect = true;
            title.raycastTarget = false;
            StylePrefabText(playerName, context.Font, 28f, Brown, FontStyles.Bold);
            playerName.overflowMode = TextOverflowModes.Ellipsis;
            StylePrefabText(score, context.Font, 30f, Gold, FontStyles.Bold);

            PrefabUtility.SaveAsPrefabAsset(root, RankingRowPrefabPath, out bool saved);
            if (!saved) throw new InvalidOperationException("RankingRow.prefab 저장에 실패했습니다.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static List<string> ValidateAppliedStyle(Context context, bool requirePrefabStyle)
    {
        List<string> errors = new List<string>(context.Errors);
        if (!context.IsValid) return errors;
        if (context.RankingTitle.font != context.Font || context.RankingTitle.fontSize < 40f)
            errors.Add("랭킹 제목의 NotoSansKR/가독성 스타일이 올바르지 않습니다.");
        if (!context.RankingList.vertical || context.RankingList.horizontal ||
            context.RankingList.viewport != context.Viewport || context.RankingList.content != context.Content)
            errors.Add("RankingList의 세로 스크롤 또는 기존 Viewport/Content 참조가 올바르지 않습니다.");
        if (context.Content.GetComponent<VerticalLayoutGroup>() == null ||
            context.Content.GetComponent<ContentSizeFitter>() == null)
            errors.Add("Ranking Content에 VerticalLayoutGroup/ContentSizeFitter가 없습니다.");
        if (context.EmptyRankingText == null)
            errors.Add("빈 랭킹 안내 Text가 없습니다. Apply를 실행해 주세요.");
        else if (context.EmptyRankingText.font != context.Font ||
                 context.EmptyRankingText.fontSize < 30f ||
                 context.EmptyRankingText.color.a < 0.95f ||
                 context.EmptyRankingText.alignment != TextAlignmentOptions.Center)
            errors.Add("빈 랭킹 안내의 NotoSansKR/가독성 스타일이 올바르지 않습니다.");
        if (context.MyInfo.GetComponent<Image>()?.sprite != context.CardSprite)
            errors.Add("내 PvP 정보 영역에 RPG 카드 스타일이 적용되지 않았습니다.");
        errors.AddRange(ValidateInfoLayout(context));
        if (context.MatchButton.GetComponent<Image>()?.sprite != context.GoldButtonSprite ||
            context.MatchButton.GetComponentInChildren<TMP_Text>(true)?.font != context.Font)
            errors.Add("매칭 버튼의 RPG/NotoSansKR 스타일이 올바르지 않습니다.");
        if (!context.TapBar.activeSelf)
            errors.Add("PvP 로비에서 표시되어야 하는 TapBar의 초기 상태가 비활성입니다.");
        if (requirePrefabStyle)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RankingRowPrefabPath);
            RankingRowUI row = prefab != null ? prefab.GetComponent<RankingRowUI>() : null;
            if (row == null || prefab.GetComponent<Image>()?.sprite != context.CardSprite ||
                prefab.GetComponent<LayoutElement>() == null)
                errors.Add("RankingRow.prefab RPG 행 카드 스타일이 올바르지 않습니다.");
            else
            {
                SerializedObject serialized = new SerializedObject(row);
                TMP_Text rank = Reference<TMP_Text>(serialized, "rankText");
                Image title = Reference<Image>(serialized, "titleImage");
                TMP_Text name = Reference<TMP_Text>(serialized, "nameText");
                TMP_Text score = Reference<TMP_Text>(serialized, "scoreText");
                if (rank == null || title == null || name == null || score == null)
                    errors.Add("RankingRowUI의 순위/칭호/닉네임/점수 SerializeField 연결이 완전하지 않습니다.");
                else
                {
                    LayoutElement scoreLayout = score.GetComponent<LayoutElement>();
                    if (name.rectTransform.sizeDelta.x < 300f || scoreLayout == null ||
                        scoreLayout.preferredWidth < 100f)
                        errors.Add("RankingRow 닉네임 또는 점수 표시 폭이 부족합니다.");
                }
            }
        }
        return errors;
    }

    private static List<string> ValidateInfoLayout(Context context)
    {
        List<string> errors = new List<string>();
        foreach (RectTransform column in new[]
                 {
                     context.RankingInfo, context.ScoreInfo, context.RemainInfo
                 })
        {
            if (column == null) continue;
            if (column.sizeDelta.x < 260f || column.sizeDelta.y < 140f)
                errors.Add("매칭 정보 열의 표시 영역이 부족합니다: " + GetPath(column));
            LayoutGroup layout = column.GetComponent<LayoutGroup>();
            if (layout != null && layout.enabled)
                errors.Add("매칭 정보 열의 LayoutGroup이 수동 3열 배치와 충돌합니다: " + GetPath(column));
        }
        ValidateInfoText(context.RankingInfo, context.MyRankingText, "현재 순위", errors);
        ValidateInfoText(context.ScoreInfo, context.MyScoreText, "PvP 점수", errors);
        ValidateInfoText(context.RemainInfo, context.RemainCountText, "남은 횟수", errors);
        if (context.RemainInfo != null &&
            context.RemainInfo.GetComponent<Image>()?.sprite != context.CardSprite)
            errors.Add("잔여 매칭 횟수 영역의 흰색 기본 Image가 RPG 카드 Sprite로 교체되지 않았습니다.");
        if (context.MyInfo.anchoredPosition.y < 360f ||
            context.MatchButton.GetComponent<RectTransform>().anchoredPosition.y < 190f)
            errors.Add("매칭 정보 박스 또는 버튼이 하단 TapBar 안전 영역과 겹칠 수 있습니다.");
        return errors;
    }

    private static void StyleInfoColumn(RectTransform rect, float x, string label, TMP_Text valueText,
        Context context)
    {
        if (rect == null) return;
        SetCentered(rect, new Vector2(280f, 170f), new Vector2(x, 0f));
        LayoutGroup layout = rect.GetComponent<LayoutGroup>();
        if (layout != null)
        {
            Record(layout);
            layout.enabled = false;
            RecordPrefabModification(layout);
        }

        TMP_Text[] texts = rect.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text labelText = texts.FirstOrDefault(text => text != valueText);
        if (labelText != null)
        {
            StyleText(labelText, label, context.Font, 32f, MutedBrown, FontStyles.Bold);
            SetCentered(labelText.rectTransform, new Vector2(250f, 58f), new Vector2(0f, 44f));
        }
        if (valueText == null) return;

        StyleText(valueText, null, context.Font, 42f, Brown, FontStyles.Bold);
        RectTransform valueParent = valueText.rectTransform.parent as RectTransform;
        if (valueParent != null && valueParent != rect)
        {
            LayoutGroup valueLayout = valueParent.GetComponent<LayoutGroup>();
            if (valueLayout != null)
            {
                Record(valueLayout);
                valueLayout.enabled = false;
                RecordPrefabModification(valueLayout);
            }
            SetCentered(valueParent, new Vector2(250f, 76f), new Vector2(0f, -38f));
            SetStretch(valueText.rectTransform, Vector2.zero, Vector2.zero);
        }
        else
        {
            SetCentered(valueText.rectTransform, new Vector2(250f, 76f), new Vector2(0f, -38f));
        }
    }

    private static void ApplyEmptyRankingText(Context context)
    {
        TMP_Text text = context.EmptyRankingText;
        if (text == null)
        {
            GameObject item = new GameObject(EmptyRankingTextName,
                typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(item, "Create empty ranking message");
            item.transform.SetParent(context.RankingList.transform, false);
            text = item.GetComponent<TMP_Text>();
            context.EmptyRankingText = text;
        }

        StyleText(text, "아직 등록된 랭킹이 없습니다.", context.Font, 34f,
            MutedBrown, FontStyles.Bold);
        SetStretch(text.rectTransform, new Vector2(80f, 80f), new Vector2(-80f, -80f));
        Record(text.gameObject);
        text.gameObject.SetActive(false);
        RecordPrefabModification(text.gameObject);
    }

    private static void ValidateInfoText(RectTransform column, TMP_Text valueText,
        string expectedLabel, List<string> errors)
    {
        if (column == null || valueText == null) return;
        TMP_Text labelText = column.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(text => text != valueText);
        if (labelText == null)
        {
            errors.Add("매칭 정보 라벨 Text가 없습니다: " + GetPath(column));
            return;
        }
        if (labelText.text != expectedLabel || labelText.fontSize < 30f ||
            labelText.color.a < 0.95f || labelText.alignment != TextAlignmentOptions.Center ||
            labelText.enableAutoSizing)
            errors.Add(expectedLabel + " 라벨의 문구/폰트 크기/정렬/Alpha/Auto Size가 올바르지 않습니다.");
        if (valueText.fontSize < 40f || valueText.color.a < 0.95f ||
            valueText.alignment != TextAlignmentOptions.Center || valueText.enableAutoSizing)
            errors.Add(expectedLabel + " 값의 폰트 크기/정렬/Alpha/Auto Size가 올바르지 않습니다.");
        if (!RectTransformContains(column, labelText.rectTransform) ||
            !RectTransformContains(column, valueText.rectTransform))
            errors.Add(expectedLabel + " 라벨 또는 값이 정보 카드 경계를 벗어납니다.");
        RectTransform valueParent = valueText.rectTransform.parent as RectTransform;
        LayoutGroup valueLayout = valueParent != null ? valueParent.GetComponent<LayoutGroup>() : null;
        if (valueLayout != null && valueLayout.enabled)
            errors.Add(expectedLabel + " 값 부모 LayoutGroup이 수동 배치와 충돌합니다.");
    }

    private static bool RectTransformContains(RectTransform parent, RectTransform child)
    {
        Vector3[] corners = new Vector3[4];
        child.GetWorldCorners(corners);
        return corners.All(corner => parent.rect.Contains(parent.InverseTransformPoint(corner)));
    }

    private static void StyleButton(Button button, string label, Sprite sprite, TMP_FontAsset font,
        float fontSize)
    {
        StyleImage(button.GetComponent<Image>(), sprite, Color.white, Image.Type.Sliced);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        StyleText(text, label, font, fontSize, Brown, FontStyles.Bold);
        SetStretch(text.rectTransform, new Vector2(18f, 8f), new Vector2(-18f, -8f));
        Record(button);
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(0xFF, 0xF4, 0xC9, 0xFF);
        colors.pressedColor = new Color32(0xD8, 0xB5, 0x6C, 0xFF);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        RecordPrefabModification(button);
    }

    private static void StyleImage(Image image, Sprite sprite, Color color, Image.Type type)
    {
        Record(image);
        image.sprite = sprite;
        image.color = color;
        image.type = type;
        RecordPrefabModification(image);
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
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        RecordPrefabModification(text);
    }

    private static void StylePrefabText(TMP_Text text, TMP_FontAsset font, float size, Color color,
        FontStyles style)
    {
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
    }

    private static void SetPrefabRect(RectTransform rect, Vector2 size, Vector2 position,
        Vector2 anchor)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void SetPrefabPreferredWidth(GameObject target, float width)
    {
        LayoutElement element = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
        element.minWidth = width;
        element.preferredWidth = width;
        element.flexibleWidth = 0f;
    }

    private static string BuildDiagnostics(Context context)
    {
        return "Hierarchy: Canvas/PvPTap/Lobby/{RankingTitleText, RankingList/Viewport/Content, " +
               "MyRankingInfoBox, MatchButton, DailyPvpLimitReachedPopup}\n" +
               "Ranking manager: " + GetPath(context.RankingManager.transform) +
               ", content=" + GetPath(context.Content) +
               ", prefab=" + RankingRowPrefabPath + "\n" +
               "Initial state: PvPTap=" + context.PvpTap.activeSelf +
               ", Lobby=" + context.Lobby.activeSelf +
               ", LimitPopup=" + context.LimitPopup.activeSelf +
               ", TapBar=" + context.TapBar.activeSelf + "\n" +
               "별도 랭킹 패널/진입 버튼/복귀 버튼은 현재 실제 Hierarchy에 없습니다.";
    }

    private static Context Finish(Context context, bool log)
    {
        if (log)
            foreach (string error in context.Errors)
                Debug.LogError("[HabitPVP MainScene] PvP 로비·랭킹: " + error);
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

    private static T Reference<T>(SerializedObject serialized, string name) where T : UnityEngine.Object
    {
        SerializedProperty property = serialized.FindProperty(name);
        return property == null ? null : property.objectReferenceValue as T;
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

    private static void SetStretch(RectTransform rect, Vector2 minOffset, Vector2 maxOffset)
    {
        Record(rect);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = minOffset;
        rect.offsetMax = maxOffset;
        RecordPrefabModification(rect);
    }

    private static void SetTop(RectTransform rect, Vector2 size, Vector2 position)
    {
        Record(rect);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        RecordPrefabModification(rect);
    }

    private static void SetBottom(RectTransform rect, Vector2 size, Vector2 position)
    {
        Record(rect);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        RecordPrefabModification(rect);
    }

    private static void SetCentered(RectTransform rect, Vector2 size, Vector2 position)
    {
        Record(rect);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        RecordPrefabModification(rect);
    }

    private static void SetTopStretch(RectTransform rect)
    {
        Record(rect);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        RecordPrefabModification(rect);
    }

    private static void Record(UnityEngine.Object target)
    {
        if (target != null) Undo.RecordObject(target, "Apply PvP Lobby Ranking UI Renewal");
    }

    private static void RecordPrefabModification(UnityEngine.Object target)
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
        private readonly Dictionary<int, int> siblingIndices;
        private readonly ScrollRect viewportOwner;
        private readonly RectTransform viewport;
        private readonly RectTransform content;

        private ProtectionSnapshot(Context context)
        {
            Component[] managers =
            {
                context.MainTabManager, context.RankingManager, context.BattleManager,
                context.BattleUiManager, context.BattleBackendManager
            };
            managerReferences = managers.ToDictionary(item => item.GetInstanceID(), CaptureObjectReferences);
            buttonEvents = context.Canvas.GetComponentsInChildren<Button>(true)
                .ToDictionary(button => button.GetInstanceID(), button => CaptureEvent(button.onClick));
            Transform[] transforms = context.PvpTap.GetComponentsInChildren<Transform>(true)
                .Concat(context.TapBar.GetComponentsInChildren<Transform>(true)).Distinct().ToArray();
            activeStates = transforms.ToDictionary(item => item.GetInstanceID(), item => item.gameObject.activeSelf);
            siblingIndices = transforms.ToDictionary(item => item.GetInstanceID(), item => item.GetSiblingIndex());
            viewportOwner = context.RankingList;
            viewport = context.RankingList.viewport;
            content = context.RankingList.content;
        }

        public static ProtectionSnapshot Capture(Context context) => new ProtectionSnapshot(context);

        public void AssertUnchanged(Context context)
        {
            foreach (Component manager in new Component[]
                     {
                         context.MainTabManager, context.RankingManager, context.BattleManager,
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
                if (activeStates.TryGetValue(id, out bool active) && active != item.gameObject.activeSelf)
                    throw new InvalidOperationException("기존 activeSelf가 변경되었습니다: " + GetPath(item));
                if (siblingIndices.TryGetValue(id, out int sibling) && sibling != item.GetSiblingIndex())
                    throw new InvalidOperationException("기존 sibling 순서가 변경되었습니다: " + GetPath(item));
            }
            if (viewportOwner != context.RankingList || viewport != context.RankingList.viewport ||
                content != context.RankingList.content)
                throw new InvalidOperationException("RankingList의 기존 Viewport/Content 참조가 변경되었습니다.");
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
        public GameObject Canvas, PvpTap, Lobby, LimitPopup, TapBar, RankingRowPrefab;
        public TMP_Text RankingTitle, MyRankingText, MyScoreText, RemainCountText, EmptyRankingText;
        public ScrollRect RankingList;
        public RectTransform Viewport, Content, MyInfo, RankingInfo, ScoreInfo, RemainInfo;
        public Button MatchButton;
        public MainTabManager MainTabManager;
        public RankingboardManager RankingManager;
        public BattleManager BattleManager;
        public BattleUIManager BattleUiManager;
        public BattleBackendManager BattleBackendManager;
        public TMP_FontAsset Font;
        public Sprite CardSprite, GoldButtonSprite;
    }
}
