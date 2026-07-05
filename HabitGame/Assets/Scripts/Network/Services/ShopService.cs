using System.Collections.Generic;
using System.Threading.Tasks;

public class ShopService
{
    private readonly ApiClient apiClient;

    public ShopService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    public Task<List<ItemResponse>> GetItemsAsync()
    {
        return apiClient.GetAsync<List<ItemResponse>>("/shop/items");
    }

    public Task<PurchaseItemResponse> PurchaseItemAsync(PurchaseItemRequest request)
    {
        return apiClient.PostAsync<PurchaseItemRequest, PurchaseItemResponse>(
            "/shop/buy",
            request
        );
    }

    public Task<List<InventoryItemResponse>> GetInventoryAsync(long userId)
    {
        return apiClient.GetAsync<List<InventoryItemResponse>>(
            $"/shop/inventory/me?userId={userId}"
        );
    }

    public Task<EquipItemResponse> EquipItemAsync(EquipItemRequest request)
    {
        return apiClient.PostAsync<EquipItemRequest, EquipItemResponse>(
            "/shop/equip",
            request
        );
    }

    public Task<EquipItemResponse> UnequipItemAsync(EquipItemRequest request)
    {
        return apiClient.PostAsync<EquipItemRequest, EquipItemResponse>(
            "/shop/unequip",
            request
        );
    }
}