using CaseFit;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CaseFitEditor
{
    public static class CaseFitHudBuilder
    {
        static readonly Color Bone = new(0.93f, 0.90f, 0.83f);
        static readonly Color Dim = new(0.59f, 0.56f, 0.49f);
        static readonly Color Brass = new(0.79f, 0.64f, 0.15f);

        [MenuItem("Tools/Case Fit/Build HUD", false, 21)]
        public static void BuildHud()
        {
            LevelRunner runner = Object.FindFirstObjectByType<LevelRunner>();
            if (runner == null)
            {
                EditorUtility.DisplayDialog("Case Fit",
                    "No LevelRunner found in the scene.\nRun Tools > Case Fit > Build Scene first.", "OK");
                return;
            }

            if (Resources.Load("TMP Settings") == null)
            {
                EditorUtility.DisplayDialog("Case Fit",
                    "TextMeshPro is not imported yet.\nRun Window > TextMeshPro > Import TMP Essential Resources, then try again.",
                    "OK");
                return;
            }

            EnsureEventSystem();

            GameObject existing = GameObject.Find("HUD Canvas");
            if (existing != null) Object.DestroyImmediate(existing);

            GameObject canvasGo = new("HUD Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            Transform root = canvasGo.transform;

            TMP_Text title = Label(root, "TitleLabel", "",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -40f),
                new Vector2(560f, 54f), 40f, TextAlignmentOptions.TopLeft, Bone);

            TMP_Text message = Label(root, "MessageLabel", "",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f),
                new Vector2(900f, 60f), 28f, TextAlignmentOptions.Center, Dim);

            TMP_Text time = Stat(root, "Time", "TIME", new Vector2(-40f, -40f));
            TMP_Text moves = Stat(root, "Moves", "MOVES", new Vector2(-260f, -40f));
            TMP_Text remaining = Stat(root, "Remaining", "LEFT", new Vector2(-480f, -40f));

            Button clear = MakeButton(root, "ClearButton", "CLEAR CASE", new Vector2(-40f, 40f));
            Button reload = MakeButton(root, "ReloadButton", "RETRY", new Vector2(-250f, 40f));
            Button next = MakeButton(root, "NextButton", "NEXT LEVEL", new Vector2(-460f, 40f));

            UnityEventTools.AddPersistentListener(clear.onClick, runner.ClearCase);
            UnityEventTools.AddPersistentListener(reload.onClick, runner.ReloadLevel);
            UnityEventTools.AddPersistentListener(next.onClick, runner.NextLevel);

            Toggle chill = MakeToggle(root, "ChillToggle", "Chill mode (no timer)", new Vector2(40f, 40f));
            UnityEventTools.AddPersistentListener(chill.onValueChanged, runner.SetChillMode);

            CaseFitHud hud = canvasGo.AddComponent<CaseFitHud>();
            SerializedObject hudSo = new(hud);
            hudSo.FindProperty("runner").objectReferenceValue = runner;
            hudSo.FindProperty("titleLabel").objectReferenceValue = title;
            hudSo.FindProperty("timeLabel").objectReferenceValue = time;
            hudSo.FindProperty("movesLabel").objectReferenceValue = moves;
            hudSo.FindProperty("remainingLabel").objectReferenceValue = remaining;
            hudSo.FindProperty("messageLabel").objectReferenceValue = message;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = canvasGo;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Case Fit] HUD built and wired. Press Play.");
        }

        static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            GameObject go = new("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        static TMP_Text Label(Transform parent, string name, string text,
                              Vector2 anchorMin, Vector2 anchorMax, Vector2 position,
                              Vector2 size, float fontSize, TextAlignmentOptions align, Color color)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMin.x, anchorMax.y);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = align;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        static TMP_Text Stat(Transform parent, string name, string caption, Vector2 position)
        {
            Label(parent, name + "Caption", caption,
                new Vector2(1f, 1f), new Vector2(1f, 1f), position,
                new Vector2(200f, 26f), 20f, TextAlignmentOptions.TopRight, Dim);

            return Label(parent, name + "Label", "0",
                new Vector2(1f, 1f), new Vector2(1f, 1f), position + new Vector2(0f, -26f),
                new Vector2(200f, 52f), 42f, TextAlignmentOptions.TopRight, Bone);
        }

        static Button MakeButton(Transform parent, string name, string caption, Vector2 position)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(190f, 60f);
            rect.anchoredPosition = position;

            Image background = go.AddComponent<Image>();
            background.color = new Color(0.16f, 0.14f, 0.10f, 0.92f);

            Button button = go.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.28f, 0.24f, 0.14f, 1f);
            button.colors = colors;

            TMP_Text label = Label(go.transform, "Text", caption,
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero,
                Vector2.zero, 24f, TextAlignmentOptions.Center, Brass);

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
        }

        static Toggle MakeToggle(Transform parent, string name, string caption, Vector2 position)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(360f, 44f);
            rect.anchoredPosition = position;

            Toggle toggle = go.AddComponent<Toggle>();

            GameObject boxGo = new("Box", typeof(RectTransform));
            boxGo.transform.SetParent(go.transform, false);
            RectTransform boxRect = boxGo.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0f, 0.5f);
            boxRect.anchorMax = new Vector2(0f, 0.5f);
            boxRect.pivot = new Vector2(0f, 0.5f);
            boxRect.sizeDelta = new Vector2(30f, 30f);
            boxRect.anchoredPosition = Vector2.zero;
            Image boxImage = boxGo.AddComponent<Image>();
            boxImage.color = new Color(0.16f, 0.14f, 0.10f, 0.92f);

            GameObject markGo = new("Check", typeof(RectTransform));
            markGo.transform.SetParent(boxGo.transform, false);
            RectTransform markRect = markGo.GetComponent<RectTransform>();
            markRect.anchorMin = Vector2.zero;
            markRect.anchorMax = Vector2.one;
            markRect.offsetMin = new Vector2(6f, 6f);
            markRect.offsetMax = new Vector2(-6f, -6f);
            Image markImage = markGo.AddComponent<Image>();
            markImage.color = Brass;

            TMP_Text label = Label(go.transform, "Text", caption,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, 0f),
                new Vector2(320f, 40f), 22f, TextAlignmentOptions.Left, Dim);
            label.rectTransform.pivot = new Vector2(0f, 0.5f);

            toggle.targetGraphic = boxImage;
            toggle.graphic = markImage;
            toggle.isOn = false;
            return toggle;
        }
    }
}
