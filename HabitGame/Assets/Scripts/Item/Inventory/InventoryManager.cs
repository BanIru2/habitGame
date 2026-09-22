using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Unity.VisualScripting.Dependencies.Sqlite;
using System.Text;
using Photon.Pun.Demo.Cockpit;


public class InventoryManager : Singleton<InventoryManager>
{
    [SerializeField]
    private InventoryBackendManager inventoryBackendManager;

    [SerializeField] 
    private ItemSlotUI itemSlotPrefab;
    [SerializeField] 
    private Transform itemSlotParent;
    [SerializeField]
    private RectTransform scrollView;

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
    private Button enhanceButton;
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

    [Header("장비 강화 UI")]
    [SerializeField]
    private GameObject enhanceTap;
    [SerializeField]
    private TextMeshProUGUI enhanceTargetItemNameText;
    [SerializeField]
    private Image enhanceTargetItemIcon;
    [SerializeField]
    private TextMeshProUGUI enhanceTargetItemLevelText;
    [SerializeField]
    private TextMeshProUGUI enhanceTargetItemExpText;
    [SerializeField]
    private Image enhanceTargetItemExpBarFill;
    [SerializeField]
    private GameObject enhanceButtonBar;
    [SerializeField]
    private Button doEnhanceButton;
    [SerializeField]
    private Button enhanceCancelButton;

    // 테스트 데이터 사용 여부 체크
    [SerializeField]
    private bool useLocalTestInventory = false;

    // 현재 보고있는 아이템 데이터 저장
    private InventoryItemViewData selectedItem;

    // 아이템 정보를 출력할 슬롯 pool
    private readonly List<ItemSlotUI> slotPool = new List<ItemSlotUI>();

    // 아이템 교체가 진행중인지 체크 
    public bool IsEquipmentChangeInProgress { get; private set; }
    private bool isItemUseInProgress;
    public bool IsItemUseInProgress => isItemUseInProgress;


    // 강화 진행중인지 저장하는 플래그
    private bool isEnhanceMode = false;
    // 아이템 강화 창 띄울 때 스크롤 뷰 사이즈 변경을 위한 스크롤뷰 높이 저장
    private const int normalScrollTop = 110;
    private const int normalScrollBottom = 180;
    private const int enhanceScrollTop = 610;
    private const int enhanceScrollBottom = 430;
    // 강화 대상 아이템 데이터
    private InventoryItemViewData enhanceTargetItemData;
    // 강화 대상을 제외한 장비 리스트
    private readonly List<InventoryItemViewData> enhanceMaterialItems = new List<InventoryItemViewData>();
    // 강화 재료로 선택된 아이템Id 리스트
    private List<long> selectedMaterialIds = new List<long>();

    private readonly StringBuilder sb = new StringBuilder(64);


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

        enhanceButton.onClick.AddListener(OpenEnhance);
        doEnhanceButton.onClick.AddListener(DoEnhance);
        enhanceCancelButton.onClick.AddListener(CloseEnhance);

