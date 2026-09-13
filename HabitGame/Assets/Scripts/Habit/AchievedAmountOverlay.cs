using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievedAmountOverlay : MonoBehaviour
{
    public static AchievedAmountOverlay Instance;

    [Header("UI")]
    [SerializeField] private GameObject popup;
    [SerializeField] private TextMeshProUGUI goalText;
    [SerializeField] private TMP_InputField amountInput;
    [SerializeField] private TextMeshProUGUI unitText;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button confirmButton;

    private Action<int> onConfirm;
    private Action onCancel;

    private void Awake()
    {
        Instance = this;

        if (cancelButton != null)
            cancelButton.onClick.AddListener(Cancel);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(Confirm);

        Close();
    }

    public void Open(
        HabitGoalResponse habit,
        Action<int> confirmCallback,
        Action cancelCallback)
    {
        if (habit == null)
        {
            Debug.LogWarning(
                "AchievedAmountOverlay에 Habit 데이터가 없습니다."
            );

            return;
        }

        onConfirm = confirmCallback;
        onCancel = cancelCallback;

        if (goalText != null)
        {
            goalText.text =
                habit.GoalName;
        }

        if (unitText != null)
        {
            unitText.text =
                habit.Unit;
        }

        if (amountInput != null)
        {
            amountInput.text = "";
        }

        if (popup != null)
        {
            popup.SetActive(true);
        }

        if (amountInput != null)
        {
            amountInput.ActivateInputField();
        }
    }

    private void Confirm()
    {
        if (amountInput == null)
        {
            Debug.LogWarning(
                "Achieved Amount Input이 연결되지 않았습니다."
            );

            return;
        }

        if (!int.TryParse(
                amountInput.text,
                out int achievedAmount))
        {
            Debug.LogWarning(
                "달성량은 숫자로 입력해주세요."
            );

            return;
        }

        if (achievedAmount <= 0)
        {
            Debug.LogWarning(
                "달성량은 0보다 커야 합니다."
            );

            return;
        }

        Action<int> callback =
            onConfirm;

        Close();

        callback?.Invoke(
            achievedAmount
        );
    }

    private void Cancel()
    {
        Action callback =
            onCancel;

        Close();

        callback?.Invoke();
    }

    public void Close()
    {
        onConfirm = null;
        onCancel = null;

        if (amountInput != null)
        {
            amountInput.text = "";
        }

        if (popup != null)
        {
            popup.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(
                Cancel
            );
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(
                Confirm
            );
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
}