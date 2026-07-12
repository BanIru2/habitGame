using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HabitListManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField habitNameInput;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject habitItemPrefab;

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
}
