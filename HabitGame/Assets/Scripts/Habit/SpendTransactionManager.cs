using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SpendTransactionManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject transactionItemPrefab;

    private void Start()
    {
        CreateTestTransactions();
    }

    private void CreateTestTransactions()
    {
        AddTransaction("Cafe", 5000, "2026.08.17");
        AddTransaction("Food", 12000, "2026.08.17");
        AddTransaction("Transport", 1500, "2026.08.16");
        AddTransaction("Shopping", 30000, "2026.08.15");
        AddTransaction("Convenience Store", 8500, "2026.08.14");
    }

    private void AddTransaction(
        string category,
        int amount,
        string date)
    {
        GameObject newItem =
            Instantiate(transactionItemPrefab, content);

        newItem.transform.Find("CategoryText")
            .GetComponent<TextMeshProUGUI>()
            .text = category;

        newItem.transform.Find("AmountText")
            .GetComponent<TextMeshProUGUI>()
            .text = "-" + amount.ToString("N0") + "₩";

        newItem.transform.Find("DateText")
            .GetComponent<TextMeshProUGUI>()
            .text = date;
    }
}
