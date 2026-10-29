using ExitGames.Client.Photon;
using Photon;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;    // 모호성 방지
using System.Threading.Tasks;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class BattleUIManager : SceneSingleton<BattleUIManager>
{
    private readonly StringBuilder sb = new StringBuilder(64);

    [SerializeField]
    private PhotonManager photonManager;

    [SerializeField]
    private GameObject lobbyPanel;
    [SerializeField]
    private GameObject loadingPanel;
    [SerializeField]
    private GameObject readyPanel;
    [SerializeField]
    private GameObject battlePanel;
    [SerializeField]
    private GameObject resultPanel;

    // Lobby Panel
    [Header("Lobby Panel")]
    [SerializeField]
    private Button matchStartButton;
    [SerializeField]
    private GameObject dailyPvpLimitReachedPopup;
    [SerializeField]
    private Button dailyPvpLimitReachedPopupCloseButton;

    // Loadinig Panel
    [Header("Loading Panel")]
    [SerializeField]
    private RectTransform loadingSpinner;
    [SerializeField]
    private float spinSpeed;

    [SerializeField]
    private Button matchCancelButton;

    [SerializeField]
    private TextMeshProUGUI currentParticipantsCount;

    private bool isMatching = false;
    public bool IsMatching => isMatching;

    // Ready Panel
    [Header("Ready Panel")]
    [SerializeField]
    private Button checkOppAttrButton;
    [SerializeField]
    private GameObject attrScoutPppup;
    [SerializeField]
    private TextMeshProUGUI remainScouterCountText;
    [SerializeField]
    private Button useScouterButton;
    [SerializeField]
    private Button closeScoutButton;
    [SerializeField]
    private TextMeshProUGUI[] oppAttrText;
    [SerializeField]
    private CanvasGroup[] oppAttrTextCanvasGroup;
    [SerializeField]
    private Button[] attrButtons;
    [SerializeField]
    private Button[] consumableItemButtons;
    [SerializeField]
    private Button[] equipItemButtons;
    [SerializeField]
    private Button readyButton;
    [SerializeField]
    private TextMeshProUGUI myNameText;
    [SerializeField]
    private TextMeshProUGUI oppNameText;
    [SerializeField]
    private TextMeshProUGUI isMeReadyText;
    [SerializeField]
    private TextMeshProUGUI isOppReadyText;

    // 선택된 특성 (외부참조 가능 -> 전투 로직에 사용)
    public AttributeType selectedAttrType { get; private set; }
    // 선택된 아이템
    private InventoryItemViewData selectedConsumableItem;

    private EquipmentType equipType;
    private bool isUsedAttributeScout = false;
    public bool IsUsedAttributeScout => isUsedAttributeScout;    // BattleManager 동기화 용 프로퍼티
    private bool isReady = false;
    private Color readyButtonDefaultColor;
    private string readyButtonDefaultText;
    private string oppNameTextDefaultText;
    private string oppBattleNameDefaultText;
    private string[] oppAttrTextDefaultTexts;
    private float[] oppAttrTextDefaultAlphas;


    [Header("Ready Panel/EquipSelectPanel")]
    [SerializeField]
    private GameObject equipSelectPanel;
    [SerializeField]
    private TextMeshProUGUI equipTypeText;
    [SerializeField]
    private Transform itemSlotParent;
    [SerializeField]
    private Button equipSelectCloseButton;
    [SerializeField]
    private ItemSlotUI itemSlotPrefab;

    private List<ItemSlotUI> itemSlotPool = new List<ItemSlotUI>();    // 아이템 슬롯 재사용을 위한 저장 풀

    [Header("Ready/EquipeSelect/ItemDetailPopup")]
    [SerializeField]
    private GameObject equipDetailPopup;
    [SerializeField]
    private Image equipIcon;
    [SerializeField] 
    private TextMeshProUGUI equipNameText;
    [SerializeField] 
    private TextMeshProUGUI equipDescText;
    [SerializeField] 
    private Button equipActionButton;
    [SerializeField]
    private TextMeshProUGUI equipActionButtonText;
    [SerializeField] 
    private Button equipDetailCloseButton;

    private InventoryItemViewData selectedEquipItem;    // 현재 선택된 장비 아이템 저장

    [Header("Battle Result Waiting")]
    [SerializeField] private GameObject resultWaitingPanel;
    [SerializeField] private TextMeshProUGUI resultWaitingText;
    [SerializeField] private UnityEngine.UI.Button resultWaitingCloseButton;

    private string activeBattleId;
    private BattleResultResponse savedResultResponse;
    private bool replayFinished;
    private bool resultDisplayed;
    private bool resultRequestFailed;

    // Battle Panel
    [Header("Battle Panel")]
    [SerializeField]
    private TextMeshProUGUI myName;
    [SerializeField]
    private TextMeshProUGUI oppName;
    [SerializeField]
    private Image myBattleTitleImage;
    [SerializeField]
    private Image oppBattleTitleImage;
    [SerializeField]
    private Image myHpBar;
    [SerializeField]
    private Image oppHpBar;
    [SerializeField]
    private TextMeshProUGUI myHpText;
    [SerializeField]
    private TextMeshProUGUI oppHpText;
    [SerializeField]
    private RawImage myCharacterImage;
    [SerializeField]
    private RawImage oppCharacterImage;
    [SerializeField]
    private TextMeshProUGUI turnCountText;

    // BattlePanel / ResultPanel
    [SerializeField]
    private GameObject winImage;
    [SerializeField]
    private GameObject loseImage;
    [SerializeField]
    private GameObject drawImage;
    [SerializeField]
    private TextMeshProUGUI currentRankingPointText;
    [SerializeField]
    private TextMeshProUGUI operatorText;
    [SerializeField]
    private TextMeshProUGUI resultRankingPointText;
    [SerializeField]
    private Button checkButton;

    [SerializeField]
    private RankingboardManager rankingboardManager;

    [SerializeField]
    private GameObject tapBar;

    protected override void Awake()
    {
        base.Awake();

        dailyPvpLimitReachedPopupCloseButton.onClick.AddListener(CloseDailyPvpLimitReachedPopup);

        CacheReadyButtonDefaultState();
        CacheOpponentInfoDefaultState();

        matchStartButton.onClick.AddListener(OnClickStartMatchmaking);
        matchCancelButton.onClick.AddListener(OnClickCancelMatchmaking);
        checkOppAttrButton.onClick.AddListener(OnClickCheckOppAttr);
        foreach(var button in attrButtons)
        {
            button.onClick.AddListener(() => OnClickAttributeButton(button));
        }
        foreach(var button in consumableItemButtons)
        {
            button.onClick.AddListener(() => OnClickConsumableItemButton(button));
        }
        foreach(var button in equipItemButtons)
        {
            button.onClick.AddListener(() => OnClickEquipItemButton(button));
        }
        readyButton.onClick.AddListener(OnClickReadyButton);
        checkButton.onClick.AddListener(OnClickCheckButton);

        useScouterButton.onClick.AddListener(UseAttrScouter);
        closeScoutButton.onClick.AddListener(CloseAttrScoutPopup);

        equipSelectCloseButton.onClick.AddListener(CloseEquipSelectPanel);

        equipActionButton.onClick.AddListener(OnEquipButtonClicked);
        equipDetailCloseButton.onClick.AddListener(CloseEquipDetailPopup);

        resultWaitingCloseButton.onClick.AddListener(OnClickCheckButton);

        ResetBattleResultState();
    }

    private void Update()
    {
        if (isMatching)
            loadingSpinner.Rotate(0, 0, -spinSpeed * Time.deltaTime);
    }

    // -----------------------------Lobby Panel-----------------------------------
    // 매칭 시작 버튼 클릭 시 화면 전환
    private void OnClickStartMatchmaking()
    {
        if (!rankingboardManager.IsCanMatchPvP())
        {
            dailyPvpLimitReachedPopup.SetActive(true);
            return;
        }

        if (!TryResolvePhotonManager()) return;

        if (!photonManager.StartMatchmaking()) return;

        ClearOpponentInfoUI();
        lobbyPanel.SetActive(false);
        tapBar.SetActive(false);
        loadingPanel.SetActive(true);
        readyPanel.SetActive(false);
        battlePanel.SetActive(false);
        isMatching = true;
    }
    
    private void CloseDailyPvpLimitReachedPopup()
    {
        dailyPvpLimitReachedPopup.SetActive(false);
    }
    // ------------------------------Loading Pannel---------------------------------
    // 매칭 취소 버튼 클릭 시 로비로 이동
    private void OnClickCancelMatchmaking()
    {
        if (!TryResolvePhotonManager())
        {
            ReturnToLobby();
            return;
        }

        photonManager.ReturnToLobby();
    }

    public void BackToMatchingAfterOpponentLeft()
    {
        readyPanel.SetActive(false);
        battlePanel.SetActive(false);
        loadingPanel.SetActive(true);
        lobbyPanel.SetActive(false);

        isMatching = true;
        ResetReadyUI();
        ClearOpponentInfoUI();

        UpdatePlayerCount(PhotonNetwork.CurrentRoom.PlayerCount);
    }

    // 로딩 완료 시 화면 전환
    public void LoadingComplete()
    {
        loadingPanel.SetActive(false);
        readyPanel.SetActive(true);
        isMatching = false;
        ResetReadyUI();
    }

    public void UpdatePlayerCount(int current)
    {
        currentParticipantsCount.text = current.ToString();
    }

    //-------------------------------Ready Panel-----------------------------------
    // 준비 단계 완료 시(양 측 다 준비 완료 or 준비 시간 끝) 화면 전환
    public void ReadyComplete(string receivedBattleId)
    {
        readyPanel.SetActive(false);
        battlePanel.SetActive(true);
        InitBattleUI(receivedBattleId);
    }

    private void ResetReadyUI()
    {
        isReady = false;
        isMeReadyText.text = "WAITING";
        isMeReadyText.color = Color.black;
        isOppReadyText.text = "WAITING";
        isOppReadyText.color = Color.black;
        readyButton.interactable = true;
        checkOppAttrButton.interactable = true;

        ClearSelectedConsumableItem();

        readyButton.image.color = readyButtonDefaultColor;
        var buttonText = readyButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = readyButtonDefaultText;
        }
    }

    private void CacheReadyButtonDefaultState()
    {
        readyButtonDefaultColor = readyButton.image.color;
        var buttonText = readyButton.GetComponentInChildren<TextMeshProUGUI>();
        readyButtonDefaultText = buttonText != null ? buttonText.text : string.Empty;
    }

    private void CacheOpponentInfoDefaultState()
    {
        oppNameTextDefaultText = oppNameText.text;
        oppBattleNameDefaultText = oppName.text;

        oppAttrTextDefaultTexts = new string[oppAttrText.Length];
        for (int i = 0; i < oppAttrText.Length; i++)
        {
            oppAttrTextDefaultTexts[i] = oppAttrText[i].text;
        }

        oppAttrTextDefaultAlphas = new float[oppAttrTextCanvasGroup.Length];
        for (int i = 0; i < oppAttrTextCanvasGroup.Length; i++)
        {
            oppAttrTextDefaultAlphas[i] = oppAttrTextCanvasGroup[i].alpha;
        }
    }

    private bool TryResolvePhotonManager()
    {
        if (photonManager == null)
        {
            photonManager = FindObjectOfType<PhotonManager>();
        }

        if (photonManager != null) return true;

        Debug.LogError("PhotonManager not found.");
        return false;
    }

    // 상대 특성 확인 버튼 클릭 시 동작
    private void OnClickCheckOppAttr()
    {
        InventoryItemViewData data = InventoryManager.Instance.GetAttributeScouterData();
        // 특성 탐색권을 보유하지 않은 경우
        if (data == null)
        {
            ErrorPopupManager.Instance.ShowMessage("특성 탐색권을 보유하고 있지 않습니다.");
            return;
        }

        OpenAttrScoutPopup(data);
    }

    // 특성 탐색권 팝업 활성화
    private void OpenAttrScoutPopup(InventoryItemViewData data)
    {
        sb.Clear();
        sb.Append("현재 남은 탐색권 개수 : ").Append(data.Response.Quantity);
        remainScouterCountText.SetText(sb);

        attrScoutPppup.SetActive(true);
    }

    // 특성 탐색권 팝업 UI 초기화
    private void InitAttrScoutPopup()
    {
        remainScouterCountText.text = "현재 남은 탐색권 개수 : ";
    }

    // 특성 탐색권 팝업 닫기
    private void CloseAttrScoutPopup()
    {
        attrScoutPppup.SetActive(false);
        InitAttrScoutPopup();
    }

    // 특성 탐색권 사용
    // 로컬 동작 후 전투 종료 시 서버 처리를 요청하여 전투가 정상종료 된 경우에만 사용처리되도록 구성
    private void UseAttrScouter()
    {
        if (isUsedAttributeScout) return;

        // 사용 플래그 활성화
        isUsedAttributeScout = true;
        // 특성 탐색권 팝업 여는 버튼 비활성화 (재사용 방지)
        checkOppAttrButton.interactable = false;

        // 출력 (알파값 활성화)
        foreach (var canvasGroup in oppAttrTextCanvasGroup)
        {
            canvasGroup.alpha = 1f;
        }

        // 사용 완료 후 팝업 닫기
        CloseAttrScoutPopup();
    }

    // 사용할 특성 선택 버튼 클릭 시 동작
    private void OnClickAttributeButton(Button clickedButton)
    {
        if (isReady)
        {
            Debug.LogWarning("준비를 취소한 뒤 세팅을 변경할 수 있습니다.");
            return;
        }

        // 선택된 버튼은 색깔강조, 그 외 버튼은 흰색으로 초기화
        foreach (var btn in attrButtons)
        {
            btn.image.color = (btn == clickedButton) ? new Color(1f, 0.56f, 0f, 0.2f) : Color.white;
        }
        // 선택된 버튼의 속성을 사용
        selectedAttrType = clickedButton.GetComponent<AttributeButtonInfo>().attrType;
    }

    // 사용할 소비 아이템 선택 버튼 클릭 시 동작
    private void OnClickConsumableItemButton(
    UnityEngine.UI.Button clickedButton)
    {
        if (isReady)
        {
            Debug.LogWarning("준비를 취소한 뒤 세팅을 변경할 수 있습니다.");
            return;
        }

        ConsumableButtonInfo buttonInfo = clickedButton.GetComponent<ConsumableButtonInfo>();

        if (buttonInfo == null || buttonInfo.itemData == null)
        {
            Debug.LogWarning("소비 아이템 버튼 정보가 없습니다.");
            return;
        }

        // 이미 선택한 포션을 다시 누르면 선택 해제
        if (selectedConsumableItem != null && selectedConsumableItem.ItemSO == buttonInfo.itemData)
        {
            ClearSelectedConsumableItem();
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager가 없습니다.");
            return;
        }

        List<InventoryItemViewData> consumableItems = InventoryManager.Instance.GetBattlePreparationConsumableItems();

        InventoryItemViewData matchedItem = consumableItems.Find(item => item.ItemSO == buttonInfo.itemData);

        if (matchedItem == null)
        {
            Debug.LogWarning("보유 중인 전투 준비 소비 아이템이 아니거나 수량이 없습니다.");

            RefreshConsumableItemUI();
            return;
        }

        // 다른 포션을 선택하면 이전 선택을 교체
        selectedConsumableItem = matchedItem;

        RefreshConsumableItemUI();
    }

    private void RefreshConsumableItemUI()
    {
        // 이미 받아둔 인벤토리를 읽는 동작
        List<InventoryItemViewData> items = InventoryManager.Instance != null ? InventoryManager.Instance.GetBattlePreparationConsumableItems() : new List<InventoryItemViewData>();

        foreach (var button in consumableItemButtons)
        {
            if (button == null)
                continue;

            ConsumableButtonInfo info = button.GetComponent<ConsumableButtonInfo>();

            if (info == null || info.itemData == null)
            {
                if (info != null)
                    info.SetCount(0);

                button.interactable = false;
                continue;
            }

            int ownedCount = 0;

            foreach (var item in items)
            {
                if (item.ItemSO == info.itemData)
                {
                    ownedCount += Mathf.Max(0, item.Response.Quantity);
                }
            }

            bool isSelected = selectedConsumableItem != null && selectedConsumableItem.ItemSO == info.itemData;

            // 실제 인벤토리는 변경하지 않고 표시만 1개 차감
            int displayCount = Mathf.Max(0, ownedCount - (isSelected ? 1 : 0));

            info.SetCount(displayCount);

            button.image.color = isSelected ? new Color(1f, 0.56f, 0f, 0.2f) : Color.white;

            // 1개 보유 → 선택 후 표시가 0이어도 다시 눌러 해제 가능
            button.interactable = !isReady && (ownedCount > 0 || isSelected);
        }
    }

    private bool TryRefreshSelectedConsumableItem()
    {
        if (selectedConsumableItem == null)
        {
            return true;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryManager가 없어 선택한 소비 아이템을 확인할 수 없습니다.");
            ClearSelectedConsumableItem();
            return false;
        }

        long inventoryId = selectedConsumableItem.Response.InventoryId;

        List<InventoryItemViewData> consumableItems = InventoryManager.Instance.GetBattlePreparationConsumableItems();

        InventoryItemViewData latestItem = consumableItems.Find(item => item.Response.InventoryId == inventoryId);

        if (latestItem == null)
        {
            Debug.LogWarning("선택한 소비 아이템을 더 이상 사용할 수 없습니다.");
            ClearSelectedConsumableItem();
            return false;
        }

        selectedConsumableItem = latestItem;
        return true;
    }

    private void ClearSelectedConsumableItem()
    {
        selectedConsumableItem = null;
        RefreshConsumableItemUI();
    }

    // 장비 아이템 장착--------------------------------------------
    private void OnClickEquipItemButton(Button clickedButton)
    {
        if (isReady)
        {
            Debug.LogWarning("준비를 취소한 뒤 세팅을 변경할 수 있습니다.");
            return;
        }

        // 클릭한 버튼의 장비 부위 정보 저장
        equipType = clickedButton.GetComponent<EquipButtonInfo>().type;
        // 보유중인 아이템을 부위에 따라 버튼으로 만들어 보여주고 선택 시 교체할 수 있도록 하는 기능 필요
        OpenEquipSelectPanel(equipType);
    }

    // 아이템 데이터 ItemSlot으로 화면에 생성
    private void RenderItems(List<InventoryItemViewData> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            InventoryItemViewData item = items[i];

            ItemSlotUI slot = GetSlot(i);
            slot.gameObject.SetActive(true);

            slot.LoadData(item, OnEquipSlotClicked);
        }

        HideUnusedSlots(items.Count);
    }

    // 슬롯이 남아 있다면 재사용, 없다면 생성
    private ItemSlotUI GetSlot(int index)
    {
        if (index < itemSlotPool.Count)
        {
            return itemSlotPool[index];
        }

        ItemSlotUI slot = Instantiate(itemSlotPrefab, itemSlotParent);
        itemSlotPool.Add(slot);

        return slot;
    }

    // 사용하지 않는 슬롯 숨기기
    private void HideUnusedSlots(int usedCount)
    {
        for (int i = usedCount; i < itemSlotPool.Count; i++)
        {
            itemSlotPool[i].gameObject.SetActive(false);
        }
    }

    private void OnEquipSlotClicked(InventoryItemViewData item)
    {
        OpenEquipDetailPopup(item);
    }

    private void OpenEquipSelectPanel(EquipmentType equipType)
    {
        equipSelectPanel.SetActive(true);
        switch (equipType)
        {
            case EquipmentType.Clothes:
                equipTypeText.text = "옷";
                break;
            case EquipmentType.Shoes:
                equipTypeText.text = "신발";
                break;
            case EquipmentType.Hat:
                equipTypeText.text = "모자";
                break;
            case EquipmentType.Weapon:
                equipTypeText.text = "무기";
                break;
        }

        List<InventoryItemViewData> items = InventoryManager.Instance.GetEquipmentItems(equipType);

        RenderItems(items);
    }

    private void OpenEquipDetailPopup(InventoryItemViewData item)
    {
        if (item == null) return;

        if (item.ItemSO is not EquipmentDataSO equipmentSO)
        {
            Debug.LogWarning("선택한 아이템이 장비 아이템이 아닙니다.");
            return;
        }

        selectedEquipItem = item;

        equipDetailPopup.SetActive(true);

        equipIcon.sprite = equipmentSO.icon;
        equipIcon.enabled = equipmentSO.icon != null;

        equipNameText.text = equipmentSO.displayName;
        equipDescText.text = equipmentSO.description;

        bool isEquipped = item.Response.IsEquipped;

        equipActionButtonText.text = isEquipped ? "해제하기" : "장착하기";
        equipActionButton.image.color = isEquipped ? Color.red : new Color32(50, 184, 255, 255);
    }

    private void CloseEquipSelectPanel()
    {
        CloseEquipDetailPopup();
        equipSelectPanel.SetActive(false);
    }

    // 상세 팝업에서 장착 버튼 클릭 시
    // 선택된 장비 장착/해제 -> 해당 부위 장비 목록 다시 가져와 UI 갱신
    private async void OnEquipButtonClicked()
    {
        if (isReady)
        {
            Debug.LogWarning("준비를 취소한 뒤 세팅을 변경할 수 있습니다.");
            return;
        }

        if (selectedEquipItem == null) return;

        if (InventoryManager.Instance.IsEquipmentChangeInProgress)
        {
            Debug.LogWarning("이미 장비 변경 요청이 진행 중입니다.");
            return;
        }

        equipActionButton.interactable = false;
        readyButton.interactable = false;

        try
        {
            long inventoryId = selectedEquipItem.Response.InventoryId;

            if (selectedEquipItem.Response.IsEquipped)
            {
                await InventoryManager.Instance.UnequipInventoryItemAsync(inventoryId);
            }
            else
            {
                await InventoryManager.Instance.EquipInventoryItemAsync(inventoryId);
            }

            List<InventoryItemViewData> items = InventoryManager.Instance.GetEquipmentItems(equipType);
            RenderItems(items);

            InventoryItemViewData updatedItem = items.Find(item => item.Response.InventoryId == inventoryId);
            
            if (updatedItem != null)
            {
                OpenEquipDetailPopup(updatedItem);
            }
            else
            {
                CloseEquipDetailPopup();
            }
        }
        catch (ApiException e)
        {
            Debug.LogError($"PvP 장비 변경 실패: {e.Message}");
            ErrorPopupManager.Instance.ShowApiError(e);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"PvP 장비 변경 처리 중 시스템 오류: {e.Message}");
            ErrorPopupManager.Instance.ShowSystemError();
        }
        finally
        {
            equipActionButton.interactable = true;
            readyButton.interactable = true;

            RefreshConsumableItemUI();
        }
    }

    private void CloseEquipDetailPopup()
    {
        selectedEquipItem = null;
        equipDetailPopup.SetActive(false);
    }
    // -------------------------------------------------------------
    public async Task<bool> PrepareLocalBattleUnitAfterReadyConfirmedAsync(int roomFlowVersion)
    {
        readyButton.interactable = false;

        if (!TryRefreshSelectedConsumableItem())
        {
            return false;
        }

        InventoryItemViewData consumableForBattle = selectedConsumableItem;

        if (consumableForBattle != null)
        {
            try
            {
                long inventoryId = consumableForBattle.Response.InventoryId;
                await InventoryManager.Instance.UseInventoryItemAsync(inventoryId);
                if (!photonManager.IsCurrentRoomFlow(roomFlowVersion))
                    return false;
            }
            catch (ApiException e)
            {
                Debug.LogError($"전투 준비 소비 아이템 사용 실패: {e.Message}");
                ErrorPopupManager.Instance.ShowApiError(e);
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"전투 준비 소비 아이템 사용 처리 중 시스템 오류: {e.Message}");
                ErrorPopupManager.Instance.ShowSystemError();
                return false;
            }
        }

        if (!photonManager.IsCurrentRoomFlow(roomFlowVersion))
            return false;

        BattleSetupData setup = new BattleSetupData
        {
            selectedAttr = selectedAttrType,
            selectedConsumableItem = consumableForBattle,
        };

        BattleUnit myUnit = CharacterManager.Instance.CreateBattleUnit(setup);
        if (!photonManager.IsCurrentRoomFlow(roomFlowVersion))
            return false;

        photonManager.RegisterBattleUnitProperties(myUnit);

        ClearSelectedConsumableItem();

        return true;
    }

    public void ResetReadyUIAfterPreparationFailure()
    {
        ResetReadyUI();
    }

    // 준비완료 버튼 클릭 시
    private void OnClickReadyButton()
    {
        if (!TryResolvePhotonManager()) return;
        if (InventoryManager.Instance != null &&
            (InventoryManager.Instance.IsEquipmentChangeInProgress ||
             InventoryManager.Instance.IsItemUseInProgress))
        {
            Debug.LogWarning("장비 변경 처리 중에는 Ready 할 수 없습니다.");
            return;
        }

        if (!TryRefreshSelectedConsumableItem())
        {
            return;
        }

        var buttonText = readyButton.GetComponentInChildren<TextMeshProUGUI>();
        if (!isReady)
        {
            isReady = true;

            readyButton.image.color = Color.red;
            buttonText.text = "준비 취소";
            isMeReadyText.text = "READY";
            isMeReadyText.color = Color.green;
        }
        else
        {
            isReady = false;
            readyButton.image.color = Color.blue;
            buttonText.text = "준비 완료";
            isMeReadyText.text = "WAITING";
            isMeReadyText.color = Color.black;
        }

        RefreshConsumableItemUI();

        // RPC 송신 (상대방 READY 글자 실시간 변경)
        BattleManager.Instance.SendReadyState(isReady);
        // 포톤 서버에 준비 상태 등록 (방장의 전투 시작 체크)
        Hashtable props = new Hashtable();
        props.Add("IsReady", isReady);
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    [PunRPC]
    public void RPC_UpdateOpponentReady(bool ready)
    {
        isOppReadyText.text = ready ? "READY" : "WAITING";
        isOppReadyText.color = ready ? Color.green : Color.red;
    }

    public void SetMyInfoUI(string name)
    {
        myNameText.text = name;
        myName.text = name;
    }

    public void ClearOpponentInfoUI()
    {
        oppNameText.text = oppNameTextDefaultText;
        oppName.text = oppBattleNameDefaultText;
        SetTitle(false, null);

        for (int i = 0; i < oppAttrText.Length; i++)
        {
            oppAttrText[i].text = i < oppAttrTextDefaultTexts.Length ? oppAttrTextDefaultTexts[i] : string.Empty;
        }

        for (int i = 0; i < oppAttrTextCanvasGroup.Length; i++)
        {
            oppAttrTextCanvasGroup[i].alpha = i < oppAttrTextDefaultAlphas.Length ? oppAttrTextDefaultAlphas[i] : 0f;
        }
    }

    // 상대방의 정보를 받아 UI 텍스트에 출력
    public void SetOpponentInfoUI(string name, int fire, int water, int grass, int aurora)
    {
        oppNameText.text = name;
        oppName.text = name;

        oppAttrText[0].text = $"Lv.{fire}";
        oppAttrText[1].text = $"Lv.{water}";
        oppAttrText[2].text = $"Lv.{grass}";
        oppAttrText[3].text = $"Lv.{aurora}";
    }

    public void SetOppoentInfoUI(string name, int fire, int water, int grass, int aurora)
    {
        SetOpponentInfoUI(name, fire, water, grass, aurora);
    }

    //------------------------------Battle Panel-------------------------------
    // UI 초기화에 필요한 기능 추가할 것
    // 전투 시작 전 호출되어야 함
    public void InitBattleUI(string receivedBattleId)
    {
        ResetBattleResultState();
        activeBattleId = receivedBattleId;

        InitCharacterHp();
        InitResultImage();
        InitRankingPointText();
        UpdateTurnText(1);
    }

    // 각 플레이어 이름 정보 받아와서 출력하기
    // 각 플레이어 유닛 정보를 앞전에 미리 받아온다면 private으로 받고 InitBattleUI에서 호출 가능
    public void UpdateTurnText(int turn)
    {
        if (turnCountText != null)
            turnCountText.text = $"{turn}턴";
    }

    public void GetName(string myNameSTr, string oppNameStr)
    {
        myNameText.text = myNameSTr;
        oppNameText.text = oppNameStr;
        myName.text = myNameSTr;
        oppName.text = oppNameStr;
    }

    // 체력바 초기화
    private void InitCharacterHp()
    {
        myHpBar.fillAmount = 1f;
        oppHpBar.fillAmount = 1f;
    }

    private void InitResultImage()
    {
        winImage.SetActive(false);
        loseImage.SetActive(false);
        drawImage.SetActive(false);
        resultPanel.SetActive(false);
    }

    private void InitRankingPointText()
    {
        currentRankingPointText.text = "0";
        operatorText.text = "+";
        resultRankingPointText.text = "0";
    }

    // 체력바 업데이트
    public void UpdateCharacterHpBar(bool isPlayer, float currentHp, float maxHp)
    {
        float ratio = Mathf.Clamp01(currentHp / maxHp);

        if (isPlayer)
        {
            myHpBar.fillAmount = ratio;
            myHpText.text = $"{currentHp} / {maxHp}";
        }
        else
        {
            oppHpBar.fillAmount = ratio;
            oppHpText.text = $"{currentHp} / {maxHp}";
        }
    }

    public void SetBattleHpUI(int myHp,int oppHp)
    {
        myHpText.text = $"{myHp} / {myHp}";
        oppHpText.text = $"{oppHp} / {oppHp}";

        InitCharacterHp();
    }

    // 칭호 세팅
    public void SetTitle(bool isMine, Sprite sprite)
    {
        Image targetImage = isMine ? myBattleTitleImage : oppBattleTitleImage;
        if (targetImage == null) return;

        targetImage.sprite = sprite;
        targetImage.gameObject.SetActive(sprite != null);
    }

    // ------------------------- Battle Finish ----------------------------
    public void FinishBattle(BattleResultResponse response)
    {
        if (response == null || response.Status != "CONFIRMED") return;
        if (activeBattleId == null || response.BattleId != activeBattleId) return;

        savedResultResponse = response;
        resultRequestFailed = false;
        TryShowBattleResult();
    }

    public void OnReplayFinished(string finishedBattleId)
    {
        if (activeBattleId == null || finishedBattleId != activeBattleId) return;

        replayFinished = true;

        if (savedResultResponse == null)
            ShowResultWaiting();

        TryShowBattleResult();
    }

    private void TryShowBattleResult()
    {
        if (!replayFinished || savedResultResponse == null || resultDisplayed)
            return;

        resultDisplayed = true;
        HideResultWaiting();

        InitResultImage();
        PrintResultImage(savedResultResponse.Result);
        SetRankingPointText(savedResultResponse);
        resultPanel.SetActive(true);
    }

    private void ShowResultWaiting()
    {
        if (resultWaitingText != null)
        {
            resultWaitingText.text = resultRequestFailed ? "전투 결과를 확인하지 못했습니다. 로비로 돌아가 주세요." : "전투 결과 확인 중…";
        }

        if (resultWaitingCloseButton != null)
            resultWaitingCloseButton.gameObject.SetActive(resultRequestFailed);

        if (resultWaitingPanel != null)
        {
            resultWaitingPanel.transform.SetAsLastSibling();
            resultWaitingPanel.SetActive(true);
        }
    }

    private void HideResultWaiting()
    {
        if (resultWaitingPanel != null)
            resultWaitingPanel.SetActive(false);
    }

    public void NotifyBattleResultRequestFailed(string failedBattleId)
    {
        if (activeBattleId == null || failedBattleId != activeBattleId) return;
        if (savedResultResponse != null || resultDisplayed) return;

        resultRequestFailed = true;

        if (replayFinished)
            ShowResultWaiting();
    }

    private void ResetBattleResultState()
    {
        activeBattleId = null;
        savedResultResponse = null;
        replayFinished = false;
        resultDisplayed = false;
        resultRequestFailed = false;
        HideResultWaiting();
    }

    private void OnClickCheckButton()
    {
        if (!TryResolvePhotonManager())
        {
            ReturnToLobby();
            return;
        }

        photonManager.ReturnToLobby();
    }

    // 로비로 돌아갈 때 1차적 데이터 초기화
    // 전투 종료 후 / 매칭 캔슬
    public async void ReturnToLobby()
    {
        BattleManager.Instance.CancelBattle();

        ResetBattleResultState();

        battlePanel.SetActive(false);
        readyPanel.SetActive(false);
        loadingPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        tapBar.SetActive(true);

        isMatching = false;
        isUsedAttributeScout = false;

        UpdatePlayerCount(0);
        ResetReadyUI();
        ClearOpponentInfoUI();
        SetTitle(true, null);

        matchStartButton.interactable = false;
        try
        {
            await rankingboardManager.LoadRankingBoard();
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"랭킹 갱신 실패: {exception.Message}");
        }

        try
        {
            await rankingboardManager.LoadRemainingCount();
            matchStartButton.interactable = true;
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"잔여 횟수 갱신 실패: {exception.Message}");

            // 잔여 횟수 확인하지 못한 경우 매칭 막기
            matchStartButton.interactable = false;

            // 정보를 불러오지 못했다는 UI 안내 출력
            rankingboardManager.ShowRemainCountLoadError();
        }


        InitResultImage();
        InitRankingPointText();
        InitCharacterHp();

        UpdateTurnText(0);
    }

    private void PrintResultImage(string result)
    {
        if (result == "WIN")
        {
            winImage.SetActive(true);
        }
        else if(result == "LOSE")
        {
            loseImage.SetActive(true);
        }
        else if (result == "DRAW")
        {
            drawImage.SetActive(true);
        }
        else
        {
            Debug.LogError("[BattleUIManager] PrintResultImage(string result) 매개 변수 명 오류");
        }
    }

    private void SetRankingPointText(BattleResultResponse response)
    {
        currentRankingPointText.text = response.ScoreBefore.ToString();

        int delta = response.ScoreDelta;

        if(delta >= 0)
        {
            operatorText.text = "+";
        }
        else
        {
            operatorText.text = "-";
        }
        resultRankingPointText.text = Mathf.Abs(delta).ToString();

    }

    public void SetMatchStartButtonInteractable(bool interactable)
    {
        matchStartButton.interactable = interactable;
    }
}
