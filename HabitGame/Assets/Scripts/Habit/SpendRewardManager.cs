using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SpendRewardManager : MonoBehaviour
{
    public static SpendRewardManager Instance;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI totalGoldText;
    [SerializeField] private TextMeshProUGUI baseGoldText;
    [SerializeField] private TextMeshProUGUI bonusGoldText;

    [Header("Reward Setting")]
    [SerializeField] private int maxWeeklyGold = 1000;

    // 예산 절약에 따른 기본 보상
    private int baseGold = 0;

    // 특수 목표 보너스
    private int bonusGold = 0;

    // 사용률
    private float usedRate = 0f;

    // 최종 획득 가능 골드
    public int TotalGold => baseGold + bonusGold;

    public int BaseGold => baseGold;
    public int BonusGold => bonusGold;

    private void Awake()
    {
        Instance = this;
    }

    // 예산 / 소비금액 기준 기본 보상 계산
    public void CalculateReward(int budget, int spent)
    {
        if (budget <= 0)
        {
            usedRate = 0f;
            baseGold = 0;

            UpdateUI();
            return;
        }

        // 사용률
        usedRate = (float)spent / budget;

        // 남은 예산
        int savedMoney = budget - spent;

        if (savedMoney < 0)
        {
            savedMoney = 0;
        }

        // 절약률
        float savingRate =
            (float)savedMoney / budget;

        savingRate = Mathf.Clamp01(savingRate);

        // 기본 보상
        baseGold =
            Mathf.RoundToInt(
                maxWeeklyGold * savingRate
            );

        Debug.Log("===== 소비 보상 계산 =====");
        Debug.Log($"Budget : {budget:N0}");
        Debug.Log($"Spent : {spent:N0}");
        Debug.Log($"Used Rate : {usedRate * 100f:0}%");
        Debug.Log($"Saving Rate : {savingRate * 100f:0}%");
        Debug.Log($"Base Gold : {baseGold}");
        Debug.Log($"Bonus Gold : {bonusGold}");
        Debug.Log($"Total Gold : {TotalGold}");

        UpdateUI();
    }

    // 특수 목표 보상 추가
    public void AddBonusGold(int value)
    {
        bonusGold += value;

        if (bonusGold < 0)
        {
            bonusGold = 0;
        }

        UpdateUI();
    }

    // 특수 목표 보상 직접 설정
    public void SetBonusGold(int value)
    {
        bonusGold = Mathf.Max(0, value);

        UpdateUI();
    }

    // 기존 SpendGoalItem 호환용
    public void AddGold(int value)
    {
        AddBonusGold(value);
    }

    // 기존 SpendGoalItem 호환용
    public void SetGold(int totalGold)
    {
        bonusGold = Mathf.Max(
            0,
            totalGold - baseGold
        );

        UpdateUI();
    }

    public int GetBaseGold()
    {
        return baseGold;
    }

    public int GetBonusGold()
    {
        return bonusGold;
    }

    public int GetTotalGold()
    {
        return TotalGold;
    }

    private void UpdateUI()
    {
        // 전체 획득 가능 골드
        if (totalGoldText != null)
        {
            totalGoldText.text =
                $"{TotalGold:N0} Gold";
        }

        // 기본 보상 + 사용률
        if (baseGoldText != null)
        {
            baseGoldText.text =
                $"Base Reward ({usedRate * 100f:0}% Used) : {baseGold:N0}";
        }

        // 특수 목표 보너스
        if (bonusGoldText != null)
        {
            bonusGoldText.text =
                $"Bonus Goal : +{bonusGold:N0}";
        }
    }
}