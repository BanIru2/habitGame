using TMPro;
using UnityEngine;

public class HabitDetailManager : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private GameObject lifePanel;
    [SerializeField] private GameObject topTab;

    [Header("Detail UI")]
    [SerializeField] private TextMeshProUGUI habitNameText;
    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private TextMeshProUGUI recordTypeText;
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private TextMeshProUGUI periodText;
    [SerializeField] private TextMeshProUGUI rewardText;

    public void OpenDetail(HabitGoalResponse data)
    {
        if (data == null)
        {
            Debug.LogWarning("HabitDetail 데이터가 없습니다.");
            return;
        }

        habitNameText.text =
            data.GoalName;

        categoryText.text =
            $"Category : {data.Category}";

        recordTypeText.text =
            $"Record Type : {data.RecordType}";

        if (data.RecordType == "check")
        {
            targetText.text =
                "Target : Complete";
        }
        else
        {
            targetText.text =
                $"Target : {data.TargetAmount} {data.Unit}";
        }

        periodText.text =
            $"Repeat : {data.Period}";

        // 현재 HabitGoalResponse에 보상 필드가 없음
        rewardText.text =
            "Expected Reward : -";

        detailPanel.SetActive(true);
        lifePanel.SetActive(false);
        topTab.SetActive(false);
    }

    public void CloseDetail()
    {
        detailPanel.SetActive(false);
        lifePanel.SetActive(true);
        topTab.SetActive(true);
    }
}