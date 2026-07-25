using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUIManager : Singleton<ShopUIManager>
{
    [SerializeField]
    private TextMeshProUGUI goldText;
    [SerializeField]
    private Button equipmentButton;
    [SerializeField]
    private Button consumableButton;

    [SerializeField]
    private ShopItemSlotUI itemSlotPrefab;
    [SerializeField]
    private Transform itemSlotParent;

    private readonly List<ShopItemSlotUI> slotPool = new List<ShopItemSlotUI>();    // 아이템 정보를 출력할 슬롯 pool

    [SerializeField]
    private ShopConfigSO shopConfigSO;

    private readonly List<ShopItemViewData> allItems = new List<ShopItemViewData>();
    private readonly List<ShopItemViewData> equipmentItems = new List<ShopItemViewData>();
    private readonly List<ShopItemViewData> consumableItems = new List<ShopItemViewData>();

    // 아이템 데이터 ItemSlot으로 화면에 생성
    private void RenderItems(List<ShopItemViewData> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            ShopItemViewData item = items[i];

            ShopItemSlotUI slot = GetSlot(i);
            slot.gameObject.SetActive(true);

            slot.LoadData(item, OnItemSlotClicked);
        }

        HideUnusedSlots(items.Count);
    }

    // 슬롯이 남아 있다면 재사용, 없다면 생성
    private ShopItemSlotUI GetSlot(int index)
    {
        if (index < slotPool.Count)
        {
            return slotPool[index];
        }

        ShopItemSlotUI slot = Instantiate(itemSlotPrefab, itemSlotParent);
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

    private void OnItemSlotClicked(ShopItemViewData item)
    {

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
}
