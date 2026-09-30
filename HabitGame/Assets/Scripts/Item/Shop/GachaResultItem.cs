using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform), typeof(Image))]
public class GachaResultItem : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image slotImage;
    private Coroutine currentRoutine;

    public bool IsAnimationCompleted { get; private set; } = true;

    public void Initialize()
    {
        rectTransform = GetComponent<RectTransform>();
        slotImage = GetComponent<Image>();
        ResetVisuals();
    }

    // 연출 시작을 위한 외부 접근 함수
    public void Play(EquipmentDataSO item, float delay, float baseDuration, float startScale)
    {
        if (rectTransform == null || slotImage == null) Initialize();

        // 데이터 및 초기 상태(투명, 0.7배) 세팅
        SetupInitialVisuals(item, startScale);

        // 이전 코루틴이 돌고 있다면 끄고 새로 시작
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(AnimationRoutine(item, delay, baseDuration, startScale));
    }

    private IEnumerator AnimationRoutine(EquipmentDataSO item, float delay, float baseDuration, float startScale)
    {
        IsAnimationCompleted = false;

        // [대기 구간] 내 차례가 올 때까지 기다림 (unscaled 대기)
        float delayTimer = 0f;
        while (delayTimer < delay)
        {
            delayTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        // [스펙 결정]
        Color startColor = item != null ? item.TierColor : Color.white;
        float overshootScale = item != null ? GetTierOvershootScale(item.Tier) : 1.08f;
        float duration = item != null ? GetTierAnimationDuration(item.Tier, baseDuration) : baseDuration;

        // [애니메이션 구간] 0초부터 duration까지 튀어오름
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);

            // 색상 및 알파 (TierColor -> White)
            Color color = Color.Lerp(startColor, Color.white, t);
            color.a = t;
            slotImage.color = color;

            // 오버슈트 스케일 (0.7 -> Overshoot -> 1.0)
            float scale;
            if (t < 0.7f)
            {
                float upT = Mathf.SmoothStep(0f, 1f, t / 0.7f);
                scale = Mathf.Lerp(startScale, overshootScale, upT);
            }
            else
            {
                float settleT = Mathf.SmoothStep(0f, 1f, (t - 0.7f) / 0.3f);
                scale = Mathf.Lerp(overshootScale, 1f, settleT);
            }

            rectTransform.localScale = Vector3.one * scale;
            yield return null; // 다음 프레임까지 대기
        }

        CompleteAnimation();
    }

    public void CompleteAnimation()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        if (slotImage != null) slotImage.color = Color.white;
        if (rectTransform != null) rectTransform.localScale = Vector3.one;
        IsAnimationCompleted = true;
    }

    public void ResetVisuals()
    {
        CompleteAnimation();
        gameObject.SetActive(false);
    }

    private void SetupInitialVisuals(EquipmentDataSO item, float startScale)
    {
        slotImage.sprite = item != null ? item.icon : null;
        Color initColor = item != null ? item.TierColor : Color.white;
        initColor.a = 0f;
        slotImage.color = initColor;
        rectTransform.localScale = Vector3.one * startScale;
        gameObject.SetActive(true);
    }

    private float GetTierOvershootScale(int tier) => tier switch
    {
        >= 4 => 1.15f,
        3 => 1.12f,
        2 => 1.10f,
        _ => 1.08f
    };

    private float GetTierAnimationDuration(int tier, float baseDuration) => tier switch
    {
        >= 4 => baseDuration + 0.06f,
        3 => baseDuration + 0.02f,
        2 => baseDuration,
        _ => Mathf.Max(0.01f, baseDuration - 0.02f)
    };
}