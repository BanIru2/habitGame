using LayerLab.ArtMakerUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

    [Header("장비 티어")]
    [SerializeField, Range(0, 4)]
    [Tooltip("1~4: 명시적 티어. 0: 기존 에셋의 해금 레벨 기준 티어를 유지합니다.")]
    private int tier;

    public const int MaxLevel = 10;

    [Header("에셋 연동 데이터")]
    public PartsType partsType; // Helmet, Chest, Sword 등
    [SerializeField]
    private int partsIndex; // 에셋의 스프라이트 번호 (0, 1, 2...)
    public int PartsIndex => partsIndex;

    // ---------------------- 티어 치환 프로퍼티 ------------------------
    public int Tier => tier > 0 ? Mathf.Clamp(tier, 1, 4) : LegacyTier;

    // 아이템 해금 조건 레벨에 따라 티어 결정
    private int LegacyTier => (unlockCondition?.requiredAttributeLevel ?? 0) switch
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        // ItemType 항상 Equipment로 유지
        itemType = ItemType.Equipment;
        // 아이콘이 등록되었을 때 자동으로 인덱스 역추적
        if (icon != null)
        {
            AutoMatchPartsIndex();
        }
    }
    private void AutoMatchPartsIndex()
    {
        if (icon == null) return;
        string assetPath = AssetDatabase.GetAssetPath(icon);
        if (string.IsNullOrEmpty(assetPath)) return;
        string folderPath = Path.GetDirectoryName(assetPath);
        string fileName = Path.GetFileName(assetPath);
        // 폴더 이름을 가져와서 partsType 자동 매칭
        // 예: ".../Thumbnail/Sword" -> "Sword"
        string folderName = new DirectoryInfo(folderPath).Name;

        // "Sub_Item" 폴더의 경우 언더바(_) 제거 처리
        if (folderName.Equals("Sub_Item", StringComparison.OrdinalIgnoreCase))
            folderName = "SubItem";
        // 폴더 이름과 똑같은 PartsType Enum으로 자동 변환
        if (Enum.TryParse<PartsType>(folderName, true, out var matchedType))
        {
            partsType = matchedType;
        }
        // --- 기존 파일 순서(인덱스) 계산 로직 유지 ---
        string[] files = Directory.GetFiles(folderPath, "*.png");
        Array.Sort(files);
        for (int i = 0; i < files.Length; i++)
        {
            if (Path.GetFileName(files[i]) == fileName)
            {
                partsIndex = i;
                EditorUtility.SetDirty(this);
                break;
            }
        }
    }
#endif
}
