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

    // =========================================
    // Long-term
    // =========================================
    public Button longTermButton;
    public GameObject durationPanel;
    public TMP_InputField durationInput;

    private string selectedPeriod = "";

    // Long-term은 현재 백엔드 DTO에 기간 필드가 없으므로
    // 로컬 테스트용으로만 보관
    private int selectedDurationDays = 0;


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

        if (longTermButton != null)
            longTermButton.image.color = Color.white;


        // -------------------------
        // Long-term 초기화
        // -------------------------
        selectedDurationDays = 0;

        if (durationInput != null)
            durationInput.text = "";

        if (durationPanel != null)
            durationPanel.SetActive(false);


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

        if (longTermButton != null)
            longTermButton.image.color = Color.white;


        // Daily
        if (period == "daily")
        {
            if (dailyButton != null)
            {
                dailyButton.image.color =
                    new Color(0.9f, 1f, 0.9f);
            }

            if (durationPanel != null)
                durationPanel.SetActive(false);
        }

        // Weekly
        else if (period == "weekly")
        {
            if (weeklyButton != null)
            {
                weeklyButton.image.color =
                    new Color(0.9f, 1f, 0.9f);
            }

            if (durationPanel != null)
                durationPanel.SetActive(false);
        }

        // Long-term
        else if (period == "long-term")
        {
            if (longTermButton != null)
            {
                longTermButton.image.color =
                    new Color(1f, 0.95f, 0.8f);
            }

            if (durationPanel != null)
                durationPanel.SetActive(true);
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

        // Value 방식은 모든 Period에서 Amount 사용
        if (selectedRecordType == "value")
        {
            enableAmount = true;
        }

        // Weekly + Complete
        // 예: 일주일에 운동 3회
        if (selectedRecordType == "check" &&
            selectedPeriod == "weekly")
        {
            enableAmount = true;
        }

        // Long-term + Complete
        // 예: 30일 동안 운동 20회
        if (selectedRecordType == "check" &&
            selectedPeriod == "long-term")
        {
            enableAmount = true;
        }


        amountPanel.alpha =
            enableAmount ? 1f : 0.4f;

        amountPanel.interactable =
            enableAmount;

        amountPanel.blocksRaycasts =
            enableAmount;


        if (unitDropdown != null)
        {
            bool completeWithFixedUnit =
                selectedRecordType == "check" &&
                (selectedPeriod == "weekly" ||
                 selectedPeriod == "long-term");

            unitDropdown.interactable =
                enableAmount && !completeWithFixedUnit;
        }
    }


    // =========================================
    // Long-term 기간 검사
    // =========================================
    private bool TryGetDurationDays(out int durationDays)
    {
        durationDays = 0;

        if (durationInput == null)
        {
            Debug.LogWarning(
                "Duration Input이 연결되지 않았습니다."
            );

            return false;
        }

        if (!int.TryParse(
                durationInput.text.Trim(),
                out durationDays))
        {
            Debug.LogWarning(
                "Duration에는 숫자를 입력해주세요."
            );

            return false;
        }

        if (durationDays <= 0)
        {
            Debug.LogWarning(
                "Duration은 1일 이상이어야 합니다."
            );

            return false;
        }

        return true;
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


        // =========================================
        // Long-term 기간 검사
        // =========================================
        if (selectedPeriod == "long-term")
        {
            if (!TryGetDurationDays(
                    out selectedDurationDays))
            {
                return;
            }
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

            // Long-term Complete
            // 예: 30일 동안 운동 20회
            else if (selectedPeriod == "long-term")
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

        if (selectedPeriod == "long-term")
        {
            Debug.Log(
                "Duration : " +
                selectedDurationDays +
                " Days"
            );
        }


        string json =
            JsonConvert.SerializeObject(
                request,
                Formatting.Indented
            );

        Debug.Log(json);


        // =========================================
        // 로컬 테스트 데이터
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


        // =========================================
        // Long-term
        //
        // 현재 백엔드 DTO에 durationDays/startDate/endDate가
        // 없으므로 서버 요청을 보내지 않고 로컬 테스트만 진행
        // =========================================
        if (selectedPeriod == "long-term")
        {
            Debug.Log(
                "===== Long-term Local Test =====\n" +
                "현재 서버 DTO에 장기목표 기간 필드가 없어 " +
                "API 요청 없이 로컬 목표로 생성합니다.\n" +
                "Duration : " +
                selectedDurationDays +
                " Days"
            );

            habitToAdd =
                localHabit;
        }

        // =========================================
        // Daily / Weekly
        // 기존 API 저장 방식 유지
        // =========================================
        else
        {
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