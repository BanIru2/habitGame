using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RankingboardManager : MonoBehaviour
{
    [SerializeField]
    private RankingRowUI rowPrefab;
    [SerializeField]
    private Transform contentParent;
    // 최초로 생성해 둔 Row 프리팹 인스턴스를 저장해두고 사용하기 위한 리스트
    private readonly List<RankingRowUI> cachedRows = new List<RankingRowUI>();
    // 표시할 Row 개수
    private int rowCount = 20;

    [SerializeField]
    private TextMeshProUGUI myRankingText;
    [SerializeField]
    private TextMeshProUGUI myScoreText;
    [SerializeField]
    private TextMeshProUGUI remainCountText;

    [SerializeField]
    private ServiceRegistry serviceRegistry;

    private const int MaxRemainCount = 5;
    // 마지막으로 전투 횟수 초기화 한 날짜 저장 키
    private const string LastResetDateKey = "Ranking_LastResetDate";
    // 남은 전투 횟수 저장 키
    private const string RemainCountKey = "Ranking_RemainCount";

    private int remainCount;
    private RankingEntryResponse myRanking;

    private void Awake()
    {
        InitializeRows();
    }

    private void Start()
    {
        LoadRemainCount();
        CheckDailyReset();

        StartCoroutine(CheckDailyResetRoutine());

        // DB 연결 오류로 임시 주석처리. 연결 후 주석해제 요망
        //LoadRankingBoard();

/*        // 테스트 호출
        LoadMockRankingBoard();*/
    }

    // mock 데이터를 통한 테스트용 함수
/*    private void LoadMockRankingBoard()
    {
        List<RankingEntryResponse> list = new List<RankingEntryResponse>();
        for(int i = 0; i < 20; i++)
        {
            RankingEntryResponse data = new RankingEntryResponse
            {
                RankingId = i,
                UserId = i + 1,
                Name = i + " name",
                Score = (i + 1) * 1000,
                Wins = i + 100,
                Losses = i + 10,
                Rank = i + 1
            };

            list.Add(data);
        }
        RankingListResponse response = new RankingListResponse
        {
            Season = 1,
            Rankings = list,
            MyRanking = list[10],
            UpdatedAt = ""
        };

        ShowRankingBoard(response);
    }*/

    //----------------------------- 랭킹 보드 생성 -----------------------------------
    // 최초로 필요한 개수의 Row 생성
    private void InitializeRows()
    {
        if (cachedRows.Count > 0) return;

        for (int i = 0; i < rowCount; i++)
        {
            RankingRowUI row = Instantiate(rowPrefab, contentParent);
            row.ClearData();
            cachedRows.Add(row);
        }
    }

    // 랭킹 보드 데이터 갱신
    private void RefreshRows(List<RankingEntryResponse> rankings)
    {
        for (int i = 0; i < cachedRows.Count; i++)
        {
            if (i < rankings.Count)
            {
                cachedRows[i].SetData(rankings[i]);
            }
            else
            {
                cachedRows[i].ClearData();
            }
        }
    }

    // 받아온 랭킹 정보를 정령 후 UI에 반영
    private void ShowRankingBoard(List<RankingEntryResponse> rankings)
    {
        rankings ??= new List<RankingEntryResponse>();
        myRanking = rankings.Find(
            ranking => ranking.UserId == ApiClient.Instance.CurrentUserId
        );
        rankings.Sort((a, b) => a.Rank.CompareTo(b.Rank));

        RefreshRows(rankings);
        RefreshMyRankingData();
    }

    // 랭킹 보드 정보를 가져오기 위한 외부 호출용 함수
    public async void LoadRankingBoard()
    {
        try
        {
            List<RankingEntryResponse> rankings = await serviceRegistry.Ranking.GetRankingsAsync();
            ShowRankingBoard(rankings);
        }
        catch (Exception e)
        {
            Debug.LogError($"[RankingboardManager] Failed to load ranking board: {e.Message}");
        }
    }

    // -------------------------- 내 랭킹 박스 ---------------------------
    private void RefreshMyRankingData()
    {
        if (myRanking == null)
        {
            myRankingText.text = "-";
            myScoreText.text = "0";
            return;
        }

        myRankingText.text = myRanking.Rank.ToString();
        myScoreText.text = myRanking.Score.ToString();
    }

    // 실제 남은 횟수 초기화
    private void ResetCount()
    {
        remainCount = MaxRemainCount;
        SaveRemainCount();
        RefreshRemainCountText();
    }

    // 전투 종료 시 호출
    // 전투 횟수 사용하여 차감
    public void UseRemainCount()
    {
        if (remainCount <= 0)
        {
            remainCount = 0;
            SaveRemainCount();
            RefreshRemainCountText();
            return;
        }

        remainCount--;
        SaveRemainCount();
        RefreshRemainCountText();
    }


    // 남은 횟수 UI 갱신
    private void RefreshRemainCountText()
    {
        remainCountText.text = remainCount.ToString();
    }

    // 현재 남은 횟수 저장하여 앱을 껐다 켜도 유지되도록
    private void SaveRemainCount()
    {
        PlayerPrefs.SetInt(RemainCountKey, remainCount);
        PlayerPrefs.Save();
    }

    // 앱을 켰을 때 저장된 남은 횟수 불러오기
    private void LoadRemainCount()
    {
        remainCount = PlayerPrefs.GetInt(RemainCountKey, MaxRemainCount);
        RefreshRemainCountText();
    }

    // 마지막 초기화 날짜와 현재 날짜를 비교하여 달라졌으면 초기화 후 초기화 여부 저장
    private void CheckDailyReset()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        string lastResetDate = PlayerPrefs.GetString(LastResetDateKey, "");

        if (lastResetDate != today)
        {
            ResetCount();
            PlayerPrefs.SetString(LastResetDateKey, today);
            PlayerPrefs.Save();
        }
    }

    // 앱을 킨 상태로 자정을 넘긴 경우 날짜 바뀜을 감지하기 위한 코루틴
    private IEnumerator CheckDailyResetRoutine()
    {
        while (true)
        {
            CheckDailyReset();
            yield return new WaitForSeconds(60f);
        }
    }

}
