using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class NumberPopupIntegrationEditor
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string CanvasPrefabPath = "Assets/Resources/UIPrefabs/Canvas.prefab";
    private const string OverlayPath = "HabitTap/AchievedAmountOverlay";
    private const string FontPath = "Assets/Login/Fonts/NotoSansKR Login SDF.asset";
    private const string CardSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/panel_rpg_card.png";
    private const string CreamButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_cream.png";
    private const string GoldButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_rpg.png";
    private const string MenuRoot = "Tools/HabitPVP/UI Renewal/MainScene/";
    private const float MinimumPopupWidth = 800f;
    private const float MinimumPopupHeight = 700f;
    private const float MinimumInputHeight = 120f;
    private const float MinimumButtonHeight = 96f;
    private static readonly Vector2 TargetPopupSize = new Vector2(880f, 760f);
    private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x86, 0x68, 0x5D, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);
    private static readonly Color32 Disabled = new Color32(0xB9, 0xAA, 0x91, 0xB8);
    private static readonly Color32 Backdrop = new Color32(0x2A, 0x1B, 0x1D, 0xB8);

    [MenuItem(MenuRoot + "Validate NUMBER Popup", false, 2230)]
    private static void ValidateMenu()
    {
        Context context = Validate(true);
        if (!context.IsValid)
        {
            return;
        }

        string diagnostics = BuildLayoutDiagnostics(context);
        Debug.Log(
            "[HabitPVP MainScene] NUMBER Popup Pre-Validate 성공. 필수 Hierarchy, 컴포넌트, " +
            "SerializeField, TMP_InputField 및 UnityEvent가 유효합니다.\n" + diagnostics,
            context.Overlay.gameObject);

        if (HasTargetPopupSize(context))
        {
            ValidatePostLayout(context);
            Debug.Log("[HabitPVP MainScene] NUMBER Popup Post-Validate도 통과했습니다.", context.Popup);
        }
        else
        {
            Debug.LogWarning(
                "[HabitPVP MainScene] NUMBER Popup은 아직 리뉴얼 목표 크기가 아닙니다. " +
                "이는 Apply 전에는 오류가 아니며 Apply 이후 Post-Validate에서 필수 검사됩니다.",
                context.Popup);
        }
    }

    [MenuItem(MenuRoot + "Dry Run NUMBER Popup", false, 2231)]
    private static void DryRunMenu()
    {
        Context context = Validate(true);
        if (!context.IsValid)
        {
            Debug.LogError("[HabitPVP MainScene] NUMBER Popup Dry Run 중단: 검증 오류를 해결해 주세요.");
            return;
        }

        Debug.Log(
            "[HabitPVP MainScene] NUMBER Popup Dry Run (변경 없음)\n" +
            "대상: Canvas/" + OverlayPath + "/Popup\n" +
            BuildLayoutDiagnostics(context) + "\n" +
            "적용 예정 Popup: size=880x760, anchor=(0.5, 0.5), pivot=(0.5, 0.5), position=(0, 70)\n" +
            "변경 대상: Overlay Dim/Image, Popup Rect/Image, Title/Goal/Unit Text, " +
            "AmountInput/Text Area/Text/Placeholder, Cancel/Confirm Button\n" +
            "- Popup이 닫힌 현재 상태에서는 Overlay Dim Image도 비활성 유지\n" +
            "- 기존 Overlay/Popup/InputField/Button 오브젝트만 재사용\n" +
            "- 크림색 RPG 카드, 골드 테두리, NotoSansKR 브라운 텍스트\n" +
            "- 습관명과 현재 누적량/목표량 2줄 표시, 정수 입력 및 단위 표시\n" +
            "- 1080x1920 기준 중앙 팝업과 112px 버튼 터치 영역\n" +
            "- AchievedAmountOverlay 6개 SerializeField, TMP_InputField 설정, UnityEvent 보존\n" +
            "- Canvas.prefab Apply/Revert 및 Scene 자동 저장 없음",
            context.Overlay.gameObject);
    }

    [MenuItem(MenuRoot + "Apply NUMBER Popup", false, 2232)]
    private static void ApplyMenu()
    {
        Context context = Validate(true);
        if (!context.IsValid)
        {
            Debug.LogError("[HabitPVP MainScene] NUMBER Popup Apply 중단: 검증 오류를 해결해 주세요.");
            return;
        }

        ProtectionSnapshot snapshot = ProtectionSnapshot.Capture(context);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply MainScene NUMBER Popup UI Renewal");

        try
        {
            ApplyStyle(context);
            snapshot.AssertUnchanged(context);
            ValidatePostLayout(context);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = context.Popup;
            Debug.Log(
                "[HabitPVP MainScene] NUMBER Popup 적용 완료. Scene은 저장하지 않았습니다. " +
                "Hierarchy/Inspector 확인 후 사용자가 직접 저장해 주세요. Edit > Undo를 지원합니다.",
                context.Popup);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogException(exception);
            Debug.LogError("[HabitPVP MainScene] NUMBER Popup Apply 실패. 이번 Undo Group을 되돌렸습니다.");
        }
    }

    [MenuItem(MenuRoot + "Apply NUMBER Popup", true)]
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

        GameObject[] canvases = context.Scene.GetRootGameObjects()
            .Where(item => item.name == "Canvas").ToArray();
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

        Transform overlay = context.Canvas.transform.Find(OverlayPath);
        if (overlay == null)
        {
            context.Errors.Add("대상 경로가 없습니다: Canvas/" + OverlayPath);
            return Finish(context, log);
        }

        context.Overlay = overlay.GetComponent<AchievedAmountOverlay>();
        context.OverlayRect = overlay as RectTransform;
        context.Popup = FindObject(overlay, "Popup", context);
        context.PopupRect = Rect(overlay, "Popup", context);
        context.GoalText = Find<TextMeshProUGUI>(overlay, "Popup/Goal Text", context);
        context.AmountInput = Find<TMP_InputField>(overlay, "Popup/AmountInput", context);
        context.UnitText = Find<TextMeshProUGUI>(overlay, "Popup/UnitText", context);
        context.CancelButton = Find<Button>(overlay, "Popup/CancelButton", context);
        context.ConfirmButton = Find<Button>(overlay, "Popup/ConfirmButton", context);
        context.TitleText = Find<TextMeshProUGUI>(overlay, "Popup/TitleText", context);
        context.InputText = Find<TextMeshProUGUI>(overlay, "Popup/AmountInput/Text Area/Text", context);
        context.Placeholder = Find<TextMeshProUGUI>(overlay,
            "Popup/AmountInput/Text Area/Placeholder", context);
        context.TapBar = context.Canvas.transform.Find("TapBar")?.gameObject;

        AchievedAmountOverlay[] overlays = UnityEngine.Object.FindObjectsOfType<AchievedAmountOverlay>(true)
            .Where(item => item.gameObject.scene == context.Scene).ToArray();
        if (context.Overlay == null || overlays.Length != 1 || overlays[0] != context.Overlay)
        {
            context.Errors.Add("MainScene의 AchievedAmountOverlay가 대상 경로에 정확히 하나 있어야 합니다.");
        }
        if (!PrefabUtility.IsPartOfPrefabInstance(overlay.gameObject))
        {
            context.Errors.Add("NUMBER Overlay가 Canvas Prefab instance에 속하지 않습니다.");
        }
        if (context.TapBar == null)
        {
            context.Errors.Add("NUMBER Popup 표시 중 숨길 Canvas/TapBar가 없습니다.");
        }

        ValidateReferences(context);
        ValidateInput(context);
        ValidatePreLayout(context);
        context.Font = Load<TMP_FontAsset>(FontPath, context);
        context.CardSprite = Load<Sprite>(CardSpritePath, context);
        context.CreamButtonSprite = Load<Sprite>(CreamButtonSpritePath, context);
        context.GoldButtonSprite = Load<Sprite>(GoldButtonSpritePath, context);
        return Finish(context, log);
    }

    private static void ValidateReferences(Context context)
    {
        if (context.Overlay == null)
        {
            return;
        }

        SerializedObject serialized = new SerializedObject(context.Overlay);
        var expected = new Dictionary<string, UnityEngine.Object>
        {
            { "popup", context.Popup },
            { "goalText", context.GoalText },
            { "amountInput", context.AmountInput },
            { "unitText", context.UnitText },
            { "cancelButton", context.CancelButton },
            { "confirmButton", context.ConfirmButton }
        };
        foreach (KeyValuePair<string, UnityEngine.Object> pair in expected)
        {
            SerializedProperty property = serialized.FindProperty(pair.Key);
            if (property == null || property.objectReferenceValue != pair.Value)
            {
                context.Errors.Add("AchievedAmountOverlay." + pair.Key + " 참조가 실제 Hierarchy와 다릅니다.");
            }
        }
    }

    private static void ValidateInput(Context context)
    {
        if (context.AmountInput == null)
        {
            return;
        }
        if (context.AmountInput.textComponent != context.InputText ||
            context.AmountInput.placeholder != context.Placeholder)
        {
            context.Errors.Add("AmountInput의 Text/Placeholder 참조가 기존 Hierarchy와 다릅니다.");
        }
        if (context.AmountInput.contentType != TMP_InputField.ContentType.IntegerNumber &&
            context.AmountInput.characterValidation != TMP_InputField.CharacterValidation.Integer)
        {
            context.Errors.Add("AmountInput이 정수 입력으로 제한되어 있지 않습니다.");
        }
    }

    private static Context Finish(Context context, bool log)
    {
        if (log)
        {
            foreach (string error in context.Errors)
            {
                Debug.LogError("[HabitPVP MainScene] NUMBER Popup: " + error);
            }
        }
        return context;
    }

    private static void ApplyStyle(Context context)
    {
        SetRect(context.OverlayRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        StyleImage(context.OverlayRect.GetComponent<Image>(), null, Backdrop, Image.Type.Simple);
        Image backdrop = context.OverlayRect.GetComponent<Image>();
        Record(backdrop);
        backdrop.enabled = context.Popup.activeSelf;
        SaveOverride(backdrop);

        SetRect(context.PopupRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 70f), TargetPopupSize);
        StyleImage(context.Popup.GetComponent<Image>(), context.CardSprite, Cream, Image.Type.Sliced);

        StyleText(context.TitleText, context.Font, 48f, 56f, Brown, FontStyles.Bold);
        SetRect(context.TitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -78f), new Vector2(720f, 92f));

        StyleText(context.GoalText, context.Font, 32f, 42f, Brown, FontStyles.Bold);
        context.GoalText.enableWordWrapping = true;
        SaveOverride(context.GoalText);
        SetRect(context.GoalText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -200f), new Vector2(720f, 116f));

        RectTransform inputRect = context.AmountInput.GetComponent<RectTransform>();
        SetRect(inputRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-80f, 0f), new Vector2(520f, 144f));
        StyleImage(context.AmountInput.GetComponent<Image>(), context.CreamButtonSprite,
            Color.white, Image.Type.Sliced);
        Record(context.AmountInput);
        context.AmountInput.caretColor = Brown;
        context.AmountInput.selectionColor = new Color32(0xE7, 0xA8, 0x35, 0x70);
        context.AmountInput.caretWidth = 4;
        SaveOverride(context.AmountInput);

        RectTransform textArea = Rect(context.AmountInput.transform, "Text Area", context);
        SetRect(textArea, Vector2.zero, Vector2.one, new Vector2(26f, 18f), new Vector2(-26f, -18f));
        StyleText(context.InputText, context.Font, 42f, 54f, Brown, FontStyles.Bold);
        StyleText(context.Placeholder, context.Font, 30f, 38f, MutedBrown, FontStyles.Normal);

        StyleText(context.UnitText, context.Font, 34f, 44f, Brown, FontStyles.Bold);
        SetRect(context.UnitText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(290f, 0f), new Vector2(150f, 110f));

        StyleButton(context.CancelButton, context.CreamButtonSprite, Cream, Brown);
        SetRect(context.CancelButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(-190f, 112f), new Vector2(300f, 112f));
        StyleButton(context.ConfirmButton, context.GoldButtonSprite, Gold, Color.white);
        SetRect(context.ConfirmButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(190f, 112f), new Vector2(300f, 112f));

        foreach (TMP_Text text in context.Popup.GetComponentsInChildren<TMP_Text>(true))
        {
            Record(text);
            text.font = context.Font;
            SaveOverride(text);
        }
    }

    private static void StyleButton(Button button, Sprite sprite, Color32 color, Color textColor)
    {
        Image image = button.GetComponent<Image>();
        StyleImage(image, sprite, color, Image.Type.Sliced);
        Record(button);
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(0xFF, 0xF4, 0xD2, 0xFF);
        colors.pressedColor = new Color32(0xD5, 0xA1, 0x55, 0xFF);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = Disabled;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        SaveOverride(button);
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            StyleText(label, label.font, 30f, 38f, textColor, FontStyles.Bold);
        }
    }

    private static void StyleImage(Image image, Sprite sprite, Color color, Image.Type type)
    {
        if (image == null)
        {
            throw new InvalidOperationException("스타일 대상 Image가 없습니다.");
        }
        Record(image);
        image.sprite = sprite;
        image.color = color;
        image.type = type;
        image.raycastTarget = true;
        SaveOverride(image);
    }

    private static void StyleText(TMP_Text text, TMP_FontAsset font, float size, float lineSpacing,
        Color color, FontStyles style)
    {
        Record(text);
        text.font = font;
        text.fontSize = size;
        text.lineSpacing = lineSpacing - size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        SaveOverride(text);
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size)
    {
        if (rect == null)
        {
            throw new InvalidOperationException("레이아웃 대상 RectTransform이 없습니다.");
        }
        Record(rect);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        if (anchorMin == Vector2.zero && anchorMax == Vector2.one)
        {
            rect.offsetMin = position;
            rect.offsetMax = size;
        }
        else
        {
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
        SaveOverride(rect);
    }

    private static void ValidatePreLayout(Context context)
    {
        if (context.OverlayRect == null || context.PopupRect == null)
        {
            context.Errors.Add("Overlay 또는 Popup RectTransform이 없어 레이아웃을 적용할 수 없습니다.");
            return;
        }

        if (context.PopupRect.rect.width <= 0f || context.PopupRect.rect.height <= 0f)
        {
            context.Errors.Add("Popup RectTransform의 현재 rect.size가 0 이하라 레이아웃을 적용할 수 없습니다.");
        }

        if (context.Popup == null || context.AmountInput == null ||
            context.CancelButton == null || context.ConfirmButton == null)
        {
            return;
        }

        if (context.Popup.GetComponent<Image>() == null || context.OverlayRect.GetComponent<Image>() == null ||
            context.AmountInput.GetComponent<Image>() == null ||
            context.CancelButton.GetComponent<Image>() == null ||
            context.ConfirmButton.GetComponent<Image>() == null)
        {
            context.Errors.Add("RPG 스타일 적용에 필요한 Popup/Input/Button Image가 없습니다.");
        }
    }

    private static void ValidatePostLayout(Context context)
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(context.PopupRect);

        if (!Approximately(context.OverlayRect.anchorMin, Vector2.zero) ||
            !Approximately(context.OverlayRect.anchorMax, Vector2.one))
        {
            throw new InvalidOperationException("NUMBER Overlay가 Canvas 전체를 덮지 않습니다.");
        }
        Image backdrop = context.OverlayRect.GetComponent<Image>();
        if (backdrop == null || backdrop.enabled != context.Popup.activeSelf || !backdrop.raycastTarget)
        {
            throw new InvalidOperationException(
                "NUMBER Dim Image 표시 상태가 Popup activeSelf와 일치하지 않거나 Raycast 차단 설정이 잘못되었습니다.");
        }
        if (context.PopupRect.rect.width < MinimumPopupWidth ||
            context.PopupRect.rect.height < MinimumPopupHeight)
        {
            throw new InvalidOperationException(
                $"NUMBER Popup이 1080x1920 기준 최소 크기보다 작습니다. " +
                $"actual={context.PopupRect.rect.size}, minimum={MinimumPopupWidth}x{MinimumPopupHeight}");
        }
        if (!Approximately(context.PopupRect.sizeDelta, TargetPopupSize))
        {
            throw new InvalidOperationException(
                $"NUMBER Popup이 Preview 검증 목표 크기와 다릅니다. " +
                $"actual={context.PopupRect.sizeDelta}, target={TargetPopupSize}");
        }
        if (context.AmountInput.GetComponent<RectTransform>().rect.height < MinimumInputHeight)
        {
            throw new InvalidOperationException("NUMBER Popup 입력창 터치 높이가 120px 미만입니다.");
        }
        if (context.CancelButton.GetComponent<RectTransform>().rect.height < MinimumButtonHeight ||
            context.ConfirmButton.GetComponent<RectTransform>().rect.height < MinimumButtonHeight)
        {
            throw new InvalidOperationException("NUMBER Popup 버튼 터치 높이가 96px 미만입니다.");
        }
        if (!Contained(context.PopupRect, context.GoalText.rectTransform) ||
            !Contained(context.PopupRect, context.AmountInput.GetComponent<RectTransform>()) ||
            !Contained(context.PopupRect, context.CancelButton.GetComponent<RectTransform>()) ||
            !Contained(context.PopupRect, context.ConfirmButton.GetComponent<RectTransform>()))
        {
            throw new InvalidOperationException("NUMBER Popup 자식 UI가 패널 경계를 벗어납니다.");
        }
        if (!Contained(context.OverlayRect, context.PopupRect))
        {
            throw new InvalidOperationException("NUMBER Popup이 Overlay의 모바일 표시 경계를 벗어납니다.");
        }

        RectTransform input = context.AmountInput.GetComponent<RectTransform>();
        RectTransform cancel = context.CancelButton.GetComponent<RectTransform>();
        RectTransform confirm = context.ConfirmButton.GetComponent<RectTransform>();
        if (Overlaps(context.PopupRect, cancel, confirm) ||
            Overlaps(context.PopupRect, input, cancel) ||
            Overlaps(context.PopupRect, input, confirm))
        {
            throw new InvalidOperationException("NUMBER Popup 입력창과 버튼이 서로 겹칩니다.");
        }
    }

    private static bool HasTargetPopupSize(Context context)
    {
        return context.PopupRect != null && Approximately(context.PopupRect.sizeDelta, TargetPopupSize);
    }

    private static string BuildLayoutDiagnostics(Context context)
    {
        RectTransform popup = context.PopupRect;
        Vector2 rectSize = popup != null ? popup.rect.size : Vector2.zero;
        Vector2 sizeDelta = popup != null ? popup.sizeDelta : Vector2.zero;
        Vector2 anchorMin = popup != null ? popup.anchorMin : Vector2.zero;
        Vector2 anchorMax = popup != null ? popup.anchorMax : Vector2.zero;
        Vector2 pivot = popup != null ? popup.pivot : Vector2.zero;
        Vector2 position = popup != null ? popup.anchoredPosition : Vector2.zero;
        bool activeSelf = context.Popup != null && context.Popup.activeSelf;
        bool activeInHierarchy = context.Popup != null && context.Popup.activeInHierarchy;
        CanvasScaler scaler = context.CanvasScaler;

        return "현재 Popup: " +
               $"rect.size={rectSize}, sizeDelta={sizeDelta}, anchorMin={anchorMin}, " +
               $"anchorMax={anchorMax}, pivot={pivot}, position={position}, " +
               $"activeSelf={activeSelf}, activeInHierarchy={activeInHierarchy}\n" +
               "검증 기준: " +
               $"minimum={MinimumPopupWidth}x{MinimumPopupHeight}, target={TargetPopupSize}, " +
               $"inputHeight>={MinimumInputHeight}, buttonHeight>={MinimumButtonHeight}\n" +
               "CanvasScaler: " +
               (scaler != null
                   ? $"mode={scaler.uiScaleMode}, reference={scaler.referenceResolution}, " +
                     $"match={scaler.matchWidthOrHeight:F1}"
                   : "missing") +
               " (rect.size는 Canvas 기준 UI 단위이며 실제 화면 픽셀은 CanvasScaler로 변환됩니다.)";
    }

    private static bool Contained(RectTransform parent, RectTransform child)
    {
        Vector3[] corners = new Vector3[4];
        child.GetWorldCorners(corners);
        UnityEngine.Rect bounds = parent.rect;
        return corners.All(corner =>
        {
            Vector3 local = parent.InverseTransformPoint(corner);
            return local.x >= bounds.xMin - 0.5f && local.x <= bounds.xMax + 0.5f &&
                   local.y >= bounds.yMin - 0.5f && local.y <= bounds.yMax + 0.5f;
        });
    }

    private static bool Overlaps(RectTransform parent, RectTransform left, RectTransform right)
    {
        return RelativeRect(parent, left).Overlaps(RelativeRect(parent, right));
    }

    private static UnityEngine.Rect RelativeRect(RectTransform parent, RectTransform child)
    {
        Vector3[] corners = new Vector3[4];
        child.GetWorldCorners(corners);
        Vector3 bottomLeft = parent.InverseTransformPoint(corners[0]);
        Vector3 topRight = parent.InverseTransformPoint(corners[2]);
        return UnityEngine.Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
    }

    private static bool Approximately(Vector2 left, Vector2 right)
    {
        return Mathf.Approximately(left.x, right.x) && Mathf.Approximately(left.y, right.y);
    }

    private static T Load<T>(string path, Context context) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            context.Errors.Add("UI Renewal 에셋을 불러올 수 없습니다: " + path);
        }
        return asset;
    }

    private static GameObject FindObject(Transform root, string path, Context context)
    {
        Transform target = root.Find(path);
        if (target == null)
        {
            context.Errors.Add("Hierarchy 경로가 없습니다: " + path);
        }
        return target != null ? target.gameObject : null;
    }

    private static T Find<T>(Transform root, string path, Context context) where T : Component
    {
        Transform target = root.Find(path);
        T component = target != null ? target.GetComponent<T>() : null;
        if (component == null)
        {
            context.Errors.Add(typeof(T).Name + "이 없습니다: " + path);
        }
        return component;
    }

    private static RectTransform Rect(Transform root, string path, Context context)
    {
        return Find<RectTransform>(root, path, context);
    }

    private static void Record(UnityEngine.Object target)
    {
        if (target == null)
        {
            throw new InvalidOperationException("Undo 기록 대상이 없습니다.");
        }
        Undo.RecordObject(target, "Style MainScene NUMBER Popup");
        EditorUtility.SetDirty(target);
    }

    private static void SaveOverride(UnityEngine.Object target)
    {
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private sealed class Context
    {
        public Scene Scene;
        public GameObject Canvas;
        public CanvasScaler CanvasScaler;
        public AchievedAmountOverlay Overlay;
        public RectTransform OverlayRect;
        public GameObject Popup;
        public RectTransform PopupRect;
        public TextMeshProUGUI GoalText;
        public TMP_InputField AmountInput;
        public TextMeshProUGUI UnitText;
        public Button CancelButton;
        public Button ConfirmButton;
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI InputText;
        public TextMeshProUGUI Placeholder;
        public GameObject TapBar;
        public TMP_FontAsset Font;
        public Sprite CardSprite;
        public Sprite CreamButtonSprite;
        public Sprite GoldButtonSprite;
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }

    private sealed class ProtectionSnapshot
    {
        private readonly int[] references;
        private readonly string inputConfiguration;
        private readonly string eventSignature;
        private readonly bool overlayActive;
        private readonly bool popupActive;
        private readonly int popupSiblingIndex;
        private readonly string canvasGroupSignature;

        private ProtectionSnapshot(Context context)
        {
            SerializedObject serialized = new SerializedObject(context.Overlay);
            references = new[]
            {
                Id(serialized.FindProperty("popup")?.objectReferenceValue),
                Id(serialized.FindProperty("goalText")?.objectReferenceValue),
                Id(serialized.FindProperty("amountInput")?.objectReferenceValue),
                Id(serialized.FindProperty("unitText")?.objectReferenceValue),
                Id(serialized.FindProperty("cancelButton")?.objectReferenceValue),
                Id(serialized.FindProperty("confirmButton")?.objectReferenceValue)
            };
            inputConfiguration = context.AmountInput.contentType + "|" +
                                 context.AmountInput.characterValidation + "|" +
                                 context.AmountInput.lineType + "|" + context.AmountInput.readOnly + "|" +
                                 Id(context.AmountInput.textComponent) + "|" + Id(context.AmountInput.placeholder);
            eventSignature = CaptureEvents(context.Canvas);
            overlayActive = context.Overlay.gameObject.activeSelf;
            popupActive = context.Popup.activeSelf;
            popupSiblingIndex = context.Popup.transform.GetSiblingIndex();
            canvasGroupSignature = CaptureCanvasGroups(context.Overlay.gameObject);
        }

        public static ProtectionSnapshot Capture(Context context)
        {
            return new ProtectionSnapshot(context);
        }

        public void AssertUnchanged(Context context)
        {
            ProtectionSnapshot after = new ProtectionSnapshot(context);
            if (!references.SequenceEqual(after.references) ||
                inputConfiguration != after.inputConfiguration || eventSignature != after.eventSignature ||
                overlayActive != after.overlayActive || popupActive != after.popupActive ||
                popupSiblingIndex != after.popupSiblingIndex ||
                canvasGroupSignature != after.canvasGroupSignature)
            {
                throw new InvalidOperationException(
                    "보호 대상 SerializeField, TMP_InputField, UnityEvent, activeSelf, " +
                    "CanvasGroup 또는 Popup 표시 순서가 변경되었습니다.");
            }
        }

        private static int Id(UnityEngine.Object value)
        {
            return value != null ? value.GetInstanceID() : 0;
        }

        private static string CaptureEvents(GameObject root)
        {
            List<string> values = new List<string>();
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                Add(values, button.transform, "Button", button.onClick);
            }
            foreach (TMP_InputField input in root.GetComponentsInChildren<TMP_InputField>(true))
            {
                Add(values, input.transform, "InputChange", input.onValueChanged);
                Add(values, input.transform, "InputEnd", input.onEndEdit);
                Add(values, input.transform, "InputSubmit", input.onSubmit);
            }
            values.Sort(StringComparer.Ordinal);
            return string.Join("\n", values);
        }

        private static string CaptureCanvasGroups(GameObject root)
        {
            return string.Join("\n", root.GetComponentsInChildren<CanvasGroup>(true)
                .Select(group => group.GetInstanceID() + "|" + group.alpha + "|" +
                                 group.interactable + "|" + group.blocksRaycasts + "|" +
                                 group.ignoreParentGroups));
        }

        private static void Add(ICollection<string> values, Transform owner, string label,
            UnityEventBase unityEvent)
        {
            string path = owner.name;
            Transform current = owner.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            values.Add(path + "|" + label + "|" + unityEvent.GetPersistentEventCount());
            for (int index = 0; index < unityEvent.GetPersistentEventCount(); index++)
            {
                values.Add(path + "|" + label + "|" + index + "|" +
                           Id(unityEvent.GetPersistentTarget(index)) + "|" +
                           unityEvent.GetPersistentMethodName(index));
            }
        }
    }
}
