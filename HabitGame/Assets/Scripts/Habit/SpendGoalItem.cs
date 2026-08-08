using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpendGoalItem : MonoBehaviour
{
    [SerializeField] private Toggle completeToggle;
    [SerializeField] private TextMeshProUGUI rewardText;

    private bool rewarded = false;

    private void Start()
    {
        completeToggle.onValueChanged.AddListener(OnToggleChanged);
    }

    void OnToggleChanged(bool isOn)
    {
        if (isOn && !rewarded)
        {
            rewarded = true;

            int reward = int.Parse(rewardText.text.Replace("+", ""));

            SpendRewardManager.Instance.AddGold(reward);

            Debug.Log("Reward!");
        }
    }
}
