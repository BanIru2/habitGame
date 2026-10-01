using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementSlotUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI titleText;
    [SerializeField]
    private Image progressBarFill;
    [SerializeField]
    private TextMeshProUGUI progressText;
    [SerializeField]
    private Button checkRewardButton;
    [SerializeField]
    private Button rewardClaimButton;


    private Color GetAttributeColor(AttributeType attribute) => attribute switch
    {
        AttributeType.Fire => new Color32(255, 95, 75, 255),   // 불 - 코랄 레드
        AttributeType.Water => new Color32(50, 160, 255, 255),  // 물 - 스카이 블루
        AttributeType.Grass => new Color32(75, 215, 100, 255),  // 풀 - 라이트 그린
        AttributeType.Aurora => new Color32(190, 95, 255, 255),  // 오로라 - 퍼플 핑크
        _ => new Color32(255, 205, 60, 255)   // None (가챠/골드 등 일반 업적) - 골드 옐로우
    };

    // 현재 진행도(current)와 목표치(target)를 받아 게이지 갱신
    public void UpdateProgress(int current, int target)
    {
        // 텍스트 표시
        if (progressText != null)
        {
            progressText.text = $"{current} / {target}";
        }
        // 비율 계산
        float ratio = target > 0 ? (float)current / target : 0f;
        // localScale.x 조절
        if (progressBarFill != null)
        {
            Vector3 scale = progressBarFill.rectTransform.localScale;
            scale.x = Mathf.Clamp01(ratio);
            progressBarFill.rectTransform.localScale = scale;
        }
    }

    public void Setup(AttributeType attribute, int current, int target)
    {
        // 게이지 색상 변경!
        if (progressBarFill != null)
        {
            progressBarFill.color = GetAttributeColor(attribute);
        }

        // 기존 진행도 갱신
        UpdateProgress(current, target);
    }

}
