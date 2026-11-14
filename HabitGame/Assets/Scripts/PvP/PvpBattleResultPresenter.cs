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

    [Header("15-D-7D result presentation")]
    [SerializeField] private TMP_Text resultTitle;
    [SerializeField] private TMP_Text resultMessage;
    [SerializeField] private TMP_Text myNameText;
    [SerializeField] private TMP_Text opponentNameText;

    private static readonly Color32 VictoryColor = new Color32(0xB5, 0x82, 0x28, 0xFF);
    private static readonly Color32 DefeatColor = new Color32(0xB8, 0x55, 0x4F, 0xFF);
    private static readonly Color32 DrawColor = new Color32(0x78, 0x68, 0x62, 0xFF);

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
}
