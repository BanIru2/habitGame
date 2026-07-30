using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class ShopUIManager : Singleton<ShopUIManager>
{
    [SerializeField]
    private TextMeshProUGUI goldText;
    [SerializeField]
    private Button equipmentPanelButton;
    [SerializeField]
    private Button consumablePanelButton;

    [SerializeField]
    private ShopItemSlotUI itemSlotPrefab;
    [SerializeField]
    private Transform itemSlotParent;

    private readonly List<ShopItemSlotUI> slotPool = new List<ShopItemSlotUI>();    // 아이템 정보를 출력할 슬롯 pool

    [SerializeField]
    private ShopConfigSO shopConfigSO;
    [SerializeField]
    private ShopBackendManager shopBackendManager;

    private List<ItemResponse> responses = new List<ItemResponse>();

    private readonly List<ShopItemViewData> equipmentItems = new List<ShopItemViewData>();
    private readonly List<ShopItemViewData> consumableItems = new List<ShopItemViewData>();

    [Header("상세 팝업")]
    [SerializeField]
    private GameObject itemDetailPopup;
    [SerializeField]
    private Image itemDetailIcon;
    [SerializeField]
    private TextMeshProUGUI itemDetailNameText;
    [SerializeField]
    private TextMeshProUGUI itemDetailDescText;
    [SerializeField]
    private TextMeshProUGUI itemDetailPriceText;
    [SerializeField]
    private TextMeshProUGUI itemDetailPurchaseStatusText;
    [SerializeField]
    private Button buyButton;
    [SerializeField]
    private TextMeshProUGUI buyButtonText;
    [SerializeField]
    private Button closeButton;

    private ShopItemViewData selectedItem;
    private bool isPurchasing;

    [Header("구매 실패 팝업")]
    [SerializeField]
    private GameObject purchaseFailPopup;
    [SerializeField]
    private TextMeshProUGUI purchaseFailPurchaseStatusText;
    [SerializeField]
    private Button purchasePopupCloseButton;

    protected override void Awake()
    {
        base.Awake();

        equipmentPanelButton.onClick.AddListener(ShowEquipmentItems);
        consumablePanelButton.onClick.AddListener(ShowConsumableItems);

        buyButton.onClick.AddListener(BuyItem);
        closeButton.onClick.AddListener(ClosePopup);

        purchasePopupCloseButton.onClick.AddListener(ClosePurchaseFailPopup);
    }

    public async void OpenShop()
    {
        await RefreshShopAsync();
        ShowEquipmentItems();
    }

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

    private async Task RefreshShopAsync()
    {
        responses = await shopBackendManager.FetchShopItemsAsync();

        equipmentItems.Clear();
        consumableItems.Clear();

        equipmentItems.AddRange(CreateViewDataList(shopConfigSO.equipmentItems));

        consumableItems.AddRange(CreateViewDataList(shopConfigSO.consumableItems));
    }

    // ItemDataSO -> ShopItemViewData로 변환해주기 위한 공통 함수
    private List<ShopItemViewData> CreateViewDataList<T>(List<T> items)
    where T : ItemDataSO
    {
        List<ShopItemViewData> viewDataList = new List<ShopItemViewData>();

        if (items == null)
            return viewDataList;

        foreach (T item in items)
        {
            if (item == null)
                continue;

            ItemResponse response = responses.Find(r => r.Id == item.itemId);
            if (response == null)
            {
                Debug.LogWarning($"상점 응답에 아이템이 없습니다: {item.itemId}");
                continue;
            }

            viewDataList.Add(new ShopItemViewData
            {
                ItemSO = item,
                ItemResponse = response
            });
        }

        return viewDataList;
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

    // 슬롯 온클릭 함수 - 상세 팝업 출력
    private void OnItemSlotClicked(ShopItemViewData viewData)
    {
        ClosePopup();

        if (viewData == null || viewData.ItemSO == null || viewData.ItemResponse == null)
            return;

        OpenItemDetail(viewData);
        selectedItem = viewData;
    }
    // ------------------------------- 상세 팝업 -----------------------------------------
    private void ClosePopup()
    {
        itemDetailPopup.SetActive(false);
        buyButton.interactable = true;
        buyButton.image.color = Color.white;
        selectedItem = null;
    }

    private void OpenItemDetail(ShopItemViewData viewData)
    {
        itemDetailPopup.SetActive(true);
        itemDetailPurchaseStatusText.gameObject.SetActive(false);
        itemDetailIcon.sprite = viewData.ItemSO.icon;
        itemDetailNameText.text = viewData.ItemSO.displayName;
        itemDetailDescText.text = viewData.ItemSO.description;
        itemDetailPriceText.text = viewData.ItemSO.cost.ToString() + " G";

        bool isAvailable = viewData.ItemResponse.PurchaseStatus == "AVAILABLE" ? true : false;
        buyButtonText.text = isAvailable ? "구매하기" : "구매불가";

        if (!isAvailable)
        {
            itemDetailPurchaseStatusText.gameObject.SetActive(true);
            itemDetailPurchaseStatusText.text = GetPurchaseStatusMessage(viewData.ItemResponse.PurchaseStatus);
            buyButton.interactable = false;
            buyButton.image.color = new Color32 (242, 242, 242, 255);
        }
    }

    private async void BuyItem()
    {
        if (isPurchasing)
            return;

        if (selectedItem == null || selectedItem.ItemResponse == null)
            return;

        isPurchasing = true;
        buyButton.interactable = false;
        closeButton.interactable = false;

        try
        {
            PurchaseItemResponse response;

            // 1. 실제 구매 요청만 별도로 처리
            try
            {
                response = await shopBackendManager.PurchaseItemAsync(
                    selectedItem.ItemResponse.Id
                );
            }
            catch (System.Exception e)
            {
                Debug.LogError($"구매 요청 실패: {e}");
                ShowPurchaseError("구매 요청 중 오류가 발생했습니다.");
                return;
            }

            // 2. 응답 자체가 없는 경우
            if (response == null)
            {
                ShowPurchaseError("구매 응답이 없습니다.");
                return;
            }

            // 3. 백엔드가 구매 불가 판정을 내린 경우
            if (response.PurchaseStatus != "SUCCESS")
            {
                HandlePurchaseFailure(response.PurchaseStatus);
                return;
            }

            // 4. 구매는 성공했으므로 이후에는 성공 후 동기화로 처리
            try
            {
                await HandlePurchaseSuccess(response);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"구매 후 화면 동기화 실패: {e}");

                ShowPurchaseSyncError();
            }
        }
        finally
        {
            isPurchasing = false;
            closeButton.interactable = true;
        }
    }

    private void ShowPurchaseSyncError()
    {
        OpenPurchaseFailPopup(
            "구매는 완료되었지만 화면 갱신에 실패했습니다.\n" +
            "상점을 다시 열어 최신 정보를 확인해주세요."
        );
    }

    // 구매 불가 사유 분기
    private string GetPurchaseStatusMessage(string status)
    {
        switch (status)
        {
            case "AVAILABLE":
                return string.Empty;

            case "OUT_OF_STOCK":
                return "더 이상 구매할 수 없습니다.";

            case "REQUIREMENT_NOT_MET":
                return "아이템 구매 조건을 충족해야 구매할 수 있습니다.";

            case "INSUFFICIENT_GOLD":
                return "골드가 부족하여 구매할 수 없습니다.";

            case "ITEM_NOT_FOUND":
                return "해당 아이템을 찾을 수 없습니다.";

            default:
                return "현재 구매할 수 없는 아이템입니다.";
        }
    }
    // -----------------------------------------------------------------------------------
    // ----------------------------- 구매 처리 상세 --------------------------------------
    private async Task HandlePurchaseSuccess(PurchaseItemResponse response)
    {
        Debug.Log($"구매 성공: {response.ItemName}, " +  $"InventoryId: {response.InventoryId}");

        if (selectedItem == null || selectedItem.ItemSO == null)
            return;

        ItemType purchasedItemType = selectedItem.ItemSO.itemType;

        // 서버가 반환한 구매 후 골드를 사용
        goldText.text = $"{response.RemainingGold} G";

        // 새 아이템을 인벤토리에 반영
        await InventoryManager.Instance.RefreshInventoryAsync();

        // 재고 및 구매 가능 상태 갱신
        await RefreshShopAsync();

        ClosePopup();

        // 현재 탭의 아이템 목록 다시 출력
        if (purchasedItemType == ItemType.Equipment)
        {
            ShowEquipmentItems();
        }
        else if (purchasedItemType == ItemType.Consumable)
        {
            ShowConsumableItems();
        }
    }

    private void HandlePurchaseFailure(string code)
    {
        Debug.LogWarning($"구매 실패: {code}");

        string message = GetPurchaseStatusMessage(code);

        OpenPurchaseFailPopup(message);
    }

    private void OpenPurchaseFailPopup(string message)
    {
        purchaseFailPurchaseStatusText.text = message;

        // 기존 아이템 상세 팝업은 숨김
        itemDetailPopup.SetActive(false);

        purchaseFailPopup.SetActive(true);
    }

    private async void ClosePurchaseFailPopup()
    {
        if (selectedItem == null || selectedItem.ItemSO == null)
        {
            purchaseFailPopup.SetActive(false);
            ClosePopup();
            return;
        }

        ItemType failedItemType = selectedItem.ItemSO.itemType;

        purchaseFailPopup.SetActive(false);
        ClosePopup();

        try
        {
            await RefreshShopAsync();

            if (failedItemType == ItemType.Equipment)
            {
                ShowEquipmentItems();
            }
            else if (failedItemType == ItemType.Consumable)
            {
                ShowConsumableItems();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"구매 실패 후 상점 갱신 실패: {e}");
        }
    }

    private void ShowPurchaseError(string message)
    {
        OpenPurchaseFailPopup(message);
    }
    // -----------------------------------------------------------------------------------
}
