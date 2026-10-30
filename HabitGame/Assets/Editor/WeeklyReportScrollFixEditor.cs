#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// HabitPVP 15-D-8B / v4: Fixes WeeklyReportPanel scrolling only.
/// Use together with the previously applied v3 design. Does not edit
/// WeeklyReportManager, API, TapBar, scenes/prefab assets, or TMP submeshes.
/// </summary>
public static class WeeklyReportScrollFixEditor
{
    private const string Menu = "Tools/HabitPVP/15-D-8B Weekly Report/";
    private const string ViewPath = "Scroll View/Viewport";
    private const string ContentPath = "Scroll View/Viewport/Content";
    private const string BarPath = "Scroll View/Scrollbar Vertical";

    private static readonly Color Cream = Html("#F8F2E5");
    private static readonly Color Gold = Html("#C5A574");
    private static readonly string[] Cards =
    {
        "HabitSummaryCard", "SpendingSummaryCard", "AIFeedbackCard"
    };
    private static readonly string[] AiTexts =
    {
        "AISummaryText", "AIGoodPointText", "AIImprovementText", "AINextWeekText"
    };

    private struct Parts
    {
        public Transform root;
        public ScrollRect scroll;
        public RectTransform viewport;
        public RectTransform content;
        public Scrollbar scrollbar;
        public RectTransform scrollbarRect;
        public Image handleImage;
        public VerticalLayoutGroup layout;
        public ContentSizeFitter fitter;
    }

    [MenuItem(Menu + "7. Validate Scroll v4")]
    private static void Validate()
    {
        Parts p;
        if (!FindParts(out p)) return;
        Debug.Log("[15-D-8B v4] Validate PASS. 대상 오브젝트와 컴포넌트를 찾았습니다. 변경 없음. " +
                  "기존 ScrollRect: content=" + (p.scroll.content != null ? p.scroll.content.name : "NULL") +
                  ", viewport=" + (p.scroll.viewport != null ? p.scroll.viewport.name : "NULL") +
                  ", verticalScrollbar=" + (p.scroll.verticalScrollbar != null ? p.scroll.verticalScrollbar.name : "NULL"));
    }

    [MenuItem(Menu + "8. Dry Run Scroll v4")]
    private static void DryRun()
    {
        Parts p;
        if (!FindParts(out p)) return;
        Debug.Log("[15-D-8B v4] Dry Run PASS (변경 없음). " +
                  "ScrollRect에 Content/Viewport/Scrollbar 연결, 휠 감도 설정, 드래그 입력 활성화, " +
                  "Scrollbar 표시·배치, Content PreferredSize 및 AI 텍스트의 고정 높이 제약 해제, " +
                  "Viewport 마스킹 확인, 이동 범위 Clamped 설정. 기존 v3 카드 디자인·기능 스크립트 유지.");
    }

    [MenuItem(Menu + "9. Apply Scroll v4 (No Auto Save)")]
    private static void Apply()
    {
        Parts p;
        if (!FindParts(out p)) return;

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("15-D-8B Weekly Report Scroll Fix v4");

        try
        {
            ConfigureScroller(p);
            ConfigureViewportAndContent(p);
            ConfigureScrollbar(p);
            ClearConflictingFixedHeights(p);

            EditorSceneManager.MarkSceneDirty(p.root.gameObject.scene);
            Undo.CollapseUndoOperations(group);
            Debug.Log("[15-D-8B v4] Apply 완료 (자동 저장 안 함). Play Mode에서 마우스 휠과 " +
                      "스크롤바 드래그를 확인하세요. 기존 v3 Apply는 다시 실행하지 마세요.");
        }
        catch (Exception ex)
        {
            Undo.RevertAllDownToGroup(group);
            Debug.LogError("[15-D-8B v4] 적용 실패, Undo 복구 시도: " + ex);
        }
    }

