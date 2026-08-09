using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 탭 이동을 관장하는 매니저 클래스
/// </summary>
public class MainTapManager : Singleton<MainTapManager>
{
    [Header("각 탭의 부모 오브젝트")]
    [SerializeField]
    private GameObject habitTap;
    [SerializeField]
    private GameObject characterTap;
    [SerializeField]
    private GameObject inventoryTap;
    [SerializeField]
    private GameObject shopTap;
    [SerializeField]
    private GameObject battleTap;

    [Header("각 탭의 버튼")]
    [SerializeField]
    private Button habitTapButton;
    [SerializeField]
    private Button characterTapButton;
    [SerializeField]
    private Button inventoryTapButton;
    [SerializeField]
    private Button shopTapButton;
    [SerializeField]
    private Button battleTapButton;

    [SerializeField]
    private RankingboardManager rankingboardManager;

    // 탭 전환 동작 요청중인지 체크
    private bool isChangingTap;

    protected override void Awake()
    {
        base.Awake();
        habitTapButton.onClick.AddListener(OnClickHabitTap);
        characterTapButton.onClick.AddListener(OnClickCharacterTap);
        inventoryTapButton.onClick.AddListener(OnClickInventoryTap);
        shopTapButton.onClick.AddListener(OnClickShopTap);
        battleTapButton.onClick.AddListener(OnClickBattleTap);
    }

    private void CloseAllTaps()
    {
        habitTap.SetActive(false);
        characterTap.SetActive(false);
        inventoryTap.SetActive(false);
        shopTap.SetActive(false);
        battleTap.SetActive(false);
    }

    private void OnClickHabitTap()
    {
        if (isChangingTap) return;

        isChangingTap = true;

        try
        {
            // OpenAsync 함수 추가 필요
            CloseAllTaps();
            characterTap.SetActive(true);
        }
        catch (ApiException exception)
        {
            Debug.LogError(
                $"습관 정보 요청 실패 ({exception.StatusCode}): " + exception.Message
            );
        }
        catch (Exception exception)
        {
            Debug.LogError($"습관 조회 실패: {exception.Message}");
        }
        finally
        {
            isChangingTap = false;
        }
    }

    private async void OnClickCharacterTap()
    {
        if (isChangingTap) return;

        isChangingTap = true;

        try
        {
            await CharacterUIManager.Instance.OpenCharacterTap();
            CloseAllTaps();
            characterTap.SetActive(true);
        }
        catch (ApiException exception)
        {
            Debug.LogError(
                $"캐릭터 정보 요청 실패 ({exception.StatusCode}): " + exception.Message
            );
        }
        catch (Exception exception)
        {
            Debug.LogError($"캐릭터 조회 실패: {exception.Message}");
        }
        finally
        {
            isChangingTap = false;
        }
    }

    private async void OnClickInventoryTap()
    {
        if (isChangingTap) return;

        isChangingTap = true;

        try
        {
            await InventoryManager.Instance.OpenInventory();
            CloseAllTaps();
            inventoryTap.SetActive(true);
        }
        catch (ApiException exception)
        {
            Debug.LogError(
                $"인벤토리 정보 요청 실패 ({exception.StatusCode}): " + exception.Message
            );
        }
        catch (Exception exception)
        {
            Debug.LogError($"인벤토리 조회 실패: {exception.Message}");
        }
        finally
        {
            isChangingTap = false;
        }
    }

    private async void OnClickShopTap()
    {
        if (isChangingTap) return;

        isChangingTap = true;

        try
        {
            await ShopUIManager.Instance.OpenShop();
            CloseAllTaps();
            shopTap.SetActive(true);
        }
        catch (ApiException exception)
        {
            Debug.LogError(
                $"상점 정보 요청 실패 ({exception.StatusCode}): " + exception.Message
            );
        }
        catch (Exception exception)
        {
            Debug.LogError($"상점 조회 실패: {exception.Message}");
        }
        finally
        {
            isChangingTap = false;
        }
    }

    private async void OnClickBattleTap()
    {
        if (isChangingTap) return;

        isChangingTap = true;

        try
        {
            await rankingboardManager.LoadRankingBoard();
            CloseAllTaps();
            battleTap.SetActive(true);
        }
        catch (ApiException exception)
        {
            Debug.LogError(
                $"랭킹 정보 요청 실패 ({exception.StatusCode}): " + exception.Message
            );
        }
        catch (Exception exception)
        {
            Debug.LogError($"랭킹 보드 조회 실패: {exception.Message}");
        }
        finally
        {
            isChangingTap = false;
        }
    }
}
