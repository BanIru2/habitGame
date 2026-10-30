#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// HabitPVP 15-D-8B: editor-only WeeklyReportPanel style integration.
/// Does not save the Scene, unpack/apply prefabs, or change runtime manager bindings.
/// </summary>
public static class WeeklyReportUiIntegrationEditor
{
    private const string MenuBase = "Tools/HabitPVP/15-D-8B Weekly Report/";
    private const string PanelName = "WeeklyReportPanel";

    private static readonly Color Cream = Hex("#F8F2E5");
    private static readonly Color CardFill = Hex("#FFF9EF");
    private static readonly Color Gold = Hex("#C5A574");
    private static readonly Color Brown = Hex("#63462C");
    private static readonly Color Body = Hex("#3D2E22");
    private static readonly Color Highlight = Hex("#76501D");

    private static readonly string[] RequiredPaths =
    {
        "Header", "Header/TitleText", "Header/WeekText", "Header/BackButton",
        "Scroll View", "Scroll View/Viewport", "Scroll View/Viewport/Content",
        "Scroll View/Viewport/Content/HabitSummaryCard",
        "Scroll View/Viewport/Content/HabitSummaryCard/HabitTitleText",
        "Scroll View/Viewport/Content/HabitSummaryCard/HabitAchievementText",
        "Scroll View/Viewport/Content/HabitSummaryCard/HabitCompletedText",
        "Scroll View/Viewport/Content/HabitSummaryCard/HabitStreakText",
        "Scroll View/Viewport/Content/SpendingSummaryCard",
        "Scroll View/Viewport/Content/SpendingSummaryCard/SpendingTitleText",
        "Scroll View/Viewport/Content/SpendingSummaryCard/SpendingBudgetText",
        "Scroll View/Viewport/Content/SpendingSummaryCard/SpendingSpentText",
        "Scroll View/Viewport/Content/SpendingSummaryCard/SpendingUsageText",
        "Scroll View/Viewport/Content/AIFeedbackCard",
        "Scroll View/Viewport/Content/AIFeedbackCard/AITitleText",
        "Scroll View/Viewport/Content/AIFeedbackCard/AISummaryText",
        "Scroll View/Viewport/Content/AIFeedbackCard/AIGoodPointText",
        "Scroll View/Viewport/Content/AIFeedbackCard/AIImprovementText",
        "Scroll View/Viewport/Content/AIFeedbackCard/AINextWeekText"
    };

    private static readonly string[] ManagerFields =
    {
        "backButton", "weekText", "habitAchievementText", "habitCompletedText",
        "spendingBudgetText", "spendingSpentText", "spendingUsageText",
        "aiSummaryText", "aiGoodPointText", "aiImprovementText", "aiNextWeekText"
    };

    [MenuItem(MenuBase + "1. Validate")]
    private static void ValidateMenu()
    {
        Transform root;
        if (TryValidate(out root))
            Debug.Log("[15-D-8B] Validate PASS: scene hierarchy and manager references present. No changes made.");
    }

    [MenuItem(MenuBase + "2. Dry Run")]
    private static void DryRunMenu()
    {
        Transform root;
        if (!TryValidate(out root)) return;
        Debug.Log(
            "[15-D-8B] Dry Run PASS (no changes): recolor panel/cards, style header and text, " +
            "configure card/Content vertical layout for variable-length AI text, and clamp vertical scroll. " +
            "No manager edits, scene save, sprite asset edits, or prefab operations.");
    }

