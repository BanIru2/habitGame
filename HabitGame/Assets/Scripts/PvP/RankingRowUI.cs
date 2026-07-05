using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RankingRowUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI rankText;
    [SerializeField]
    private TextMeshProUGUI nameText;
    [SerializeField]
    private TextMeshProUGUI scoreText;

    public void SetData(RankingEntryResponse data)
    {
        rankText.text = data.Rank.ToString();
        nameText.text = data.Name.ToString();
        scoreText.text = data.Score.ToString();
    }

    public void ClearData()
    {
        rankText.text = "";
        nameText.text = "";
        scoreText.text = "";
    }
}
