using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PhotoPopupIntegrationEditor
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string CanvasPrefabPath = "Assets/Resources/UIPrefabs/Canvas.prefab";
    private const string PopupRootPath = "HabitTap/PhotoVerificationPopupRoot";
    private const string FontPath = "Assets/Login/Fonts/NotoSansKR Login SDF.asset";
    private const string CardSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/panel_rpg_card.png";
    private const string CreamButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_cream.png";
    private const string GoldButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_rpg.png";
    private const string MenuRoot = "Tools/HabitPVP/UI Renewal/MainScene/";

    private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);
    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x86, 0x68, 0x5D, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);
    private static readonly Color32 RewardBrown = new Color32(0x8A, 0x55, 0x16, 0xFF);
    private static readonly Color32 Backdrop = new Color32(0x2A, 0x1B, 0x1D, 0xB8);
    private static readonly Color32 Disabled = new Color32(0xB9, 0xAA, 0x91, 0xB8);

    [MenuItem(MenuRoot + "Validate PHOTO Popup", false, 2240)]
    private static void ValidateMenu()
    {
        Context context = Validate(true);
        if (!context.IsValid) return;

        Debug.Log(
            "[HabitPVP MainScene] PHOTO Popup Pre-Validate 성공. 실제 MainScene Hierarchy, " +
            "PhotoVerificationManager 15개 SerializeField, 4개 Button UnityEvent, activeSelf, " +
            "CanvasGroup 및 TapBar 구조가 유효합니다.\n" + Diagnostics(context), context.Root);

        if (HasRenewalObjects(context))
        {
            ValidatePostLayout(context);
            Debug.Log("[HabitPVP MainScene] PHOTO Popup Post-Validate도 통과했습니다.", context.Root);
        }
        else
        {
            Debug.LogWarning(
                "[HabitPVP MainScene] PHOTO Popup은 아직 MainScene 리뉴얼 레이아웃이 아닙니다. " +
                "Apply 이후 Post-Validate에서 레이아웃을 필수 검사합니다.", context.Root);
        }
    }

    [MenuItem(MenuRoot + "Dry Run PHOTO Popup", false, 2241)]
    private static void DryRunMenu()
    {
        Context context = Validate(true);
        if (!context.IsValid)
        {
            Debug.LogError("[HabitPVP MainScene] PHOTO Popup Dry Run 중단: 검증 오류를 해결해 주세요.");
            return;
        }

        Debug.Log(
            "[HabitPVP MainScene] PHOTO Popup Dry Run (변경 없음)\n" +
            "대상: Canvas/" + PopupRootPath + "\n" + Diagnostics(context) + "\n" +
            "적용 예정: Root Dim Image, 입력/진행/결과 RPG 카드, 사진 미리보기 골드 테두리, " +
            "NotoSansKR 텍스트, 습관명 표시, 120px 버튼 터치 영역\n" +
            "런타임: 열 때만 Dim/Popup 표시 및 TapBar 상태 저장 후 숨김, 닫을 때 저장 상태 복원\n" +
            "보존: PhotoVerificationManager SerializeField, 모든 Button UnityEvent, activeSelf, " +
            "CanvasGroup, sibling 순서, 기존 NativeCamera/API/Gemini/완료 처리\n" +
            "Scene 자동 저장 및 Canvas.prefab Apply/Revert 없음", context.Root);
    }

    [MenuItem(MenuRoot + "Apply PHOTO Popup", false, 2242)]
    private static void ApplyMenu()
    {
        Context context = Validate(true);
        if (!context.IsValid)
        {
            Debug.LogError("[HabitPVP MainScene] PHOTO Popup Apply 중단: 검증 오류를 해결해 주세요.");
            return;
        }

        ProtectionSnapshot snapshot = ProtectionSnapshot.Capture(context);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply MainScene PHOTO Popup UI Renewal");

        try
        {
            ApplyStyle(context);
            snapshot.AssertUnchanged(context);
            ValidatePostLayout(context);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = context.Root;
            Debug.Log(
                "[HabitPVP MainScene] PHOTO Popup 적용 완료. Scene은 저장하지 않았습니다. " +
                "Hierarchy/Inspector 확인 후 사용자가 직접 저장해 주세요. Edit > Undo를 지원합니다.",
                context.Root);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogException(exception);
            Debug.LogError("[HabitPVP MainScene] PHOTO Popup Apply 실패. 이번 Undo Group을 되돌렸습니다.");
        }
    }

    [MenuItem(MenuRoot + "Apply PHOTO Popup", true)]
    private static bool CanApply()
    {
        Scene scene = SceneManager.GetActiveScene();
        return !EditorApplication.isPlayingOrWillChangePlaymode && scene.IsValid() && scene.isLoaded &&
               string.Equals(scene.path, ScenePath, StringComparison.Ordinal);
    }

    private static Context Validate(bool log)
    {
        Context context = new Context { Scene = SceneManager.GetActiveScene() };
        if (!context.Scene.IsValid() || !context.Scene.isLoaded ||
            !string.Equals(context.Scene.path, ScenePath, StringComparison.Ordinal))
        {
            context.Errors.Add("정확한 MainScene을 활성 Scene으로 연 Edit Mode에서만 실행할 수 있습니다.");
            return Finish(context, log);
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            context.Errors.Add("Play Mode 또는 전환 중에는 실행할 수 없습니다.");
        }

        GameObject[] canvases = context.Scene.GetRootGameObjects().Where(item => item.name == "Canvas").ToArray();
        if (canvases.Length != 1)
        {
            context.Errors.Add("Scene 루트 Canvas가 정확히 하나여야 합니다. 현재: " + canvases.Length);
            return Finish(context, log);
        }

        context.Canvas = canvases[0];
        context.CanvasScaler = context.Canvas.GetComponent<CanvasScaler>();
        if (PrefabUtility.GetOutermostPrefabInstanceRoot(context.Canvas) != context.Canvas ||
            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(context.Canvas) != CanvasPrefabPath)
        {
            context.Errors.Add("Canvas가 예상 Canvas.prefab의 최상위 MainScene instance가 아닙니다.");
        }
        if (context.CanvasScaler == null ||
            context.CanvasScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
            !Approximately(context.CanvasScaler.referenceResolution, ReferenceResolution))
        {
            context.Errors.Add("CanvasScaler가 Scale With Screen Size / 1080x1920 기준이 아닙니다.");
        }

        Transform root = context.Canvas.transform.Find(PopupRootPath);
        if (root == null)
        {
            context.Errors.Add("대상 경로가 없습니다: Canvas/" + PopupRootPath);
            return Finish(context, log);
        }

        context.Root = root.gameObject;
        context.RootRect = root as RectTransform;
        context.Manager = root.GetComponent<PhotoVerificationManager>();
        context.TapBar = context.Canvas.transform.Find("TapBar")?.gameObject;
        if (context.Manager == null)
        {
            context.Errors.Add("PhotoVerificationPopupRoot에 PhotoVerificationManager가 없습니다.");
            return Finish(context, log);
        }
        if (!PrefabUtility.IsPartOfPrefabInstance(context.Root))
        {
            context.Errors.Add("PHOTO Popup Root가 Canvas Prefab instance에 속하지 않습니다.");
        }
        if (context.TapBar == null)
        {
            context.Errors.Add("PHOTO Popup 표시 중 숨길 Canvas/TapBar가 없습니다.");
        }

        SerializedObject manager = new SerializedObject(context.Manager);
        context.PhotoPopup = Read<GameObject>(manager, "photoVerificationPopup", context);
        context.PhotoView = Read<RawImage>(manager, "photoView", context);
        context.PhotoButton = Read<Button>(manager, "photoButton", context);
        context.VerifyButton = Read<Button>(manager, "verifyButton", context);
        context.CancelButton = Read<Button>(manager, "cancelButton", context);
        context.NoticePopup = Read<GameObject>(manager, "noticePopup", context);
        context.NoticeText = Read<TextMeshProUGUI>(manager, "noticeText", context);
        context.ResultPopup = Read<GameObject>(manager, "resultPopup", context);
        context.ResultText = Read<TextMeshProUGUI>(manager, "resultText", context);
        context.ReasonText = Read<TextMeshProUGUI>(manager, "reasonText", context);
        context.DetectedActionText = Read<TextMeshProUGUI>(manager, "detectedActionText", context);
        context.AttrText = Read<TextMeshProUGUI>(manager, "attrText", context);
        context.ExpText = Read<TextMeshProUGUI>(manager, "expText", context);
        context.StreakText = Read<TextMeshProUGUI>(manager, "streakText", context);
        context.ResultButton = Read<Button>(manager, "checkVerificationResultButton", context);

        foreach (UnityEngine.Object reference in context.AllReferences())
        {
            Transform target = reference is Component component ? component.transform :
                reference is GameObject gameObject ? gameObject.transform : null;
            if (target != null && !target.IsChildOf(root))
            {
                context.Errors.Add("Manager 참조가 PHOTO Root 밖을 가리킵니다: " + reference.name);
            }
        }

        context.Font = Load<TMP_FontAsset>(FontPath, context);
        context.CardSprite = Load<Sprite>(CardSpritePath, context);
        context.CreamButtonSprite = Load<Sprite>(CreamButtonSpritePath, context);
        context.GoldButtonSprite = Load<Sprite>(GoldButtonSpritePath, context);
        ValidateRequiredGraphics(context);
        return Finish(context, log);
    }

    private static void ValidateRequiredGraphics(Context context)
    {
        if (context.PhotoPopup == null) return;
        if (context.PhotoPopup.GetComponent<RectTransform>() == null ||
            context.NoticePopup.GetComponent<RectTransform>() == null ||
            context.ResultPopup.GetComponent<RectTransform>() == null)
        {
            context.Errors.Add("입력/진행/결과 Popup에 RectTransform이 필요합니다.");
        }
        foreach (Button button in new[]
                 { context.PhotoButton, context.VerifyButton, context.CancelButton, context.ResultButton })
        {
            if (button != null && button.GetComponent<Image>() == null)
            {
                context.Errors.Add("Button Image가 없습니다: " + button.name);
            }
        }
    }

    private static Context Finish(Context context, bool log)
    {
        if (log)
        {
            foreach (string error in context.Errors)
            {
                Debug.LogError("[HabitPVP MainScene] PHOTO Popup: " + error, context.Root);
            }
        }
        return context;
    }

    private static T Read<T>(SerializedObject owner, string propertyName, Context context)
        where T : UnityEngine.Object
    {
        SerializedProperty property = owner.FindProperty(propertyName);
        T value = property != null ? property.objectReferenceValue as T : null;
        if (value == null)
        {
            context.Errors.Add("PhotoVerificationManager." + propertyName + " 참조가 비어 있거나 타입이 다릅니다.");
        }
        return value;
    }

    private static void ApplyStyle(Context context)
    {
        SetStretch(context.RootRect, Vector2.zero, Vector2.zero);
        Image backdrop = GetOrAdd<Image>(context.Root);
        bool newBackdrop = backdrop.sprite == null && backdrop.color == Color.white;
        Record(backdrop);
        backdrop.sprite = null;
        backdrop.type = Image.Type.Simple;
        backdrop.color = Backdrop;
        backdrop.raycastTarget = true;
        if (newBackdrop) backdrop.enabled = false;
        Save(backdrop);

        StylePanel(context.PhotoPopup, context.CardSprite, new Vector2(940f, 1360f), new Vector2(0f, -105f));
        EnsureText(context.PhotoPopup.transform, "RenewalTitle", "사진으로 습관 인증", context.Font,
            54f, Brown, FontStyles.Bold, new Vector2(0f, 570f), new Vector2(820f, 82f));
        EnsureText(context.PhotoPopup.transform, "RenewalHabitName", "인증할 습관", context.Font,
            38f, Brown, FontStyles.Bold, new Vector2(0f, 485f), new Vector2(820f, 70f));
        EnsureText(context.PhotoPopup.transform, "RenewalDescription", "사진을 선택한 뒤 인증 버튼을 눌러주세요.",
            context.Font, 30f, MutedBrown, FontStyles.Normal, new Vector2(0f, 420f), new Vector2(820f, 58f));

        SetCentered(context.PhotoView.rectTransform, new Vector2(780f, 760f), new Vector2(0f, 40f));
        Record(context.PhotoView);
        context.PhotoView.color = Color.white;
        context.PhotoView.raycastTarget = false;
        Save(context.PhotoView);
        Outline outline = GetOrAdd<Outline>(context.PhotoView.gameObject);
        Record(outline);
        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(5f, -5f);
        outline.useGraphicAlpha = false;
        Save(outline);

        StyleButton(context.PhotoButton, context.CreamButtonSprite, Cream, Brown, context.Font);
        StyleButton(context.VerifyButton, context.GoldButtonSprite, Gold, Color.white, context.Font);
        StyleButton(context.CancelButton, context.CreamButtonSprite, Cream, Brown, context.Font);
        SetCentered((RectTransform)context.PhotoButton.transform, new Vector2(264f, 120f), new Vector2(-290f, -555f));
        SetCentered((RectTransform)context.VerifyButton.transform, new Vector2(264f, 120f), new Vector2(0f, -555f));
        SetCentered((RectTransform)context.CancelButton.transform, new Vector2(264f, 120f), new Vector2(290f, -555f));

        StylePanel(context.NoticePopup, context.CardSprite, new Vector2(800f, 340f), new Vector2(0f, 30f));
        StyleText(context.NoticeText, context.Font, 34f, Brown, FontStyles.Bold);
        SetStretch(context.NoticeText.rectTransform, new Vector2(72f, 64f), new Vector2(-72f, -64f));

        StylePanel(context.ResultPopup, context.CardSprite, new Vector2(900f, 1340f), new Vector2(0f, -100f));
        StyleAndPlace(context.ResultText, context.Font, 54f, Brown, FontStyles.Bold,
            new Vector2(0f, 520f), new Vector2(780f, 180f));
        StyleAndPlace(context.ReasonText, context.Font, 34f, Brown, FontStyles.Normal,
            new Vector2(0f, 342f), new Vector2(760f, 150f));
        StyleAndPlace(context.DetectedActionText, context.Font, 38f, Brown, FontStyles.Bold,
            new Vector2(0f, 172f), new Vector2(760f, 150f));
        StyleAndPlace(context.AttrText, context.Font, 36f, RewardBrown, FontStyles.Bold,
            new Vector2(0f, 12f), new Vector2(760f, 100f));
        StyleAndPlace(context.ExpText, context.Font, 42f, RewardBrown, FontStyles.Bold,
            new Vector2(0f, -112f), new Vector2(760f, 110f));
        StyleAndPlace(context.StreakText, context.Font, 36f, Brown, FontStyles.Bold,
            new Vector2(0f, -242f), new Vector2(760f, 100f));
        StyleButton(context.ResultButton, context.GoldButtonSprite, Gold, Color.white, context.Font);
        SetCentered((RectTransform)context.ResultButton.transform, new Vector2(520f, 120f), new Vector2(0f, -535f));
    }

    private static void StylePanel(GameObject target, Sprite sprite, Vector2 size, Vector2 position)
    {
        Image image = GetOrAdd<Image>(target);
        Record(image);
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = Cream;
        image.raycastTarget = true;
        Save(image);
        SetCentered(target.GetComponent<RectTransform>(), size, position);
    }

    private static void StyleButton(Button button, Sprite sprite, Color color, Color textColor, TMP_FontAsset font)
    {
        Image image = button.GetComponent<Image>();
        Record(image);
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        Save(image);
        Record(button);
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(0xFF, 0xF4, 0xD2, 0xFF);
        colors.pressedColor = new Color32(0xD5, 0xA1, 0x55, 0xFF);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = Disabled;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        Save(button);
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) StyleText(label, font, 30f, textColor, FontStyles.Bold);
    }

    private static void EnsureText(Transform parent, string name, string value, TMP_FontAsset font,
        float size, Color color, FontStyles style, Vector2 position, Vector2 dimensions)
    {
        Transform existing = parent.Find(name);
        TextMeshProUGUI text;
        if (existing == null)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(item, "Create MainScene PHOTO presentation text");
            item.transform.SetParent(parent, false);
            text = item.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            text = existing.GetComponent<TextMeshProUGUI>();
            if (text == null) throw new InvalidOperationException(name + "에 TextMeshProUGUI가 없습니다.");
        }
        Record(text);
        text.text = value;
        StyleText(text, font, size, color, style);
        SetCentered(text.rectTransform, dimensions, position);
    }

    private static void StyleAndPlace(TMP_Text text, TMP_FontAsset font, float size, Color color,
        FontStyles style, Vector2 position, Vector2 dimensions)
    {
        StyleText(text, font, size, color, style);
        SetCentered(text.rectTransform, dimensions, position);
    }

    private static void StyleText(TMP_Text text, TMP_FontAsset font, float size, Color color, FontStyles style)
    {
        Record(text);
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        Save(text);
    }

    private static void SetCentered(RectTransform rect, Vector2 size, Vector2 position)
    {
        Record(rect);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Save(rect);
    }

    private static void SetStretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        Record(rect);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        Save(rect);
    }

    private static void ValidatePostLayout(Context context)
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(context.RootRect);
        if (!Approximately(context.RootRect.anchorMin, Vector2.zero) ||
            !Approximately(context.RootRect.anchorMax, Vector2.one))
        {
            throw new InvalidOperationException("PHOTO Dim Root가 Canvas/HabitTap 영역을 Stretch로 덮지 않습니다.");
        }
        Image backdrop = context.Root.GetComponent<Image>();
        if (backdrop == null || !backdrop.raycastTarget)
        {
            throw new InvalidOperationException("PHOTO Dim Image 또는 Raycast 차단 설정이 없습니다.");
        }
        if (context.Root.activeSelf || context.PhotoPopup.activeSelf == false)
        {
            if (backdrop.enabled)
            {
                throw new InvalidOperationException("닫힌 PHOTO Root에서 Dim Image가 활성화되어 있습니다.");
            }
        }
        CheckSize(context.PhotoPopup, new Vector2(940f, 1360f), "입력 Popup");
        CheckSize(context.NoticePopup, new Vector2(800f, 340f), "진행 Popup");
        CheckSize(context.ResultPopup, new Vector2(900f, 1340f), "결과 Popup");
        foreach (Button button in new[]
                 { context.PhotoButton, context.VerifyButton, context.CancelButton, context.ResultButton })
        {
            if (((RectTransform)button.transform).rect.height < 96f)
            {
                throw new InvalidOperationException(button.name + " 버튼 터치 높이가 96px 미만입니다.");
            }
        }
        if (!Contained(context.PhotoPopup.GetComponent<RectTransform>(), context.PhotoView.rectTransform) ||
            !Contained(context.PhotoPopup.GetComponent<RectTransform>(), (RectTransform)context.PhotoButton.transform) ||
            !Contained(context.PhotoPopup.GetComponent<RectTransform>(), (RectTransform)context.VerifyButton.transform) ||
            !Contained(context.PhotoPopup.GetComponent<RectTransform>(), (RectTransform)context.CancelButton.transform))
        {
            throw new InvalidOperationException("PHOTO 입력 UI가 Popup 경계를 벗어납니다.");
        }
    }

    private static void CheckSize(GameObject target, Vector2 expected, string label)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        if (!Approximately(rect.sizeDelta, expected))
        {
            throw new InvalidOperationException(label + " 크기가 리뉴얼 기준과 다릅니다: " + rect.sizeDelta);
        }
    }

    private static bool HasRenewalObjects(Context context)
    {
        return context.Root.GetComponent<Image>() != null &&
               context.PhotoPopup.transform.Find("RenewalTitle") != null &&
               context.PhotoPopup.transform.Find("RenewalHabitName") != null &&
               context.PhotoPopup.transform.Find("RenewalDescription") != null;
    }

    private static bool Contained(RectTransform parent, RectTransform child)
    {
        Vector3[] corners = new Vector3[4];
        child.GetWorldCorners(corners);
        return corners.All(corner =>
        {
            Vector3 local = parent.InverseTransformPoint(corner);
            return local.x >= parent.rect.xMin - 0.5f && local.x <= parent.rect.xMax + 0.5f &&
                   local.y >= parent.rect.yMin - 0.5f && local.y <= parent.rect.yMax + 0.5f;
        });
    }

    private static string Diagnostics(Context context)
    {
        return "Root activeSelf=" + context.Root.activeSelf +
               ", Input activeSelf=" + context.PhotoPopup.activeSelf +
               ", Notice activeSelf=" + context.NoticePopup.activeSelf +
               ", Result activeSelf=" + context.ResultPopup.activeSelf +
               ", TapBar activeSelf=" + context.TapBar.activeSelf +
               ", Canvas reference=" + context.CanvasScaler.referenceResolution;
    }

    private static T Load<T>(string path, Context context) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) context.Errors.Add("UI Renewal 에셋을 불러올 수 없습니다: " + path);
        return asset;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static bool Approximately(Vector2 left, Vector2 right)
    {
        return Mathf.Approximately(left.x, right.x) && Mathf.Approximately(left.y, right.y);
    }

    private static void Record(UnityEngine.Object target)
    {
        if (target == null) throw new InvalidOperationException("Undo 기록 대상이 없습니다.");
        Undo.RecordObject(target, "Style MainScene PHOTO Popup");
        EditorUtility.SetDirty(target);
    }

    private static void Save(UnityEngine.Object target)
    {
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private sealed class Context
    {
        public Scene Scene;
        public GameObject Canvas;
        public CanvasScaler CanvasScaler;
        public GameObject Root;
        public RectTransform RootRect;
        public PhotoVerificationManager Manager;
        public GameObject PhotoPopup;
        public RawImage PhotoView;
        public Button PhotoButton;
        public Button VerifyButton;
        public Button CancelButton;
        public GameObject NoticePopup;
        public TextMeshProUGUI NoticeText;
        public GameObject ResultPopup;
        public TextMeshProUGUI ResultText;
        public TextMeshProUGUI ReasonText;
        public TextMeshProUGUI DetectedActionText;
        public TextMeshProUGUI AttrText;
        public TextMeshProUGUI ExpText;
        public TextMeshProUGUI StreakText;
        public Button ResultButton;
        public GameObject TapBar;
        public TMP_FontAsset Font;
        public Sprite CardSprite;
        public Sprite CreamButtonSprite;
        public Sprite GoldButtonSprite;
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;

        public IEnumerable<UnityEngine.Object> AllReferences()
        {
            return new UnityEngine.Object[]
            {
                PhotoPopup, PhotoView, PhotoButton, VerifyButton, CancelButton, NoticePopup, NoticeText,
                ResultPopup, ResultText, ReasonText, DetectedActionText, AttrText, ExpText, StreakText,
                ResultButton
            }.Where(item => item != null);
        }
    }

    private sealed class ProtectionSnapshot
    {
        private readonly int[] references;
        private readonly string events;
        private readonly string activeStates;
        private readonly string canvasGroups;
        private readonly string siblingIndices;

        private ProtectionSnapshot(Context context)
        {
            references = context.AllReferences().Select(Id).ToArray();
            events = CaptureEvents(context.Root);
            activeStates = CaptureActiveStates(context);
            canvasGroups = CaptureCanvasGroups(context.Root);
            siblingIndices = CaptureSiblingIndices(context);
        }

        public static ProtectionSnapshot Capture(Context context) => new ProtectionSnapshot(context);

        public void AssertUnchanged(Context context)
        {
            ProtectionSnapshot after = new ProtectionSnapshot(context);
            if (!references.SequenceEqual(after.references) || events != after.events ||
                activeStates != after.activeStates || canvasGroups != after.canvasGroups ||
                siblingIndices != after.siblingIndices)
            {
                throw new InvalidOperationException(
                    "보호 대상 SerializeField, UnityEvent, activeSelf, CanvasGroup 또는 sibling 순서가 변경되었습니다.");
            }
        }

        private static string CaptureActiveStates(Context context)
        {
            return context.Root.activeSelf + "|" + context.PhotoPopup.activeSelf + "|" +
                   context.NoticePopup.activeSelf + "|" + context.ResultPopup.activeSelf + "|" +
                   context.TapBar.activeSelf;
        }

        private static string CaptureSiblingIndices(Context context)
        {
            return context.Root.transform.GetSiblingIndex() + "|" + context.PhotoPopup.transform.GetSiblingIndex() +
                   "|" + context.NoticePopup.transform.GetSiblingIndex() + "|" +
                   context.ResultPopup.transform.GetSiblingIndex();
        }

        private static string CaptureEvents(GameObject root)
        {
            List<string> values = new List<string>();
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                AddEvent(values, button.transform, button.onClick);
            }
            values.Sort(StringComparer.Ordinal);
            return string.Join("\n", values);
        }

        private static string CaptureCanvasGroups(GameObject root)
        {
            return string.Join("\n", root.GetComponentsInChildren<CanvasGroup>(true)
                .Select(group => Id(group) + "|" + group.alpha + "|" + group.interactable + "|" +
                                 group.blocksRaycasts + "|" + group.ignoreParentGroups));
        }

        private static void AddEvent(ICollection<string> values, Transform owner, UnityEventBase unityEvent)
        {
            string path = owner.name;
            for (Transform current = owner.parent; current != null; current = current.parent)
            {
                path = current.name + "/" + path;
            }
            values.Add(path + "|" + unityEvent.GetPersistentEventCount());
            for (int index = 0; index < unityEvent.GetPersistentEventCount(); index++)
            {
                values.Add(path + "|" + index + "|" + Id(unityEvent.GetPersistentTarget(index)) + "|" +
                           unityEvent.GetPersistentMethodName(index));
            }
        }

        private static int Id(UnityEngine.Object value) => value != null ? value.GetInstanceID() : 0;
    }
}
