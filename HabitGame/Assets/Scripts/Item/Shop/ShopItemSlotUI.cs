using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemSlotUI : MonoBehaviour
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
    private TextMeshProUGUI equipTypeText;
    [SerializeField]
    private TextMeshProUGUI consumeTypeText;
    [SerializeField]
    private TextMeshProUGUI priceText;

    private Color normalColor = new Color32(202, 202, 202, 255);
    private Color unableColor = new Color32(118, 118, 118, 255);

    private ShopItemViewData viewData;

    public void LoadData(ShopItemViewData vData, Action<ShopItemViewData> onClick)
    {
        viewData = vData;

        ApplyItemInfo();
        ApplySlotColor();
        ApplyConsumeType();
        ApplyEquipType();
        ApplyClickEvent(onClick);
    }

    // 아이템 정보 UI 적용
    private void ApplyItemInfo()
    {
        itemNameText.text = viewData.ItemSO.displayName;
        itemDescribeText.text = viewData.ItemSO.description;
        priceText.text = viewData.ItemSO.cost.ToString() + " G";

        iconImage.enabled = viewData.ItemSO.icon != null;
        iconImage.sprite = viewData.ItemSO.icon;

        button.interactable = true;
    }

/*    public void ClearData()
    {
        viewData = null;

        itemNameText.text = "";
        itemDescribeText.text = "";
        priceText.text = "";

        equipTypeText.text = "";
        consumeTypeText.text = "";

        equipTypeText.gameObject.SetActive(false);
        consumeTypeText.gameObject.SetActive(false);

        iconImage.sprite = null;
        iconImage.enabled = false;

        button.onClick.RemoveAllListeners();
        button.interactable = false;
    }*/

    // 장비의 경우 장비 종류 UI 적용
    private void ApplyEquipType()
    {
        bool isEquip = viewData.ItemSO is EquipmentDataSO;

        equipTypeText.gameObject.SetActive(isEquip);

        if (isEquip)
        {
            EquipmentDataSO equipmentSO = viewData.ItemSO as EquipmentDataSO;
            switch (equipmentSO.equipmentType)
            {
                case EquipmentType.Clothes:
                    equipTypeText.text = "옷";
                    break;
                case EquipmentType.Shoes:
                    equipTypeText.text = "신발";
                    break;
                case EquipmentType.Hat:
                    equipTypeText.text = "모자";
                    break;
                case EquipmentType.Weapon:
                    equipTypeText.text = "무기";
                    break;
            }
        }
    }

    // 소모품의 경우 소비 종류(ItemEffectType) UI 적용
    private void ApplyConsumeType()
    {
        // 소모품인지 확인
        bool isConsume = viewData.ItemSO is ConsumableDataSO;

        consumeTypeText.gameObject.SetActive(isConsume);

        if (isConsume)
        {
            ConsumableDataSO consumableSO = viewData.ItemSO as ConsumableDataSO;
            switch (consumableSO.effectType)
            {
                case ItemEffectType.AttributeLevelUp:
                    consumeTypeText.text = "영구적 특성 레벨 상승";
                    break;
                case ItemEffectType.BattleStatUp:
                    consumeTypeText.text = "일회용 전투 스탯 상승";
                    break;
                case ItemEffectType.PermanentStatUp:
                    consumeTypeText.text = "영구적 스탯 상승";
                    break;
                case ItemEffectType.ProtectStreak:
                    consumeTypeText.text = "연속 달성 방어권";
                    break;
            }
        }
    }

    private void ApplySlotColor()
    {
        if (viewData.ItemResponse.PurchaseStatus != "AVAILABLE")
        {
            backgroundImage.color = unableColor;
        }
        else
        {
            backgroundImage.color = normalColor;
        }
    }

    // 버튼 클릭 이벤트 연결
    private void ApplyClickEvent(Action<ShopItemViewData> onClick)
    {
        button.onClick.RemoveAllListeners();

        if (onClick != null)
        {
            button.onClick.AddListener(() => onClick.Invoke(viewData));
        }
    }
}
