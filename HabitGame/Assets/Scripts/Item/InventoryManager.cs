using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : Singleton<InventoryManager>
{
    [SerializeField] private ItemSlotUI itemSlotPrefab;
    [SerializeField] private Transform itemSlotParent;

    private readonly List<InventoryItemViewData> allItems = new List<InventoryItemViewData>();
    private readonly List<InventoryItemViewData> equipmentItems = new List<InventoryItemViewData>();
    private readonly List<InventoryItemViewData> consumableItems = new List<InventoryItemViewData>();

    [SerializeField] private List<ItemDataSO> testItems;


    [SerializeField]
    private Button equipButton;
    [SerializeField]
    private Button consumableButton;

    [SerializeField]
    private GameObject popupDim;

    [Header ("장비 상세 정보 팝업")]
    [SerializeField]
    private GameObject equipDetail;
    [SerializeField]
    private Image equipIcon;
    [SerializeField]
    private TextMeshProUGUI equipNameText;
    [SerializeField]
    private TextMeshProUGUI equipDescText;
    [SerializeField]
    private Button doEquipButton;
    [SerializeField]
    private Button equipCloseButton;
    [SerializeField]
    private TextMeshProUGUI doEquipButtonText;

    [Header("소비 상세 정보 팝업")]
    [SerializeField]
    private GameObject consumDetail;
    [SerializeField]
    private Image consumIcon;
    [SerializeField]
    private TextMeshProUGUI consumNameText;
    [SerializeField]
    private TextMeshProUGUI consumDescText;
    [SerializeField]
    private Button consumCloseButton;

    [Header("기능 소비 상세 정보 팝업")]
    [SerializeField]
    private GameObject funcDetail;
    [SerializeField]
    private Image funcIcon;
    [SerializeField]
    private TextMeshProUGUI funcNameText;
    [SerializeField]
    private TextMeshProUGUI funcDescText;
    [SerializeField]
    private Button funcUseButton;
    [SerializeField]
    private Button funcCloseButton;

    // 현재 보고있는 아이템 데이터 저장
    private InventoryItemViewData selectedItem;

    private readonly List<ItemSlotUI> slotPool = new List<ItemSlotUI>();    // 아이템 정보를 출력할 슬롯 pool

    protected override void Awake()
    {
        base.Awake();

        equipButton.onClick.AddListener(ShowEquipmentItems);
        consumableButton.onClick.AddListener(ShowConsumableItems);

        equipCloseButton.onClick.AddListener(ClosePopup);
        consumCloseButton.onClick.AddListener(ClosePopup);
        funcCloseButton.onClick.AddListener(ClosePopup);

        doEquipButton.onClick.AddListener(OnEquipActionButtonClicked);
        funcUseButton.onClick.AddListener(UseFuncItem);

        ClosePopup();
    }

    // InventoryTap이 켜질때 마다 아이템 목록 다시 그리기
    // 인벤토리 탭 여는 버튼 클릭 시 호출하도록
    public void OpenInventory()
    {
        // 실제 DB 연결 함수로 변경 필요
        List<InventoryItemResponse> responses = CreateTestInventoryResponses();

        BuildViewData(responses);
        ShowEquipmentItems();
    }

    // --------------------------------- 테스트 데이터 생성 -----------------------------------------

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

            slot.LoadData(item, OnItemSlotClicked);
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
    // 아이템 상세 정보 창 띄우기
    private void OnItemSlotClicked(InventoryItemViewData viewData)
    {
        ClosePopup();

        var itemSO = viewData.ItemSO;
        var data = viewData.Response;

        if(itemSO is EquipmentDataSO equipSO)
        {
            OpenEquipDetail(data, equipSO);
            popupDim.SetActive(true);
        }
        else if(itemSO is ConsumableDataSO consumSO)
        {
            if (consumSO.useTiming == ItemUseTiming.BattlePreparation)
            {
                OpenConsumDetail(data, consumSO);
                popupDim.SetActive(true);
            }
            else if (consumSO.useTiming == ItemUseTiming.OutOfBattle)
            {
                OpenFuncDetail(data, consumSO);
                popupDim.SetActive(true);
            }
        }

        selectedItem = viewData;
    }

    // ---------------------------------- detail popup ---------------------------------
    // 상세 팝업 모두 비활성화
    private void ClosePopup()
    {
        popupDim.SetActive(false);
        equipDetail.SetActive(false);
        consumDetail.SetActive(false);
        funcDetail.SetActive(false);
        selectedItem = null;
    }

    private void OpenEquipDetail(InventoryItemResponse data, EquipmentDataSO itemSO)
    {
        equipDetail.SetActive(true);
        equipIcon.sprite = itemSO.icon;
        equipNameText.text = itemSO.displayName;
        equipDescText.text = itemSO.description;

        doEquipButtonText.text = data.IsEquipped ? "해제하기" : "장착하기";
        doEquipButton.image.color = data.IsEquipped ? Color.red : new Color32(50,184,255, 255);
    }

    private void OpenConsumDetail(InventoryItemResponse data, ConsumableDataSO itemSO)
    {
        consumDetail.SetActive(true);
        consumIcon.sprite = itemSO.icon;
        consumNameText.text = itemSO.displayName;
        consumDescText.text = itemSO.description;
    }

    private void OpenFuncDetail(InventoryItemResponse data, ConsumableDataSO itemSO)
    {
        funcDetail.SetActive(true);
        funcIcon.sprite = itemSO.icon;
        funcNameText.text = itemSO.displayName;
        funcDescText.text = itemSO.description;
    }

    // 장착 버튼 클릭 시 동작
    private void OnEquipActionButtonClicked()
    {
        if (selectedItem == null) return;

        if (selectedItem.Response.IsEquipped)
        {
            DoUnequipItem();
        }
        else
        {
            DoEquipItem();
        }
    }

    // 장착 처리
    private void DoEquipItem()
    {
        if (selectedItem == null) return;

        EquipmentDataSO selectedEquipment = selectedItem.ItemSO as EquipmentDataSO;
        if (selectedEquipment == null) return;

        // 추후 DB 장착 요청 성공 후 실행

        UnequipSameTypeItemsLocally(selectedEquipment);

        selectedItem.Response.IsEquipped = true;

        Debug.Log($"장착 : {selectedEquipment.displayName}");

        ClosePopup();
        ShowEquipmentItems();
    }

    private void UnequipSameTypeItemsLocally(EquipmentDataSO selectedEquipment)
    {
        foreach (InventoryItemViewData item in equipmentItems)
        {
            if (item.ItemSO is EquipmentDataSO equipmentSO &&
                equipmentSO.equipmentType == selectedEquipment.equipmentType)
            {
                item.Response.IsEquipped = false;
            }
        }
    }

    // 장착 해제 처리
    private void DoUnequipItem()
    {
        if (selectedItem == null) return;

        EquipmentDataSO selectedEquipment = selectedItem.ItemSO as EquipmentDataSO;
        if (selectedEquipment == null) return;

        // 추후 DB 장착 해제 요청 성공 후 실행
        selectedItem.Response.IsEquipped = false;

        Debug.Log($"장착 해제 : {selectedEquipment.displayName}");

        ClosePopup();
        ShowEquipmentItems();
    }

    // 사용 처리
    private void UseFuncItem()
    {
        if (selectedItem == null) return;

        if (selectedItem.ItemSO is ConsumableDataSO so)
        {
            if (so.useTiming == ItemUseTiming.OutOfBattle)
                Debug.Log($"사용 : {so.displayName}");
        }
    }
}
