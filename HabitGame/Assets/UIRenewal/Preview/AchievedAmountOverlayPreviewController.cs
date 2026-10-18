using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UITestScene-only local preview for the NUMBER input overlay.
/// It calls AchievedAmountOverlay directly and never touches HabitItem or an API service.
/// </summary>
public sealed class AchievedAmountOverlayPreviewController : MonoBehaviour
{
    [SerializeField] private AchievedAmountOverlay overlay;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text statusText;

    private const int TargetAmount = 2000;
    private int currentAmount = 750;

    private void Awake()
    {
        if (openButton != null)
        {
            openButton.onClick.AddListener(OpenPreview);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePreview);
        }
    }

    private void Start()
    {
        OpenPreview();
    }

    public void OpenPreview()
    {
        if (overlay == null)
        {
            SetStatus("Overlay reference is missing.");
            return;
        }

        HabitGoalResponse previewHabit = new HabitGoalResponse
        {
            Id = 0,
            GoalName = "하루 2L 물 마시기",
            RecordType = "value",
            VerificationType = "none",
            CurrentAmount = currentAmount,
            TargetAmount = TargetAmount,
            Unit = "mL",
            Period = "daily",
            IsActive = true
        };

        overlay.Open(previewHabit, ConfirmPreview, CancelPreview);
        SetStatus($"현재 누적 {currentAmount:N0} / {TargetAmount:N0} mL");
    }

    public void ClosePreview()
    {
        if (overlay != null)
        {
            overlay.Close();
        }

        SetStatus($"닫힘 · 누적 {currentAmount:N0} / {TargetAmount:N0} mL 유지");
    }

    private void ConfirmPreview(int achievedAmount)
    {
        currentAmount = Mathf.Min(currentAmount + achievedAmount, TargetAmount);
        SetStatus($"+{achievedAmount:N0} mL 확인 · 누적 {currentAmount:N0} / {TargetAmount:N0} mL");
    }

    private void CancelPreview()
    {
        SetStatus($"취소 · 누적 {currentAmount:N0} / {TargetAmount:N0} mL 유지");
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void OnDestroy()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OpenPreview);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePreview);
        }
    }
}
