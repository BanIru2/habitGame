using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BettleUIManager : MonoBehaviour
{
    [SerializeField]
    private GameObject lobbyPanel;
    [SerializeField]
    private GameObject loadingPanel;
    [SerializeField]
    private GameObject readyPanel;
    [SerializeField]
    private GameObject battlePanel;
    
    [SerializeField]
    private RectTransform loadingSpinner;
    [SerializeField]
    private float spinSpeed;

    [SerializeField]
    private Button matchStartButton;
    [SerializeField]
    private Button matchCancelButton;

    [SerializeField]
    private TextMeshProUGUI currentParticipantsCount;

    private bool isMatching = false;


    private void Awake()
    {
        matchStartButton.onClick.AddListener(OnClickStartMatchmaking);
        matchCancelButton.onClick.AddListener(OnClickCancelMatchmaking);
    }

    private void Update()
    {
        // 매칭 중 중앙 스피너 돌리기
        if (isMatching)
        {
            loadingSpinner.Rotate(0, 0, -spinSpeed * Time.deltaTime);
        }
    }

    // 매칭 시작 버튼 클릭 시 화면 전환
    private void OnClickStartMatchmaking()
    {
        lobbyPanel.SetActive(false);
        loadingPanel.SetActive(true);
        isMatching = true;
    }

    // 매칭 취소 버튼 클릭 시 화면 전환
    private void OnClickCancelMatchmaking()
    {
        loadingPanel.SetActive(false); 
        lobbyPanel.SetActive(true);
        isMatching = false;
    }

    // 로딩 완료 시 화면 전환
    public void LoadingComplete()
    {
        loadingPanel.SetActive(false);
        readyPanel.SetActive(true);
        isMatching = false;
    }

    // 준비 단계 완료 시(양 측 다 준비 완료 or 준비 시간 끝) 화면 전환
    public void ReadyComplete()
    {
        readyPanel.SetActive(false);
        battlePanel.SetActive(true);
    }
}
