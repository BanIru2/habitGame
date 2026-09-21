using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
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
    private TextMeshProUGUI equipTypeText;

    [SerializeField]
    private Color normalColor = new Color32(202,202,202,255);
    private Color equippedColor = Color.green;

    private InventoryItemViewData viewData;

    private static readonly System.Text.StringBuilder sb = new System.Text.StringBuilder(64);

    // 외부 호출 - 아이템 슬롯 내부 동작 시작점
    // 어디서 클릭했냐에 따라 각각 다른 기능을 수행할 수 있도록 onClick함수를 받아 실행
    public void LoadData(InventoryItemViewData vData, Action<InventoryItemViewData> onClick)
    {
        this.viewData = vData;

        ApplyItemInfo();
        ApplyQuantity();
        ApplyEquipType();
        ApplyEquippedState();
        ApplyClickEvent(onClick);
    }

    // 아이템 정보 UI 적용
    private void ApplyItemInfo()
    {
        if (viewData.ItemSO == null)
        {
            itemNameText.text = viewData.Response.ItemId;
            itemDescribeText.text = "SO 매칭 실패";

            iconImage.enabled = false;
            backgroundImage.color = Color.red;
            return;
        }

        // 장비 아이템의 경우 레벨, 경험치 정보를 이름 옆에 한번에 세팅
        if (viewData.ItemSO is EquipmentDataSO equipSO)
        {
            ApplyEquipLevel(equipSO);
        }
        // 소비 아이템의 경우 SO에 이미 존재하는 문자열 포인터만 그대로 전달
        else
        {
            itemNameText.text = viewData.ItemSO.displayName;
        }

        itemDescribeText.text = viewData.ItemSO.description;
        iconImage.enabled = viewData.ItemSO.icon != null;
        iconImage.sprite = viewData.ItemSO.icon;
    }

    // 소모품의 경우 보유 개수 UI 적용
    private void ApplyQuantity()
    {
        // 소모품인지 확인
        bool showQuantity = viewData.ItemSO is ConsumableDataSO && viewData.Response.Quantity > 0;

        quantityBackground.SetActive(showQuantity);

        if (showQuantity)
        {
            quantityText.text = $"{viewData.Response.Quantity}";
        }
    }

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

    // 장비 아이템의 경우 장착 여부 UI 적용
    private void ApplyEquippedState()
    {
        if (viewData.ItemSO == null) return;

        backgroundImage.color = viewData.Response.IsEquipped ? equippedColor : normalColor;
    }

    // 장비 아이템 레벨/경험치 정보 적용
    private void ApplyEquipLevel(EquipmentDataSO equipSO)
    {
        int level = viewData.Response.Level;
        sb.Clear();
        sb.Append(equipSO.displayName);
        // 아이템 레벨이 최대레벨인 경우 별도 처리
        if (level == EquipmentDataSO.MaxLevel)
        {
            sb.Append(" MaxLevel");
        }
        else if(level > EquipmentDataSO.MaxLevel){
            Debug.LogError($"장비 아이템의 레벨이 최대값을 초과했습니다. : {equipSO.displayName}, Level. {level}");
        }
        else
        {
        int curExp = viewData.Response.Exp;
        int reqExp = equipSO.GetRequiredExp(level);
        sb.Append(" Lv.").Append(level).Append(" (").Append(curExp).Append("/").Append(reqExp).Append(")");
        }

        itemNameText.SetText(sb);
    }

    // 버튼 클릭 이벤트 연결
    private void ApplyClickEvent(Action<InventoryItemViewData> onClick)
    {
        button.onClick.RemoveAllListeners();

        if (onClick != null)
        {
            button.onClick.AddListener(() => onClick.Invoke(viewData));
        }
    }
}
