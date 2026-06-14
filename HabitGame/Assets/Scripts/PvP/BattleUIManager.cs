using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Photon;
using Photon.Pun;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;    // 모호성 방지

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
    // 가장 최근에 착용했던 장비 아이템 데이터 (DB에서 불러와야 함)
    // Ready 차례가 되면 바로 출력될 수 있도록 순서를 정해야 함
    private Dictionary<EquipmentType, EquipmentDataSO> lastSelectedEquipData = new Dictionary<EquipmentType, EquipmentDataSO>();
    // 이번에 착용할 선택된 장비 아이템 데이터
    // Ready 차례가 되면 lastSelectedEquipData를 복사해 넣기
    public Dictionary<EquipmentType, EquipmentDataSO> selectedEquipData;
    private EquipmentType equipType;
    private bool isReady = false;

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
    private Image myCharacterImage;
    [SerializeField]
    private Image oppCharacterImage;

    private bool isBattle = false;
    private float remainTime;
    private int remainTimeForDisplay;



    protected override void Awake()
    {
        base.Awake();

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
    }

    private void Update()
    {
        // 매칭 중 중앙 스피너 돌리기
        if (isMatching)
        {
            loadingSpinner.Rotate(0, 0, -spinSpeed * Time.deltaTime);
        }
        if (isBattle)
        {
            if(remainTime > 0)
            {
                remainTime -= Time.deltaTime;
                UpdateTimerText();
            }
            else
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
        if (photonManager == null)
        {
            photonManager = FindObjectOfType<PhotonManager>();
        }

        lobbyPanel.SetActive(false);
        loadingPanel.SetActive(true);
        isMatching = true;

        photonManager.StartMatchmaking();
    }
    // ------------------------------Loading Pannel---------------------------------
    // 매칭 취소 버튼 클릭 시 화면 전환
    private void OnClickCancelMatchmaking()
    {
        if (photonManager == null)
        {
            photonManager = FindObjectOfType<PhotonManager>();
        }

        photonManager.CancelMatchmaking();
    }

    public void CancelMatchingComplete()
    {
        loadingPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        isMatching = false;

        UpdatePlayerCount(0);
        ResetReadyUI();
    }

    public void BackToMatchingAfterOpponentLeft()
    {
        readyPanel.SetActive(false);
        battlePanel.SetActive(false);
        loadingPanel.SetActive(true);
        lobbyPanel.SetActive(false);

        isMatching = true;
        isReady = false;

        UpdatePlayerCount(PhotonNetwork.CurrentRoom.PlayerCount);
    }

    // 로딩 완료 시 화면 전환
    public void LoadingComplete()
    {
        loadingPanel.SetActive(false);
        readyPanel.SetActive(true);
        isMatching = false;
        selectedEquipData = new Dictionary<EquipmentType, EquipmentDataSO>(lastSelectedEquipData);
        ResetReadyUI();
    }

    public void UpdatePlayerCount(int current)
    {
        currentParticipantsCount.text = current.ToString();
    }

    //-------------------------------Ready Panel-----------------------------------
    // 준비 단계 완료 시(양 측 다 준비 완료 or 준비 시간 끝) 화면 전환
    public void ReadyComplete()
    {
        readyPanel.SetActive(false);
        battlePanel.SetActive(true);
    }

    private void ResetReadyUI()
    {
        isReady = false;
        isMeReadyText.text = "WAITING";
        isMeReadyText.color = Color.black;
        isOppReadyText.text = "WAITING";
        isOppReadyText.color = Color.black;
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

    // InventoryManager(가명)에서 현재 보유중인 장비 아이템 정보를 불러올 수 있도록 하는 기능 필요
    // InventoryManager 작성 후 기능 구현 예정

    // DB에서 불러온 데이터로 가장 최근에 착용했던 장비 아이템 데이터를 받아와야 함
    // 최근전투뿐만 아니라 전투 외 인벤토리에서 착용한 경우 이부분까지 최신화 된 데이터로

    // 사용할 장비 아이템 선택 버튼 클릭 시 동작

    private void OnClickEquipItemButton(Button clickedButton)
    {
        // 클릭한 버튼의 장비 부위 정보 저장
        equipType = clickedButton.GetComponent<EquipButtonInfo>().type;
        // 보유중인 아이템을 부위에 따라 버튼으로 만들어 보여주고 선택 시 교체할 수 있도록 하는 기능 필요

    }

    // 준비완료 버튼 클릭 시
    // 실제 준비 완료 데이터를 상대방에게 넘겨줘야할 수도?
    private void OnClickReadyButton()
    {
        var buttonText = readyButton.GetComponentInChildren<TextMeshProUGUI>();
        if (!isReady)
        {
            isReady = true;
            readyButton.image.color = Color.red;
            buttonText.text = "준비 완료";
            isMeReadyText.text = "READY";
            isMeReadyText.color = Color.green;
        }
        else
        {
            isReady = false;
            readyButton.image.color = Color.blue;
            buttonText.text = "취소";
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

    // 상대방의 정보를 받아 UI 텍스트에 출력
    public void SetOppoentInfoUI(string name, int fire, int water, int grass, int aurora)
    {
        oppNameText.text = name;
        oppName.text = name;

        oppAttrText[0].text = $"Lv.{fire}";
        oppAttrText[1].text = $"Lv.{water}";
        oppAttrText[2].text = $"Lv.{grass}";
        oppAttrText[3].text = $"Lv.{aurora}";
    }

    //------------------------------Battle Panel-------------------------------
    // UI 초기화에 필요한 기능 추가할 것
    // 전투 시작 전 호출되어야 함
    public void InitBattleUI()
    {
        isBattle = true;
        remainTime = 60f;
        remainTimeForDisplay = -1;
        InitCharacterHp();
    }

    // 각 플레이어 이름 정보 받아와서 출력하기
    // 각 플레이어 유닛 정보를 앞전에 미리 받아온다면 private으로 받고 InitBattleUI에서 호출 가능
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

    // 체력바 업데이트
    public void UpdateCharacterHpBar(bool isPlayer, float currentHp, float maxHp)
    {
        float ratio = Mathf.Clamp01(currentHp / maxHp);

        if (isPlayer)
        {
            myHpBar.fillAmount = ratio;
        }
        else
        {
            oppHpBar.fillAmount = ratio;
        }
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

    public void FinishBattle()
    {
        isBattle = false;
    }

    private void TimeOver()
    {
        BattleManager.Instance.TreatTimeOver();
    }
}
