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

    // 재료로 선택 시 하이라이트 주기 위한 주황색
    private readonly Color selectedColor = new Color32(255, 180, 50, 255);

    // 외부 호출 - 아이템 슬롯 내부 동작 시작점
    // 어디서 클릭했냐에 따라 각각 다른 기능을 수행할 수 있도록 onClick함수를 받아 실행
    public void LoadData(InventoryItemViewData vData, Action<InventoryItemViewData> onClick)
    {
        this.viewData = vData;

        ResetUI();

        // SO가 없거나 데이터가 비정상인 경우 예외 처리
        if (viewData == null || viewData.ItemSO == null)
        {
            ApplyFallbackUI();
            ApplyClickEvent(onClick);
            return;
        }

        // 공통 정보 적용
        ApplyCommonInfo();

        // 아이템 타입별 정보 적용
        if (viewData.ItemSO is EquipmentDataSO equipSO)
        {
            ApplyEquipmentInfo(equipSO);
        }
        else if (viewData.ItemSO is ConsumableDataSO consumableSO)
        {
            ApplyConsumableInfo(consumableSO);
        }

        ApplyClickEvent(onClick);
    }

    // UI 값 일괄 초기화
    private void ResetUI()
    {
        quantityBackground.SetActive(false);
        equipTypeText.gameObject.SetActive(false);
        iconImage.enabled = false;
        backgroundImage.color = normalColor;
    }

    // 아이템 공통 정보 UI 적용
    private void ApplyCommonInfo()
    {
        itemDescribeText.text = viewData.ItemSO.description;

        bool hasIcon = viewData.ItemSO.icon != null;
        iconImage.enabled = hasIcon;
        if (hasIcon)
        {
            iconImage.sprite = viewData.ItemSO.icon;
        }
    }

    // SO 누락 등 비정상 데이터 방어용 UI 처리
    private void ApplyFallbackUI()
    {
        // viewData, viewData.Response에 대한 null 여부 검사 및 예외 처리(Unknown)
        itemNameText.text = viewData?.Response?.ItemId ?? "Unknown";
        itemDescribeText.text = "SO 매칭 실패";
        iconImage.enabled = false;
        backgroundImage.color = Color.red;
    }

    // 소모품 UI 적용
    private void ApplyConsumableInfo(ConsumableDataSO consumableSO)
    {
        // 이름 표기
        itemNameText.text = consumableSO.displayName;
        // 수량 표기
        // viewData.Response에 대한 null검사 및 예외처리(0)
        int quantity = viewData.Response?.Quantity ?? 0;
        if (quantity > 0)
        {
            quantityBackground.SetActive(true);
            quantityText.text = quantity.ToString();
        }

        backgroundImage.color = normalColor;
    }

    // 장비명 한글 변환 헬퍼
    private string GetEquipmentTypeName(EquipmentType type) => type switch
    {
        EquipmentType.Clothes => "옷",
        EquipmentType.Shoes => "신발",
        EquipmentType.Hat => "모자",
        EquipmentType.Weapon => "무기",
        _ => string.Empty
    };

    // 장비 UI 적용
    private void ApplyEquipmentInfo(EquipmentDataSO equipSO)
    {
        // 이름, 레벨, 경험치 텍스트 적용
        ApplyEquipLevel(equipSO);

        // 부위 표시
        equipTypeText.gameObject.SetActive(true);
        equipTypeText.text = GetEquipmentTypeName(equipSO.equipmentType);

        // 착용 여부에 따른 배경색 적용
        backgroundImage.color = GetDefaultBackgroundColor();
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

    private Color GetDefaultBackgroundColor()
    {
        // viewData, viewData.Response의 null 여부 검사 및 예외 처리(false)
        bool isEquipped = viewData?.Response?.IsEquipped ?? false;
        return isEquipped ? equippedColor : normalColor;
    }

    // 재료로 선택되면 주황색, 선택 해제되면 원래 회색 배경으로 복구
    public void SetSelected(bool isSelected)
    {
        // 선택 시 주황색, 선택 해제 시 본래 색상(착용 여부 반영)으로 안전하게 복원
        backgroundImage.color = isSelected ? selectedColor : GetDefaultBackgroundColor();
    }

}
