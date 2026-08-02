using System;
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
        userId = GetCurrentUserId();

        return apiClient.GetAsync<List<InventoryItemResponse>>(
            $"/inventory/me?userId={userId}"
        );
    }

    // 아이템 장착
    public Task<EquipItemResponse> EquipItemAsync(EquipItemRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        request.UserId = GetCurrentUserId();

        return apiClient.PostAsync<EquipItemRequest, EquipItemResponse>(
            "/inventory/equip",
            request
        );
    }

    // 아이템 해제
    public Task<EquipItemResponse> UnequipItemAsync(EquipItemRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        request.UserId = GetCurrentUserId();

        return apiClient.PostAsync<EquipItemRequest, EquipItemResponse>(
            "/inventory/unequip",
            request
        );
    }

    // 소비 아이템 사용
    public Task<UseItemResponse> UseItemAsync(UseItemRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        request.UserId = GetCurrentUserId();

        return apiClient.PostAsync<UseItemRequest, UseItemResponse>(
            "/inventory/use",
            request
        );
    }

    private long GetCurrentUserId()
    {
        long userId = apiClient.CurrentUserId;
        if (userId <= 0)
            throw new InvalidOperationException("로그인이 필요합니다.");

        return userId;
    }
}
