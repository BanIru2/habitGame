using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpendListManager : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private TMP_InputField goalNameInput;
    [SerializeField] private TMP_InputField goldInput;

    [Header("UI")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject spendItemPrefab; 

    [Header("Reward")]
    [SerializeField] private int reward = 300;

    [SerializeField] private SpendCycleManager cycleManager;
    [SerializeField] private GameObject spendPanel;
    [SerializeField] private GameObject spendAddPanel;

    public void AddSpendGoal()
    {
        if (string.IsNullOrWhiteSpace(goalNameInput.text))
            return;

        if (string.IsNullOrWhiteSpace(goldInput.text))
            return;

        GameObject newItem = Instantiate(spendItemPrefab, content);
        //Debug.Log(newItem.transform.Find("GoalDescription"));
        newItem.transform.Find("GoalName")
            .GetComponent<TextMeshProUGUI>().text = goalNameInput.text;

        string cycle = cycleManager.isWeekly ? "Weekly" : "Daily";

        newItem.transform.Find("GoalDescription")
            .GetComponent<TextMeshProUGUI>().text =
            "Limit : " + goldInput.text + " KRW (" + cycle + ")";

        newItem.transform.Find("RewardText")
            .GetComponent<TextMeshProUGUI>().text =
            "+" + reward;

        Toggle toggle = newItem.transform.Find("CompleteToggle")
            .GetComponent<Toggle>();

        toggle.isOn = false;

        goalNameInput.text = "";
        goldInput.text = "";
        cycleManager.SelectDaily();

        spendAddPanel.SetActive(false);
        spendPanel.SetActive(true);
    }
}
