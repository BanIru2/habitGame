using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class SpendCycleManager : MonoBehaviour
{
    public Button dailyButton;
    public Button weeklyButton;

    public Color selectedColor = new Color(0.15f, 0.55f, 0.20f);
    public Color normalColor = Color.white;

    public bool isWeekly = false;

    private Image dailyImage;
    private Image weeklyImage;

    void Start()
    {
        dailyImage = dailyButton.GetComponent<Image>();
        weeklyImage = weeklyButton.GetComponent<Image>();

        SelectDaily();
    }

    public void SelectDaily()
    {
        isWeekly = false;

        dailyImage.color = selectedColor;
        weeklyImage.color = normalColor;
    }

    public void SelectWeekly()
    {
        isWeekly = true;

        weeklyImage.color = selectedColor;
        dailyImage.color = normalColor;
    }
}
