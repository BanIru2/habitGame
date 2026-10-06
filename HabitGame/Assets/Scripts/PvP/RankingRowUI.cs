using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankingRowUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI rankText;
    [SerializeField]
    private Image titleImage;
    [SerializeField]
    private TextMeshProUGUI nameText;
    [SerializeField]
    private TextMeshProUGUI scoreText;

    public void SetData(RankingEntryResponse data, Sprite titleSprite = null)
    {
        rankText.text = data.Rank.ToString();
        nameText.text = data.Name.ToString();
        scoreText.text = data.Score.ToString();
        BindTitleImage(titleSprite);
    }

    public void ClearData()
    {
        rankText.text = "";
        nameText.text = "";
        scoreText.text = "";
        titleImage.sprite = null;
        titleImage.gameObject.SetActive(false);
    }

    private void BindTitleImage(Sprite titleSprite)
    {
        if (titleImage != null)
        {
            if (titleSprite != null)
            {
                titleImage.sprite = titleSprite;
                titleImage.gameObject.SetActive(true);
            }
            else
            {
                titleImage.gameObject.SetActive(false);
            }
        }
    }
}
