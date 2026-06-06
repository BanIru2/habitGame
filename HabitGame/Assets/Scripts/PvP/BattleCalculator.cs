using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleCalculator
{
    // 데미지 연산 공식
    // atk * (K/(K+def)) * crtkDamage * attrMult * (attrLevDiff * attrLevelScale + 1) * auroraMult * randMod
    public static float CalculateDamage(BattleUnit attacker, BattleUnit defender, bool isCrit, int turn)
    {
        BattleFormulaSO formula = SORegistry.Instance.BattleFormula;
        AttributeRuleSO attributeRule = SORegistry.Instance.AttributeRule;

        float atk = attacker.atk;
        float defMult = formula.GetDefenseMultiplier(defender.def);
        float critDamage = isCrit ? formula.critMultiplier : 1;
        float attrMult = attributeRule.GetMatchupMultiplier(attacker.attribute, defender.attribute);
        int levelDif = attacker.attributeLevel - defender.attributeLevel;
        float attrLevMult = formula.GetAttributeLevelMultiplier(levelDif);
        float auroraMult = (attacker.attribute == AttributeType.Aurora) ? formula.GetAuroraMultiplier(turn) : 1;
        float randMod = Random.Range(formula.randomMultiplierMin, formula.randomMultiplierMax);

        float damage = atk * defMult * critDamage * attrMult * attrLevMult * auroraMult * randMod;

        return Mathf.Max(1, damage);    // 최소 데미지 1 보장
    }

}
