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

    private int amount = 3;

    [Header("Category")]
    public Button physicalButton;
    public Button rhythmButton;
    public Button ecoButton;
    public Button growthButton;

    private string selectedCategory = "";

    [Header("Record Type")]
    public Button completeButton;
    public Button valueButton;

    private string selectedRecordType = "";

    [Header("Period")]
    public Button dailyButton;
    public Button weeklyButton;

    private string selectedPeriod = "";

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
        // Habit Name
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
        // -------------------------
        selectedRecordType = "";

        if (completeButton != null)
            completeButton.image.color = Color.white;

        if (valueButton != null)
            valueButton.image.color = Color.white;

        // -------------------------
        // Period 초기화
        // -------------------------
        selectedPeriod = "";

        if (dailyButton != null)
            dailyButton.image.color = Color.white;

        if (weeklyButton != null)
            weeklyButton.image.color = Color.white;

        // -------------------------
        // Amount 초기화
        // -------------------------
        amount = 3;

        if (amountText != null)
            amountText.text = amount.ToString();

        if (unitDropdown != null)
        {
            unitDropdown.value = 0;
            unitDropdown.RefreshShownValue();
        }

        UpdateAmountPanel();
    }

    // =========================================
    // Complete 방식
    // =========================================
    public void CompleteMode()
    {
        selectedRecordType = "check";

        if (completeButton != null)
        {
            completeButton.image.color =
                new Color(0.9f, 1f, 0.9f);
        }

        if (valueButton != null)
        {
            valueButton.image.color =
                Color.white;
        }

        UpdateAmountPanel();
    }

    // =========================================
    // Value 방식
    // =========================================
    public void ValueMode()
    {
        selectedRecordType = "value";

        if (completeButton != null)
        {
            completeButton.image.color =
                Color.white;
        }

        if (valueButton != null)
        {
            valueButton.image.color =
                new Color(0.9f, 1f, 0.9f);
        }

        UpdateAmountPanel();
    }

    // =========================================
    // Amount 증가
    // =========================================
    public void IncreaseAmount()
    {
        amount++;

        if (amountText != null)
        {
            amountText.text =
                amount.ToString();
        }
    }

    // =========================================
    // Amount 감소
    // =========================================
    public void DecreaseAmount()
    {
        if (amount > 1)
        {
            amount--;
        }

        if (amountText != null)
        {
            amountText.text =
                amount.ToString();
        }
    }

    // =========================================
    // Category 선택
    // =========================================
    public void SelectCategory(string category)
    {
        selectedCategory = category;

        if (physicalButton != null)
            physicalButton.image.color = Color.white;

        if (rhythmButton != null)
            rhythmButton.image.color = Color.white;

        if (ecoButton != null)
            ecoButton.image.color = Color.white;

        if (growthButton != null)
            growthButton.image.color = Color.white;

        switch (category)
        {
            case "physical":
                if (physicalButton != null)
                {
                    physicalButton.image.color =
                        new Color(1f, 0.9f, 0.9f);
                }
                break;

            case "rhythm":
                if (rhythmButton != null)
                {
                    rhythmButton.image.color =
                        new Color(0.9f, 0.95f, 1f);
                }
                break;

            case "eco":
                if (ecoButton != null)
                {
                    ecoButton.image.color =
                        new Color(0.9f, 1f, 0.9f);
                }
                break;

            case "growth":
                if (growthButton != null)
                {
                    growthButton.image.color =
                        new Color(0.95f, 0.9f, 1f);
                }
                break;
        }
    }

    // =========================================
    // Period 선택
    // =========================================
    public void SelectPeriod(string period)
    {
        selectedPeriod = period;

        if (dailyButton != null)
            dailyButton.image.color = Color.white;

        if (weeklyButton != null)
            weeklyButton.image.color = Color.white;

        if (period == "daily")
        {
            if (dailyButton != null)
            {
                dailyButton.image.color =
                    new Color(0.9f, 1f, 0.9f);
            }
        }
        else if (period == "weekly")
        {
            if (weeklyButton != null)
            {
                weeklyButton.image.color =
                    new Color(0.9f, 1f, 0.9f);
            }
        }

        UpdateAmountPanel();
    }

    // =========================================
    // Amount 영역 상태 갱신
    // =========================================
    private void UpdateAmountPanel()
    {
        if (amountPanel == null)
            return;

        bool enableAmount = false;

        // Value 방식은 Daily/Weekly 모두 Amount 사용
        if (selectedRecordType == "value")
        {
            enableAmount = true;
        }

        // Weekly + Complete는 "주 몇 회" 설정
        if (selectedRecordType == "check" &&
            selectedPeriod == "weekly")
        {
            enableAmount = true;
        }

        amountPanel.alpha =
            enableAmount ? 1f : 0.4f;

        amountPanel.interactable =
            enableAmount;

        amountPanel.blocksRaycasts =
            enableAmount;

        // Weekly + Complete에서는 단위를 직접 선택하지 않음
        if (unitDropdown != null)
        {
            bool weeklyComplete =
                selectedRecordType == "check" &&
                selectedPeriod == "weekly";

            unitDropdown.interactable =
                enableAmount && !weeklyComplete;
        }
    }

    // =========================================
    // 저장
    // =========================================
    public async void SaveGoal()
    {
        // Habit Name
        if (habitNameInput == null ||
            string.IsNullOrWhiteSpace(habitNameInput.text))
        {
            Debug.LogWarning(
                "Habit Name을 입력해주세요."
            );
            return;
        }

        // Category
        if (string.IsNullOrWhiteSpace(selectedCategory))
        {
            Debug.LogWarning(
                "Category를 선택해주세요."
            );
            return;
        }

        // Record Type
        if (string.IsNullOrWhiteSpace(selectedRecordType))
        {
            Debug.LogWarning(
                "Record Type을 선택해주세요."
            );
            return;
        }

        // Period
        if (string.IsNullOrWhiteSpace(selectedPeriod))
        {
            Debug.LogWarning(
                "Repeat을 선택해주세요."
            );
            return;
        }

        CreateHabitGoalRequest request =
            new CreateHabitGoalRequest();

        request.GoalName =
            habitNameInput.text.Trim();

        request.Category =
            selectedCategory;

        request.RecordType =
            selectedRecordType;

        request.Period =
            selectedPeriod;

        // =========================================
        // 목표값 결정
        // =========================================
        if (selectedRecordType == "check")
        {
            // Weekly Complete
            // 예: 일주일에 운동 3회
            if (selectedPeriod == "weekly")
            {
                request.TargetAmount =
                    amount;

                request.Unit =
                    "회";
            }

            // Daily Complete
            else
            {
                request.TargetAmount =
                    1;

                request.Unit =
                    "check";
            }
        }
        else
        {
            // Value 방식
            request.TargetAmount =
                amount;

            if (unitDropdown != null &&
                unitDropdown.options.Count > 0)
            {
                request.Unit =
                    unitDropdown
                        .options[unitDropdown.value]
                        .text;
            }
            else
            {
                request.Unit = "";
            }
        }

        // =========================================
        // 요청 확인
        // =========================================
        Debug.Log(
            "===== Habit Goal ====="
        );

        Debug.Log(
            "Name : " +
            request.GoalName
        );

        Debug.Log(
            "Category : " +
            request.Category
        );

        Debug.Log(
            "RecordType : " +
            request.RecordType
        );

        Debug.Log(
            "Amount : " +
            request.TargetAmount
        );

        Debug.Log(
            "Unit : " +
            request.Unit
        );

        Debug.Log(
            "Period : " +
            request.Period
        );

        string json =
            JsonConvert.SerializeObject(
                request,
                Formatting.Indented
            );

        Debug.Log(json);

        // =========================================
        // 서버 연결 실패 시 로컬 테스트 데이터
        // =========================================
        HabitGoalResponse localHabit =
            new HabitGoalResponse
            {
                UserId =
                    ApiClient.Instance.CurrentUserId,

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

                IsActive =
                    true
            };

        HabitGoalResponse habitToAdd = null;

        try
        {
            HabitGoalResponse response =
                await ServiceRegistry.Instance.Habit
                    .CreateGoalAsync(request);

            if (response != null)
            {
                habitToAdd =
                    response;

                Debug.Log(
                    "===== API Success ====="
                );

                Debug.Log(
                    "Goal ID : " +
                    response.Id
                );

                Debug.Log(
                    "Message : " +
                    response.Message
                );
            }
            else
            {
                Debug.LogWarning(
                    "API 응답이 비어있어 로컬 데이터로 표시합니다."
                );

                habitToAdd =
                    localHabit;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(
                "Habit API 연결 실패 - 로컬 데이터로 추가합니다.\n" +
                e.Message
            );

            habitToAdd =
                localHabit;
        }

        // =========================================
        // Habit List에 추가
        // =========================================
        HabitListManager listManager =
            FindObjectOfType<HabitListManager>();

        if (listManager != null)
        {
            listManager.AddHabit(
                habitToAdd
            );
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