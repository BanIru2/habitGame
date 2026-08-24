using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HabitGoalUI : MonoBehaviour
{
    private HabitService habitService;

    [Header("Amount")]
    public CanvasGroup amountPanel;
    public TMP_Text amountText;
    public TMP_Dropdown unitDropdown;

    private int amount = 30;

    [Header("Category")]
    public Button physicalButton;
    public Button rhythmButton;
    public Button ecoButton;
    public Button growthButton;

    private string selectedCategory = "";

    [Header("Period")]
    public Button dailyButton;
    public Button weeklyButton;

    private string selectedPeriod = "daily";

    [Header("Record Type")]
    private string selectedRecordType = "value";

    [Header("Habit Name")]
    public TMP_InputField habitNameInput;


    private void Start()
    {
        habitService = new HabitService(ApiClient.Instance);
    }


    // =========================================
    // 기록 방식
    // =========================================

    public void CompleteMode()
    {
        selectedRecordType = "check";

        amountPanel.alpha = 0.4f;
        amountPanel.interactable = false;
        amountPanel.blocksRaycasts = false;
    }


    public void ValueMode()
    {
        selectedRecordType = "value";

        amountPanel.alpha = 1f;
        amountPanel.interactable = true;
        amountPanel.blocksRaycasts = true;
    }


    // =========================================
    // 목표 수치
    // =========================================

    public void IncreaseAmount()
    {
        amount++;

        amountText.text = amount.ToString();
    }


    public void DecreaseAmount()
    {
        if (amount > 1)
        {
            amount--;
        }

        amountText.text = amount.ToString();
    }


    // =========================================
    // 카테고리
    // =========================================

    public void SelectCategory(string category)
    {
        selectedCategory = category;

        physicalButton.image.color = Color.white;
        rhythmButton.image.color = Color.white;
        ecoButton.image.color = Color.white;
        growthButton.image.color = Color.white;

        switch (category)
        {
            case "physical":
                physicalButton.image.color =
                    new Color(1f, 0.9f, 0.9f);
                break;

            case "rhythm":
                rhythmButton.image.color =
                    new Color(0.9f, 0.95f, 1f);
                break;

            case "eco":
                ecoButton.image.color =
                    new Color(0.9f, 1f, 0.9f);
                break;

            case "growth":
                growthButton.image.color =
                    new Color(0.95f, 0.9f, 1f);
                break;
        }
    }


    // =========================================
    // 반복 주기
    // =========================================

    public void SelectPeriod(string period)
    {
        selectedPeriod = period;

        dailyButton.image.color = Color.white;
        weeklyButton.image.color = Color.white;

        if (period == "daily")
        {
            dailyButton.image.color =
                new Color(0.9f, 1f, 0.9f);
        }
        else
        {
            weeklyButton.image.color =
                new Color(0.9f, 1f, 0.9f);
        }
    }


    // =========================================
    // 저장
    // =========================================

    public async void SaveGoal()
    {
        // 습관 이름 확인
        if (string.IsNullOrWhiteSpace(habitNameInput.text))
        {
            Debug.LogWarning("Habit Name을 입력해주세요.");
            return;
        }

        // 카테고리 확인
        if (string.IsNullOrWhiteSpace(selectedCategory))
        {
            Debug.LogWarning("Category를 선택해주세요.");
            return;
        }


        CreateHabitGoalRequest request =
            new CreateHabitGoalRequest();

        request.UserId =
            ApiClient.Instance.CurrentUserId;

        request.GoalName =
            habitNameInput.text.Trim();

        request.Category =
            selectedCategory;

        request.RecordType =
            selectedRecordType;

        request.Period =
            selectedPeriod;


        // -----------------------------------------
        // Record Type에 따른 목표값 처리
        // -----------------------------------------

        if (selectedRecordType == "check")
        {
            // Complete 방식
            request.TargetAmount = 1;
            request.Unit = "check";
        }
        else
        {
            // Value 방식
            request.TargetAmount = amount;

            request.Unit =
                unitDropdown
                    .options[unitDropdown.value]
                    .text
                    .ToLower();
        }


        Debug.Log("===== Habit Goal =====");
        Debug.Log("Name : " + request.GoalName);
        Debug.Log("Category : " + request.Category);
        Debug.Log("RecordType : " + request.RecordType);
        Debug.Log("Amount : " + request.TargetAmount);
        Debug.Log("Unit : " + request.Unit);
        Debug.Log("Period : " + request.Period);


        string json =
            JsonConvert.SerializeObject(
                request,
                Formatting.Indented
            );

        Debug.Log(json);


        // -----------------------------------------
        // API 실패 시에도 UI 테스트가 가능하도록
        // 현재 입력값으로 local Habit 데이터 생성
        // -----------------------------------------

        HabitGoalResponse localHabit =
            new HabitGoalResponse
            {
                UserId = request.UserId,

                GoalName =
                    request.GoalName,

                Category =
                    request.Category,

                RecordType =
                    request.RecordType,

                TargetAmount =
                    request.TargetAmount,

                Unit =
                    request.Unit,

                Period =
                    request.Period,

                IsActive = true
            };


        HabitGoalResponse habitToAdd = null;


        try
        {
            // 실제 DB에 습관 생성
            HabitGoalResponse response =
                await habitService.CreateGoalAsync(request);

            if (response != null)
            {
                habitToAdd = response;

                Debug.Log("===== API Success =====");
                Debug.Log("Goal ID : " + response.Id);
                Debug.Log("Message : " + response.Message);
            }
            else
            {
                Debug.LogWarning(
                    "API 응답이 비어있어 로컬 데이터로 표시합니다."
                );

                habitToAdd = localHabit;
            }
        }
        catch (System.Exception e)
        {
            // 현재 서버가 꺼져 있어도
            // Unity UI 기능 테스트는 계속 가능
            Debug.LogWarning(
                "Habit API 연결 실패 - 로컬 데이터로 추가합니다.\n"
                + e.Message
            );

            habitToAdd = localHabit;
        }


        // -----------------------------------------
        // Habit 리스트에 추가
        // -----------------------------------------

        HabitListManager listManager =
            FindObjectOfType<HabitListManager>();

        if (listManager != null)
        {
            listManager.AddHabit(habitToAdd);
        }
        else
        {
            Debug.LogError(
                "HabitListManager를 찾을 수 없습니다."
            );

            return;
        }


        // -----------------------------------------
        // Life 화면으로 복귀
        // -----------------------------------------

        HabitUIManager uiManager =
            FindObjectOfType<HabitUIManager>();

        if (uiManager != null)
        {
            uiManager.BackToLife();
        }


        // 입력창 초기화
        habitNameInput.text = "";
    }
}