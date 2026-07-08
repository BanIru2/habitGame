using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class InventoryBackendManager : MonoBehaviour
{
    public async Task<List<InventoryItemResponse>> FetchInventoryAsync()
    {
        return await ServiceRegistry.Instance.Inventory.GetInventoryAsync(ApiClient.Instance.CurrentUserId);
    }

    private EquipItemRequest CreateEquipItemRequest(long inventoryId)
    {
        return new EquipItemRequest
        {
            UserId = ApiClient.Instance.CurrentUserId,
            InventoryId = inventoryId
        };
    }

    public async Task<EquipItemResponse> EquipItemAsync(long inventoryId)
    {
        EquipItemRequest request = CreateEquipItemRequest(inventoryId);
        return await ServiceRegistry.Instance.Inventory.EquipItemAsync(request);
    }

    public async Task<EquipItemResponse> UnequipItemAsync(long inventoryId)
    {
        EquipItemRequest request = CreateEquipItemRequest(inventoryId);
        return await ServiceRegistry.Instance.Inventory.UnequipItemAsync(request);
    }

    public async Task<UseItemResponse> UseItemAsync(long inventoryId)
    {
        UseItemRequest request = new UseItemRequest { InventoryId = inventoryId, UserId = ApiClient.Instance.CurrentUserId };
        return await ServiceRegistry.Instance.Inventory.UseItemAsync(request);
    }
}
