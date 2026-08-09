using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RecordSpendManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField amountInput;

    [SerializeField] private OpenRecordPanel panelManager;

    public void Save()
    {
        if (string.IsNullOrWhiteSpace(amountInput.text))
            return;

        int amount = int.Parse(amountInput.text);

        SpendBudgetManager.Instance.AddSpending(amount);

        amountInput.text = "";

        panelManager.BackToSpend();
    }
}
