using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PvpBattleResultIntegrationEditor
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string CanvasPrefabPath = "Assets/Resources/UIPrefabs/Canvas.prefab";
    private const string FontPath = "Assets/Login/Fonts/NotoSansKR Login SDF.asset";
    private const string CardSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/panel_rpg_card.png";
    private const string GoldButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_rpg.png";
    private const string MenuRoot =
        "Tools/HabitPVP/UI Renewal/MainScene/15-D-7D PvP Battle Result/";

    private const string TitleName = "__15D7D_ResultTitle";
    private const string MessageName = "__15D7D_ResultMessage";
    private const string PlayerPanelName = "__15D7D_PlayerPanel";
    private const string MyNameName = "__15D7D_MyName";
    private const string OpponentNameName = "__15D7D_OpponentName";
    private const string VersusName = "__15D7D_Versus";
    private const string RankingTitleName = "__15D7D_RankingTitle";
    private const string FinalScoreName = "__15D7D_FinalScore";

    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 CreamLight = new Color32(0xFF, 0xF9, 0xEA, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x86, 0x68, 0x5D, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);
    private static readonly Color32 Backdrop = new Color32(0x35, 0x24, 0x25, 0xA6);
    private static readonly Color32 ReturnButtonNormal = new Color32(0xE8, 0xCC, 0x94, 0xFF);
    private static readonly Color32 ReturnButtonHighlighted = new Color32(0xF0, 0xDB, 0xAC, 0xFF);
    private static readonly Color32 ReturnButtonPressed = new Color32(0xD9, 0xB7, 0x7C, 0xFF);
    private static readonly Color32 ReturnButtonDisabled = new Color32(0xC8, 0xB1, 0x88, 0xA6);
    private static readonly Color32 ReturnButtonText = new Color32(0x62, 0x46, 0x28, 0xFF);

    [MenuItem(MenuRoot + "Validate Structure", false, 2400)]
    private static void ValidateStructureMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid) return;

        Debug.Log("[HabitPVP 15-D-7D] 결과 화면 구조 검증 성공. 파일을 변경하지 않았습니다.\n" +
                  BuildDiagnostics(context), context.ResultPanel);
    }

    [MenuItem(MenuRoot + "Dry Run", false, 2401)]
    private static void DryRunMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid)
        {
            Debug.LogError("[HabitPVP 15-D-7D] Dry Run 중단: 구조 오류를 먼저 해결해 주세요.");
            return;
        }

        Debug.Log(
            "[HabitPVP 15-D-7D] PvP 결과 화면 Dry Run (변경/Dirty/저장 없음)\n" +
            "- 기존 ResultPanel/ResultBackground, WIN/LOSE/DRAW 상태 오브젝트, 점수 3개와 CheckButton을 재사용\n" +
            "- 크림·골드 RPG 결과 카드, 동적 VICTORY/DEFEAT/DRAW 헤더와 결과 메시지 적용\n" +
            "- Sprite 없는 단색 WIN/LOSE/DRAW Image 렌더링만 비활성화하여 큰 색상 사각형 제거\n" +
            "- 기존 Battle 닉네임을 복사하는 좌우 플레이어 카드와 VS 표시 추가\n" +
            "- 기존 ScoreBefore, 부호, ScoreDelta를 보존하고 계산된 변경 후 RP를 추가\n" +
            "- CheckButton은 기존 참조/Listener/크기를 유지하고 밝은 베이지 상태색과 진갈색 글자로 대비 개선\n" +
            "- 고정 이름의 15-D-7D 오브젝트와 Presenter를 재사용하여 반복 Apply 시 중복 생성 방지\n" +
            "- Canvas.prefab 원본, BattleUIManager/Photon/서버 결과 처리, Scene 저장은 변경하지 않음\n" +
            BuildDiagnostics(context), context.ResultPanel);
    }

    [MenuItem(MenuRoot + "Apply (No Auto Save)", false, 2402)]
    private static void ApplyMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid) return;

        ProtectionSnapshot snapshot = ProtectionSnapshot.Capture(context);
        Hash128 prefabHash = AssetDatabase.GetAssetDependencyHash(CanvasPrefabPath);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply 15-D-7D PvP Battle Result UI Renewal");

        try
        {
            ApplyDesign(context);
            snapshot.AssertUnchanged(context);
            if (AssetDatabase.GetAssetDependencyHash(CanvasPrefabPath) != prefabHash)
                throw new InvalidOperationException("Canvas.prefab 원본의 dependency hash가 변경되었습니다.");

            List<string> errors = ValidateAppliedDesign(context);
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", errors));

            EditorSceneManager.MarkSceneDirty(context.Scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = context.ResultPanel;
            Debug.Log("[HabitPVP 15-D-7D] 결과 UI 적용 성공. MainScene은 dirty 상태이며 자동 저장하지 않았습니다. " +
                      "Canvas.prefab과 결과 처리 로직은 변경하지 않았습니다.", context.ResultPanel);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(group);
            Debug.LogException(exception);
            Debug.LogError("[HabitPVP 15-D-7D] Apply 실패. 이번 Scene 변경을 Undo로 복원했습니다. " +
                           "Scene 저장이나 Prefab Apply는 수행하지 않았습니다.");
        }
    }

    [MenuItem(MenuRoot + "Validate Applied Design", false, 2403)]
    private static void ValidateAppliedDesignMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid) return;

        List<string> errors = ValidateAppliedDesign(context);
        if (errors.Count == 0)
        {
            Debug.Log("[HabitPVP 15-D-7D] 적용 디자인 검증 성공. 파일을 변경하지 않았습니다.\n" +
                      "정적 검증 범위: 결과 분기, 닉네임 복사, 점수 표시, 버튼 참조, 1080x1920 배치, 중복 방지.\n" +
                      BuildDiagnostics(context), context.ResultPanel);
            return;
        }

        foreach (string error in errors)
            Debug.LogError("[HabitPVP 15-D-7D] 적용 디자인 오류: " + error, context.ResultPanel);
    }

    [MenuItem(MenuRoot + "Validate Structure", true)]
    [MenuItem(MenuRoot + "Dry Run", true)]
    [MenuItem(MenuRoot + "Apply (No Auto Save)", true)]
    [MenuItem(MenuRoot + "Validate Applied Design", true)]
    private static bool CanUseMenus()
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
        context.Battle = Find(context.PvpTap, "Battle")?.gameObject;
        context.ResultPanel = Find(context.Battle, "ResultPanel")?.gameObject;
        context.ResultBackground = FindRect(context.ResultPanel, "ResultBackground");
        context.ResultImages = FindRect(context.ResultBackground, "ResultImages");
        context.WinImage = Find(context.ResultImages, "WinImage")?.gameObject;
        context.LoseImage = Find(context.ResultImages, "LoseImage")?.gameObject;
        context.DrawImage = Find(context.ResultImages, "DrawImage")?.gameObject;
        context.RankingPoint = FindRect(context.ResultBackground, "RankingPointText");
        context.ScoreBefore = FindComponent<TMP_Text>(context.RankingPoint, "CurrentRankingPointText");
        context.ScoreOperator = FindComponent<TMP_Text>(context.RankingPoint, "OperatorText");
        context.ScoreDelta = FindComponent<TMP_Text>(context.RankingPoint, "ResultRankingPointText");
        context.CheckButton = FindComponent<Button>(context.ResultBackground, "CheckButton");
        context.CheckButtonText = context.CheckButton != null
            ? context.CheckButton.GetComponentInChildren<TMP_Text>(true)
            : null;
        context.Manager = FindUnique<BattleUIManager>(context);
        context.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        context.CardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardSpritePath);
        context.GoldButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GoldButtonSpritePath);

        Require(context, context.PvpTap, "Canvas/PvPTap");
        Require(context, context.Battle, "Canvas/PvPTap/Battle");
        Require(context, context.ResultPanel, "Battle/ResultPanel");
        Require(context, context.ResultBackground, "ResultPanel/ResultBackground");
        Require(context, context.ResultImages, "ResultBackground/ResultImages");
        Require(context, context.WinImage, "ResultImages/WinImage");
        Require(context, context.LoseImage, "ResultImages/LoseImage");
        Require(context, context.DrawImage, "ResultImages/DrawImage");
        Require(context, context.RankingPoint, "ResultBackground/RankingPointText");
        Require(context, context.ScoreBefore, "RankingPointText/CurrentRankingPointText");
        Require(context, context.ScoreOperator, "RankingPointText/OperatorText");
        Require(context, context.ScoreDelta, "RankingPointText/ResultRankingPointText");
        Require(context, context.CheckButton, "ResultBackground/CheckButton");
        Require(context, context.CheckButtonText, "CheckButton TMP text");
        Require(context, context.Font, FontPath);
        Require(context, context.CardSprite, CardSpritePath);
        Require(context, context.GoldButtonSprite, GoldButtonSpritePath);

        if (context.Manager != null)
            ValidateManagerReferences(context);
        if (context.ResultPanel != null && context.ResultPanel.activeSelf)
            context.Errors.Add("ResultPanel의 초기 activeSelf는 false여야 합니다.");

        return Finish(context, log);
    }

    private static void ValidateManagerReferences(Context context)
    {
        SerializedObject serialized = new SerializedObject(context.Manager);
        CheckReference(context, serialized, "battlePanel", context.Battle);
        CheckReference(context, serialized, "resultPanel", context.ResultPanel);
        CheckReference(context, serialized, "winImage", context.WinImage);
        CheckReference(context, serialized, "loseImage", context.LoseImage);
        CheckReference(context, serialized, "drawImage", context.DrawImage);
        CheckReference(context, serialized, "currentRankingPointText", context.ScoreBefore);
        CheckReference(context, serialized, "operatorText", context.ScoreOperator);
        CheckReference(context, serialized, "resultRankingPointText", context.ScoreDelta);
        CheckReference(context, serialized, "checkButton", context.CheckButton);

        context.MyNameSource = serialized.FindProperty("myName")?.objectReferenceValue as TMP_Text;
        context.OpponentNameSource = serialized.FindProperty("oppName")?.objectReferenceValue as TMP_Text;
        Require(context, context.MyNameSource, "BattleUIManager.myName");
        Require(context, context.OpponentNameSource, "BattleUIManager.oppName");
    }

    private static void ApplyDesign(Context context)
    {
        SetRect(context.ResultPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
            Vector2.zero, Vector2.zero, Vector2.zero);
        Image rootImage = context.ResultPanel.GetComponent<Image>();
        if (rootImage == null)
            rootImage = Undo.AddComponent<Image>(context.ResultPanel);
        StyleImage(rootImage, null, Backdrop, Image.Type.Simple, true);

        SetRect(context.ResultBackground, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(930f, 1380f));
        StyleImage(context.ResultBackground.GetComponent<Image>(), context.CardSprite, Cream,
            Image.Type.Sliced, true);

        TMP_Text title = EnsureText(context.ResultBackground, TitleName, context.Font);
        TMP_Text message = EnsureText(context.ResultBackground, MessageName, context.Font);
        StyleText(title, 78f, 54f, 84f, Gold, FontStyles.Bold);
        StyleText(message, 34f, 28f, 40f, MutedBrown, FontStyles.Bold);
        SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(780f, 96f));
        SetRect(message.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(780f, 58f));

        SetRect(context.ResultImages, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -278f), new Vector2(620f, 110f));
        foreach (Image image in context.ResultImages.GetComponentsInChildren<Image>(true))
        {
            Record(image, "Hide legacy solid result image");
            if (image.sprite == null)
                image.enabled = false;
            image.preserveAspect = true;
            image.raycastTarget = false;
            SaveOverride(image);
        }

        RectTransform playerPanel = EnsurePanel(context.ResultBackground, PlayerPanelName,
            context.CardSprite, CreamLight);
        SetRect(playerPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 145f), new Vector2(780f, 230f));
        TMP_Text myName = EnsureText(playerPanel, MyNameName, context.Font);
        TMP_Text versus = EnsureText(playerPanel, VersusName, context.Font);
        TMP_Text opponentName = EnsureText(playerPanel, OpponentNameName, context.Font);
        StyleText(myName, 38f, 26f, 44f, Brown, FontStyles.Bold);
        StyleText(versus, 42f, 36f, 46f, Gold, FontStyles.Bold);
        StyleText(opponentName, 38f, 26f, 44f, Brown, FontStyles.Bold);
        versus.text = "VS";
        SetRect(myName.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(35f, 0f), new Vector2(290f, 130f));
        SetRect(versus.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f));
        SetRect(opponentName.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f), new Vector2(-35f, 0f), new Vector2(290f, 130f));

        TMP_Text rankingTitle = EnsureText(context.ResultBackground, RankingTitleName, context.Font);
        rankingTitle.text = "RANKING POINT";
        StyleText(rankingTitle, 34f, 30f, 38f, Brown, FontStyles.Bold);
        SetRect(rankingTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -36f), new Vector2(700f, 55f));

        SetRect(context.RankingPoint, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -130f), new Vector2(660f, 100f));
        StyleScoreText(context.ScoreBefore, 48f, Brown);
        StyleScoreText(context.ScoreOperator, 52f, Gold);
        StyleScoreText(context.ScoreDelta, 48f, Gold);
        SetRect(context.ScoreBefore.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(-190f, 0f), new Vector2(250f, 82f));
        SetRect(context.ScoreOperator.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 82f));
        SetRect(context.ScoreDelta.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(190f, 0f), new Vector2(250f, 82f));

        TMP_Text finalScore = EnsureText(context.ResultBackground, FinalScoreName, context.Font);
        StyleText(finalScore, 38f, 30f, 44f, Brown, FontStyles.Bold);
        SetRect(finalScore.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -226f), new Vector2(700f, 68f));

        SetRect(context.CheckButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 92f),
            new Vector2(650f, 132f));
        StyleImage(context.CheckButton.image, context.GoldButtonSprite, Color.white, Image.Type.Sliced, true);
        Record(context.CheckButton, "Style result return button");
        context.CheckButton.transition = Selectable.Transition.ColorTint;
        context.CheckButton.targetGraphic = context.CheckButton.image;
        ColorBlock colors = context.CheckButton.colors;
        colors.normalColor = ReturnButtonNormal;
        colors.highlightedColor = ReturnButtonHighlighted;
        colors.pressedColor = ReturnButtonPressed;
        colors.selectedColor = ReturnButtonHighlighted;
        colors.disabledColor = ReturnButtonDisabled;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        context.CheckButton.colors = colors;
        SaveOverride(context.CheckButton);
        context.CheckButtonText.text = "PvP 로비로 돌아가기";
        StyleText(context.CheckButtonText, 38f, 30f, 42f, ReturnButtonText, FontStyles.Bold);
        SetRect(context.CheckButtonText.rectTransform, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-60f, -34f));

        PvpBattleResultPresenter presenter = context.ResultPanel.GetComponent<PvpBattleResultPresenter>();
        if (presenter == null)
            presenter = Undo.AddComponent<PvpBattleResultPresenter>(context.ResultPanel);
        SerializedObject presenterSerialized = new SerializedObject(presenter);
        SetReference(presenterSerialized, "myNameSource", context.MyNameSource);
        SetReference(presenterSerialized, "opponentNameSource", context.OpponentNameSource);
        SetReference(presenterSerialized, "winImage", context.WinImage);
        SetReference(presenterSerialized, "loseImage", context.LoseImage);
        SetReference(presenterSerialized, "drawImage", context.DrawImage);
        SetReference(presenterSerialized, "scoreBeforeSource", context.ScoreBefore);
        SetReference(presenterSerialized, "scoreOperatorSource", context.ScoreOperator);
        SetReference(presenterSerialized, "scoreDeltaSource", context.ScoreDelta);
        SetReference(presenterSerialized, "resultTitle", title);
        SetReference(presenterSerialized, "resultMessage", message);
        SetReference(presenterSerialized, "myNameText", myName);
        SetReference(presenterSerialized, "opponentNameText", opponentName);
        SetReference(presenterSerialized, "finalScoreText", finalScore);
        presenterSerialized.ApplyModifiedProperties();
        SaveOverride(presenter);
    }

    private static List<string> ValidateAppliedDesign(Context context)
    {
        List<string> errors = new List<string>();
        TMP_Text title = FindComponent<TMP_Text>(context.ResultBackground, TitleName);
        TMP_Text message = FindComponent<TMP_Text>(context.ResultBackground, MessageName);
        TMP_Text myName = FindComponent<TMP_Text>(context.ResultBackground, PlayerPanelName + "/" + MyNameName);
        TMP_Text opponentName = FindComponent<TMP_Text>(context.ResultBackground,
            PlayerPanelName + "/" + OpponentNameName);
        TMP_Text finalScore = FindComponent<TMP_Text>(context.ResultBackground, FinalScoreName);
        PvpBattleResultPresenter[] presenters = context.ResultPanel.GetComponents<PvpBattleResultPresenter>();

        if (title == null || message == null || myName == null || opponentName == null || finalScore == null)
            errors.Add("15-D-7D 결과 텍스트 또는 플레이어 카드가 누락되었습니다.");
        if (presenters.Length != 1)
            errors.Add("PvpBattleResultPresenter가 정확히 하나여야 합니다. 현재: " + presenters.Length);
        if (context.ResultBackground.sizeDelta != new Vector2(930f, 1380f))
            errors.Add("ResultBackground 크기가 1080x1920 기준값과 다릅니다.");
        if (context.CheckButton.GetComponent<RectTransform>().sizeDelta != new Vector2(650f, 132f))
            errors.Add("로비 복귀 버튼 터치 영역이 적용값과 다릅니다.");
        if (context.CheckButtonText.text != "PvP 로비로 돌아가기")
            errors.Add("로비 복귀 버튼 문구가 적용되지 않았습니다.");
        foreach (Image image in context.ResultImages.GetComponentsInChildren<Image>(true))
            if (image.sprite == null && image.enabled)
                errors.Add("Sprite 없는 기존 결과 Image 렌더링이 활성 상태입니다: " +
                           image.gameObject.name);
        if (context.CheckButton.transition != Selectable.Transition.ColorTint)
            errors.Add("로비 복귀 버튼 Transition이 Color Tint가 아닙니다.");
        if (context.CheckButton.targetGraphic != context.CheckButton.image)
            errors.Add("로비 복귀 버튼 Target Graphic이 기존 Button Image가 아닙니다.");
        ColorBlock buttonColors = context.CheckButton.colors;
        if (!SameColor(buttonColors.normalColor, ReturnButtonNormal) ||
            !SameColor(buttonColors.highlightedColor, ReturnButtonHighlighted) ||
            !SameColor(buttonColors.pressedColor, ReturnButtonPressed) ||
            !SameColor(buttonColors.selectedColor, ReturnButtonHighlighted) ||
            !SameColor(buttonColors.disabledColor, ReturnButtonDisabled))
            errors.Add("로비 복귀 버튼 상태 색상이 15-D-7D 가독성 팔레트와 다릅니다.");
        if (!SameColor(context.CheckButtonText.color, ReturnButtonText))
            errors.Add("로비 복귀 버튼 TMP 글자색 또는 Alpha가 적용값과 다릅니다.");
        if (CountNamed(context.ResultPanel.transform, TitleName) != 1 ||
            CountNamed(context.ResultPanel.transform, PlayerPanelName) != 1 ||
            CountNamed(context.ResultPanel.transform, FinalScoreName) != 1)
            errors.Add("반복 Apply로 15-D-7D 오브젝트가 중복되었거나 누락되었습니다.");

        if (presenters.Length == 1)
        {
            SerializedObject serialized = new SerializedObject(presenters[0]);
            foreach (string property in new[]
                     {
                         "myNameSource", "opponentNameSource", "winImage", "loseImage", "drawImage",
                         "scoreBeforeSource", "scoreOperatorSource", "scoreDeltaSource", "resultTitle",
                         "resultMessage", "myNameText", "opponentNameText", "finalScoreText"
                     })
                if (serialized.FindProperty(property)?.objectReferenceValue == null)
                    errors.Add("Presenter 참조가 비어 있습니다: " + property);
        }

        return errors;
    }

    private static Context Finish(Context context, bool log)
    {
        if (context.Manager == null)
            context.Errors.Add("MainScene의 BattleUIManager를 정확히 찾을 수 없습니다.");
        if (log)
            foreach (string error in context.Errors.Distinct())
                Debug.LogError("[HabitPVP 15-D-7D] " + error);
        return context;
    }

    private static string BuildDiagnostics(Context context)
    {
        return "- Scene: " + context.Scene.path + "\n" +
               "- Target: Canvas/PvPTap/Battle/ResultPanel/ResultBackground\n" +
               "- Result support: WIN / LOSE / DRAW (기존 BattleUIManager 분기 재사용)\n" +
               "- Score source: ScoreBefore + ScoreDelta (기존 부호 텍스트 재사용)\n" +
               "- Return: 기존 CheckButton -> BattleUIManager 런타임 Listener 유지\n" +
               "- Canvas source: " + CanvasPrefabPath;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
    }

    private static Transform Find(Component root, string path)
    {
        return root != null ? root.transform.Find(path) : null;
    }

    private static Transform Find(GameObject root, string path)
    {
        return root != null ? root.transform.Find(path) : null;
    }

    private static RectTransform FindRect(Component root, string path)
    {
        return Find(root, path) as RectTransform;
    }

    private static RectTransform FindRect(GameObject root, string path)
    {
        return Find(root, path) as RectTransform;
    }

    private static T FindComponent<T>(Component root, string path) where T : Component
    {
        Transform transform = Find(root, path);
        return transform != null ? transform.GetComponent<T>() : null;
    }

    private static T FindComponent<T>(GameObject root, string path) where T : Component
    {
        Transform transform = Find(root, path);
        return transform != null ? transform.GetComponent<T>() : null;
    }

    private static T FindUnique<T>(Context context) where T : Component
    {
        T[] values = Resources.FindObjectsOfTypeAll<T>()
            .Where(value => value.gameObject.scene == context.Scene).ToArray();
        if (values.Length != 1)
        {
            context.Errors.Add(typeof(T).Name + "이 MainScene에 정확히 하나 있어야 합니다. 현재: " + values.Length);
            return null;
        }
        return values[0];
    }

    private static void Require(Context context, UnityEngine.Object value, string label)
    {
        if (value == null) context.Errors.Add("필수 대상이 없습니다: " + label);
    }

    private static void CheckReference(Context context, SerializedObject serialized, string propertyName,
        UnityEngine.Object expected)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            context.Errors.Add("BattleUIManager SerializeField가 없습니다: " + propertyName);
        else if (property.objectReferenceValue != expected)
            context.Errors.Add("BattleUIManager." + propertyName + " 참조가 실제 Hierarchy와 다릅니다.");
    }

    private static RectTransform EnsurePanel(Transform parent, string name, Sprite sprite, Color color)
    {
        Transform existing = parent.Find(name);
        GameObject target;
        if (existing != null)
        {
            target = existing.gameObject;
        }
        else
        {
            target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(target, "Create " + name);
            target.transform.SetParent(parent, false);
        }

        StyleImage(target.GetComponent<Image>(), sprite, color, Image.Type.Sliced, false);
        return target.GetComponent<RectTransform>();
    }

    private static TMP_Text EnsureText(Transform parent, string name, TMP_FontAsset font)
    {
        Transform existing = parent.Find(name);
        TMP_Text text;
        if (existing != null)
        {
            text = existing.GetComponent<TMP_Text>();
            if (text == null) throw new InvalidOperationException(name + "에 TMP_Text가 없습니다.");
        }
        else
        {
            GameObject target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(target, "Create " + name);
            target.transform.SetParent(parent, false);
            text = target.GetComponent<TMP_Text>();
        }

        Record(text, "Configure " + name);
        text.font = font;
        text.raycastTarget = false;
        SaveOverride(text);
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 position, Vector2 size)
    {
        Record(rect, "Set result layout");
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        SaveOverride(rect);
    }

    private static void StyleText(TMP_Text text, float size, float minSize, float maxSize,
        Color color, FontStyles style)
    {
        Record(text, "Style result text");
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        SaveOverride(text);
    }

    private static void StyleScoreText(TMP_Text text, float size, Color color)
    {
        StyleText(text, size, 32f, size, color, FontStyles.Bold);
    }

    private static void StyleImage(Image image, Sprite sprite, Color color, Image.Type type, bool raycast)
    {
        if (image == null) return;
        Record(image, "Style result image");
        image.sprite = sprite;
        image.color = color;
        image.type = type;
        image.raycastTarget = raycast;
        SaveOverride(image);
    }

    private static void SetReference(SerializedObject serialized, string propertyName,
        UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null) throw new InvalidOperationException("Presenter 필드가 없습니다: " + propertyName);
        property.objectReferenceValue = value;
    }

    private static int CountNamed(Transform root, string name)
    {
        return root.GetComponentsInChildren<Transform>(true).Count(item => item.name == name);
    }

    private static bool SameColor(Color left, Color right)
    {
        return Mathf.Abs(left.r - right.r) < 0.001f &&
               Mathf.Abs(left.g - right.g) < 0.001f &&
               Mathf.Abs(left.b - right.b) < 0.001f &&
               Mathf.Abs(left.a - right.a) < 0.001f;
    }

    private static void Record(UnityEngine.Object target, string action)
    {
        if (target != null) Undo.RecordObject(target, action);
    }

    private static void SaveOverride(UnityEngine.Object target)
    {
        if (target != null && PrefabUtility.IsPartOfPrefabInstance(target))
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private sealed class ProtectionSnapshot
    {
        private readonly UnityEngine.Object[] managerReferences;
        private readonly bool resultPanelActive;
        private readonly int persistentEventCount;

        private ProtectionSnapshot(Context context)
        {
            SerializedObject serialized = new SerializedObject(context.Manager);
            managerReferences = new[]
            {
                serialized.FindProperty("resultPanel").objectReferenceValue,
                serialized.FindProperty("winImage").objectReferenceValue,
                serialized.FindProperty("loseImage").objectReferenceValue,
                serialized.FindProperty("drawImage").objectReferenceValue,
                serialized.FindProperty("currentRankingPointText").objectReferenceValue,
                serialized.FindProperty("operatorText").objectReferenceValue,
                serialized.FindProperty("resultRankingPointText").objectReferenceValue,
                serialized.FindProperty("checkButton").objectReferenceValue
            };
            resultPanelActive = context.ResultPanel.activeSelf;
            persistentEventCount = context.CheckButton.onClick.GetPersistentEventCount();
        }

        public static ProtectionSnapshot Capture(Context context)
        {
            return new ProtectionSnapshot(context);
        }

        public void AssertUnchanged(Context context)
        {
            SerializedObject serialized = new SerializedObject(context.Manager);
            UnityEngine.Object[] current =
            {
                serialized.FindProperty("resultPanel").objectReferenceValue,
                serialized.FindProperty("winImage").objectReferenceValue,
                serialized.FindProperty("loseImage").objectReferenceValue,
                serialized.FindProperty("drawImage").objectReferenceValue,
                serialized.FindProperty("currentRankingPointText").objectReferenceValue,
                serialized.FindProperty("operatorText").objectReferenceValue,
                serialized.FindProperty("resultRankingPointText").objectReferenceValue,
                serialized.FindProperty("checkButton").objectReferenceValue
            };
            if (!managerReferences.SequenceEqual(current))
                throw new InvalidOperationException("BattleUIManager 결과 SerializeField 참조가 변경되었습니다.");
            if (resultPanelActive != context.ResultPanel.activeSelf)
                throw new InvalidOperationException("ResultPanel 초기 activeSelf가 변경되었습니다.");
            if (persistentEventCount != context.CheckButton.onClick.GetPersistentEventCount())
                throw new InvalidOperationException("CheckButton Persistent UnityEvent 수가 변경되었습니다.");
        }
    }

    private sealed class Context
    {
        public Scene Scene;
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
        public GameObject Canvas, PvpTap, Battle, ResultPanel;
        public RectTransform ResultBackground, ResultImages, RankingPoint;
        public GameObject WinImage, LoseImage, DrawImage;
        public TMP_Text ScoreBefore, ScoreOperator, ScoreDelta, CheckButtonText;
        public TMP_Text MyNameSource, OpponentNameSource;
        public Button CheckButton;
        public BattleUIManager Manager;
        public TMP_FontAsset Font;
        public Sprite CardSprite, GoldButtonSprite;
    }
}
