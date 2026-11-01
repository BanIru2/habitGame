using TMPro;
using UnityEngine;

public sealed class PvpBattleResultPresenter : MonoBehaviour
{
    [Header("Existing runtime sources")]
    [SerializeField] private TMP_Text myNameSource;
    [SerializeField] private TMP_Text opponentNameSource;
    [SerializeField] private GameObject winImage;
    [SerializeField] private GameObject loseImage;
    [SerializeField] private GameObject drawImage;
    [SerializeField] private TMP_Text scoreBeforeSource;
    [SerializeField] private TMP_Text scoreOperatorSource;
    [SerializeField] private TMP_Text scoreDeltaSource;

    [Header("15-D-7D result presentation")]
    [SerializeField] private TMP_Text resultTitle;
    [SerializeField] private TMP_Text resultMessage;
    [SerializeField] private TMP_Text myNameText;
    [SerializeField] private TMP_Text opponentNameText;
    [SerializeField] private TMP_Text finalScoreText;

    private static readonly Color32 VictoryColor = new Color32(0xB5, 0x82, 0x28, 0xFF);
    private static readonly Color32 DefeatColor = new Color32(0xB8, 0x55, 0x4F, 0xFF);
    private static readonly Color32 DrawColor = new Color32(0x78, 0x68, 0x62, 0xFF);
    private static readonly Color32 PositiveColor = new Color32(0x4F, 0x7F, 0x52, 0xFF);
    private static readonly Color32 NegativeColor = new Color32(0xB8, 0x55, 0x4F, 0xFF);
    private static readonly Color32 NeutralColor = new Color32(0x78, 0x68, 0x62, 0xFF);

    private void OnEnable()
    {
        Refresh();
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (myNameText != null && myNameSource != null)
            myNameText.text = myNameSource.text;
        if (opponentNameText != null && opponentNameSource != null)
            opponentNameText.text = opponentNameSource.text;

        RefreshResultCopy();
        RefreshScoreCopy();
    }

    private void RefreshResultCopy()
    {
        bool isWin = winImage != null && winImage.activeSelf;
        bool isLose = loseImage != null && loseImage.activeSelf;
        bool isDraw = drawImage != null && drawImage.activeSelf;

        string title = isWin ? "VICTORY" : isLose ? "DEFEAT" : isDraw ? "DRAW" : string.Empty;
        string message = isWin ? "멋진 승리입니다!" :
            isLose ? "다음 전투를 준비해 보세요." :
            isDraw ? "팽팽한 승부였습니다." : string.Empty;
        Color color = isWin ? VictoryColor : isLose ? DefeatColor : DrawColor;

        if (resultTitle != null)
        {
            resultTitle.text = title;
            resultTitle.color = color;
        }

        if (resultMessage != null)
        {
            resultMessage.text = message;
            resultMessage.color = color;
        }
    }

    private void RefreshScoreCopy()
    {
        if (finalScoreText == null || scoreBeforeSource == null ||
            scoreOperatorSource == null || scoreDeltaSource == null)
            return;

        bool hasBefore = int.TryParse(scoreBeforeSource.text, out int before);
        bool hasDelta = int.TryParse(scoreDeltaSource.text, out int absoluteDelta);
        string scoreOperator = scoreOperatorSource.text != null ? scoreOperatorSource.text.Trim() : string.Empty;
        int signedDelta = scoreOperator == "-" ? -absoluteDelta : absoluteDelta;

        if (!hasBefore || !hasDelta)
        {
            finalScoreText.gameObject.SetActive(false);
            return;
        }

        finalScoreText.gameObject.SetActive(true);
        finalScoreText.text = $"변경 후  {before + signedDelta:N0} RP";
        Color deltaColor = signedDelta > 0 ? PositiveColor : signedDelta < 0 ? NegativeColor : NeutralColor;
        finalScoreText.color = deltaColor;
        scoreOperatorSource.color = deltaColor;
        scoreDeltaSource.color = deltaColor;
    }
}
