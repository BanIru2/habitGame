using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    [SerializeField]
    private Button button;
    [SerializeField]
    private Image backgroundImage;

    [SerializeField]
    private Image iconImage;
    [SerializeField]
    private TextMeshProUGUI itemNameText;
    [SerializeField]
    private TextMeshProUGUI itemDescribeText;
    [SerializeField]
    private TextMeshProUGUI quantityText;
    [SerializeField]
    private GameObject quantityBackground;

    [SerializeField]
    private Color normalColor = new Color32(202,202,202,255);
    private Color equippedColor = Color.green;

    private InventoryItemResponse data;
    private ItemDataSO itemSO;

    // 외부 호출 - 아이템 슬롯 내부 동작 시작점
    // 어디서 클릭했냐에 따라 각각 다른 기능을 수행할 수 있도록 onClick함수를 받아 실행
    public void LoadData(InventoryItemResponse response, ItemDataSO so, Action<InventoryItemResponse, ItemDataSO> onClick)
    {
        this.data = response;
        this.itemSO = so;

        ApplyItemInfo();
        ApplyQuantity();
        ApplyEquippedState();
        ApplyClickEvent(onClick);
    }

    // 아이템 정보 UI 적용
    private void ApplyItemInfo()
    {
        if (itemSO == null)
        {
            itemNameText.text = data.ItemId;
            itemDescribeText.text = "SO 매칭 실패";

            iconImage.enabled = false;
            backgroundImage.color = Color.red;
            return;
        }

        itemNameText.text = itemSO.displayName;
        itemDescribeText.text = itemSO.description;

        iconImage.enabled = itemSO.icon != null;
        iconImage.sprite = itemSO.icon;
    }

    // 소모품의 경우 보유 개수 UI 적용
    private void ApplyQuantity()
    {
        // 소모품인지 확인
        bool showQuantity = itemSO is ConsumableDataSO && data.Quantity > 0;

        quantityBackground.SetActive(showQuantity);

        if (showQuantity)
        {
            quantityText.text = $"{data.Quantity}";
        }
    }

    // 장비 아이템의 경우 장착 여부 UI 적용
    private void ApplyEquippedState()
    {
        if (itemSO == null) return;

        backgroundImage.color = data.IsEquipped ? equippedColor : normalColor;
    }

    // 버튼 클릭 이벤트 연결
    private void ApplyClickEvent(Action<InventoryItemResponse, ItemDataSO> onClick)
    {
        button.onClick.RemoveAllListeners();

        if (onClick != null)
        {
            button.onClick.AddListener(() => onClick.Invoke(data, itemSO));
        }
    }
}
