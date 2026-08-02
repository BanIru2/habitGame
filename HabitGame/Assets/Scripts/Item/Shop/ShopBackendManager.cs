using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ShopBackendManager : MonoBehaviour
{
    public async Task<List<ItemResponse>> FetchShopItemsAsync()
    {
        return await ServiceRegistry.Instance.Shop.GetItemsAsync();
    }

    public async Task<PurchaseItemResponse> PurchaseItemAsync(string itemId)
    {
        PurchaseItemRequest request = new PurchaseItemRequest
        {
            UserId = ApiClient.Instance.CurrentUserId,
            ItemId = itemId
        };

        return await ServiceRegistry.Instance.Shop.PurchaseItemAsync(request);
    }
}
