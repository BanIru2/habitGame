using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SpendingBudgetIntegrationEditor
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
    private const string EmptyStateName = "__15D8A_EmptyState";
    private const string EmptyStateCardName = "__15D8A_EmptyStateCard";
    private const string EmptyStateBadgeName = "__15D8A_EmptyStateBadge";
    private const string EmptyStateBadgeLabelName = "Label";
    private const string EmptyStateTitleName = "__15D8A_EmptyStateTitle";
    private const string EmptyStateBodyName = "__15D8A_EmptyStateBody";
    private const string MainGoalEmptyStateName = "__15D8A2_MainGoalEmptyState";
    private const string BudgetTitleName = "__15D8A2_BudgetTitle";
    private const string RecordTitleName = "__15D8A2_RecordTitle";
    private const string RewardTitleName = "__15D8A2_RewardTitle";
    private const string GoalTitleName = "__15D8A2_GoalTitle";
    private const string BudgetUsageName = "__15D8A2_BudgetUsage";
    private const string BudgetRemainingName = "__15D8A2_BudgetRemaining";
    private const string BudgetFooterName = "__15D8A3_BudgetFooter";
    private const string RewardCellPrefix = "__15D8A3_RewardCell_";
    private const string RewardGridName = "__15D8A4_RewardGrid";
    private const string RewardLabelName = "Label";
    private const string RewardDisplayName = "Value";
    private const string RecordSubtitleName = "__15D8A5_RecordSubtitle";
    private const string RecordCategoryLabelName = "__15D8A5_RecordCategoryLabel";
    private const string RecordCurrencyName = "__15D8A5_RecordCurrency";
    private const string MenuRoot =
        "Tools/HabitPVP/UI Renewal/MainScene/15-D-8A Spending & Budget/";
    private const string TmpDiagnosticsMenu =
        "Tools/HabitPVP/UI Renewal/Diagnostics/Inspect TMP SubMeshUI";
    private const string HistoryMenuRoot = MenuRoot + "SpendHistory/";
    private const string BudgetSettingMenuRoot = MenuRoot + "BudgetSetting/";
    private const string RecordSpendMenuRoot = MenuRoot + "RecordSpendAdd/";
    private const string SpendAddMenuRoot = MenuRoot + "SpendAdd_1/";
    private const string SpendAddTitleName = "__15D8A_SpendAddTitle";
    private const string SpendAddSubtitleName = "__15D8A_SpendAddSubtitle";
    private const string SpendAddOptionalName = "__15D8A_SpendAddOptional";
    private const string SpendAddNameLabelName = "__15D8A_GoalNameLabel";
    private const string SpendAddAmountLabelName = "__15D8A_GoalAmountLabel";
    private const string SpendAddPreviewTitleName = "__15D8A_GoalPreviewTitle";
    private const string SpendAddEmptyStateName = "__15D8A_GoalPreviewEmptyState";
    private const string SpendAddInputCardName = "__15D8A_GoalInputCard";
    private const string SpendAddPreviewCardName = "__15D8A_GoalPreviewCard";
    private const string SpendAddRewardCardName = "__15D8A_GoalRewardCard";
    private const string SpendAddInputTitleName = "__15D8A_GoalInputTitle";

    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 CreamLight = new Color32(0xFF, 0xF9, 0xEA, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x86, 0x68, 0x5D, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);
    private static readonly Color32 Green = new Color32(0x6F, 0xA8, 0x63, 0xFF);

    [MenuItem(SpendAddMenuRoot + "Validate", false, 2460)]
    private static void ValidateSpendAddMenu()
    {
        Context context = ValidateStructure(true);
        if (context.IsValid)
            Debug.Log("[HabitPVP 15-D-8A] SpendAdd_1 구조 검증 성공. 입력, Preview, " +
                      "Skip/Complete, 주기 선택 레거시 참조와 UnityEvent가 확인되었습니다.",
                context.GoalSetting);
    }

    [MenuItem(SpendAddMenuRoot + "Dry Run", false, 2461)]
    private static void DryRunSpendAddMenu()
    {
        Context context = ValidateStructure(true);
        if (!context.IsValid) return;
        Debug.Log("[HabitPVP 15-D-8A] SpendAdd_1 Dry Run (변경/Dirty/저장 없음)\n" +
                  "- Weekly 전용 제목/설명과 목표 이름·금액 입력 순서 정돈\n" +
                  "- Daily/Weekly 버튼은 참조와 이벤트를 보존한 채 화면에서만 숨김\n" +
                  "- 기존 보상 값/텍스트는 변경하지 않고 RPG 카드 스타일만 적용\n" +
                  "- Preview는 세로 스크롤·동적 Content·목표 카드 가독성 개선\n" +
                  "- Skip/Complete 문구를 건너뛰기/설정 완료로 변경하되 상태와 이벤트 보존\n" +
                  "- 자동 Scene 저장 및 Prefab Apply 없음", context.GoalSetting);
    }

    [MenuItem(SpendAddMenuRoot + "Apply (No Auto Save)", false, 2462)]
    private static void ApplySpendAddMenu()
    {
        Context context = ValidateStructure(true);
        if (!context.IsValid) return;

        ProtectedState protectedState = new ProtectedState(context);
        bool skipInteractable = context.SkipButton.interactable;
        bool completeInteractable = context.CompleteButton.interactable;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply 15-D-8A SpendAdd_1 UI Renewal");
        try
        {
            StyleGoalSetting(context);
            if (context.SkipButton.interactable != skipInteractable ||
                context.CompleteButton.interactable != completeInteractable)
                throw new InvalidOperationException("Skip/Complete 활성 상태가 변경되었습니다.");
            protectedState.AssertUnchanged(context);
            List<string> errors = ValidateSpendAddApplied(context);
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", errors));

            EditorSceneManager.MarkSceneDirty(context.Scene);
            Undo.CollapseUndoOperations(group);
            Debug.Log("[HabitPVP 15-D-8A] SpendAdd_1 전용 적용 성공. 자동 저장과 Prefab Apply는 수행하지 않았습니다.",
                context.GoalSetting);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(group);
            Debug.LogError("[HabitPVP 15-D-8A] SpendAdd_1 적용 실패. 이번 변경을 Undo로 복원했습니다.\n" + exception);
        }
    }

    [MenuItem(SpendAddMenuRoot + "Validate Applied Design", false, 2463)]
    private static void ValidateAppliedSpendAddMenu()
    {
        Context context = ValidateStructure(true);
        if (!context.IsValid) return;
        List<string> errors = ValidateSpendAddApplied(context);
        if (errors.Count == 0)
        {
            Debug.Log("[HabitPVP 15-D-8A] SpendAdd_1 적용 디자인 검증 성공.", context.GoalSetting);
            return;
        }
        foreach (string error in errors)
            Debug.LogError("[HabitPVP 15-D-8A] " + error, context.GoalSetting);
    }

    [MenuItem(RecordSpendMenuRoot + "Validate RecordSpendAdd", false, 2450)]
    private static void ValidateRecordSpendMenu()
    {
        Context context = ValidateStructure(true);
        if (context.IsValid)
            Debug.Log("[HabitPVP 15-D-8A] RecordSpendAdd 구조 검증 성공. 기존 Input/Dropdown, " +
                      "Template/Viewport/Content/Item, Save/Back UnityEvent가 확인되었습니다.",
                context.RecordSpend);
    }

    [MenuItem(RecordSpendMenuRoot + "Dry Run RecordSpendAdd", false, 2451)]
    private static void DryRunRecordSpendMenu()
    {
        Context context = ValidateStructure(true);
        if (!context.IsValid) return;
        Debug.Log("[HabitPVP 15-D-8A] RecordSpendAdd Dry Run (변경/Dirty/저장 없음)\n" +
                  "- 제목/안내/입력 카드/저장 버튼의 1080x1920 세로 레이아웃\n" +
                  "- 기존 TMP_InputField 숫자 값과 TMP_Dropdown 영문 OptionData 보존\n" +
                  "- 닫힌 캡션과 펼친 목록만 한국어로 표시하는 Presenter 연결\n" +
                  "- Dropdown 84px 항목, 베이지 목록, 선택/Pressed 상태 및 내부 세로 스크롤\n" +
                  "- 자동 Scene 저장 및 Prefab Apply 없음", context.RecordSpend);
    }

    [MenuItem(RecordSpendMenuRoot + "Apply RecordSpendAdd (No Auto Save)", false, 2452)]
    private static void ApplyRecordSpendMenu()
    {
        Context context = ValidateStructure(true);
        if (!context.IsValid) return;

        ProtectedState protectedState = new ProtectedState(context);
        string[] optionTexts = context.RecordCategoryDropdown.options
            .Select(option => option.text).ToArray();
        int selectedIndex = context.RecordCategoryDropdown.value;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply 15-D-8A RecordSpendAdd UI Renewal");
        try
        {
            StyleRecordSpend(context);
            if (!optionTexts.SequenceEqual(context.RecordCategoryDropdown.options
                    .Select(option => option.text)) ||
                selectedIndex != context.RecordCategoryDropdown.value)
                throw new InvalidOperationException("RecordSpendAdd Dropdown OptionData 또는 선택값이 변경되었습니다.");
            protectedState.AssertUnchanged(context);
            List<string> errors = ValidateRecordSpendApplied(context);
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", errors));

            EditorSceneManager.MarkSceneDirty(context.Scene);
            Undo.CollapseUndoOperations(group);
            Debug.Log("[HabitPVP 15-D-8A] RecordSpendAdd 전용 적용 성공. 자동 저장과 Prefab Apply는 수행하지 않았습니다.",
                context.RecordSpend);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(group);
            Debug.LogError("[HabitPVP 15-D-8A] RecordSpendAdd 적용 실패. 이번 변경을 Undo로 복원했습니다.\n" +
                           exception);
        }
    }

    [MenuItem(RecordSpendMenuRoot + "Validate Applied RecordSpendAdd", false, 2453)]
    private static void ValidateAppliedRecordSpendMenu()
    {
        Context context = ValidateStructure(true);
        if (!context.IsValid) return;
        List<string> errors = ValidateRecordSpendApplied(context);
        if (errors.Count == 0)
        {
            Debug.Log("[HabitPVP 15-D-8A] RecordSpendAdd 적용 검증 성공.", context.RecordSpend);
            return;
        }
        foreach (string error in errors)
            Debug.LogError("[HabitPVP 15-D-8A] " + error, context.RecordSpend);
    }

    [MenuItem(BudgetSettingMenuRoot + "Validate BudgetSetting", false, 2430)]
    private static void ValidateBudgetSettingMenu()
    {
        Context context = ValidateStructure(true);
        if (context.IsValid)
            Debug.Log("[HabitPVP 15-D-8A] BudgetSetting 구조 검증 성공. 기존 Input/Button/Manager 및 " +
                      "SerializeField 참조가 확인되었습니다.", context.BudgetSetting);
    }

    [MenuItem(BudgetSettingMenuRoot + "Dry Run BudgetSetting", false, 2431)]
    private static void DryRunBudgetSettingMenu()
    {
        Context context = ValidateStructure(true);
        if (!context.IsValid) return;
        Debug.Log("[HabitPVP 15-D-8A] BudgetSetting Dry Run (변경/Dirty/저장 없음)\n" +
                  "- 1540px 세로 Content로 상단 빈 공간 축소\n" +
                  "- 제목 → 설명 → 입력 카드 → 주간 제한 안내 → 저장/뒤로 버튼 순서 정돈\n" +
                  "- 기존 TMP_InputField와 Button을 재사용하고 interactable 상태는 변경하지 않음\n" +
                  "- NotoSansKR, 크림/브라운/골드 RPG 스타일 유지\n" +
                  "- 자동 Scene 저장 및 Prefab Apply 없음", context.BudgetSetting);
    }

    [MenuItem(BudgetSettingMenuRoot + "Apply BudgetSetting (No Auto Save)", false, 2432)]
    private static void ApplyBudgetSettingMenu()
    {
        Context context = ValidateStructure(true);
        if (!context.IsValid) return;

        ProtectedState protectedState = new ProtectedState(context);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply 15-D-8A BudgetSetting UI Renewal");
        try
        {
            bool inputInteractable = context.BudgetInput.interactable;
            bool saveInteractable = context.BudgetSaveButton.interactable;
            StyleBudgetSetting(context);
            if (context.BudgetInput.interactable != inputInteractable ||
                context.BudgetSaveButton.interactable != saveInteractable)
                throw new InvalidOperationException("BudgetSetting의 주간 잠금 interactable 상태가 변경되었습니다.");
            protectedState.AssertUnchanged(context);
            List<string> errors = ValidateBudgetSettingApplied(context);
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", errors));

            EditorSceneManager.MarkSceneDirty(context.Scene);
            Undo.CollapseUndoOperations(group);
            Debug.Log("[HabitPVP 15-D-8A] BudgetSetting 전용 적용 성공. 자동 저장과 Prefab Apply는 수행하지 않았습니다.",
                context.BudgetSetting);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(group);
            Debug.LogError("[HabitPVP 15-D-8A] BudgetSetting 적용 실패. 이번 변경을 Undo로 복원했습니다.\n" + exception);
        }
    }

    [MenuItem(BudgetSettingMenuRoot + "Validate Applied BudgetSetting", false, 2433)]
    private static void ValidateAppliedBudgetSettingMenu()
    {
        Context context = ValidateStructure(true);
        if (!context.IsValid) return;
        List<string> errors = ValidateBudgetSettingApplied(context);
        if (errors.Count == 0)
        {
            Debug.Log("[HabitPVP 15-D-8A] BudgetSetting 적용 검증 성공.", context.BudgetSetting);
            return;
        }
        foreach (string error in errors)
            Debug.LogError("[HabitPVP 15-D-8A] " + error, context.BudgetSetting);
    }

    [MenuItem(HistoryMenuRoot + "Validate SpendHistory", false, 2440)]
    private static void ValidateSpendHistoryMenu()
    {
        Context context = ValidateStructure(true);
        if (context.IsValid)
            Debug.Log("[HabitPVP 15-D-8A] SpendHistory 구조 검증 성공. ScrollRect/Viewport/Content, " +
                      "거래 템플릿, EmptyState, 제목 및 뒤로가기 참조가 확인되었습니다.", context.History);
    }

    [MenuItem(HistoryMenuRoot + "Dry Run SpendHistory", false, 2441)]
    private static void DryRunSpendHistoryMenu()
    {
        Context context = ValidateStructure(true);
        if (!context.IsValid) return;

        Image scrollBackground = context.TransactionScroll.GetComponent<Image>();
        Debug.Log("[HabitPVP 15-D-8A] SpendHistory Dry Run (변경/Dirty/저장 없음)\n" +
                  "- 회색 영역 원인: HistoryScrollView 루트의 Unity 기본 Background Image\n" +
                  $"- 현재 Scroll 배경: {(scrollBackground != null ? scrollBackground.color.ToString() : "없음")}\n" +
                  "- 보정: ScrollRect/Viewport/Mask를 유지하고 루트 Image만 크림색으로 통일\n" +
                  "- EmptyState: 베이지 RPG 카드, 골드 배지, 46px 제목, 34px 보조 문구\n" +
                  "- 제목/뒤로가기: 기존 동작과 클릭 영역을 유지하며 정렬/스타일 보정\n" +
                  "- 거래 템플릿/API/Toggle/Content/Mask 참조 보존\n" +
                  "- 자동 Scene 저장 및 Prefab Apply 없음", context.History);
    }

    [MenuItem(HistoryMenuRoot + "Apply SpendHistory (No Auto Save)", false, 2442)]
    private static void ApplySpendHistoryMenu()
    {
        Context context = ValidateStructure(true);
        if (!context.IsValid) return;

        ProtectedState protectedState = new ProtectedState(context);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply 15-D-8A SpendHistory UI Renewal");
        try
        {
            StyleHistory(context);
            ConnectRuntimeTransactionStyle(context);
            EnsureEmptyState(context);
            LayoutRebuilder.ForceRebuildLayoutImmediate(context.TransactionContent);
            protectedState.AssertUnchanged(context);

            List<string> errors = ValidateSpendHistoryApplied(context);
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", errors));

            EditorSceneManager.MarkSceneDirty(context.Scene);
            Undo.CollapseUndoOperations(group);
            Debug.Log("[HabitPVP 15-D-8A] SpendHistory 전용 적용 성공. 자동 저장과 Prefab Apply는 수행하지 않았습니다.",
                context.History);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(group);
            Debug.LogError("[HabitPVP 15-D-8A] SpendHistory 적용 실패. 이번 변경을 Undo로 복원했습니다.\n" + exception);
        }
    }

    [MenuItem(HistoryMenuRoot + "Validate Applied SpendHistory", false, 2443)]
    private static void ValidateAppliedSpendHistoryMenu()
    {
        Context context = ValidateStructure(true);
        if (!context.IsValid) return;
        List<string> errors = ValidateSpendHistoryApplied(context);
        if (errors.Count == 0)
        {
            Debug.Log("[HabitPVP 15-D-8A] SpendHistory 적용 검증 성공.", context.History);
            return;
        }
        foreach (string error in errors)
            Debug.LogError("[HabitPVP 15-D-8A] " + error, context.History);
    }

    [MenuItem(MenuRoot + "Validate Structure", false, 2420)]
    private static void ValidateStructureMenu()
    {
        Context context = ValidateStructure(true);
        if (context.IsValid)
        {
            Debug.Log(
                "[HabitPVP 15-D-8A] 구조 검증 성공. 소비 메인, 예산 설정, 소비 기록, " +
                "특수 목표, 소비 내역과 기존 Manager/API 참조가 확인되었습니다. " +
                (context.SpendingTitle != null
                    ? $"소비 메인 제목은 '{GetHierarchyPath(context.SpendingTitle.transform)}'을 재사용합니다. "
                    : "소비 메인에는 전용 제목 TMP가 없으며 TopTab 소비 버튼을 그대로 유지합니다. ") +
                "WeeklyReportPanel은 검사 대상에서 제외했으며 변경하지 않습니다.",
                context.Spend);
        }
    }

    [MenuItem(MenuRoot + "Dry Run", false, 2421)]
    private static void DryRunMenu()
    {
        Context context = ValidateStructure(true);
        if (!context.IsValid)
        {
            Debug.LogError("[HabitPVP 15-D-8A] Dry Run 중단: 구조 오류를 먼저 해결해 주세요.");
            return;
        }

        Debug.Log(
            "[HabitPVP 15-D-8A] Dry Run (변경/Dirty/저장 없음)\n" +
            "- Spend: 기존 ScrollRect/Viewport/Content/세로 Scrollbar 재사용, " +
            "ScrollRect 활성화, 가로 비활성, 1080x1920 세로 스크롤\n" +
            $"- 주간 예산: BudgetLayout 마지막 자식에 FooterRow " +
            $"{(context.BudgetFooter != null ? "재사용" : "생성")}, 카드 높이 440\n" +
            "- 소비 기록: 제목 여백을 확보한 동일 크기 2열 버튼\n" +
            $"- 예상 보상: RewardCard 아래 GridRoot " +
            $"{(context.RewardGrid != null ? "재사용" : "생성")}, " +
            "2열 x 2행 셀/Label/Value 구성, 카드 높이 620\n" +
            "- 참조 보호: 기존 SpendRewardManager TMP는 유지하고 표시 TMP를 매 프레임 동기화\n" +
            "- 특수 목표: VerticalLayoutGroup, 빈 상태와 목록 높이 자동 전환\n" +
            (context.SpendingTitle != null
                ? $"- 제목: 기존 '{GetHierarchyPath(context.SpendingTitle.transform)}' TMP 재사용\n"
                : "- 제목: Spend 전용 TMP 없음, TopTab 소비 버튼 유지, 새 제목 생성 없음\n") +
            "- BudgetSetting: 예산 입력 카드, 숫자 입력, 저장/뒤로 버튼\n" +
            "- RecordSpendAdd: 금액/카테고리 입력, Dropdown 스크롤, 저장/뒤로 버튼\n" +
            "- SpendAdd_1: 목표 입력, 주기 버튼, Preview 동적 목록\n" +
            "- SpendHistory: 거래 카드 템플릿, 동적 목록, 빈 목록 안내\n" +
            "- 보존: Canvas.prefab 원본, WeeklyReportPanel, MainTabManager, UnityEvent, API/DTO\n" +
            "- 저장: 자동 Scene 저장 및 Prefab Apply 없음",
            context.Spend);
    }

    [MenuItem(MenuRoot + "Apply (No Auto Save)", false, 2422)]
    private static void ApplyMenu()
    {
        Context context = ValidateStructure(true);
        if (!context.IsValid)
        {
            Debug.LogError("[HabitPVP 15-D-8A] Apply 중단: 구조 오류를 먼저 해결해 주세요.");
            return;
        }

        ProtectedState protectedState = new ProtectedState(context);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply 15-D-8A Spending and Budget UI Renewal");

        try
        {
            ApplyDesign(context);
            protectedState.AssertUnchanged(context);

            List<string> errors = ValidateAppliedDesign(context);
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", errors));

            EditorSceneManager.MarkSceneDirty(context.Scene);
            Undo.CollapseUndoOperations(group);
            Debug.Log(
                "[HabitPVP 15-D-8A] 적용 성공. MainScene의 소비/예산 관련 Scene Override만 " +
                "Dirty 처리했습니다. 자동 저장, Canvas.prefab Apply, WeeklyReportPanel 변경은 수행하지 않았습니다.",
                context.Spend);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(group);
            Debug.LogError(
                "[HabitPVP 15-D-8A] Apply 실패. 이번 Scene 변경을 Undo로 복원했습니다. " +
                "Scene 저장이나 Prefab Apply는 수행하지 않았습니다.\n" + exception);
        }
    }

    [MenuItem(MenuRoot + "Validate Applied Design", false, 2423)]
    private static void ValidateAppliedDesignMenu()
    {
        Context context = ValidateStructure(true);
        if (!context.IsValid) return;

        List<string> errors = ValidateAppliedDesign(context);
        if (errors.Count == 0)
        {
            Debug.Log(
                "[HabitPVP 15-D-8A] 적용 디자인 검증 성공. 세로 스크롤, 동적 Content 높이, " +
                "NotoSansKR, RPG 카드, 한글 대시보드, 입력/버튼 참조와 빈 목록 안내가 확인되었습니다.",
                context.Spend);
            return;
        }

        foreach (string error in errors)
            Debug.LogError("[HabitPVP 15-D-8A] " + error, context.Spend);
    }

    [MenuItem(TmpDiagnosticsMenu, false, 2490)]
    private static void InspectTmpSubMeshesMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();
        TextMeshProUGUI[] texts = roots
            .SelectMany(root => root.GetComponentsInChildren<TextMeshProUGUI>(true)).ToArray();
        TMP_SubMeshUI[] subMeshes = roots
            .SelectMany(root => root.GetComponentsInChildren<TMP_SubMeshUI>(true)).ToArray();

        int issueCount = 0;
        int spendingSubMeshCount = 0;
        StringBuilder report = new StringBuilder();
        report.AppendLine("[HabitPVP TMP Diagnostics] 읽기 전용 TMP_SubMeshUI 검사");
        report.AppendLine($"- Scene: {scene.path}");
        report.AppendLine($"- TextMeshProUGUI: {texts.Length}, TMP_SubMeshUI: {subMeshes.Length}");

        foreach (TMP_SubMeshUI subMesh in subMeshes)
        {
            string path = GetHierarchyPath(subMesh.transform);
            bool isSpending = path.IndexOf("/Spend", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isSpending) spendingSubMeshCount++;

            SerializedObject serialized = new SerializedObject(subMesh);
            SerializedProperty textComponentProperty = serialized.FindProperty("m_TextComponent");
            TextMeshProUGUI serializedParent = textComponentProperty != null
                ? textComponentProperty.objectReferenceValue as TextMeshProUGUI
                : null;
            TextMeshProUGUI hierarchyParent = FindParentText(subMesh.transform.parent);
            TMP_FontAsset font = subMesh.fontAsset;
            Material material = subMesh.sharedMaterial;
            Material parentMaterial = hierarchyParent != null ? hierarchyParent.fontSharedMaterial : null;

            List<string> issues = new List<string>();
            if (hierarchyParent == null) issues.Add("Hierarchy parent TMP 없음");
            if (serializedParent == null) issues.Add("m_TextComponent null");
            if (serializedParent != null && serializedParent != hierarchyParent)
                issues.Add("m_TextComponent와 Hierarchy parent 불일치");
            if (font == null) issues.Add("Font Asset null");
            if (material == null) issues.Add("Shared Material null");
            if (material != null && material.shader == null) issues.Add("Material Shader null");
            if (material != null && material.mainTexture == null) issues.Add("Material Atlas Texture null");
            if (hierarchyParent != null && hierarchyParent.font == null)
                issues.Add("Parent Font Asset null");
            if (hierarchyParent != null && parentMaterial == null)
                issues.Add("Parent fontSharedMaterial null (TMP 3.0.7 UpdateMaterial 710 위험)");

            issueCount += issues.Count > 0 ? 1 : 0;
            Texture2D[] atlasTextures = font != null ? font.atlasTextures : null;
            int validAtlasCount = atlasTextures == null
                ? 0
                : atlasTextures.Count(texture => texture != null);

            report.AppendLine();
            report.AppendLine($"[{(issues.Count == 0 ? "OK" : "ISSUE")}] {path}");
            report.AppendLine(
                $"  activeSelf={subMesh.gameObject.activeSelf}, activeInHierarchy={subMesh.gameObject.activeInHierarchy}, " +
                $"hideFlags={subMesh.gameObject.hideFlags}, 15-D-8A={IsDashboardGenerated(path)}");
            report.AppendLine(
                $"  parent={ObjectName(hierarchyParent)}, serializedParent={ObjectName(serializedParent)}");
            report.AppendLine(
                $"  font={ObjectName(font)}, material={ObjectName(material)}, " +
                $"shader={ObjectName(material != null ? material.shader : null)}, " +
                $"atlas={ObjectName(material != null ? material.mainTexture : null)}, " +
                $"fontAtlases={validAtlasCount}/{(atlasTextures != null ? atlasTextures.Length : 0)}");
            if (issues.Count > 0)
                report.AppendLine("  issues=" + string.Join(", ", issues));
        }

        foreach (TextMeshProUGUI text in texts)
        {
            Material material = text.fontSharedMaterial;
            if (text.font != null && material != null && material.shader != null) continue;

            issueCount++;
            report.AppendLine();
            report.AppendLine($"[PARENT TMP ISSUE] {GetHierarchyPath(text.transform)}");
            report.AppendLine(
                $"  font={ObjectName(text.font)}, material={ObjectName(material)}, " +
                $"shader={ObjectName(material != null ? material.shader : null)}");
        }

        report.AppendLine();
        report.AppendLine(
            $"[SUMMARY] Spend SubMeshUI={spendingSubMeshCount}, issue objects={issueCount}. " +
            "이 메뉴는 활성 상태, Mesh, Material, Font Asset 또는 Scene Dirty 상태를 변경하지 않습니다.");

        if (issueCount == 0)
            Debug.Log(report.ToString());
        else
            Debug.LogWarning(report.ToString());
    }

    [MenuItem(TmpDiagnosticsMenu, true)]
    private static bool CanInspectTmpSubMeshes()
    {
        Scene scene = SceneManager.GetActiveScene();
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
               scene.IsValid() && scene.isLoaded && scene.path == ScenePath;
    }

    [MenuItem(MenuRoot + "Validate Structure", true)]
    [MenuItem(MenuRoot + "Dry Run", true)]
    [MenuItem(MenuRoot + "Apply (No Auto Save)", true)]
    [MenuItem(MenuRoot + "Validate Applied Design", true)]
    [MenuItem(BudgetSettingMenuRoot + "Validate BudgetSetting", true)]
    [MenuItem(BudgetSettingMenuRoot + "Dry Run BudgetSetting", true)]
    [MenuItem(BudgetSettingMenuRoot + "Apply BudgetSetting (No Auto Save)", true)]
    [MenuItem(BudgetSettingMenuRoot + "Validate Applied BudgetSetting", true)]
    [MenuItem(HistoryMenuRoot + "Validate SpendHistory", true)]
    [MenuItem(HistoryMenuRoot + "Dry Run SpendHistory", true)]
    [MenuItem(HistoryMenuRoot + "Apply SpendHistory (No Auto Save)", true)]
    [MenuItem(HistoryMenuRoot + "Validate Applied SpendHistory", true)]
    [MenuItem(RecordSpendMenuRoot + "Validate RecordSpendAdd", true)]
    [MenuItem(RecordSpendMenuRoot + "Dry Run RecordSpendAdd", true)]
    [MenuItem(RecordSpendMenuRoot + "Apply RecordSpendAdd (No Auto Save)", true)]
    [MenuItem(RecordSpendMenuRoot + "Validate Applied RecordSpendAdd", true)]
    [MenuItem(SpendAddMenuRoot + "Validate", true)]
    [MenuItem(SpendAddMenuRoot + "Dry Run", true)]
    [MenuItem(SpendAddMenuRoot + "Apply (No Auto Save)", true)]
    [MenuItem(SpendAddMenuRoot + "Validate Applied Design", true)]
    private static bool CanUseMenus()
    {
        Scene scene = SceneManager.GetActiveScene();
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
               scene.IsValid() && scene.isLoaded && scene.path == ScenePath;
    }

    private static Context ValidateStructure(bool logErrors)
    {
        Context context = new Context { Scene = SceneManager.GetActiveScene() };
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            context.Errors.Add("Play Mode를 종료한 뒤 실행해 주세요.");
        if (context.Scene.path != ScenePath)
            context.Errors.Add($"MainScene에서만 실행할 수 있습니다. 현재: '{context.Scene.path}'");

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
            context.CanvasScaler = context.Canvas.GetComponent<CanvasScaler>();
            Require(context, context.CanvasScaler, "Canvas/CanvasScaler");
        }

        context.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        context.CardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardSpritePath);
        context.CreamButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CreamButtonSpritePath);
        context.GoldButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GoldButtonSpritePath);
        Require(context, context.Font, "NotoSansKR TMP Font Asset");
        Require(context, context.CardSprite, "RPG 카드 Sprite");
        Require(context, context.CreamButtonSprite, "크림 버튼 Sprite");
        Require(context, context.GoldButtonSprite, "골드 버튼 Sprite");

        if (context.Canvas != null)
        {
            context.TapBar = FindUnique(context.Canvas.transform, "TapBar", context.Errors)?.gameObject;
            context.WeeklyReportPanel = FindUnique(
                context.Canvas.transform, "WeeklyReportPanel", context.Errors)?.gameObject;
            context.HabitUiManager = FindUniqueComponent<HabitUIManager>(context);
            context.BudgetManager = FindUniqueComponent<SpendBudgetManager>(context);
            context.RewardManager = FindUniqueComponent<SpendRewardManager>(context);

            context.Spend = FindUnique(context.Canvas.transform, "Spend", context.Errors)?.gameObject;
            Transform spend = context.Spend != null ? context.Spend.transform : null;
            context.SpendingTitle = FindOptionalScreenTitle(spend, context.Errors);
            context.Scroll = GetUnique<ScrollRect>(spend, "SpendScrollView", context.Errors);
            context.Content = context.Scroll != null ? context.Scroll.content : null;
            Require(context, context.Content, "SpendScrollView/Content");
            context.RecordCard = FindUnique(spend, "RecordCard", context.Errors) as RectTransform;
            context.GoalListCard = FindUnique(spend, "GoldListCard", context.Errors) as RectTransform;
            context.RewardCard = FindUnique(spend, "RewardCard", context.Errors) as RectTransform;
            context.BudgetCard = FindUnique(spend, "BudgetCard", context.Errors) as RectTransform;
            context.GoalScroll = GetUnique<ScrollRect>(context.GoalListCard, "GoalScrollView", context.Errors);
            context.RecordButton = GetUnique<Button>(context.RecordCard, "RecordButton", context.Errors);
            context.HistoryButton = GetUnique<Button>(context.RecordCard, "ViewHistoryButton", context.Errors);
            context.ClaimButton = GetUnique<Button>(context.RewardCard, "ClaimRewardButton", context.Errors);
            context.BudgetSettingButton = GetUnique<Button>(
                context.BudgetCard, "BudgetSettingButton", context.Errors);
            context.BudgetText = GetUnique<TMP_Text>(context.BudgetCard, "BudgetText", context.Errors);
            context.UsedText = GetUnique<TMP_Text>(context.BudgetCard, "UsedText", context.Errors);
            context.BudgetSlider = GetUnique<Slider>(context.BudgetCard, "Slider", context.Errors);
            context.BudgetTitle = FindDescendant(context.BudgetCard, BudgetTitleName)?.GetComponent<TMP_Text>();
            context.RecordTitle = FindDescendant(context.RecordCard, RecordTitleName)?.GetComponent<TMP_Text>();
            context.RewardTitle = FindDescendant(context.RewardCard, RewardTitleName)?.GetComponent<TMP_Text>();
            context.GoalTitle = FindDescendant(context.GoalListCard, GoalTitleName)?.GetComponent<TMP_Text>();
            context.BudgetUsageText = FindDescendant(context.BudgetCard, BudgetUsageName)?.GetComponent<TMP_Text>();
            context.BudgetRemainingText = FindDescendant(context.BudgetCard, BudgetRemainingName)?.GetComponent<TMP_Text>();
            context.BudgetFooter = FindDescendant(context.BudgetCard, BudgetFooterName) as RectTransform;
            context.MainGoalEmptyState = FindDescendant(
                context.GoalListCard, MainGoalEmptyStateName)?.gameObject;
            context.DashboardPresenter = context.Spend.GetComponent<SpendingDashboardPresenter>();
            context.RewardGrid = FindDescendant(context.RewardCard, RewardGridName) as RectTransform;
            string[] rewardCellSuffixes = { "Base", "Bonus", "Streak", "StreakBonus" };
            for (int index = 0; index < rewardCellSuffixes.Length; index++)
            {
                context.RewardCells[index] = FindDescendant(
                    context.RewardCard, RewardCellPrefix + rewardCellSuffixes[index]) as RectTransform;
                context.RewardDisplayLabels[index] = FindDescendant(
                    context.RewardCells[index], RewardLabelName)?.GetComponent<TMP_Text>();
                context.RewardDisplayTexts[index] = FindDescendant(
                    context.RewardCells[index], RewardDisplayName)?.GetComponent<TMP_Text>();
            }

            if (context.RewardManager != null)
            {
                SerializedObject reward = new SerializedObject(context.RewardManager);
                context.TotalRewardText = ReadReference<TMP_Text>(reward, "totalGoldText");
                context.BaseRewardText = ReadReference<TMP_Text>(reward, "baseGoldText");
                context.BonusRewardText = ReadReference<TMP_Text>(reward, "bonusGoldText");
                context.RewardStatusText = ReadReference<TMP_Text>(reward, "rewardTitleText");
                context.StreakText = ReadReference<TMP_Text>(reward, "streakText");
                context.StreakBonusText = ReadReference<TMP_Text>(reward, "streakBonusText");
            }
            Require(context, context.TotalRewardText, "SpendRewardManager totalGoldText 참조");
            Require(context, context.BaseRewardText, "SpendRewardManager baseGoldText 참조");
            Require(context, context.BonusRewardText, "SpendRewardManager bonusGoldText 참조");
            Require(context, context.RewardStatusText, "SpendRewardManager rewardTitleText 참조");
            Require(context, context.StreakText, "SpendRewardManager streakText 참조");
            Require(context, context.StreakBonusText, "SpendRewardManager streakBonusText 참조");

            ReadBudgetSetting(context);
            ReadRecordSpend(context);
            ReadGoalSetting(context);
            ReadHistory(context);
        }

        if (logErrors)
        {
            foreach (string error in context.Errors)
                Debug.LogError("[HabitPVP 15-D-8A] " + error);
        }

        return context;
    }

    private static void ReadBudgetSetting(Context context)
    {
        context.BudgetSetting = FindUnique(
            context.Canvas.transform, "BudgetSetting", context.Errors)?.gameObject;
        Transform root = context.BudgetSetting != null ? context.BudgetSetting.transform : null;
        context.BudgetSettingManager = root != null ? root.GetComponent<BudgetSettingManager>() : null;
        Require(context, context.BudgetSettingManager, "BudgetSetting/BudgetSettingManager");
        if (context.BudgetSettingManager != null)
        {
            SerializedObject serialized = new SerializedObject(context.BudgetSettingManager);
            context.BudgetInput = ReadReference<TMP_InputField>(serialized, "budgetInput");
            context.BudgetSaveButton = ReadReference<Button>(serialized, "saveButton");
            context.BudgetBackButton = ReadReference<Button>(serialized, "backButton");
        }
        Require(context, context.BudgetInput, "BudgetSetting budgetInput 참조");
        Require(context, context.BudgetSaveButton, "BudgetSetting saveButton 참조");
        Require(context, context.BudgetBackButton, "BudgetSetting backButton 참조");
        context.BudgetSettingContent = FindUnique(root, "Content", context.Errors) as RectTransform;
        context.BudgetSettingCard = FindUnique(root, "BudgetCard", context.Errors) as RectTransform;
        context.BudgetSettingTitle = GetUnique<TMP_Text>(root, "title", context.Errors);
        context.BudgetDescription = GetUnique<TMP_Text>(root, "Description", context.Errors);
        context.BudgetDescriptionSecondary = GetUnique<TMP_Text>(root, "Description (1)", context.Errors);
    }

    private static void ReadRecordSpend(Context context)
    {
        context.RecordSpend = FindUnique(
            context.Canvas.transform, "RecordSpendAdd", context.Errors)?.gameObject;
        context.RecordSpendManager = context.RecordSpend != null
            ? context.RecordSpend.GetComponent<RecordSpendManager>()
            : null;
        Require(context, context.RecordSpendManager, "RecordSpendAdd/RecordSpendManager");
        if (context.RecordSpendManager != null)
        {
            SerializedObject serialized = new SerializedObject(context.RecordSpendManager);
            context.RecordAmountInput = ReadReference<TMP_InputField>(serialized, "amountInput");
            context.RecordCategoryDropdown = ReadReference<TMP_Dropdown>(serialized, "categoryDropdown");
        }
        Require(context, context.RecordAmountInput, "RecordSpendAdd amountInput 참조");
        Require(context, context.RecordCategoryDropdown, "RecordSpendAdd categoryDropdown 참조");
        context.RecordFormScroll = context.RecordSpend != null
            ? context.RecordSpend.GetComponentsInChildren<ScrollRect>(true).FirstOrDefault()
            : null;
        context.RecordContent = context.RecordFormScroll != null ? context.RecordFormScroll.content : null;
        context.RecordInputCard = context.RecordAmountInput != null
            ? context.RecordAmountInput.transform.parent as RectTransform
            : null;
        context.RecordAmountLabel = context.RecordInputCard != null
            ? context.RecordInputCard.Find("label")?.GetComponent<TMP_Text>()
            : null;
        context.RecordScreenTitle = context.RecordSpend != null
            ? context.RecordSpend.transform.Find("TopHeader/Title")?.GetComponent<TMP_Text>()
            : null;
        context.RecordSubtitle = FindDescendant(
            context.RecordContent, RecordSubtitleName)?.GetComponent<TMP_Text>();
        context.RecordCategoryLabel = FindDescendant(
            context.RecordInputCard, RecordCategoryLabelName)?.GetComponent<TMP_Text>();
        context.RecordCurrency = FindDescendant(
            context.RecordInputCard, RecordCurrencyName)?.GetComponent<TMP_Text>();
        context.RecordCategoryPresenter = context.RecordCategoryDropdown != null
            ? context.RecordCategoryDropdown.GetComponent<SpendCategoryDropdownPresenter>()
            : null;
        context.RecordSaveButton = FindButtonByPersistentMethod(
            context.RecordSpend?.transform, "Save");
        context.RecordBackButton = FindButtonByPersistentMethod(
            context.RecordSpend?.transform, "BackToSpend");
        Require(context, context.RecordFormScroll, "RecordSpendAdd Form ScrollRect");
        Require(context, context.RecordContent, "RecordSpendAdd Form Content");
        Require(context, context.RecordInputCard, "RecordSpendAdd Amount 입력 카드");
        Require(context, context.RecordAmountLabel, "RecordSpendAdd Amount 라벨");
        Require(context, context.RecordScreenTitle, "RecordSpendAdd 화면 제목");
        Require(context, context.RecordSaveButton, "RecordSpendAdd Save 버튼");
        Require(context, context.RecordBackButton, "RecordSpendAdd BackToSpend 버튼");
    }

    private static void ReadGoalSetting(Context context)
    {
        context.GoalSetting = FindUnique(
            context.Canvas.transform, "SpendAdd_1", context.Errors)?.gameObject;
        context.SpendListManager = context.Canvas.transform.Find("HabitTap/Managers")
            ?.GetComponent<SpendListManager>();
        Require(context, context.SpendListManager, "HabitTap/Managers/SpendListManager");
        if (context.SpendListManager != null)
        {
            SerializedObject serialized = new SerializedObject(context.SpendListManager);
            context.GoalNameInput = ReadReference<TMP_InputField>(serialized, "goalNameInput");
            context.GoalLimitInput = ReadReference<TMP_InputField>(serialized, "goldInput");
            context.GoalPreviewContent = ReadReference<Transform>(serialized, "previewContent");
            context.SpecialGoalPreviewTemplate = ReadReference<GameObject>(
                serialized, "specialGoalPreviewPrefab");
            context.MainGoalContent = ReadReference<Transform>(serialized, "mainContent");
            context.MainGoalTemplate = ReadReference<GameObject>(serialized, "spendItemPrefab");
            context.SkipButton = ReadReference<Button>(serialized, "skipButton");
            context.CompleteButton = ReadReference<Button>(serialized, "completeButton");
        }
        Require(context, context.GoalNameInput, "SpendAdd_1 goalNameInput 참조");
        Require(context, context.GoalLimitInput, "SpendAdd_1 goldInput 참조");
        Require(context, context.GoalPreviewContent, "SpendAdd_1 previewContent 참조");
        Require(context, context.SpecialGoalPreviewTemplate, "SpendAdd_1 specialGoalPreviewPrefab 참조");
        Require(context, context.MainGoalContent, "Spend mainContent 참조");
        Require(context, context.MainGoalTemplate, "Spend spendItemPrefab 참조");
        Require(context, context.SkipButton, "SpendAdd_1 skipButton 참조");
        Require(context, context.CompleteButton, "SpendAdd_1 completeButton 참조");

        context.SpendCycleManager = context.GoalSetting != null
            ? context.GoalSetting.GetComponentInChildren<SpendCycleManager>(true)
            : null;
        Require(context, context.SpendCycleManager, "SpendAdd_1/SpendCycleManager");
        if (context.SpendCycleManager != null)
        {
            context.DailyButton = context.SpendCycleManager.dailyButton;
            context.WeeklyButton = context.SpendCycleManager.weeklyButton;
        }
        context.AddGoalButton = FindButtonByLabel(context.GoalSetting?.transform, "Add Goal") ??
                                context.GoalSetting?.GetComponentsInChildren<Button>(true)
                                    .FirstOrDefault(item =>
                                        item.name.IndexOf("Add", StringComparison.OrdinalIgnoreCase) >= 0);
        context.GoalFormScroll = context.GoalSetting?.GetComponentsInChildren<ScrollRect>(true)
            .FirstOrDefault(item => item.content != context.GoalPreviewContent);
        context.GoalPreviewScroll = context.GoalSetting?.GetComponentsInChildren<ScrollRect>(true)
            .FirstOrDefault(item => item.content == context.GoalPreviewContent);
        Require(context, context.DailyButton, "SpendAdd_1 Daily 버튼");
        Require(context, context.WeeklyButton, "SpendAdd_1 Weekly 버튼");
        Require(context, context.AddGoalButton, "SpendAdd_1 Add Goal 버튼");
        Require(context, context.GoalFormScroll, "SpendAdd_1 Form ScrollRect");
        Require(context, context.GoalPreviewScroll, "SpendAdd_1 Preview ScrollRect");
    }

    private static void ReadHistory(Context context)
    {
        context.History = FindUnique(
            context.Canvas.transform, "SpendHistory", context.Errors)?.gameObject;
        context.TransactionManager = context.History != null
            ? context.History.GetComponent<SpendTransactionManager>()
            : null;
        Require(context, context.TransactionManager, "SpendHistory/SpendTransactionManager");
        if (context.TransactionManager != null)
        {
            SerializedObject serialized = new SerializedObject(context.TransactionManager);
            context.TransactionContent = ReadReference<Transform>(serialized, "content") as RectTransform;
            context.TransactionTemplate = ReadReference<GameObject>(serialized, "transactionItemPrefab");
            context.EmptyState = ReadReference<GameObject>(serialized, "emptyState");
        }
        Require(context, context.TransactionContent, "SpendHistory content 참조");
        Require(context, context.TransactionTemplate, "SpendHistory transactionItemPrefab 참조");
        context.TransactionScroll = context.History?.GetComponentsInChildren<ScrollRect>(true)
            .FirstOrDefault(item => item.content == context.TransactionContent);
        context.HistoryTitle = context.History?.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(item => item.name.Equals("title", StringComparison.OrdinalIgnoreCase));
        context.HistoryBackButton = FindButtonByPersistentMethod(
            context.History?.transform, "BackToSpendHistory");
        Require(context, context.TransactionScroll, "SpendHistory 거래 ScrollRect");
        Require(context, context.HistoryTitle, "SpendHistory 제목");
        Require(context, context.HistoryBackButton, "SpendHistory BackToSpendHistory 버튼");

        if (context.TransactionTemplate != null)
        {
            foreach (string name in new[] { "CategoryText", "AmountText", "DateText", "ExceptionToggle" })
            {
                if (context.TransactionTemplate.transform.Find(name) == null)
                    context.Errors.Add($"TransactionItem 런타임 조회 이름 '{name}'이 없습니다.");
            }
        }
    }

    private static void ApplyDesign(Context context)
    {
        EnsureMainDashboardPresentation(context);
        StyleMain(context);
        StyleBudgetSetting(context);
        StyleRecordSpend(context);
        StyleGoalSetting(context);
        StyleHistory(context);
        ConnectRuntimeTransactionStyle(context);
        EnsureEmptyState(context);

        LayoutRebuilder.ForceRebuildLayoutImmediate(context.Content);
        LayoutRebuilder.ForceRebuildLayoutImmediate(context.TransactionContent);
    }

    private static void EnsureMainDashboardPresentation(Context context)
    {
        context.BudgetTitle = EnsureDashboardText(context.BudgetCard, BudgetTitleName);
        context.RecordTitle = EnsureDashboardText(context.RecordCard, RecordTitleName);
        context.RewardTitle = EnsureDashboardText(context.RewardCard, RewardTitleName);
        context.GoalTitle = EnsureDashboardText(context.GoalListCard, GoalTitleName);
        context.BudgetFooter = EnsureDashboardContainer(context.BudgetCard, BudgetFooterName);
        context.BudgetUsageText = EnsureDashboardText(context.BudgetCard, BudgetUsageName);
        context.BudgetRemainingText = EnsureDashboardText(context.BudgetCard, BudgetRemainingName);
        ReparentAddedObject(context.BudgetUsageText.transform, context.BudgetFooter);
        ReparentAddedObject(context.BudgetRemainingText.transform, context.BudgetFooter);

        Transform existingEmptyState = context.GoalListCard.Find(MainGoalEmptyStateName);
        if (existingEmptyState == null)
        {
            GameObject emptyState = new GameObject(
                MainGoalEmptyStateName, typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(emptyState, "Create spending goal empty state");
            Undo.SetTransformParent(
                emptyState.transform, context.GoalListCard, "Parent spending goal empty state");
            context.MainGoalEmptyState = emptyState;
        }
        else
        {
            context.MainGoalEmptyState = existingEmptyState.gameObject;
            GetOrAdd<TextMeshProUGUI>(context.MainGoalEmptyState);
        }

        context.MainGoalEmptyState.transform.SetAsLastSibling();
        LayoutElement goalLayout = GetOrAdd<LayoutElement>(context.GoalListCard.gameObject);
        EnsureRewardCellStructure(context);
        context.DashboardPresenter = GetOrAdd<SpendingDashboardPresenter>(context.Spend);

        Undo.RecordObject(context.BudgetManager, "Connect spending budget summary text");
        SerializedObject budget = new SerializedObject(context.BudgetManager);
        budget.FindProperty("usageText").objectReferenceValue = context.BudgetUsageText;
        budget.FindProperty("remainingText").objectReferenceValue = context.BudgetRemainingText;
        budget.ApplyModifiedProperties();
        SaveOverride(context.BudgetManager);

        Undo.RecordObject(context.DashboardPresenter, "Connect spending dashboard presentation");
        SerializedObject presenter = new SerializedObject(context.DashboardPresenter);
        presenter.FindProperty("goalContent").objectReferenceValue = context.MainGoalContent;
        presenter.FindProperty("goalTemplate").objectReferenceValue = context.MainGoalTemplate;
        presenter.FindProperty("goalEmptyState").objectReferenceValue = context.MainGoalEmptyState;
        presenter.FindProperty("goalScroll").objectReferenceValue = context.GoalScroll;
        presenter.FindProperty("goalCardLayout").objectReferenceValue = goalLayout;
        presenter.FindProperty("baseRewardText").objectReferenceValue = context.BaseRewardText;
        presenter.FindProperty("bonusRewardText").objectReferenceValue = context.BonusRewardText;
        presenter.FindProperty("streakText").objectReferenceValue = context.StreakText;
        presenter.FindProperty("streakBonusText").objectReferenceValue = context.StreakBonusText;
        presenter.FindProperty("baseRewardDisplayLabel").objectReferenceValue =
            context.RewardDisplayLabels[0];
        presenter.FindProperty("bonusRewardDisplayLabel").objectReferenceValue =
            context.RewardDisplayLabels[1];
        presenter.FindProperty("streakDisplayLabel").objectReferenceValue =
            context.RewardDisplayLabels[2];
        presenter.FindProperty("streakBonusDisplayLabel").objectReferenceValue =
            context.RewardDisplayLabels[3];
        presenter.FindProperty("baseRewardDisplayText").objectReferenceValue =
            context.RewardDisplayTexts[0];
        presenter.FindProperty("bonusRewardDisplayText").objectReferenceValue =
            context.RewardDisplayTexts[1];
        presenter.FindProperty("streakDisplayText").objectReferenceValue =
            context.RewardDisplayTexts[2];
        presenter.FindProperty("streakBonusDisplayText").objectReferenceValue =
            context.RewardDisplayTexts[3];
        presenter.ApplyModifiedProperties();
        SaveOverride(context.DashboardPresenter);
    }

    private static void EnsureRewardCellStructure(Context context)
    {
        context.RewardGrid = EnsureDashboardContainer(context.RewardCard, RewardGridName);
        string[] suffixes = { "Base", "Bonus", "Streak", "StreakBonus" };

        for (int index = 0; index < suffixes.Length; index++)
        {
            RectTransform cell = EnsureDashboardContainer(
                context.RewardCard, RewardCellPrefix + suffixes[index]);
            ReparentAddedObject(cell, context.RewardGrid);
            context.RewardCells[index] = cell;
            context.RewardDisplayLabels[index] = EnsureDashboardText(cell, RewardLabelName);
            context.RewardDisplayTexts[index] = EnsureDashboardText(cell, RewardDisplayName);
        }
    }

    private static TMP_Text EnsureDashboardText(RectTransform parent, string name)
    {
        Transform existing = FindDescendant(parent, name);
        if (existing != null)
            return GetOrAdd<TextMeshProUGUI>(existing.gameObject);

        GameObject target = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(target, "Create spending dashboard text");
        Undo.SetTransformParent(target.transform, parent, "Parent spending dashboard text");
        return target.GetComponent<TMP_Text>();
    }

    private static RectTransform EnsureDashboardContainer(RectTransform parent, string name)
    {
        Transform existing = FindDescendant(parent, name);
        if (existing != null)
            return existing as RectTransform;

        GameObject target = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(target, "Create spending dashboard layout container");
        Undo.SetTransformParent(target.transform, parent, "Parent spending dashboard layout container");
        return target.GetComponent<RectTransform>();
    }

    private static void ReparentAddedObject(Transform child, Transform parent)
    {
        if (child == null || parent == null || child.parent == parent) return;
        Undo.SetTransformParent(child, parent, "Organize spending dashboard layout");
        RectTransform rect = child as RectTransform;
        if (rect != null)
        {
            Record(rect);
            rect.localScale = Vector3.one;
        }
    }

    private static void StyleMain(Context context)
    {
        Image background = GetOrAdd<Image>(context.Spend);
        StyleImage(background, null, Cream, Image.Type.Simple, true);
        StyleText(context.SpendingTitle, "소비 관리", context.Font, 48f, Brown,
            FontStyles.Bold, TextAlignmentOptions.Center, true);

        RectTransform scrollRect = context.Scroll.GetComponent<RectTransform>();
        Record(scrollRect);
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(24f, 150f);
        scrollRect.offsetMax = new Vector2(-24f, -72f);
        ConfigureVerticalScroll(context.Scroll, 35f, ScrollRect.MovementType.Elastic);

        Record(context.Content);
        context.Content.anchorMin = new Vector2(0f, 1f);
        context.Content.anchorMax = Vector2.one;
        context.Content.pivot = new Vector2(0.5f, 1f);
        context.Content.anchoredPosition = Vector2.zero;
        context.Content.sizeDelta = new Vector2(0f, context.Content.sizeDelta.y);
        ConfigureVerticalContent(
            context.Content, new RectOffset(14, 14, 24, 56), 32f, true);

        StyleCard(context.BudgetCard, 440f, context);
        StyleCard(context.RecordCard, 260f, context);
        StyleCard(context.GoalListCard, 310f, context);
        StyleCard(context.RewardCard, 620f, context);
        OrderDashboardCards(context);
        StyleButton(context.BudgetSettingButton, context.CreamButtonSprite, "예산 설정", context);
        StyleButton(context.RecordButton, context.GoldButtonSprite, "소비 기록하기", context);
        StyleButton(context.HistoryButton, context.CreamButtonSprite, "소비 내역 보기", context);
        StyleButton(context.ClaimButton, context.GoldButtonSprite, "보상 받기", context);

        foreach (TMP_Text text in context.Spend.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == context.SpendingTitle || text == context.BudgetText || text == context.UsedText) continue;
            StyleText(text, null, context.Font, Mathf.Max(text.fontSize, 27f),
                text.name.IndexOf("Gold", StringComparison.OrdinalIgnoreCase) >= 0 ? Gold : Brown,
                text.fontStyle, text.alignment, true);
        }

        StyleBudgetSummary(context);
        StyleRecordCard(context);
        StyleRewardCard(context);
        StyleGoalCard(context);
        StyleGoalScroll(context);
        StyleTemplate(context.MainGoalTemplate, 118f, context);
    }

    private static void StyleBudgetSummary(Context context)
    {
        RectTransform budgetLayout = context.BudgetCard.Find("BudgetLayout") as RectTransform;
        if (budgetLayout == null)
            throw new InvalidOperationException("BudgetCard/BudgetLayout을 찾을 수 없습니다.");

        SetStretch(budgetLayout, Vector2.zero, Vector2.zero);
        ConfigureVerticalLayoutGroup(
            budgetLayout.gameObject, new RectOffset(36, 36, 104, 48), 20f);
        SetActive(budgetLayout.Find("Title")?.gameObject, false);
        ReparentAddedObject(context.BudgetFooter, budgetLayout);
        Record(context.BudgetFooter);
        context.BudgetFooter.SetAsLastSibling();
        SaveOverride(context.BudgetFooter);

        StyleText(context.BudgetTitle, "이번 주 예산", context.Font, 37f, Brown,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
        SetTopLeft(context.BudgetTitle.rectTransform,
            new Vector2(520f, 58f), new Vector2(34f, -26f));

        RectTransform settingButton = context.BudgetSettingButton.GetComponent<RectTransform>();
        ConfigureIgnoreLayout(settingButton.gameObject, true);
        SetTopRight(settingButton, new Vector2(250f, 72f), new Vector2(-30f, -18f));

        StyleText(context.UsedText, null, context.Font, 50f, Brown,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
        ConfigureLayoutItem(context.UsedText.gameObject, 66f, 66f, 0f, false);

        StyleText(context.BudgetText, null, context.Font, 31f, MutedBrown,
            FontStyles.Bold, TextAlignmentOptions.MidlineRight, false);
        ConfigureLayoutItem(context.BudgetText.gameObject, 46f, 46f, 0f, false);

        StyleText(context.BudgetUsageText, "사용률 —", context.Font, 29f, MutedBrown,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
        ConfigureLayoutItem(context.BudgetUsageText.gameObject, 54f, 54f, 1f, false);

        StyleText(context.BudgetRemainingText, "예산을 설정해 주세요", context.Font, 29f,
            Green, FontStyles.Bold, TextAlignmentOptions.MidlineRight, false);
        ConfigureLayoutItem(context.BudgetRemainingText.gameObject, 54f, 54f, 1f, false);

        ConfigureHorizontalLayoutGroup(
            context.BudgetFooter.gameObject, new RectOffset(0, 0, 0, 0), 40f);
        ConfigureLayoutItem(context.BudgetFooter.gameObject, 58f, 58f, 0f, false);

        Slider slider = context.BudgetSlider;
        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        ConfigureLayoutItem(slider.gameObject, 36f, 36f, 0f, false);
        Image track = slider.transform.Find("Background")?.GetComponent<Image>();
        Image fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
        if (track != null) StyleImage(track, context.CreamButtonSprite, Cream, Image.Type.Sliced, false);
        if (fill != null) StyleImage(fill, context.GoldButtonSprite, Green, Image.Type.Sliced, false);
    }

    private static void OrderDashboardCards(Context context)
    {
        RectTransform[] cards =
        {
            context.BudgetCard,
            context.RecordCard,
            context.RewardCard,
            context.GoalListCard
        };
        if (cards.Any(card => card.parent != context.Content))
            throw new InvalidOperationException("소비 메인 카드가 동일한 Content 직계 자식이 아닙니다.");

        foreach (RectTransform card in cards)
        {
            Record(card);
            card.SetAsLastSibling();
            SaveOverride(card);
        }
    }

    private static void StyleRecordCard(Context context)
    {
        SetActive(context.RecordCard.Find("title")?.gameObject, false);
        ConfigureHorizontalLayoutGroup(
            context.RecordCard.gameObject, new RectOffset(34, 34, 120, 36), 24f);

        StyleText(context.RecordTitle, "소비 기록", context.Font, 36f, Brown,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
        ConfigureIgnoreLayout(context.RecordTitle.gameObject, true);
        SetTopLeft(context.RecordTitle.rectTransform,
            new Vector2(520f, 58f), new Vector2(34f, -22f));
        ConfigureLayoutItem(context.RecordButton.gameObject, 96f, 96f, 1f, false);
        ConfigureLayoutItem(context.HistoryButton.gameObject, 96f, 96f, 1f, false);
    }

    private static void StyleRewardCard(Context context)
    {
        DisableLayoutGroup<GridLayoutGroup>(context.RewardCard.gameObject);
        ConfigureIgnoreLayout(context.RewardGrid.gameObject, true);
        ConfigureRewardGrid(context.RewardGrid.gameObject);
        SetTopCentered(context.RewardGrid,
            new Vector2(880f, 240f), new Vector2(0f, -180f));

        StyleText(context.RewardTitle, "이번 주 예상 보상", context.Font, 37f, Brown,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
        ConfigureIgnoreLayout(context.RewardTitle.gameObject, true);
        SetTopLeft(context.RewardTitle.rectTransform,
            new Vector2(500f, 58f), new Vector2(34f, -24f));

        StyleText(context.RewardStatusText, null, context.Font, 28f, MutedBrown,
            FontStyles.Bold, TextAlignmentOptions.MidlineRight, false);
        ConfigureIgnoreLayout(context.RewardStatusText.gameObject, true);
        SetTopRight(context.RewardStatusText.rectTransform,
            new Vector2(360f, 54f), new Vector2(-34f, -28f));

        StyleText(context.TotalRewardText, null, context.Font, 42f, Gold,
            FontStyles.Bold, TextAlignmentOptions.Center, false);
        ConfigureIgnoreLayout(context.TotalRewardText.gameObject, true);
        SetTopCentered(context.TotalRewardText.rectTransform,
            new Vector2(760f, 70f), new Vector2(0f, -92f));

        TMP_Text[] sourceTexts =
        {
            context.BaseRewardText,
            context.BonusRewardText,
            context.StreakText,
            context.StreakBonusText
        };
        for (int index = 0; index < sourceTexts.Length; index++)
        {
            RectTransform cell = context.RewardCells[index];
            ConfigureIgnoreLayout(cell.gameObject, false);
            StyleImage(GetOrAdd<Image>(cell.gameObject), context.CardSprite,
                CreamLight, Image.Type.Sliced, false);
            Record(cell);
            cell.SetSiblingIndex(index);
            SaveOverride(cell);
            StyleRewardValue(sourceTexts[index], context.RewardDisplayLabels[index],
                context.RewardDisplayTexts[index], context);
        }

        ConfigureIgnoreLayout(context.ClaimButton.gameObject, true);
        SetTopCentered(context.ClaimButton.GetComponent<RectTransform>(),
            new Vector2(430f, 94f), new Vector2(0f, -490f));
    }

    private static void StyleRewardValue(TMP_Text source, TMP_Text label,
        TMP_Text display, Context context)
    {
        StyleText(source, null, context.Font, 29f, Brown,
            FontStyles.Bold, TextAlignmentOptions.Center, true);
        ConfigureIgnoreLayout(source.gameObject, true);
        SetActive(source.gameObject, false);

        StyleText(label, "—", context.Font, 25f, MutedBrown,
            FontStyles.Bold, TextAlignmentOptions.Center, true);
        SetTopCentered(label.rectTransform,
            new Vector2(384f, 42f), new Vector2(0f, -12f));
        Record(label);
        label.margin = new Vector4(8f, 4f, 8f, 2f);
        SaveOverride(label);

        StyleText(display, "—", context.Font, 31f, Brown,
            FontStyles.Bold, TextAlignmentOptions.Center, true);
        SetTopCentered(display.rectTransform,
            new Vector2(384f, 46f), new Vector2(0f, -56f));
        Record(display);
        display.margin = new Vector4(12f, 8f, 12f, 8f);
        SaveOverride(display);
    }

    private static void StyleGoalCard(Context context)
    {
        SetActive(context.GoalListCard.Find("Header")?.gameObject, false);
        ConfigureVerticalLayoutGroup(
            context.GoalListCard.gameObject, new RectOffset(30, 30, 96, 28), 0f);

        StyleText(context.GoalTitle, "특수 소비 목표", context.Font, 37f, Brown,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
        ConfigureIgnoreLayout(context.GoalTitle.gameObject, true);
        SetTopLeft(context.GoalTitle.rectTransform,
            new Vector2(620f, 58f), new Vector2(34f, -24f));
        ConfigureIgnoreLayout(context.MainGoalEmptyState, true);
        SetCentered(context.MainGoalEmptyState.GetComponent<RectTransform>(),
            new Vector2(760f, 130f), new Vector2(0f, -40f));
        StyleText(context.MainGoalEmptyState.GetComponent<TMP_Text>(),
            "등록된 목표가 없습니다\n새로운 목표를 설정해보세요", context.Font, 30f,
            MutedBrown, FontStyles.Bold, TextAlignmentOptions.Center, true);
    }

    private static void StyleBudgetSetting(Context context)
    {
        StyleImage(GetOrAdd<Image>(context.BudgetSetting), context.CardSprite, Cream,
            Image.Type.Sliced, true);
        SetCentered(context.BudgetSettingContent, new Vector2(960f, 1540f), new Vector2(0f, 20f));
        StyleText(context.BudgetSettingTitle, "주간 예산 설정", context.Font, 56f, Brown,
            FontStyles.Bold, TextAlignmentOptions.Center, true);
        SetTopCentered(context.BudgetSettingTitle.rectTransform,
            new Vector2(860f, 100f), new Vector2(0f, -48f));
        StyleText(context.BudgetDescription, "이번 주 소비 한도를 설정해보세요.", context.Font,
            34f, MutedBrown, FontStyles.Bold, TextAlignmentOptions.Center, true);
        SetTopCentered(context.BudgetDescription.rectTransform,
            new Vector2(860f, 72f), new Vector2(0f, -166f));

        StyleImage(context.BudgetSettingCard.GetComponent<Image>(), context.CardSprite,
            Color.white, Image.Type.Sliced, true);
        SetTopCentered(context.BudgetSettingCard,
            new Vector2(880f, 390f), new Vector2(0f, -270f));
        TMP_Text label = context.BudgetSettingCard.Find("BudgetLabel")?.GetComponent<TMP_Text>();
        if (label != null)
        {
            StyleText(label, "이번 주 예산", context.Font, 36f, Brown,
                FontStyles.Bold, TextAlignmentOptions.Center, true);
            SetTopCentered(label.rectTransform, new Vector2(740f, 68f), new Vector2(0f, -44f));
        }

        StyleInput(context.BudgetInput, "예: 500000", true, context);
        SetTopCentered(context.BudgetInput.GetComponent<RectTransform>(),
            new Vector2(720f, 126f), new Vector2(0f, -146f));
        StyleText(context.BudgetInput.textComponent, null, context.Font, 38f, Brown,
            FontStyles.Bold, TextAlignmentOptions.MidlineRight, false);
        if (context.BudgetInput.placeholder is TMP_Text placeholder)
            StyleText(placeholder, "예: 500000", context.Font, 32f, MutedBrown,
                FontStyles.Normal, TextAlignmentOptions.MidlineRight, false);
        Record(context.BudgetInput);
        ColorBlock inputColors = context.BudgetInput.colors;
        inputColors.disabledColor = new Color(0.88f, 0.82f, 0.70f, 1f);
        context.BudgetInput.colors = inputColors;
        SaveOverride(context.BudgetInput);

        StyleText(context.BudgetDescriptionSecondary,
            "주간 예산은 한 번만 설정할 수 있으며\n다음 주 월요일부터 다시 변경할 수 있습니다.", context.Font,
            30f, MutedBrown, FontStyles.Bold, TextAlignmentOptions.Center, true);
        SetTopCentered(context.BudgetDescriptionSecondary.rectTransform,
            new Vector2(860f, 116f), new Vector2(0f, -700f));
        Record(context.BudgetDescriptionSecondary);
        context.BudgetDescriptionSecondary.lineSpacing = 10f;
        SaveOverride(context.BudgetDescriptionSecondary);

        StyleButton(context.BudgetBackButton, context.CreamButtonSprite, "뒤로 가기", context);
        StyleButton(context.BudgetSaveButton, context.GoldButtonSprite, "예산 저장", context);
        StyleText(context.BudgetBackButton.GetComponentInChildren<TMP_Text>(true), null,
            context.Font, 32f, Brown, FontStyles.Bold, TextAlignmentOptions.Center, false);
        StyleText(context.BudgetSaveButton.GetComponentInChildren<TMP_Text>(true), null,
            context.Font, 32f, Brown, FontStyles.Bold, TextAlignmentOptions.Center, false);
        SetTopCentered(context.BudgetBackButton.GetComponent<RectTransform>(),
            new Vector2(390f, 116f), new Vector2(-215f, -850f));
        SetTopCentered(context.BudgetSaveButton.GetComponent<RectTransform>(),
            new Vector2(390f, 116f), new Vector2(215f, -850f));
    }

    private static void StyleRecordSpend(Context context)
    {
        EnsureRecordSpendPresentation(context);
        StyleImage(GetOrAdd<Image>(context.RecordSpend), null, Cream,
            Image.Type.Simple, true);
        StyleText(context.RecordScreenTitle, "소비 기록 추가", context.Font, 46f, Brown,
            FontStyles.Bold, TextAlignmentOptions.Center, false);
        StyleText(context.RecordSubtitle, "오늘의 소비를 기록하세요", context.Font, 32f,
            MutedBrown, FontStyles.Bold, TextAlignmentOptions.Center, false);

        ConfigureVerticalScroll(context.RecordFormScroll, 38f, ScrollRect.MovementType.Clamped);
        RectTransform formRect = context.RecordFormScroll.GetComponent<RectTransform>();
        Record(formRect);
        formRect.anchorMin = Vector2.zero;
        formRect.anchorMax = Vector2.one;
        formRect.offsetMin = new Vector2(24f, 24f);
        formRect.offsetMax = new Vector2(-24f, -160f);
        SaveOverride(formRect);

        DisableLayoutGroup<VerticalLayoutGroup>(context.RecordContent.gameObject);
        ContentSizeFitter contentFitter = context.RecordContent.GetComponent<ContentSizeFitter>();
        if (contentFitter != null && contentFitter.enabled)
        {
            Record(contentFitter);
            contentFitter.enabled = false;
            SaveOverride(contentFitter);
        }
        Record(context.RecordContent);
        context.RecordContent.anchorMin = new Vector2(0f, 1f);
        context.RecordContent.anchorMax = Vector2.one;
        context.RecordContent.pivot = new Vector2(0.5f, 1f);
        context.RecordContent.anchoredPosition = Vector2.zero;
        context.RecordContent.sizeDelta = new Vector2(0f, 1240f);
        SaveOverride(context.RecordContent);

        SetTopCentered(context.RecordSubtitle.rectTransform,
            new Vector2(900f, 64f), new Vector2(0f, -44f));
        StyleImage(GetOrAdd<Image>(context.RecordInputCard.gameObject), context.CardSprite,
            CreamLight, Image.Type.Sliced, true);
        SetTopCentered(context.RecordInputCard,
            new Vector2(880f, 620f), new Vector2(0f, -138f));

        StyleText(context.RecordAmountLabel, "소비 금액", context.Font, 31f, Brown,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
        SetTopCentered(context.RecordAmountLabel.rectTransform,
            new Vector2(740f, 54f), new Vector2(0f, -56f));
        StyleInput(context.RecordAmountInput, "금액을 입력하세요", true, context);
        SetTopCentered(context.RecordAmountInput.GetComponent<RectTransform>(),
            new Vector2(740f, 118f), new Vector2(0f, -122f));
        Record(context.RecordAmountInput);
        context.RecordAmountInput.lineType = TMP_InputField.LineType.SingleLine;
        context.RecordAmountInput.customCaretColor = true;
        context.RecordAmountInput.caretColor = Brown;
        context.RecordAmountInput.selectionColor = new Color32(0xE7, 0xA8, 0x35, 0x66);
        SaveOverride(context.RecordAmountInput);
        if (context.RecordAmountInput.textViewport != null)
            SetStretch(context.RecordAmountInput.textViewport,
                new Vector2(72f, 10f), new Vector2(-24f, -10f));
        StyleText(context.RecordCurrency, "₩", context.Font, 34f, Brown,
            FontStyles.Bold, TextAlignmentOptions.Center, false);
        SetTopLeft(context.RecordCurrency.rectTransform,
            new Vector2(56f, 80f), new Vector2(18f, -19f));

        StyleText(context.RecordCategoryLabel, "소비 카테고리", context.Font, 31f, Brown,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
        SetTopCentered(context.RecordCategoryLabel.rectTransform,
            new Vector2(740f, 54f), new Vector2(0f, -292f));
        StyleDropdown(context.RecordCategoryDropdown, context);
        SetTopCentered(context.RecordCategoryDropdown.GetComponent<RectTransform>(),
            new Vector2(740f, 118f), new Vector2(0f, -492f));

        StyleButton(context.RecordBackButton, context.CreamButtonSprite, "뒤로", context);
        StyleButton(context.RecordSaveButton, context.GoldButtonSprite, "소비 기록 저장", context);
        ApplyRpgSelectableColors(context.RecordBackButton);
        ApplyRpgSelectableColors(context.RecordSaveButton);
        SetTopCentered(context.RecordSaveButton.GetComponent<RectTransform>(),
            new Vector2(700f, 124f), new Vector2(0f, -830f));
    }

    private static void EnsureRecordSpendPresentation(Context context)
    {
        context.RecordSubtitle = EnsureDashboardText(context.RecordContent, RecordSubtitleName);
        context.RecordCategoryLabel = EnsureDashboardText(
            context.RecordInputCard, RecordCategoryLabelName);
        context.RecordCurrency = EnsureDashboardText(
            context.RecordAmountInput.GetComponent<RectTransform>(), RecordCurrencyName);
        context.RecordCategoryPresenter = GetOrAdd<SpendCategoryDropdownPresenter>(
            context.RecordCategoryDropdown.gameObject);
    }

    // 15-D-8A SpendAdd_1: the original UI is part of Canvas.prefab. Unity does not
    // allow a scene instance to reparent its original prefab children. Keep every
    // existing input/button/preview/reward under Content; position *added* cards
    // behind them as decoration. No prefab unpacking or prefab asset modification.
    private static void StyleGoalSetting(Context context)
    {
        RectTransform content = context.GoalFormScroll.content;
        if (content == null)
            throw new InvalidOperationException("SpendAdd_1 Form Content가 없습니다.");

        Transform header = FindDescendant(context.GoalSetting.transform, "TopHeader");
        Transform title = header?.Find("title");
        Transform goalName = context.GoalNameInput.transform.parent;
        Transform amount = context.GoalLimitInput.transform.parent;
        Transform addButton = context.AddGoalButton.transform;
        Transform preview = FindDescendant(context.GoalSetting.transform, "GoalList");
        Transform reward = FindDescendant(context.GoalSetting.transform, "Reward");
        Transform actions = FindDescendant(context.GoalSetting.transform, "BottomAction");
        Transform guide = FindDescendant(context.GoalSetting.transform, "GuideText");
        if (header == null || title == null || goalName == null || amount == null ||
            addButton == null || preview == null || reward == null || actions == null)
            throw new InvalidOperationException("SpendAdd_1 기존 UI 구성 요소가 누락되었습니다.");

        // Check rather than attempt to reparent existing Prefab children.
        foreach (Transform original in new[] { goalName, amount, addButton, preview, reward, actions })
            if (original.parent != content)
                throw new InvalidOperationException(
                    $"SpendAdd_1 기존 Prefab 자식의 부모가 예상과 다릅니다: {GetHierarchyPath(original)}. " +
                    "원본 Prefab 구조를 자동 변경하지 않습니다.");

        StyleImage(GetOrAdd<Image>(context.GoalSetting), null, Cream, Image.Type.Simple, true);
        StyleImage(GetOrAdd<Image>(header.gameObject), context.CardSprite, CreamLight,
            Image.Type.Sliced, false);
        SetTopCentered(header as RectTransform, new Vector2(984f, 236f), new Vector2(0f, -36f));
        ConfigureVerticalLayoutGroup(header.gameObject, new RectOffset(44, 44, 28, 24), 8f);
        context.SpendAddTitle = title.GetComponent<TMP_Text>();
        context.SpendAddSubtitle = EnsureDashboardText(header as RectTransform, SpendAddSubtitleName);
        context.SpendAddOptional = EnsureDashboardText(header as RectTransform, SpendAddOptionalName);
        StyleText(context.SpendAddTitle, "특수 소비 목표 설정", context.Font, 42f, Brown,
            FontStyles.Bold, TextAlignmentOptions.Center, false);
        StyleText(context.SpendAddSubtitle, "이번 주 도전할 소비 목표를 추가하세요.", context.Font,
            28f, Brown, FontStyles.Normal, TextAlignmentOptions.Center, false);
        StyleText(context.SpendAddOptional, "목표 없이 건너뛰어도 됩니다.", context.Font,
            24f, MutedBrown, FontStyles.Normal, TextAlignmentOptions.Center, false);
        ConfigureLayoutItem(context.SpendAddTitle.gameObject, 62f, 62f, 0f, false);
        ConfigureLayoutItem(context.SpendAddSubtitle.gameObject, 46f, 46f, 0f, false);
        ConfigureLayoutItem(context.SpendAddOptional.gameObject, 40f, 40f, 0f, false);

        SetStretch(context.GoalFormScroll.GetComponent<RectTransform>(),
            new Vector2(42f, 54f), new Vector2(-42f, -300f));
        ConfigureVerticalScroll(context.GoalFormScroll, 32f, ScrollRect.MovementType.Clamped);
        // Use one stable scrollable coordinate system. Existing Prefab siblings can
        // receive RectTransform overrides, but their parent hierarchy cannot change.
        DisableLayoutGroup<VerticalLayoutGroup>(content.gameObject);
        DisableLayoutGroup<HorizontalLayoutGroup>(content.gameObject);
        DisableSpendAddContentFitter(content.gameObject);
        Record(content);
        // The original Content used horizontal stretch with zero sizeDelta.
        // In the inactive prefab-based Edit Mode hierarchy the Viewport can
        // report zero width, which makes Content.rect.width zero as well.
        // Use a definite width within the 1080px reference Canvas so that the
        // three 940px cards and the 880px BottomAction have valid bounds.
        // Keep the ScrollRect and original prefab hierarchy unchanged.
        content.anchorMin = new Vector2(0.5f, 1f);
        content.anchorMax = new Vector2(0.5f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(996f, 1820f);
        SaveOverride(content);
        Image contentImage = content.GetComponent<Image>();
        if (contentImage != null)
            StyleImage(contentImage, null, Cream, Image.Type.Simple, false);

        context.GoalInputCard = EnsureDashboardContainer(content, SpendAddInputCardName);
        context.GoalPreviewCard = EnsureDashboardContainer(content, SpendAddPreviewCardName);
        context.GoalRewardCard = EnsureDashboardContainer(content, SpendAddRewardCardName);
        foreach (RectTransform card in new[]
                 { context.GoalInputCard, context.GoalPreviewCard, context.GoalRewardCard })
        {
            if (card.parent != content)
                throw new InvalidOperationException($"SpendAdd_1 장식 카드 부모 불일치: {card.name}");
            DisableLayoutGroup<VerticalLayoutGroup>(card.gameObject);
            DisableLayoutGroup<HorizontalLayoutGroup>(card.gameObject);
            DisableSpendAddContentFitter(card.gameObject);
            StyleImage(GetOrAdd<Image>(card.gameObject), context.CardSprite, CreamLight,
                Image.Type.Sliced, false);
        }
        SetTopCentered(context.GoalInputCard, new Vector2(940f, 680f), new Vector2(0f, -24f));
        SetTopCentered(context.GoalPreviewCard, new Vector2(940f, 620f), new Vector2(0f, -736f));
        SetTopCentered(context.GoalRewardCard, new Vector2(940f, 190f), new Vector2(0f, -1384f));
        // Only newly added card objects are reordered; prefab children are untouched.
        context.GoalRewardCard.SetAsFirstSibling();
        context.GoalPreviewCard.SetAsFirstSibling();
        context.GoalInputCard.SetAsFirstSibling();

        context.GoalInputTitle = EnsureDashboardText(context.GoalInputCard, SpendAddInputTitleName);
        context.GoalPreviewTitle = EnsureDashboardText(context.GoalPreviewCard, SpendAddPreviewTitleName);
        TMP_Text nameLabel = EnsureDashboardText(goalName as RectTransform, SpendAddNameLabelName);
        TMP_Text amountLabel = EnsureDashboardText(amount as RectTransform, SpendAddAmountLabelName);
        TMP_Text rewardTitle = EnsureDashboardText(context.GoalRewardCard, "__15D8A_RewardTitle");
        StyleText(context.GoalInputTitle, "새 목표 추가", context.Font, 34f, Brown,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
        StyleText(context.GoalPreviewTitle, "이번 주 추가한 목표", context.Font, 32f, Brown,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
        StyleText(rewardTitle, "특수 목표 완료 보상", context.Font, 30f, Brown,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
        StyleText(nameLabel, "목표 이름", context.Font, 30f, Brown,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
        StyleText(amountLabel, "목표 금액", context.Font, 30f, Brown,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
        SetActive(context.GoalInputTitle.gameObject, true);
        SetActive(context.GoalPreviewTitle.gameObject, true);
        SetActive(rewardTitle.gameObject, true);
        SetActive(nameLabel.gameObject, true);
        SetActive(amountLabel.gameObject, true);
        SetTopCentered(context.GoalInputTitle.rectTransform,
            new Vector2(840f, 54f), new Vector2(0f, -48f));
        SetTopCentered(context.GoalPreviewTitle.rectTransform,
            new Vector2(840f, 60f), new Vector2(0f, -32f));
        SetTopCentered(rewardTitle.rectTransform,
            new Vector2(840f, 50f), new Vector2(0f, -20f));

        DisableLayoutGroup<VerticalLayoutGroup>(goalName.gameObject);
        DisableLayoutGroup<HorizontalLayoutGroup>(goalName.gameObject);
        DisableSpendAddContentFitter(goalName.gameObject);
        DisableLayoutGroup<VerticalLayoutGroup>(amount.gameObject);
        DisableLayoutGroup<HorizontalLayoutGroup>(amount.gameObject);
        DisableSpendAddContentFitter(amount.gameObject);
        SetTopCentered(goalName as RectTransform,
            new Vector2(820f, 158f), new Vector2(0f, -174f));
        SetTopCentered(amount as RectTransform,
            new Vector2(820f, 158f), new Vector2(0f, -364f));
        SetTopCentered(nameLabel.rectTransform,
            new Vector2(760f, 44f), new Vector2(0f, -4f));
        SetTopCentered(amountLabel.rectTransform,
            new Vector2(760f, 44f), new Vector2(0f, -4f));
        StyleInput(context.GoalNameInput, "예: 카페 지출 줄이기", false, context);
        StyleInput(context.GoalLimitInput, "예: 30000", true, context);
        SetTopCentered(context.GoalNameInput.GetComponent<RectTransform>(),
            new Vector2(760f, 92f), new Vector2(0f, -58f));
        SetTopCentered(context.GoalLimitInput.GetComponent<RectTransform>(),
            new Vector2(760f, 92f), new Vector2(0f, -58f));
        StyleButton(context.AddGoalButton, context.GoldButtonSprite, "목표 추가", context);
        SetTopCentered(context.AddGoalButton.GetComponent<RectTransform>(),
            new Vector2(780f, 104f), new Vector2(0f, -574f));

        // Existing Preview and Reward stay in their original Content parent.
        // Card backgrounds and titles are independent, non-raycast decoration.
        DisableLayoutGroup<VerticalLayoutGroup>(preview.gameObject);
        DisableSpendAddContentFitter(preview.gameObject);
        SetTopCentered(preview as RectTransform,
            new Vector2(840f, 444f), new Vector2(0f, -856f));
        ConfigureVerticalScroll(context.GoalPreviewScroll, 30f, ScrollRect.MovementType.Clamped);
        if (context.GoalPreviewScroll.transform.parent == preview)
            SetTopCentered(context.GoalPreviewScroll.GetComponent<RectTransform>(),
                new Vector2(800f, 344f), new Vector2(0f, -30f));
        ConfigureVerticalContent(context.GoalPreviewContent as RectTransform,
            new RectOffset(12, 12, 12, 18), 14f);
        StyleTemplate(context.SpecialGoalPreviewTemplate, 126f, context);
        Image previewImage = preview.GetComponent<Image>();
        if (previewImage != null)
            StyleImage(previewImage, null, new Color(1f, 1f, 1f, 0f), Image.Type.Simple, false);
        Image previewScrollImage = context.GoalPreviewScroll.GetComponent<Image>();
        if (previewScrollImage != null)
            StyleImage(previewScrollImage, null, CreamLight, Image.Type.Simple, false);

        // Reward is an original prefab child. Its text can retain absolute legacy
        // anchors after moving to the new visual card, so style AND position the
        // existing runtime text components. Never replace the reward amount/text.
        DisableLayoutGroup<VerticalLayoutGroup>(reward.gameObject);
        DisableLayoutGroup<HorizontalLayoutGroup>(reward.gameObject);
        DisableSpendAddContentFitter(reward.gameObject);
        SetTopCentered(reward as RectTransform,
            new Vector2(840f, 90f), new Vector2(0f, -1460f));
        TMP_Text oldRewardTitle = reward.Find("Label")?.GetComponent<TMP_Text>();
        if (oldRewardTitle != null)
            SetActive(oldRewardTitle.gameObject, false);

        // The actual Canvas prefab keeps RewardText under Reward/RewardBox.
        // In v6 only direct Reward children were positioned, so RewardText
        // retained legacy offsets and protruded below GoalRewardCard by 96px.
        // Keep RewardBox and RewardText in their existing prefab hierarchy;
        // only override their RectTransforms inside the existing Reward area.
        RectTransform rewardBox = reward.Find("RewardBox") as RectTransform;
        if (rewardBox != null)
        {
            DisableLayoutGroup<VerticalLayoutGroup>(rewardBox.gameObject);
            DisableLayoutGroup<HorizontalLayoutGroup>(rewardBox.gameObject);
            DisableLayoutGroup<GridLayoutGroup>(rewardBox.gameObject);
            DisableSpendAddContentFitter(rewardBox.gameObject);
            SetTopCentered(rewardBox, new Vector2(812f, 82f), new Vector2(0f, -4f));
        }

        TMP_Text[] rewardValues = reward.GetComponentsInChildren<TMP_Text>(true)
            .Where(item => item != oldRewardTitle && IsActiveBelow(item.transform, reward))
            .ToArray();
        for (int index = 0; index < rewardValues.Length; index++)
        {
            TMP_Text value = rewardValues[index];
            StyleText(value, null, context.Font,
                rewardValues.Length == 1 ? 32f : 29f,
                Gold, FontStyles.Bold, TextAlignmentOptions.Center, true);
            DisableSpendAddContentFitter(value.gameObject);
            if (value.transform.parent == rewardBox && rewardBox != null)
            {
                // The reward value fills its existing RewardBox; no text/data
                // replacement and no prefab-child reparenting is necessary.
                SetStretch(value.rectTransform,
                    new Vector2(12f, 4f), new Vector2(-12f, -4f));
            }
            else if (value.transform.parent == reward)
            {
                float rowHeight = 90f / Mathf.Max(1, rewardValues.Length);
                SetTopCentered(value.rectTransform,
                    new Vector2(800f, rowHeight),
                    new Vector2(0f, -index * rowHeight));
            }
        }

        ConfigureHorizontalLayoutGroup(actions.gameObject,
            new RectOffset(24, 24, 14, 14), 28f);
        // Prefab-origin BottomAction may carry a ContentSizeFitter that overrides
        // the explicitly assigned RectTransform during the Edit Mode rebuild.
        DisableSpendAddContentFitter(actions.gameObject);
        SetTopCentered(actions as RectTransform,
            new Vector2(880f, 136f), new Vector2(0f, -1608f));
        StyleButton(context.SkipButton, context.CreamButtonSprite, "건너뛰기", context);
        StyleButton(context.CompleteButton, context.GoldButtonSprite, "설정 완료", context);
        ConfigureLayoutItem(context.SkipButton.gameObject, 108f, 108f, 1f, false);
        ConfigureLayoutItem(context.CompleteButton.gameObject, 108f, 108f, 1f, false);

        SetActive(goalName.gameObject, true);
        SetActive(amount.gameObject, true);
        SetActive(addButton.gameObject, true);
        SetActive(preview.gameObject, true);
        SetActive(reward.gameObject, true);
        SetActive(actions.gameObject, true);
        SetActive(context.DailyButton.gameObject, false);
        SetActive(context.WeeklyButton.gameObject, false);
        SetActive(guide?.gameObject, false);
        foreach (string obsoleteName in new[]
                 { SpendAddTitleName, SpendAddSubtitleName, SpendAddOptionalName,
                     SpendAddNameLabelName, SpendAddAmountLabelName, SpendAddPreviewTitleName })
            foreach (Transform obsolete in context.GoalSetting.GetComponentsInChildren<Transform>(true)
                         .Where(item => item.name == obsoleteName))
            {
                bool keep = obsolete == context.SpendAddSubtitle.transform ||
                            obsolete == context.SpendAddOptional.transform ||
                            obsolete == context.GoalPreviewTitle.transform ||
                            obsolete == nameLabel.transform || obsolete == amountLabel.transform;
                if (!keep) SetActive(obsolete.gameObject, false);
            }
        foreach (TMP_Text text in context.GoalSetting.GetComponentsInChildren<TMP_Text>(true))
            if (text != context.GoalPreviewTitle && text.text == "이번 주 추가한 목표")
                SetActive(text.gameObject, false);
        if (goalName.Find("Label")?.GetComponent<TMP_Text>() is TMP_Text oldNameLabel)
            SetActive(oldNameLabel.gameObject, false);
        if (amount.Find("Label")?.GetComponent<TMP_Text>() is TMP_Text oldAmountLabel)
            SetActive(oldAmountLabel.gameObject, false);
        if (preview.Find("Label")?.GetComponent<TMP_Text>() is TMP_Text oldPreviewLabel)
            SetActive(oldPreviewLabel.gameObject, false);

        TMP_Text emptyState = context.GoalPreviewContent.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(item => item.name == SpendAddEmptyStateName);
        if (emptyState == null)
            emptyState = EnsureDashboardText(context.GoalPreviewContent as RectTransform,
                SpendAddEmptyStateName);
        StyleText(emptyState, "아직 추가한 목표가 없습니다.", context.Font, 34f,
            Brown, FontStyles.Bold, TextAlignmentOptions.Center, false);
        ConfigureLayoutItem(emptyState.gameObject, 280f, 280f, 0f, false);
        SpendGoalPreviewEmptyState presenter =
            GetOrAdd<SpendGoalPreviewEmptyState>(context.GoalSetting);
        Record(presenter);
        presenter.Bind(context.GoalPreviewContent, emptyState.gameObject,
            context.SpecialGoalPreviewTemplate);
        SaveOverride(presenter);
        foreach (RectTransform child in context.GoalPreviewContent.Cast<Transform>().OfType<RectTransform>())
            if (child.gameObject != emptyState.gameObject)
                StyleTemplate(child.gameObject, 126f, context);

        // Correct actual geometry only after the existing prefab UI has been styled.
        // This preserves the original parent hierarchy, listeners and all data bindings.
        FitSpendAddBottomAction(context, content, actions as RectTransform);
    }

    /// <summary>
    /// Adjust the original BottomAction within the actual Form Content rect, rather
    /// than assuming prefab-driven RectTransforms match their sizeDelta in Edit Mode.
    /// Keep a visible gap after the reward card, and grow Content only if needed.
    /// </summary>
    private static void FitSpendAddBottomAction(Context context, RectTransform content,
        RectTransform actions)
    {
        List<GameObject> activated = ActivateHierarchyForLayout(context.GoalSetting.transform);
        try
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            LayoutRebuilder.ForceRebuildLayoutImmediate(actions);

            const float sidePadding = 28f;
            const float bottomPadding = 176f;
            Rect available = content.rect;
            Rect actionBounds = GetRectInReference(content, actions);
            Rect rewardBounds = GetRectInReference(content, context.GoalRewardCard);

            // Do not expand an uninitialized canvas into an enormous blank area.
            if (available.width < 940f || available.height < 1000f)
                throw new InvalidOperationException(
                    "SpendAdd_1 Content 크기를 확정할 수 없습니다. " +
                    $"content={FormatRect(available)}, actions={FormatRect(actionBounds)}, " +
                    $"contentAnchors={content.anchorMin}..{content.anchorMax}, " +
                    $"contentSizeDelta={content.sizeDelta}");

            float maxActionWidth = available.width - sidePadding * 2f;
            if (actionBounds.width > maxActionWidth + 1f)
            {
                Record(actions);
                actions.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    maxActionWidth);
                SaveOverride(actions);
                LayoutRebuilder.ForceRebuildLayoutImmediate(actions);
                actionBounds = GetRectInReference(content, actions);
            }

            // Align the row horizontally in Content and 32px below the reward card.
            // Both measurements use the same Content-local coordinate system.
            Vector2 correction = new Vector2(
                available.center.x - actionBounds.center.x,
                (rewardBounds.yMin - 32f) - actionBounds.yMax);
            if (correction.sqrMagnitude > 0.25f)
            {
                Record(actions);
                actions.anchoredPosition += correction;
                SaveOverride(actions);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(actions);
            actionBounds = GetRectInReference(content, actions);
            float requiredHeight = available.yMax - actionBounds.yMin + bottomPadding;
            if (requiredHeight > content.rect.height + 1f)
            {
                if (requiredHeight > 3000f)
                    throw new InvalidOperationException(
                        "SpendAdd_1 하단 버튼의 높이가 비정상입니다. " +
                        $"content={FormatRect(content.rect)}, " +
                        $"actions={FormatRect(actionBounds)}, requiredHeight={requiredHeight:0.##}");
                Record(content);
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, requiredHeight);
                SaveOverride(content);
            }

            // Retain strict geometry validation, but include the actual two rectangles.
            if (!ContainsRect(content.rect, GetRectInReference(content, actions)))
                throw new InvalidOperationException(
                    "SpendAdd_1 하단 버튼 자동 정렬 후에도 Content 경계를 벗어납니다. " +
                    $"content={FormatRect(content.rect)}, " +
                    $"actions={FormatRect(GetRectInReference(content, actions))}, " +
                    $"anchors={actions.anchorMin}..{actions.anchorMax}, " +
                    $"pivot={actions.pivot}, scale={actions.localScale}");
        }
        finally
        {
            RestoreActivatedHierarchy(activated);
        }
    }

    private static void DisableSpendAddContentFitter(GameObject target)
    {
        ContentSizeFitter fitter = target.GetComponent<ContentSizeFitter>();
        if (fitter == null || !fitter.enabled) return;
        Record(fitter);
        fitter.enabled = false;
        SaveOverride(fitter);
    }

    private static void MoveBefore(Transform item, Transform expectedParent, int siblingIndex)
    {
        if (item == null || item.parent != expectedParent) return;
        Record(item);
        item.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, expectedParent.childCount - 1));
        SaveOverride(item);
    }

    private static void StyleHistory(Context context)
    {
        StyleImage(GetOrAdd<Image>(context.History), null, Cream, Image.Type.Simple, true);
        StyleText(context.HistoryTitle, "소비 내역", context.Font, 48f, Brown,
            FontStyles.Bold, TextAlignmentOptions.Center, true);
        SetTopCentered(context.HistoryTitle.rectTransform,
            new Vector2(700f, 88f), new Vector2(70f, -42f));
        ConfigureVerticalScroll(context.TransactionScroll, 35f, ScrollRect.MovementType.Elastic);
        StyleImage(context.TransactionScroll.GetComponent<Image>(), null, CreamLight,
            Image.Type.Simple, true);
        if (context.TransactionScroll.viewport != null)
        {
            Image viewportImage = context.TransactionScroll.viewport.GetComponent<Image>();
            if (viewportImage != null)
                StyleImage(viewportImage, null, CreamLight, Image.Type.Simple, true);
        }
        RectTransform scrollRect = context.TransactionScroll.GetComponent<RectTransform>();
        Record(scrollRect);
        SetStretch(scrollRect, new Vector2(42f, 150f), new Vector2(-42f, -150f));
        ConfigureVerticalContent(context.TransactionContent,
            new RectOffset(18, 18, 18, 24), 18f);
        StyleButton(context.HistoryBackButton, context.CreamButtonSprite, "← 뒤로", context);
        RectTransform back = context.HistoryBackButton.GetComponent<RectTransform>();
        Record(back);
        back.anchorMin = back.anchorMax = back.pivot = new Vector2(0f, 1f);
        back.sizeDelta = new Vector2(190f, 78f);
        back.anchoredPosition = new Vector2(36f, -38f);
        StyleTransactionTemplate(context.TransactionTemplate, context);
    }

    private static void ConnectRuntimeTransactionStyle(Context context)
    {
        SerializedObject serialized = new SerializedObject(context.TransactionManager);
        SerializedProperty fontProperty = serialized.FindProperty("itemFont");
        SerializedProperty cardProperty = serialized.FindProperty("itemCardSprite");
        if (fontProperty == null || cardProperty == null)
            throw new InvalidOperationException(
                "SpendTransactionManager 런타임 카드 스타일 필드를 찾을 수 없습니다.");

        Undo.RecordObject(context.TransactionManager, "Connect 15-D-8A runtime transaction style");
        fontProperty.objectReferenceValue = context.Font;
        cardProperty.objectReferenceValue = context.CardSprite;
        serialized.ApplyModifiedProperties();
        SaveOverride(context.TransactionManager);
    }

    private static void EnsureEmptyState(Context context)
    {
        Transform parent = context.TransactionScroll.viewport != null
            ? context.TransactionScroll.viewport
            : context.History.transform;
        Transform[] matches = parent.GetComponentsInChildren<Transform>(true)
            .Where(item => item.name == EmptyStateName).ToArray();
        if (matches.Length > 1)
            throw new InvalidOperationException($"{EmptyStateName} 오브젝트가 중복되어 있습니다.");

        GameObject target;
        TMP_Text legacyText;
        if (matches.Length == 0)
        {
            target = new GameObject(EmptyStateName, typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(target, "Create 15-D-8A empty spending history state");
            Undo.SetTransformParent(target.transform, parent, "Parent 15-D-8A empty spending history state");
            legacyText = target.GetComponent<TMP_Text>();
        }
        else
        {
            target = matches[0].gameObject;
            legacyText = GetOrAdd<TextMeshProUGUI>(target);
        }

        target.transform.SetAsLastSibling();
        SetCentered(target.GetComponent<RectTransform>(), new Vector2(820f, 460f), Vector2.zero);
        StyleText(legacyText, string.Empty, context.Font, 1f,
            MutedBrown, FontStyles.Normal, TextAlignmentOptions.Center, true);
        legacyText.raycastTarget = false;

        GameObject card = EnsureEmptyStateObject(target.transform, EmptyStateCardName, true);
        SetStretch(card.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        StyleImage(card.GetComponent<Image>(), context.CardSprite, CreamLight, Image.Type.Sliced, false);
        card.transform.SetAsFirstSibling();

        GameObject badge = EnsureEmptyStateObject(target.transform, EmptyStateBadgeName, true);
        SetCentered(badge.GetComponent<RectTransform>(), new Vector2(174f, 76f), new Vector2(0f, 126f));
        StyleImage(badge.GetComponent<Image>(), context.GoldButtonSprite, Color.white,
            Image.Type.Sliced, false);
        GameObject badgeLabel = EnsureEmptyStateObject(badge.transform, EmptyStateBadgeLabelName, false);
        SetStretch(badgeLabel.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        StyleText(badgeLabel.GetComponent<TMP_Text>(), "기록", context.Font, 27f, Brown,
            FontStyles.Bold, TextAlignmentOptions.Center, true);
        badgeLabel.GetComponent<TMP_Text>().raycastTarget = false;

        GameObject title = EnsureEmptyStateObject(target.transform, EmptyStateTitleName, false);
        SetCentered(title.GetComponent<RectTransform>(), new Vector2(720f, 88f), new Vector2(0f, 22f));
        StyleText(title.GetComponent<TMP_Text>(), "아직 소비 내역이 없습니다", context.Font, 46f,
            Brown, FontStyles.Bold, TextAlignmentOptions.Center, true);
        title.GetComponent<TMP_Text>().raycastTarget = false;

        GameObject body = EnsureEmptyStateObject(target.transform, EmptyStateBodyName, false);
        SetCentered(body.GetComponent<RectTransform>(), new Vector2(720f, 132f), new Vector2(0f, -94f));
        TMP_Text bodyText = body.GetComponent<TMP_Text>();
        StyleText(bodyText, "소비 기록이 등록되면\n이곳에서 확인할 수 있습니다", context.Font, 34f,
            MutedBrown, FontStyles.Normal, TextAlignmentOptions.Center, true);
        Record(bodyText);
        bodyText.lineSpacing = 12f;
        bodyText.raycastTarget = false;
        SaveOverride(bodyText);
        if (target.activeSelf)
        {
            Undo.RecordObject(target, "Hide 15-D-8A empty state until runtime load");
            target.SetActive(false);
        }

        SerializedObject serialized = new SerializedObject(context.TransactionManager);
        SerializedProperty property = serialized.FindProperty("emptyState");
        if (property == null)
            throw new InvalidOperationException("SpendTransactionManager.emptyState 필드를 찾을 수 없습니다.");
        if (property.objectReferenceValue != target)
        {
            Undo.RecordObject(context.TransactionManager, "Connect 15-D-8A empty state");
            property.objectReferenceValue = target;
            serialized.ApplyModifiedProperties();
            SaveOverride(context.TransactionManager);
        }
        context.EmptyState = target;
    }

    private static GameObject EnsureEmptyStateObject(Transform parent, string name, bool withImage)
    {
        Transform[] matches = parent.Cast<Transform>().Where(item => item.name == name).ToArray();
        if (matches.Length > 1)
            throw new InvalidOperationException($"{name} 오브젝트가 중복되어 있습니다.");
        if (matches.Length == 1)
        {
            GameObject existing = matches[0].gameObject;
            if (withImage) GetOrAdd<Image>(existing);
            else GetOrAdd<TextMeshProUGUI>(existing);
            return existing;
        }

        GameObject created = withImage
            ? new GameObject(name, typeof(RectTransform), typeof(Image))
            : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(created, "Create SpendHistory empty state element");
        Undo.SetTransformParent(created.transform, parent, "Parent SpendHistory empty state element");
        return created;
    }

    private static List<string> ValidateBudgetSettingApplied(Context context)
    {
        List<string> errors = new List<string>();
        if (context.BudgetSettingContent.rect.height < 1500f)
            errors.Add("BudgetSetting Content가 화면 상단 공간을 충분히 활용하지 못합니다.");
        if (context.BudgetSettingTitle.font != context.Font ||
            context.BudgetSettingTitle.fontSize < 56f ||
            context.BudgetSettingTitle.alignment != TextAlignmentOptions.Center)
            errors.Add("BudgetSetting 제목의 NotoSansKR/크기/가운데 정렬이 누락되었습니다.");
        if (context.BudgetDescription.font != context.Font || context.BudgetDescription.fontSize < 34f ||
            context.BudgetDescriptionSecondary.font != context.Font ||
            context.BudgetDescriptionSecondary.fontSize < 30f ||
            context.BudgetDescriptionSecondary.lineSpacing < 10f)
            errors.Add("BudgetSetting 안내 문구의 크기 또는 줄 간격이 부족합니다.");
        if (context.BudgetSettingCard.GetComponent<Image>()?.sprite != context.CardSprite ||
            context.BudgetInput.textComponent?.font != context.Font ||
            context.BudgetInput.textComponent.fontSize < 38f)
            errors.Add("BudgetSetting 입력 카드 또는 금액 텍스트 RPG 스타일이 누락되었습니다.");

        TMP_Text saveLabel = context.BudgetSaveButton.GetComponentInChildren<TMP_Text>(true);
        TMP_Text backLabel = context.BudgetBackButton.GetComponentInChildren<TMP_Text>(true);
        if (saveLabel == null || backLabel == null ||
            saveLabel.alignment != TextAlignmentOptions.Center ||
            backLabel.alignment != TextAlignmentOptions.Center)
            errors.Add("BudgetSetting 버튼 텍스트 가운데 정렬이 누락되었습니다.");
        return errors;
    }

    private static List<string> ValidateSpendHistoryApplied(Context context)
    {
        List<string> errors = new List<string>();
        if (!IsVerticalScroll(context.TransactionScroll) || context.TransactionScroll.viewport == null ||
            context.TransactionScroll.content != context.TransactionContent)
            errors.Add("SpendHistory ScrollRect/Viewport/Content 연결이 올바르지 않습니다.");
        Image scrollBackground = context.TransactionScroll.GetComponent<Image>();
        if (scrollBackground == null || scrollBackground.sprite != null ||
            !Mathf.Approximately(scrollBackground.color.r, CreamLight.r / 255f) ||
            !Mathf.Approximately(scrollBackground.color.g, CreamLight.g / 255f) ||
            !Mathf.Approximately(scrollBackground.color.b, CreamLight.b / 255f) ||
            !Mathf.Approximately(scrollBackground.color.a, 1f))
            errors.Add("SpendHistory HistoryScrollView 배경이 크림색으로 보정되지 않았습니다.");
        if (context.TransactionScroll.viewport != null &&
            context.TransactionScroll.viewport.GetComponent<Mask>() == null)
            errors.Add("SpendHistory Viewport Mask가 누락되었습니다.");
        if (context.TransactionContent.GetComponent<VerticalLayoutGroup>() == null ||
            context.TransactionContent.GetComponent<ContentSizeFitter>() == null)
            errors.Add("SpendHistory 동적 Content Layout 구성이 누락되었습니다.");

        SerializedObject manager = new SerializedObject(context.TransactionManager);
        GameObject emptyState = manager.FindProperty("emptyState")?.objectReferenceValue as GameObject;
        if (emptyState == null || context.History.GetComponentsInChildren<Transform>(true)
                .Count(item => item.name == EmptyStateName) != 1)
            errors.Add("SpendHistory EmptyState가 없거나 중복되었습니다.");
        else
        {
            TMP_Text emptyTitle = FindDescendant(emptyState.transform, EmptyStateTitleName)?.GetComponent<TMP_Text>();
            TMP_Text emptyBody = FindDescendant(emptyState.transform, EmptyStateBodyName)?.GetComponent<TMP_Text>();
            if (FindDescendant(emptyState.transform, EmptyStateCardName)?.GetComponent<Image>()?.sprite != context.CardSprite ||
                emptyTitle?.font != context.Font || emptyTitle.fontSize < 46f ||
                emptyBody?.font != context.Font || emptyBody.fontSize < 34f || emptyBody.lineSpacing < 12f)
                errors.Add("SpendHistory EmptyState RPG 카드 또는 NotoSansKR 스타일이 누락되었습니다.");
        }
        if (context.HistoryTitle.font != context.Font || context.HistoryTitle.alignment != TextAlignmentOptions.Center)
            errors.Add("SpendHistory 제목 스타일 또는 가운데 정렬이 누락되었습니다.");
        return errors;
    }

    private static List<string> ValidateRecordSpendApplied(Context context)
    {
        List<string> errors = new List<string>();
        if (!context.IsValid) return context.Errors.ToList();
        if (context.RecordSubtitle == null || context.RecordCategoryLabel == null ||
            context.RecordCurrency == null)
            errors.Add("RecordSpendAdd 제목/입력 라벨/원화 표시가 누락되었습니다.");
        if (context.RecordCategoryPresenter == null)
            errors.Add("Dropdown 표시 전용 한국어 Presenter가 연결되지 않았습니다.");
        if (context.RecordInputCard.GetComponent<Image>()?.sprite != context.CardSprite ||
            context.RecordInputCard.rect.height < 600f)
            errors.Add("금액과 카테고리를 감싸는 RPG 입력 카드가 적용되지 않았습니다.");
        if (!IsVerticalScroll(context.RecordFormScroll) ||
            context.RecordContent.rect.height < 1200f)
            errors.Add("RecordSpendAdd Form의 세로 스크롤 범위가 부족합니다.");
        VerticalLayoutGroup formLayout = context.RecordContent.GetComponent<VerticalLayoutGroup>();
        if (formLayout != null && formLayout.enabled)
            errors.Add("RecordSpendAdd 절대 배치와 충돌하는 기존 VerticalLayoutGroup이 활성 상태입니다.");
        if (context.RecordAmountInput.contentType != TMP_InputField.ContentType.IntegerNumber ||
            context.RecordAmountInput.characterLimit < 10 ||
            context.RecordAmountInput.textComponent?.font != context.Font)
            errors.Add("금액 Input의 숫자 제한, 최대 길이 또는 NotoSansKR 스타일이 누락되었습니다.");
        if (context.RecordCategoryDropdown.captionText?.font != context.Font ||
            context.RecordCategoryDropdown.itemText?.font != context.Font)
            errors.Add("Dropdown 캡션/항목의 NotoSansKR 스타일이 누락되었습니다.");

        RectTransform template = context.RecordCategoryDropdown.template;
        ScrollRect dropdownScroll = template != null
            ? template.GetComponentInChildren<ScrollRect>(true)
            : null;
        RectTransform item = dropdownScroll?.content != null
            ? dropdownScroll.content.Find("Item") as RectTransform
            : null;
        if (template == null || template.rect.height < 520f || dropdownScroll == null ||
            !IsVerticalScroll(dropdownScroll) || dropdownScroll.viewport == null ||
            dropdownScroll.content == null || item == null || item.rect.height < 80f)
            errors.Add("Dropdown Template/Viewport/Content/Item의 높이 또는 내부 스크롤 구성이 부족합니다.");
        if (context.RecordSaveButton.GetComponent<RectTransform>().rect.height < 120f ||
            context.RecordSaveButton.GetComponentInChildren<TMP_Text>(true)?.text != "소비 기록 저장")
            errors.Add("소비 기록 저장 버튼의 크기 또는 한국어 라벨이 적용되지 않았습니다.");
        if (FindButtonByPersistentMethod(context.RecordSpend.transform, "Save") !=
            context.RecordSaveButton ||
            FindButtonByPersistentMethod(context.RecordSpend.transform, "BackToSpend") !=
            context.RecordBackButton)
            errors.Add("기존 Save/BackToSpend UnityEvent를 확인할 수 없습니다.");
        return errors.Distinct().ToList();
    }

    private static List<string> ValidateSpendAddApplied(Context context)
    {
        List<string> errors = new List<string>();
        RectTransform content = context.GoalFormScroll?.content;
        if (content == null)
        {
            errors.Add("SpendAdd_1 Form Content가 없습니다.");
            return errors;
        }
        RectTransform inputCard = FindDescendant(content, SpendAddInputCardName) as RectTransform;
        RectTransform previewCard = FindDescendant(content, SpendAddPreviewCardName) as RectTransform;
        RectTransform rewardCard = FindDescendant(content, SpendAddRewardCardName) as RectTransform;
        RectTransform actions = FindDescendant(content, "BottomAction") as RectTransform;
        Transform header = FindDescendant(context.GoalSetting.transform, "TopHeader");
        if (!IsVerticalScroll(context.GoalFormScroll) ||
            content.rect.height < 1800f || content.rect.width < 940f ||
            (content.GetComponent<VerticalLayoutGroup>()?.enabled ?? false) ||
            (content.GetComponent<ContentSizeFitter>()?.enabled ?? false))
            errors.Add("SpendAdd_1 Form 고정 높이/세로 스크롤/수동 배치 설정이 올바르지 않습니다.");
        if (!IsVerticalScroll(context.GoalPreviewScroll) ||
            context.GoalPreviewContent.GetComponent<VerticalLayoutGroup>() == null ||
            context.GoalPreviewContent.GetComponent<ContentSizeFitter>() == null)
            errors.Add("SpendAdd_1 Preview 동적 목록/세로 스크롤 구성이 누락되었습니다.");
        if (context.DailyButton.gameObject.activeSelf || context.WeeklyButton.gameObject.activeSelf)
            errors.Add("주간 전용 화면에 주기 선택 버튼이 노출됩니다.");
        if (FindDescendant(header, "title")?.GetComponent<TMP_Text>()?.text != "특수 소비 목표 설정")
            errors.Add("SpendAdd_1 한글 제목이 누락되었습니다.");
        TMP_Text[] titles = context.GoalSetting.GetComponentsInChildren<TMP_Text>(true)
            .Where(t => t.text == "이번 주 추가한 목표" &&
                        IsActiveBelow(t.transform, context.GoalSetting.transform)).ToArray();
        if (titles.Length != 1 || inputCard == null || previewCard == null || rewardCard == null ||
            titles.Length == 1 && (titles[0].name != SpendAddPreviewTitleName ||
                                   titles[0].transform.parent != previewCard))
            errors.Add("SpendAdd_1 Preview 제목이 누락되었거나 중복되었습니다.");
        if (FindDescendant(context.GoalPreviewContent, SpendAddEmptyStateName) == null ||
            context.GoalSetting.GetComponent<SpendGoalPreviewEmptyState>() == null)
            errors.Add("SpendAdd_1 Preview EmptyState/Presenter 연결이 누락되었습니다.");
        if (context.SkipButton.GetComponentInChildren<TMP_Text>(true)?.text != "건너뛰기" ||
            context.CompleteButton.GetComponentInChildren<TMP_Text>(true)?.text != "설정 완료")
            errors.Add("SpendAdd_1 하단 버튼 라벨이 누락되었습니다.");
        if (context.GoalNameInput.textComponent?.font != context.Font ||
            context.GoalLimitInput.textComponent?.font != context.Font)
            errors.Add("SpendAdd_1 입력 필드 폰트가 누락되었습니다.");

        if (inputCard == null || previewCard == null || rewardCard == null || actions == null)
        {
            errors.Add("SpendAdd_1 입력/Preview/보상/버튼 배경이 누락되었습니다.");
            return errors;
        }
        List<GameObject> temporarilyActive = ActivateHierarchyForLayout(context.GoalSetting.transform);
        try
        {
            // Original prefab children are expected to REMAIN under Content.
            Transform nameGroup = context.GoalNameInput.transform.parent;
            Transform amountGroup = context.GoalLimitInput.transform.parent;
            Transform preview = FindDescendant(context.GoalSetting.transform, "GoalList");
            Transform reward = FindDescendant(context.GoalSetting.transform, "Reward");
            if (new[] { nameGroup, amountGroup, context.AddGoalButton.transform,
                        preview, reward, (Transform)actions }.Any(item => item?.parent != content))
                errors.Add("SpendAdd_1 기존 Prefab 자식의 원래 Content 부모가 유지되지 않았습니다.");
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            LayoutRebuilder.ForceRebuildLayoutImmediate(actions);
            Rect input = RectInRoot(inputCard, context.GoalSetting.transform);
            Rect previewRect = RectInRoot(previewCard, context.GoalSetting.transform);
            Rect rewardRect = RectInRoot(rewardCard, context.GoalSetting.transform);
            Rect buttonsRect = RectInRoot(actions, context.GoalSetting.transform);
            if (input.Overlaps(previewRect) || previewRect.Overlaps(rewardRect) ||
                rewardRect.Overlaps(buttonsRect) ||
                !(input.yMin > previewRect.yMax && previewRect.yMin > rewardRect.yMax &&
                  rewardRect.yMin > buttonsRect.yMax))
                errors.Add("SpendAdd_1 카드 간 겹침 또는 순서 오류: " +
                           $"input={FormatRect(input)}, preview={FormatRect(previewRect)}, " +
                           $"reward={FormatRect(rewardRect)}, buttons={FormatRect(buttonsRect)}");
            RectTransform nameLabel = FindDescendant(nameGroup, SpendAddNameLabelName) as RectTransform;
            RectTransform amountLabel = FindDescendant(amountGroup, SpendAddAmountLabelName) as RectTransform;
            foreach (var pair in new[]
                     {
                         new { Name="이름 라벨", Rect=nameLabel },
                         new { Name="이름 입력", Rect=context.GoalNameInput.GetComponent<RectTransform>() },
                         new { Name="금액 라벨", Rect=amountLabel },
                         new { Name="금액 입력", Rect=context.GoalLimitInput.GetComponent<RectTransform>() },
                         new { Name="목표 추가", Rect=context.AddGoalButton.GetComponent<RectTransform>() }
                     })
            {
                if (pair.Rect == null || !ContainsRect(input, RectInRoot(pair.Rect, context.GoalSetting.transform)))
                    errors.Add($"SpendAdd_1 {pair.Name}이 입력 카드 내부에 없습니다. " +
                               (pair.Rect == null ? "오브젝트 없음" :
                                   FormatRect(RectInRoot(pair.Rect, context.GoalSetting.transform))));
            }
            if (preview != null &&
                !ContainsRect(previewRect, RectInRoot(preview as RectTransform, context.GoalSetting.transform)))
                errors.Add("SpendAdd_1 Preview 목록이 Preview 카드 범위를 벗어납니다.");
            if (reward != null &&
                !ContainsRect(rewardRect, RectInRoot(reward as RectTransform, context.GoalSetting.transform)))
                errors.Add("SpendAdd_1 보상 영역이 보상 카드 범위를 벗어납니다.");
            if (reward != null)
                foreach (TMP_Text value in reward.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (!IsActiveBelow(value.transform, reward)) continue;
                    Rect valueBounds = RectInRoot(value.rectTransform, context.GoalSetting.transform);
                    if (!ContainsRect(rewardRect, valueBounds))
                        errors.Add("SpendAdd_1 기존 보상 텍스트가 보상 카드 밖으로 나갑니다: " +
                                   $"{GetHierarchyPath(value.transform)} bounds={FormatRect(valueBounds)} " +
                                   $"card={FormatRect(rewardRect)}");
                }
            Rect contentBounds = RectInRoot(content, context.GoalSetting.transform);
            if (!ContainsRect(contentBounds, buttonsRect))
                errors.Add("SpendAdd_1 하단 버튼 영역이 스크롤 Content 밖으로 나갑니다. " +
                           $"Content={FormatRect(contentBounds)}, " +
                           $"BottomAction={FormatRect(buttonsRect)}, " +
                           $"contentHeight={content.rect.height:0.##}, " +
                           $"actionParent={actions.parent?.name}, " +
                           $"actionScale={actions.localScale}");
        }
        finally
        {
            RestoreActivatedHierarchy(temporarilyActive);
        }
        return errors;
    }

    private static List<GameObject> ActivateHierarchyForLayout(Transform target)
    {
        List<GameObject> changed = new List<GameObject>();
        for (Transform current = target; current != null; current = current.parent)
            if (!current.gameObject.activeSelf)
                changed.Add(current.gameObject);
        for (int index = changed.Count - 1; index >= 0; index--)
            changed[index].SetActive(true);
        return changed;
    }

    // Check active state relative to the selected screen, even when its root is disabled
    // in Edit Mode. Checking only activeSelf misidentifies children of hidden legacy groups.
    private static bool IsActiveBelow(Transform child, Transform root)
    {
        for (Transform current = child; current != null && current != root; current = current.parent)
            if (!current.gameObject.activeSelf) return false;
        for (Transform current = child; current != null; current = current.parent)
            if (current == root) return true;
        return false;
    }

    private static void RebuildSpendAddLayout(Context context, params RectTransform[] roots)
    {
        RectTransform goalNameGroup = context.GoalNameInput.transform.parent as RectTransform;
        RectTransform amountGroup = context.GoalLimitInput.transform.parent as RectTransform;
        foreach (RectTransform target in new[] { goalNameGroup, amountGroup }
                     .Concat(roots).Where(item => item != null))
        {
            LayoutRebuilder.MarkLayoutForRebuild(target);
            LayoutRebuilder.ForceRebuildLayoutImmediate(target);
        }
        LayoutRebuilder.MarkLayoutForRebuild(context.GoalFormScroll.content);
        LayoutRebuilder.ForceRebuildLayoutImmediate(context.GoalFormScroll.content);
        foreach (RectTransform target in roots.Where(item => item != null))
            LayoutRebuilder.ForceRebuildLayoutImmediate(target);
    }

    private static string BuildSpendAddBoundsReport(Context context,
        RectTransform inputCard, Rect inputBounds)
    {
        StringBuilder report = new StringBuilder();
        LayoutElement layout = inputCard.GetComponent<LayoutElement>();
        report.AppendLine("[HabitPVP 15-D-8A] SpendAdd_1 GoalInputCard 경계 진단");
        report.AppendLine($"GoalInputCard: Bounds={FormatRect(inputBounds)}, " +
                          $"ActualHeight={inputCard.rect.height:0.##}, " +
                          $"PreferredHeight={layout?.preferredHeight ?? -1f:0.##}");
        report.AppendLine($"GoalName group parent={context.GoalNameInput.transform.parent?.parent?.name ?? "<missing>"}; " +
                          $"Limit group parent={context.GoalLimitInput.transform.parent?.parent?.name ?? "<missing>"}; " +
                          $"AddGoalButton parent={context.AddGoalButton.transform.parent?.name ?? "<null>"}");
        AppendBoundsDiagnostic(report, "GoalNameLabel",
            FindDescendant(context.GoalNameInput.transform.parent, SpendAddNameLabelName) as RectTransform,
            inputBounds, context.GoalSetting.transform);
        AppendBoundsDiagnostic(report, "GoalNameInput",
            context.GoalNameInput.GetComponent<RectTransform>(), inputBounds, context.GoalSetting.transform);
        AppendBoundsDiagnostic(report, "LimitLabel",
            FindDescendant(context.GoalLimitInput.transform.parent, SpendAddAmountLabelName) as RectTransform,
            inputBounds, context.GoalSetting.transform);
        AppendBoundsDiagnostic(report, "LimitInput",
            context.GoalLimitInput.GetComponent<RectTransform>(), inputBounds, context.GoalSetting.transform);
        AppendBoundsDiagnostic(report, "AddGoalButton",
            context.AddGoalButton.GetComponent<RectTransform>(), inputBounds, context.GoalSetting.transform);
        return report.ToString();
    }

    private static void AppendBoundsDiagnostic(StringBuilder report, string label,
        RectTransform target, Rect parentBounds, Transform root)
    {
        if (target == null)
        {
            report.AppendLine($"{label}: <missing>");
            return;
        }
        Rect bounds = RectInRoot(target, root);
        report.AppendLine($"{label}: Bounds={FormatRect(bounds)}, Parent={target.parent?.name}, " +
                          $"anchorMin={target.anchorMin}, anchorMax={target.anchorMax}, " +
                          $"pivot={target.pivot}, anchoredPosition={target.anchoredPosition}, " +
                          $"sizeDelta={target.sizeDelta}");
        report.AppendLine($"  Overflow: Top={Mathf.Max(0f, bounds.yMax - parentBounds.yMax):0.##}, " +
                          $"Bottom={Mathf.Max(0f, parentBounds.yMin - bounds.yMin):0.##}, " +
                          $"Left={Mathf.Max(0f, parentBounds.xMin - bounds.xMin):0.##}, " +
                          $"Right={Mathf.Max(0f, bounds.xMax - parentBounds.xMax):0.##}");
    }

    private static string FormatRect(Rect value)
    {
        return $"(xMin={value.xMin:0.##}, yMin={value.yMin:0.##}, " +
               $"xMax={value.xMax:0.##}, yMax={value.yMax:0.##})";
    }

    private static void RestoreActivatedHierarchy(IEnumerable<GameObject> changed)
    {
        foreach (GameObject item in changed)
            item.SetActive(false);
    }

    private static Rect RectInRoot(RectTransform target, Transform root)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        for (int index = 0; index < corners.Length; index++)
            corners[index] = root.InverseTransformPoint(corners[index]);
        return Rect.MinMaxRect(corners.Min(item => item.x), corners.Min(item => item.y),
            corners.Max(item => item.x), corners.Max(item => item.y));
    }

    private static bool ContainsRect(Rect parent, Rect child)
    {
        const float tolerance = 1f;
        return child.xMin >= parent.xMin - tolerance && child.xMax <= parent.xMax + tolerance &&
               child.yMin >= parent.yMin - tolerance && child.yMax <= parent.yMax + tolerance;
    }

    private static List<string> ValidateAppliedDesign(Context context)
    {
        List<GameObject> temporarilyActivated = new List<GameObject>();
        for (Transform current = context.Spend.transform; current != null; current = current.parent)
        {
            if (!current.gameObject.activeSelf)
                temporarilyActivated.Add(current.gameObject);
        }

        try
        {
            for (int index = temporarilyActivated.Count - 1; index >= 0; index--)
                temporarilyActivated[index].SetActive(true);

            RebuildDashboardLayout(context);
            return ValidateAppliedDesignCore(context);
        }
        finally
        {
            for (int index = 0; index < temporarilyActivated.Count; index++)
                temporarilyActivated[index].SetActive(false);
        }
    }

    private static void RebuildDashboardLayout(Context context)
    {
        RectTransform budgetLayout = context.BudgetCard.Find("BudgetLayout") as RectTransform;
        RectTransform[] innerLayouts =
        {
            context.BudgetFooter,
            budgetLayout,
            context.RewardGrid,
            context.BudgetCard,
            context.RecordCard,
            context.RewardCard,
            context.GoalListCard,
            context.Content
        };

        foreach (RectTransform layout in innerLayouts.Where(item => item != null))
            LayoutRebuilder.MarkLayoutForRebuild(layout);

        LayoutRebuilder.ForceRebuildLayoutImmediate(context.Content);
        foreach (RectTransform layout in innerLayouts.Where(item => item != null))
            LayoutRebuilder.ForceRebuildLayoutImmediate(layout);
        LayoutRebuilder.ForceRebuildLayoutImmediate(context.Content);
        foreach (RectTransform layout in innerLayouts.Where(item => item != null))
            LayoutRebuilder.ForceRebuildLayoutImmediate(layout);
    }

    private static List<string> ValidateAppliedDesignCore(Context context)
    {
        List<string> errors = new List<string>();
        if (!context.IsValid) return context.Errors.ToList();
        if (context.CanvasScaler.referenceResolution != new Vector2(1080f, 1920f))
            errors.Add("CanvasScaler 기준 해상도가 1080x1920이 아닙니다.");
        if (!IsVerticalScroll(context.Scroll) ||
            context.Content.GetComponent<VerticalLayoutGroup>() == null ||
            context.Content.GetComponent<ContentSizeFitter>() == null ||
            context.Content.GetComponent<ContentSizeFitter>().verticalFit !=
                ContentSizeFitter.FitMode.PreferredSize ||
            context.Scroll.content != context.Content)
            errors.Add("Spend 메인 세로 ScrollRect/동적 Content 높이 구성이 누락되었습니다.");
        if (context.Scroll.viewport == null ||
            (context.Scroll.viewport.GetComponent<Mask>() == null &&
             context.Scroll.viewport.GetComponent<RectMask2D>() == null))
            errors.Add("Spend 메인 Viewport의 Mask/RectMask2D 구성이 누락되었습니다.");
        if (context.Scroll.viewport != null &&
            context.Content.rect.height <= context.Scroll.viewport.rect.height + 1f)
            errors.Add($"Spend Content 높이({context.Content.rect.height:0.##})가 " +
                       $"Viewport 높이({context.Scroll.viewport.rect.height:0.##})보다 크지 않아 " +
                       "마지막 카드까지 이동할 세로 스크롤 범위가 없습니다.");
        if (context.Scroll.verticalScrollbar != null &&
            (!context.Scroll.verticalScrollbar.enabled ||
             !context.Scroll.verticalScrollbar.gameObject.activeSelf ||
             !context.Scroll.verticalScrollbar.interactable ||
             context.Scroll.verticalScrollbar.handleRect == null))
            errors.Add("Spend 메인에 연결된 세로 Scrollbar가 비활성화되었거나 Handle이 없습니다.");
        VerticalLayoutGroup dashboardLayout = context.Content.GetComponent<VerticalLayoutGroup>();
        if (dashboardLayout == null || !dashboardLayout.childControlHeight ||
            dashboardLayout.spacing < 32f ||
            dashboardLayout.padding.top < 24 || dashboardLayout.padding.bottom < 56)
            errors.Add("소비 메인 카드 간 spacing/padding이 충분하지 않습니다.");
        foreach (RectTransform card in new[]
                 { context.BudgetCard, context.RecordCard, context.GoalListCard, context.RewardCard })
        {
            if (card?.GetComponent<Image>()?.sprite != context.CardSprite ||
                card.GetComponent<LayoutElement>() == null)
                errors.Add("Spend 메인 RPG 카드 Sprite/LayoutElement 구성이 누락되었습니다.");
        }
        if (!(context.BudgetCard.GetSiblingIndex() < context.RecordCard.GetSiblingIndex() &&
              context.RecordCard.GetSiblingIndex() < context.RewardCard.GetSiblingIndex() &&
              context.RewardCard.GetSiblingIndex() < context.GoalListCard.GetSiblingIndex()))
            errors.Add("소비 메인 카드 순서(예산/기록/보상/특수 목표)가 올바르지 않습니다.");
        if (context.BudgetText.font != context.Font || context.UsedText.font != context.Font ||
            context.BudgetSlider.fillRect == null)
            errors.Add("예산 금액/사용률 Font 또는 진행률 Fill 구성이 누락되었습니다.");
        RectTransform budgetLayout = context.BudgetCard.Find("BudgetLayout") as RectTransform;
        VerticalLayoutGroup budgetVerticalLayout =
            budgetLayout != null ? budgetLayout.GetComponent<VerticalLayoutGroup>() : null;
        if (budgetLayout == null || budgetLayout.GetComponent<VerticalLayoutGroup>() == null ||
            context.BudgetText.GetComponent<LayoutElement>() == null ||
            context.UsedText.GetComponent<LayoutElement>() == null ||
            context.BudgetSlider.GetComponent<LayoutElement>() == null ||
            !IsIgnoredByLayout(context.BudgetSettingButton.gameObject))
            errors.Add("예산 카드의 VerticalLayoutGroup/LayoutElement 구성이 누락되었습니다.");
        if (budgetLayout == null || context.BudgetFooter == null ||
            context.BudgetFooter.parent != budgetLayout ||
            context.BudgetFooter.GetSiblingIndex() != budgetLayout.childCount - 1 ||
            budgetVerticalLayout == null || budgetVerticalLayout.spacing < 20f ||
            budgetVerticalLayout.padding.bottom < 48 ||
            context.BudgetFooter.GetComponent<HorizontalLayoutGroup>() == null ||
            context.BudgetCard.GetComponentsInChildren<Transform>(true)
                .Count(item => item.name == BudgetFooterName) != 1 ||
            context.BudgetUsageText.transform.parent != context.BudgetFooter ||
            context.BudgetRemainingText.transform.parent != context.BudgetFooter ||
            IsIgnoredByLayout(context.BudgetUsageText.gameObject) ||
            IsIgnoredByLayout(context.BudgetRemainingText.gameObject))
            errors.Add("예산 카드 하단 사용률/잔여 정보의 2열 Footer 구성이 누락되었습니다.");
        HorizontalLayoutGroup recordLayout = context.RecordCard.GetComponent<HorizontalLayoutGroup>();
        if (recordLayout == null || recordLayout.spacing < 24f ||
            recordLayout.padding.top < 120 ||
            (context.RecordCard.GetComponent<LayoutElement>()?.preferredHeight ?? 0f) < 260f ||
            !IsIgnoredByLayout(context.RecordTitle.gameObject) ||
            IsIgnoredByLayout(context.RecordButton.gameObject) ||
            IsIgnoredByLayout(context.HistoryButton.gameObject))
            errors.Add("소비 기록 카드의 2열 HorizontalLayoutGroup 구성이 누락되었습니다.");
        GridLayoutGroup rewardGrid = context.RewardGrid != null
            ? context.RewardGrid.GetComponent<GridLayoutGroup>()
            : null;
        if (rewardGrid == null ||
            context.RewardGrid.parent != context.RewardCard ||
            context.RewardCard.GetComponentsInChildren<Transform>(true)
                .Count(item => item.name == RewardGridName) != 1 ||
            (context.RewardCard.GetComponent<GridLayoutGroup>()?.enabled ?? false) ||
            rewardGrid.constraint != GridLayoutGroup.Constraint.FixedColumnCount ||
            rewardGrid.constraintCount != 2 ||
            rewardGrid.cellSize.y < 110f ||
            (context.RewardCard.GetComponent<LayoutElement>()?.preferredHeight ?? 0f) < 620f ||
            !IsIgnoredByLayout(context.RewardTitle.gameObject) ||
            !IsIgnoredByLayout(context.RewardStatusText.gameObject) ||
            !IsIgnoredByLayout(context.TotalRewardText.gameObject) ||
            !IsIgnoredByLayout(context.ClaimButton.gameObject) ||
            !IsIgnoredByLayout(context.BaseRewardText.gameObject) ||
            !IsIgnoredByLayout(context.BonusRewardText.gameObject) ||
            !IsIgnoredByLayout(context.StreakText.gameObject) ||
            !IsIgnoredByLayout(context.StreakBonusText.gameObject) ||
            context.BaseRewardText.gameObject.activeSelf ||
            context.BonusRewardText.gameObject.activeSelf ||
            context.StreakText.gameObject.activeSelf ||
            context.StreakBonusText.gameObject.activeSelf)
            errors.Add("예상 보상 카드의 2열 GridLayoutGroup 구성이 누락되었습니다.");
        Transform[] rewardCells = context.RewardCard.GetComponentsInChildren<Transform>(true)
            .Where(item => item.name.StartsWith(RewardCellPrefix, StringComparison.Ordinal))
            .ToArray();
        if (rewardCells.Length != 4 || rewardCells.Any(item =>
                item.parent != context.RewardGrid || item.GetComponent<Image>() == null ||
                item.GetComponentsInChildren<Transform>(true)
                    .Count(child => child.name == RewardLabelName) != 1 ||
                item.GetComponentsInChildren<Transform>(true)
                    .Count(child => child.name == RewardDisplayName) != 1 ||
                IsIgnoredByLayout(item.gameObject)))
            errors.Add("예상 보상 카드의 2x2 서브 카드 경계가 누락되었거나 중복되었습니다.");
        if (context.RewardDisplayLabels.Any(item => item == null) ||
            context.RewardDisplayTexts.Any(item => item == null) ||
            context.RewardDisplayLabels.Where(item => item != null)
                .Any(item => item.transform.parent == null ||
                             !item.transform.parent.name.StartsWith(
                                 RewardCellPrefix, StringComparison.Ordinal)) ||
            context.RewardDisplayTexts.Where(item => item != null)
                .Any(item => item.transform.parent == null ||
                             !item.transform.parent.name.StartsWith(
                                 RewardCellPrefix, StringComparison.Ordinal)))
            errors.Add("예상 보상 라벨/값 표시 TMP가 각 서브 카드 내부에 연결되지 않았습니다.");
        if (context.GoalListCard.GetComponent<VerticalLayoutGroup>() == null ||
            context.GoalScroll.GetComponent<LayoutElement>() == null ||
            !IsIgnoredByLayout(context.GoalTitle.gameObject) ||
            !IsIgnoredByLayout(context.MainGoalEmptyState))
            errors.Add("특수 소비 목표 카드의 VerticalLayoutGroup/동적 목록 높이가 누락되었습니다.");
        if (context.BudgetTitle == null || context.RecordTitle == null ||
            context.RewardTitle == null || context.GoalTitle == null ||
            context.BudgetUsageText == null || context.BudgetRemainingText == null)
            errors.Add("소비 메인 카드의 한글 제목 또는 예산 보조 정보가 누락되었습니다.");
        if (context.DashboardPresenter == null || context.MainGoalEmptyState == null)
            errors.Add("특수 소비 목표 빈 상태/한글 표시 Presenter 구성이 누락되었습니다.");
        else
        {
            SerializedObject presenter = new SerializedObject(context.DashboardPresenter);
            if (presenter.FindProperty("goalContent")?.objectReferenceValue != context.MainGoalContent ||
                presenter.FindProperty("goalTemplate")?.objectReferenceValue != context.MainGoalTemplate ||
                presenter.FindProperty("goalEmptyState")?.objectReferenceValue != context.MainGoalEmptyState ||
                presenter.FindProperty("goalScroll")?.objectReferenceValue != context.GoalScroll ||
                presenter.FindProperty("baseRewardDisplayLabel")?.objectReferenceValue !=
                    context.RewardDisplayLabels[0] ||
                presenter.FindProperty("bonusRewardDisplayLabel")?.objectReferenceValue !=
                    context.RewardDisplayLabels[1] ||
                presenter.FindProperty("streakDisplayLabel")?.objectReferenceValue !=
                    context.RewardDisplayLabels[2] ||
                presenter.FindProperty("streakBonusDisplayLabel")?.objectReferenceValue !=
                    context.RewardDisplayLabels[3] ||
                presenter.FindProperty("baseRewardDisplayText")?.objectReferenceValue !=
                    context.RewardDisplayTexts[0] ||
                presenter.FindProperty("bonusRewardDisplayText")?.objectReferenceValue !=
                    context.RewardDisplayTexts[1] ||
                presenter.FindProperty("streakDisplayText")?.objectReferenceValue !=
                    context.RewardDisplayTexts[2] ||
                presenter.FindProperty("streakBonusDisplayText")?.objectReferenceValue !=
                    context.RewardDisplayTexts[3])
                errors.Add("소비 대시보드 Presenter의 목표/보상 표시 참조가 누락되었습니다.");
        }

        ValidateDashboardGeometry(context, errors);
        SerializedObject budgetManager = new SerializedObject(context.BudgetManager);
        if (budgetManager.FindProperty("usageText")?.objectReferenceValue != context.BudgetUsageText ||
            budgetManager.FindProperty("remainingText")?.objectReferenceValue != context.BudgetRemainingText)
            errors.Add("예산 사용률/잔여 금액 런타임 참조가 누락되었습니다.");
        if (context.GoalListCard.GetComponentsInChildren<Transform>(true)
                .Count(item => item.name == MainGoalEmptyStateName) != 1)
            errors.Add("특수 소비 목표 빈 상태가 누락되었거나 중복되었습니다.");
        if (context.BudgetSettingCard.GetComponent<Image>()?.sprite != context.CardSprite ||
            context.BudgetInput.textComponent?.font != context.Font)
            errors.Add("BudgetSetting 카드/Input RPG 스타일이 누락되었습니다.");
        if (!IsVerticalScroll(context.RecordFormScroll) ||
            context.RecordAmountInput.textComponent?.font != context.Font ||
            context.RecordCategoryDropdown.captionText?.font != context.Font)
            errors.Add("RecordSpendAdd 입력/Dropdown/세로 Scroll 구성이 누락되었습니다.");
        ScrollRect dropdownScroll = context.RecordCategoryDropdown.template != null
            ? context.RecordCategoryDropdown.template.GetComponentInChildren<ScrollRect>(true)
            : null;
        if (dropdownScroll == null || !IsVerticalScroll(dropdownScroll) ||
            dropdownScroll.viewport == null || dropdownScroll.content == null)
            errors.Add("RecordSpendAdd Dropdown의 휠/드래그 Scroll 구성이 누락되었습니다.");
        errors.AddRange(ValidateRecordSpendApplied(context));
        if (!IsVerticalScroll(context.GoalFormScroll) ||
            !IsVerticalScroll(context.GoalPreviewScroll) ||
            context.GoalPreviewContent.GetComponent<VerticalLayoutGroup>() == null ||
            context.GoalPreviewContent.GetComponent<ContentSizeFitter>() == null)
            errors.Add("SpendAdd_1 Form/Preview 세로 Scroll 또는 동적 Layout 구성이 누락되었습니다.");
        if (!IsVerticalScroll(context.TransactionScroll) ||
            context.TransactionContent.GetComponent<VerticalLayoutGroup>() == null ||
            context.TransactionContent.GetComponent<ContentSizeFitter>() == null)
            errors.Add("SpendHistory 세로 Scroll/동적 Content 높이 구성이 누락되었습니다.");
        SerializedObject runtimeStyle = new SerializedObject(context.TransactionManager);
        if (runtimeStyle.FindProperty("itemFont")?.objectReferenceValue != context.Font ||
            runtimeStyle.FindProperty("itemCardSprite")?.objectReferenceValue != context.CardSprite)
            errors.Add("런타임 거래 카드의 NotoSansKR/RPG 카드 스타일 참조가 누락되었습니다.");

        SerializedObject transactionManager = new SerializedObject(context.TransactionManager);
        GameObject emptyState = transactionManager.FindProperty("emptyState")?.objectReferenceValue as GameObject;
        int emptyStateCount = context.History.GetComponentsInChildren<Transform>(true)
            .Count(item => item.name == EmptyStateName);
        if (emptyState == null || emptyStateCount != 1 ||
            emptyState.GetComponent<TMP_Text>()?.font != context.Font)
            errors.Add("SpendHistory 빈 목록 안내가 누락되었거나 중복되었습니다.");
        return errors.Distinct().ToList();
    }

    private static void ValidateDashboardGeometry(Context context, ICollection<string> errors)
    {
        if (context.BudgetFooter != null)
        {
            if (!ContainsRect(context.BudgetCard, context.BudgetFooter, 2f))
                errors.Add("예산 FooterRow가 BudgetCard 경계 밖으로 나갑니다.\n" +
                           DescribeContainment(context.BudgetCard, context.BudgetFooter));
            if (RectsOverlap(context.BudgetCard,
                    context.BudgetSlider.GetComponent<RectTransform>(), context.BudgetFooter, 2f))
                errors.Add("예산 진행률 바와 FooterRow가 겹칩니다.\n" +
                           DescribeRect(context.BudgetCard,
                               context.BudgetSlider.GetComponent<RectTransform>()) + "\n" +
                           DescribeRect(context.BudgetCard, context.BudgetFooter));
            if (RectsOverlap(context.BudgetFooter,
                    context.BudgetUsageText.rectTransform,
                    context.BudgetRemainingText.rectTransform, 2f))
                errors.Add("예산 사용률과 잔여/초과 안내 영역이 겹칩니다.");
        }

        if (context.RewardGrid != null)
        {
            if (!ContainsRect(context.RewardCard, context.RewardGrid, 2f))
                errors.Add("보상 2x2 Grid가 RewardCard 경계 밖으로 나갑니다.\n" +
                           DescribeContainment(context.RewardCard, context.RewardGrid));
            if (RectsOverlap(context.RewardCard, context.RewardGrid,
                    context.ClaimButton.GetComponent<RectTransform>(), 2f))
                errors.Add("보상 2x2 Grid와 보상 받기 버튼이 겹칩니다.");

            for (int index = 0; index < context.RewardCells.Length; index++)
            {
                RectTransform cell = context.RewardCells[index];
                TMP_Text label = context.RewardDisplayLabels[index];
                TMP_Text display = context.RewardDisplayTexts[index];
                if (cell == null || label == null || display == null) continue;
                if (!ContainsRect(context.RewardGrid, cell, 2f))
                    errors.Add($"보상 서브 카드 {index + 1}이 Grid 경계 밖으로 나갑니다.\n" +
                               DescribeRect(context.RewardGrid, cell));
                if (!ContainsRect(cell, label.rectTransform, 2f))
                    errors.Add($"보상 라벨 {index + 1}이 서브 카드 경계 밖으로 나갑니다.");
                if (!ContainsRect(cell, display.rectTransform, 2f))
                    errors.Add($"보상 값 {index + 1}이 서브 카드 경계 밖으로 나갑니다.");
                if (RectsOverlap(cell, label.rectTransform, display.rectTransform, 1f))
                    errors.Add($"보상 라벨과 값 {index + 1}이 겹칩니다.");
                for (int other = index + 1; other < context.RewardCells.Length; other++)
                {
                    if (context.RewardCells[other] != null &&
                        RectsOverlap(context.RewardGrid, cell, context.RewardCells[other], 2f))
                        errors.Add($"보상 서브 카드 {index + 1}과 {other + 1}이 겹칩니다.");
                }
            }
        }

        RectTransform[] cards =
        {
            context.BudgetCard,
            context.RecordCard,
            context.RewardCard,
            context.GoalListCard
        };
        string[] labels = { "예산", "소비 기록", "예상 보상", "특수 소비 목표" };
        for (int index = 0; index < cards.Length - 1; index++)
        {
            if (RectsOverlap(context.Content, cards[index], cards[index + 1], 2f))
                errors.Add($"{labels[index]} 카드와 {labels[index + 1]} 카드가 겹칩니다.\n" +
                           DescribeRect(context.Content, cards[index]) + "\n" +
                           DescribeRect(context.Content, cards[index + 1]));
            else if (VerticalGap(context.Content, cards[index], cards[index + 1]) < 24f)
                errors.Add($"{labels[index]} 카드와 {labels[index + 1]} 카드 사이 여백이 부족합니다.");
        }
        foreach (RectTransform card in cards)
        {
            if (!ContainsRect(context.Content, card, 2f))
                errors.Add($"Content 높이가 {card.name} 카드 전체를 포함하지 못합니다.\n" +
                           DescribeRect(context.Content, card));
        }
    }

    private static Rect GetRectInReference(RectTransform reference, RectTransform target)
    {
        if (reference == null || target == null) return default;
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector3 first = reference.InverseTransformPoint(corners[0]);
        float minX = first.x;
        float maxX = first.x;
        float minY = first.y;
        float maxY = first.y;
        for (int index = 1; index < corners.Length; index++)
        {
            Vector3 local = reference.InverseTransformPoint(corners[index]);
            minX = Mathf.Min(minX, local.x);
            maxX = Mathf.Max(maxX, local.x);
            minY = Mathf.Min(minY, local.y);
            maxY = Mathf.Max(maxY, local.y);
        }
        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private static bool ContainsRect(RectTransform parent, RectTransform child, float tolerance)
    {
        if (parent == null || child == null) return false;
        Rect childRect = GetRectInReference(parent, child);
        Rect parentRect = parent.rect;
        return childRect.xMin >= parentRect.xMin - tolerance &&
               childRect.xMax <= parentRect.xMax + tolerance &&
               childRect.yMin >= parentRect.yMin - tolerance &&
               childRect.yMax <= parentRect.yMax + tolerance;
    }

    private static string DescribeRect(RectTransform reference, RectTransform target)
    {
        if (reference == null || target == null) return "  RectTransform 없음";
        Rect relative = GetRectInReference(reference, target);
        LayoutElement layout = target.GetComponent<LayoutElement>();
        return $"  {GetHierarchyPath(target)} | parent={target.parent?.name ?? "<none>"}, " +
               $"reference={reference.name}, bounds={relative}, rect={target.rect}, " +
               $"anchored={target.anchoredPosition}, sizeDelta={target.sizeDelta}, " +
               $"preferredHeight={(layout != null ? layout.preferredHeight : -1f):0.##}";
    }

    private static string DescribeContainment(RectTransform parent, RectTransform child)
    {
        if (parent == null || child == null) return "  RectTransform 없음";
        Rect parentRect = parent.rect;
        Rect childRect = GetRectInReference(parent, child);
        float left = Mathf.Max(0f, parentRect.xMin - childRect.xMin);
        float right = Mathf.Max(0f, childRect.xMax - parentRect.xMax);
        float bottom = Mathf.Max(0f, parentRect.yMin - childRect.yMin);
        float top = Mathf.Max(0f, childRect.yMax - parentRect.yMax);
        LayoutElement parentLayout = parent.GetComponent<LayoutElement>();
        LayoutElement childLayout = child.GetComponent<LayoutElement>();
        return $"  Parent {GetHierarchyPath(parent)} | bounds={parentRect}, " +
               $"pivot={parent.pivot}, anchors={parent.anchorMin}..{parent.anchorMax}, " +
               $"preferredHeight={(parentLayout != null ? parentLayout.preferredHeight : -1f):0.##}\n" +
               $"  Child {GetHierarchyPath(child)} | bounds={childRect}, " +
               $"pivot={child.pivot}, anchors={child.anchorMin}..{child.anchorMax}, " +
               $"preferredHeight={(childLayout != null ? childLayout.preferredHeight : -1f):0.##}\n" +
               $"  Overflow left={left:0.##}, right={right:0.##}, " +
               $"bottom={bottom:0.##}, top={top:0.##}";
    }

    private static bool RectsOverlap(RectTransform reference, RectTransform first,
        RectTransform second, float tolerance)
    {
        if (reference == null || first == null || second == null) return false;
        Rect a = GetRectInReference(reference, first);
        Rect b = GetRectInReference(reference, second);
        return a.xMin < b.xMax - tolerance && a.xMax > b.xMin + tolerance &&
               a.yMin < b.yMax - tolerance && a.yMax > b.yMin + tolerance;
    }

    private static float VerticalGap(RectTransform reference, RectTransform upper,
        RectTransform lower)
    {
        Rect upperRect = GetRectInReference(reference, upper);
        Rect lowerRect = GetRectInReference(reference, lower);
        return upperRect.yMin - lowerRect.yMax;
    }

    private static void StyleCard(RectTransform card, float height, Context context)
    {
        StyleImage(card.GetComponent<Image>(), context.CardSprite, Color.white,
            Image.Type.Sliced, true);
        LayoutElement element = GetOrAdd<LayoutElement>(card.gameObject);
        Record(element);
        element.minHeight = height;
        element.preferredHeight = height;
        element.flexibleHeight = 0f;
    }

    private static void StyleTemplate(GameObject template, float height, Context context)
    {
        if (!CanModifySceneObject(template)) return;
        Image image = template.GetComponent<Image>();
        if (image != null)
            StyleImage(image, context.CardSprite, Color.white, Image.Type.Sliced, true);
        LayoutElement element = GetOrAdd<LayoutElement>(template);
        Record(element);
        element.minHeight = height;
        element.preferredHeight = height;
        element.flexibleHeight = 0f;
        foreach (TMP_Text text in template.GetComponentsInChildren<TMP_Text>(true))
            StyleText(text, null, context.Font, Mathf.Max(text.fontSize, 25f), Brown,
                text.fontStyle, text.alignment, true);
    }

    private static void StyleTransactionTemplate(GameObject template, Context context)
    {
        StyleTemplate(template, 126f, context);
        if (!CanModifySceneObject(template)) return;
        TMP_Text category = template.transform.Find("CategoryText")?.GetComponent<TMP_Text>();
        TMP_Text amount = template.transform.Find("AmountText")?.GetComponent<TMP_Text>();
        TMP_Text date = template.transform.Find("DateText")?.GetComponent<TMP_Text>();
        StyleText(category, null, context.Font, 30f, Brown, FontStyles.Bold,
            TextAlignmentOptions.MidlineLeft, false);
        StyleText(amount, null, context.Font, 31f, Brown, FontStyles.Bold,
            TextAlignmentOptions.MidlineRight, false);
        StyleText(date, null, context.Font, 24f, MutedBrown, FontStyles.Normal,
            TextAlignmentOptions.MidlineLeft, false);
    }

    private static void StyleGoalScroll(Context context)
    {
        ConfigureVerticalScroll(context.GoalScroll, 28f, ScrollRect.MovementType.Clamped);
        ConfigureLayoutItem(context.GoalScroll.gameObject, 180f, 380f, 1f, false);
        StyleImage(context.GoalScroll.GetComponent<Image>(), context.CreamButtonSprite,
            CreamLight, Image.Type.Sliced, true);
        if (context.GoalScroll.viewport != null)
        {
            Image viewportImage = context.GoalScroll.viewport.GetComponent<Image>();
            if (viewportImage != null)
                StyleImage(viewportImage, null, new Color(1f, 1f, 1f, 0.01f),
                    Image.Type.Simple, true);
        }
        ConfigureVerticalContent(context.GoalScroll.content, new RectOffset(8, 8, 8, 18), 16f);
        foreach (RectTransform child in context.GoalScroll.content.Cast<Transform>().OfType<RectTransform>())
            StyleTemplate(child.gameObject, 118f, context);
    }

    private static void StyleMoneyText(TMP_Text text, Vector2 position, Context context)
    {
        StyleText(text, null, context.Font, 30f, Brown, FontStyles.Bold,
            TextAlignmentOptions.MidlineRight, false);
        RectTransform rect = text.rectTransform;
        Record(rect);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(-92f, 52f);
    }

    private static void StyleInput(TMP_InputField input, string placeholderValue,
        bool numeric, Context context)
    {
        StyleImage(input.GetComponent<Image>(), context.CreamButtonSprite, CreamLight,
            Image.Type.Sliced, true);
        ApplyRpgSelectableColors(input);
        Record(input);
        if (numeric)
        {
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.characterLimit = 10;
        }
        if (input.textComponent != null)
        {
            StyleText(input.textComponent, null, context.Font, 32f, Brown,
                FontStyles.Bold, numeric ? TextAlignmentOptions.MidlineRight :
                    TextAlignmentOptions.MidlineLeft, false);
        }
        if (input.placeholder is TMP_Text placeholder)
        {
            StyleText(placeholder, placeholderValue, context.Font, 28f, MutedBrown,
                FontStyles.Normal, numeric ? TextAlignmentOptions.MidlineRight :
                    TextAlignmentOptions.MidlineLeft, false);
        }
    }

    private static void StyleDropdown(TMP_Dropdown dropdown, Context context)
    {
        StyleImage(dropdown.GetComponent<Image>(), context.CreamButtonSprite, CreamLight,
            Image.Type.Sliced, true);
        StyleText(dropdown.captionText, null, context.Font, 31f, Brown,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
        StyleText(dropdown.itemText, null, context.Font, 30f, Brown,
            FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
        ApplyRpgSelectableColors(dropdown);
        if (dropdown.captionText != null)
            SetStretch(dropdown.captionText.rectTransform,
                new Vector2(32f, 10f), new Vector2(-92f, -10f));
        Transform arrow = dropdown.transform.Find("Arrow");
        if (arrow != null)
        {
            Image arrowImage = arrow.GetComponent<Image>();
            if (arrowImage != null)
            {
                Record(arrowImage);
                arrowImage.color = Brown;
                SaveOverride(arrowImage);
            }
            SetTopRight(arrow as RectTransform,
                new Vector2(48f, 48f), new Vector2(-28f, -35f));
        }
        RectTransform template = dropdown.template;
        if (template == null) return;
        Record(template);
        template.anchorMin = new Vector2(0f, 0f);
        template.anchorMax = new Vector2(1f, 0f);
        template.pivot = new Vector2(0.5f, 1f);
        template.anchoredPosition = new Vector2(0f, -8f);
        template.sizeDelta = new Vector2(0f, 560f);
        SaveOverride(template);
        StyleImage(template.GetComponent<Image>(), context.CardSprite, CreamLight,
            Image.Type.Sliced, true);
        ScrollRect scroll = template.GetComponent<ScrollRect>() ??
                            template.GetComponentInChildren<ScrollRect>(true);
        if (scroll == null) return;
        if (scroll.viewport == null)
            scroll.viewport = template.Find("Viewport") as RectTransform;
        if (scroll.content == null && scroll.viewport != null)
            scroll.content = scroll.viewport.Find("Content") as RectTransform;
        ConfigureVerticalScroll(scroll, 45f, ScrollRect.MovementType.Clamped);
        if (scroll.viewport != null)
        {
            Image viewportImage = scroll.viewport.GetComponent<Image>();
            if (viewportImage != null)
                StyleImage(viewportImage, null, new Color(1f, 1f, 1f, 0.01f),
                    Image.Type.Simple, true);
        }
        if (scroll.content != null)
        {
            Record(scroll.content);
            scroll.content.sizeDelta = new Vector2(scroll.content.sizeDelta.x, 84f);
            SaveOverride(scroll.content);
            RectTransform item = scroll.content.Find("Item") as RectTransform;
            if (item != null)
            {
                Record(item);
                item.sizeDelta = new Vector2(item.sizeDelta.x, 84f);
                SaveOverride(item);
                TMP_Text itemLabel = item.Find("Item Label")?.GetComponent<TMP_Text>();
                StyleText(itemLabel, null, context.Font, 30f, Brown,
                    FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
                if (itemLabel != null)
                    SetStretch(itemLabel.rectTransform,
                        new Vector2(32f, 8f), new Vector2(-24f, -8f));
                Image itemBackground = item.Find("Item Background")?.GetComponent<Image>();
                if (itemBackground != null)
                    StyleImage(itemBackground, null,
                        new Color32(0xE7, 0xA8, 0x35, 0x54), Image.Type.Simple, true);
                Toggle toggle = item.GetComponent<Toggle>();
                if (toggle != null)
                    ApplyRpgSelectableColors(toggle);
            }
        }
        if (scroll.verticalScrollbar != null)
        {
            Image scrollbarImage = scroll.verticalScrollbar.GetComponent<Image>();
            if (scrollbarImage != null)
                StyleImage(scrollbarImage, null,
                    new Color32(0xD8, 0xC3, 0x9C, 0xA0), Image.Type.Simple, true);
            Image handleImage = scroll.verticalScrollbar.handleRect != null
                ? scroll.verticalScrollbar.handleRect.GetComponent<Image>()
                : null;
            if (handleImage != null)
                StyleImage(handleImage, context.GoldButtonSprite, Gold,
                    Image.Type.Sliced, true);
        }
    }

    private static void ApplyRpgSelectableColors(Selectable selectable)
    {
        if (selectable == null) return;
        Record(selectable);
        ColorBlock colors = selectable.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(0xFF, 0xF3, 0xCF, 0xFF);
        colors.pressedColor = new Color32(0xE6, 0xC5, 0x82, 0xFF);
        colors.selectedColor = new Color32(0xF4, 0xD7, 0x96, 0xFF);
        colors.disabledColor = new Color32(0xC8, 0xB8, 0xA0, 0x99);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        selectable.colors = colors;
        SaveOverride(selectable);
    }

    private static void StyleButton(Button button, Sprite sprite, string labelValue, Context context)
    {
        if (button == null) return;
        StyleImage(button.GetComponent<Image>(), sprite, Color.white, Image.Type.Sliced, true);
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        StyleText(label, labelValue, context.Font, 30f, Brown,
            FontStyles.Bold, TextAlignmentOptions.Center, false);
    }

    private static void StyleText(TMP_Text text, string value, TMP_FontAsset font,
        float size, Color color, FontStyles style, TextAlignmentOptions alignment, bool wrap)
    {
        if (text == null) return;
        Record(text);
        if (value != null) text.text = value;
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = alignment;
        text.enableWordWrapping = wrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        SaveOverride(text);
    }

    private static void StyleImage(Image image, Sprite sprite, Color color,
        Image.Type type, bool raycast)
    {
        if (image == null) return;
        Record(image);
        image.sprite = sprite;
        image.color = color;
        image.type = type;
        image.raycastTarget = raycast;
        SaveOverride(image);
    }

    private static void ConfigureVerticalScroll(ScrollRect scroll, float sensitivity,
        ScrollRect.MovementType movementType)
    {
        if (scroll == null) return;
        Record(scroll);
        scroll.enabled = true;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = movementType;
        scroll.inertia = true;
        scroll.scrollSensitivity = sensitivity;
        if (scroll.viewport != null)
        {
            Graphic graphic = scroll.viewport.GetComponent<Graphic>();
            if (graphic != null)
            {
                Record(graphic);
                graphic.raycastTarget = true;
                SaveOverride(graphic);
            }
        }
        SaveOverride(scroll);
    }

    private static void ConfigureVerticalLayoutGroup(GameObject target,
        RectOffset padding, float spacing)
    {
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(target);
        Record(layout);
        layout.padding = padding;
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        SaveOverride(layout);
    }

    private static void ConfigureHorizontalLayoutGroup(GameObject target,
        RectOffset padding, float spacing)
    {
        HorizontalLayoutGroup layout = GetOrAdd<HorizontalLayoutGroup>(target);
        Record(layout);
        layout.padding = padding;
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        SaveOverride(layout);
    }

    private static void ConfigureRewardGrid(GameObject target)
    {
        GridLayoutGroup layout = GetOrAdd<GridLayoutGroup>(target);
        Record(layout);
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.cellSize = new Vector2(420f, 110f);
        layout.spacing = new Vector2(40f, 20f);
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 2;
        SaveOverride(layout);
    }

    private static void DisableLayoutGroup<T>(GameObject target) where T : Behaviour
    {
        T layout = target != null ? target.GetComponent<T>() : null;
        if (layout == null || !layout.enabled) return;
        Record(layout);
        layout.enabled = false;
        SaveOverride(layout);
    }

    private static void ConfigureLayoutItem(GameObject target, float minHeight,
        float preferredHeight, float flexibleWidth, bool ignoreLayout)
    {
        LayoutElement element = GetOrAdd<LayoutElement>(target);
        Record(element);
        element.ignoreLayout = ignoreLayout;
        element.minHeight = minHeight;
        element.preferredHeight = preferredHeight;
        element.flexibleHeight = 0f;
        element.minWidth = 0f;
        element.preferredWidth = 0f;
        element.flexibleWidth = flexibleWidth;
        SaveOverride(element);
    }

    private static void ConfigureIgnoreLayout(GameObject target, bool ignoreLayout)
    {
        if (target == null) return;
        LayoutElement element = GetOrAdd<LayoutElement>(target);
        Record(element);
        element.ignoreLayout = ignoreLayout;
        SaveOverride(element);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target == null || target.activeSelf == active) return;
        Undo.RecordObject(target, "Apply 15-D-8A dashboard active state");
        target.SetActive(active);
        SaveOverride(target);
    }

    private static void ConfigureVerticalContent(RectTransform content,
        RectOffset padding, float spacing, bool controlChildHeight = false)
    {
        if (content == null) return;
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(content.gameObject);
        Record(layout);
        layout.padding = padding;
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = controlChildHeight;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(content.gameObject);
        Record(fitter);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        SaveOverride(layout);
        SaveOverride(fitter);
    }

    private static bool IsVerticalScroll(ScrollRect scroll)
    {
        return scroll != null && scroll.enabled && scroll.vertical && !scroll.horizontal &&
               scroll.viewport != null && scroll.content != null &&
               scroll.scrollSensitivity >= 20f;
    }

    private static bool IsIgnoredByLayout(GameObject target)
    {
        LayoutElement element = target != null ? target.GetComponent<LayoutElement>() : null;
        return element != null && element.ignoreLayout;
    }

    private static bool CanModifySceneObject(GameObject target)
    {
        return target != null && !EditorUtility.IsPersistent(target) &&
               target.scene.IsValid() && target.scene.path == ScenePath;
    }

    private static TextMeshProUGUI FindParentText(Transform start)
    {
        for (Transform current = start; current != null; current = current.parent)
        {
            TextMeshProUGUI text = current.GetComponent<TextMeshProUGUI>();
            if (text != null) return text;
        }

        return null;
    }

    private static bool IsDashboardGenerated(string path)
    {
        return !string.IsNullOrEmpty(path) &&
               path.IndexOf("__15D8A", StringComparison.Ordinal) >= 0;
    }

    private static string ObjectName(UnityEngine.Object target)
    {
        return target != null ? target.name : "<null>";
    }

    private static Button FindButtonByLabel(Transform root, string label)
    {
        return root == null ? null : root.GetComponentsInChildren<Button>(true).FirstOrDefault(button =>
        {
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            return text != null && string.Equals(
                text.text.Trim(), label, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static Button FindButtonByPersistentMethod(Transform root, string methodName)
    {
        return root == null ? null : root.GetComponentsInChildren<Button>(true).FirstOrDefault(button =>
            Enumerable.Range(0, button.onClick.GetPersistentEventCount())
                .Any(index => button.onClick.GetPersistentMethodName(index) == methodName));
    }

    private static T FindUniqueComponent<T>(Context context) where T : Component
    {
        T[] values = context.Scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        if (values.Length != 1)
            context.Errors.Add($"{typeof(T).Name} 컴포넌트가 정확히 하나여야 합니다. 현재: {values.Length}");
        return values.Length == 1 ? values[0] : null;
    }

    private static T ReadReference<T>(SerializedObject serialized, string name)
        where T : UnityEngine.Object
    {
        return serialized.FindProperty(name)?.objectReferenceValue as T;
    }

    private static T GetUnique<T>(Transform root, string name, List<string> errors)
        where T : Component
    {
        Transform transform = FindUnique(root, name, errors);
        T component = transform != null ? transform.GetComponent<T>() : null;
        if (transform != null && component == null)
            errors.Add($"'{name}'에 {typeof(T).Name} 컴포넌트가 없습니다.");
        return component;
    }

    private static TMP_Text FindOptionalScreenTitle(Transform spendRoot, List<string> errors)
    {
        if (spendRoot == null) return null;

        HashSet<string> supportedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SpendingTitleText", "ScreenTitleText", "TitleText", "Title"
        };

        TMP_Text[] candidates = spendRoot.GetComponentsInChildren<TMP_Text>(true)
            .Where(text => supportedNames.Contains(text.name))
            .Where(text =>
            {
                Transform parent = text.transform.parent;
                if (parent == spendRoot) return true;
                if (parent == null || parent.parent != spendRoot) return false;
                return parent.name.Equals("Header", StringComparison.OrdinalIgnoreCase) ||
                       parent.name.Equals("TopHeader", StringComparison.OrdinalIgnoreCase);
            })
            .ToArray();

        if (candidates.Length <= 1)
            return candidates.FirstOrDefault();

        errors.Add(
            "소비 메인 제목 TMP 후보를 하나로 확정할 수 없습니다: " +
            string.Join(", ", candidates.Select(item => GetHierarchyPath(item.transform))));
        return null;
    }

    private static string GetHierarchyPath(Transform target)
    {
        if (target == null) return "<null>";
        Stack<string> names = new Stack<string>();
        Transform current = target;
        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }
        return string.Join("/", names);
    }

    private static Transform FindUnique(Transform root, string name, List<string> errors)
    {
        if (root == null)
        {
            errors.Add($"'{name}' 탐색 기준 Transform이 없습니다.");
            return null;
        }
        Transform[] matches = root.GetComponentsInChildren<Transform>(true)
            .Where(item => item.name == name).ToArray();
        if (matches.Length != 1)
            errors.Add($"'{name}' 오브젝트가 정확히 하나여야 합니다. 현재: {matches.Length}");
        return matches.Length == 1 ? matches[0] : null;
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        return root == null
            ? null
            : root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == name);
    }

    private static void Require(Context context, UnityEngine.Object value, string label)
    {
        if (value == null) context.Errors.Add(label + "이(가) 없습니다.");
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
        SaveOverride(rect);
    }

    private static void SetTopCentered(RectTransform rect, Vector2 size, Vector2 position)
    {
        Record(rect);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        SaveOverride(rect);
    }

    private static void SetTopLeft(RectTransform rect, Vector2 size, Vector2 position)
    {
        Record(rect);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        SaveOverride(rect);
    }

    private static void SetTopRight(RectTransform rect, Vector2 size, Vector2 position)
    {
        Record(rect);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        SaveOverride(rect);
    }

    private static void SetStretch(RectTransform rect, Vector2 minOffset, Vector2 maxOffset)
    {
        Record(rect);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = minOffset;
        rect.offsetMax = maxOffset;
        SaveOverride(rect);
    }

    private static void Record(UnityEngine.Object target)
    {
        if (target != null) Undo.RecordObject(target, "Apply 15-D-8A Spending UI Renewal");
    }

    private static void SaveOverride(UnityEngine.Object target)
    {
        if (target != null && PrefabUtility.IsPartOfPrefabInstance(target))
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private sealed class ProtectedState
    {
        private readonly Hash128 canvasPrefabHash;
        private readonly bool weeklyReportActive;
        private readonly Vector3 weeklyReportLocalPosition;
        private readonly int weeklyReportChildCount;
        private readonly UnityEngine.Object[] protectedReferences;
        private readonly string[] buttonListeners;

        public ProtectedState(Context context)
        {
            canvasPrefabHash = AssetDatabase.GetAssetDependencyHash(CanvasPrefabPath);
            weeklyReportActive = context.WeeklyReportPanel.activeSelf;
            weeklyReportLocalPosition = context.WeeklyReportPanel.transform.localPosition;
            weeklyReportChildCount = context.WeeklyReportPanel.GetComponentsInChildren<Transform>(true).Length;
            protectedReferences = CaptureReferences(context);
            buttonListeners = CaptureListeners(context);
        }

        public void AssertUnchanged(Context context)
        {
            if (canvasPrefabHash != AssetDatabase.GetAssetDependencyHash(CanvasPrefabPath))
                throw new InvalidOperationException("Canvas.prefab 원본이 변경되었습니다.");
            if (weeklyReportActive != context.WeeklyReportPanel.activeSelf ||
                weeklyReportLocalPosition != context.WeeklyReportPanel.transform.localPosition ||
                weeklyReportChildCount != context.WeeklyReportPanel.GetComponentsInChildren<Transform>(true).Length)
                throw new InvalidOperationException("제외 대상 WeeklyReportPanel이 변경되었습니다.");
            if (!protectedReferences.SequenceEqual(CaptureReferences(context)))
                throw new InvalidOperationException("기존 SerializeField 참조가 변경되었습니다.");
            if (!buttonListeners.SequenceEqual(CaptureListeners(context)))
                throw new InvalidOperationException("기존 Button UnityEvent가 변경되었습니다.");
        }

        private static UnityEngine.Object[] CaptureReferences(Context context)
        {
            List<UnityEngine.Object> values = new List<UnityEngine.Object>();
            Capture(context.HabitUiManager, values,
                "topTab", "lifePanel", "spendPanel", "lifeAddPanel", "spendAddPanel",
                "budgetSettingPanel", "spendHistoryPanel", "weeklyReportPanel");
            Capture(context.BudgetManager, values, "budgetText", "usedText", "budgetSlider");
            Capture(context.RewardManager, values,
                "totalGoldText", "baseGoldText", "bonusGoldText", "rewardTitleText",
                "streakText", "streakBonusText", "claimRewardButton");
            Capture(context.BudgetSettingManager, values, "budgetInput", "saveButton", "backButton");
            Capture(context.RecordSpendManager, values,
                "amountInput", "categoryDropdown", "panelManager", "transactionManager");
            Capture(context.SpendListManager, values,
                "goalNameInput", "goldInput", "previewContent", "specialGoalPreviewPrefab",
                "mainContent", "spendItemPrefab", "skipButton", "completeButton");
            Capture(context.TransactionManager, values, "content", "transactionItemPrefab");
            Capture(context.Scroll, values, "m_Content", "m_Viewport", "m_VerticalScrollbar");
            return values.ToArray();
        }

        private static void Capture(UnityEngine.Object target, ICollection<UnityEngine.Object> values,
            params string[] names)
        {
            SerializedObject serialized = new SerializedObject(target);
            foreach (string name in names)
                values.Add(serialized.FindProperty(name)?.objectReferenceValue);
        }

        private static string[] CaptureListeners(Context context)
        {
            return new[]
                {
                    context.RecordButton, context.HistoryButton, context.ClaimButton,
                    context.BudgetSettingButton, context.BudgetSaveButton, context.BudgetBackButton,
                    context.RecordSaveButton, context.RecordBackButton, context.DailyButton,
                    context.WeeklyButton, context.AddGoalButton, context.SkipButton,
                    context.CompleteButton, context.HistoryBackButton
                }
                .Where(button => button != null)
                .SelectMany(button => CaptureEvent(button.name, button.onClick))
                .ToArray();
        }

        private static IEnumerable<string> CaptureEvent(string owner, UnityEventBase unityEvent)
        {
            for (int index = 0; index < unityEvent.GetPersistentEventCount(); index++)
            {
                UnityEngine.Object target = unityEvent.GetPersistentTarget(index);
                yield return owner + "|" + (target != null ? target.GetInstanceID().ToString() : "null") +
                             "|" + unityEvent.GetPersistentMethodName(index);
            }
        }
    }

    private sealed class Context
    {
        public Scene Scene;
        public GameObject Canvas;
        public CanvasScaler CanvasScaler;
        public GameObject TapBar;
        public GameObject WeeklyReportPanel;
        public HabitUIManager HabitUiManager;
        public SpendBudgetManager BudgetManager;
        public SpendRewardManager RewardManager;
        public SpendingDashboardPresenter DashboardPresenter;
        public GameObject Spend;
        public TMP_Text SpendingTitle;
        public ScrollRect Scroll;
        public RectTransform Content;
        public RectTransform BudgetCard;
        public RectTransform RecordCard;
        public RectTransform GoalListCard;
        public RectTransform RewardCard;
        public ScrollRect GoalScroll;
        public Button RecordButton;
        public Button HistoryButton;
        public Button ClaimButton;
        public Button BudgetSettingButton;
        public TMP_Text BudgetText;
        public TMP_Text UsedText;
        public TMP_Text BudgetUsageText;
        public TMP_Text BudgetRemainingText;
        public RectTransform BudgetFooter;
        public Slider BudgetSlider;
        public TMP_Text BudgetTitle;
        public TMP_Text RecordTitle;
        public TMP_Text RewardTitle;
        public TMP_Text GoalTitle;
        public TMP_Text TotalRewardText;
        public TMP_Text BaseRewardText;
        public TMP_Text BonusRewardText;
        public TMP_Text RewardStatusText;
        public TMP_Text StreakText;
        public TMP_Text StreakBonusText;
        public RectTransform RewardGrid;
        public readonly RectTransform[] RewardCells = new RectTransform[4];
        public readonly TMP_Text[] RewardDisplayLabels = new TMP_Text[4];
        public readonly TMP_Text[] RewardDisplayTexts = new TMP_Text[4];
        public GameObject MainGoalEmptyState;
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
        public GameObject RecordSpend;
        public RecordSpendManager RecordSpendManager;
        public ScrollRect RecordFormScroll;
        public RectTransform RecordContent;
        public RectTransform RecordInputCard;
        public TMP_InputField RecordAmountInput;
        public TMP_Text RecordAmountLabel;
        public TMP_Dropdown RecordCategoryDropdown;
        public TMP_Text RecordScreenTitle;
        public TMP_Text RecordSubtitle;
        public TMP_Text RecordCategoryLabel;
        public TMP_Text RecordCurrency;
        public SpendCategoryDropdownPresenter RecordCategoryPresenter;
        public Button RecordSaveButton;
        public Button RecordBackButton;
        public GameObject GoalSetting;
        public SpendListManager SpendListManager;
        public SpendCycleManager SpendCycleManager;
        public ScrollRect GoalFormScroll;
        public ScrollRect GoalPreviewScroll;
        public TMP_InputField GoalNameInput;
        public TMP_InputField GoalLimitInput;
        public Transform GoalPreviewContent;
        public GameObject SpecialGoalPreviewTemplate;
        public Transform MainGoalContent;
        public GameObject MainGoalTemplate;
        public Button DailyButton;
        public Button WeeklyButton;
        public Button AddGoalButton;
        public Button SkipButton;
        public Button CompleteButton;
        public TMP_Text SpendAddTitle;
        public TMP_Text SpendAddSubtitle;
        public TMP_Text SpendAddOptional;
        public TMP_Text GoalNameLabel;
        public TMP_Text GoalAmountLabel;
        public TMP_Text GoalPreviewTitle;
        public TMP_Text GoalInputTitle;
        public RectTransform GoalInputCard;
        public RectTransform GoalPreviewCard;
        public RectTransform GoalRewardCard;
        public GameObject History;
        public SpendTransactionManager TransactionManager;
        public ScrollRect TransactionScroll;
        public RectTransform TransactionContent;
        public GameObject TransactionTemplate;
        public GameObject EmptyState;
        public TMP_Text HistoryTitle;
        public Button HistoryBackButton;
        public TMP_FontAsset Font;
        public Sprite CardSprite;
        public Sprite CreamButtonSprite;
        public Sprite GoldButtonSprite;
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }
}
