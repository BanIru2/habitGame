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
public static class SpendingReportUiRenewalEditor
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
    private const string PreviewRootName = "SpendingReportPreviewRoot";
    private const string ValidateMenu =
        "Tools/HabitPVP/UI Renewal/Validate Spending & Report Main Renewal";
    private const string ApplyMenu =
        "Tools/HabitPVP/UI Renewal/Apply Spending & Report Main Renewal";
    private const string PreviewMenu =
        "Tools/HabitPVP/UI Renewal/Preview Mode/J. Spending & Report Main Renewal";

    private static readonly string[] OtherPreviewRoots =
    {
        "HabitItemPreviewTestRoot", "NumberOverlayPreviewRoot", "PhotoVerificationPreviewRoot",
        "CharacterMainPreviewRoot", "InventoryMainPreviewRoot", "ShopGachaPreviewRoot",
        "PvpRankingPreviewRoot"
    };

    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 CreamLight = new Color32(0xFF, 0xF9, 0xEA, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x86, 0x68, 0x5D, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);
    private static readonly Color32 Green = new Color32(0x6F, 0xA8, 0x63, 0xFF);

    static SpendingReportUiRenewalEditor()
    {
        EditorApplication.update -= RestoreWhenAnotherPreviewIsSelected;
        EditorApplication.update += RestoreWhenAnotherPreviewIsSelected;
        EditorSceneManager.sceneSaving -= RestoreBeforeSceneSave;
        EditorSceneManager.sceneSaving += RestoreBeforeSceneSave;
    }

    [MenuItem(ValidateMenu, false, 2110)]
    private static void Validate()
    {
        Context context = ValidateScene(true);
        if (!context.IsValid) return;
        List<string> errors = ValidateAppliedLayout(context, true);
        if (errors.Count == 0)
        {
            Debug.Log("[HabitPVP Spending] 검증 성공. Spend 메인/입력/내역과 J Preview 구성이 정상입니다.",
                context.Spend);
            return;
        }
        foreach (string error in errors) Debug.LogError("[HabitPVP Spending] " + error, context.Spend);
    }

    [MenuItem(ApplyMenu, false, 2111)]
    private static void Apply()
    {
        Context context = ValidateScene(true);
        if (!context.IsValid) return;
        RestoreSpendingPreviewState();
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Spending & Report Main UI Renewal");
        try
        {
            ApplyMainStyle(context);
            ApplyBudgetSettingStyle(context);
            ApplySpendGoalStyle(context);
            ApplyRecordSpendStyle(context);
            ApplySpendHistoryStyle(context);
            ApplyWeeklyReportStyle(context);
            EnsurePreviewRoot(context);
            List<string> errors = ValidateAppliedLayout(context, true);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            EditorSceneManager.MarkSceneDirty(context.Scene);
            Undo.CollapseUndoOperations(group);
            if (!EditorSceneManager.SaveScene(context.Scene))
                throw new InvalidOperationException("UITestScene 저장에 실패했습니다.");
            Debug.Log("[HabitPVP Spending] 적용 성공. UITestScene Spend/SpendHistory Override와 J Preview Root만 저장했습니다. " +
                      "하위 화면, Canvas.prefab, MainScene/NG, Manager/API 코드는 변경하지 않았습니다.", context.Spend);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(group);
            Debug.LogError("[HabitPVP Spending] 적용 실패. 이번 변경을 Undo로 되돌렸습니다.\n" + exception);
        }
    }

    [MenuItem(PreviewMenu, false, 2112)]
    private static void ActivatePreview()
    {
        Context context = ValidateScene(false);
        if (!context.IsValid) return;
        RestoreOtherPreviewStates();
        RestoreSpendingPreviewState();
        GameObject root = FindRoot(context.Scene, PreviewRootName);
        SpendingReportPreviewController controller =
            root != null ? root.GetComponent<SpendingReportPreviewController>() : null;
        if (root == null || controller == null)
        {
            Debug.LogError("[HabitPVP Spending] J Preview가 없습니다. Apply Spending & Report Main Renewal을 먼저 실행해주세요.");
            return;
        }
        foreach (string rootName in OtherPreviewRoots)
            SetActive(FindRoot(context.Scene, rootName), false, "Hide other preview for J mode");
        IsolateScene(context.Scene, root, controller);
        SetActive(root, true, "Activate Spending preview");
        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(context.Scene);
        Debug.Log("[HabitPVP Spending] J 모드: 가상 예산/사용액/목표/보상/소비 내역과 로컬 버튼 반응을 표시합니다. " +
                  "SpendingService와 게임 Manager/API는 호출하지 않습니다.", root);
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
            context.Errors.Add("Play Mode를 종료한 뒤 실행해주세요.");
        if (context.Scene.path != ScenePath)
            context.Errors.Add($"UITestScene에서만 실행할 수 있습니다. 현재: '{context.Scene.path}'");

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
                context.Errors.Add($"Canvas 원본 Prefab 경로가 다릅니다: '{prefabPath}'");
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
            context.Spend = FindUnique(context.Canvas.transform, "Spend", context.Errors)?.gameObject;
            Transform spend = context.Spend != null ? context.Spend.transform : null;
            context.Scroll = GetUnique<ScrollRect>(spend, "SpendScrollView", context.Errors);
            context.Content = context.Scroll != null ? context.Scroll.content : null;
            context.RecordCard = FindUnique(spend, "RecordCard", context.Errors) as RectTransform;
            context.GoldListCard = FindUnique(spend, "GoldListCard", context.Errors) as RectTransform;
            context.RewardCard = FindUnique(spend, "RewardCard", context.Errors) as RectTransform;
            context.BudgetCard = FindUnique(spend, "BudgetCard", context.Errors) as RectTransform;
            context.GoalScroll = GetUnique<ScrollRect>(context.GoldListCard, "GoalScrollView", context.Errors);
            context.RecordButton = GetUnique<Button>(context.RecordCard, "RecordButton", context.Errors);
            context.HistoryButton = GetUnique<Button>(context.RecordCard, "ViewHistoryButton", context.Errors);
            context.ClaimButton = GetUnique<Button>(context.RewardCard, "ClaimRewardButton", context.Errors);
            context.BudgetSettingButton = GetUnique<Button>(context.BudgetCard, "BudgetSettingButton", context.Errors);
            context.BudgetText = GetUnique<TMP_Text>(context.BudgetCard, "BudgetText", context.Errors);
            context.UsedText = GetUnique<TMP_Text>(context.BudgetCard, "UsedText", context.Errors);
            context.BudgetSlider = GetUnique<Slider>(context.BudgetCard, "Slider", context.Errors);

            context.BudgetSetting = FindUnique(context.Canvas.transform, "BudgetSetting", context.Errors)?.gameObject;
            Transform budgetSetting = context.BudgetSetting != null ? context.BudgetSetting.transform : null;
            context.BudgetSettingManager = budgetSetting != null
                ? budgetSetting.GetComponent<BudgetSettingManager>()
                : null;
            if (budgetSetting != null && context.BudgetSettingManager == null)
                context.Errors.Add("'BudgetSetting'에 BudgetSettingManager 컴포넌트가 없습니다.");
            if (context.BudgetSettingManager != null)
            {
                SerializedObject budgetManager = new SerializedObject(context.BudgetSettingManager);
                context.BudgetInput = budgetManager.FindProperty("budgetInput")?.objectReferenceValue as TMP_InputField;
                context.BudgetSaveButton = budgetManager.FindProperty("saveButton")?.objectReferenceValue as Button;
                context.BudgetBackButton = budgetManager.FindProperty("backButton")?.objectReferenceValue as Button;
                if (context.BudgetInput == null) context.Errors.Add("BudgetSetting budgetInput 참조가 없습니다.");
                if (context.BudgetSaveButton == null) context.Errors.Add("BudgetSetting saveButton 참조가 없습니다.");
                if (context.BudgetBackButton == null) context.Errors.Add("BudgetSetting backButton 참조가 없습니다.");
            }
            context.BudgetSettingContent = FindUnique(budgetSetting, "Content", context.Errors) as RectTransform;
            context.BudgetSettingCard = FindUnique(budgetSetting, "BudgetCard", context.Errors) as RectTransform;
            context.BudgetSettingTitle = GetUnique<TMP_Text>(budgetSetting, "title", context.Errors);
            context.BudgetDescription = GetUnique<TMP_Text>(budgetSetting, "Description", context.Errors);
            context.BudgetDescriptionSecondary = GetUnique<TMP_Text>(budgetSetting, "Description (1)", context.Errors);

            context.RecordSpendAdd = FindUnique(context.Canvas.transform, "RecordSpendAdd", context.Errors)?.gameObject;
            if (context.RecordSpendAdd != null)
            {
                context.RecordSpendManager = context.RecordSpendAdd.GetComponent<RecordSpendManager>();
                if (context.RecordSpendManager == null)
                    context.Errors.Add("'RecordSpendAdd'에 RecordSpendManager 컴포넌트가 없습니다.");
                else
                {
                    SerializedObject recordManager = new SerializedObject(context.RecordSpendManager);
                    context.RecordAmountInput = recordManager.FindProperty("amountInput")?.objectReferenceValue as TMP_InputField;
                    context.RecordCategoryDropdown = recordManager.FindProperty("categoryDropdown")?.objectReferenceValue as TMP_Dropdown;
                }
                context.RecordFormScroll = context.RecordSpendAdd.GetComponentsInChildren<ScrollRect>(true).FirstOrDefault();
                context.RecordSaveButton = FindButtonByPersistentMethod(context.RecordSpendAdd.transform, "Save");
                context.RecordBackButton = FindButtonByPersistentMethod(context.RecordSpendAdd.transform, "BackToSpend");
                if (context.RecordAmountInput == null || context.RecordCategoryDropdown == null)
                    context.Errors.Add("RecordSpendAdd 금액 Input/카테고리 Dropdown SerializeField 참조가 없습니다.");
                if (context.RecordFormScroll == null)
                    context.Errors.Add("RecordSpendAdd Form ScrollRect를 찾을 수 없습니다.");
                if (context.RecordSaveButton == null || context.RecordBackButton == null)
                    context.Errors.Add("RecordSpendAdd Save/BackToSpend UnityEvent 버튼을 찾을 수 없습니다.");
            }

            context.SpendAdd = FindUnique(context.Canvas.transform, "SpendAdd_1", context.Errors)?.gameObject;
            if (context.SpendAdd != null)
            {
                context.SpendListManager = context.Canvas.transform
                    .Find("HabitTap/Managers")
                    ?.GetComponent<SpendListManager>();

                context.SpendCycleManager = context.SpendAdd.GetComponentInChildren<SpendCycleManager>(true);
                if (context.SpendListManager == null) context.Errors.Add("SpendAdd_1의 SpendListManager를 찾을 수 없습니다.");
                else
                {
                    SerializedObject spendManager = new SerializedObject(context.SpendListManager);
                    context.GoalNameInput = spendManager.FindProperty("goalNameInput")?.objectReferenceValue as TMP_InputField;
                    context.GoalLimitInput = spendManager.FindProperty("goldInput")?.objectReferenceValue as TMP_InputField;
                    context.GoalPreviewContent = spendManager.FindProperty("previewContent")?.objectReferenceValue as Transform;
                    context.SkipButton = spendManager.FindProperty("skipButton")?.objectReferenceValue as Button;
                    context.CompleteButton = spendManager.FindProperty("completeButton")?.objectReferenceValue as Button;
                }
                if (context.SpendCycleManager != null)
                {
                    context.DailyButton = context.SpendCycleManager.dailyButton;
                    context.WeeklyButton = context.SpendCycleManager.weeklyButton;
                }
                context.AddGoalButton = FindButtonByLabel(context.SpendAdd.transform, "Add Goal");
                if (context.AddGoalButton == null)
                    context.AddGoalButton = context.SpendAdd.GetComponentsInChildren<Button>(true)
                        .FirstOrDefault(item => item.name.IndexOf("Add", StringComparison.OrdinalIgnoreCase) >= 0);
                context.GoalFormScroll = context.SpendAdd.GetComponentsInChildren<ScrollRect>(true)
                    .FirstOrDefault(item => context.GoalPreviewContent == null || item.content != context.GoalPreviewContent);
                context.GoalPreviewScroll = context.SpendAdd.GetComponentsInChildren<ScrollRect>(true)
                    .FirstOrDefault(item => item.content == context.GoalPreviewContent);
                if (context.GoalNameInput == null || context.GoalLimitInput == null)
                    context.Errors.Add("SpendAdd_1 목표명/소비 한도 Input 참조가 없습니다.");
                if (context.GoalPreviewContent == null || context.GoalPreviewScroll == null)
                    context.Errors.Add("SpendAdd_1 목표 Preview ScrollRect 참조가 없습니다.");
                if (context.GoalFormScroll == null)
                    context.Errors.Add("SpendAdd_1 전체 Form ScrollRect를 찾을 수 없습니다.");
                if (context.DailyButton == null || context.WeeklyButton == null)
                    context.Errors.Add("SpendAdd_1 Daily/Weekly 버튼 참조가 없습니다.");
                if (context.AddGoalButton == null || context.SkipButton == null || context.CompleteButton == null)
                    context.Errors.Add("SpendAdd_1 Add Goal/Skip/Complete 버튼 참조가 없습니다.");
            }

            context.SpendHistory = FindUnique(context.Canvas.transform, "SpendHistory", context.Errors)?.gameObject;
            if (context.SpendHistory != null)
            {
                context.TransactionManager = context.SpendHistory.GetComponent<SpendTransactionManager>();
                if (context.TransactionManager == null)
                    context.Errors.Add("'SpendHistory'에 SpendTransactionManager 컴포넌트가 없습니다.");
                else
                {
                    SerializedObject transactionManager = new SerializedObject(context.TransactionManager);
                    context.TransactionContent = transactionManager.FindProperty("content")?.objectReferenceValue as RectTransform;
                    context.TransactionItemPrefab = transactionManager.FindProperty("transactionItemPrefab")?.objectReferenceValue as GameObject;
                }
                context.TransactionScroll = context.SpendHistory.GetComponentsInChildren<ScrollRect>(true)
                    .FirstOrDefault(item => item.content == context.TransactionContent);
                context.HistoryTitle = context.SpendHistory.GetComponentsInChildren<TMP_Text>(true)
                    .FirstOrDefault(item => item.name.Equals("title", StringComparison.OrdinalIgnoreCase) ||
                                            item.name.Equals("Title", StringComparison.OrdinalIgnoreCase));
                context.HistoryBackButton = FindButtonByPersistentMethod(context.SpendHistory.transform, "BackToSpendHistory");
                if (context.TransactionContent == null || context.TransactionItemPrefab == null)
                    context.Errors.Add("SpendHistory content/transactionItemPrefab SerializeField 참조가 없습니다.");
                if (context.TransactionScroll == null || context.HistoryTitle == null || context.HistoryBackButton == null)
                    context.Errors.Add("SpendHistory 제목/ScrollRect/뒤로 가기 버튼 구조를 찾을 수 없습니다.");
                if (context.TransactionItemPrefab != null &&
                    (context.TransactionItemPrefab.transform.Find("CategoryText") == null ||
                     context.TransactionItemPrefab.transform.Find("AmountText") == null ||
                     context.TransactionItemPrefab.transform.Find("DateText") == null ||
                     context.TransactionItemPrefab.transform.Find("ExceptionToggle") == null))
                    context.Errors.Add("TransactionItem 원본의 런타임 조회 이름이 변경되었습니다.");
            }

            context.WeeklyReportPanel = FindUnique(context.Canvas.transform, "WeeklyReportPanel", context.Errors)?.gameObject;
            if (context.WeeklyReportPanel != null)
            {
                context.WeeklyReportManager = context.WeeklyReportPanel.GetComponent<WeeklyReportManager>();
                if (context.WeeklyReportManager == null)
                {
                    context.Errors.Add("'WeeklyReportPanel'에 WeeklyReportManager 컴포넌트가 없습니다.");
                }
                else
                {
                    SerializedObject reportManager = new SerializedObject(context.WeeklyReportManager);
                    context.ReportBackButton = reportManager.FindProperty("backButton")?.objectReferenceValue as Button;
                    context.ReportWeekText = reportManager.FindProperty("weekText")?.objectReferenceValue as TMP_Text;
                    context.HabitAchievementText = reportManager.FindProperty("habitAchievementText")?.objectReferenceValue as TMP_Text;
                    context.HabitCompletedText = reportManager.FindProperty("habitCompletedText")?.objectReferenceValue as TMP_Text;
                    context.SpendingBudgetText = reportManager.FindProperty("spendingBudgetText")?.objectReferenceValue as TMP_Text;
                    context.SpendingSpentText = reportManager.FindProperty("spendingSpentText")?.objectReferenceValue as TMP_Text;
                    context.SpendingUsageText = reportManager.FindProperty("spendingUsageText")?.objectReferenceValue as TMP_Text;
                    context.AiSummaryText = reportManager.FindProperty("aiSummaryText")?.objectReferenceValue as TMP_Text;
                    context.AiGoodPointText = reportManager.FindProperty("aiGoodPointText")?.objectReferenceValue as TMP_Text;
                    context.AiImprovementText = reportManager.FindProperty("aiImprovementText")?.objectReferenceValue as TMP_Text;
                    context.AiNextWeekText = reportManager.FindProperty("aiNextWeekText")?.objectReferenceValue as TMP_Text;
                }
                context.ReportScroll = context.WeeklyReportPanel.GetComponentsInChildren<ScrollRect>(true).FirstOrDefault();
                context.ReportContent = context.ReportScroll != null ? context.ReportScroll.content : null;
                Transform reportHeader = FindUnique(context.WeeklyReportPanel.transform, "Header", context.Errors);
                context.ReportTitle = reportHeader != null
                    ? reportHeader.GetComponentsInChildren<TMP_Text>(true)
                        .FirstOrDefault(item => item != context.ReportWeekText &&
                                                (context.ReportBackButton == null ||
                                                 !item.transform.IsChildOf(context.ReportBackButton.transform)))
                    : null;
                context.HabitSummaryCard = FindUnique(context.WeeklyReportPanel.transform, "HabitSummaryCard", context.Errors) as RectTransform;
                context.SpendingSummaryCard = FindUnique(context.WeeklyReportPanel.transform, "SpendingSummaryCard", context.Errors) as RectTransform;
                context.AiFeedbackCard = FindUnique(context.WeeklyReportPanel.transform, "AIFeedbackCard", context.Errors) as RectTransform;
                if (context.ReportBackButton == null || context.ReportWeekText == null || context.ReportTitle == null ||
                    context.ReportScroll == null ||
                    context.ReportContent == null || context.HabitAchievementText == null || context.HabitCompletedText == null ||
                    context.SpendingBudgetText == null || context.SpendingSpentText == null || context.SpendingUsageText == null ||
                    context.AiSummaryText == null || context.AiGoodPointText == null || context.AiImprovementText == null ||
                    context.AiNextWeekText == null)
                    context.Errors.Add("WeeklyReportManager SerializeField 또는 ScrollRect 참조가 누락되었습니다.");
            }
        }

        if (logErrors)
            foreach (string error in context.Errors) Debug.LogError("[HabitPVP Spending] " + error);
        return context;
    }

    private static void ApplySpendHistoryStyle(Context context)
    {
        Image background = GetOrAdd<Image>(context.SpendHistory);
        Record(background, "Style SpendHistory background");
        background.color = Cream;
        background.sprite = null;
        background.raycastTarget = true;

        Record(context.HistoryTitle, "Style SpendHistory title");
        context.HistoryTitle.text = "소비 내역";
        context.HistoryTitle.font = context.Font;
        context.HistoryTitle.fontSize = 48f;
        context.HistoryTitle.fontStyle = FontStyles.Bold;
        context.HistoryTitle.color = Brown;
        context.HistoryTitle.alignment = TextAlignmentOptions.Center;
        SetTopCentered(context.HistoryTitle.rectTransform, new Vector2(700f, 88f), new Vector2(70f, -42f));

        Record(context.TransactionScroll, "Enable SpendHistory scrolling");
        context.TransactionScroll.horizontal = false;
        context.TransactionScroll.vertical = true;
        context.TransactionScroll.movementType = ScrollRect.MovementType.Elastic;
        context.TransactionScroll.inertia = true;
        context.TransactionScroll.scrollSensitivity = 35f;
        RectTransform scrollRect = context.TransactionScroll.GetComponent<RectTransform>();
        Record(scrollRect, "Layout SpendHistory scroll");
        SetStretch(scrollRect, new Vector2(42f, 54f), new Vector2(-42f, -150f));
        if (context.TransactionScroll.viewport != null)
        {
            Graphic viewportGraphic = context.TransactionScroll.viewport.GetComponent<Graphic>();
            if (viewportGraphic != null)
            {
                Record(viewportGraphic, "Enable SpendHistory viewport raycast");
                viewportGraphic.raycastTarget = true;
            }
        }

        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(context.TransactionContent.gameObject);
        Record(layout, "Layout SpendHistory content");
        layout.padding = new RectOffset(18, 18, 18, 24);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(context.TransactionContent.gameObject);
        Record(fitter, "Fit SpendHistory content");
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        StyleButton(context.HistoryBackButton, context.CreamButtonSprite, context);
        RectTransform backRect = context.HistoryBackButton.GetComponent<RectTransform>();
        Record(backRect, "Layout SpendHistory back button");
        backRect.anchorMin = backRect.anchorMax = backRect.pivot = new Vector2(0f, 1f);
        backRect.sizeDelta = new Vector2(190f, 78f);
        backRect.anchoredPosition = new Vector2(36f, -38f);
    }

    private static void ApplyWeeklyReportStyle(Context context)
    {
        Image panelImage = GetOrAdd<Image>(context.WeeklyReportPanel);
        Record(panelImage, "Style weekly report background");
        panelImage.color = Cream;
        panelImage.raycastTarget = true;

        context.ReportScroll.horizontal = false;
        context.ReportScroll.vertical = true;
        context.ReportScroll.movementType = ScrollRect.MovementType.Elastic;
        context.ReportScroll.scrollSensitivity = 38f;
        if (context.ReportScroll.viewport != null)
        {
            Graphic viewportGraphic = GetOrAdd<Image>(context.ReportScroll.viewport.gameObject);
            viewportGraphic.color = new Color32(255, 255, 255, 1);
            viewportGraphic.raycastTarget = true;
            Mask mask = GetOrAdd<Mask>(context.ReportScroll.viewport.gameObject);
            mask.showMaskGraphic = false;
        }

        VerticalLayoutGroup contentLayout = GetOrAdd<VerticalLayoutGroup>(context.ReportContent.gameObject);
        contentLayout.padding = new RectOffset(42, 42, 30, 70);
        contentLayout.spacing = 28f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        ContentSizeFitter contentFitter = GetOrAdd<ContentSizeFitter>(context.ReportContent.gameObject);
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        context.ReportContent.anchorMin = new Vector2(0f, 1f);
        context.ReportContent.anchorMax = new Vector2(1f, 1f);
        context.ReportContent.pivot = new Vector2(0.5f, 1f);
        context.ReportContent.anchoredPosition = Vector2.zero;
        context.ReportContent.sizeDelta = new Vector2(0f, context.ReportContent.sizeDelta.y);

        StyleButton(context.ReportBackButton, context.CreamButtonSprite, context);
        SetButtonLabel(context.ReportBackButton, "← 뒤로");
        TMP_Text backLabel = context.ReportBackButton.GetComponentInChildren<TMP_Text>(true);
        if (backLabel != null) backLabel.fontSize = 28f;

        StyleWeeklyText(context.ReportWeekText, 31f, FontStyles.Bold, Gold, TextAlignmentOptions.Center);
        StyleWeeklyText(context.ReportTitle, 43f, FontStyles.Bold, Brown, TextAlignmentOptions.Center);
        StyleWeeklyCard(context.HabitSummaryCard, 300f, context);
        StyleWeeklyCard(context.SpendingSummaryCard, 360f, context);
        StyleWeeklyCard(context.AiFeedbackCard, 1320f, context);

        foreach (TMP_Text text in new[] { context.HabitAchievementText, context.HabitCompletedText })
            StyleWeeklyStat(text, context);
        foreach (TMP_Text text in new[] { context.SpendingBudgetText, context.SpendingSpentText, context.SpendingUsageText })
            StyleWeeklyStat(text, context);

        StyleWeeklyFeedback(context.AiSummaryText, 250f, context);
        StyleWeeklyFeedback(context.AiGoodPointText, 250f, context);
        StyleWeeklyFeedback(context.AiImprovementText, 330f, context);
        StyleWeeklyFeedback(context.AiNextWeekText, 290f, context);
    }

    private static void StyleWeeklyCard(RectTransform card, float minimumHeight, Context context)
    {
        if (card == null) return;
        Image image = GetOrAdd<Image>(card.gameObject);
        image.sprite = context.CardSprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        LayoutElement element = GetOrAdd<LayoutElement>(card.gameObject);
        element.minHeight = minimumHeight;
        element.preferredHeight = -1f;
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(card.gameObject);
        layout.padding = new RectOffset(34, 34, 30, 34);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static void StyleWeeklyStat(TMP_Text text, Context context)
    {
        StyleWeeklyText(text, 29f, FontStyles.Bold, Brown, TextAlignmentOptions.MidlineLeft);
        EnsureWeeklyTextCard(text, 72f, false, context);
        text.margin = Vector4.zero;
    }

    private static void StyleWeeklyFeedback(TMP_Text text, float minimumHeight, Context context)
    {
        StyleWeeklyText(text, 27f, FontStyles.Normal, Brown, TextAlignmentOptions.TopLeft);
        text.margin = Vector4.zero;
        text.lineSpacing = 8f;
        EnsureWeeklyTextCard(text, minimumHeight, true, context);
    }

    private static void EnsureWeeklyTextCard(TMP_Text text, float minimumHeight, bool fitContent, Context context)
    {
        if (text == null)
            throw new InvalidOperationException("WeeklyReportPanel TMP 참조가 없어 배경 카드를 생성할 수 없습니다.");
        if (text.transform.parent == null)
            throw new InvalidOperationException($"WeeklyReportPanel '{text.name}' 텍스트에 부모 Transform이 없습니다.");

        string cardName = text.name + "Card";
        Transform originalParent = text.transform.parent;
        Image background = originalParent.name == cardName ? originalParent.GetComponent<Image>() : null;
        if (background == null)
        {
            int siblingIndex = text.transform.GetSiblingIndex();
            background = CreateImage(originalParent, cardName, context.CreamButtonSprite, CreamLight);
            background.transform.SetSiblingIndex(siblingIndex);
            Undo.SetTransformParent(text.transform, background.transform, "Wrap weekly report text in background card");
        }

        background.sprite = context.CreamButtonSprite;
        background.type = Image.Type.Sliced;
        background.color = CreamLight;
        background.raycastTarget = false;

        LayoutElement element = GetOrAdd<LayoutElement>(background.gameObject);
        element.minHeight = minimumHeight;
        element.preferredHeight = fitContent ? -1f : minimumHeight;
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(background.gameObject);
        layout.padding = fitContent ? new RectOffset(30, 30, 24, 24) : new RectOffset(24, 24, 10, 10);
        layout.spacing = 0f;
        layout.childAlignment = fitContent ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = !fitContent;

        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(background.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = fitContent ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;

        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private static void StyleWeeklyText(TMP_Text text, float size, FontStyles style, Color color,
        TextAlignmentOptions alignment)
    {
        if (text == null) return;
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
    }

    private static void ApplyMainStyle(Context context)
    {
        RectTransform scrollRect = context.Scroll.GetComponent<RectTransform>();
        Record(scrollRect, "Style spending main scroll");
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.offsetMin = new Vector2(24f, 150f);
        scrollRect.offsetMax = new Vector2(-24f, -72f);
        context.Scroll.horizontal = false;
        context.Scroll.vertical = true;
        context.Scroll.movementType = ScrollRect.MovementType.Elastic;
        context.Scroll.scrollSensitivity = 35f;

        RectTransform content = context.Content;
        Record(content, "Style spending content");
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, content.sizeDelta.y);
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(content.gameObject);
        layout.padding = new RectOffset(14, 14, 22, 50);
        layout.spacing = 26f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(content.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        StyleCard(context.BudgetCard, 350f, context);
        StyleCard(context.RecordCard, 180f, context);
        StyleCard(context.GoldListCard, 520f, context);
        StyleCard(context.RewardCard, 410f, context);
        StyleButton(context.BudgetSettingButton, context.CreamButtonSprite, context);
        StyleButton(context.RecordButton, context.GoldButtonSprite, context);
        StyleButton(context.HistoryButton, context.CreamButtonSprite, context);
        StyleButton(context.ClaimButton, context.GoldButtonSprite, context);
        StyleBudget(context);
        StyleGoalScroll(context);

        foreach (TMP_Text text in context.Spend.GetComponentsInChildren<TMP_Text>(true))
        {
            Record(text, "Style spending text");
            text.font = context.Font;
            text.color = text.name.IndexOf("Gold", StringComparison.OrdinalIgnoreCase) >= 0 ? Gold : Brown;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        float preferredHeight = LayoutUtility.GetPreferredHeight(content);
        if (preferredHeight > 0f)
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
    }

    private static void StyleCard(RectTransform card, float height, Context context)
    {
        if (card == null) return;
        Image image = card.GetComponent<Image>();
        if (image != null)
        {
            Record(image, "Style spending card image");
            image.sprite = context.CardSprite;
            image.color = Color.white;
            image.type = Image.Type.Sliced;
        }
        LayoutElement element = GetOrAdd<LayoutElement>(card.gameObject);
        element.minHeight = height;
        element.preferredHeight = height;
        element.flexibleHeight = 0f;
    }

    private static void ApplyBudgetSettingStyle(Context context)
    {
        Image background = context.BudgetSetting.GetComponent<Image>();
        if (background != null)
        {
            Record(background, "Style BudgetSetting background");
            background.sprite = context.CardSprite;
            background.color = Cream;
            background.type = Image.Type.Sliced;
        }

        Record(context.BudgetSettingContent, "Layout BudgetSetting content");
        SetCentered(context.BudgetSettingContent, new Vector2(960f, 1160f), new Vector2(0f, 35f));

        StyleBudgetSettingText(context.BudgetSettingTitle, 48f, FontStyles.Bold, Brown);
        context.BudgetSettingTitle.text = "주간 예산 설정";
        SetTopCentered(context.BudgetSettingTitle.rectTransform, new Vector2(860f, 88f), new Vector2(0f, -50f));

        StyleBudgetSettingText(context.BudgetDescription, 30f, FontStyles.Bold, MutedBrown);
        context.BudgetDescription.text = "이번 주 소비 한도를 설정해보세요.";
        SetTopCentered(context.BudgetDescription.rectTransform, new Vector2(860f, 62f), new Vector2(0f, -155f));

        Image cardImage = context.BudgetSettingCard.GetComponent<Image>();
        if (cardImage != null)
        {
            Record(cardImage, "Style BudgetSetting card");
            cardImage.sprite = context.CardSprite;
            cardImage.color = Color.white;
            cardImage.type = Image.Type.Sliced;
        }
        Record(context.BudgetSettingCard, "Layout BudgetSetting card");
        SetTopCentered(context.BudgetSettingCard, new Vector2(880f, 330f), new Vector2(0f, -245f));

        TMP_Text budgetLabel = context.BudgetSettingCard.Find("BudgetLabel")?.GetComponent<TMP_Text>();
        if (budgetLabel != null)
        {
            StyleBudgetSettingText(budgetLabel, 33f, FontStyles.Bold, Brown);
            budgetLabel.text = "이번 주 예산";
            SetTopCentered(budgetLabel.rectTransform, new Vector2(720f, 62f), new Vector2(0f, -40f));
        }

        RectTransform inputRect = context.BudgetInput.GetComponent<RectTransform>();
        Record(inputRect, "Layout BudgetSetting input");
        SetTopCentered(inputRect, new Vector2(700f, 112f), new Vector2(0f, -130f));
        Image inputImage = context.BudgetInput.GetComponent<Image>();
        if (inputImage != null)
        {
            Record(inputImage, "Style BudgetSetting input");
            inputImage.sprite = context.CreamButtonSprite;
            inputImage.color = CreamLight;
            inputImage.type = Image.Type.Sliced;
        }
        context.BudgetInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        context.BudgetInput.characterLimit = 10;
        if (context.BudgetInput.textComponent != null)
        {
            StyleBudgetSettingText(context.BudgetInput.textComponent, 36f, FontStyles.Bold, Brown);
            context.BudgetInput.textComponent.alignment = TextAlignmentOptions.MidlineRight;
        }
        if (context.BudgetInput.placeholder is TMP_Text placeholder)
        {
            StyleBudgetSettingText(placeholder, 31f, FontStyles.Normal, MutedBrown);
            placeholder.text = "예: 500000";
            placeholder.alignment = TextAlignmentOptions.MidlineRight;
        }

        StyleBudgetSettingText(context.BudgetDescriptionSecondary, 27f, FontStyles.Bold, MutedBrown);
        context.BudgetDescriptionSecondary.text = "원 단위 · 0원보다 큰 숫자만 저장할 수 있습니다.";
        SetTopCentered(context.BudgetDescriptionSecondary.rectTransform, new Vector2(860f, 70f), new Vector2(0f, -610f));

        StyleButton(context.BudgetBackButton, context.CreamButtonSprite, context);
        StyleButton(context.BudgetSaveButton, context.GoldButtonSprite, context);
        SetButtonLabel(context.BudgetBackButton, "뒤로 가기");
        SetButtonLabel(context.BudgetSaveButton, "예산 저장");
        Record(context.BudgetBackButton.GetComponent<RectTransform>(), "Layout BudgetSetting back button");
        Record(context.BudgetSaveButton.GetComponent<RectTransform>(), "Layout BudgetSetting save button");
        SetTopCentered(context.BudgetBackButton.GetComponent<RectTransform>(), new Vector2(390f, 108f),
            new Vector2(-215f, -750f));
        SetTopCentered(context.BudgetSaveButton.GetComponent<RectTransform>(), new Vector2(390f, 108f),
            new Vector2(215f, -750f));
    }

    private static void StyleBudgetSettingText(TMP_Text text, float size, FontStyles style, Color color)
    {
        if (text == null) return;
        Record(text, "Style BudgetSetting text");
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.Center;
    }

    private static void ApplySpendGoalStyle(Context context)
    {
        Image background = context.SpendAdd.GetComponent<Image>();
        if (background != null)
        {
            Record(background, "Style SpendAdd background");
            background.sprite = context.CardSprite;
            background.color = Cream;
            background.type = Image.Type.Sliced;
        }
        if (context.GoalFormScroll != null)
        {
            context.GoalFormScroll.horizontal = false;
            context.GoalFormScroll.vertical = true;
            context.GoalFormScroll.movementType = ScrollRect.MovementType.Clamped;
            context.GoalFormScroll.scrollSensitivity = 32f;
        }
        StyleGoalInput(context.GoalNameInput, "예: 배달 음식 줄이기", false, context);
        StyleGoalInput(context.GoalLimitInput, "예: 80000", true, context);
        StyleButton(context.DailyButton, context.CreamButtonSprite, context);
        StyleButton(context.WeeklyButton, context.CreamButtonSprite, context);
        StyleButton(context.AddGoalButton, context.GoldButtonSprite, context);
        StyleButton(context.SkipButton, context.CreamButtonSprite, context);
        StyleButton(context.CompleteButton, context.GoldButtonSprite, context);
        SetButtonLabel(context.DailyButton, "Daily");
        SetButtonLabel(context.WeeklyButton, "Weekly");
        SetButtonLabel(context.AddGoalButton, "목표 추가");
        SetButtonLabel(context.SkipButton, "Skip");
        SetButtonLabel(context.CompleteButton, "Complete");
        context.GoalPreviewScroll.horizontal = false;
        context.GoalPreviewScroll.vertical = true;
        context.GoalPreviewScroll.movementType = ScrollRect.MovementType.Clamped;
        context.GoalPreviewScroll.scrollSensitivity = 30f;
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(context.GoalPreviewContent.gameObject);
        layout.padding = new RectOffset(12, 12, 12, 18);
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(context.GoalPreviewContent.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        foreach (RectTransform child in context.GoalPreviewContent.Cast<Transform>().OfType<RectTransform>())
        {
            Image image = child.GetComponent<Image>();
            if (image != null)
            {
                Record(image, "Style SpendAdd preview card");
                image.sprite = context.CardSprite;
                image.color = Color.white;
                image.type = Image.Type.Sliced;
            }
            LayoutElement element = GetOrAdd<LayoutElement>(child.gameObject);
            element.minHeight = 126f;
            element.preferredHeight = 126f;
        }
        foreach (TMP_Text text in context.SpendAdd.GetComponentsInChildren<TMP_Text>(true))
        {
            Record(text, "Style SpendAdd text");
            text.font = context.Font;
            text.color = text.name.IndexOf("Gold", StringComparison.OrdinalIgnoreCase) >= 0 ? Gold : Brown;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }
    }

    private static void StyleGoalInput(TMP_InputField input, string placeholderValue, bool numeric, Context context)
    {
        Image image = input.GetComponent<Image>();
        if (image != null)
        {
            Record(image, "Style SpendAdd input");
            image.sprite = context.CreamButtonSprite;
            image.color = CreamLight;
            image.type = Image.Type.Sliced;
        }
        if (numeric)
        {
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.characterLimit = 10;
        }
        if (input.textComponent != null)
        {
            input.textComponent.font = context.Font;
            input.textComponent.fontSize = 32f;
            input.textComponent.color = Brown;
            input.textComponent.enableWordWrapping = false;
            input.textComponent.alignment = numeric ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
        }
        if (input.placeholder is TMP_Text placeholder)
        {
            placeholder.font = context.Font;
            placeholder.fontSize = 28f;
            placeholder.color = MutedBrown;
            placeholder.text = placeholderValue;
            placeholder.enableWordWrapping = false;
            placeholder.alignment = numeric ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
        }
    }

    private static void ApplyRecordSpendStyle(Context context)
    {
        Image background = context.RecordSpendAdd.GetComponent<Image>();
        if (background != null)
        {
            Record(background, "Style RecordSpendAdd background");
            background.sprite = context.CardSprite;
            background.color = Cream;
            background.type = Image.Type.Sliced;
        }
        context.RecordFormScroll.horizontal = false;
        context.RecordFormScroll.vertical = true;
        context.RecordFormScroll.movementType = ScrollRect.MovementType.Clamped;
        context.RecordFormScroll.scrollSensitivity = 32f;
        StyleGoalInput(context.RecordAmountInput, "예: 12000", true, context);
        RectTransform amountRect = context.RecordAmountInput.GetComponent<RectTransform>();
        Record(amountRect, "Layout RecordSpendAdd amount input");
        amountRect.sizeDelta = new Vector2(680f, 112f);
        StyleDropdown(context.RecordCategoryDropdown, context);
        RectTransform dropdownRect = context.RecordCategoryDropdown.GetComponent<RectTransform>();
        Record(dropdownRect, "Layout RecordSpendAdd category dropdown");
        dropdownRect.sizeDelta = new Vector2(740f, 112f);
        StyleButton(context.RecordBackButton, context.CreamButtonSprite, context);
        StyleButton(context.RecordSaveButton, context.GoldButtonSprite, context);
        SetButtonLabel(context.RecordBackButton, "뒤로 가기");
        SetButtonLabel(context.RecordSaveButton, "소비 저장");
        RectTransform backRect = context.RecordBackButton.GetComponent<RectTransform>();
        RectTransform saveRect = context.RecordSaveButton.GetComponent<RectTransform>();
        if (backRect.parent == saveRect.parent)
        {
            float buttonY = (backRect.anchoredPosition.y + saveRect.anchoredPosition.y) * 0.5f;
            Record(backRect, "Layout RecordSpendAdd back button");
            Record(saveRect, "Layout RecordSpendAdd save button");
            backRect.anchoredPosition = new Vector2(-215f, buttonY);
            saveRect.anchoredPosition = new Vector2(215f, buttonY);
            backRect.sizeDelta = new Vector2(390f, 108f);
            saveRect.sizeDelta = new Vector2(390f, 108f);
        }
        foreach (TMP_Text text in context.RecordSpendAdd.GetComponentsInChildren<TMP_Text>(true))
        {
            Record(text, "Style RecordSpendAdd text");
            text.font = context.Font;
            text.color = Brown;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }
    }

    private static void StyleDropdown(TMP_Dropdown dropdown, Context context)
    {
        Image image = dropdown.GetComponent<Image>();
        if (image != null)
        {
            Record(image, "Style RecordSpendAdd dropdown");
            image.sprite = context.CreamButtonSprite;
            image.color = CreamLight;
            image.type = Image.Type.Sliced;
        }
        if (dropdown.captionText != null)
        {
            Record(dropdown.captionText, "Style RecordSpendAdd dropdown caption");
            dropdown.captionText.font = context.Font;
            dropdown.captionText.fontSize = 32f;
            dropdown.captionText.fontStyle = FontStyles.Bold;
            dropdown.captionText.color = Brown;
            dropdown.captionText.raycastTarget = false;
        }
        if (dropdown.itemText != null)
        {
            Record(dropdown.itemText, "Style RecordSpendAdd dropdown item");
            dropdown.itemText.font = context.Font;
            dropdown.itemText.fontSize = 31f;
            dropdown.itemText.fontStyle = FontStyles.Bold;
            dropdown.itemText.color = Brown;
            dropdown.itemText.raycastTarget = false;
        }
        RectTransform template = dropdown.template;
        if (template == null) return;
        Record(template, "Resize RecordSpendAdd dropdown template");
        if (template.sizeDelta.y < 420f)
            template.sizeDelta = new Vector2(template.sizeDelta.x, 420f);
        ScrollRect scroll = template.GetComponent<ScrollRect>();
        if (scroll == null) scroll = template.GetComponentInChildren<ScrollRect>(true);
        if (scroll != null)
        {
            Record(scroll, "Enable RecordSpendAdd dropdown scrolling");
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.scrollSensitivity = 32f;
            if (scroll.viewport == null)
            {
                Transform viewport = template.Find("Viewport");
                if (viewport != null) scroll.viewport = viewport as RectTransform;
            }
            if (scroll.content == null && scroll.viewport != null)
            {
                Transform content = scroll.viewport.Find("Content");
                if (content != null) scroll.content = content as RectTransform;
            }
        }
        if (scroll != null && scroll.viewport != null)
        {
            Graphic viewportGraphic = scroll.viewport.GetComponent<Graphic>();
            if (viewportGraphic != null)
            {
                Record(viewportGraphic, "Enable RecordSpendAdd dropdown viewport raycast");
                viewportGraphic.raycastTarget = true;
            }
        }
    }

    private static void SetButtonLabel(Button button, string value)
    {
        TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
        if (label != null) label.text = value;
    }

    private static void StyleButton(Button button, Sprite sprite, Context context)
    {
        if (button == null) return;
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            Record(image, "Style spending button");
            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Sliced;
        }
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            Record(label, "Style spending button label");
            label.font = context.Font;
            label.fontSize = 30f;
            label.fontStyle = FontStyles.Bold;
            label.color = Brown;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
        }
    }

    private static void StyleBudget(Context context)
    {
        StyleMoneyText(context.BudgetText, TextAlignmentOptions.MidlineRight, new Vector2(-46f, -82f));
        StyleMoneyText(context.UsedText, TextAlignmentOptions.MidlineRight, new Vector2(-46f, -138f));
        Slider slider = context.BudgetSlider;
        if (slider == null) return;
        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        Record(sliderRect, "Style budget slider rect");
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(0f, 76f);
        sliderRect.sizeDelta = new Vector2(-90f, 36f);
        Image background = slider.transform.Find("Background")?.GetComponent<Image>();
        Image fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
        if (background != null)
        {
            Record(background, "Style budget slider track");
            background.sprite = context.CreamButtonSprite;
            background.color = Cream;
            background.type = Image.Type.Sliced;
        }
        if (fill != null)
        {
            Record(fill, "Style budget slider fill");
            fill.sprite = context.GoldButtonSprite;
            fill.color = Gold;
            fill.type = Image.Type.Sliced;
        }
    }

    private static void StyleMoneyText(TMP_Text text, TextAlignmentOptions alignment, Vector2 position)
    {
        if (text == null) return;
        Record(text, "Align budget money text");
        text.alignment = alignment;
        text.fontSize = 34f;
        text.fontStyle = FontStyles.Bold;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(-92f, 52f);
    }

    private static void StyleGoalScroll(Context context)
    {
        if (context.GoalScroll == null) return;
        context.GoalScroll.horizontal = false;
        context.GoalScroll.vertical = true;
        context.GoalScroll.movementType = ScrollRect.MovementType.Clamped;
        context.GoalScroll.scrollSensitivity = 28f;
        RectTransform rect = context.GoalScroll.GetComponent<RectTransform>();
        Record(rect, "Style goal scroll");
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(30f, 30f);
        rect.offsetMax = new Vector2(-30f, -90f);
        RectTransform goalContent = context.GoalScroll.content;
        if (goalContent == null) return;
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(goalContent.gameObject);
        layout.padding = new RectOffset(8, 8, 8, 18);
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(goalContent.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        foreach (RectTransform child in goalContent.Cast<Transform>().OfType<RectTransform>())
        {
            LayoutElement element = GetOrAdd<LayoutElement>(child.gameObject);
            element.minHeight = 118f;
            element.preferredHeight = 118f;
        }
    }

    private static void EnsurePreviewRoot(Context context)
    {
        GameObject existing = FindRoot(context.Scene, PreviewRootName);
        if (existing != null) Undo.DestroyObjectImmediate(existing);
        GameObject root = new GameObject(PreviewRootName, typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(root, "Create Spending preview root");
        root.transform.SetAsLastSibling();
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 550;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 1f;

        Image background = CreateImage(root.transform, "PreviewBackground", null, Cream);
        SetStretch(background.rectTransform, Vector2.zero, Vector2.zero);
        TMP_Text heading = CreateText(background.transform, "PreviewHeading",
            "J. SPENDING & REPORT · LOCAL PREVIEW", context.Font, 27f, MutedBrown, FontStyles.Bold);
        SetCentered(heading.rectTransform, new Vector2(980f, 44f), new Vector2(0f, 885f));

        Image scrollPanel = CreateImage(background.transform, "MainPanel", context.CardSprite, CreamLight);
        SetCentered(scrollPanel.rectTransform, new Vector2(1020f, 1650f), new Vector2(0f, -5f));
        ScrollRect scroll = Undo.AddComponent<ScrollRect>(scrollPanel.gameObject);
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 35f;
        Image viewport = CreateImage(scrollPanel.transform, "Viewport", null, new Color32(255, 255, 255, 5));
        SetStretch(viewport.rectTransform, new Vector2(34f, 38f), new Vector2(-34f, -38f));
        Mask mask = Undo.AddComponent<Mask>(viewport.gameObject);
        mask.showMaskGraphic = false;
        RectTransform content = CreateContent(viewport.transform);
        scroll.viewport = viewport.rectTransform;
        scroll.content = content;

        TMP_Text title = CreateText(content, "Title", "이번 주 소비 원정", context.Font, 50f, Brown, FontStyles.Bold);
        AddLayout(title.gameObject, 86f);
        TMP_Text notice = CreateText(content, "PreviewNotice",
            "아래 금액·목표·보상은 Preview 전용 가상 데이터입니다.", context.Font, 27f, MutedBrown, FontStyles.Bold);
        notice.enableWordWrapping = true;
        AddLayout(notice.gameObject, 68f);

        Image budgetCard = CreatePreviewCard(content, "BudgetCard", 390f, context);
        CreateCardTitle(budgetCard.transform, "주간 예산", context);
        TMP_Text budgetText = CreateText(budgetCard.transform, "BudgetText", "500,000원", context.Font, 42f, Brown, FontStyles.Bold);
        budgetText.alignment = TextAlignmentOptions.MidlineRight;
        SetCentered(budgetText.rectTransform, new Vector2(430f, 64f), new Vector2(210f, 78f));
        TMP_Text usedText = CreateText(budgetCard.transform, "UsedText", "320,000원 (64%)", context.Font, 31f, MutedBrown, FontStyles.Bold);
        usedText.alignment = TextAlignmentOptions.MidlineRight;
        SetCentered(usedText.rectTransform, new Vector2(430f, 58f), new Vector2(210f, 18f));
        Slider budgetSlider = CreateSlider(budgetCard.transform, context);
        Button budgetSetting = CreateButton(budgetCard.transform, "BudgetSettingButton", "예산 설정",
            context.CreamButtonSprite, context.Font, new Vector2(-225f, -125f), new Vector2(360f, 92f));
        Button toggleBudget = CreateButton(budgetCard.transform, "ToggleBudgetButton", "가상 사용액 변경",
            context.GoldButtonSprite, context.Font, new Vector2(225f, -125f), new Vector2(410f, 92f));

        Image recordCard = CreatePreviewCard(content, "RecordCard", 240f, context);
        CreateCardTitle(recordCard.transform, "소비 기록", context);
        Button record = CreateButton(recordCard.transform, "RecordButton", "소비 기록하기",
            context.GoldButtonSprite, context.Font, new Vector2(-230f, -45f), new Vector2(410f, 102f));
        Button history = CreateButton(recordCard.transform, "ViewHistoryButton", "내역 보기",
            context.CreamButtonSprite, context.Font, new Vector2(230f, -45f), new Vector2(410f, 102f));

        Image goalCard = CreatePreviewCard(content, "GoldListCard", 370f, context);
        CreateCardTitle(goalCard.transform, "소비 목표", context);
        TMP_Text goalStatus = CreateText(goalCard.transform, "GoalStatus",
            "외식비 80,000원 이하 · 62,000원 사용 · 진행 중", context.Font, 30f, Brown, FontStyles.Bold);
        goalStatus.enableWordWrapping = true;
        goalStatus.overflowMode = TextOverflowModes.Ellipsis;
        SetCentered(goalStatus.rectTransform, new Vector2(820f, 100f), new Vector2(0f, 30f));
        Button toggleGoal = CreateButton(goalCard.transform, "ToggleGoalButton", "가상 달성 상태 변경",
            context.CreamButtonSprite, context.Font, new Vector2(-215f, -105f), new Vector2(390f, 94f));
        Button openGoalSetting = CreateButton(goalCard.transform, "OpenGoalSettingButton", "소비 목표 추가",
            context.GoldButtonSprite, context.Font, new Vector2(235f, -105f), new Vector2(390f, 94f));

        Image rewardCard = CreatePreviewCard(content, "RewardCard", 360f, context);
        CreateCardTitle(rewardCard.transform, "소비 보상", context);
        TMP_Text streak = CreateText(rewardCard.transform, "StreakText", "연속 절약 스트릭 · 3주",
            context.Font, 31f, Brown, FontStyles.Bold);
        SetCentered(streak.rectTransform, new Vector2(820f, 55f), new Vector2(0f, 68f));
        TMP_Text rewardStatus = CreateText(rewardCard.transform, "RewardStatus",
            "3주 연속 달성 · 예상 보상 860 G", context.Font, 29f, MutedBrown, FontStyles.Bold);
        SetCentered(rewardStatus.rectTransform, new Vector2(820f, 60f), new Vector2(0f, 12f));
        Button claim = CreateButton(rewardCard.transform, "ClaimRewardButton", "가상 보상 수령",
            context.GoldButtonSprite, context.Font, new Vector2(0f, -105f), new Vector2(520f, 100f));

        Image reportCard = CreatePreviewCard(content, "ReportCard", 230f, context);
        CreateCardTitle(reportCard.transform, "주간 리포트", context);
        Button report = CreateButton(reportCard.transform, "ReportButton", "주간 리포트 열기",
            context.CreamButtonSprite, context.Font, new Vector2(0f, -45f), new Vector2(600f, 100f));

        TMP_Text localNotice = CreateText(content, "LocalNotice",
            "Preview 전용 가상 데이터 · 서버와 게임 Manager를 호출하지 않습니다.",
            context.Font, 26f, Brown, FontStyles.Bold);
        localNotice.enableWordWrapping = true;
        localNotice.overflowMode = TextOverflowModes.Ellipsis;
        AddLayout(localNotice.gameObject, 105f);

        Image budgetSettingPanel = CreateImage(background.transform, "BudgetSettingPanel", context.CardSprite, CreamLight);
        SetCentered(budgetSettingPanel.rectTransform, new Vector2(1020f, 1540f), new Vector2(0f, -5f));
        TMP_Text settingTitle = CreateText(budgetSettingPanel.transform, "Title", "주간 예산 설정",
            context.Font, 50f, Brown, FontStyles.Bold);
        SetCentered(settingTitle.rectTransform, new Vector2(850f, 86f), new Vector2(0f, 580f));
        TMP_Text settingDescription = CreateText(budgetSettingPanel.transform, "Description",
            "현재 가상 예산을 확인하고 새 금액을 입력해보세요.",
            context.Font, 29f, MutedBrown, FontStyles.Bold);
        settingDescription.enableWordWrapping = true;
        SetCentered(settingDescription.rectTransform, new Vector2(840f, 100f), new Vector2(0f, 470f));

        Image settingCard = CreateImage(budgetSettingPanel.transform, "BudgetCard", context.CardSprite, Color.white);
        SetCentered(settingCard.rectTransform, new Vector2(870f, 410f), new Vector2(0f, 170f));
        TMP_Text settingLabel = CreateText(settingCard.transform, "BudgetLabel", "이번 주 예산",
            context.Font, 34f, Brown, FontStyles.Bold);
        SetCentered(settingLabel.rectTransform, new Vector2(720f, 60f), new Vector2(0f, 115f));
        TMP_InputField budgetInput = CreateInputField(settingCard.transform, context,
            new Vector2(-40f, 5f), new Vector2(690f, 118f));
        TMP_Text wonLabel = CreateText(settingCard.transform, "WonLabel", "원", context.Font, 34f, Brown, FontStyles.Bold);
        SetCentered(wonLabel.rectTransform, new Vector2(75f, 90f), new Vector2(355f, 5f));
        TMP_Text ruleText = CreateText(settingCard.transform, "RuleText",
            "0원보다 큰 숫자만 저장할 수 있습니다.", context.Font, 27f, MutedBrown, FontStyles.Bold);
        SetCentered(ruleText.rectTransform, new Vector2(760f, 70f), new Vector2(0f, -105f));

        TMP_Text settingNotice = CreateText(budgetSettingPanel.transform, "BudgetSettingNotice",
            "현재 가상 예산이 입력되어 있습니다.", context.Font, 28f, MutedBrown, FontStyles.Bold);
        settingNotice.enableWordWrapping = true;
        SetCentered(settingNotice.rectTransform, new Vector2(840f, 95f), new Vector2(0f, -115f));
        Button budgetBack = CreateButton(budgetSettingPanel.transform, "BackButton", "뒤로 가기",
            context.CreamButtonSprite, context.Font, new Vector2(-225f, -300f), new Vector2(390f, 108f));
        Button budgetSave = CreateButton(budgetSettingPanel.transform, "SaveButton", "예산 저장",
            context.GoldButtonSprite, context.Font, new Vector2(225f, -300f), new Vector2(390f, 108f));
        TMP_Text localOnly = CreateText(budgetSettingPanel.transform, "LocalOnlyNotice",
            "J Preview 로컬 데이터만 변경됩니다. 실제 예산은 저장되지 않습니다.",
            context.Font, 25f, Brown, FontStyles.Bold);
        localOnly.enableWordWrapping = true;
        SetCentered(localOnly.rectTransform, new Vector2(820f, 100f), new Vector2(0f, -455f));
        budgetSettingPanel.gameObject.SetActive(false);

        Image recordSpendPanel = CreateImage(background.transform, "RecordSpendPanel", context.CardSprite, CreamLight);
        SetCentered(recordSpendPanel.rectTransform, new Vector2(1020f, 1540f), new Vector2(0f, -5f));
        TMP_Text recordTitle = CreateText(recordSpendPanel.transform, "Title", "소비 기록",
            context.Font, 50f, Brown, FontStyles.Bold);
        SetCentered(recordTitle.rectTransform, new Vector2(850f, 86f), new Vector2(0f, 585f));
        TMP_Text recordDescription = CreateText(recordSpendPanel.transform, "Description",
            "금액과 카테고리를 입력하면 Preview 사용액에만 반영됩니다.",
            context.Font, 28f, MutedBrown, FontStyles.Bold);
        recordDescription.enableWordWrapping = true;
        SetCentered(recordDescription.rectTransform, new Vector2(850f, 100f), new Vector2(0f, 480f));
        Image recordFormCard = CreateImage(recordSpendPanel.transform, "RecordFormCard", context.CardSprite, Color.white);
        SetCentered(recordFormCard.rectTransform, new Vector2(890f, 610f), new Vector2(0f, 120f));
        TMP_Text amountLabel = CreateText(recordFormCard.transform, "AmountLabel", "소비 금액",
            context.Font, 31f, Brown, FontStyles.Bold);
        amountLabel.alignment = TextAlignmentOptions.MidlineLeft;
        SetCentered(amountLabel.rectTransform, new Vector2(740f, 56f), new Vector2(0f, 220f));
        TMP_InputField recordAmount = CreatePreviewInputField(recordFormCard.transform, "AmountInput",
            "예: 12000", true, context, new Vector2(-40f, 145f), new Vector2(660f, 112f));
        TMP_Text recordWon = CreateText(recordFormCard.transform, "WonLabel", "원",
            context.Font, 32f, Brown, FontStyles.Bold);
        SetCentered(recordWon.rectTransform, new Vector2(70f, 80f), new Vector2(355f, 145f));
        TMP_Text categoryLabel = CreateText(recordFormCard.transform, "CategoryLabel", "소비 카테고리",
            context.Font, 31f, Brown, FontStyles.Bold);
        categoryLabel.alignment = TextAlignmentOptions.MidlineLeft;
        SetCentered(categoryLabel.rectTransform, new Vector2(740f, 56f), new Vector2(0f, 55f));
        TMP_Dropdown recordCategory = CreatePreviewDropdown(recordFormCard.transform, context,
            new Vector2(0f, -30f), new Vector2(740f, 112f), context.RecordCategoryDropdown.options);
        TMP_Text recordRule = CreateText(recordFormCard.transform, "RuleText",
            "0원보다 큰 숫자와 카테고리 선택이 필요합니다.",
            context.Font, 27f, MutedBrown, FontStyles.Bold);
        SetCentered(recordRule.rectTransform, new Vector2(760f, 70f), new Vector2(0f, -155f));
        TMP_Text recordNotice = CreateText(recordSpendPanel.transform, "RecordNotice",
            "금액과 카테고리를 선택해주세요.", context.Font, 28f, MutedBrown, FontStyles.Bold);
        recordNotice.enableWordWrapping = true;
        SetCentered(recordNotice.rectTransform, new Vector2(840f, 95f), new Vector2(0f, -265f));
        Button recordBack = CreateButton(recordSpendPanel.transform, "BackButton", "뒤로 가기",
            context.CreamButtonSprite, context.Font, new Vector2(-225f, -410f), new Vector2(390f, 108f));
        Button recordSave = CreateButton(recordSpendPanel.transform, "SaveButton", "소비 저장",
            context.GoldButtonSprite, context.Font, new Vector2(225f, -410f), new Vector2(390f, 108f));
        recordSpendPanel.gameObject.SetActive(false);

        Image historyPanel = CreateImage(background.transform, "SpendHistoryPanel", context.CardSprite, CreamLight);
        SetCentered(historyPanel.rectTransform, new Vector2(1020f, 1650f), new Vector2(0f, -5f));
        TMP_Text historyTitle = CreateText(historyPanel.transform, "Title", "소비 내역",
            context.Font, 50f, Brown, FontStyles.Bold);
        SetCentered(historyTitle.rectTransform, new Vector2(850f, 80f), new Vector2(0f, 705f));
        TMP_Text historyDescription = CreateText(historyPanel.transform, "Description",
            "거래별 금액과 날짜를 확인하고 예외 상태를 바꿔보세요.",
            context.Font, 27f, MutedBrown, FontStyles.Bold);
        SetCentered(historyDescription.rectTransform, new Vector2(850f, 64f), new Vector2(0f, 635f));

        Image historyListCard = CreateImage(historyPanel.transform, "TransactionListCard", context.CardSprite, Color.white);
        SetCentered(historyListCard.rectTransform, new Vector2(900f, 1170f), new Vector2(0f, 5f));
        Image historyViewport = CreateImage(historyListCard.transform, "Viewport", null, new Color32(255, 255, 255, 4));
        historyViewport.raycastTarget = true;
        SetStretch(historyViewport.rectTransform, new Vector2(24f, 24f), new Vector2(-24f, -24f));
        Mask historyMask = Undo.AddComponent<Mask>(historyViewport.gameObject);
        historyMask.showMaskGraphic = false;
        RectTransform transactionContent = CreateContent(historyViewport.transform);
        VerticalLayoutGroup transactionLayout = transactionContent.GetComponent<VerticalLayoutGroup>();
        transactionLayout.padding = new RectOffset(8, 8, 8, 16);
        transactionLayout.spacing = 16f;
        ScrollRect transactionScroll = Undo.AddComponent<ScrollRect>(historyListCard.gameObject);
        transactionScroll.viewport = historyViewport.rectTransform;
        transactionScroll.content = transactionContent;
        transactionScroll.horizontal = false;
        transactionScroll.vertical = true;
        transactionScroll.movementType = ScrollRect.MovementType.Elastic;
        transactionScroll.inertia = true;
        transactionScroll.scrollSensitivity = 35f;

        Image transactionTemplate = CreateImage(transactionContent, "TransactionItemTemplate", context.CardSprite, CreamLight);
        AddLayout(transactionTemplate.gameObject, 190f);
        TMP_Text categoryText = CreateText(transactionTemplate.transform, "CategoryText", "식비",
            context.Font, 31f, Brown, FontStyles.Bold);
        categoryText.alignment = TextAlignmentOptions.MidlineLeft;
        SetCentered(categoryText.rectTransform, new Vector2(330f, 54f), new Vector2(-225f, 45f));
        TMP_Text amountText = CreateText(transactionTemplate.transform, "AmountText", "-12,800원",
            context.Font, 36f, Brown, FontStyles.Bold);
        amountText.alignment = TextAlignmentOptions.MidlineRight;
        SetCentered(amountText.rectTransform, new Vector2(390f, 60f), new Vector2(205f, 45f));
        TMP_Text dateText = CreateText(transactionTemplate.transform, "DateText", "2026.10.20",
            context.Font, 25f, MutedBrown, FontStyles.Bold);
        dateText.alignment = TextAlignmentOptions.MidlineLeft;
        SetCentered(dateText.rectTransform, new Vector2(330f, 50f), new Vector2(-225f, -45f));
        Image toggleImage = CreateImage(transactionTemplate.transform, "ExceptionToggle", context.CreamButtonSprite, CreamLight);
        SetCentered(toggleImage.rectTransform, new Vector2(300f, 72f), new Vector2(245f, -45f));
        Toggle exceptionToggle = Undo.AddComponent<Toggle>(toggleImage.gameObject);
        exceptionToggle.targetGraphic = toggleImage;
        TMP_Text toggleLabel = CreateText(toggleImage.transform, "Label", "예외 처리",
            context.Font, 25f, Brown, FontStyles.Bold);
        toggleLabel.raycastTarget = false;
        SetStretch(toggleLabel.rectTransform, new Vector2(12f, 6f), new Vector2(-12f, -6f));
        transactionTemplate.gameObject.SetActive(false);

        TMP_Text historyNotice = CreateText(historyPanel.transform, "HistoryNotice",
            "가상 거래만 표시하며 서버와 실제 예산 계산은 호출하지 않습니다.",
            context.Font, 24f, MutedBrown, FontStyles.Bold);
        SetCentered(historyNotice.rectTransform, new Vector2(850f, 70f), new Vector2(0f, -610f));
        Button historyBack = CreateButton(historyPanel.transform, "BackButton", "뒤로 가기",
            context.CreamButtonSprite, context.Font, new Vector2(0f, -715f), new Vector2(460f, 104f));
        historyPanel.gameObject.SetActive(false);

        Image weeklyReportPanel = CreateImage(background.transform, "WeeklyReportPanel", context.CardSprite, CreamLight);
        SetCentered(weeklyReportPanel.rectTransform, new Vector2(1020f, 1650f), new Vector2(0f, -5f));
        Button weeklyReportBack = CreateButton(weeklyReportPanel.transform, "BackButton", "← 소비 메인",
            context.CreamButtonSprite, context.Font, new Vector2(-330f, 715f), new Vector2(300f, 88f));
        TMP_Text reportTitle = CreateText(weeklyReportPanel.transform, "Title", "AI 주간 원정 리포트",
            context.Font, 43f, Brown, FontStyles.Bold);
        SetCentered(reportTitle.rectTransform, new Vector2(590f, 70f), new Vector2(145f, 715f));
        TMP_Text reportWeek = CreateText(weeklyReportPanel.transform, "WeekText", "2026.10.12 ~ 2026.10.18",
            context.Font, 27f, Gold, FontStyles.Bold);
        SetCentered(reportWeek.rectTransform, new Vector2(850f, 52f), new Vector2(0f, 642f));

        Image reportViewport = CreateImage(weeklyReportPanel.transform, "Viewport", null, new Color32(255, 255, 255, 4));
        reportViewport.raycastTarget = true;
        SetCentered(reportViewport.rectTransform, new Vector2(930f, 1270f), new Vector2(0f, -55f));
        Mask reportMask = Undo.AddComponent<Mask>(reportViewport.gameObject);
        reportMask.showMaskGraphic = false;
        RectTransform reportContent = CreateContent(reportViewport.transform);
        VerticalLayoutGroup reportLayout = reportContent.GetComponent<VerticalLayoutGroup>();
        reportLayout.padding = new RectOffset(12, 12, 12, 40);
        reportLayout.spacing = 24f;
        ScrollRect reportScroll = Undo.AddComponent<ScrollRect>(weeklyReportPanel.gameObject);
        reportScroll.viewport = reportViewport.rectTransform;
        reportScroll.content = reportContent;
        reportScroll.horizontal = false;
        reportScroll.vertical = true;
        reportScroll.movementType = ScrollRect.MovementType.Elastic;
        reportScroll.inertia = true;
        reportScroll.scrollSensitivity = 38f;

        Image previewHabitCard = CreatePreviewReportCard(reportContent, "HabitSummaryCard", "습관 통계", 300f, context);
        TMP_Text reportHabitAchievement = CreatePreviewReportLine(previewHabitCard.transform, "AchievementText",
            "습관 달성률 82%", 76f, context);
        TMP_Text reportHabitCompleted = CreatePreviewReportLine(previewHabitCard.transform, "CompletedText",
            "완료 18회 / 계획 22회", 76f, context);

        Image previewSpendingCard = CreatePreviewReportCard(reportContent, "SpendingSummaryCard", "소비 통계", 390f, context);
        TMP_Text reportSpendingBudget = CreatePreviewReportLine(previewSpendingCard.transform, "BudgetText",
            "주간 예산 500,000원", 76f, context);
        TMP_Text reportSpendingSpent = CreatePreviewReportLine(previewSpendingCard.transform, "SpentText",
            "사용 금액 320,000원", 76f, context);
        TMP_Text reportSpendingUsage = CreatePreviewReportLine(previewSpendingCard.transform, "UsageText",
            "예산 사용률 64%", 76f, context);

        Image previewAiCard = CreatePreviewReportCard(reportContent, "AIFeedbackCard", "AI 피드백", 1500f, context);
        TMP_Text reportSummary = CreatePreviewFeedback(previewAiCard.transform, "SummaryText", "AI 요약", 260f, context);
        TMP_Text reportGoodPoint = CreatePreviewFeedback(previewAiCard.transform, "GoodPointText", "잘한 점", 260f, context);
        TMP_Text reportImprovement = CreatePreviewFeedback(previewAiCard.transform, "ImprovementText", "개선할 점", 390f, context);
        TMP_Text reportNextWeek = CreatePreviewFeedback(previewAiCard.transform, "NextWeekText", "다음 주 제안", 320f, context);
        weeklyReportPanel.gameObject.SetActive(false);

        Image goalSettingPanel = CreateImage(background.transform, "GoalSettingPanel", context.CardSprite, CreamLight);
        SetCentered(goalSettingPanel.rectTransform, new Vector2(1020f, 1650f), new Vector2(0f, -5f));
        TMP_Text goalSettingTitle = CreateText(goalSettingPanel.transform, "Title", "소비 목표 추가",
            context.Font, 48f, Brown, FontStyles.Bold);
        SetCentered(goalSettingTitle.rectTransform, new Vector2(850f, 76f), new Vector2(0f, 720f));
        TMP_Text goalSettingDescription = CreateText(goalSettingPanel.transform, "Description",
            "목표명과 소비 한도를 입력하고 기간을 선택하세요.", context.Font, 27f, MutedBrown, FontStyles.Bold);
        SetCentered(goalSettingDescription.rectTransform, new Vector2(850f, 64f), new Vector2(0f, 650f));
        Image goalFormCard = CreateImage(goalSettingPanel.transform, "GoalFormCard", context.CardSprite, Color.white);
        SetCentered(goalFormCard.rectTransform, new Vector2(890f, 570f), new Vector2(0f, 325f));
        TMP_Text nameLabel = CreateText(goalFormCard.transform, "GoalNameLabel", "목표명",
            context.Font, 29f, Brown, FontStyles.Bold);
        nameLabel.alignment = TextAlignmentOptions.MidlineLeft;
        SetCentered(nameLabel.rectTransform, new Vector2(720f, 52f), new Vector2(0f, 215f));
        TMP_InputField goalNameInput = CreatePreviewInputField(goalFormCard.transform, "GoalNameInput",
            "예: 배달 음식 줄이기", false, context, new Vector2(0f, 145f), new Vector2(740f, 104f));
        TMP_Text limitLabel = CreateText(goalFormCard.transform, "GoalLimitLabel", "소비 한도",
            context.Font, 29f, Brown, FontStyles.Bold);
        limitLabel.alignment = TextAlignmentOptions.MidlineLeft;
        SetCentered(limitLabel.rectTransform, new Vector2(720f, 52f), new Vector2(0f, 70f));
        TMP_InputField goalLimitInput = CreatePreviewInputField(goalFormCard.transform, "GoalLimitInput",
            "예: 80000", true, context, new Vector2(-40f, 0f), new Vector2(660f, 104f));
        TMP_Text goalWon = CreateText(goalFormCard.transform, "WonLabel", "원",
            context.Font, 30f, Brown, FontStyles.Bold);
        SetCentered(goalWon.rectTransform, new Vector2(70f, 80f), new Vector2(355f, 0f));
        Button daily = CreateButton(goalFormCard.transform, "DailyButton", "Daily", context.CreamButtonSprite,
            context.Font, new Vector2(-205f, -115f), new Vector2(330f, 88f));
        Button weekly = CreateButton(goalFormCard.transform, "WeeklyButton", "Weekly", context.CreamButtonSprite,
            context.Font, new Vector2(205f, -115f), new Vector2(330f, 88f));
        Button addGoal = CreateButton(goalFormCard.transform, "AddGoalButton", "목표 추가", context.GoldButtonSprite,
            context.Font, new Vector2(0f, -220f), new Vector2(520f, 94f));

        Image goalListCard = CreateImage(goalSettingPanel.transform, "GoalPreviewCard", context.CardSprite, Color.white);
        SetCentered(goalListCard.rectTransform, new Vector2(890f, 470f), new Vector2(0f, -245f));
        TMP_Text listTitle = CreateText(goalListCard.transform, "Title", "등록할 목표 목록",
            context.Font, 31f, Brown, FontStyles.Bold);
        SetTopCentered(listTitle.rectTransform, new Vector2(760f, 56f), new Vector2(0f, -24f));
        Image goalViewport = CreateImage(goalListCard.transform, "Viewport", null, new Color32(255, 255, 255, 4));
        SetCentered(goalViewport.rectTransform, new Vector2(780f, 320f), new Vector2(0f, -45f));
        Mask goalMask = Undo.AddComponent<Mask>(goalViewport.gameObject);
        goalMask.showMaskGraphic = false;
        RectTransform goalPreviewContent = CreateContent(goalViewport.transform);
        VerticalLayoutGroup previewLayout = goalPreviewContent.GetComponent<VerticalLayoutGroup>();
        previewLayout.padding = new RectOffset(8, 8, 8, 14);
        previewLayout.spacing = 12f;
        ScrollRect goalPreviewScroll = Undo.AddComponent<ScrollRect>(goalListCard.gameObject);
        goalPreviewScroll.viewport = goalViewport.rectTransform;
        goalPreviewScroll.content = goalPreviewContent;
        goalPreviewScroll.horizontal = false;
        goalPreviewScroll.vertical = true;
        goalPreviewScroll.movementType = ScrollRect.MovementType.Clamped;
        Image goalItemTemplate = CreateImage(goalPreviewContent, "GoalPreviewItemTemplate", context.CardSprite, Color.white);
        TMP_Text goalItemText = CreateText(goalItemTemplate.transform, "Text",
            "목표명\n80,000원 이하 · Daily · +300 G", context.Font, 27f, Brown, FontStyles.Bold);
        goalItemText.alignment = TextAlignmentOptions.MidlineLeft;
        SetStretch(goalItemText.rectTransform, new Vector2(24f, 12f), new Vector2(-24f, -12f));
        AddLayout(goalItemTemplate.gameObject, 112f);
        goalItemTemplate.gameObject.SetActive(false);
        TMP_Text goalSettingNotice = CreateText(goalSettingPanel.transform, "GoalSettingNotice",
            "가상 소비 목표를 추가하거나 건너뛸 수 있습니다.", context.Font, 25f, MutedBrown, FontStyles.Bold);
        SetCentered(goalSettingNotice.rectTransform, new Vector2(850f, 70f), new Vector2(0f, -520f));
        Button skipGoal = CreateButton(goalSettingPanel.transform, "SkipButton", "Skip", context.CreamButtonSprite,
            context.Font, new Vector2(-220f, -650f), new Vector2(390f, 104f));
        Button completeGoal = CreateButton(goalSettingPanel.transform, "CompleteButton", "Complete", context.GoldButtonSprite,
            context.Font, new Vector2(220f, -650f), new Vector2(390f, 104f));
        goalSettingPanel.gameObject.SetActive(false);

        SpendingReportPreviewController controller = Undo.AddComponent<SpendingReportPreviewController>(root);
        SerializedObject serialized = new SerializedObject(controller);
        SetReference(serialized, "budgetText", budgetText);
        SetReference(serialized, "usedText", usedText);
        SetReference(serialized, "budgetSlider", budgetSlider);
        SetReference(serialized, "goalStatusText", goalStatus);
        SetReference(serialized, "rewardStatusText", rewardStatus);
        SetReference(serialized, "localNoticeText", localNotice);
        SetReference(serialized, "budgetSettingButton", budgetSetting);
        SetReference(serialized, "recordButton", record);
        SetReference(serialized, "historyButton", history);
        SetReference(serialized, "reportButton", report);
        SetReference(serialized, "toggleBudgetButton", toggleBudget);
        SetReference(serialized, "toggleGoalButton", toggleGoal);
        SetReference(serialized, "claimRewardButton", claim);
        SetReference(serialized, "mainScroll", scroll);
        SetReference(serialized, "mainPanel", scrollPanel.gameObject);
        SetReference(serialized, "budgetSettingPanel", budgetSettingPanel.gameObject);
        SetReference(serialized, "budgetInput", budgetInput);
        SetReference(serialized, "budgetSaveButton", budgetSave);
        SetReference(serialized, "budgetBackButton", budgetBack);
        SetReference(serialized, "budgetSettingNoticeText", settingNotice);
        SetReference(serialized, "recordSpendPanel", recordSpendPanel.gameObject);
        SetReference(serialized, "recordAmountInput", recordAmount);
        SetReference(serialized, "recordCategoryDropdown", recordCategory);
        SetReference(serialized, "recordSaveButton", recordSave);
        SetReference(serialized, "recordBackButton", recordBack);
        SetReference(serialized, "recordNoticeText", recordNotice);
        SetReference(serialized, "openGoalSettingButton", openGoalSetting);
        SetReference(serialized, "goalSettingPanel", goalSettingPanel.gameObject);
        SetReference(serialized, "goalNameInput", goalNameInput);
        SetReference(serialized, "goalLimitInput", goalLimitInput);
        SetReference(serialized, "dailyButton", daily);
        SetReference(serialized, "weeklyButton", weekly);
        SetReference(serialized, "addGoalButton", addGoal);
        SetReference(serialized, "skipGoalButton", skipGoal);
        SetReference(serialized, "completeGoalButton", completeGoal);
        SetReference(serialized, "goalSettingNoticeText", goalSettingNotice);
        SetReference(serialized, "goalPreviewContent", goalPreviewContent);
        SetReference(serialized, "goalPreviewScroll", goalPreviewScroll);
        SetReference(serialized, "goalPreviewItemTemplate", goalItemTemplate.gameObject);
        SetReference(serialized, "spendHistoryPanel", historyPanel.gameObject);
        SetReference(serialized, "transactionPreviewContent", transactionContent);
        SetReference(serialized, "transactionPreviewScroll", transactionScroll);
        SetReference(serialized, "transactionPreviewItemTemplate", transactionTemplate.gameObject);
        SetReference(serialized, "historyBackButton", historyBack);
        SetReference(serialized, "historyNoticeText", historyNotice);
        SetReference(serialized, "weeklyReportPanel", weeklyReportPanel.gameObject);
        SetReference(serialized, "weeklyReportScroll", reportScroll);
        SetReference(serialized, "weeklyReportBackButton", weeklyReportBack);
        SetReference(serialized, "reportWeekText", reportWeek);
        SetReference(serialized, "reportHabitAchievementText", reportHabitAchievement);
        SetReference(serialized, "reportHabitCompletedText", reportHabitCompleted);
        SetReference(serialized, "reportSpendingBudgetText", reportSpendingBudget);
        SetReference(serialized, "reportSpendingSpentText", reportSpendingSpent);
        SetReference(serialized, "reportSpendingUsageText", reportSpendingUsage);
        SetReference(serialized, "reportAiSummaryText", reportSummary);
        SetReference(serialized, "reportAiGoodPointText", reportGoodPoint);
        SetReference(serialized, "reportAiImprovementText", reportImprovement);
        SetReference(serialized, "reportAiNextWeekText", reportNextWeek);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        float preferredHeight = LayoutUtility.GetPreferredHeight(content);
        if (preferredHeight > 0f) content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
        root.SetActive(false);
        Dirty(root);
    }

    private static Image CreatePreviewCard(Transform parent, string name, float height, Context context)
    {
        Image card = CreateImage(parent, name, context.CardSprite, Color.white);
        AddLayout(card.gameObject, height);
        return card;
    }

    private static Image CreatePreviewReportCard(Transform parent, string name, string title, float minimumHeight,
        Context context)
    {
        Image card = CreateImage(parent, name, context.CardSprite, Color.white);
        LayoutElement element = GetOrAdd<LayoutElement>(card.gameObject);
        element.minHeight = minimumHeight;
        element.preferredHeight = -1f;
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(card.gameObject);
        layout.padding = new RectOffset(30, 30, 28, 34);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        TMP_Text heading = CreateText(card.transform, "SectionTitle", title, context.Font, 32f, Brown, FontStyles.Bold);
        heading.alignment = TextAlignmentOptions.MidlineLeft;
        AddLayout(heading.gameObject, 58f);
        return card;
    }

    private static TMP_Text CreatePreviewReportLine(Transform parent, string name, string value, float height,
        Context context)
    {
        Image background = CreateImage(parent, name + "Card", context.CreamButtonSprite, CreamLight);
        background.raycastTarget = false;
        AddLayout(background.gameObject, height);
        TMP_Text text = CreateText(background.transform, name, value, context.Font, 28f, Brown, FontStyles.Bold);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.margin = Vector4.zero;
        text.enableWordWrapping = true;
        SetStretch(text.rectTransform, new Vector2(24f, 8f), new Vector2(-24f, -8f));
        return text;
    }

    private static TMP_Text CreatePreviewFeedback(Transform parent, string name, string value, float minimumHeight,
        Context context)
    {
        Image background = CreateImage(parent, name + "Card", context.CreamButtonSprite, CreamLight);
        background.raycastTarget = false;
        LayoutElement element = GetOrAdd<LayoutElement>(background.gameObject);
        element.minHeight = minimumHeight;
        element.preferredHeight = -1f;
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(background.gameObject);
        layout.padding = new RectOffset(28, 28, 22, 22);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(background.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text text = CreateText(background.transform, name, value, context.Font, 27f, Brown, FontStyles.Normal);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.margin = Vector4.zero;
        text.lineSpacing = 8f;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private static void CreateCardTitle(Transform parent, string value, Context context)
    {
        TMP_Text title = CreateText(parent, "Title", value, context.Font, 36f, Brown, FontStyles.Bold);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.sizeDelta = new Vector2(820f, 60f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -22f);
    }

    private static Slider CreateSlider(Transform parent, Context context)
    {
        GameObject sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        Undo.RegisterCreatedObjectUndo(sliderObject, "Create spending preview slider");
        sliderObject.transform.SetParent(parent, false);
        RectTransform rect = sliderObject.GetComponent<RectTransform>();
        SetCentered(rect, new Vector2(820f, 42f), new Vector2(0f, -45f));
        Image track = CreateImage(sliderObject.transform, "Background", context.CreamButtonSprite, Cream);
        SetStretch(track.rectTransform, Vector2.zero, Vector2.zero);
        Image fillArea = CreateImage(sliderObject.transform, "Fill Area", null, Color.clear);
        SetStretch(fillArea.rectTransform, new Vector2(8f, 8f), new Vector2(-8f, -8f));
        Image fill = CreateImage(fillArea.transform, "Fill", context.GoldButtonSprite, Gold);
        SetStretch(fill.rectTransform, Vector2.zero, Vector2.zero);
        Slider slider = sliderObject.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.targetGraphic = track;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.64f;
        slider.interactable = false;
        return slider;
    }

    private static RectTransform CreateContent(Transform parent)
    {
        GameObject item = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        Undo.RegisterCreatedObjectUndo(item, "Create Spending preview content");
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = item.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 32);
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
        if (!context.IsValid) return context.Errors.ToList();
        if (!context.Scroll.vertical || context.Scroll.horizontal)
            errors.Add("SpendScrollView가 세로 전용 ScrollRect가 아닙니다.");
        if (context.Content == null || context.Content.GetComponent<VerticalLayoutGroup>() == null ||
            context.Content.GetComponent<ContentSizeFitter>() == null)
            errors.Add("Spend Content의 VerticalLayoutGroup/ContentSizeFitter가 누락되었습니다.");
        foreach (RectTransform card in new[] { context.BudgetCard, context.RecordCard, context.GoldListCard, context.RewardCard })
            if (card == null || card.GetComponent<LayoutElement>() == null)
                errors.Add("Spend 메인 카드 LayoutElement가 누락되었습니다.");
        if (context.BudgetText.font != context.Font || context.UsedText.font != context.Font)
            errors.Add("예산 텍스트에 NotoSansKR TMP Font가 적용되지 않았습니다.");
        if (context.BudgetSettingCard.GetComponent<Image>()?.sprite != context.CardSprite)
            errors.Add("BudgetSetting 예산 카드에 RPG 패널 Sprite가 적용되지 않았습니다.");
        if (context.BudgetInput.textComponent == null || context.BudgetInput.textComponent.font != context.Font)
            errors.Add("BudgetSetting 입력 텍스트에 NotoSansKR TMP Font가 적용되지 않았습니다.");
        if (context.BudgetSaveButton == null || context.BudgetBackButton == null)
            errors.Add("BudgetSetting 저장/뒤로 가기 버튼 참조가 유지되지 않았습니다.");
        if (!context.RecordFormScroll.vertical || context.RecordFormScroll.horizontal)
            errors.Add("RecordSpendAdd Form ScrollRect가 세로 전용이 아닙니다.");
        if (context.RecordAmountInput.textComponent == null || context.RecordAmountInput.textComponent.font != context.Font)
            errors.Add("RecordSpendAdd 금액 입력에 NotoSansKR TMP Font가 적용되지 않았습니다.");
        if (context.RecordCategoryDropdown.captionText == null ||
            context.RecordCategoryDropdown.captionText.font != context.Font || context.RecordSaveButton == null ||
            context.RecordBackButton == null)
            errors.Add("RecordSpendAdd Dropdown/저장/뒤로 가기 참조 또는 스타일이 유지되지 않았습니다.");
        ScrollRect recordDropdownScroll = context.RecordCategoryDropdown.template != null
            ? context.RecordCategoryDropdown.template.GetComponentInChildren<ScrollRect>(true)
            : null;
        if (recordDropdownScroll == null || !recordDropdownScroll.vertical || recordDropdownScroll.horizontal ||
            recordDropdownScroll.viewport == null || recordDropdownScroll.content == null ||
            recordDropdownScroll.scrollSensitivity < 20f ||
            recordDropdownScroll.viewport.GetComponent<Graphic>()?.raycastTarget != true)
            errors.Add("RecordSpendAdd Dropdown의 휠/드래그 ScrollRect 구성이 누락되었습니다.");
        if (!context.GoalFormScroll.vertical || context.GoalFormScroll.horizontal)
            errors.Add("SpendAdd_1 전체 Form ScrollRect가 세로 전용이 아닙니다.");
        if (!context.GoalPreviewScroll.vertical || context.GoalPreviewScroll.horizontal ||
            context.GoalPreviewContent.GetComponent<VerticalLayoutGroup>() == null ||
            context.GoalPreviewContent.GetComponent<ContentSizeFitter>() == null)
            errors.Add("SpendAdd_1 Preview 목록의 세로 ScrollRect/Layout 구성이 누락되었습니다.");
        if (context.GoalNameInput.textComponent == null || context.GoalNameInput.textComponent.font != context.Font ||
            context.GoalLimitInput.textComponent == null || context.GoalLimitInput.textComponent.font != context.Font)
            errors.Add("SpendAdd_1 입력 텍스트에 NotoSansKR TMP Font가 적용되지 않았습니다.");
        if (!context.TransactionScroll.vertical || context.TransactionScroll.horizontal ||
            context.TransactionContent.GetComponent<VerticalLayoutGroup>() == null ||
            context.TransactionContent.GetComponent<ContentSizeFitter>() == null ||
            context.TransactionScroll.scrollSensitivity < 20f)
            errors.Add("SpendHistory 거래 목록의 세로 휠/드래그 ScrollRect 구성이 누락되었습니다.");
        if (context.HistoryTitle.font != context.Font || context.HistoryBackButton == null)
            errors.Add("SpendHistory 제목 Font 또는 뒤로 가기 버튼 참조가 유지되지 않았습니다.");
        if (!context.ReportScroll.vertical || context.ReportScroll.horizontal || context.ReportScroll.scrollSensitivity < 20f ||
            context.ReportContent.GetComponent<VerticalLayoutGroup>() == null ||
            context.ReportContent.GetComponent<ContentSizeFitter>() == null)
            errors.Add("WeeklyReportPanel의 세로 휠/드래그 ScrollRect와 자동 높이 Layout 구성이 누락되었습니다.");
        if (context.ReportBackButton == null ||
            context.ReportBackButton.onClick.GetPersistentEventCount() == 0 ||
            !Enumerable.Range(0, context.ReportBackButton.onClick.GetPersistentEventCount())
                .Any(index => context.ReportBackButton.onClick.GetPersistentMethodName(index) == "BackFromWeeklyReport"))
            errors.Add("WeeklyReportPanel Back 버튼의 BackFromWeeklyReport UnityEvent가 유지되지 않았습니다.");
        foreach (TMP_Text text in new[] { context.ReportWeekText, context.HabitAchievementText,
                     context.HabitCompletedText, context.SpendingBudgetText, context.SpendingSpentText,
                     context.SpendingUsageText, context.AiSummaryText, context.AiGoodPointText,
                     context.AiImprovementText, context.AiNextWeekText })
            if (text == null || text.font != context.Font || !text.enableWordWrapping)
                errors.Add("WeeklyReportPanel 텍스트의 NotoSansKR/자동 줄바꿈 설정이 누락되었습니다.");
        foreach (TMP_Text text in new[] { context.HabitAchievementText, context.HabitCompletedText,
                     context.SpendingBudgetText, context.SpendingSpentText, context.SpendingUsageText })
            if (text == null || text.GetComponent<Image>() != null || text.transform.parent == null ||
                text.transform.parent.GetComponent<Image>() == null ||
                text.transform.parent.GetComponent<LayoutElement>() == null)
                errors.Add("WeeklyReportPanel 통계 텍스트와 별도 배경 카드 구성이 올바르지 않습니다.");
        foreach (TMP_Text text in new[] { context.AiSummaryText, context.AiGoodPointText,
                     context.AiImprovementText, context.AiNextWeekText })
            if (text == null || text.GetComponent<Image>() != null || text.transform.parent == null ||
                text.transform.parent.GetComponent<Image>() == null ||
                text.transform.parent.GetComponent<ContentSizeFitter>() == null ||
                text.transform.parent.GetComponent<LayoutElement>() == null)
                errors.Add("WeeklyReportPanel AI 피드백 카드의 자동 높이 구성이 누락되었습니다.");
        GameObject preview = FindRoot(context.Scene, PreviewRootName);
        if (requirePreview && (preview == null || preview.GetComponent<SpendingReportPreviewController>() == null))
            errors.Add("J Preview Root/Controller가 없습니다.");
        if (preview != null)
        {
            RectTransform content = preview.transform.Find("PreviewBackground/MainPanel/Viewport/Content") as RectTransform;
            Transform budgetSettingPanel = preview.transform.Find("PreviewBackground/BudgetSettingPanel");
            Transform recordSpendPanel = preview.transform.Find("PreviewBackground/RecordSpendPanel");
            Transform goalSettingPanel = preview.transform.Find("PreviewBackground/GoalSettingPanel");
            Transform spendHistoryPanel = preview.transform.Find("PreviewBackground/SpendHistoryPanel");
            Transform weeklyReportPanel = preview.transform.Find("PreviewBackground/WeeklyReportPanel");
            if (content == null || content.Find("BudgetCard") == null || content.Find("RecordCard") == null ||
                content.Find("GoldListCard") == null || content.Find("RewardCard") == null || content.Find("ReportCard") == null)
                errors.Add("J Preview 메인 카드 구조가 누락되었습니다.");
            if (budgetSettingPanel == null || budgetSettingPanel.Find("BudgetCard/BudgetInput") == null ||
                budgetSettingPanel.Find("SaveButton") == null || budgetSettingPanel.Find("BackButton") == null)
                errors.Add("J Preview BudgetSetting 입력/저장/뒤로 가기 구조가 누락되었습니다.");
            if (recordSpendPanel == null || recordSpendPanel.Find("RecordFormCard/AmountInput") == null ||
                recordSpendPanel.Find("RecordFormCard/CategoryDropdown") == null ||
                recordSpendPanel.Find("SaveButton") == null || recordSpendPanel.Find("BackButton") == null)
                errors.Add("J Preview RecordSpendAdd 입력/Dropdown/저장/뒤로 가기 구조가 누락되었습니다.");
            if (goalSettingPanel == null || goalSettingPanel.Find("GoalFormCard/GoalNameInput") == null ||
                goalSettingPanel.Find("GoalFormCard/GoalLimitInput") == null ||
                goalSettingPanel.Find("GoalFormCard/AddGoalButton") == null ||
                goalSettingPanel.Find("GoalPreviewCard/Viewport/Content") == null ||
                goalSettingPanel.Find("SkipButton") == null || goalSettingPanel.Find("CompleteButton") == null)
                errors.Add("J Preview SpendAdd 입력/선택/목록/완료 구조가 누락되었습니다.");
            if (spendHistoryPanel == null ||
                spendHistoryPanel.Find("TransactionListCard/Viewport/Content/TransactionItemTemplate/CategoryText") == null ||
                spendHistoryPanel.Find("TransactionListCard/Viewport/Content/TransactionItemTemplate/AmountText") == null ||
                spendHistoryPanel.Find("TransactionListCard/Viewport/Content/TransactionItemTemplate/DateText") == null ||
                spendHistoryPanel.Find("TransactionListCard/Viewport/Content/TransactionItemTemplate/ExceptionToggle") == null ||
                spendHistoryPanel.Find("BackButton") == null)
                errors.Add("J Preview SpendHistory 거래 카드/Toggle/뒤로 가기 구조가 누락되었습니다.");
            if (weeklyReportPanel == null || weeklyReportPanel.Find("BackButton") == null ||
                weeklyReportPanel.Find("Viewport/Content/HabitSummaryCard") == null ||
                weeklyReportPanel.Find("Viewport/Content/SpendingSummaryCard") == null ||
                weeklyReportPanel.Find("Viewport/Content/AIFeedbackCard/SummaryTextCard/SummaryText") == null ||
                weeklyReportPanel.Find("Viewport/Content/AIFeedbackCard/GoodPointTextCard/GoodPointText") == null ||
                weeklyReportPanel.Find("Viewport/Content/AIFeedbackCard/ImprovementTextCard/ImprovementText") == null ||
                weeklyReportPanel.Find("Viewport/Content/AIFeedbackCard/NextWeekTextCard/NextWeekText") == null)
                errors.Add("J Preview WeeklyReport 통계/AI 카드/뒤로 가기 구조가 누락되었습니다.");
            else
            {
                RectTransform reportContent = weeklyReportPanel.Find("Viewport/Content") as RectTransform;
                ScrollRect reportScroll = weeklyReportPanel.GetComponent<ScrollRect>();
                if (reportContent == null || reportScroll == null || !reportScroll.vertical || reportScroll.horizontal ||
                    reportContent.GetComponent<VerticalLayoutGroup>() == null ||
                    reportContent.GetComponent<ContentSizeFitter>() == null)
                    errors.Add("J Preview WeeklyReport의 세로 ScrollRect/자동 높이 Layout이 누락되었습니다.");
            }
        }
        return errors;
    }

    private static void IsolateScene(Scene scene, GameObject previewRoot, SpendingReportPreviewController controller)
    {
        GameObject[] objects = scene.GetRootGameObjects()
            .Where(root => root != previewRoot && !IsPreviewRoot(root) && root.activeSelf &&
                           root.GetComponent<EventSystem>() == null &&
                           root.GetComponentInChildren<Camera>(true) == null &&
                           root.GetComponentInChildren<AudioListener>(true) == null)
            .OrderBy(root => root.GetInstanceID()).ToArray();
        bool[] objectStates = objects.Select(item => item.activeSelf).ToArray();
        SerializedObject serialized = new SerializedObject(controller);
        SetArray(serialized, "isolatedGameObjects", objects);
        SetBoolArray(serialized, "isolatedGameObjectActiveStates", objectStates);
        SetArray(serialized, "isolatedBehaviours", Array.Empty<Behaviour>());
        SetBoolArray(serialized, "isolatedBehaviourEnabledStates", Array.Empty<bool>());
        serialized.ApplyModifiedProperties();
        Dirty(controller);
        foreach (GameObject target in objects) SetActive(target, false, "Isolate scene object for Spending preview");
    }

    public static void RestoreSpendingPreviewState()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) return;
        GameObject root = FindRoot(scene, PreviewRootName);
        SpendingReportPreviewController controller =
            root != null ? root.GetComponent<SpendingReportPreviewController>() : null;
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
                    SetActive(target, states.GetArrayElementAtIndex(index).boolValue, "Restore Spending preview state");
            }
        }
        ClearSnapshot(controller);
        SetActive(root, false, "Restore Spending preview root");
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void RestoreOtherPreviewStates()
    {
        PvpRankingUiRenewalEditor.RestorePvpPreviewState();
        ShopGachaUiRenewalEditor.RestoreShopPreviewState();
        InventoryMainUiRenewalEditor.RestoreInventoryPreviewState();
        CharacterMainUiRenewalEditor.RestoreCharacterPreviewState();
        PhotoVerificationPopupUiRenewalEditor.RestorePhotoPreviewState();
        AchievedAmountOverlayUiRenewalEditor.RestoreNumberPreviewState();
        HabitGoalFormUiRenewalEditor.RestoreHabitMainPreviewState();
    }

    private static bool HasSnapshot(SpendingReportPreviewController controller)
    {
        SerializedProperty objects = new SerializedObject(controller).FindProperty("isolatedGameObjects");
        return objects != null && objects.arraySize > 0;
    }

    private static void ClearSnapshot(SpendingReportPreviewController controller)
    {
        SerializedObject serialized = new SerializedObject(controller);
        foreach (string name in new[]
                 { "isolatedBehaviours", "isolatedBehaviourEnabledStates", "isolatedGameObjects", "isolatedGameObjectActiveStates" })
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
            RestoreSpendingPreviewState();
    }

    private static void RestoreBeforeSceneSave(Scene scene, string path)
    {
        if (scene.path == ScenePath && scene == SceneManager.GetActiveScene()) RestoreSpendingPreviewState();
    }

    private static bool IsPreviewRoot(GameObject root)
    {
        return root != null && (root.name == PreviewRootName || OtherPreviewRoots.Contains(root.name));
    }

    private static T GetUnique<T>(Transform root, string name, List<string> errors) where T : Component
    {
        Transform transform = FindUnique(root, name, errors);
        T component = transform != null ? transform.GetComponent<T>() : null;
        if (transform != null && component == null) errors.Add($"'{name}'에 {typeof(T).Name} 컴포넌트가 없습니다.");
        return component;
    }

    private static Transform FindUnique(Transform root, string name, List<string> errors)
    {
        if (root == null)
        {
            errors.Add($"'{name}' 탐색 기준 Transform이 없습니다.");
            return null;
        }
        Transform[] matches = root.GetComponentsInChildren<Transform>(true).Where(item => item.name == name).ToArray();
        if (matches.Length != 1)
            errors.Add($"'{name}' 오브젝트가 정확히 하나여야 합니다. 현재: {matches.Length}");
        return matches.Length == 1 ? matches[0] : null;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene.IsValid() ? scene.GetRootGameObjects().FirstOrDefault(item => item.name == name) : null;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(item, "Create Spending preview image");
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
        Undo.RegisterCreatedObjectUndo(item, "Create Spending preview text");
        item.transform.SetParent(parent, false);
        TMP_Text text = item.GetComponent<TMP_Text>();
        text.text = value;
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, Sprite sprite,
        TMP_FontAsset font, Vector2 position, Vector2 size)
    {
        Image image = CreateImage(parent, name, sprite, Color.white);
        SetCentered(image.rectTransform, size, position);
        Button button = Undo.AddComponent<Button>(image.gameObject);
        button.targetGraphic = image;
        TMP_Text text = CreateText(image.transform, "Text", label, font, 29f, Brown, FontStyles.Bold);
        text.enableWordWrapping = false;
        SetStretch(text.rectTransform, new Vector2(18f, 10f), new Vector2(-18f, -10f));
        return button;
    }

    private static TMP_InputField CreateInputField(Transform parent, Context context, Vector2 position, Vector2 size)
    {
        Image image = CreateImage(parent, "BudgetInput", context.CreamButtonSprite, CreamLight);
        SetCentered(image.rectTransform, size, position);
        TMP_InputField input = Undo.AddComponent<TMP_InputField>(image.gameObject);
        input.targetGraphic = image;
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.characterLimit = 10;

        GameObject viewport = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        Undo.RegisterCreatedObjectUndo(viewport, "Create BudgetSetting preview input viewport");
        viewport.transform.SetParent(image.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        SetStretch(viewportRect, new Vector2(28f, 12f), new Vector2(-28f, -12f));

        TMP_Text placeholder = CreateText(viewport.transform, "Placeholder", "예: 500000",
            context.Font, 34f, MutedBrown, FontStyles.Normal);
        placeholder.alignment = TextAlignmentOptions.MidlineRight;
        placeholder.enableWordWrapping = false;
        SetStretch(placeholder.rectTransform, Vector2.zero, Vector2.zero);
        TMP_Text inputText = CreateText(viewport.transform, "Text", string.Empty,
            context.Font, 38f, Brown, FontStyles.Bold);
        inputText.alignment = TextAlignmentOptions.MidlineRight;
        inputText.enableWordWrapping = false;
        SetStretch(inputText.rectTransform, Vector2.zero, Vector2.zero);
        input.textViewport = viewportRect;
        input.textComponent = inputText;
        input.placeholder = placeholder;
        return input;
    }

    private static TMP_InputField CreatePreviewInputField(Transform parent, string name, string placeholderValue,
        bool numeric, Context context, Vector2 position, Vector2 size)
    {
        Image image = CreateImage(parent, name, context.CreamButtonSprite, CreamLight);
        SetCentered(image.rectTransform, size, position);
        TMP_InputField input = Undo.AddComponent<TMP_InputField>(image.gameObject);
        input.targetGraphic = image;
        input.contentType = numeric ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.Standard;
        input.characterLimit = numeric ? 10 : 30;
        GameObject viewport = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        Undo.RegisterCreatedObjectUndo(viewport, "Create SpendAdd preview input viewport");
        viewport.transform.SetParent(image.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        SetStretch(viewportRect, new Vector2(24f, 10f), new Vector2(-24f, -10f));
        TMP_Text placeholder = CreateText(viewport.transform, "Placeholder", placeholderValue,
            context.Font, 28f, MutedBrown, FontStyles.Normal);
        placeholder.alignment = numeric ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
        placeholder.enableWordWrapping = false;
        SetStretch(placeholder.rectTransform, Vector2.zero, Vector2.zero);
        TMP_Text inputText = CreateText(viewport.transform, "Text", string.Empty,
            context.Font, 32f, Brown, FontStyles.Bold);
        inputText.alignment = numeric ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
        inputText.enableWordWrapping = false;
        SetStretch(inputText.rectTransform, Vector2.zero, Vector2.zero);
        input.textViewport = viewportRect;
        input.textComponent = inputText;
        input.placeholder = placeholder;
        return input;
    }

    private static TMP_Dropdown CreatePreviewDropdown(Transform parent, Context context, Vector2 position,
        Vector2 size, List<TMP_Dropdown.OptionData> sourceOptions)
    {
        Image image = CreateImage(parent, "CategoryDropdown", context.CreamButtonSprite, CreamLight);
        SetCentered(image.rectTransform, size, position);
        TMP_Dropdown dropdown = Undo.AddComponent<TMP_Dropdown>(image.gameObject);
        dropdown.targetGraphic = image;
        TMP_Text caption = CreateText(image.transform, "Label", string.Empty,
            context.Font, 30f, Brown, FontStyles.Bold);
        caption.alignment = TextAlignmentOptions.MidlineLeft;
        SetStretch(caption.rectTransform, new Vector2(28f, 10f), new Vector2(-80f, -10f));
        TMP_Text arrow = CreateText(image.transform, "Arrow", "▼", context.Font, 26f, Brown, FontStyles.Bold);
        SetCentered(arrow.rectTransform, new Vector2(60f, 60f), new Vector2(size.x * 0.5f - 48f, 0f));
        Image template = CreateImage(image.transform, "Template", context.CardSprite, Color.white);
        SetTopCentered(template.rectTransform, new Vector2(size.x, 360f), new Vector2(0f, -size.y));
        ScrollRect scroll = Undo.AddComponent<ScrollRect>(template.gameObject);
        Image viewport = CreateImage(template.transform, "Viewport", null, Color.white);
        viewport.raycastTarget = true;
        SetStretch(viewport.rectTransform, new Vector2(8f, 8f), new Vector2(-8f, -8f));
        Mask mask = Undo.AddComponent<Mask>(viewport.gameObject);
        mask.showMaskGraphic = false;
        RectTransform content = CreateContent(viewport.transform);
        Image itemImage = CreateImage(content, "Item", null, Color.clear);
        itemImage.raycastTarget = true;
        Toggle item = Undo.AddComponent<Toggle>(itemImage.gameObject);
        item.targetGraphic = itemImage;
        AddLayout(item.gameObject, 72f);
        TMP_Text itemLabel = CreateText(item.transform, "Item Label", "Category",
            context.Font, 31f, Brown, FontStyles.Bold);
        itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
        itemLabel.raycastTarget = false;
        SetStretch(itemLabel.rectTransform, new Vector2(24f, 4f), new Vector2(-24f, -4f));
        scroll.viewport = viewport.rectTransform;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.scrollSensitivity = 32f;
        dropdown.template = template.rectTransform;
        dropdown.captionText = caption;
        dropdown.itemText = itemLabel;
        dropdown.options = sourceOptions.Select(option => new TMP_Dropdown.OptionData(option.text, option.image)).ToList();
        template.gameObject.SetActive(false);
        dropdown.RefreshShownValue();
        return dropdown;
    }

    private static Button FindButtonByLabel(Transform root, string label)
    {
        return root == null ? null : root.GetComponentsInChildren<Button>(true).FirstOrDefault(button =>
        {
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            return text != null && string.Equals(text.text.Trim(), label, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static Button FindButtonByPersistentMethod(Transform root, string methodName)
    {
        return root == null ? null : root.GetComponentsInChildren<Button>(true).FirstOrDefault(button =>
            Enumerable.Range(0, button.onClick.GetPersistentEventCount())
                .Any(index => button.onClick.GetPersistentMethodName(index) == methodName));
    }

    private static void AddLayout(GameObject target, float height)
    {
        LayoutElement element = Undo.AddComponent<LayoutElement>(target);
        element.minHeight = height;
        element.preferredHeight = height;
    }

    private static void SetCentered(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void SetTopCentered(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void SetStretch(RectTransform rect, Vector2 minOffset, Vector2 maxOffset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = minOffset;
        rect.offsetMax = maxOffset;
    }

    private static void SetReference(SerializedObject serialized, string name, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null) throw new InvalidOperationException($"Preview 필드 '{name}'을 찾을 수 없습니다.");
        property.objectReferenceValue = value;
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

    private static void SetActive(GameObject target, bool active, string undoName)
    {
        if (target == null || target.activeSelf == active) return;
        Undo.RecordObject(target, undoName);
        target.SetActive(active);
        Dirty(target);
    }

    private static void Record(UnityEngine.Object target, string name)
    {
        if (target != null) Undo.RecordObject(target, name);
    }

    private static void Dirty(UnityEngine.Object target)
    {
        if (target != null) EditorUtility.SetDirty(target);
    }

    private sealed class Context
    {
        public Scene Scene;
        public GameObject Canvas;
        public GameObject Spend;
        public TMP_FontAsset Font;
        public Sprite CardSprite;
        public Sprite CreamButtonSprite;
        public Sprite GoldButtonSprite;
        public ScrollRect Scroll;
        public RectTransform Content;
        public RectTransform RecordCard;
        public RectTransform GoldListCard;
        public RectTransform RewardCard;
        public RectTransform BudgetCard;
        public ScrollRect GoalScroll;
        public Button RecordButton;
        public Button HistoryButton;
        public Button ClaimButton;
        public Button BudgetSettingButton;
        public TMP_Text BudgetText;
        public TMP_Text UsedText;
        public Slider BudgetSlider;
        public GameObject BudgetSetting;
        public BudgetSettingManager BudgetSettingManager;
        public RectTransform BudgetSettingContent;
        public RectTransform BudgetSettingCard;
        public TMP_InputField BudgetInput;
        public Button BudgetSaveButton;
        public Button BudgetBackButton;
        public TMP_Text BudgetSettingTitle;
        public TMP_Text BudgetDescription;
        public TMP_Text BudgetDescriptionSecondary;
        public GameObject RecordSpendAdd;
        public RecordSpendManager RecordSpendManager;
        public ScrollRect RecordFormScroll;
        public TMP_InputField RecordAmountInput;
        public TMP_Dropdown RecordCategoryDropdown;
        public Button RecordSaveButton;
        public Button RecordBackButton;
        public GameObject SpendAdd;
        public SpendListManager SpendListManager;
        public SpendCycleManager SpendCycleManager;
        public ScrollRect GoalFormScroll;
        public ScrollRect GoalPreviewScroll;
        public TMP_InputField GoalNameInput;
        public TMP_InputField GoalLimitInput;
        public Transform GoalPreviewContent;
        public Button DailyButton;
        public Button WeeklyButton;
        public Button AddGoalButton;
        public Button SkipButton;
        public Button CompleteButton;
        public GameObject SpendHistory;
        public SpendTransactionManager TransactionManager;
        public ScrollRect TransactionScroll;
        public RectTransform TransactionContent;
        public GameObject TransactionItemPrefab;
        public TMP_Text HistoryTitle;
        public Button HistoryBackButton;
        public GameObject WeeklyReportPanel;
        public WeeklyReportManager WeeklyReportManager;
        public ScrollRect ReportScroll;
        public RectTransform ReportContent;
        public RectTransform HabitSummaryCard;
        public RectTransform SpendingSummaryCard;
        public RectTransform AiFeedbackCard;
        public Button ReportBackButton;
        public TMP_Text ReportWeekText;
        public TMP_Text ReportTitle;
        public TMP_Text HabitAchievementText;
        public TMP_Text HabitCompletedText;
        public TMP_Text SpendingBudgetText;
        public TMP_Text SpendingSpentText;
        public TMP_Text SpendingUsageText;
        public TMP_Text AiSummaryText;
        public TMP_Text AiGoodPointText;
        public TMP_Text AiImprovementText;
        public TMP_Text AiNextWeekText;
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }
}
