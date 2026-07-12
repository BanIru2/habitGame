using System.Collections.Generic;
using System.Threading.Tasks;

public class InventoryService
{
    private readonly ApiClient apiClient;

    public InventoryService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    // 인벤토리 조회
    public Task<List<InventoryItemResponse>> GetInventoryAsync(long userId)
    {
        return apiClient.GetAsync<List<InventoryItemResponse>>(
            $"/inventory/me?userId={userId}"
        );
    }

    // 아이템 장착
    public Task<EquipItemResponse> EquipItemAsync(EquipItemRequest request)
    {
        return apiClient.PostAsync<EquipItemRequest, EquipItemResponse>(
            "/inventory/equip",
            request
        );
    }

    // 아이템 해제
    public Task<EquipItemResponse> UnequipItemAsync(EquipItemRequest request)
    {
        return apiClient.PostAsync<EquipItemRequest, EquipItemResponse>(
            "/inventory/unequip",
            request
        );
    }

    // 소비 아이템 사용
    public Task<UseItemResponse> UseItemAsync(UseItemRequest request)
    {
        return apiClient.PostAsync<UseItemRequest, UseItemResponse>(
            "/inventory/use",
            request
        );
    }
}
