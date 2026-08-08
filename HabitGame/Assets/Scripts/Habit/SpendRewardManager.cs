using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SpendRewardManager : MonoBehaviour
{
    public static SpendRewardManager Instance;

    [SerializeField]
    private TextMeshProUGUI goldText;

    private int gold = 1200;

    void Awake()
    {
        Instance = this;
        UpdateUI();
    }

    public void AddGold(int value)
    {
        gold += value;
        UpdateUI();
    }

    void UpdateUI()
    {
        goldText.text = gold + " Gold";
    }
}