    [MenuItem(Menu + "10. Validate Applied Scroll v4")]
    private static void ValidateApplied()
    {
        Parts p;
        if (!FindParts(out p)) return;

        bool links = p.scroll.content == p.content &&
                     p.scroll.viewport == p.viewport &&
                     p.scroll.verticalScrollbar == p.scrollbar;
        bool scrollReady = p.scroll.enabled && p.scroll.vertical && !p.scroll.horizontal &&
                           p.scroll.movementType == ScrollRect.MovementType.Clamped &&
                           p.scroll.scrollSensitivity >= 40f;
        bool scrollbarReady = p.scrollbar.gameObject.activeSelf &&
                              p.scrollbar.enabled && p.scrollbar.interactable &&
                              p.scrollbar.handleRect != null &&
                              p.handleImage != null && p.handleImage.raycastTarget;
        bool layoutReady = p.layout.childControlHeight && !p.layout.childForceExpandHeight &&
                           p.fitter.verticalFit == ContentSizeFitter.FitMode.PreferredSize;
        bool maskReady = p.viewport.GetComponent<RectMask2D>() != null ||
                         p.viewport.GetComponent<Mask>() != null;
        Image target = p.scroll.GetComponent<Image>();
        bool raycastReady = target != null && target.raycastTarget;
        bool passed = links && scrollReady && scrollbarReady && layoutReady && maskReady && raycastReady;

        // Layout values can be stale while WeeklyReportPanel is inactive in Edit Mode.
        float contentHeight = p.content.rect.height;
        float viewHeight = p.viewport.rect.height;
        float preferredHeight = LayoutUtility.GetPreferredHeight(p.content);
        string details = " [Content H=" + contentHeight.ToString("F1") +
                         ", Viewport H=" + viewHeight.ToString("F1") +
                         ", Preferred H=" + preferredHeight.ToString("F1") + "]";
        if (passed)
        {
            Debug.Log("[15-D-8B v4] Static Validate PASS." + details +
                      " 입력과 실제 스크롤은 Play Mode에서 확인하세요.");
        }
        else
        {
            Debug.LogError("[15-D-8B v4] Static Validate FAIL: links=" + links +
                           ", scroll=" + scrollReady + ", scrollbar=" + scrollbarReady +
                           ", layout=" + layoutReady + ", mask=" + maskReady +
                           ", raycast=" + raycastReady + details);
        }

        // Missing EventSystem or GraphicRaycaster also prevents pointer input in Play Mode.
        if (UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            Debug.LogWarning("[15-D-8B v4] 활성 EventSystem을 찾지 못했습니다. Play Mode의 EventSystem 확인 필요.");
        Canvas canvas = p.root.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.GetComponent<GraphicRaycaster>() == null)
            Debug.LogWarning("[15-D-8B v4] 상위 Canvas에 GraphicRaycaster가 있는지 확인하세요.");
    }

    private static void ConfigureScroller(Parts p)
    {
        Save(p.scroll);
        p.scroll.enabled = true;
        p.scroll.content = p.content;
        p.scroll.viewport = p.viewport;
        p.scroll.horizontal = false;
        p.scroll.vertical = true;
        p.scroll.movementType = ScrollRect.MovementType.Clamped;
        p.scroll.inertia = false;
        p.scroll.scrollSensitivity = 80f;
        p.scroll.verticalScrollbar = p.scrollbar;
        p.scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        p.scroll.verticalScrollbarSpacing = 6f;
        Dirty(p.scroll);

        // A raycastable Graphic on Scroll View makes pointer-wheel and drag input reliable.
        Image inputImage = p.scroll.GetComponent<Image>();
        if (inputImage == null)
        {
            inputImage = Undo.AddComponent<Image>(p.scroll.gameObject);
            Save(inputImage);
            inputImage.sprite = null;
            inputImage.color = Cream;
        }
        else
        {
            Save(inputImage);
        }
        inputImage.raycastTarget = true;
        Dirty(inputImage);
    }

