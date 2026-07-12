using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HabitUIManager : MonoBehaviour
{
    [SerializeField] private GameObject topTab;

    [SerializeField] private GameObject lifePanel;
    [SerializeField] private GameObject spendPanel;

    [SerializeField] private GameObject lifeAddPanel;
    [SerializeField] private GameObject spendAddPanel;

    private void Start()
    {
        OpenLife();
    }

    // 생활습관 화면
    public void OpenLife()
    {
        topTab.SetActive(true);

        lifePanel.SetActive(true);
        spendPanel.SetActive(false);

        lifeAddPanel.SetActive(false);
        spendAddPanel.SetActive(false);
    }

    // 소비습관 화면
    public void OpenSpend()
    {
        topTab.SetActive(true);

        lifePanel.SetActive(false);
        spendPanel.SetActive(true);

        lifeAddPanel.SetActive(false);
        spendAddPanel.SetActive(false);
    }

    // 생활습관 추가 화면
    public void OpenLifeAddPanel()
    {
        topTab.SetActive(false);

        lifePanel.SetActive(false);
        spendPanel.SetActive(false);

        lifeAddPanel.SetActive(true);
        spendAddPanel.SetActive(false);
    }

    // 소비습관 추가 화면
    public void OpenSpendAddPanel()
    {
        topTab.SetActive(false);

        lifePanel.SetActive(false);
        spendPanel.SetActive(false);

        lifeAddPanel.SetActive(false);
        spendAddPanel.SetActive(true);
    }

    // 생활습관으로 돌아가기
    public void BackToLife()
    {
        OpenLife();
    }

    // 소비습관으로 돌아가기
    public void BackToSpend()
    {
        OpenSpend();
    }
}
