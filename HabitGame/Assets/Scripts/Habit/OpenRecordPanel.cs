using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenRecordPanel : MonoBehaviour
{
    [SerializeField] private GameObject topTab;
    [SerializeField] private GameObject spendPanel;
    [SerializeField] private GameObject recordPanel;

    public void OpenRecord()
    {
        SetTapBarActive(false);
        topTab.SetActive(false);

        spendPanel.SetActive(false);
        recordPanel.SetActive(true);
    }

    public void BackToSpend()
    {
        SetTapBarActive(true);
        topTab.SetActive(true);

        recordPanel.SetActive(false);
        spendPanel.SetActive(true);
    }

    private void SetTapBarActive(bool active)
    {
        Transform tapBar = transform.root.Find("TapBar");
        if (tapBar != null)
        {
            tapBar.gameObject.SetActive(active);
        }
        else
        {
            Debug.LogError("Canvas/TapBar could not be found.");
        }
    }
}
