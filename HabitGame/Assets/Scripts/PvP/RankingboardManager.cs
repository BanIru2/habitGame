using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Threading.Tasks;

public class RankingboardManager : Singleton<RankingboardManager>
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

    private const int MaxRemainCount = 5;

    private int remainCount;
    private RankingEntryResponse myRanking;

    protected override void Awake()
    {
        base.Awake();
        InitializeRows();
    }

    // mock 데이터를 통한 테스트용 함수 - 필요 시 Start함수로 호출
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
    // PvP 탭을 열 때 호출 필요
    public async Task LoadRankingBoard()
    {
        List<RankingEntryResponse> rankings = await ServiceRegistry.Instance.Ranking.GetRankingsAsync();
        ShowRankingBoard(rankings);
    }

    public async Task LoadRemainingCount()
    {
        await BattleBackendManager.Instance.GetRemainCount();
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

    // 남은 횟수 0보다 큰지 알려주기
    public bool IsCanMatchPvP()
    {
        return remainCount > 0;
    }

    // 남은 횟수 응답 결과 적용
    public void ApplyRemainCount(DailyPvpLimitResponse response)
    {
        remainCount = response.RemainingCount;
        remainCountText.text = $"{remainCount.ToString()}   /   {MaxRemainCount}";
    }

    public void ShowRemainCountLoadError()
    {
        remainCountText.text = "불러오기 실패";
    }
}
