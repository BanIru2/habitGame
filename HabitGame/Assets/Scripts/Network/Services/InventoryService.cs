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
    public Task<List<InventoryItemResponse>> GetInventoryAsync()
    {
        return apiClient.GetAsync<List<InventoryItemResponse>>("/inventory");
    }

    // 아이템 장착/해제
    public Task<EquipItemResponse> SetEquippedAsync(EquipItemRequest request)
    {
        return apiClient.PatchAsync<EquipItemRequest, EquipItemResponse>(
            "/inventory/equip",
            request
        );
    }
}
