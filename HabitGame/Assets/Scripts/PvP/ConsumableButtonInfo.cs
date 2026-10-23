using UnityEngine;

public class ConsumableButtonInfo : MonoBehaviour
{
    public ConsumableDataSO itemData;

    [SerializeField]
    private TMPro.TextMeshProUGUI countText;

    public void SetCount(int count)
    {
        if (countText != null)
        {
            countText.text = Mathf.Max(0, count).ToString();
        }
    }
}