using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Formula", fileName = "BattleFormula")]
public class BattleFormulaSO : ScriptableObject
{
    [Header("방어력 보정 상수 K")]
    public float defenseConstantK;    // 방어력 보정 상수 K
    [Header("치명타 데미지 배율")]
    public float critMultiplier;    // 치명타 데미지 배율
    [Header("랜덤 배율 최소 / 최대")]
    public float randomMultiplierMin;    // 랜덤 배율 최소값
    public float randomMultiplierMax;    // 랜덤 배율 최대값
    [Header("상성 레벨 계수")]
    // 상성 배율은 AttirbuteRuleSO.cs에서 정의
    public float attributeLevelScale;    // 상성 레벨 계수
    [Header("오로라 특성 보정 배율")]
    [Tooltip("최대 스택 [제한 없을 시 0]")]
    public int auroraStackMax = 0;    // 오로라 스택 최대 Clamp
    [Tooltip("스택 당 배율 수치 증가량")]
    public float auroraMultiplierPerStack;    // 오로라 스택 당 증가 비율

    // 방어력 보정값 산출
    public float GetDefenseMultiplier(float defense)
    {   
        return defenseConstantK / (defenseConstantK + defense);
    }
    // 특성 레벨 차이에 따른 배율 산출
    public float GetAttributeLevelMultiplier(int levelDifference)
    {
        return 1f + levelDifference * attributeLevelScale;
    }
    // 오로라 특성 보정 배율 산출
    public float GetAuroraMultiplier(int auroraStack)
    {
        // 오로라 배율에 최대치를 정하는 경우 최대치 고정을 위한 기능
        if (auroraStackMax > 0)
            auroraStack = Mathf.Min(auroraStack, auroraStackMax);

        return 1f + auroraStack * auroraMultiplierPerStack;
    }
}