    private static void ConfigureViewportAndContent(Parts p)
    {
        // Reserve a narrow strip for the visible scrollbar without moving any cards.
        Save(p.viewport);
        p.viewport.anchorMin = Vector2.zero;
        p.viewport.anchorMax = Vector2.one;
        p.viewport.offsetMin = new Vector2(0f, 0f);
        p.viewport.offsetMax = new Vector2(-36f, 0f);
        Dirty(p.viewport);

        if (p.viewport.GetComponent<Mask>() == null &&
            p.viewport.GetComponent<RectMask2D>() == null)
            Undo.AddComponent<RectMask2D>(p.viewport.gameObject);

        Save(p.content);
        p.content.anchorMin = new Vector2(0f, 1f);
        p.content.anchorMax = new Vector2(1f, 1f);
        p.content.pivot = new Vector2(0.5f, 1f);
        // Height remains owned by ContentSizeFitter, never an arbitrary hard-coded value.
        p.content.anchoredPosition = new Vector2(0f, 0f);
        p.content.sizeDelta = new Vector2(0f, p.content.sizeDelta.y);
        Dirty(p.content);

        Save(p.layout);
        p.layout.childControlWidth = true;
        p.layout.childControlHeight = true;
        p.layout.childForceExpandWidth = true;
        p.layout.childForceExpandHeight = false;
        // Only a modest gap after the final card; do not allow excessive overscroll.
        RectOffset pad = p.layout.padding;
        p.layout.padding = new RectOffset(pad.left, pad.right, pad.top, 24);
        Dirty(p.layout);

        Save(p.fitter);
        p.fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        p.fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        Dirty(p.fitter);
    }

