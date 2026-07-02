using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] private ItemSlotUI itemSlotPrefab;
    [SerializeField] private Transform itemSlotParent;
    [SerializeField] private List<ItemDataSO> testItems;

    private readonly List<ItemSlotUI> spawnedSlots = new List<ItemSlotUI>();

    // InventoryTap이 켜질때 마다 아이템 목록 다시 그리기
    private void OnEnable()
    {
        RenderTestItems();
    }

    // 테스트용 아이템 SO목록을 화면에 슬롯으로 생성
    private void RenderTestItems()
    {
        ClearSlots();

        for (int i = 0; i < testItems.Count; i++)
        {
            ItemDataSO itemSO = testItems[i];
            if (itemSO == null) continue;

            InventoryItemResponse response = CreateTestInventoryItem(itemSO, i);

            ItemSlotUI slot = Instantiate(itemSlotPrefab, itemSlotParent);
            slot.LoadData(response, itemSO, OnItemSlotClicked);

            spawnedSlots.Add(slot);
        }
    }

    // 테스트용 InventoryItemResponse 생성
    private InventoryItemResponse CreateTestInventoryItem(ItemDataSO itemSO, int index)
    {
        return new InventoryItemResponse
        {
            InventoryId = index + 1,
            ItemId = string.IsNullOrEmpty(itemSO.itemId) ? itemSO.name : itemSO.itemId,
            ItemType = itemSO.itemType.ToString(),
            Quantity = itemSO is ConsumableDataSO ? index + 1 : 1,
            IsEquipped = itemSO is EquipmentDataSO && index == 0,
            SlotType = itemSO is EquipmentDataSO equipmentSO
                ? equipmentSO.equipmentType.ToString()
                : null
        };
    }

    // 이전에 만들어진 슬롯 지우기
    // OnEnable 다회 호출에 대비한 초기화
    private void ClearSlots()
    {
        foreach (ItemSlotUI slot in spawnedSlots)
        {
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }

        spawnedSlots.Clear();
    }

    // ItemSlotUI.cs로 넘겨줄 onClick함수
    private void OnItemSlotClicked(InventoryItemResponse data, ItemDataSO itemSO)
    {
        Debug.Log($"아이템 클릭: {itemSO.displayName}, inventoryId: {data.InventoryId}");
    }
}
