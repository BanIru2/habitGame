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
        //habitTap.SetActive(false);
        characterTap.SetActive(false);
        inventoryTap.SetActive(false);
        shopTap.SetActive(false);
        battleTap.SetActive(false);
    }

    private void OnClickHabitTap()
    {

    }

    private void OnClickCharacterTap()
    {
        CloseAllTaps();
        characterTap.SetActive(true);
        CharacterUIManager.Instance.OpenCharacterTap();
    }

    private void OnClickInventoryTap()
    {
        CloseAllTaps();
        inventoryTap.SetActive(true);
        InventoryManager.Instance.OpenInventory();
    }

    private void OnClickShopTap()
    {
        CloseAllTaps();
        shopTap.SetActive(true);
        ShopUIManager.Instance.OpenShop();
    }

    private void OnClickBattleTap()
    {
        CloseAllTaps();
        battleTap.SetActive(true);
        rankingboardManager.LoadRankingBoard();
    }
}
