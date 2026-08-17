using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HabitListManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField habitNameInput;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject habitItemPrefab;

    // 기존 습관 하나 추가
    public void AddHabit()
    {
        if (string.IsNullOrWhiteSpace(habitNameInput.text))
            return;

        GameObject newHabit =
            Instantiate(habitItemPrefab, content);

        TextMeshProUGUI label =
            newHabit.transform.Find("Label")
            .GetComponent<TextMeshProUGUI>();

        label.text = habitNameInput.text;

        Toggle toggle =
            newHabit.GetComponentInChildren<Toggle>();

        toggle.isOn = false;

        habitNameInput.text = "";
    }

    // ⭐ DB에서 받아온 습관 목록으로 UI 갱신
    public void RefreshHabitList(List<HabitGoalResponse> habits)
    {
        // 기존 리스트 삭제
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        // DB에서 받아온 습관을 하나씩 생성
        foreach (HabitGoalResponse habit in habits)
        {
            GameObject newHabit =
                Instantiate(habitItemPrefab, content);

            TextMeshProUGUI label =
                newHabit.transform.Find("Label")
                .GetComponent<TextMeshProUGUI>();

            label.text = habit.GoalName;

            Toggle toggle =
                newHabit.GetComponentInChildren<Toggle>();

            toggle.isOn = false;
        }
    }
}