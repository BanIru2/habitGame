using System.Collections.Generic;
using System.Threading.Tasks;
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

    [Header("보상 미리보기")]
    [SerializeField]
    private GameObject previewPopup;
    [SerializeField]
    private Image previewTitleImage;
    [SerializeField]
    private DimCloser previewDimCloser;

    [Header("프리팹 & 데이터")]
    [SerializeField]
    private TitleSlotUI slotPrefab;      // TitleSlot.prefab
    [SerializeField]
    private List<TitleDataSO> registeredTitles; // 전체 마스터 칭호 카탈로그

    [Header("백엔드 연결")]
    [SerializeField]
    private TitleBackendManager backendManager;

    [Header("로컬 테스트")]
    [SerializeField]
    private bool useLocalTestData = false;

    [Header("장착 상태")]
    [SerializeField]
    private string equippedTitleId = null; // 현재 장착 중인 칭호 ID (미장착 시 null)
    public string EquippedTitleId => equippedTitleId;

    // 백엔드로부터 동기화된 유저 보유 칭호 ID 집합
    private readonly HashSet<string> ownedTitleIds = new HashSet<string>();

    // 슬롯 풀
    private readonly List<TitleSlotUI> slotPool = new List<TitleSlotUI>();

    // 장착 / 해제 요청이 끝날 때까지 추가 클릭으로 인한 중복 요청 방지
    private bool isEquippingTitle;

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

        if (previewDimCloser != null)
        {
            previewDimCloser.AddListener(ClosePreview);
        }
    }

    private async void Start()
    {
        if (useLocalTestData)
        {
            LoadLocalTestData();
        }
        else
        {
            await FetchTitlesFromServer();
        }

        // 로그인 시 이미 장착 중인 칭호가 있다면 캐릭터 탭에 반영
        UpdateCharacterEquippedTitle();
    }

    // 로컬 테스트용 더미 데이터 세팅
    private void LoadLocalTestData()
    {
        ownedTitleIds.Clear();
        if (registeredTitles != null)
        {
            foreach (var title in registeredTitles)
            {
                if (title != null && !string.IsNullOrEmpty(title.titleId))
                {
                    ownedTitleIds.Add(title.titleId);
                }
            }
        }
    }

    public async void OpenPopup()
    {
        if (titlePopup != null)
        {
            titlePopup.SetActive(true);
        }

        if (useLocalTestData)
        {
            RefreshTitleUI();
            return;
        }

        await FetchTitlesFromServer();
        RefreshTitleUI();
    }

    public void ClosePopup()
    {
        if (titlePopup != null)
        {
            titlePopup.SetActive(false);
        }
    }

    // 서버로부터 내 칭호 목록 및 장착 칭호 조회
    public async Task FetchTitlesFromServer()
    {
        if (backendManager == null)
        {
            Debug.LogError("[TitleManager] TitleBackendManager가 연결되지 않았습니다.");
            return;
        }

        try
        {
            UserTitlesResponse response = await backendManager.FetchTitlesAsync();
            if (response != null)
            {
                ownedTitleIds.Clear();
                if (response.OwnedTitleIds != null)
                {
                    foreach (var id in response.OwnedTitleIds)
                    {
                        if (!string.IsNullOrEmpty(id))
                            ownedTitleIds.Add(id);
                    }
                }

                equippedTitleId = response.EquippedTitleId;
                UpdateCharacterEquippedTitle();
            }
        }
        catch (ApiException e)
        {
            Debug.LogError($"[TitleManager] 칭호 목록 조회 실패 ({e.StatusCode}): {e.Message}");
            if (ErrorPopupManager.Instance != null)
            {
                ErrorPopupManager.Instance.ShowApiError(e);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TitleManager] 칭호 시스템 오류: {e.Message}");
            if (ErrorPopupManager.Instance != null)
            {
                ErrorPopupManager.Instance.ShowSystemError();
            }
        }
    }

    public void RefreshTitleUI()
    {
        if (registeredTitles == null || registeredTitles.Count == 0)
        {
            HideUnusedSlots(0);
            return;
        }

        // 전체 마스터 칭호 중 유저가 실제로 보유한 칭호만 필터링
        List<TitleDataSO> ownedList = registeredTitles.FindAll(
            t => t != null && ownedTitleIds.Contains(t.titleId)
        );

        for (int i = 0; i < ownedList.Count; i++)
        {
            TitleDataSO titleSO = ownedList[i];
            TitleSlotUI slot = GetSlot(i);
            slot.gameObject.SetActive(true);

            // 현재 장착 중인 칭호인지 확인
            bool isEquipped = (titleSO != null && !string.IsNullOrEmpty(titleSO.titleId) && titleSO.titleId == equippedTitleId);

            // 클릭 시 즉시 장착/해제 토글
            slot.LoadData(titleSO, isEquipped, OnClickTitleSlot);
        }

        HideUnusedSlots(ownedList.Count);
    }

    // 슬롯 클릭 콜백 (장착 / 해제 토글)
    private async void OnClickTitleSlot(TitleDataSO data)
    {
        if (data == null || string.IsNullOrEmpty(data.titleId)) return;
        if (isEquippingTitle) return;

        // 이미 장착 중인 것을 누르면 장착 해제(null), 아니면 새로 장착
        string targetTitleId = (equippedTitleId == data.titleId) ? null : data.titleId;

        // 로컬 테스트 분기
        if (useLocalTestData)
        {
            equippedTitleId = targetTitleId;
            Debug.Log($"[TitleManager] (로컬) 칭호 변경: '{equippedTitleId ?? "미장착"}'");
            RefreshTitleUI();
            UpdateCharacterEquippedTitle();
            return;
        }

        if (backendManager == null)
        {
            Debug.LogError("[TitleManager] TitleBackendManager가 연결되지 않았습니다.");
            return;
        }

        try
        {
            isEquippingTitle = true;
            EquipTitleResponse response = await backendManager.EquipTitleAsync(targetTitleId);
            equippedTitleId = response != null ? response.EquippedTitleId : targetTitleId;
            Debug.Log($"[TitleManager] (서버) 칭호 장착/해제 완료: '{equippedTitleId ?? "미장착"}'");

            RefreshTitleUI();
            UpdateCharacterEquippedTitle();
        }
        catch (ApiException e)
        {
            Debug.LogError($"[TitleManager] 칭호 장착 실패 ({e.StatusCode}): {e.Message}");
            if (ErrorPopupManager.Instance != null)
            {
                ErrorPopupManager.Instance.ShowApiError(e);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TitleManager] 칭호 장착 시스템 오류: {e.Message}");
            if (ErrorPopupManager.Instance != null)
            {
                ErrorPopupManager.Instance.ShowSystemError();
            }
        }
        finally
        {
            isEquippingTitle = false;
        }
    }

    private void UpdateCharacterEquippedTitle()
    {
        Sprite targetSprite = GetEquippedTitleSprite();
        if (CharacterUIManager.Instance != null)
        {
            CharacterUIManager.Instance.SetEquippedTitle(targetSprite);
        }
    }

    // --------------------------------------- 풀링 ----------------------------------------
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
    // -------------------------------------------------------------------------------------
    // ------------------------------------- 조회 ------------------------------------------
    // id 받아 SO 조회
    private TitleDataSO GetTitleData(string titleId)
    {
        if (string.IsNullOrEmpty(titleId) || registeredTitles == null) return null;
        return registeredTitles.Find(t => t != null && t.titleId == titleId);
    }

    // id 받아 Sprite 조회
    public Sprite GetTitleSprite(string titleId)
    {
        TitleDataSO data = GetTitleData(titleId);
        return data != null ? data.displaySprite : null;
    }

    public Sprite GetEquippedTitleSprite()
    {
        return GetTitleSprite(equippedTitleId);
    }

    // ---------------------------------- 미리 보기 ---------------------------------------
    public void ShowPreview(Sprite titleSprite)
    {
        if (titleSprite == null) return;
        if (previewTitleImage != null)
        {
            previewTitleImage.sprite = titleSprite;
        }
        if (previewPopup != null)
        {
            previewPopup.SetActive(true);
        }
    }

    public void ClosePreview()
    {
        if (previewPopup != null)
        {
            previewPopup.SetActive(false);
        }
    }
}
