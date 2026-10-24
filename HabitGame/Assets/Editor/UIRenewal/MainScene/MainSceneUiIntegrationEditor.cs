using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainSceneUiIntegrationEditor
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string CanvasPrefabPath = "Assets/Resources/UIPrefabs/Canvas.prefab";
    private const string MainTabManagerPath = "Assets/Scripts/Core/MainTabManager.cs";
    private const string FontPath = "Assets/Login/Fonts/NotoSansKR Login SDF.asset";
    private const string ButtonSheetPath =
        "Assets/Skyden_Games/Free_Casual_GUI/Demo/Sprites/Buttons/Buttons.png";
    private const string SummarySpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/panel_type4.png";
    private const string ListSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/panel_type1.png";
    private const string StreakSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/Pannel_time_orange.png";
    private const string ProgressBackgroundPath =
        "Assets/Skyden_Games/Free_Casual_GUI/Demo/Sprites/Bars/Bar.png";
    private const string ProgressFillPath =
        "Assets/Skyden_Games/Free_Casual_GUI/Demo/Sprites/Bars/Bar_Fill_02.png";

    private const string MenuRoot = "Tools/HabitPVP/UI Renewal/MainScene/";
    private const string ValidateMenu = MenuRoot + "Validate Habit Main";
    private const string DryRunMenu = MenuRoot + "Dry Run Habit Main";
    private const string ApplyMenu = MenuRoot + "Apply Habit Main";

    private const string HabitRootPath = "HabitTap";
    private const string LifePath = HabitRootPath + "/Life";
    private const string TopTabPath = HabitRootPath + "/TopTab";
    private const string LifeButtonPath = TopTabPath + "/lifebutton";
    private const string SpendButtonPath = TopTabPath + "/spendbutton";
    private const string SummaryPath = LifePath + "/HabitSummaryCard";
    private const string SummaryTitlePath = SummaryPath + "/SummaryTitle";
    private const string SummaryCountPath = SummaryPath + "/SummaryCount";
    private const string SummaryStreakPath = SummaryPath + "/SummaryStreak";
    private const string ProgressPath = SummaryPath + "/HabitProgressBar";
    private const string ListPath = LifePath + "/HabitListCard";
    private const string ListTitlePath = ListPath + "/HabitListHeader/ListTitle";
    private const string AddButtonPath = ListPath + "/HabitListHeader/AddButton";
    private const string ScrollPath = ListPath + "/HabitScrollView";
    private const string ViewportPath = ScrollPath + "/Viewport";
    private const string ContentPath = ViewportPath + "/Content";
    private const string TapBarPath = "TapBar";
    private const string HabitTapButtonPath = TapBarPath + "/HabitTapButton";
    private const string CharacterTapButtonPath = TapBarPath + "/CharacterTapButton";
    private const string ShopTapButtonPath = TapBarPath + "/ShopTapButton";
    private const string PvpTapButtonPath = TapBarPath + "/PvPTapButton";
    private const string StreakBackgroundName = "UIRenewal_StreakBackground";

    private static readonly Color Cream = new Color32(0xFF, 0xF4, 0xD6, 0xFF);
    private static readonly Color Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color Blue = new Color32(0x2F, 0x73, 0xD9, 0xFF);

    [MenuItem(ValidateMenu, false, 2200)]
    private static void ValidateHabitMain()
    {
        Context context = Validate(true);
        if (context.IsValid)
        {
            Debug.Log("[HabitPVP MainScene] Habit Main 검증 성공. Scene과 Prefab은 변경하지 않았습니다.",
                context.Canvas);
        }
    }

    [MenuItem(DryRunMenu, false, 2201)]
    private static void DryRunHabitMain()
    {
        Context context = Validate(true);
        if (!context.IsValid)
        {
            Debug.LogError("[HabitPVP MainScene] Dry Run 중단: 검증 오류를 해결해 주세요.");
            return;
        }

        Debug.Log(BuildDryRunReport(context), context.Canvas);
    }

    [MenuItem(ApplyMenu, false, 2202)]
    private static void ApplyHabitMain()
    {
        Context context = Validate(true);
        if (!context.IsValid)
        {
            Debug.LogError("[HabitPVP MainScene] Apply 중단: 검증 오류를 해결해 주세요.");
            return;
        }

        ProtectedSnapshot before = ProtectedSnapshot.Capture(context);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply MainScene Habit Main UI Renewal");

        try
        {
            ApplyVisuals(context);
            RecordOverrides(context);
            before.AssertUnchanged(context);

            Context after = Validate(false);
            if (!after.IsValid)
            {
                throw new InvalidOperationException(
                    "적용 후 참조 무결성 검증 실패: " + string.Join(" | ", after.Errors));
            }

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log(
                "[HabitPVP MainScene] Habit Main 적용 완료. Scene은 저장하지 않았습니다. " +
                "Hierarchy와 Inspector를 확인한 뒤 사용자가 직접 저장해 주세요. Edit > Undo를 지원합니다.",
                context.Canvas);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogException(exception);
            Debug.LogError(
                "[HabitPVP MainScene] Apply 실패. 이번 Undo Group의 변경을 모두 되돌렸습니다. Scene은 저장하지 않았습니다.");
        }
    }

    [MenuItem(ApplyMenu, true)]
    private static bool CanApplyHabitMain()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return false;
        }

        Scene scene = SceneManager.GetActiveScene();
        return scene.IsValid() && scene.isLoaded &&
               string.Equals(scene.path, ScenePath, StringComparison.Ordinal);
    }

    private static Context Validate(bool log)
    {
        Context context = new Context();
        Scene scene = SceneManager.GetActiveScene();
        context.Scene = scene;

        if (!scene.IsValid() || !scene.isLoaded)
        {
            context.Errors.Add("활성 Scene이 유효하고 로드된 상태여야 합니다.");
            return FinishValidation(context, log);
        }
        if (!string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
        {
            context.Errors.Add($"활성 Scene 경로가 정확하지 않습니다. 현재: '{scene.path}', 필요: '{ScenePath}'");
            return FinishValidation(context, log);
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            context.Errors.Add("Play Mode 또는 Play Mode 전환 중에는 실행할 수 없습니다.");
        }

        GameObject[] canvases = scene.GetRootGameObjects()
            .Where(root => root.name == "Canvas").ToArray();
        if (canvases.Length != 1)
        {
            context.Errors.Add($"Scene 루트 Canvas가 정확히 하나여야 합니다. 발견 수: {canvases.Length}");
            return FinishValidation(context, log);
        }

        context.Canvas = canvases[0];
        if (!PrefabUtility.IsPartOfPrefabInstance(context.Canvas) ||
            PrefabUtility.GetOutermostPrefabInstanceRoot(context.Canvas) != context.Canvas)
        {
            context.Errors.Add("Canvas가 Prefab instance 최상위 루트가 아닙니다.");
        }
        string source = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(context.Canvas);
        if (!string.Equals(source, CanvasPrefabPath, StringComparison.Ordinal))
        {
            context.Errors.Add($"Canvas Prefab source가 다릅니다. 현재: '{source}'");
        }

        context.LifeRoot = FindRect(context, LifePath);
        context.HabitBackground = context.LifeRoot == null
            ? null
            : context.LifeRoot.GetComponent<Image>();
        context.Summary = Find<Image>(context, SummaryPath);
        context.List = Find<Image>(context, ListPath);
        context.SummaryTitle = Find<TMP_Text>(context, SummaryTitlePath);
        context.SummaryCount = Find<TMP_Text>(context, SummaryCountPath);
        context.SummaryStreak = Find<TMP_Text>(context, SummaryStreakPath);
        context.ListTitle = Find<TMP_Text>(context, ListTitlePath);
        context.LifeButton = FindButton(context, LifeButtonPath);
        context.SpendButton = FindButton(context, SpendButtonPath);
        context.AddButton = FindButton(context, AddButtonPath);
        context.Progress = Find<Slider>(context, ProgressPath);
        context.ProgressBackground = Find<Image>(context, ProgressPath + "/Background");
        context.ProgressFill = Find<Image>(context, ProgressPath + "/Fill Area/Fill");
        context.Scroll = Find<ScrollRect>(context, ScrollPath);
        context.Viewport = FindRect(context, ViewportPath);
        context.Content = FindRect(context, ContentPath);
        context.TapBar = FindRect(context, TapBarPath);
        context.HabitTapButton = Find<Button>(context, HabitTapButtonPath);
        context.CharacterTapButton = Find<Button>(context, CharacterTapButtonPath);
        context.ShopTapButton = Find<Button>(context, ShopTapButtonPath);
        context.PvpTapButton = Find<Button>(context, PvpTapButtonPath);

        if (context.Progress != null && context.ProgressFill != null &&
            context.Progress.fillRect != context.ProgressFill.rectTransform)
        {
            context.Errors.Add("HabitProgressBar.fillRect가 예상 Fill과 다릅니다.");
        }
        if (context.Scroll != null &&
            (context.Scroll.viewport != context.Viewport || context.Scroll.content != context.Content))
        {
            context.Errors.Add("HabitScrollView의 viewport/content 참조가 예상 경로와 다릅니다.");
        }
        if (context.TapBar != null && !Mathf.Approximately(context.TapBar.rect.height, 180f))
        {
            context.Errors.Add($"TapBar 높이가 180px이 아닙니다. 현재 계산 높이: {context.TapBar.rect.height}");
        }

        context.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        context.SummarySprite = LoadSprite(context, SummarySpritePath, "panel_type4");
        context.ListSprite = LoadSprite(context, ListSpritePath, "panel_type1");
        context.StreakSprite = LoadSprite(context, StreakSpritePath, "Pannel_time_orange");
        context.ProgressBackgroundSprite = LoadSprite(context, ProgressBackgroundPath, "Bar");
        context.ProgressFillSprite = LoadSprite(context, ProgressFillPath, "Bar_Fill_02");
        Dictionary<string, Sprite> buttons = LoadButtonSprites(context);
        buttons.TryGetValue("Buttons_015", out context.LifeButtonSprite);
        buttons.TryGetValue("Buttons_019", out context.SpendButtonSprite);
        buttons.TryGetValue("Buttons_02", out context.AddButtonSprite);
        if (context.Font == null)
        {
            context.Errors.Add($"NotoSansKR TMP Font Asset이 없습니다: {FontPath}");
        }
        if (context.LifeButtonSprite == null || context.SpendButtonSprite == null ||
            context.AddButtonSprite == null)
        {
            context.Errors.Add("필요한 Free Casual GUI 버튼 Sprite(Buttons_015, Buttons_019, Buttons_02)가 없습니다.");
        }

        context.HabitUIManager = FindUniqueManager(context, "HabitUIManager");
        context.HabitListManager = FindUniqueManager(context, "HabitListManager");
        context.HabitSummaryManager = FindUniqueManager(context, "HabitSummaryManager");
        context.MainTabManager = FindUniqueManager(context, "MainTabManager");
        ValidateManagerReferences(context);
        ValidateMainTabManagerButtonReferences(context);
        ValidateMainTabManagerListenerCode(context);
        ValidateButtons(context);
        ValidatePrefabScope(context);

        Transform existingStreak = context.Canvas.transform.Find(SummaryPath + "/" + StreakBackgroundName);
        if (existingStreak != null)
        {
            context.StreakBackground = existingStreak.GetComponent<Image>();
            if (context.StreakBackground == null)
            {
                context.Errors.Add(StreakBackgroundName + "에 Image가 없습니다.");
            }
        }

        return FinishValidation(context, log);
    }

    private static void ValidateManagerReferences(Context context)
    {
        ValidateReferences(context, context.HabitUIManager,
            "topTab", "lifePanel", "spendPanel", "lifeAddPanel", "spendAddPanel",
            "budgetSettingPanel", "spendHistoryPanel", "weeklyReportPanel", "habitListManager",
            "habitGoalUI");
        ValidateReferences(context, context.HabitListManager,
            "habitNameInput", "content", "dailySection", "weeklySection", "longTermSection", "habitItemPrefab",
            "dailyTabButton", "weeklyTabButton", "longTermTabButton", "summaryManager",
            "summaryTitleText", "summaryStreakObject", "summaryStreakText", "habitListTitleText",
            "importWeeklyButton");
        ValidateReferences(context, context.HabitSummaryManager,
            "summaryCountText", "progressBar", "content");
        ValidateReferences(context, context.MainTabManager,
            "habitTap", "characterTap", "inventoryTap", "shopTap", "battleTap",
            "habitTapButton", "characterTapButton", "inventoryTapButton", "shopTapButton",
            "battleTapButton", "rankingboardManager");

        ValidateExpectedReference(context, context.HabitSummaryManager, "summaryCountText", context.SummaryCount);
        ValidateExpectedReference(context, context.HabitSummaryManager, "progressBar", context.Progress);
        ValidateExpectedReference(context, context.HabitSummaryManager, "content", context.Content);
    }

    private static void ValidateReferences(Context context, MonoBehaviour manager, params string[] names)
    {
        if (manager == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(manager);
        foreach (string name in names)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference ||
                property.objectReferenceValue == null)
            {
                context.Errors.Add($"{manager.GetType().Name}.{name} 참조가 없거나 비어 있습니다.");
            }
        }
    }

    private static void ValidateExpectedReference(
        Context context, MonoBehaviour manager, string name, UnityEngine.Object expected)
    {
        if (manager == null || expected == null)
        {
            return;
        }

        SerializedProperty property = new SerializedObject(manager).FindProperty(name);
        if (property == null || property.objectReferenceValue != expected)
        {
            context.Errors.Add($"{manager.GetType().Name}.{name}가 예상 Habit Main 대상을 참조하지 않습니다.");
        }
    }

    private static void ValidateMainTabManagerButtonReferences(Context context)
    {
        ValidateExpectedReference(
            context, context.MainTabManager, "habitTapButton", context.HabitTapButton);
        ValidateExpectedReference(
            context, context.MainTabManager, "characterTapButton", context.CharacterTapButton);
        ValidateExpectedReference(
            context, context.MainTabManager, "shopTapButton", context.ShopTapButton);
        ValidateExpectedReference(
            context, context.MainTabManager, "battleTapButton", context.PvpTapButton);
    }

    private static void ValidateMainTabManagerListenerCode(Context context)
    {
        if (!File.Exists(MainTabManagerPath))
        {
            context.Errors.Add("MainTabManager 소스 파일이 없습니다: " + MainTabManagerPath);
            return;
        }

        string source = File.ReadAllText(MainTabManagerPath);
        string[] requiredListeners =
        {
            "habitTapButton.onClick.AddListener(OnClickHabitTap)",
            "characterTapButton.onClick.AddListener(OnClickCharacterTap)",
            "shopTapButton.onClick.AddListener(OnClickShopTap)",
            "battleTapButton.onClick.AddListener(OnClickBattleTap)"
        };
        foreach (string listener in requiredListeners)
        {
            if (!source.Contains(listener))
            {
                context.Errors.Add("MainTabManager의 필수 코드 기반 이벤트 연결이 없습니다: " + listener);
            }
        }

        context.HasCodeBasedTapBarListeners = requiredListeners.All(listener => source.Contains(listener));
    }

    private static void ValidateButtons(Context context)
    {
        Button[] required = { context.LifeButton, context.SpendButton, context.AddButton };
        foreach (Button button in required.Where(item => item != null))
        {
            if (button.targetGraphic != button.GetComponent<Image>())
            {
                context.Errors.Add($"Button.targetGraphic이 동일 오브젝트의 Image가 아닙니다: {GetPath(button.transform)}");
            }
            if (button.onClick.GetPersistentEventCount() == 0)
            {
                context.Errors.Add($"대상 Button persistent UnityEvent가 비어 있습니다: {GetPath(button.transform)}");
            }
        }

        if (context.TapBar != null)
        {
            Button[] navigation = context.TapBar.GetComponentsInChildren<Button>(true);
            Button[] expectedNavigation =
            {
                context.HabitTapButton, context.CharacterTapButton,
                context.ShopTapButton, context.PvpTapButton
            };
            if (navigation.Length != expectedNavigation.Length)
            {
                context.Errors.Add(
                    $"TapBar 하단 네비게이션 Button이 정확히 4개여야 합니다. 발견 수: {navigation.Length}");
            }
            foreach (Button button in expectedNavigation.Where(item => item != null))
            {
                if (button.targetGraphic == null)
                {
                    context.Errors.Add("TapBar Button.targetGraphic 참조가 비어 있습니다: " +
                                       GetPath(button.transform));
                }
                if (button.transform.parent != context.TapBar)
                {
                    context.Errors.Add("TapBar Button이 TapBar의 직속 자식이 아닙니다: " +
                                       GetPath(button.transform));
                }
            }
        }
    }

    private static void ValidatePrefabScope(Context context)
    {
        if (context.Canvas == null)
        {
            return;
        }

        Transform[] targets =
        {
            context.LifeRoot,
            context.Summary == null ? null : context.Summary.transform,
            context.List == null ? null : context.List.transform,
            context.Viewport,
            context.TapBar
        };
        foreach (Transform target in targets.Where(item => item != null))
        {
            if (PrefabUtility.GetOutermostPrefabInstanceRoot(target.gameObject) != context.Canvas)
            {
                context.Errors.Add($"Canvas Prefab instance 밖의 대상입니다: {GetPath(target)}");
            }
        }
    }

    private static Context FinishValidation(Context context, bool log)
    {
        if (log)
        {
            Debug.Log(
                $"[HabitPVP MainScene] Scene: {context.Scene.path}\n" +
                $"Canvas source: {CanvasPrefabPath}\n" +
                $"Habit root: Canvas/{HabitRootPath}\n" +
                $"Life: Canvas/{LifePath}\nTopTab: Canvas/{TopTabPath}\n" +
                $"Summary: Canvas/{SummaryPath}\nList: Canvas/{ListPath}\n" +
                $"Scroll viewport: Canvas/{ViewportPath}\nTapBar: Canvas/{TapBarPath}",
                context.Canvas);
            foreach (string error in context.Errors)
            {
                Debug.LogError("[HabitPVP MainScene] " + error);
            }
            if (context.HasCodeBasedTapBarListeners)
            {
                Debug.Log(
                    "[HabitPVP MainScene] TapBar 4개 버튼은 persistent UnityEvent 대신 " +
                    "MainTabManager.Awake()의 AddListener로 연결됩니다. 버튼 존재와 SerializeField 매핑을 확인했습니다.",
                    context.MainTabManager);
            }
        }
        return context;
    }

    private static string BuildDryRunReport(Context context)
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("[HabitPVP MainScene] Dry Run — Scene/파일 변경 없음");
        report.AppendLine("- Canvas/HabitTap/Life 배경을 크림색으로 변경");
        report.AppendLine("- HabitSummaryCard, HabitListCard에 Free Casual GUI 골드 패널 적용");
        report.AppendLine("- Summary 제목/완료 수/Streak 및 목록 제목에 NotoSansKR과 브라운/블루 스타일 적용");
        report.AppendLine("- HabitProgressBar의 Background/Fill Sprite 적용");
        report.AppendLine("- lifebutton, spendbutton, AddButton에 기존 Renewal 버튼 스타일 적용");
        report.AppendLine("- HabitScrollView/Viewport 하단 inset을 180px로 설정");
        report.AppendLine("- TapBar 오브젝트/이벤트와 Manager/API 참조는 변경하지 않음");
        report.AppendLine("- HabitItem prefab, Add Form, NUMBER, PHOTO, 소비 및 다른 탭은 변경하지 않음");
        report.Append("- Scene 자동 저장 없음; 적용 시 Canvas instance의 대상 속성/추가 오브젝트만 Override 생성");
        return report.ToString();
    }

    private static void ApplyVisuals(Context context)
    {
        List<UnityEngine.Object> targets = new List<UnityEngine.Object>
        {
            context.HabitBackground, context.Summary, context.List,
            context.SummaryTitle, context.SummaryCount, context.SummaryStreak, context.ListTitle,
            context.LifeButton, context.LifeButton.GetComponent<Image>(),
            context.SpendButton, context.SpendButton.GetComponent<Image>(),
            context.AddButton, context.AddButton.GetComponent<Image>(),
            context.ProgressBackground, context.ProgressFill,
            context.Viewport
        };
        targets.AddRange(new[] { context.LifeButton, context.SpendButton, context.AddButton }
            .Select(button => button.transform.Find("Text (TMP)"))
            .Where(transform => transform != null)
            .SelectMany(transform => new UnityEngine.Object[]
            {
                transform, transform.GetComponent<TMP_Text>()
            }));
        Undo.RecordObjects(targets.Where(item => item != null).Distinct().ToArray(),
            "Style MainScene Habit Main");

        if (context.HabitBackground == null)
        {
            context.HabitBackground = Undo.AddComponent<Image>(context.LifeRoot.gameObject);
        }
        context.HabitBackground.color = Cream;
        context.HabitBackground.raycastTarget = false;
        ApplyPanel(context.Summary, context.SummarySprite);
        ApplyPanel(context.List, context.ListSprite);
        ApplyText(context.SummaryTitle, Brown, 44f, TextAlignmentOptions.MidlineLeft);
        ApplyText(context.SummaryCount, Blue, 40f, TextAlignmentOptions.MidlineRight);
        ApplyText(context.SummaryStreak, Color.white, 26f, TextAlignmentOptions.Center);
        ApplyText(context.ListTitle, Brown, 44f, TextAlignmentOptions.MidlineLeft);

        ApplyButton(context.LifeButton, context.LifeButtonSprite, context.Font, 50f);
        ApplyButton(context.SpendButton, context.SpendButtonSprite, context.Font, 50f);
        ApplyButton(context.AddButton, context.AddButtonSprite, context.Font, 44f);

        context.ProgressBackground.sprite = context.ProgressBackgroundSprite;
        context.ProgressBackground.type = Image.Type.Simple;
        context.ProgressBackground.color = Color.white;
        context.ProgressBackground.preserveAspect = false;
        context.ProgressFill.sprite = context.ProgressFillSprite;
        context.ProgressFill.type = Image.Type.Sliced;
        context.ProgressFill.color = Blue;
        context.ProgressFill.preserveAspect = false;

        Vector2 viewportOffset = context.Viewport.offsetMin;
        viewportOffset.y = 180f;
        context.Viewport.offsetMin = viewportOffset;

        if (context.StreakBackground == null)
        {
            GameObject background = new GameObject(
                StreakBackgroundName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            background.layer = context.Summary.gameObject.layer;
            Undo.RegisterCreatedObjectUndo(background, "Create Habit Streak background");
            Undo.SetTransformParent(background.transform, context.Summary.transform, "Parent Habit Streak background");
            context.StreakBackground = background.GetComponent<Image>();
        }
        else
        {
            Undo.RecordObject(context.StreakBackground, "Style Habit Streak background");
            Undo.RecordObject(context.StreakBackground.rectTransform, "Layout Habit Streak background");
        }

        context.StreakBackground.sprite = context.StreakSprite;
        context.StreakBackground.type = Image.Type.Simple;
        context.StreakBackground.color = Color.white;
        context.StreakBackground.raycastTarget = false;
        RectTransform streakRect = context.StreakBackground.rectTransform;
        streakRect.anchorMin = new Vector2(0.5f, 0f);
        streakRect.anchorMax = new Vector2(0.5f, 0f);
        streakRect.pivot = new Vector2(0.5f, 0f);
        streakRect.anchoredPosition = new Vector2(0f, 5f);
        streakRect.sizeDelta = new Vector2(240f, 240f * 184f / 402f);
        context.StreakBackground.transform.SetSiblingIndex(
            Mathf.Max(0, context.SummaryStreak.transform.GetSiblingIndex()));
    }

    private static void ApplyPanel(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.fillCenter = true;
        image.preserveAspect = false;
    }

    private static void ApplyText(TMP_Text text, Color color, float size, TextAlignmentOptions alignment)
    {
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        text.color = color;
        text.fontSize = size;
        text.fontStyle |= FontStyles.Bold;
        text.alignment = alignment;
    }

    private static void ApplyButton(Button button, Sprite sprite, TMP_FontAsset font, float fontSize)
    {
        Image image = button.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = Color.white;
        image.preserveAspect = false;
        TMP_Text text = button.transform.Find("Text (TMP)")?.GetComponent<TMP_Text>();
        if (text == null)
        {
            throw new InvalidOperationException("버튼 직속 Text (TMP)가 없습니다: " + GetPath(button.transform));
        }
        text.font = font;
        text.color = Color.white;
        text.fontSize = fontSize;
        text.fontStyle |= FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
    }

    private static void RecordOverrides(Context context)
    {
        UnityEngine.Object[] targets =
        {
            context.HabitBackground, context.Summary, context.List,
            context.SummaryTitle, context.SummaryCount, context.SummaryStreak, context.ListTitle,
            context.LifeButton.GetComponent<Image>(), context.SpendButton.GetComponent<Image>(),
            context.AddButton.GetComponent<Image>(), context.ProgressBackground, context.ProgressFill,
            context.Viewport, context.StreakBackground, context.StreakBackground.rectTransform
        };
        foreach (UnityEngine.Object target in targets.Where(item => item != null))
        {
            EditorUtility.SetDirty(target);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
        foreach (Button button in new[] { context.LifeButton, context.SpendButton, context.AddButton })
        {
            TMP_Text text = button.transform.Find("Text (TMP)").GetComponent<TMP_Text>();
            EditorUtility.SetDirty(text);
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);
        }
    }

    private static T Find<T>(Context context, string path) where T : Component
    {
        Transform transform = context.Canvas.transform.Find(path);
        if (transform == null)
        {
            context.Errors.Add("필수 경로가 없습니다: Canvas/" + path);
            return null;
        }
        T[] components = transform.GetComponents<T>();
        if (components.Length != 1)
        {
            context.Errors.Add($"{typeof(T).Name}가 정확히 하나여야 합니다: Canvas/{path}");
            return null;
        }
        if (transform.GetComponents<Component>().Any(component => component == null))
        {
            context.Errors.Add("Missing Script가 있습니다: Canvas/" + path);
        }
        return components[0];
    }

    private static RectTransform FindRect(Context context, string path)
    {
        return Find<RectTransform>(context, path);
    }

    private static Button FindButton(Context context, string path)
    {
        Button button = Find<Button>(context, path);
        if (button == null)
        {
            return null;
        }
        if (button.GetComponent<Image>() == null || button.transform.Find("Text (TMP)")?.GetComponent<TMP_Text>() == null)
        {
            context.Errors.Add("Button Image 또는 직속 Text (TMP)가 없습니다: Canvas/" + path);
        }
        return button;
    }

    private static MonoBehaviour FindUniqueManager(Context context, string typeName)
    {
        MonoBehaviour[] matches = context.Scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
            .Where(component => component != null && component.GetType().Name == typeName).ToArray();
        if (matches.Length != 1)
        {
            context.Errors.Add($"{typeName}가 정확히 하나여야 합니다. 발견 수: {matches.Length}");
            return null;
        }
        return matches[0];
    }

    private static Sprite LoadSprite(Context context, string path, string label)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            context.Errors.Add($"{label} Sprite를 불러올 수 없습니다: {path}");
        }
        return sprite;
    }

    private static Dictionary<string, Sprite> LoadButtonSprites(Context context)
    {
        TextureImporter importer = AssetImporter.GetAtPath(ButtonSheetPath) as TextureImporter;
        if (importer == null || importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Multiple)
        {
            context.Errors.Add("Free Casual GUI 버튼 시트가 Multiple Sprite가 아닙니다: " + ButtonSheetPath);
            return new Dictionary<string, Sprite>();
        }
        return AssetDatabase.LoadAllAssetsAtPath(ButtonSheetPath).OfType<Sprite>()
            .GroupBy(sprite => sprite.name).ToDictionary(group => group.Key, group => group.First());
    }

    private static string GetPath(Transform transform)
    {
        List<string> names = new List<string>();
        while (transform != null)
        {
            names.Add(transform.name);
            transform = transform.parent;
        }
        names.Reverse();
        return string.Join("/", names);
    }

    private sealed class ProtectedSnapshot
    {
        private readonly Dictionary<int, string> managerJson;
        private readonly Dictionary<int, string> buttonEvents;

        private ProtectedSnapshot(Dictionary<int, string> managers, Dictionary<int, string> buttons)
        {
            managerJson = managers;
            buttonEvents = buttons;
        }

        public static ProtectedSnapshot Capture(Context context)
        {
            MonoBehaviour[] managers =
            {
                context.HabitUIManager, context.HabitListManager,
                context.HabitSummaryManager, context.MainTabManager
            };
            Dictionary<int, string> managerState = managers.ToDictionary(
                manager => manager.GetInstanceID(), manager => EditorJsonUtility.ToJson(manager));
            Dictionary<int, string> events = context.Canvas.GetComponentsInChildren<Button>(true)
                .ToDictionary(button => button.GetInstanceID(), DescribeEvents);
            return new ProtectedSnapshot(managerState, events);
        }

        public void AssertUnchanged(Context context)
        {
            MonoBehaviour[] managers =
            {
                context.HabitUIManager, context.HabitListManager,
                context.HabitSummaryManager, context.MainTabManager
            };
            foreach (MonoBehaviour manager in managers)
            {
                if (!managerJson.TryGetValue(manager.GetInstanceID(), out string before) ||
                    !string.Equals(before, EditorJsonUtility.ToJson(manager), StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(manager.GetType().Name + " 직렬화 참조가 변경되었습니다.");
                }
            }

            Button[] buttons = context.Canvas.GetComponentsInChildren<Button>(true);
            if (buttons.Length != buttonEvents.Count)
            {
                throw new InvalidOperationException("Button 수가 변경되었습니다.");
            }
            foreach (Button button in buttons)
            {
                if (!buttonEvents.TryGetValue(button.GetInstanceID(), out string before) ||
                    !string.Equals(before, DescribeEvents(button), StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Button UnityEvent가 변경되었습니다: " + GetPath(button.transform));
                }
            }
        }

        private static string DescribeEvents(Button button)
        {
            StringBuilder value = new StringBuilder();
            int count = button.onClick.GetPersistentEventCount();
            value.Append(count);
            for (int index = 0; index < count; index++)
            {
                UnityEngine.Object target = button.onClick.GetPersistentTarget(index);
                value.Append('|').Append(target == null ? 0 : target.GetInstanceID())
                    .Append(':').Append(button.onClick.GetPersistentMethodName(index))
                    .Append(':').Append((int)button.onClick.GetPersistentListenerState(index));
            }
            return value.ToString();
        }
    }

    private sealed class Context
    {
        public Scene Scene;
        public GameObject Canvas;
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
        public RectTransform LifeRoot;
        public Image HabitBackground;
        public Image Summary;
        public Image List;
        public TMP_Text SummaryTitle;
        public TMP_Text SummaryCount;
        public TMP_Text SummaryStreak;
        public TMP_Text ListTitle;
        public Button LifeButton;
        public Button SpendButton;
        public Button AddButton;
        public Slider Progress;
        public Image ProgressBackground;
        public Image ProgressFill;
        public ScrollRect Scroll;
        public RectTransform Viewport;
        public RectTransform Content;
        public RectTransform TapBar;
        public Button HabitTapButton;
        public Button CharacterTapButton;
        public Button ShopTapButton;
        public Button PvpTapButton;
        public bool HasCodeBasedTapBarListeners;
        public Image StreakBackground;
        public TMP_FontAsset Font;
        public Sprite SummarySprite;
        public Sprite ListSprite;
        public Sprite StreakSprite;
        public Sprite ProgressBackgroundSprite;
        public Sprite ProgressFillSprite;
        public Sprite LifeButtonSprite;
        public Sprite SpendButtonSprite;
        public Sprite AddButtonSprite;
        public MonoBehaviour HabitUIManager;
        public MonoBehaviour HabitListManager;
        public MonoBehaviour HabitSummaryManager;
        public MonoBehaviour MainTabManager;
    }
}
