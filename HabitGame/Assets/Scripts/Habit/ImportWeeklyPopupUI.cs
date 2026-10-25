
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ImportWeeklyPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject importWeeklyPopup;

    [Header("지난주 목표 목록 UI")]
    [SerializeField] private Transform goalContent;
    [SerializeField] private ImportGoalItemUI importGoalItemPrefab;
    [SerializeField] private GameObject emptyGoalText;

    [Header("팝업 안내 문구")]
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("가져오기 버튼")]
    [SerializeField] private Button importButton;

    private HabitService habitService;

    private List<HabitGoalResponse> previousWeeklyGoals =
        new List<HabitGoalResponse>();

    // 서버 데이터를 표시하기 위해 생성한 목표 UI만 관리
    private readonly List<ImportGoalItemUI> createdItems =
        new List<ImportGoalItemUI>();

    private bool isImporting;

    // 다른 매니저가 가져오기 성공 후 목록을 갱신할 수 있도록 알림
    public event Action ImportCompleted;

    private void Awake()
    {
        if (ServiceRegistry.Instance != null)
        {
            habitService = ServiceRegistry.Instance.Habit;
        }
    }

    // 지난주 목표 가져오기 팝업 열기
    public async void OpenPopup()
    {
        if (importWeeklyPopup != null)
        {
            importWeeklyPopup.SetActive(true);
            ShowMessage("이번 주에 다시 도전할 목표를 선택해 주세요.");
        }

        await LoadPreviousWeeklyGoalsAsync();
    }

    // 지난주 주간 목표 서버 조회
    private async Task LoadPreviousWeeklyGoalsAsync()
    {
        // 씬 초기화 순서에 따라 서비스 연결 재시도
        if (habitService == null && ServiceRegistry.Instance != null)
        {
            habitService = ServiceRegistry.Instance.Habit;
        }

        if (habitService == null)
        {
            Debug.LogWarning(
                "ImportWeeklyPopupUI: HabitService가 연결되지 않았습니다."
            );
            return;
        }

        try
        {
            List<HabitGoalResponse> goals =
                await habitService.GetPreviousWeeklyGoalsAsync();

            previousWeeklyGoals =
                goals ?? new List<HabitGoalResponse>();

            Debug.Log(
                $"지난주 주간 목표 {previousWeeklyGoals.Count}개 조회 성공"
            );

            DisplayGoals();
        }
        catch (Exception e)
        {
            Debug.LogError(
                "지난주 주간 목표 조회 실패: " + e.Message
            );
        }
    }

    // 조회한 목표들을 팝업에 표시
    private void DisplayGoals()
    {
        if (goalContent == null || importGoalItemPrefab == null)
        {
            Debug.LogError(
                "지난주 목표 목록 UI 연결이 누락되었습니다."
            );
            return;
        }

        // 이전에 생성했던 목표 UI만 제거
        // 원본 ImportGoalItem 템플릿은 유지
        foreach (ImportGoalItemUI item in createdItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        createdItems.Clear();

        // 지난주 목표가 없으면 안내 문구 표시
        if (emptyGoalText != null)
        {
            emptyGoalText.SetActive(previousWeeklyGoals.TrueForAll(goal => goal == null));
        }

        // 서버에서 받은 목표 개수만큼 카드 생성
        foreach (HabitGoalResponse goal in previousWeeklyGoals)
        {
            if (goal == null)
                continue;

            ImportGoalItemUI newItem =
                Instantiate(importGoalItemPrefab, goalContent);

            newItem.gameObject.SetActive(true);
            newItem.SetData(goal);

            createdItems.Add(newItem);
        }
    }

    // 선택한 지난주 목표를 이번 주 목표로 가져오기
    public async void ImportSelectedGoals()
    {
        if (isImporting)
            return;

        ImportWeeklyGoalsRequest request =
            new ImportWeeklyGoalsRequest();

        foreach (ImportGoalItemUI item in createdItems)
        {
            if (item == null || !item.IsSelected)
                continue;

            if (!item.TryGetTargetAmount(out int targetAmount))
            {
                Debug.LogWarning(
                    "선택한 목표의 목표량은 1 이상의 정수여야 합니다."
                );
                ShowMessage("목표량은 1 이상의 숫자로 입력해 주세요.");
                return;
            }

            request.Goals.Add(new ImportWeeklyGoalItem
            {
                SourceGoalId = item.GoalId,
                TargetAmount = targetAmount
            });
        }

        if (request.Goals.Count == 0)
        {
            Debug.LogWarning(
                "가져올 지난주 목표를 하나 이상 선택해 주세요."
            );
            ShowMessage("가져올 목표를 하나 이상 선택해 주세요.");
            return;
        }

        if (habitService == null && ServiceRegistry.Instance != null)
        {
            habitService = ServiceRegistry.Instance.Habit;
        }

        if (habitService == null)
        {
            Debug.LogWarning(
                "ImportWeeklyPopupUI: HabitService가 연결되지 않았습니다."
            );
            return;
        }

        isImporting = true;

        if (importButton != null)
            importButton.interactable = false;

        try
        {
            List<HabitGoalResponse> importedGoals =
                await habitService.ImportPreviousWeeklyGoalsAsync(request);

            Debug.Log(
                $"지난주 주간 목표 {importedGoals?.Count ?? 0}개 가져오기 성공"
            );

            ClosePopup();

            // 실제 목록 갱신은 기존 목표 조회 매니저와 연결
            ImportCompleted?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError(
                "지난주 주간 목표 가져오기 실패: " + e.Message
            );
            ShowMessage("목표 가져오기에 실패했습니다. 다시 시도해 주세요.");
        }
        finally
        {
            isImporting = false;
            if (importButton != null)
                importButton.interactable = true;
        }
    }
    // 팝업 안내 문구 변경
    private void ShowMessage(string message)
    {
        if (descriptionText != null)
            descriptionText.text = message;
    }
    // 팝업 닫기
    public void ClosePopup()
    {
        if (importWeeklyPopup != null)
        {
            importWeeklyPopup.SetActive(false);
        }
    }
}
