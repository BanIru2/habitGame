using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HabitListManager : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField habitNameInput;

    [SerializeField]
    private Transform content;

    [SerializeField]
    private GameObject habitItemPrefab;

    [Header("Summary")]
    [SerializeField]
    private HabitSummaryManager summaryManager;

    // =========================================
    // Habit 하나 추가
    // =========================================
    public void AddHabit(
        HabitGoalResponse habit)
    {
        if (habit == null)
        {
            Debug.LogWarning(
                "추가할 습관 데이터가 없습니다."
            );
            return;
        }

        CreateHabitItem(habit);

        if (habitNameInput != null)
        {
            habitNameInput.text = "";
        }

        RefreshSummary();
    }

    // =========================================
    // 서버에서 받은 Habit 목록으로 UI 갱신
    // =========================================
    public void RefreshHabitList(
        List<HabitGoalResponse> habits)
    {
        if (content == null)
        {
            Debug.LogError(
                "HabitListManager의 Content가 연결되지 않았습니다."
            );
            return;
        }

        // 기존 Habit UI 제거
        for (int i = content.childCount - 1;
             i >= 0;
             i--)
        {
            Transform existingHabit =
                content.GetChild(i);

            existingHabit.SetParent(null, false);

            Destroy(
                existingHabit.gameObject
            );
        }

        if (habits == null)
        {
            RefreshSummary();
            return;
        }

        foreach (HabitGoalResponse habit in habits)
        {
            if (habit == null)
                continue;

            CreateHabitItem(habit);
        }

        RefreshSummary();
    }

    // =========================================
    // Habit Item 생성
    // =========================================
    private void CreateHabitItem(
        HabitGoalResponse habit)
    {
        if (habit == null)
        {
            return;
        }

        if (habitItemPrefab == null)
        {
            Debug.LogError(
                "HabitItemPrefab이 연결되지 않았습니다."
            );
            return;
        }

        if (content == null)
        {
            Debug.LogError(
                "Habit Content가 연결되지 않았습니다."
            );
            return;
        }

        GameObject newHabit =
            Instantiate(
                habitItemPrefab,
                content
            );

        // =========================================
        // Habit 이름 표시
        // =========================================
        Transform labelTransform =
            newHabit.transform.Find("Label");

        if (labelTransform != null)
        {
            TextMeshProUGUI label =
                labelTransform
                    .GetComponent<TextMeshProUGUI>();

            if (label != null)
            {
                label.text =
                    habit.GoalName;
            }
        }
        else
        {
            Debug.LogWarning(
                "HabitItem Prefab에서 Label을 찾을 수 없습니다."
            );
        }

        // =========================================
        // Toggle 기본 상태
        // =========================================
        Toggle toggle =
            newHabit.GetComponentInChildren<Toggle>();

        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(
                habit.CompletedToday
            );

            toggle.interactable =
                !habit.CompletedToday;
        }

        // =========================================
        // Habit 데이터 연결
        // =========================================
        HabitItem habitItem =
            newHabit.GetComponent<HabitItem>();

        if (habitItem != null)
        {
            habitItem.SetData(habit);
        }
        else
        {
            Debug.LogError(
                "HabitItem Prefab 루트에 " +
                "HabitItem 스크립트가 없습니다."
            );
        }
    }

    // =========================================
    // Summary 갱신
    // =========================================
    private void RefreshSummary()
    {
        if (summaryManager != null)
        {
            summaryManager.RefreshSummary();
        }
    }
}