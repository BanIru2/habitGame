using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HabitListManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField habitNameInput;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject habitItemPrefab;

    [Header("Summary")]
    [SerializeField] private HabitSummaryManager summaryManager;

    // 습관 데이터 전체를 받아서 Item 생성
    public void AddHabit(HabitGoalResponse habit)
    {
        if (habit == null)
        {
            Debug.LogWarning("추가할 습관 데이터가 없습니다.");
            return;
        }

        CreateHabitItem(habit);

        if (habitNameInput != null)
            habitNameInput.text = "";

        if (summaryManager != null)
            summaryManager.RefreshSummary();
    }

    // DB에서 받아온 습관 목록으로 UI 갱신
    public void RefreshHabitList(List<HabitGoalResponse> habits)
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        if (habits == null)
            return;

        foreach (HabitGoalResponse habit in habits)
        {
            CreateHabitItem(habit);
        }

        if (summaryManager != null)
            summaryManager.RefreshSummary();
    }

    private void CreateHabitItem(HabitGoalResponse habit)
    {
        GameObject newHabit =
            Instantiate(habitItemPrefab, content);

        TextMeshProUGUI label =
            newHabit.transform.Find("Label")
                .GetComponent<TextMeshProUGUI>();

        label.text = habit.GoalName;

        Toggle toggle =
            newHabit.GetComponentInChildren<Toggle>();

        if (toggle != null)
            toggle.isOn = false;

        HabitItem habitItem =
            newHabit.GetComponent<HabitItem>();

        if (habitItem != null)
        {
            habitItem.SetData(habit);
        }
        else
        {
            Debug.LogError(
                "HabitItem Prefab 루트에 HabitItem 스크립트가 없습니다."
            );
        }
    }
}