using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] private ItemSlotUI itemSlotPrefab;
    [SerializeField] private Transform itemSlotParent;
    [SerializeField] private List<ItemDataSO> testItems;

    [SerializeField]
    private Button equipButton;
    [SerializeField]
    private Button consumableButton;

    private readonly List<InventoryItemViewData> allItems = new List<InventoryItemViewData>();
    private readonly List<InventoryItemViewData> equipmentItems = new List<InventoryItemViewData>();
    private readonly List<InventoryItemViewData> consumableItems = new List<InventoryItemViewData>();

    private readonly List<ItemSlotUI> slotPool = new List<ItemSlotUI>();    // 아이템 정보를 출력할 슬롯 pool

    private void Awake()
    {
        equipButton.onClick.AddListener(ShowEquipmentItems);
        consumableButton.onClick.AddListener(ShowConsumableItems);
    }

    // InventoryTap이 켜질때 마다 아이템 목록 다시 그리기
    private void OnEnable()
    {
        List<InventoryItemResponse> responses = CreateTestInventoryResponses();

        BuildViewData(responses);
        ShowEquipmentItems();
    }

    // --------------------------------- 테스트 -----------------------------------------

    // 테스트용 InventoryItemResponse 생성
    private List<InventoryItemResponse> CreateTestInventoryResponses()
    {
        List<InventoryItemResponse> responses = new List<InventoryItemResponse>();

        for (int i = 0; i < testItems.Count; i++)
        {
            ItemDataSO itemSO = testItems[i];
            if (itemSO == null) continue;

            InventoryItemResponse response = CreateTestInventoryItem(itemSO, i);
            responses.Add(response);
        }

        return responses;
    }

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
    // ------------------------------------------------------------------------------------------------

    // 아이템 response와 so를 연결하고 장비/소비 분류
    private void BuildViewData(List<InventoryItemResponse> responses)
    {
        allItems.Clear();
        equipmentItems.Clear();
        consumableItems.Clear();

        foreach (InventoryItemResponse response in responses)
        {
            ItemDataSO itemSO = SORegistry.Instance.GetItem(response.ItemId);

            InventoryItemViewData viewData = new InventoryItemViewData { Response = response, ItemSO = itemSO };

            allItems.Add(viewData);

            if (itemSO is EquipmentDataSO)
            {
                equipmentItems.Add(viewData);
            }
            else if (itemSO is ConsumableDataSO)
            {
                consumableItems.Add(viewData);
            }
            else
            {
                Debug.LogWarning($"아이템 SO 매칭 실패 또는 분류 실패: {response.ItemId}");
            }
        }
    }

    // 아이템 데이터 ItemSlot으로 화면에 생성
    private void RenderItems(List<InventoryItemViewData> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            InventoryItemViewData item = items[i];

            ItemSlotUI slot = GetSlot(i);
            slot.gameObject.SetActive(true);

            slot.LoadData(item.Response, item.ItemSO, OnItemSlotClicked);
        }

        HideUnusedSlots(items.Count);
    }

    // 슬롯이 남아 있다면 재사용, 없다면 생성
    private ItemSlotUI GetSlot(int index)
    {
        if (index < slotPool.Count)
        {
            return slotPool[index];
        }

        ItemSlotUI slot = Instantiate(itemSlotPrefab, itemSlotParent);
        slotPool.Add(slot);

        return slot;
    }

    // 사용하지 않는 슬롯 숨기기
    private void HideUnusedSlots(int usedCount)
    {
        for (int i = usedCount; i < slotPool.Count; i++)
        {
            slotPool[i].gameObject.SetActive(false);
        }
    }

    // 장비 아이템 출력 (장비 버튼 onClick)
    public void ShowEquipmentItems()
    {
        RenderItems(equipmentItems);
    }

    // 소비 아이템 출력 (소비 버튼 onClick)
    public void ShowConsumableItems()
    {
        RenderItems(consumableItems);
    }

    // ItemSlotUI.cs로 넘겨줄 onClick함수
    private void OnItemSlotClicked(InventoryItemResponse data, ItemDataSO itemSO)
    {
        Debug.Log($"아이템 클릭: {itemSO.displayName}, inventoryId: {data.InventoryId}");
    }
}