    private static void ConfigureScrollbar(Parts p)
    {
        // Do not create a second scrollbar; restore and reuse existing one.
        if (!p.scrollbar.gameObject.activeSelf)
        {
            Undo.RecordObject(p.scrollbar.gameObject, "Enable weekly report scrollbar");
            p.scrollbar.gameObject.SetActive(true);
            Dirty(p.scrollbar.gameObject);
        }

        Save(p.scrollbarRect);
        p.scrollbarRect.anchorMin = new Vector2(1f, 0f);
        p.scrollbarRect.anchorMax = new Vector2(1f, 1f);
        p.scrollbarRect.pivot = new Vector2(1f, 0.5f);
        p.scrollbarRect.anchoredPosition = new Vector2(-5f, 0f);
        p.scrollbarRect.sizeDelta = new Vector2(22f, -16f);
        Dirty(p.scrollbarRect);

        Save(p.scrollbar);
        p.scrollbar.handleRect = p.handleImage.rectTransform;
        p.scrollbar.enabled = true;
        p.scrollbar.interactable = true;
        p.scrollbar.direction = Scrollbar.Direction.BottomToTop;
        p.scrollbar.numberOfSteps = 0; // smooth, not discrete steps
        Dirty(p.scrollbar);

        Image trackImage = p.scrollbar.GetComponent<Image>();
        if (trackImage == null)
        {
            trackImage = Undo.AddComponent<Image>(p.scrollbar.gameObject);
            Save(trackImage);
            trackImage.sprite = null;
            trackImage.type = Image.Type.Simple;
        }
        else
        {
            Save(trackImage);
        }
        trackImage.color = new Color(Gold.r, Gold.g, Gold.b, 0.25f);
        trackImage.raycastTarget = true;
        Dirty(trackImage);

        Save(p.handleImage);
        p.handleImage.color = new Color(Gold.r, Gold.g, Gold.b, 1f);
        p.handleImage.raycastTarget = true;
        p.handleImage.enabled = true;
        Dirty(p.handleImage);

        CanvasGroup cg = p.scrollbar.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            Save(cg);
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
            Dirty(cg);
        }
    }

    private static void ClearConflictingFixedHeights(Parts p)
    {
        foreach (string name in Cards)
        {
            Transform tr = p.content.Find(name);
            if (tr == null) throw new InvalidOperationException("카드를 찾을 수 없습니다: " + name);
            ReleaseHeightOverride(tr);
        }

        Transform ai = p.content.Find("AIFeedbackCard");
        if (ai.GetComponent<VerticalLayoutGroup>() == null)
            throw new InvalidOperationException("AIFeedbackCard에 VerticalLayoutGroup이 없습니다. 기존 v3 Apply를 먼저 확인하세요.");

        foreach (string name in AiTexts)
        {
            Transform tr = ai.Find(name);
            if (tr == null || tr.GetComponent<TMP_Text>() == null)
                throw new InvalidOperationException("AI 텍스트를 찾을 수 없습니다: " + name);
            ReleaseHeightOverride(tr);
        }
    }

    private static void ReleaseHeightOverride(Transform tr)
    {
        LayoutElement el = tr.GetComponent<LayoutElement>();
        if (el == null) return; // Keep the current hierarchy; do not add/remove components.

        // Unity's VerticalLayoutGroup should measure the real preferred text height.
        // Only remove *fixed vertical* LayoutElement overrides; keep width settings.
        if (el.ignoreLayout || el.minHeight >= 0f || el.preferredHeight >= 0f || el.flexibleHeight >= 0f)
        {
            Save(el);
            el.ignoreLayout = false;
            el.minHeight = -1f;
            el.preferredHeight = -1f;
            el.flexibleHeight = -1f;
            Dirty(el);
        }
    }

    private static bool FindParts(out Parts p)
    {
        p = new Parts();
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[15-D-8B v4] Play Mode를 종료한 후 메뉴를 실행하세요.");
            return false;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.name != "MainScene")
        {
            Debug.LogError("[15-D-8B v4] MainScene을 먼저 열어 주세요. 다른 Scene은 수정하지 않습니다.");
            return false;
        }

        int count = 0;
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            foreach (Transform tr in sceneRoot.GetComponentsInChildren<Transform>(true))
                if (tr.name == "WeeklyReportPanel") { p.root = tr; count++; }

        if (count != 1)
        {
            Debug.LogError("[15-D-8B v4] WeeklyReportPanel은 하나만 있어야 합니다. 현재: " + count);
            return false;
        }

        Transform scrollTr = p.root.Find("Scroll View");
        Transform viewTr = p.root.Find(ViewPath);
        Transform contentTr = p.root.Find(ContentPath);
        Transform barTr = p.root.Find(BarPath);
        Transform handleTr = barTr != null ? barTr.Find("Sliding Area/Handle") : null;

        if (scrollTr == null || viewTr == null || contentTr == null || barTr == null || handleTr == null)
        {
            Debug.LogError("[15-D-8B v4] Scroll View / Viewport / Content / Scrollbar Vertical / Handle 중 누락된 오브젝트가 있습니다. 변경 없음.");
            return false;
        }

        p.scroll = scrollTr.GetComponent<ScrollRect>();
        p.viewport = viewTr.GetComponent<RectTransform>();
        p.content = contentTr.GetComponent<RectTransform>();
        p.scrollbar = barTr.GetComponent<Scrollbar>();
        p.scrollbarRect = barTr.GetComponent<RectTransform>();
        p.handleImage = handleTr.GetComponent<Image>();
        p.layout = contentTr.GetComponent<VerticalLayoutGroup>();
        p.fitter = contentTr.GetComponent<ContentSizeFitter>();

        if (p.scroll == null || p.viewport == null || p.content == null ||
            p.scrollbar == null || p.scrollbarRect == null || p.handleImage == null ||
            p.layout == null || p.fitter == null)
        {
            Debug.LogError("[15-D-8B v4] 필수 ScrollRect/Scrollbar/Image/Layout 컴포넌트가 누락되었습니다. 변경 없음.");
            return false;
        }
        if (barTr.parent != scrollTr)
        {
            Debug.LogError("[15-D-8B v4] Scrollbar Vertical의 부모가 Scroll View가 아닙니다. 변경 없음.");
            return false;
        }
        return true;
    }

    private static void Save(UnityEngine.Object obj)
    {
        Undo.RecordObject(obj, "15-D-8B Scroll Fix v4");
    }

    private static void Dirty(UnityEngine.Object obj)
    {
        EditorUtility.SetDirty(obj);
    }

    private static Color Html(string hex)
    {
        Color result;
        return ColorUtility.TryParseHtmlString(hex, out result) ? result : Color.white;
    }
}
#endif
