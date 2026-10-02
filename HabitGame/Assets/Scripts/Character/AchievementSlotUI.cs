using System;
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
    [SerializeField]
    private TextMeshProUGUI rewardClaimText;

    private AchievementViewData viewData;

    // 수령 버튼 색
    private static readonly Color32 ClaimableColor = new Color32(255, 205, 60, 255);
    private static readonly Color32 InProgressColor = new Color32(140, 140, 140, 255);
    private static readonly Color32 ClaimedColor = new Color32(80, 80, 80, 255);
    // 수령 버튼 문자 색
    private static readonly Color32 ClaimableTextColor = new Color32(255, 136, 1, 255);
    private static readonly Color32 InProgressTextColor = new Color32(220, 220, 220, 255); // 연회색
    private static readonly Color32 ClaimedTextColor = new Color32(140, 140, 140, 255);   // 어두운 회색

    private Color GetAttributeColor(AttributeType attribute) => attribute switch
    {
        AttributeType.Fire => new Color32(255, 95, 75, 255),   // 불 - 코랄 레드
        AttributeType.Water => new Color32(50, 160, 255, 255),  // 물 - 스카이 블루
        AttributeType.Grass => new Color32(75, 215, 100, 255),  // 풀 - 라이트 그린
        AttributeType.Aurora => new Color32(190, 95, 255, 255),  // 오로라 - 퍼플 핑크
        _ => new Color32(255, 205, 60, 255)   // None (가챠/골드 등 일반 업적) - 골드 옐로우
    };

    public void LoadData(AchievementViewData vData, Action<AchievementViewData> onClaim, Action<AchievementViewData> onCheckReward)
    {
        ResetUI();
        viewData = vData;
        if (viewData == null || viewData.AchievementSO == null) return;
        ApplyContent();
        ApplyBarColor();
        ApplyButtons(onClaim, onCheckReward);
        UpdateProgress(); // ★ 여기서 바로 진행도와 버튼 상태를 갱신!
    }

    // 업적 내용 텍스트 반영
    private void ApplyContent()
    {
        if (titleText != null)
        {
            titleText.text = viewData.AchievementSO.content;
        }
    }

    // 진행도 바 채우기 색 결정
    private void ApplyBarColor()
    {
        if (progressBarFill != null)
        {
            progressBarFill.color = GetAttributeColor(viewData.AchievementSO.attribute);
        }
    }

    // 현재 진행도(current)와 목표치(target)를 받아 게이지 갱신
    public void UpdateProgress()
    {
        if (viewData == null || viewData.AchievementSO == null) return;
        int current = viewData.Response != null ? viewData.Response.CurrentCount : 0;
        int target = viewData.AchievementSO.targetCount;
        // 진행도 텍스트 표시
        if (progressText != null)
        {
            progressText.text = $"{current} / {target}";
        }
        // 게이지 비율 반영
        if (progressBarFill != null)
        {
            float ratio = target > 0 ? (float)current / target : 0f;
            Vector3 scale = progressBarFill.rectTransform.localScale;
            scale.x = Mathf.Clamp01(ratio);
            progressBarFill.rectTransform.localScale = scale;
        }
        // 진행도에 따라 보상 받기 버튼 상태 동기화
        UpdateRewardClaimButton();
    }

    // 버튼 상태 및 클릭 이벤트 바인딩
    private void ApplyButtons(Action<AchievementViewData> onClaim, Action<AchievementViewData> onCheckReward)
    {
        // 보상 받기 버튼: 달성 완료 & 미수령 상태일 때만 활성화
        if (rewardClaimButton != null)
        {
            if (viewData.CanClaim && onClaim != null)
            {
                rewardClaimButton.onClick.AddListener(() => onClaim(viewData));
            }
        }
        // 보상 확인 버튼
        if (checkRewardButton != null && onCheckReward != null)
        {
            checkRewardButton.onClick.AddListener(() => onCheckReward(viewData));
        }
    }

    public void RefreshClaimButton()
    {
        UpdateRewardClaimButton();
    }

    private void UpdateRewardClaimButton()
    {
        if (rewardClaimButton == null || viewData == null)
        {
            return;
        }

        bool isClaimed = viewData.Response != null && viewData.Response.IsClaimed;

        if (isClaimed)
        {
            rewardClaimButton.interactable = false;

            if (rewardClaimText != null)
            {
                rewardClaimText.text = "수령\n완료";
                rewardClaimText.color = ClaimedTextColor;
            }

            if (rewardClaimButton.image != null)
            {
                rewardClaimButton.image.color = ClaimedColor;
            }

            return;
        }

        rewardClaimButton.interactable = viewData.CanClaim;

        if (rewardClaimText != null)
        {
            rewardClaimText.text = "보상\n수령";
            rewardClaimText.color = viewData.CanClaim ? ClaimableTextColor : InProgressTextColor;
        }

        if (rewardClaimButton.image != null)
        {
            rewardClaimButton.image.color = viewData.CanClaim ? ClaimableColor : InProgressColor;
        }
    }

    // UI 초기화
    public void ResetUI()
    {
        if (titleText != null) titleText.text = "";
        if (progressText != null) progressText.text = "";
        // 게이지 0으로 리셋
        if (progressBarFill != null)
        {
            Vector3 scale = progressBarFill.rectTransform.localScale;
            scale.x = 0f;
            progressBarFill.rectTransform.localScale = scale;
        }
        // 버튼에 걸려있던 이전 클릭 리스너 해제
        if (checkRewardButton != null) checkRewardButton.onClick.RemoveAllListeners();
        if (rewardClaimButton != null)
        {
            rewardClaimButton.onClick.RemoveAllListeners();
            rewardClaimButton.interactable = false;
        }
    }

}