        ClosePopup();
    }

    // InventoryTap이 켜질때 마다 아이템 목록 다시 그리기
    // 인벤토리 탭 여는 버튼 클릭 시 호출하도록
    public async Task OpenInventory()
    {
        /*        // 로컬 테스트용 응답 객체 생성
                List<InventoryItemResponse> responses = CreateTestInventoryResponses();*/
        if (isEnhanceMode) CloseEnhance();

        await RefreshInventoryAsync();
        ShowEquipmentItems();
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
            Quantity = itemSO is ConsumableDataSO ? index + 1 : 1,
            IsEquipped = false
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
        if (isEnhanceMode)
        {
            OnMaterialSlotClicked(viewData);
            return;
        }

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

        List<InventoryItemResponse> updatedInventory;

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
                updatedInventory = await inventoryBackendManager.EquipItemAsync(id);
            }
            else
            {
                updatedInventory = await inventoryBackendManager.UnequipItemAsync(id);
            }

            BuildViewData(updatedInventory);
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
        catch(ApiException e)
        {
            Debug.LogError($"장착 실패 : {e.Message}");
            ErrorPopupManager.Instance.ShowApiError(e);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"장착 처리 중 시스템 오류: {e.Message}");
            ErrorPopupManager.Instance.ShowSystemError();
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
        catch (ApiException e)
        {
            Debug.LogError($"장착 해제 실패 : {e.Message}");
            ErrorPopupManager.Instance.ShowApiError(e);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"장착 해제 처리 중 시스템 오류: {e.Message}");
            ErrorPopupManager.Instance.ShowSystemError();
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

        if (selectedItem.Response == null || selectedItem.Response.Quantity <= 0)
        {
            Debug.LogWarning("아이템 수량 부족");
            return;
        }

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
        catch (ApiException e)
        {
            Debug.LogError($"아이템 사용 실패 : {e.Message}");
            ErrorPopupManager.Instance.ShowApiError(e);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"아이템 사용 처리 중 시스템 오류: {e.Message}");
            ErrorPopupManager.Instance.ShowSystemError();
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

    public async Task<List<InventoryItemResponse>> UseInventoryItemAsync(long inventoryId)
    {
        if (isItemUseInProgress)
            throw new System.InvalidOperationException("Item use already in progress.");

        isItemUseInProgress = true;

        try
        {
            List<InventoryItemResponse> updatedInventory = await inventoryBackendManager.UseItemAsync(inventoryId);
            BuildViewData(updatedInventory);
            return updatedInventory;
        }
        finally
        {
            isItemUseInProgress = false;
        }
    }

    // --------------------------------- 장비 강화 --------------------------------
    private void OpenEnhance()
    {
        isEnhanceMode = true;
        selectedMaterialIds.Clear();

        enhanceTargetItemData = selectedItem;

        equipDetail.SetActive(false);
        SetScrollArea(enhanceScrollTop, enhanceScrollBottom);
        enhanceTap.SetActive(true);
        enhanceButtonBar.SetActive(true);
        SetEnhanceTargetData();
        ShowEnhanceMaterials();
    }

    private void CloseEnhance()
    {
        isEnhanceMode = false;
        selectedMaterialIds.Clear();
        enhanceTargetItemData = null;

        enhanceTap.SetActive(false);
        enhanceButtonBar.SetActive(false);
        SetScrollArea(normalScrollTop, normalScrollBottom);
        ShowEquipmentItems();
    }

    private void SetScrollArea(float top, float bottom)
    {
        if (scrollView == null) return;
        // Top 설정: offsetMax.y에 -top을 대입 (Right인 offsetMax.x는 유지)
        scrollView.offsetMax = new Vector2(scrollView.offsetMax.x, -top);
        // Bottom 설정: offsetMin.y에 bottom을 그대로 대입 (Left인 offsetMin.x는 유지)
        scrollView.offsetMin = new Vector2(scrollView.offsetMin.x, bottom);
    }

    // 강화 대상 아이템 데이터 UI세팅
    private void SetEnhanceTargetData()
    {
        if (enhanceTargetItemData == null || enhanceTargetItemData.Response == null) return;

        int level = enhanceTargetItemData.Response.Level;
        int curExp = enhanceTargetItemData.Response.Exp;
        if (enhanceTargetItemData.ItemSO is EquipmentDataSO equipSO)
        {
            int reqExp = equipSO.GetRequiredExp(level);
            enhanceTargetItemNameText.text = enhanceTargetItemData.ItemSO.displayName;
            enhanceTargetItemIcon.sprite = enhanceTargetItemData.ItemSO.icon;
            if (enhanceTargetItemData.Response.Level == EquipmentDataSO.MaxLevel)
            {
                enhanceTargetItemLevelText.text = "MaxLevel";
                enhanceTargetItemExpText.text = "MAX";
                ApplyExpFill(1.0f);
            }
            else
            {
                sb.Clear();
                sb.Append("Lv. ").Append(level);
                enhanceTargetItemLevelText.SetText(sb);

                sb.Clear();
                sb.Append(curExp).Append("/").Append(reqExp);
                enhanceTargetItemExpText.SetText(sb);

                float ratio = (reqExp > 0) ? (float)curExp / reqExp : 0f;
                ApplyExpFill(ratio);
            }
        }
    }

    private void ApplyExpFill(float ratio)
    {
        if (enhanceTargetItemExpBarFill == null) return;
        Vector3 scale = enhanceTargetItemExpBarFill.rectTransform.localScale;
        scale.x = Mathf.Clamp01(ratio);
        enhanceTargetItemExpBarFill.rectTransform.localScale = scale;
    }

    private void ShowEnhanceMaterials()
    {
        enhanceMaterialItems.Clear();
        foreach (var item in equipmentItems)
        {
            // 강화 대상 장비 본인은 재료 목록에서 제외
            if (item.Response.InventoryId == enhanceTargetItemData.Response.InventoryId)
                continue;
            // 현재 캐릭터가 장착 중인(isEquipped) 장비 제외
            if (item.Response.IsEquipped)
                continue;
            enhanceMaterialItems.Add(item);
        }
        // 필터링된 재료 장비들만 하단 스크롤뷰에 렌더링
        RenderItems(enhanceMaterialItems);
    }

    private void OnMaterialSlotClicked(InventoryItemViewData viewData)
    {
        if (viewData == null || viewData.Response == null) return;

        long invenId = viewData.Response.InventoryId;
        // 이미 선택된 재료면 선택 해제
        if (selectedMaterialIds.Contains(invenId))
        {
            selectedMaterialIds.Remove(invenId);
        }
        // 아직 선택 안 된 재료면 선택 추가
        else
        {
            selectedMaterialIds.Add(invenId);
        }

        // 슬롯 선택에 따른 하이라이트 갱신
        RefreshMaterialSlotsHighlight();
        // 상단 게이지 프리뷰 갱신
        UpdateEnhancePreview();
    }

    // 선택한 슬롯에 색상 하이라이트 추가
    private void RefreshMaterialSlotsHighlight()
    {
        for (int i = 0; i < enhanceMaterialItems.Count; i++)
        {
            ItemSlotUI slot = GetSlot(i);
            long slotInvId = enhanceMaterialItems[i].Response.InventoryId;
            slot.SetSelected(selectedMaterialIds.Contains(slotInvId));
        }
    }

    // 선택된 재료들의 경험치를 합산하여 상단 게이지를 통해 상승 레벨 미리보기 갱신
    private void UpdateEnhancePreview()
    {
        if (enhanceTargetItemData == null || !(enhanceTargetItemData.ItemSO is EquipmentDataSO targetSO)) return;

        // 선택된 재료가 0개면 원래 상태로 복구
        if (selectedMaterialIds.Count == 0)
        {
            SetEnhanceTargetData();
            // 강화 실행 버튼 비활성화
            if (doEnhanceButton != null) doEnhanceButton.interactable = false;
            return;
        }
        // 선택된 모든 재료 장비의 재료 경험치 총합 계산
        int totalAddedExp = 0;
        foreach (long matId in selectedMaterialIds)
        {
            InventoryItemViewData matItem = null;
            foreach (var item in equipmentItems)
            {
                if (item.Response.InventoryId == matId)
                {
                    matItem = item;
                    break;
                }
            }

            if (matItem != null && matItem.ItemSO is EquipmentDataSO matSO)
            {
                totalAddedExp += matSO.ProvideExp;
            }
        }
        // 레벨업 시뮬레이션 계산
        int previewLevel = enhanceTargetItemData.Response.Level;
        int previewExp = enhanceTargetItemData.Response.Exp + totalAddedExp;
        while (previewLevel < EquipmentDataSO.MaxLevel)
        {
            int reqExp = targetSO.GetRequiredExp(previewLevel);
            if (previewExp >= reqExp)
            {
                previewExp -= reqExp;
                previewLevel++;
            }
            else
            {
                break;
            }
        }
        // 미리보기 UI 반영
        if (previewLevel >= EquipmentDataSO.MaxLevel)
        {
            enhanceTargetItemLevelText.text = "MaxLevel";
            enhanceTargetItemExpText.text = "MAX";
            ApplyExpFill(1.0f);
        }
        else
        {
            int reqExp = targetSO.GetRequiredExp(previewLevel);
            sb.Clear();
            sb.Append("Lv. ").Append(previewLevel);
            enhanceTargetItemLevelText.SetText(sb);

            sb.Clear();
            sb.Append(previewExp).Append(" / ").Append(reqExp);
            enhanceTargetItemExpText.SetText(sb);

            float ratio = (reqExp > 0) ? (float)previewExp / reqExp : 0f;
            ApplyExpFill(ratio);
        }
        // 강화 버튼 활성화
        if (doEnhanceButton != null)
        {
            // 대상이 이미 만렙이 아니고 재료가 선택되었을 때만 활성화
            doEnhanceButton.interactable = (enhanceTargetItemData.Response.Level < EquipmentDataSO.MaxLevel);
        }
    }

    // 강화 버튼을 통한 강화 동작
    private async void DoEnhance()
    {
        if (enhanceTargetItemData == null || selectedMaterialIds.Count == 0) return;

        if (doEnhanceButton != null) doEnhanceButton.interactable = false;

        long targetId = enhanceTargetItemData.Response.InventoryId;
        try
        {
            // 서버에 강화 요청 (대상 ID와 재료 ID 리스트 전달)
            List<InventoryItemResponse> enhancedResponses = await inventoryBackendManager.EnhanceItemAsync(targetId, selectedMaterialIds);
            if (enhancedResponses != null)
            {
                // 인벤토리 전체 최신 데이터로 동기화
                BuildViewData(enhancedResponses);

                // 대상 장비가 착용 중이었으면 캐릭터 최종 스탯도 즉시 재계산
                if (enhanceTargetItemData.Response.IsEquipped)
                {
                    await CharacterManager.Instance.RefreshCharacterAsync();
                }

                // 대상 장비의 최신 정보로 갱신
                foreach (var item in equipmentItems)
                {
                    if (item.Response.InventoryId == targetId)
                    {
                        enhanceTargetItemData = item;
                        break;
                    }
                }

                // 선택 목록 비우고 UI 갱신
                selectedMaterialIds.Clear();
                SetEnhanceTargetData();
                ShowEnhanceMaterials();
            }
        }
        catch (ApiException e)
        {
            Debug.LogError($"강화 통신 오류: {e.Message}");
            ErrorPopupManager.Instance.ShowApiError(e);
        }
        catch (Exception e)
        {
            Debug.LogError($"강화 시스템 오류: {e.Message}");
            ErrorPopupManager.Instance.ShowSystemError();
        }
        finally
        {
            // 강화 성공 시 버튼 비활성화, 에러 발생으로 실패 시 재시도 가능하도록 활성화
            if (doEnhanceButton != null)
            {
                doEnhanceButton.interactable = (selectedMaterialIds.Count > 0);
            }
        }
    }
}
