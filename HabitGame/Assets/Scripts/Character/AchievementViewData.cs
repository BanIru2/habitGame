/// <summary>
/// 업적 슬롯용 뷰 데이터
/// </summary>
public class AchievementViewData
{
    public AchievementDataSO AchievementSO;
    public AchievementResponse Response;

    // 수령 가능 여부
    public bool CanClaim => Response != null && AchievementSO != null
        && (Response.CurrentCount >= AchievementSO.targetCount) && !Response.IsClaimed;

    // 진행도
    public float ProgressRatio => (AchievementSO != null && AchievementSO.targetCount > 0)
        ? UnityEngine.Mathf.Clamp01((float)Response.CurrentCount / AchievementSO.targetCount)
        : 0f;
}
