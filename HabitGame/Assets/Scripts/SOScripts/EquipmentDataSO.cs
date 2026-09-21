using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

    public const int MaxLevel = 10;
    
    // ---------------------- 티어 치환 프로퍼티 ------------------------
    // 아이템 해금 조건 레벨에 따라 티어 결정
    public int Tier => unlockCondition.requiredAttributeLevel switch
    {
        >= 7 => 4, // 4티어
        >= 4 => 3, // 3티어
        >= 1 => 2, // 2티어
        _ => 1     // 1티어
    };

    // 재료로 사용 시 베이스 장비에 제공하는 경험치
    public int ProvideExp => Tier switch
    {
        4 => 80,
        3 => 40,
        2 => 20,
        _ => 10
    };

    // 티어별 UI 테두리/텍스트 색상
    public Color TierColor => Tier switch
    {
        4 => new Color32(163, 73, 255, 255),  // 4티어: 보라색
        3 => new Color32(50, 150, 255, 255),  // 3티어: 파란색
        2 => new Color32(80, 220, 100, 255),  // 2티어: 초록색
        _ => new Color32(200, 200, 200, 255)  // 1티어: 회색
    };

    // ------------------------ 강화식 헬퍼 함수 ---------------------------
    // 해당 레벨의 레벨업 필요 경험치: Lv * Tier * 10 EXP (Lv.1: 10, Lv.2: 20, Lv.3: 30, Lv.4: 40 ...)
    public int GetRequiredExp(int level) => level * Tier * 10;


    // 강화 레벨에 따른 스탯 배율 (기본 스탯 * (1 + (Lv - 1) * 0.1))
    public static float GetStatMultiplier(int level) => 1f + (level - 1) * 0.1f;
    // ---------------------------------------------------------------------


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
