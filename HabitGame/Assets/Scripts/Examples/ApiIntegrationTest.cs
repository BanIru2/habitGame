using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unity Play Mode에서 Backend Service와 DTO 역직렬화를 수동 검증하기 위한 개발용 도구입니다.
/// API는 ContextMenu를 선택했을 때만 호출됩니다.
/// </summary>
[DisallowMultipleComponent]
public class ApiIntegrationTest : MonoBehaviour
{
    private const long TestUserId = 1;

    [Header("Auth Login Test")]
    [SerializeField] private string loginEmail = string.Empty;
    [SerializeField] private string loginPassword = string.Empty;

    [ContextMenu("Test/Character")]
    private async void TestCharacter()
    {
        const string apiName = "Character";
        CharacterResponse response = null;

        if (!CanRunInPlayMode(apiName))
            return;

        try
        {
            ApiClient.Instance.SetCurrentUserId(TestUserId);
            response = await GetRegistry().Character.GetMyCharacterAsync();

            if (response == null)
            {
                LogNullResponse(apiName);
                return;
            }

            Debug.Log(
                $"[API TEST][{apiName}] SUCCESS\n" +
                $"userId={response.UserId}\n" +
                $"gold={response.Gold}\n" +
                $"hp={response.Hp}\n" +
                $"atk={response.Atk}\n" +
                $"def={response.Def}\n" +
                $"spd={response.Spd}\n" +
                $"critRate={response.Crit}",
                this
            );
        }
        catch (Exception exception)
        {
            LogFailure(apiName, exception, response == null);
        }
    }

    [ContextMenu("Test/Shop Items")]
    private async void TestShopItems()
    {
        const string apiName = "Shop Items";
        List<ItemResponse> response = null;

        if (!CanRunInPlayMode(apiName))
            return;

        try
        {
            response = await GetRegistry().Shop.GetItemsAsync();

            if (response == null)
            {
                LogNullResponse(apiName);
                return;
            }

            string firstItem = response.Count > 0
                ? $"id={response[0].Id}, name={response[0].Name}, price={response[0].Price}, type={response[0].ItemType}"
                : "<none>";

            Debug.Log(
                $"[API TEST][{apiName}] SUCCESS\n" +
                $"count={response.Count}\n" +
                $"firstItem={firstItem}",
                this
            );
        }
        catch (Exception exception)
        {
            LogFailure(apiName, exception, response == null);
        }
    }

    [ContextMenu("Test/Ranking")]
    private async void TestRanking()
    {
        const string apiName = "Ranking";
        List<RankingEntryResponse> response = null;

        if (!CanRunInPlayMode(apiName))
            return;

        try
        {
            response = await GetRegistry().Ranking.GetRankingsAsync();

            if (response == null)
            {
                LogNullResponse(apiName);
                return;
            }

            RankingEntryResponse myRanking = response.Find(entry => entry.UserId == TestUserId);
            string myRank = myRanking != null ? myRanking.Rank.ToString() : "<not found>";
            string myScore = myRanking != null ? myRanking.Score.ToString() : "<not found>";

            Debug.Log(
                $"[API TEST][{apiName}] SUCCESS\n" +
                $"count={response.Count}\n" +
                $"testUserId={TestUserId}\n" +
                $"myRank={myRank}\n" +
                $"myScore={myScore}",
                this
            );
        }
        catch (Exception exception)
        {
            LogFailure(apiName, exception, response == null);
        }
    }

    [ContextMenu("Test/Spending Overview")]
    private async void TestSpendingOverview()
    {
        const string apiName = "Spending Overview";
        SpendingOverviewResponse response = null;

        if (!CanRunInPlayMode(apiName))
            return;

        try
        {
            response = await GetRegistry().Spending.GetOverviewAsync(TestUserId);

            if (response == null)
            {
                LogNullResponse(apiName);
                return;
            }

            int goalCount = response.Goals != null ? response.Goals.Count : 0;

            Debug.Log(
                $"[API TEST][{apiName}] SUCCESS\n" +
                $"budgetId={response.BudgetId}\n" +
                $"budgetAmount={response.BudgetAmount}\n" +
                $"currentSpent={response.CurrentSpent}\n" +
                $"period={response.Period}\n" +
                $"usageRate={response.UsageRate}\n" +
                $"expectedGold={response.ExpectedGold}\n" +
                $"goalCount={goalCount}",
                this
            );
        }
        catch (Exception exception)
        {
            LogFailure(apiName, exception, response == null);
        }
    }

    [ContextMenu("Test/Auth Login")]
    private async void TestAuthLogin()
    {
        const string apiName = "Auth Login";
        LoginResponse response = null;

        if (!CanRunInPlayMode(apiName))
            return;

        if (string.IsNullOrWhiteSpace(loginEmail) || string.IsNullOrWhiteSpace(loginPassword))
        {
            Debug.LogWarning(
                $"[API TEST][{apiName}] SKIPPED\n" +
                "Email and password must be entered in the Inspector. No request was sent.",
                this
            );
            return;
        }

        try
        {
            var request = new LoginRequest
            {
                Email = loginEmail.Trim(),
                Password = loginPassword
            };

            response = await GetRegistry().Auth.LoginAsync(request);

            if (response == null)
            {
                LogNullResponse(apiName);
                return;
            }

            Debug.Log(
                $"[API TEST][{apiName}] SUCCESS\n" +
                $"userId={response.UserId}\n" +
                $"email={response.Email}\n" +
                $"nickname={response.Nickname}\n" +
                $"message={response.Message}",
                this
            );
        }
        catch (Exception exception)
        {
            LogFailure(apiName, exception, response == null);
        }
    }

    private static ServiceRegistry GetRegistry()
    {
        ServiceRegistry registry = ServiceRegistry.Instance;
        if (registry == null)
            throw new InvalidOperationException("ServiceRegistry is unavailable.");

        return registry;
    }

    private bool CanRunInPlayMode(string apiName)
    {
        if (Application.isPlaying)
            return true;

        Debug.LogWarning(
            $"[API TEST][{apiName}] SKIPPED\n" +
            "Enter Play Mode before running this ContextMenu test. No request was sent.",
            this
        );
        return false;
    }

    private void LogNullResponse(string apiName)
    {
        Debug.LogError(
            $"[API TEST][{apiName}] FAILURE\n" +
            "exception=Response was null.\n" +
            "responseNull=true",
            this
        );
    }

    private void LogFailure(string apiName, Exception exception, bool responseIsNull)
    {
        Debug.LogError(
            $"[API TEST][{apiName}] FAILURE\n" +
            $"exception={exception.Message}\n" +
            $"responseNull={responseIsNull.ToString().ToLowerInvariant()}",
            this
        );
    }
}
