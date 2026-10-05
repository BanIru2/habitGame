using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField]
    private Button titleButton;          // TitleButton
    [SerializeField]
    private Button closeButton;          // TitlePopup의 CloseButton
    [SerializeField]
    private GameObject titlePopup;       // TitlePopup 오브젝트
    [SerializeField]
    private Transform slotParent;        // 슬롯을 소환할 Content transform

    [Header("프리팹 & 데이터")]
    [SerializeField]
    private TitleSlotUI slotPrefab;      // TitleSlot.prefab
    [SerializeField]
    private List<TitleDataSO> registeredTitles; // 보유 칭호 목록

    [Header("장착 상태 로컬 테스트용 Id")]
    [SerializeField]
    private string equippedTitleId = ""; // 현재 장착 중인 칭호 ID

    // 슬롯 풀
    private readonly List<TitleSlotUI> slotPool = new List<TitleSlotUI>();

    private void Awake()
    {
        if (titleButton != null)
        {
            titleButton.onClick.AddListener(OpenPopup);
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePopup);
        }
    }

    public void OpenPopup()
    {
        if (titlePopup != null)
        {
            titlePopup.SetActive(true);
        }
        RefreshTitleUI();
    }

    public void ClosePopup()
    {
        if (titlePopup != null)
        {
            titlePopup.SetActive(false);
        }
    }

    public void RefreshTitleUI()
    {
        if (registeredTitles == null || registeredTitles.Count == 0)
        {
            HideUnusedSlots(0);
            return;
        }

        for (int i = 0; i < registeredTitles.Count; i++)
        {
            TitleDataSO titleSO = registeredTitles[i];
            TitleSlotUI slot = GetSlot(i);
            slot.gameObject.SetActive(true);

            // 현재 장착 중인 칭호인지 확인
            bool isEquipped = (titleSO != null && !string.IsNullOrEmpty(titleSO.titleId) && titleSO.titleId == equippedTitleId);

            // 클릭 시 즉시 장착/해제 토글
            slot.LoadData(titleSO, isEquipped, OnClickTitleSlot);
        }

        HideUnusedSlots(registeredTitles.Count);
    }

    // 슬롯 클릭 콜백 (즉시 장착 / 해제 토글)
    private void OnClickTitleSlot(TitleDataSO data)
    {
        if (data == null || string.IsNullOrEmpty(data.titleId)) return;

        if (equippedTitleId == data.titleId)
        {
            // 이미 장착 중인 걸 누르면 장착 해제
            equippedTitleId = "";
            Debug.Log($"[TitleManager] 칭호 장착 해제: {data.titleId}");
        }
        else
        {
            // 새로 장착
            equippedTitleId = data.titleId;
            Debug.Log($"[TitleManager] 칭호 장착 완료: {data.titleId}");
        }

        // 목록 슬롯들의 테두리 하이라이트 즉시 갱신 (선택한 것만 노란색)
        RefreshTitleUI();

        // TODO: CharacterUIManager 등에 실시간 장착 이미지 반영
    }

    private TitleSlotUI GetSlot(int index)
    {
        if (index < slotPool.Count)
        {
            return slotPool[index];
        }

        TitleSlotUI newSlot = Instantiate(slotPrefab, slotParent);
        slotPool.Add(newSlot);
        return newSlot;
    }

    private void HideUnusedSlots(int usedCount)
    {
        for (int i = usedCount; i < slotPool.Count; i++)
        {
            slotPool[i].gameObject.SetActive(false);
        }
    }
}