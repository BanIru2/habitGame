using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Attribute Rule", fileName = "AttributeRule")]
public class AttributeRuleSO : ScriptableObject
{
    [Header("상성 배율 ; 밸런싱 조절용")]
    public float advantageMultiplier = 1.2f;
    public float neutralMultiplier = 1f;
    public float disadvantageMultiplier = 0.8f;

    // 상성 관계를 따져 상성 배율 반환
    // 실제 데미지 연산 시 호출
    public float GetMatchupMultiplier(AttributeType attacker, AttributeType defender)
    {
        if (attacker == AttributeType.None || defender == AttributeType.None)
            return neutralMultiplier;

        if (attacker == AttributeType.Aurora || defender == AttributeType.Aurora)
            return neutralMultiplier;

        if (attacker == defender)
            return neutralMultiplier;

        if (IsAdvantage(attacker, defender))
            return advantageMultiplier;
        // defender > attacker
        return disadvantageMultiplier;
    }

    // attacker가 이기는 상황에 대해 true
    private bool IsAdvantage(AttributeType attacker, AttributeType defender)
    {
        switch (attacker)
        {
            case AttributeType.Fire:
                return defender == AttributeType.Grass;

            case AttributeType.Water:
                return defender == AttributeType.Fire;

            case AttributeType.Grass:
                return defender == AttributeType.Water;

            default:
                return false;
        }
    }
}
