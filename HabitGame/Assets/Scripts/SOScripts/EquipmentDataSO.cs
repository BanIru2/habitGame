using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EquipmentType
{
    Weapon, Clothes, Shoes, Hat
}

[CreateAssetMenu(menuName = "Item/Equipment Data", fileName = "EquipmentData")]
public class EquipmentDataSO : ItemDataSO
{
    [Header("장비 종류")]
    public EquipmentType equipmentType;
    [Header("상승 스탯 종류 / 수치")]
    public StatBonus[] statBonuses;

    // 무기에 특성 부여 위한 변수
    // 추후 개발 시 고려 및 추가
/*    public bool hasAttributeBonus;
    public AttributeType attributeType;
    public float attributeDamageBonus;*/

    // ItemType 항상 Equipment로 유지
    private void OnValidate()
    {
        itemType = ItemType.Equipment;
    }
}
