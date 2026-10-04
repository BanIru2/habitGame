using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaResultPopupManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private GameObject dim;
    [SerializeField] private RectTransform popupRect;
    [SerializeField] private RectTransform resultContainer;
    [SerializeField] private GridLayoutGroup resultGridLayout;
    [SerializeField] private GameObject resultItemPrefab;

    [Header("세팅값")]
    [SerializeField, Min(0f)] private float introDuration = 0.18f;
    [SerializeField, Min(0f)] private float itemAnimationStartDelay = 0.12f;
    [SerializeField, Min(0f)] private float itemAnimationInterval = 0.09f;
    [SerializeField, Min(0.01f)] private float itemAnimationDuration = 0.28f;
    [SerializeField, Range(0f, 1f)] private float itemStartScale = 0.7f;
    [SerializeField, Range(0f, 1f)] private float popupStartScale = 0.94f;

    private const int MaxGachaCount = 10;
    private readonly GachaResultItem[] slotPool = new GachaResultItem[MaxGachaCount];

    private Image dimImage;
    private Color dimTargetColor;
    private DimCloser dimCloser;
    private Coroutine introRoutine;

    private bool isSlotPoolPrepared;
    private bool isAnimationPlaying;
    private int activeSlotCount;

    private void Awake()
    {
        if (popupRect == null) popupRect = GetComponent<RectTransform>();

        if (dim != null)
        {
            dimImage = dim.GetComponent<Image>();
            if (dimImage != null) dimTargetColor = dimImage.color;

            dimCloser = dim.GetComponent<DimCloser>();
            if (dimCloser != null) dimCloser.AddListener(OnTouchedDim);
        }

        PrepareSlotPool();
    }

    public void Show(List<EquipmentDataSO> results)
    {
        if (results == null || results.Count == 0) return;

        PrepareSlotPool();
        NormalizeVisuals();

        // 그리드 컬럼 맞추기
        SetupGridLayout(results.Count);

        // 팝업 창과 딤(어두운 배경) 켜기
        if (dim != null) dim.SetActive(true);
        if (popupRoot != null) popupRoot.SetActive(true);

        isAnimationPlaying = true;

        if (introRoutine != null) StopCoroutine(introRoutine);
        introRoutine = StartCoroutine(IntroRoutine()); // 딤 페이드 & 팝업 스케일 인트로 시작!

        // 뽑힌 개수만큼만 실행! (단챠면 1번, 10연챠면 10번)
        for (int i = 0; i < results.Count; i++)
        {
            float delay = itemAnimationStartDelay + (i * itemAnimationInterval);
            slotPool[i].Play(results[i], delay, itemAnimationDuration, itemStartScale);
        }

        // 나머지 남는 슬롯(단챠 시 9개)은 화면에서 숨기기
        for (int i = results.Count; i < MaxGachaCount; i++)
        {
            slotPool[i].ResetVisuals();
        }
    }

    private IEnumerator IntroRoutine()
    {
        // 시작 상태
        if (dimImage != null)
        {
            Color c = dimTargetColor;
            c.a = 0f;
            dimImage.color = c;
        }
        if (popupRect != null) popupRect.localScale = Vector3.one * popupStartScale;

        float elapsed = 0f;
        while (elapsed < introDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = introDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / introDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            if (dimImage != null)
            {
                Color c = dimTargetColor;
                c.a = dimTargetColor.a * easedT;
                dimImage.color = c;
            }

            if (popupRect != null)
            {
                popupRect.localScale = Vector3.one * Mathf.Lerp(popupStartScale, 1f, easedT);
            }

            yield return null;
        }

        // 인트로 완료
        if (dimImage != null) dimImage.color = dimTargetColor;
        if (popupRect != null) popupRect.localScale = Vector3.one;
    }

    public void OnTouchedDim()
    {
        // 연출 중 터치하면 즉시 스킵! 완료 후 터치하면 닫기!
        if (isAnimationPlaying)
        {
            CompleteAnimation();
        }
        else
        {
            Close();
        }
    }

    public void CompleteAnimation()
    {
        isAnimationPlaying = false;

        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }

        if (dimImage != null) dimImage.color = dimTargetColor;
        if (popupRect != null) popupRect.localScale = Vector3.one;

        // 모든 슬롯 즉시 완성 상태로 변경
        for (int i = 0; i < MaxGachaCount; i++)
        {
            if (slotPool[i] != null && slotPool[i].gameObject.activeSelf)
                slotPool[i].CompleteAnimation();
        }
    }

    public void Close()
    {
        CompleteAnimation();
        if (popupRoot != null) popupRoot.SetActive(false);
        if (dim != null) dim.SetActive(false);
    }

    private void SetupGridLayout(int count)
    {
        float availableWidth = resultContainer.rect.width - (resultGridLayout.padding.left + resultGridLayout.padding.right);
        float cellWidthWithSpacing = resultGridLayout.cellSize.x + resultGridLayout.spacing.x;
        int maxColumns = cellWidthWithSpacing > 0f ? Mathf.FloorToInt((availableWidth + resultGridLayout.spacing.x) / cellWidthWithSpacing) : 1;
        resultGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        resultGridLayout.constraintCount = Mathf.Min(count, Mathf.Max(1, maxColumns));
    }

    private void PrepareSlotPool()
    {
        if (isSlotPoolPrepared) return;
        for (int i = 0; i < MaxGachaCount; i++)
        {
            if (slotPool[i] == null)
            {
                GameObject obj = Instantiate(resultItemPrefab, resultContainer);
                slotPool[i] = obj.GetComponent<GachaResultItem>() ?? obj.AddComponent<GachaResultItem>();
                slotPool[i].Initialize();
            }
        }
        isSlotPoolPrepared = true;
    }

    private void NormalizeVisuals()
    {
        CompleteAnimation();
        for (int i = 0; i < MaxGachaCount; i++)
        {
            if (slotPool[i] != null) slotPool[i].ResetVisuals();
        }
    }
}