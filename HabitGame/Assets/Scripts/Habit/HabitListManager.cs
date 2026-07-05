using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HabitListManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField habitNameInput;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject habitItemPrefab;

    public void AddHabit()
    {
        //if (string.IsNullOrWhiteSpace(habitNameInput.text))
        //    return;

        //GameObject newHabit = Instantiate(habitItemPrefab, content);

        //Transform label = newHabit.transform.Find("Label");

        //label.GetComponent<TextMeshProUGUI>().text = habitNameInput.text;

        //habitNameInput.text = "";
        Debug.Log("1");

        if (string.IsNullOrWhiteSpace(habitNameInput.text))
            return;

        Debug.Log("2");

        GameObject newHabit = Instantiate(habitItemPrefab, content);

        Debug.Log("3");

        Transform label = newHabit.transform.Find("Label");

        Debug.Log("4");

        label.GetComponent<TextMeshProUGUI>().text = habitNameInput.text;

        Debug.Log("5");

        habitNameInput.text = "";
    }
}
