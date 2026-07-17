using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;


public class InventoryManager : Singleton<InventoryManager>
{
    [SerializeField]
    private InventoryBackendManager inventoryBackendManager;

    [SerializeField] 
    private ItemSlotUI itemSlotPrefab;
    [SerializeField] 
    private Transform itemSlotParent;

    private readonly List<InventoryItemViewData> allItems = new List<InventoryItemViewData>();
    private readonly List<InventoryItemViewData> equipmentItems = new List<InventoryItemViewData>();
    private readonly List<InventoryItemViewData> consumableItems = new List<InventoryItemViewData>();

    [SerializeField] 
    private List<ItemDataSO> testItems;


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

    // 아이템 교체가 진행중인지 체크 
    public bool IsEquipmentChangeInProgress { get; private set; }
    private bool isItemUseInProgress;
    public bool IsItemUseInProgress => isItemUseInProgress;

    [SerializeField] 
    private bool useLocalTestInventory = false;


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
    public async void OpenInventory()
    {
        /*        // 로컬 테스트용 응답 객체 생성
                List<InventoryItemResponse> responses = CreateTestInventoryResponses();*/
        try
        {
            await RefreshInventoryAsync();
            ShowEquipmentItems();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"인벤토리 조회 실패: {e.Message}");
        }
    }

    // DB기준 인벤토리 상태를 클라이언트 런타임에 동기화하는 함수
    // PvP 준비 단계에서의 장비 선택에 대해 반응하기 위해 public 함수로 분리
    public async Task RefreshInventoryAsync()
    {
        List<InventoryItemResponse> responses;

        if (useLocalTestInventory)
        {
            responses = CreateTestInventoryResponses();
        }
        else
        {
            responses = await inventoryBackendManager.FetchInventoryAsync();
        }

        BuildViewData(responses);
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
            IsEquipped = false,
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

    // 로컬 테스트를 위한 분기 생성용 함수
    private void SetLocalTestEquippedState(long inventoryId, bool shouldEquip)
    {
        InventoryItemViewData targetItem = null;

        foreach (InventoryItemViewData item in equipmentItems)
        {
            if (item.Response.InventoryId == inventoryId)
            {
                targetItem = item;
                break;
            }
        }

        if (targetItem == null)
        {
            Debug.LogWarning($"테스트 장착 변경 실패: inventoryId {inventoryId} 아이템을 찾을 수 없습니다.");
            return;
        }

        if (targetItem.ItemSO is not EquipmentDataSO targetEquipment)
        {
            Debug.LogWarning($"테스트 장착 변경 실패: inventoryId {inventoryId} 아이템은 장비가 아닙니다.");
            return;
        }

        // 장착하는 경우: 같은 부위 장비는 하나만 장착되도록 기존 장착 해제
        if (shouldEquip)
        {
            foreach (InventoryItemViewData item in equipmentItems)
            {
                if (item.ItemSO is not EquipmentDataSO equipmentSO) continue;

                if (equipmentSO.equipmentType == targetEquipment.equipmentType)
                {
                    item.Response.IsEquipped = false;
                }
            }
        }

        targetItem.Response.IsEquipped = shouldEquip;
    }

    private async Task SetEquipmentEquippedStateAsync(long id, bool shouldEquip)
    {
        if (IsEquipmentChangeInProgress)
        {
            Debug.LogWarning("장비 변경 요청이 이미 진행 중입니다.");
            return;
        }
        IsEquipmentChangeInProgress = true;

        try
        {
            // 서버 안 타고 로컬 테스트 데이터의 IsEquipped만 바꾸기
            if (useLocalTestInventory)
            {
                SetLocalTestEquippedState(id, shouldEquip);
                return;
            }

            if (shouldEquip)
            {
                await inventoryBackendManager.EquipItemAsync(id);
            }
            else
            {
                await inventoryBackendManager.UnequipItemAsync(id);
            }

            await RefreshInventoryAsync();
            await CharacterManager.Instance.RefreshCharacterAsync();
        }
        finally
        {
            IsEquipmentChangeInProgress = false;
        }

    }

    // 장비 아이템 장착/해제 요청
    // PvP 준비단계에서의 호출을 위해 public 함수로 분리
    public async Task EquipInventoryItemAsync(long inventoryId)
    {
        await SetEquipmentEquippedStateAsync(inventoryId, true);
    }

    public async Task UnequipInventoryItemAsync(long inventoryId)
    {
        await SetEquipmentEquippedStateAsync(inventoryId, false);
    }

    // 장착 처리
    private async void DoEquipItem()
    {
        if (selectedItem == null) return;
        if (IsEquipmentChangeInProgress) return;

        EquipmentDataSO selectedEquipment = selectedItem.ItemSO as EquipmentDataSO;
        if (selectedEquipment == null) return;

        doEquipButton.interactable = false;

        try
        {
            var id = selectedItem.Response.InventoryId;
            await EquipInventoryItemAsync(id);

            Debug.Log($"{selectedEquipment.displayName} 장착");


            ClosePopup();
            ShowEquipmentItems();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"장착 실패: {e.Message}");
        }
        finally
        {
            doEquipButton.interactable = true;
        }
    }

    // 장착 해제 처리
    private async void DoUnequipItem()
    {
        if (selectedItem == null) return;
        if (IsEquipmentChangeInProgress) return;

        EquipmentDataSO selectedEquipment = selectedItem.ItemSO as EquipmentDataSO;
        if (selectedEquipment == null) return;

        doEquipButton.interactable = false;

        try
        {
            var id = selectedItem.Response.InventoryId;
            await UnequipInventoryItemAsync(id);

            Debug.Log($"{selectedEquipment.displayName} 장착 해제");

            ClosePopup();
            ShowEquipmentItems();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"장착 해제 실패: {e.Message}");
        }
        finally
        {
            doEquipButton.interactable = true;
        }
    }

    // 사용 처리
    private async void UseFuncItem()
    {
        if (isItemUseInProgress) return;
        if (selectedItem == null) return;

        if (selectedItem.ItemSO is not ConsumableDataSO so) return;
        if (so.useTiming != ItemUseTiming.OutOfBattle) return;

        funcUseButton.interactable = false;

        try
        {
            long id = selectedItem.Response.InventoryId;

            await UseInventoryItemAsync(id);

            Debug.Log($"사용 완료: {so.displayName}");

            await CharacterManager.Instance.RefreshCharacterAsync();

            ClosePopup();
            ShowConsumableItems();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"아이템 사용 실패: {e.Message}");
        }
        finally
        {
            funcUseButton.interactable = true;
        }
    }


    // ---------------------------- PVP 준비 단계와 교환 -------------------------------------
    // PvP 준비단계에서 각 부위에 해당하는 보유 장비 리스트를 보내주기 위한 함수
    public List<InventoryItemViewData> GetEquipmentItems(EquipmentType equipmentType)
    {
        List<InventoryItemViewData> result = new List<InventoryItemViewData>();

        foreach (InventoryItemViewData item in equipmentItems)
        {
            if (item.ItemSO is EquipmentDataSO equipmentSO && equipmentSO.equipmentType == equipmentType)
            {
                result.Add(item);
            }
        }

        return result;
    }

    public List<InventoryItemViewData> GetBattlePreparationConsumableItems()
    {
        List<InventoryItemViewData> result = new List<InventoryItemViewData>();

        foreach (InventoryItemViewData item in consumableItems)
        {
            if (item.Response.Quantity <= 0) continue;

            if (item.ItemSO is ConsumableDataSO consumableSO && consumableSO.useTiming == ItemUseTiming.BattlePreparation)
            {
                result.Add(item);
            }
        }

        return result;
    }

    public async Task<UseItemResponse> UseInventoryItemAsync(long inventoryId)
    {
        if (isItemUseInProgress)
            throw new System.InvalidOperationException("Item use already in progress.");

        isItemUseInProgress = true;

        try
        {
            UseItemResponse response = await inventoryBackendManager.UseItemAsync(inventoryId);
            await RefreshInventoryAsync();
            return response;
        }
        finally
        {
            isItemUseInProgress = false;
        }
    }
}