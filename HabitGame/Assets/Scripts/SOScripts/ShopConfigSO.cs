using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShopItemEntry
{
    public ItemDataSO item;
    public bool isAvailable = true;    // 해당 아이템 상점 구매 활성화 여부

    // 특수 조건이 필요한 아이템에 대해 적용할 경우 채용
/*    [Header("조건 미달 시 구매 정책")]
    public bool allowPurchaseWhenLocked;
    [Min(1f)] public float lockedCostMultiplier = 2f;

    public int GetLockedCost()
    {
        return Mathf.CeilToInt(item.cost * lockedCostMultiplier);
    }*/
}

[CreateAssetMenu(menuName = "Shop/Config", fileName = "ShopConfig")]
public class ShopConfigSO : ScriptableObject
{
    public ShopItemEntry[] equipmentItems;
    public ShopItemEntry[] consumableItems;
}
