using System.Threading.Tasks;

public class ShopService
{
    private readonly ApiClient apiClient;

    public ShopService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    // 아이템 구매
    public Task<PurchaseItemResponse> PurchaseItemAsync(PurchaseItemRequest request)
    {
        return apiClient.PostAsync<PurchaseItemRequest, PurchaseItemResponse>(
            "/shop/purchase",
            request
        );
    }
}
