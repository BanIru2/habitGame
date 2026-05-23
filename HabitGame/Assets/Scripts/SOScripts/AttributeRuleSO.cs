using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Attribute Rule", fileName = "AttributeRule")]
public class AttributeRuleSO : ScriptableObject
{
    [Header("»ó¼º ¹èÀ² ; ¹ë·±½Ì Á¶Àı¿ë")]
    public float advantageMultiplier = 1.2f;
    public float neutralMultiplier = 1f;
    public float disadvantageMultiplier = 0.8f;

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
