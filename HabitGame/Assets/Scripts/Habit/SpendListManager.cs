using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpendListManager : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private TMP_InputField goalNameInput;
    [SerializeField] private TMP_InputField goldInput;

    [Header("UI")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject spendItemPrefab;

    [Header("Reward")]
    [SerializeField] private int reward = 300;

    [SerializeField] private SpendCycleManager cycleManager;
    [SerializeField] private GameObject spendPanel;
    [SerializeField] private GameObject spendAddPanel;

    // 아직 서버에 저장되지 않은 특수목표
    private readonly List<CreateSpendingSpecialGoalData>
        pendingSpecialGoals =
            new List<CreateSpendingSpecialGoalData>();

    // 생성한 UI Item을 순서대로 보관
    private readonly List<SpendGoalItem>
        pendingGoalItems =
            new List<SpendGoalItem>();

    public void AddSpendGoal()
    {
        if (goalNameInput == null || goldInput == null)
        {
            Debug.LogWarning(
                "소비 목표 Input이 연결되지 않았습니다."
            );
            return;
        }

        string goalName = goalNameInput.text.Trim();

        if (string.IsNullOrWhiteSpace(goalName))
        {
            Debug.LogWarning(
                "소비 목표 이름을 입력해주세요."
            );
            return;
        }

        if (!int.TryParse(goldInput.text, out int limitAmount))
        {
            Debug.LogWarning(
                "목표 금액은 숫자로 입력해주세요."
            );
            return;
        }

        if (limitAmount <= 0)
        {
            Debug.LogWarning(
                "목표 금액은 0원보다 커야 합니다."
            );
            return;
        }

        if (spendItemPrefab == null || content == null)
        {
            Debug.LogWarning(
                "Spend Item Prefab 또는 Content가 연결되지 않았습니다."
            );
            return;
        }

        bool isWeekly =
            cycleManager != null && cycleManager.isWeekly;

        string cycle =
            isWeekly ? "Weekly" : "Daily";

        CreateSpendingSpecialGoalData specialGoal =
            new CreateSpendingSpecialGoalData
            {
                // SpendingService가 예산의 UserId는 넣어주지만
                // nested DTO의 UserId는 자동 설정하지 않으므로 넣어준다.
                UserId = ApiClient.Instance.CurrentUserId,

                GoalName = goalName,
                LimitAmount = limitAmount,
                RewardGold = reward,

                // 현재 DTO에 유사 필드가 함께 존재하므로
                // 서버 호환을 위해 동일 내용을 함께 전달
                Title = goalName,
                TargetDescription =
                    $"{cycle} spending limit: {limitAmount} KRW",
                BonusGold = reward
            };

        pendingSpecialGoals.Add(specialGoal);

        GameObject newItem =
            Instantiate(spendItemPrefab, content);

        Transform goalNameTransform =
            newItem.transform.Find("GoalName");

        if (goalNameTransform != null)
        {
            TextMeshProUGUI goalNameText =
                goalNameTransform.GetComponent<TextMeshProUGUI>();

            if (goalNameText != null)
                goalNameText.text = goalName;
        }

        Transform descriptionTransform =
            newItem.transform.Find("GoalDescription");

        if (descriptionTransform != null)
        {
            TextMeshProUGUI descriptionText =
                descriptionTransform.GetComponent<TextMeshProUGUI>();

            if (descriptionText != null)
            {
                descriptionText.text =
                    $"Limit : {limitAmount} KRW ({cycle})";
            }
        }

        Transform rewardTransform =
            newItem.transform.Find("RewardText");

        if (rewardTransform != null)
        {
            TextMeshProUGUI rewardText =
                rewardTransform.GetComponent<TextMeshProUGUI>();

            if (rewardText != null)
                rewardText.text = "+" + reward;
        }

        SpendGoalItem goalItem =
            newItem.GetComponent<SpendGoalItem>();

        if (goalItem != null)
        {
            pendingGoalItems.Add(goalItem);
        }
        else
        {
            Debug.LogWarning(
                "생성된 Spend Item에 SpendGoalItem 컴포넌트가 없습니다."
            );
        }

        Toggle toggle =
            newItem.GetComponentInChildren<Toggle>();

        if (toggle != null)
            toggle.SetIsOnWithoutNotify(false);

        goalNameInput.text = "";
        goldInput.text = "";

        if (cycleManager != null)
            cycleManager.SelectDaily();

        if (spendAddPanel != null)
            spendAddPanel.SetActive(false);

        if (spendPanel != null)
            spendPanel.SetActive(true);

        Debug.Log(
            $"특수 목표 임시 등록: {goalName} / {limitAmount}원"
        );
    }

    public List<CreateSpendingSpecialGoalData>
        GetPendingSpecialGoals()
    {
        return new List<CreateSpendingSpecialGoalData>(
            pendingSpecialGoals
        );
    }

    // 예산 저장 응답으로 받은 서버 특수목표와
    // 현재 생성된 UI Item을 연결한다.
    public void ApplySavedSpecialGoals(
        List<SpendingSpecialGoalResponse> savedGoals)
    {
        if (savedGoals == null)
        {
            Debug.LogWarning(
                "서버에서 반환된 특수 목표 목록이 없습니다."
            );
            return;
        }

        int count = Mathf.Min(
            pendingGoalItems.Count,
            savedGoals.Count
        );

        for (int i = 0; i < count; i++)
        {
            SpendGoalItem item =
                pendingGoalItems[i];

            SpendingSpecialGoalResponse goal =
                savedGoals[i];

            if (item == null || goal == null)
                continue;

            item.SetSpecialGoalId(goal.Id);

            Debug.Log(
                $"Special Goal 연결 완료 : {goal.Id}"
            );
        }

        pendingSpecialGoals.Clear();
        pendingGoalItems.Clear();
    }
}