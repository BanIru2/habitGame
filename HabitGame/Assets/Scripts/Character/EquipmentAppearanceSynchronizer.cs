using System.Collections.Generic;
using LayerLab.ArtMakerUnity;
using UnityEngine;

public static class EquipmentAppearanceSynchronizer
{
    // 실제 인벤토리 장착 시스템이 관리하는 파츠
    private static readonly PartsType[] ManagedEquipmentParts =
    {
        PartsType.Helmet,
        PartsType.Chest,

        PartsType.Sword,
        PartsType.Axe,
        PartsType.Bow,
        PartsType.Wand,
        PartsType.Staff,
        PartsType.Spear,
        PartsType.Blunt,
        PartsType.Crossbow,

        PartsType.Shield,
        PartsType.SubItem,
    };

    // 게임 캐릭터용 최초 초기화
    public static void InitializeGameCharacter(PartsManager target)
    {
        if (target == null)
        {
            Debug.LogWarning("초기화할 PartsManager가 없습니다.");
            return;
        }

        // 공용 초기화 함수는 그대로 사용
        target.Init();

        // Init()이 기본으로 활성화하는 장비 파츠 제거
        foreach (PartsType type in ManagedEquipmentParts)
        {
            target.UnequipParts(type);
        }
    }

    // 현재 장착 상태와 캐릭터 외형 동기화
    public static void Sync(PartsManager target, IReadOnlyDictionary<PartsType, int> equippedParts)
    {
        if (target == null)
        {
            Debug.LogWarning("장비 외형을 적용할 PartsManager가 없습니다.");
            return;
        }

        foreach (PartsType type in ManagedEquipmentParts)
        {
            int desiredIndex = -1;

            bool hasDesiredPart = equippedParts != null && equippedParts.TryGetValue(type, out desiredIndex);

            if (!hasDesiredPart)
            {
                if (target.IsEquipped(type) || target.IsPartsVisible(type))
                {
                    target.UnequipParts(type);
                }

                continue;
            }

            int count = target.GetPartsCount(type);

            if (desiredIndex < 0 || desiredIndex >= count)
            {
                Debug.LogWarning($"유효하지 않은 장비 파츠입니다. type={type}, index={desiredIndex}, count={count}");
                target.UnequipParts(type);
                continue;
            }

            bool needsChange = !target.IsPartsVisible(type) || target.GetActiveIndex(type) != desiredIndex;

            if (needsChange)
            {
                target.EquipParts(type, desiredIndex);
            }
        }
    }
}