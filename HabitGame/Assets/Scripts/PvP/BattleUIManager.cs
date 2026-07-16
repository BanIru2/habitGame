using ExitGames.Client.Photon;
using Photon;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;    // 모호성 방지
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class BattleUIManager : Singleton<BattleUIManager>
{
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
    // 선택된 아이템 (외부참조 가능 -> 전투 로직에 사용)
    public ConsumableDataSO selectedItem { get; private set; }

    private EquipmentType equipType;
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


    // Battle Panel
    [Header("Battle Panel")]
    [SerializeField]
    private TextMeshProUGUI myName;
    [SerializeField]
    private TextMeshProUGUI oppName;
    [SerializeField]
    private TextMeshProUGUI timerText;
    [SerializeField]
    private Image myHpBar;
    [SerializeField]
    private Image oppHpBar;
    [SerializeField]
    private TextMeshProUGUI myHpText;
    [SerializeField]
    private TextMeshProUGUI oppHpText;
    [SerializeField]
    private Image myCharacterImage;
    [SerializeField]
    private Image oppCharacterImage;
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

    private bool isBattle = false;    // UI 타이머 표시 진행을 위한 플래그
    private float remainTime;
    private int remainTimeForDisplay;
    private double battleEndTime;

    [SerializeField]
    private RankingboardManager rankingboardManager;


    protected override void Awake()
    {
        base.Awake();

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

        equipSelectCloseButton.onClick.AddListener(CloseEquipSelectPanel);

        equipActionButton.onClick.AddListener(OnEquipButtonClicked);
        equipDetailCloseButton.onClick.AddListener(CloseEquipDetailPopup);
    }

    private void Update()
    {
        // 매칭 중 중앙 스피너 돌리기
        if (isMatching)
        {
            loadingSpinner.Rotate(0, 0, -spinSpeed * Time.deltaTime);
        }

        // 타이머 작동
        if (isBattle)
        {
            remainTime = Mathf.Max(0f, (float)(battleEndTime - PhotonNetwork.Time));
            UpdateTimerText();

            if (remainTime <= 0f)
            {
                isBattle = false;
                TimeOver();
            }
        }
    }

    // -----------------------------Lobby Panel-----------------------------------
    // 매칭 시작 버튼 클릭 시 화면 전환
    private void OnClickStartMatchmaking()
    {
        if (!TryResolvePhotonManager()) return;

        if (!photonManager.StartMatchmaking())
        {
            return;
        }

        ClearOpponentInfoUI();
        lobbyPanel.SetActive(false);
        loadingPanel.SetActive(true);
        readyPanel.SetActive(false);
        battlePanel.SetActive(false);
        isMatching = true;
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
    public void ReadyComplete(double endTime)
    {
        readyPanel.SetActive(false);
        battlePanel.SetActive(true);
        InitBattleUI(endTime);
    }

    private void ResetReadyUI()
    {
        isReady = false;
        isMeReadyText.text = "WAITING";
        isMeReadyText.color = Color.black;
        isOppReadyText.text = "WAITING";
        isOppReadyText.color = Color.black;

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
        // 사용 가능 여부 확인 및 횟수 차감
        // 상대방 특성 레벨 체크(받아와야함)  << 이단계에서 이미 상대의 특성 정보를 갖고 있어야 함
        // 텍스트 상대방 특성 레벨에 맞춰 변경
        // 출력 (알파값 활성화)
        foreach(var canvasGroup in oppAttrTextCanvasGroup){
            canvasGroup.alpha = 1f;
        }
    }

    // 사용할 특성 선택 버튼 클릭 시 동작
    private void OnClickAttributeButton(Button clickedButton)
    {
        // 선택된 버튼은 색깔강조, 그 외 버튼은 흰색으로 초기화
        foreach(var btn in attrButtons)
        {
            btn.image.color = (btn == clickedButton) ? new Color(1f, 0.56f, 0f, 0.2f) : Color.white;
        }
        // 선택된 버튼의 속성을 사용
        selectedAttrType = clickedButton.GetComponent<AttributeButtonInfo>().attrType;
    }

    // 사용할 소비 아이템 선택 버튼 클릭 시 동작

    private void OnClickConsumableItemButton(Button clickedButton)
    {
        // 선택된 버튼은 색깔강조, 그 외 버튼은 흰색으로 초기화
        foreach (var btn in consumableItemButtons)
        {
            btn.image.color = (btn == clickedButton) ? new Color(1f, 0.56f, 0f, 0.2f) : Color.white;
        }
        selectedItem = clickedButton.GetComponent<ConsumableButtonInfo>().itemData;
    }

    // 장비 아이템 장착--------------------------------------------
    private void OnClickEquipItemButton(Button clickedButton)
    {
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

    private async void OpenEquipSelectPanel(EquipmentType equipType)
    {
        equipSelectPanel.SetActive(true);
        equipTypeText.text = equipType.ToString();

        // test
        await InventoryManager.Instance.RefreshInventoryAsync();

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
        catch (System.Exception e)
        {
            Debug.LogError($"PvP 장비 변경 실패: {e.Message}");
        }
        finally
        {
            equipActionButton.interactable = true;
            readyButton.interactable = true;
        }
    }

    private void CloseEquipDetailPopup()
    {
        selectedEquipItem = null;
        equipDetailPopup.SetActive(false);
    }
    // -------------------------------------------------------------
    private BattleSetupData CreateBattleSetupData()
    {
        return new BattleSetupData
        {
            selectedAttr = selectedAttrType,
            selectedItem = selectedItem,
        };
    }

    // 준비완료 버튼 클릭 시
    private void OnClickReadyButton()
    {
        if (!TryResolvePhotonManager()) return;
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsEquipmentChangeInProgress)
        {
            Debug.LogWarning("장비 변경 처리 중에는 Ready 할 수 없습니다.");
            return;
        }

        var buttonText = readyButton.GetComponentInChildren<TextMeshProUGUI>();
        if (!isReady)
        {
            isReady = true;

            BattleSetupData setup = CreateBattleSetupData();
            BattleUnit myUnit = CharacterManager.Instance.CreateBattleUnit(setup);
            photonManager.RegisterBattleUnitProperties(myUnit);

            readyButton.image.color = Color.red;
            buttonText.text = "Ready";
            isMeReadyText.text = "READY";
            isMeReadyText.color = Color.green;
        }
        else
        {
            isReady = false;
            readyButton.image.color = Color.blue;
            buttonText.text = "Cancel";
            isMeReadyText.text = "WAITING";
            isMeReadyText.color = Color.black;
        }
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
    public void InitBattleUI(double endTime)
    {
        battleEndTime = endTime;
        isBattle = true;
        remainTime = Mathf.Max(0f, (float)(battleEndTime - PhotonNetwork.Time));
        remainTimeForDisplay = -1;
        InitCharacterHp();
        UpdateTimerText();
        InitResultImage();
        InitRankingPointText();
    }

    // 각 플레이어 이름 정보 받아와서 출력하기
    // 각 플레이어 유닛 정보를 앞전에 미리 받아온다면 private으로 받고 InitBattleUI에서 호출 가능
    public void UpdateTurnText(int turn)
    {
        if(turnCountText != null)
        {
            turnCountText.text = turn.ToString();
        }
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

    private void UpdateTimerText()
    {
        int currentSecond = Mathf.CeilToInt(remainTime);

        if(currentSecond != remainTimeForDisplay)
        {
            timerText.text = $"{currentSecond}";
            remainTimeForDisplay = currentSecond;

            timerText.color = (currentSecond <= 10) ? Color.red : Color.black;
        }
    }

    // ------------------------- Battle Finish ----------------------------
    public void FinishBattle(BattleResultResponse response)
    {
        isBattle = false;

        rankingboardManager.UseRemainCount();

        InitResultImage();
        PrintResultImage(response.Result);
        SetRankingPointText(response);
        resultPanel.SetActive(true);
    }

    private void TimeOver()
    {
        BattleManager.Instance.TreatTimeOver();
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
    public void ReturnToLobby()
    {
        battlePanel.SetActive(false);
        readyPanel.SetActive(false);
        loadingPanel.SetActive(false);
        lobbyPanel.SetActive(true);

        isMatching = false;
        isBattle = false;

        UpdatePlayerCount(0);
        ResetReadyUI();
        ClearOpponentInfoUI();

        InitResultImage();
        InitRankingPointText();
        InitCharacterHp();

        turnCountText.text = "0";
        timerText.text = "60";
        timerText.color = Color.black;

        remainTime = 0f;
        remainTimeForDisplay = -1;
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
}