    [MenuItem(MenuBase + "3. Apply (No Auto Save)")]
    private static void ApplyMenu()
    {
        Transform root;
        if (!TryValidate(out root)) return;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("15-D-8B Weekly Report UI Renewal");

        try
        {
            StyleRoot(root);
            StyleHeader(root);
            StyleScroll(root);
            StyleCards(root);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("[15-D-8B v2] Apply finished (NOT saved). Readability and viewport color updated; check 1080x1920 Game view.");
        }
        catch (Exception ex)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogError("[15-D-8B] Apply failed; this operation was reverted: " + ex);
        }
    }

    [MenuItem(MenuBase + "4. Validate Applied Design")]
    private static void ValidateAppliedMenu()
    {
        Transform root;
        if (!TryValidate(out root)) return;
        Transform content = root.Find("Scroll View/Viewport/Content");
        VerticalLayoutGroup group = content.GetComponent<VerticalLayoutGroup>();
        ContentSizeFitter fit = content.GetComponent<ContentSizeFitter>();
        ScrollRect scroll = root.Find("Scroll View").GetComponent<ScrollRect>();
        Transform ai = root.Find("Scroll View/Viewport/Content/AIFeedbackCard");
        TMP_Text habitText = RequireTMP(root, "Scroll View/Viewport/Content/HabitSummaryCard/HabitAchievementText");
        TMP_Text aiText = RequireTMP(root, "Scroll View/Viewport/Content/AIFeedbackCard/AISummaryText");
        Image viewportImage = root.Find("Scroll View/Viewport").GetComponent<Image>();
        bool viewportTintOk = viewportImage == null ||
                              Mathf.Abs(viewportImage.color.r - Cream.r) < 0.01f &&
                              Mathf.Abs(viewportImage.color.g - Cream.g) < 0.01f &&
                              Mathf.Abs(viewportImage.color.b - Cream.b) < 0.01f;
        bool ok = group != null && group.childControlHeight && !group.childForceExpandHeight &&
                  fit != null && fit.verticalFit == ContentSizeFitter.FitMode.PreferredSize &&
                  scroll != null && scroll.vertical && !scroll.horizontal &&
                  ai.GetComponent<VerticalLayoutGroup>() != null &&
                  habitText.fontSize >= 39f && aiText.fontSize >= 38f && viewportTintOk;
        Debug.Log(ok
            ? "[15-D-8B v2] Applied Design PASS (static checks). Verify text readability, scrolling and TapBar in Game view."
            : "[15-D-8B v2] Applied Design FAIL. Inspect font sizes, Viewport Image color, Content layout and ScrollRect.");
    }


    // V3 is a narrowly-scoped visual pass. Use this menu instead of re-applying all v2 layouts.
    [MenuItem(MenuBase + "5. Apply Visual Polish v3 (No Auto Save)")]
    private static void ApplyVisualPolishMenu()
    {
        Transform root;
        if (!TryValidate(out root)) return;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("15-D-8B Weekly Report Visual Polish v3");
        try
        {
            ApplySolidBackground(root, "WeeklyReportPanel");
            ApplySolidBackground(root.Find("Scroll View"), "Scroll View");
            ApplySolidBackground(root.Find("Scroll View/Viewport"), "Viewport");
            PolishAiReadability(root);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("[15-D-8B v3] Visual polish applied (NOT saved): solid cream backgrounds and improved AI text contrast. Layout/API/Manager/TabBar unchanged.");
        }
        catch (Exception ex)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogError("[15-D-8B v3] Visual polish failed; changes reverted: " + ex);
        }
    }

    [MenuItem(MenuBase + "6. Validate Visual Polish v3")]
    private static void ValidateVisualPolishMenu()
    {
        Transform root;
        if (!TryValidate(out root)) return;
        bool ok = true;
        Transform[] backgrounds = { root, root.Find("Scroll View"), root.Find("Scroll View/Viewport") };
        foreach (Transform tr in backgrounds)
        {
            Image img = tr.GetComponent<Image>();
            if (img == null) continue;
            Color c = img.color;
            if (img.sprite != null || img.type != Image.Type.Simple ||
                Mathf.Abs(c.r - Cream.r) > 0.02f ||
                Mathf.Abs(c.g - Cream.g) > 0.02f ||
                Mathf.Abs(c.b - Cream.b) > 0.02f || c.a < 0.99f)
            {
                Debug.LogWarning("[15-D-8B v3] Background check failed on " + tr.name);
                ok = false;
            }
        }

        const string prefix = "Scroll View/Viewport/Content/AIFeedbackCard/";
        foreach (string name in new[] { "AISummaryText", "AIGoodPointText", "AIImprovementText", "AINextWeekText" })
        {
            TMP_Text tmp = RequireTMP(root, prefix + name);
            if (tmp.fontSize < 43f || tmp.alpha < 0.99f || tmp.enableVertexGradient ||
                (tmp.fontStyle & FontStyles.Bold) == 0)
            {
                Debug.LogWarning("[15-D-8B v3] AI readability check failed on " + name);
                ok = false;
            }
        }
        Debug.Log(ok
            ? "[15-D-8B v3] Visual Polish PASS (static checks). Inspect the actual Game view for the background, contrast, and scroll."
            : "[15-D-8B v3] Visual Polish FAIL; see warnings. No changes made by validation.");
    }

    private static void ApplySolidBackground(Transform target, string label)
    {
        if (target == null) throw new InvalidOperationException("Missing background target: " + label);
        Image img = target.GetComponent<Image>();
        if (img == null)
        {
            Debug.Log("[15-D-8B v3] " + label + " has no Image; leaving it unchanged.");
            return;
        }
        // An Image color multiplies its source sprite pixels. A dark/grey sprite can still
        // look grey even when tinted cream. Use a solid UI Image on backgrounds ONLY.
        // Preserve Image, Mask/RectMask2D, ScrollRect, raycastTarget and all references.
        Record(img);
        img.sprite = null;
        img.type = Image.Type.Simple;
        img.color = new Color(Cream.r, Cream.g, Cream.b, 1f);
        Dirty(img);
    }

    private static void PolishAiReadability(Transform root)
    {
        const string prefix = "Scroll View/Viewport/Content/AIFeedbackCard/";
        foreach (string name in new[] { "AISummaryText", "AIGoodPointText", "AIImprovementText", "AINextWeekText" })
        {
            TMP_Text tmp = RequireTMP(root, prefix + name);
            Record(tmp);
            tmp.fontSize = 44f;
            tmp.color = Body;
            tmp.alpha = 1f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableVertexGradient = false;
            tmp.enableAutoSizing = false;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.lineSpacing = 8f;
            tmp.paragraphSpacing = 8f;
            Dirty(tmp);
        }
    }

    // v6: ScrollRect + content sizing correction, in this Editor script only.
    // Existing v2/v3 design and all report data bindings are left untouched.
    private const string ScrollPathV6 = "Scroll View";
    private const string ViewPathV6 = "Scroll View/Viewport";
    private const string ContentPathV6 = "Scroll View/Viewport/Content";
    private static readonly string[] CardNamesV6 =
    {
        "HabitSummaryCard", "SpendingSummaryCard", "AIFeedbackCard"
    };

    private struct ScrollPartsV6
    {
        public Transform root;
        public ScrollRect scroll;
        public RectTransform scrollRect;
        public RectTransform viewport;
        public RectTransform content;
        public VerticalLayoutGroup contentLayout;
        public ContentSizeFitter contentFitter;
        public Scrollbar scrollbar;
        public RectTransform scrollbarRect;
        public Image handleImage;
    }

    [MenuItem(MenuBase + "15. Validate Scroll Layout v6")]
    private static void ValidateScrollV6()
    {
        ScrollPartsV6 p;
        if (!FindScrollPartsV6(out p)) return;
        LogScrollDimensionsV6(p, "Validate (read-only)");
        Debug.Log("[15-D-8B v6] Validate PASS: ScrollRect, scrollbar/handle, 3 cards and layout components found. No changes made.");
    }

    [MenuItem(MenuBase + "16. Dry Run Scroll Layout v6")]
    private static void DryRunScrollV6()
    {
        ScrollPartsV6 p;
        if (!FindScrollPartsV6(out p)) return;
        float targetInset = GetBottomInsetV6(p);
        float predictedHeight = GetCardsRequiredHeightV6(p, false);
        Debug.Log("[15-D-8B v6] Dry Run (no changes): scroll bottom inset=" +
                  targetInset.ToString("F1") + ", text-based Content required height=" +
                  predictedHeight.ToString("F1") + ". Will link Viewport/Content/Scrollbar, " +
                  "enable wheel + drag, restore vertical layout and reserve space above TapBar. " +
                  "No runtime script, manager changes, scene save, or prefab operation.");
    }

    [MenuItem(MenuBase + "17. Apply Scroll Layout v6 (No Auto Save)")]
    private static void ApplyScrollV6()
    {
        ScrollPartsV6 p;
        if (!FindScrollPartsV6(out p)) return;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("15-D-8B Weekly Report Scroll Layout v6");
        try
        {
            // Important: a viewport hidden underneath the bottom TapBar gives a
            // full-size scrollbar even when the last lines are not actually visible.
            float bottomInset = GetBottomInsetV6(p);
            Record(p.scrollRect);
            Vector2 min = p.scrollRect.offsetMin;
            p.scrollRect.offsetMin = new Vector2(min.x, bottomInset);
            Dirty(p.scrollRect);

            Record(p.viewport);
            p.viewport.anchorMin = Vector2.zero;
            p.viewport.anchorMax = Vector2.one;
            p.viewport.offsetMin = Vector2.zero;
            p.viewport.offsetMax = new Vector2(-34f, 0f);
            Dirty(p.viewport);

            if (p.viewport.GetComponent<RectMask2D>() == null &&
                p.viewport.GetComponent<Mask>() == null)
                Undo.AddComponent<RectMask2D>(p.viewport.gameObject);

            // Use only ContentSizeFitter on Content. Do NOT put a ContentSizeFitter
            // on cards because the parent layout also controls their height.
            Record(p.content);
            p.content.anchorMin = new Vector2(0f, 1f);
            p.content.anchorMax = new Vector2(1f, 1f);
            p.content.pivot = new Vector2(0.5f, 1f);
            p.content.anchoredPosition = Vector2.zero;
            Dirty(p.content);

            Record(p.contentLayout);
            p.contentLayout.childControlWidth = true;
            p.contentLayout.childControlHeight = true;
            p.contentLayout.childForceExpandWidth = true;
            p.contentLayout.childForceExpandHeight = false;
            p.contentLayout.childAlignment = TextAnchor.UpperCenter;
            // Just enough after the last card to reveal its bottom border.
            RectOffset oldPadding = p.contentLayout.padding;
            p.contentLayout.padding = new RectOffset(oldPadding.left, oldPadding.right,
                                                    oldPadding.top, 18);
            Dirty(p.contentLayout);

            Record(p.contentFitter);
            p.contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            p.contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Dirty(p.contentFitter);

            // TMP and each card's VerticalLayoutGroup are native ILayoutElements.
            // Clear old LayoutElement height overrides: a fixed preferredHeight
            // can take priority over the card's dynamic preferred height and
            // prevent future longer/shorter server responses from resizing it.
            float measured = GetCardsRequiredHeightV6(p, true);

            Record(p.scroll);
            p.scroll.enabled = true;
            p.scroll.content = p.content;
            p.scroll.viewport = p.viewport;
            p.scroll.horizontal = false;
            p.scroll.vertical = true;
            p.scroll.movementType = ScrollRect.MovementType.Clamped;
            p.scroll.inertia = false;
            p.scroll.scrollSensitivity = 85f;
            p.scroll.verticalScrollbar = p.scrollbar;
            p.scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            p.scroll.verticalScrollbarSpacing = 4f;
            Dirty(p.scroll);

            Image input = p.scroll.GetComponent<Image>();
            if (input == null) input = Undo.AddComponent<Image>(p.scroll.gameObject);
            Record(input);
            input.raycastTarget = true;
            Dirty(input);

            if (!p.scrollbar.gameObject.activeSelf)
            {
                Undo.RecordObject(p.scrollbar.gameObject, "Enable weekly report scrollbar");
                p.scrollbar.gameObject.SetActive(true);
                Dirty(p.scrollbar.gameObject);
            }
            Record(p.scrollbarRect);
            p.scrollbarRect.anchorMin = new Vector2(1f, 0f);
            p.scrollbarRect.anchorMax = new Vector2(1f, 1f);
            p.scrollbarRect.pivot = new Vector2(1f, 0.5f);
            p.scrollbarRect.anchoredPosition = new Vector2(-5f, 0f);
            p.scrollbarRect.sizeDelta = new Vector2(22f, -16f);
            Dirty(p.scrollbarRect);

            Record(p.scrollbar);
            p.scrollbar.enabled = true;
            p.scrollbar.interactable = true;
            p.scrollbar.direction = Scrollbar.Direction.BottomToTop;
            p.scrollbar.numberOfSteps = 0;
            p.scrollbar.handleRect = p.handleImage.rectTransform;
            Dirty(p.scrollbar);

            Record(p.handleImage);
            p.handleImage.enabled = true;
            p.handleImage.raycastTarget = true;
            Dirty(p.handleImage);
            Image track = p.scrollbar.GetComponent<Image>();
            if (track != null)
            {
                Record(track);
                track.raycastTarget = true;
                Dirty(track);
            }
            CanvasGroup group = p.scrollbar.GetComponent<CanvasGroup>();
            if (group != null)
            {
                Record(group);
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
                Dirty(group);
            }

            // Rebuild only the affected UI hierarchy when it is active. Do not
            // call Canvas.ForceUpdateCanvases or touch TMP-generated SubMeshes.
            if (p.content.gameObject.activeInHierarchy)
            {
                foreach (string name in CardNamesV6)
                {
                    RectTransform card = p.content.Find(name) as RectTransform;
                    if (card != null) LayoutRebuilder.ForceRebuildLayoutImmediate(card);
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate(p.content);
            }

            EditorSceneManager.MarkSceneDirty(p.root.gameObject.scene);
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("[15-D-8B v6] Apply PASS (not saved). Predicted Content height=" +
                      measured.ToString("F1") + "; bottom inset=" +
                      bottomInset.ToString("F1") + ". Test wheel and scrollbar drag IN PLAY MODE.");
            LogScrollDimensionsV6(p, "After Apply (layout sizes may remain stale if panel inactive)");
        }
        catch (Exception ex)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogError("[15-D-8B v6] Apply failed; Undo rollback attempted: " + ex);
        }
    }

    [MenuItem(MenuBase + "18. Validate Applied Scroll Layout v6")]
    private static void ValidateAppliedScrollV6()
    {
        ScrollPartsV6 p;
        if (!FindScrollPartsV6(out p)) return;
        bool connected = p.scroll.content == p.content &&
                         p.scroll.viewport == p.viewport &&
                         p.scroll.verticalScrollbar == p.scrollbar;
        bool input = p.scroll.enabled && p.scroll.vertical && !p.scroll.horizontal &&
                     p.scroll.scrollSensitivity > 0f &&
                     p.scrollbar.enabled && p.scrollbar.interactable &&
                     p.scrollbar.gameObject.activeSelf &&
                     p.scrollbar.handleRect == p.handleImage.rectTransform &&
                     p.handleImage.raycastTarget;
        bool layout = p.contentLayout.childControlHeight &&
                      !p.contentLayout.childForceExpandHeight &&
                      p.contentFitter.verticalFit == ContentSizeFitter.FitMode.PreferredSize;
        bool viewport = p.viewport.GetComponent<RectMask2D>() != null ||
                        p.viewport.GetComponent<Mask>() != null;
        bool cards = true;
        foreach (string name in CardNamesV6)
        {
            Transform card = p.content.Find(name);
            if (card.GetComponent<VerticalLayoutGroup>() == null) cards = false;
            LayoutElement el = card.GetComponent<LayoutElement>();
            if (el != null && (el.ignoreLayout || el.minHeight >= 0f ||
                               el.preferredHeight >= 0f || el.flexibleHeight >= 0f))
                cards = false;
            foreach (Transform child in card)
            {
                if (child.GetComponent<TMP_Text>() == null) continue;
                LayoutElement textEl = child.GetComponent<LayoutElement>();
                if (textEl != null && (textEl.ignoreLayout || textEl.minHeight >= 0f ||
                                      textEl.preferredHeight >= 0f || textEl.flexibleHeight >= 0f))
                    cards = false;
            }
        }
        LogScrollDimensionsV6(p, "Validate Applied");
        if (connected && input && layout && viewport && cards)
            Debug.Log("[15-D-8B v6] Static Validate PASS. Test in Play Mode: scrollbar drag, mouse wheel, and bottom AI text.");
        else
            Debug.LogError("[15-D-8B v6] Static Validate FAIL: links=" + connected +
                           ", input=" + input + ", layout=" + layout +
                           ", mask=" + viewport + ", cards=" + cards);
    }

    // No global scene search: only use the existing WeeklyReportPanel in MainScene.
    private static bool FindScrollPartsV6(out ScrollPartsV6 p)
    {
        p = new ScrollPartsV6();
        Transform root;
        if (!TryValidate(out root)) return false;
        p.root = root;
        Transform scrollObject = root.Find(ScrollPathV6);
        Transform viewportObject = root.Find(ViewPathV6);
        Transform contentObject = root.Find(ContentPathV6);
        Transform scrollbarObject = scrollObject.Find("Scrollbar Vertical");
        if (scrollbarObject == null)
        {
            Debug.LogError("[15-D-8B v6] Existing Scrollbar Vertical is missing. Do not create duplicates.");
            return false;
        }
        Transform handle = scrollbarObject.Find("Sliding Area/Handle");
        if (handle == null)
        {
            Debug.LogError("[15-D-8B v6] Existing Scrollbar Vertical/Sliding Area/Handle is missing.");
            return false;
        }
        p.scroll = scrollObject.GetComponent<ScrollRect>();
        p.scrollRect = scrollObject as RectTransform;
        p.viewport = viewportObject as RectTransform;
        p.content = contentObject as RectTransform;
        p.contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
        p.contentFitter = contentObject.GetComponent<ContentSizeFitter>();
        p.scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        p.scrollbarRect = scrollbarObject as RectTransform;
        p.handleImage = handle.GetComponent<Image>();
        if (p.scroll == null || p.scrollRect == null || p.viewport == null || p.content == null ||
            p.contentLayout == null || p.contentFitter == null || p.scrollbar == null ||
            p.scrollbarRect == null || p.handleImage == null)
        {
            Debug.LogError("[15-D-8B v6] ScrollRect/RectTransform/Scrollbar/Image/Layout component missing.");
            return false;
        }
        foreach (string name in CardNamesV6)
        {
            Transform card = p.content.Find(name);
            if (card == null || card.GetComponent<VerticalLayoutGroup>() == null)
            {
                Debug.LogError("[15-D-8B v6] Card or VerticalLayoutGroup missing: " + name);
                return false;
            }
        }
        return true;
    }

    // Reserve the area covered by the bottom TabBar if present. This is a
    // scene-time layout inset; no changes to TabBar, MainTabManager or navigation.
    private static float GetBottomInsetV6(ScrollPartsV6 p)
    {
        float inset = 20f;
        Canvas canvas = p.root.GetComponentInParent<Canvas>();
        if (canvas == null) return inset;
        foreach (RectTransform candidate in canvas.GetComponentsInChildren<RectTransform>(true))
        {
            if (candidate.name != "TapBar") continue;
            RectTransform rootRect = p.root as RectTransform;
            if (rootRect == null) return inset;
            Vector3[] corners = new Vector3[4];
            candidate.GetWorldCorners(corners);
            float topFromBottom = rootRect.InverseTransformPoint(corners[1]).y - rootRect.rect.yMin;
            if (topFromBottom > 0f && topFromBottom < rootRect.rect.height * 0.4f)
                inset = Mathf.Max(inset, topFromBottom + 12f);
            break;
        }
        return inset;
    }

    // Returns an independent text-size estimate for diagnostics. When apply==true,
    // releases stale fixed-height overrides on texts and cards. Their existing
    // VerticalLayoutGroups + ContentSizeFitter then handle future text lengths.
    // No TMP_SubMeshUI or child hierarchy changes are made.
    private static float GetCardsRequiredHeightV6(ScrollPartsV6 p, bool apply)
    {
        float result = p.contentLayout.padding.top + (apply ? 18f : p.contentLayout.padding.bottom);
        int activeCards = 0;
        float contentWidth = Mathf.Max(140f, p.viewport.rect.width -
                              p.contentLayout.padding.left - p.contentLayout.padding.right);
        foreach (string name in CardNamesV6)
        {
            Transform card = p.content.Find(name);
            if (!card.gameObject.activeSelf) continue;
            VerticalLayoutGroup layout = card.GetComponent<VerticalLayoutGroup>();
            float textWidth = Mathf.Max(80f, contentWidth - layout.padding.left - layout.padding.right);
            float cardHeight = layout.padding.top + layout.padding.bottom;
            int textCount = 0;
            foreach (Transform child in card)
            {
                if (!child.gameObject.activeSelf) continue;
                TMP_Text tmp = child.GetComponent<TMP_Text>();
                if (tmp == null) continue;
                if (apply)
                {
                    LayoutElement existing = child.GetComponent<LayoutElement>();
                    if (existing != null)
                    {
                        Record(existing);
                        existing.ignoreLayout = false;
                        existing.minHeight = -1f;
                        existing.preferredHeight = -1f;
                        existing.flexibleHeight = -1f;
                        Dirty(existing);
                    }
                }
                Vector2 preferred = tmp.GetPreferredValues(textWidth, Mathf.Infinity);
                float textHeight = preferred.y;
                if (float.IsNaN(textHeight) || float.IsInfinity(textHeight) || textHeight <= 0f)
                    textHeight = tmp.fontSize * 1.35f;
                cardHeight += Mathf.Ceil(textHeight + 4f);
                textCount++;
            }
            if (textCount > 1) cardHeight += (textCount - 1) * layout.spacing;
            cardHeight = Mathf.Ceil(cardHeight + 8f);

            if (apply)
            {
                Record(layout);
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                Dirty(layout);

                LayoutElement element = card.GetComponent<LayoutElement>();
                if (element != null)
                {
                    Record(element);
                    element.ignoreLayout = false;
                    element.minHeight = -1f;
                    element.preferredHeight = -1f;
                    element.flexibleHeight = -1f;
                    Dirty(element);
                }
            }
            result += cardHeight;
            activeCards++;
        }
        if (activeCards > 1) result += (activeCards - 1) * p.contentLayout.spacing;
        return Mathf.Ceil(result);
    }

    private static void LogScrollDimensionsV6(ScrollPartsV6 p, string phase)
    {
        float contentH = p.content.rect.height;
        float viewportH = p.viewport.rect.height;
        float calculated = GetCardsRequiredHeightV6(p, false);
        Debug.Log("[15-D-8B v6] " + phase +
                  ": ContentH=" + contentH.ToString("F1") +
                  ", ViewportH=" + viewportH.ToString("F1") +
                  ", EstimatedTextContentH=" + calculated.ToString("F1") +
                  ", estimatedOverflow=" + Mathf.Max(0f, calculated - viewportH).ToString("F1") +
                  ", runtimeOverflow=" + Mathf.Max(0f, contentH - viewportH).ToString("F1") +
                  ". Inactive Edit Mode sizes may be stale; verify Play Mode.");
    }

    private static bool TryValidate(out Transform root)
    {
        root = null;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[15-D-8B] Exit Play Mode before using this tool.");
            return false;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("[15-D-8B] Open MainScene first.");
            return false;
        }
        if (scene.name != "MainScene")
        {
            Debug.LogError("[15-D-8B] Active scene is " + scene.name + "; expected MainScene. Nothing changed.");
            return false;
        }

        var matches = new List<Transform>();
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in sceneRoot.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == PanelName) matches.Add(candidate);
            }
        }
        if (matches.Count != 1)
        {
            Debug.LogError("[15-D-8B] Expected exactly one WeeklyReportPanel, found " + matches.Count);
            return false;
        }
        root = matches[0];

        foreach (string path in RequiredPaths)
        {
            if (root.Find(path) == null)
            {
                Debug.LogError("[15-D-8B] Missing required object: " + path);
                return false;
            }
        }
        var manager = root.GetComponent<WeeklyReportManager>();
        if (manager == null)
        {
            Debug.LogError("[15-D-8B] Missing WeeklyReportManager.");
            return false;
        }
        SerializedObject so = new SerializedObject(manager);
        foreach (string field in ManagerFields)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null || p.objectReferenceValue == null)
            {
                Debug.LogError("[15-D-8B] Missing serialized manager reference: " + field);
                return false;
            }
        }
        if (root.Find("Scroll View").GetComponent<ScrollRect>() == null ||
            root.Find("Scroll View/Viewport/Content").GetComponent<VerticalLayoutGroup>() == null ||
            root.Find("Scroll View/Viewport/Content").GetComponent<ContentSizeFitter>() == null)
        {
            Debug.LogError("[15-D-8B] Expected ScrollRect + Content VerticalLayoutGroup/ContentSizeFitter. Nothing changed.");
            return false;
        }
        return true;
    }

    private static void StyleRoot(Transform root)
    {
        Image background = GetOrAdd<Image>(root.gameObject);
        Record(background);
        background.color = Cream;
        background.raycastTarget = false;
        Dirty(background);
    }

    private static void StyleHeader(Transform root)
    {
        Transform header = root.Find("Header");
        SetTopStretch(header.GetComponent<RectTransform>(), 208f);

        TMP_Text title = RequireTMP(root, "Header/TitleText");
        StyleText(title, 49f, Brown, true, TextAlignmentOptions.Center);
        Record(title);
        title.text = "주간 성장 리포트";
        Dirty(title);
        SetTopBand(title.rectTransform, 36f, 70f, 155f, 155f);

        TMP_Text week = RequireTMP(root, "Header/WeekText");
        StyleText(week, 38f, Brown, true, TextAlignmentOptions.Center);
        SetTopBand(week.rectTransform, 125f, 48f, 145f, 145f);

        Transform back = root.Find("Header/BackButton");
        RectTransform backRect = back.GetComponent<RectTransform>();
        Record(backRect);
        backRect.anchorMin = new Vector2(0, 1);
        backRect.anchorMax = new Vector2(0, 1);
        backRect.pivot = new Vector2(0, 1);
        backRect.anchoredPosition = new Vector2(32, -35);
        backRect.sizeDelta = new Vector2(110, 86);
        Dirty(backRect);

        Image image = back.GetComponent<Image>();
        if (image != null)
        {
            Record(image);
            image.color = Gold;
            Dirty(image);
        }
        TMP_Text buttonText = back.GetComponentInChildren<TMP_Text>(true);
        if (buttonText != null)
        {
            StyleText(buttonText, 33f, Brown, true, TextAlignmentOptions.Center);
            Record(buttonText);
            buttonText.text = "뒤로";
            Dirty(buttonText);
        }
    }

    private static void StyleScroll(Transform root)
    {
        Transform scrollTransform = root.Find("Scroll View");
        RectTransform rect = scrollTransform.GetComponent<RectTransform>();
        Record(rect);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(24f, 20f);
        rect.offsetMax = new Vector2(-24f, -215f);
        Dirty(rect);

        Image scrollBackground = scrollTransform.GetComponent<Image>();
        if (scrollBackground != null)
        {
            Record(scrollBackground);
            scrollBackground.color = Cream;
            Dirty(scrollBackground);
        }

        // The Viewport may have its own Image over the Scroll View background.
        // Tint only the existing Image: preserve Mask/RectMask2D and raycast settings.
        Transform viewport = root.Find("Scroll View/Viewport");
        Image viewportImage = viewport.GetComponent<Image>();
        if (viewportImage != null)
        {
            Record(viewportImage);
            viewportImage.color = Cream;
            Dirty(viewportImage);
        }

        ScrollRect scroll = scrollTransform.GetComponent<ScrollRect>();
        Record(scroll);
        scroll.vertical = true;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        Dirty(scroll);

        Transform content = root.Find("Scroll View/Viewport/Content");
        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        Record(vlg);
        vlg.padding = new RectOffset(15, 15, 12, 36);
        vlg.spacing = 24f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        Dirty(vlg);

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        Record(fitter);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        Dirty(fitter);
    }

    private static void StyleCards(Transform root)
    {
        const string prefix = "Scroll View/Viewport/Content/";
        StyleCard(root, prefix + "HabitSummaryCard", 16f);
        StyleCard(root, prefix + "SpendingSummaryCard", 16f);
        StyleCard(root, prefix + "AIFeedbackCard", 27f);

        StyleStaticHeading(root, prefix + "HabitSummaryCard/HabitTitleText", "생활습관 달성 현황");
        StyleStaticHeading(root, prefix + "SpendingSummaryCard/SpendingTitleText", "주간 소비 현황");
        StyleStaticHeading(root, prefix + "AIFeedbackCard/AITitleText", "AI 주간 성장 분석");

        string[] habit = { "HabitAchievementText", "HabitCompletedText", "HabitStreakText" };
        foreach (string name in habit)
            StyleText(RequireTMP(root, prefix + "HabitSummaryCard/" + name), 39f, Body, true, TextAlignmentOptions.TopLeft);

        string[] spending = { "SpendingBudgetText", "SpendingSpentText", "SpendingUsageText" };
        foreach (string name in spending)
            StyleText(RequireTMP(root, prefix + "SpendingSummaryCard/" + name), 39f, Body, true, TextAlignmentOptions.TopLeft);

        string[] ai = { "AISummaryText", "AIGoodPointText", "AIImprovementText", "AINextWeekText" };
        foreach (string name in ai)
        {
            TMP_Text feedback = RequireTMP(root, prefix + "AIFeedbackCard/" + name);
            StyleText(feedback, 38f, Body, false, TextAlignmentOptions.TopLeft);
            Record(feedback);
            feedback.lineSpacing = 5f;
            Dirty(feedback);
        }
    }

    private static void StyleCard(Transform root, string path, float spacing)
    {
        GameObject go = root.Find(path).gameObject;
        Image image = GetOrAdd<Image>(go);
        Record(image);
        image.color = CardFill;
        image.raycastTarget = false;
        // Do not replace the existing sprite. Its rounded corners (if any) remain intact.
        Dirty(image);

        Outline border = GetOrAdd<Outline>(go);
        Record(border);
        border.effectColor = Gold;
        border.effectDistance = new Vector2(2.5f, -2.5f);
        border.useGraphicAlpha = true;
        Dirty(border);

        VerticalLayoutGroup group = GetOrAdd<VerticalLayoutGroup>(go);
        Record(group);
        group.padding = new RectOffset(34, 34, 33, 33);
        group.spacing = spacing;
        group.childAlignment = TextAnchor.UpperLeft;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;
        Dirty(group);
    }

    private static void StyleStaticHeading(Transform root, string path, string title)
    {
        TMP_Text heading = RequireTMP(root, path);
        StyleText(heading, 42f, Highlight, true, TextAlignmentOptions.TopLeft);
        Record(heading);
        heading.text = title;
        Dirty(heading);
    }

    private static TMP_Text RequireTMP(Transform root, string path)
    {
        TMP_Text tmp = root.Find(path).GetComponent<TMP_Text>();
        if (tmp == null) throw new InvalidOperationException("Missing TMP_Text on " + path);
        return tmp;
    }

    private static void StyleText(TMP_Text tmp, float size, Color color, bool bold, TextAlignmentOptions alignment)
    {
        Record(tmp);
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alpha = 1f;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = alignment;
        tmp.enableAutoSizing = false;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.richText = true;
        Dirty(tmp);
    }

    private static void SetTopStretch(RectTransform rect, float height)
    {
        Record(rect);
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0, height);
        Dirty(rect);
    }

    private static void SetTopBand(RectTransform rect, float top, float height, float left, float right)
    {
        Record(rect);
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.offsetMin = new Vector2(left, -top - height);
        rect.offsetMax = new Vector2(-right, -top);
        Dirty(rect);
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        return comp != null ? comp : Undo.AddComponent<T>(go);
    }

    private static void Record(UnityEngine.Object obj)
    {
        Undo.RecordObject(obj, "15-D-8B Weekly Report UI Renewal");
    }

    private static void Dirty(UnityEngine.Object obj)
    {
        EditorUtility.SetDirty(obj);
    }

    private static Color Hex(string html)
    {
        Color color;
        return ColorUtility.TryParseHtmlString(html, out color) ? color : Color.white;
    }
}
#endif
