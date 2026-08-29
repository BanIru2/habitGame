using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HabitGoalUI : MonoBehaviour
{
    [Header("Habit Name")]
    public TMP_InputField habitNameInput;


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


    [Header("Record Type")]
    public Button completeButton;
    public Button valueButton;

    private string selectedRecordType = "value";


    [Header("Period")]
    public Button dailyButton;
    public Button weeklyButton;

    private string selectedPeriod = "daily";


    // =========================================
    // Add Habit 화면이 켜질 때마다 초기화
    // =========================================

    private void OnEnable()
    {
        ResetForm();
    }


    // =========================================
    // 입력 폼 초기화
    // =========================================

    public void ResetForm()
    {
        // 습관 이름 초기화
        if (habitNameInput != null)
            habitNameInput.text = "";

        // -------------------------
        // Category 초기화
        // -------------------------
        selectedCategory = "";

        if (physicalButton != null)
            physicalButton.image.color = Color.white;

        if (rhythmButton != null)
            rhythmButton.image.color = Color.white;

        if (ecoButton != null)
            ecoButton.image.color = Color.white;

        if (growthButton != null)
            growthButton.image.color = Color.white;


        // -------------------------
        // Record Type 초기화
        // 아무것도 선택하지 않음
        // -------------------------
        selectedRecordType = "";

        if (completeButton != null)
            completeButton.image.color = Color.white;

        if (valueButton != null)
            valueButton.image.color = Color.white;


        // Record Type이 아직 선택되지 않았으므로
        // Amount 영역 비활성화
        if (amountPanel != null)
        {
            amountPanel.alpha = 0.4f;
            amountPanel.interactable = false;
            amountPanel.blocksRaycasts = false;
        }


        // -------------------------
        // Amount 초기화
        // -------------------------
        amount = 30;

        if (amountText != null)
            amountText.text = amount.ToString();

        if (unitDropdown != null)
        {
            unitDropdown.value = 0;
            unitDropdown.RefreshShownValue();
        }


        // -------------------------
        // Period 초기화
        // 아무것도 선택하지 않음
        // -------------------------
        selectedPeriod = "";

        if (dailyButton != null)
            dailyButton.image.color = Color.white;

        if (weeklyButton != null)
            weeklyButton.image.color = Color.white;
    }


    // =========================================
    // 기록 방식
    // =========================================

    public void CompleteMode()
    {
        selectedRecordType = "check";

        if (amountPanel != null)
        {
            amountPanel.alpha = 0.4f;
            amountPanel.interactable = false;
            amountPanel.blocksRaycasts = false;
        }

        if (completeButton != null)
        {
            completeButton.image.color =
                new Color(0.9f, 1f, 0.9f);
        }

        if (valueButton != null)
        {
            valueButton.image.color = Color.white;
        }
    }


    public void ValueMode()
    {
        selectedRecordType = "value";

        if (amountPanel != null)
        {
            amountPanel.alpha = 1f;
            amountPanel.interactable = true;
            amountPanel.blocksRaycasts = true;
        }

        if (completeButton != null)
        {
            completeButton.image.color = Color.white;
        }

        if (valueButton != null)
        {
            valueButton.image.color =
                new Color(0.9f, 1f, 0.9f);
        }
    }


    // =========================================
    // 목표 수치
    // =========================================

    public void IncreaseAmount()
    {
        amount++;

        if (amountText != null)
        {
            amountText.text = amount.ToString();
        }
    }


    public void DecreaseAmount()
    {
        if (amount > 1)
        {
            amount--;
        }

        if (amountText != null)
        {
            amountText.text = amount.ToString();
        }
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


        // =========================================
        // Record Type에 따른 목표값
        // =========================================

        if (selectedRecordType == "check")
        {
            request.TargetAmount = 1;
            request.Unit = "check";
        }
        else
        {
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


        // =========================================
        // API 실패 시 로컬 테스트용 데이터
        // =========================================

        HabitGoalResponse localHabit =
            new HabitGoalResponse
            {
                UserId = request.UserId,

                GoalName = request.GoalName,

                Category = request.Category,

                RecordType = request.RecordType,

                TargetAmount = request.TargetAmount,

                Unit = request.Unit,

                Period = request.Period,

                IsActive = true
            };


        HabitGoalResponse habitToAdd = null;


        try
        {
            // ServiceRegistry 사용
            HabitGoalResponse response =
                await ServiceRegistry.Instance.Habit
                    .CreateGoalAsync(request);


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
            Debug.LogWarning(
                "Habit API 연결 실패 - 로컬 데이터로 추가합니다.\n"
                + e.Message
            );

            habitToAdd = localHabit;
        }


        // =========================================
        // Habit 리스트에 추가
        // =========================================

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


        // =========================================
        // Life 화면으로 복귀
        // =========================================

        HabitUIManager uiManager =
            FindObjectOfType<HabitUIManager>();


        if (uiManager != null)
        {
            uiManager.BackToLife();
        }
    }
}