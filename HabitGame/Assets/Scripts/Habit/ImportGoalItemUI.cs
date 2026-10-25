
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ImportGoalItemUI : MonoBehaviour
{
    [SerializeField] private Toggle selectToggle;
    [SerializeField] private TextMeshProUGUI goalNameText;
    [SerializeField] private TMP_InputField targetAmountInput;
    [SerializeField] private TextMeshProUGUI unitText;

    private HabitGoalResponse goal;

    // 지난주 목표 데이터를 UI에 표시
    public void SetData(HabitGoalResponse data)
    {
        goal = data;

        if (goal == null)
            return;

        if (goalNameText != null)
            goalNameText.text = goal.GoalName;

        if (targetAmountInput != null)
            targetAmountInput.text = goal.TargetAmount.ToString();

        if (unitText != null)
            unitText.text = goal.Unit;

        if (selectToggle != null)
            selectToggle.SetIsOnWithoutNotify(false);
    }

    // 가져오기 선택 여부
    public bool IsSelected =>
        selectToggle != null && selectToggle.isOn;

    // 지난주 원본 목표 ID
    public long GoalId => goal != null ? goal.Id : 0;

    // 이번 주에 설정할 목표량
    public bool TryGetTargetAmount(out int amount)
    {
        amount = 0;

        return targetAmountInput != null
            && int.TryParse(targetAmountInput.text, out amount)
            && amount > 0;
    }
}
