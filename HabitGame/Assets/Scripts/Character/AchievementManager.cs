using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class AchievementManager : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField]
    private Button achievementButton;
    [SerializeField]
    private Button closeButton;
    [SerializeField]
    private GameObject achievementPopup;
    [SerializeField]
    private Transform slotParent;

    [Header("프리팹 & 데이터")]
    [SerializeField]
    private AchievementSlotUI slotPrefab;
    [SerializeField]
    private AchievementDataSO[] registeredAchievements;

    [Header("백엔드 연결")]
    [SerializeField]
    private AchievementBackendManager backendManager;

    [Header("로컬 테스트")]
    [SerializeField]
    private bool useLocalTestData;

    // 슬롯 풀
    private readonly List<AchievementSlotUI> slotPool = new List<AchievementSlotUI>();
    // 실제로 출력 및 처리해야 할 업적 데이터
    private readonly List<AchievementViewData> achievementViewDataList = new List<AchievementViewData>();
    // 보상 수령 중복 요청 방지 플래그
    private bool isClaimingReward = false;

    private void Awake()
    {
        if (useLocalTestData)
        {
            LoadLocalTestData();
        }

        achievementButton.onClick.AddListener(OpenPopup);
        closeButton.onClick.AddListener(ClosePopup);
    }

    // ---------------------------------- 로컬 테스트 데이터 세팅 --------------------------------
    private void LoadLocalTestData()
    {
        List<AchievementResponse> dummyResponses = new List<AchievementResponse>
        {
            // 진행 중인 상태
            new AchievementResponse
            {
                AchievementId = "ach_1",
                CurrentCount = 5,
                IsClaimed = false
            },
            // 달성 완료되어 보상 수령 가능한 상태
            new AchievementResponse
            {
                AchievementId = "ach_2",
                CurrentCount = 7,
                IsClaimed = false
            }
        };

        // 데이터 세팅
        SetAchievementResponses(dummyResponses);
    }
    // ------------------------------------------------------------------------------------

    public async void OpenPopup()
    {
        achievementPopup.SetActive(true);

        // 로컬 테스트 분기
        if (useLocalTestData)
        {
            LoadLocalTestData();
            RefreshAchievementUI();
            return;
        }

        await FetchAchievementsFromServer();
    }

    public void ClosePopup()
    {
        achievementPopup.SetActive(false);
    }

    // 업적 데이터 서버 요청
    private async Task FetchAchievementsFromServer()
    {
        if (backendManager == null)
        {
            Debug.LogError("[AchievementManager] BackendManager가 연결되지 않았습니다.");
            return;
        }
        try
        {
            List<AchievementResponse> responses = await backendManager.FetchAchievementsAsync();
            SetAchievementResponses(responses);
        }
        catch (ApiException e)
        {
            Debug.LogError($"[AchievementManager] 업적 목록 조회 실패 ({e.StatusCode}): {e.Message}");
            ErrorPopupManager.Instance.ShowApiError(e);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementManager] 업적 시스템 오류: {e.Message}");
            ErrorPopupManager.Instance.ShowSystemError();
        }
    }

    // ------------------------------- 목록 렌더링 -----------------------------
    // 데이터 진입점
    public void SetAchievementResponses(IReadOnlyList<AchievementResponse> responses)
    {
        BuildAchievementViewData(responses);
        if (achievementPopup.activeSelf)
        {
            RefreshAchievementUI();
        }
    }

    private void BuildAchievementViewData(IReadOnlyList<AchievementResponse> responses)
    {
        achievementViewDataList.Clear();

        if (registeredAchievements == null || responses == null)
        {
            return;
        }

        // SO와 Response를 연결하기 위한 임시 조회표
        var responseById = new Dictionary<string, AchievementResponse>(responses.Count);

        for (int i = 0; i < responses.Count; i++)
        {
            AchievementResponse response = responses[i];

            if (response == null || string.IsNullOrEmpty(response.AchievementId))
            {
                continue;
            }

            responseById[response.AchievementId] = response;
        }

        for (int i = 0; i < registeredAchievements.Length; i++)
        {
            AchievementDataSO achievement = registeredAchievements[i];

            if (achievement == null || string.IsNullOrEmpty(achievement.achievementId))
            {
                continue;
            }

            if (!responseById.TryGetValue(achievement.achievementId, out AchievementResponse response))
            {
                Debug.LogWarning($"[AchievementManager] 진행 데이터가 없습니다: " + achievement.achievementId);
                continue;
            }

            achievementViewDataList.Add(new AchievementViewData
                {
                    AchievementSO = achievement,
                    Response = response
                }
            );
        }
    }

    public void RefreshAchievementUI()
    {
        if (achievementViewDataList.Count == 0)
        {
            HideUnusedSlots(0);
            return;
        }

        for (int i = 0; i < achievementViewDataList.Count; i++)
        {
            AchievementViewData viewData = achievementViewDataList[i];

            AchievementSlotUI slot = GetSlot(i);

            slot.gameObject.SetActive(true);

            slot.LoadData(viewData, OnClaimReward, OnCheckReward);
        }

        HideUnusedSlots(achievementViewDataList.Count);
    }

    private AchievementSlotUI GetSlot(int index)
    {
        if (index < slotPool.Count)
        {
            return slotPool[index];
        }
        // 풀에 없으면 새로 생성 후 풀에 추가
        AchievementSlotUI newSlot = Instantiate(slotPrefab, slotParent);
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
    // -----------------------------------------------------------------------------------
    // 보상 수령 콜백 (단일 타겟팅 갱신)
    private async void OnClaimReward(AchievementViewData data)
    {
        if (data == null || data.AchievementSO == null) return;
        if (isClaimingReward) return; // 연타 방지
        // 로컬 테스트 분기
        if (useLocalTestData)
        {
            if (data.Response != null) data.Response.IsClaimed = true;
            RefreshAchievementUI();
            return;
        }
        // 서버 요청
        try
        {
            isClaimingReward = true;
            string achId = data.AchievementSO.achievementId;
            AchievementRewardClaimResponse result = await backendManager.ClaimRewardAsync(achId);
            if (result != null)
            {
                // 대상 업적 상태 갱신
                if (data.Response != null)
                {
                    data.Response.IsClaimed = true;
                }
                // UI 갱신
                RefreshAchievementUI();
                // 칭호 획득 알림
                Debug.Log($"[보상 수령 완료] 칭호 ID: {result.RewardTitleId} 획득!");
            }
        }
        catch (ApiException e)
        {
            Debug.LogError($"[AchievementManager] 보상 수령 실패 ({e.StatusCode}): {e.Message}");
            ErrorPopupManager.Instance.ShowApiError(e);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementManager] 보상 수령 시스템 오류: {e.Message}");
            ErrorPopupManager.Instance.ShowSystemError();
        }
        finally
        {
            isClaimingReward = false; // 플래그 해제
        }
    }

    private void OnCheckReward(AchievementViewData data)
    {
        
    }
}
