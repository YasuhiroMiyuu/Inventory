using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CaseFit
{
    public class CaseFitHudAuto : MonoBehaviour
    {
        [Header("Runner")]
        [Tooltip("Leave empty to find the LevelRunner in the scene automatically.")]
        [SerializeField] LevelRunner runner;

        [Header("Text Source")]
        [Tooltip("Off: show LEVEL 1 / 2 / 3 instead of the title stored in the level asset.")]
        [SerializeField] bool useLevelAssetText = false;

        [Header("Parts")]
        [SerializeField] bool showButtons = true;
        [SerializeField] bool showChillToggle = true;

        [Header("Style")]
        [SerializeField] Color boneColor = new(0.93f, 0.90f, 0.83f);
        [SerializeField] Color dimColor = new(0.62f, 0.59f, 0.51f);
        [SerializeField] Color brassColor = new(0.85f, 0.70f, 0.20f);
        [SerializeField] Color warningColor = new(0.88f, 0.30f, 0.20f);
        [SerializeField] float warningSeconds = 10f;

        TMP_Text titleLabel;
        TMP_Text timeLabel;
        TMP_Text movesLabel;
        TMP_Text remainingLabel;
        TMP_Text messageLabel;

        void Awake()
        {
            if (runner == null) runner = FindFirstObjectByType<LevelRunner>();

            if (runner == null)
            {
                Debug.LogError("[Case Fit] CaseFitHudAuto: no LevelRunner in this scene. Add one, or drag it into 'Runner'.", this);
                enabled = false;
                return;
            }

            EnsureEventSystem();
            BuildHud();

            runner.onLevelLoaded.AddListener(HandleLevelLoaded);
            runner.onLevelCleared.AddListener(HandleCleared);
            runner.onLevelFailed.AddListener(HandleFailed);
            runner.onStuck.AddListener(HandleStuck);

            Debug.Log("[Case Fit] HUD built at runtime and linked to " + runner.name, this);
        }

        void OnDestroy()
        {
            if (runner == null) return;

            runner.onLevelLoaded.RemoveListener(HandleLevelLoaded);
            runner.onLevelCleared.RemoveListener(HandleCleared);
            runner.onLevelFailed.RemoveListener(HandleFailed);
            runner.onStuck.RemoveListener(HandleStuck);
        }

        void Update()
        {
            if (runner == null) return;

            if (timeLabel != null)
            {
                if (runner.ChillMode)
                {
                    timeLabel.text = "--:--";
                    timeLabel.color = boneColor;
                }
                else
                {
                    int whole = Mathf.Max(0, Mathf.CeilToInt(runner.TimeLeft));
                    timeLabel.text = (whole / 60) + ":" + (whole % 60).ToString("00");
                    timeLabel.color = whole <= warningSeconds ? warningColor : boneColor;
                }
            }

            if (movesLabel != null) movesLabel.text = runner.Moves.ToString();
            if (remainingLabel != null) remainingLabel.text = runner.Remaining.ToString();
        }

        void HandleLevelLoaded(LevelDefinition level)
        {
            bool useAsset = useLevelAssetText && !string.IsNullOrEmpty(level.title);

            if (titleLabel != null)
                titleLabel.text = useAsset ? level.title : "LEVEL " + (runner.CurrentIndex + 1);

            if (messageLabel != null)
                messageLabel.text = useLevelAssetText ? level.brief : "";
        }

        void HandleCleared(int stars)
        {
            if (messageLabel != null) messageLabel.text = "LEVEL CLEAR    " + stars + " / 3 STARS";
        }

        void HandleFailed()
        {
            if (messageLabel != null) messageLabel.text = "TIME UP";
        }

        void HandleStuck()
        {
            if (messageLabel != null) messageLabel.text = "No room left - press CLEAR CASE and try again";
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            GameObject go = new("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        void BuildHud()
        {
            GameObject canvasGo = new("HUD (runtime)");
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            Transform root = canvasGo.transform;

            titleLabel = Label(root, "TitleLabel", "", new Vector2(0f, 1f), new Vector2(40f, -40f),
                               new Vector2(640f, 56f), 40f, TextAlignmentOptions.TopLeft, boneColor);

            messageLabel = Label(root, "MessageLabel", "", new Vector2(0.5f, 0f), new Vector2(0f, 130f),
                                 new Vector2(1000f, 60f), 28f, TextAlignmentOptions.Center, dimColor);

            timeLabel = Stat(root, "Time", "TIME", new Vector2(-40f, -40f));
            movesLabel = Stat(root, "Moves", "MOVES", new Vector2(-250f, -40f));
            remainingLabel = Stat(root, "Remaining", "LEFT", new Vector2(-460f, -40f));

            if (showButtons)
            {
                MakeButton(root, "ClearButton", "CLEAR CASE", new Vector2(-40f, 40f), runner.ClearCase);
                MakeButton(root, "RetryButton", "RETRY", new Vector2(-250f, 40f), runner.ReloadLevel);
                MakeButton(root, "NextButton", "NEXT LEVEL", new Vector2(-460f, 40f), runner.NextLevel);
            }

            if (showChillToggle) MakeChillToggle(root, new Vector2(40f, 40f));
        }

        TMP_Text Label(Transform parent, string name, string text, Vector2 anchor, Vector2 position,
                       Vector2 size, float fontSize, TextAlignmentOptions align, Color color)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
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

        TMP_Text Stat(Transform parent, string name, string caption, Vector2 position)
        {
            Label(parent, name + "Caption", caption, new Vector2(1f, 1f), position,
                  new Vector2(200f, 28f), 20f, TextAlignmentOptions.TopRight, dimColor);

            return Label(parent, name + "Value", "0", new Vector2(1f, 1f), position + new Vector2(0f, -28f),
                         new Vector2(200f, 56f), 44f, TextAlignmentOptions.TopRight, boneColor);
        }

        void MakeButton(Transform parent, string name, string caption, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(195f, 62f);
            rect.anchoredPosition = position;

            Image background = go.AddComponent<Image>();
            background.color = new Color(0.14f, 0.12f, 0.09f, 0.94f);

            Button button = go.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.30f, 0.26f, 0.15f, 1f);
            button.colors = colors;
            button.onClick.AddListener(action);

            TMP_Text label = Label(go.transform, "Text", caption, new Vector2(0.5f, 0.5f), Vector2.zero,
                                   Vector2.zero, 24f, TextAlignmentOptions.Center, brassColor);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        void MakeChillToggle(Transform parent, Vector2 position)
        {
            GameObject go = new("ChillToggle", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(380f, 46f);
            rect.anchoredPosition = position;

            Toggle toggle = go.AddComponent<Toggle>();

            GameObject boxGo = new("Box", typeof(RectTransform));
            boxGo.transform.SetParent(go.transform, false);
            RectTransform boxRect = boxGo.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0f, 0.5f);
            boxRect.anchorMax = new Vector2(0f, 0.5f);
            boxRect.pivot = new Vector2(0f, 0.5f);
            boxRect.sizeDelta = new Vector2(32f, 32f);
            Image boxImage = boxGo.AddComponent<Image>();
            boxImage.color = new Color(0.14f, 0.12f, 0.09f, 0.94f);

            GameObject markGo = new("Check", typeof(RectTransform));
            markGo.transform.SetParent(boxGo.transform, false);
            RectTransform markRect = markGo.GetComponent<RectTransform>();
            markRect.anchorMin = Vector2.zero;
            markRect.anchorMax = Vector2.one;
            markRect.offsetMin = new Vector2(7f, 7f);
            markRect.offsetMax = new Vector2(-7f, -7f);
            Image markImage = markGo.AddComponent<Image>();
            markImage.color = brassColor;

            TMP_Text label = Label(go.transform, "Text", "Chill mode (no timer)", new Vector2(0f, 0.5f),
                                   new Vector2(46f, 0f), new Vector2(330f, 40f), 22f,
                                   TextAlignmentOptions.Left, dimColor);
            label.rectTransform.pivot = new Vector2(0f, 0.5f);

            toggle.targetGraphic = boxImage;
            toggle.graphic = markImage;
            toggle.isOn = runner.ChillMode;
            toggle.onValueChanged.AddListener(runner.SetChillMode);
        }
    }
}