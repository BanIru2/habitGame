
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

    // 이미 이번 주에 가져온 목표인지 여부
    public bool AlreadyImported { get; private set; }

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

        // 서버 응답의 가져오기 상태 반영
        SetAlreadyImported(goal.AlreadyImported);
    }

    // 이미 가져온 목표 선택 및 수정 방지
    public void SetAlreadyImported(bool imported)
    {
        AlreadyImported = imported;

        if (selectToggle != null)
        {
            selectToggle.SetIsOnWithoutNotify(false);
            selectToggle.interactable = !imported;
        }

        if (targetAmountInput != null)
            targetAmountInput.interactable = !imported;

        // 이미 가져온 목표임을 사용자에게 표시
        if (goalNameText != null && goal != null)
        {
            goalNameText.text = imported
                ? $"{goal.GoalName} (가져옴)"
                : goal.GoalName;
        }
    }

    // 가져오기 선택 여부
    public bool IsSelected =>
        !AlreadyImported &&
        selectToggle != null &&
        selectToggle.isOn;

    // 지난주 원본 목표 ID
    public long GoalId => goal != null ? goal.Id : 0;

    // 이번 주에 설정할 목표량
    public bool TryGetTargetAmount(out int amount)
    {
        amount = 0;

        if (AlreadyImported)
            return false;

        return targetAmountInput != null
            && int.TryParse(targetAmountInput.text, out amount)
            && amount > 0;
    }
}
