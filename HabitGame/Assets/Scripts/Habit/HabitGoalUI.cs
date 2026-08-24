using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HabitGoalUI : MonoBehaviour
{
    private HabitService habitService; 

    public CanvasGroup amountPanel;

    public TMP_Text amountText;
    public TMP_Dropdown unitDropdown;
    private int amount = 30;

    public Button physicalButton;
    public Button rhythmButton;
    public Button ecoButton;
    public Button growthButton;

    private string selectedCategory = "";

    public Button dailyButton;
    public Button weeklyButton;

    private string selectedPeriod = "daily";
    private string selectedRecordType = "value";
    private string submittedHabitName = "";

    private void Awake()
    {
        habitNameInput.onEndEdit.AddListener(OnHabitNameEndEdit);
    }

    private void Start()
    {
        habitService = new HabitService(ApiClient.Instance);
    }

    private void OnHabitNameEndEdit(string value)
    {
        submittedHabitName = value;
    }
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

    public void IncreaseAmount()
    {
        amount++;
        amountText.text = amount.ToString();
    }

    public void DecreaseAmount()
    {
        if (amount > 1)
            amount--;

        amountText.text = amount.ToString();
    }

    public void SelectCategory(string category) //카테고리
    {
        selectedCategory = category;

        physicalButton.image.color = Color.white;
        rhythmButton.image.color = Color.white;
        ecoButton.image.color = Color.white;
        growthButton.image.color = Color.white;

        switch (category)
        {
            case "physical":
                physicalButton.image.color = new Color(1f, 0.9f, 0.9f);
                break;

            case "rhythm":
                rhythmButton.image.color = new Color(0.9f, 0.95f, 1f);
                break;

            case "eco":
                ecoButton.image.color = new Color(0.9f, 1f, 0.9f);
                break;

            case "growth":
                growthButton.image.color = new Color(0.95f, 0.9f, 1f);
                break;
        }
    }
    public void SelectPeriod(string period)
    {
        selectedPeriod = period;

        dailyButton.image.color = Color.white;
        weeklyButton.image.color = Color.white;

        if (period == "daily")
            dailyButton.image.color = new Color(0.9f, 1f, 0.9f);
        else
            weeklyButton.image.color = new Color(0.9f, 1f, 0.9f);
    }

    public TMP_InputField habitNameInput;

    public async void SaveGoal()
    {
        string goalName = habitNameInput.text;
        if (string.IsNullOrWhiteSpace(goalName))
            goalName = submittedHabitName;

        Debug.Log("===== Habit Goal =====");
        Debug.Log("Name : " + goalName);
        Debug.Log("Category : " + selectedCategory);
        Debug.Log("RecordType : " + selectedRecordType);
        Debug.Log("Amount : " + amount);
        Debug.Log("Unit : " + unitDropdown.options[unitDropdown.value].text);
        Debug.Log("Period : " + selectedPeriod);

        CreateHabitGoalRequest request = new CreateHabitGoalRequest();

        request.UserId = ApiClient.Instance.CurrentUserId;
        request.GoalName = goalName;
        request.Category = selectedCategory;
        request.RecordType = selectedRecordType;
        request.TargetAmount = amount;
        request.Unit = unitDropdown.options[unitDropdown.value].text.ToLower();
        request.Period = selectedPeriod;

        string json = JsonConvert.SerializeObject(request, Formatting.Indented);
        Debug.Log(json);

        try
        {
            HabitGoalResponse response = await habitService.CreateGoalAsync(request);

            Debug.Log("===== API Success ====="); 
            Debug.Log("Goal ID : " + response.Id);
            Debug.Log("Message : " + response.Message);

            // 기존 저장 버튼 기능
            FindObjectOfType<HabitListManager>().AddHabit();
            FindObjectOfType<HabitUIManager>().BackToLife();
        }
        catch (System.Exception e)
        {
            Debug.LogError("API Error : " + e.Message);
        }
    }
}
