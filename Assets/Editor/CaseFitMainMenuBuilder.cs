using System.Collections.Generic;
using CaseFit;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CaseFitEditor
{
    public static class CaseFitMainMenuBuilder
    {
        const string ScenePath = "Assets/Scenes/MainMenu.unity";

        static readonly Color Bone = new(0.93f, 0.90f, 0.83f);
        static readonly Color Dim = new(0.62f, 0.59f, 0.51f);
        static readonly Color Brass = new(0.85f, 0.70f, 0.20f);
        static readonly Color Panel = new(0.09f, 0.08f, 0.06f, 0.96f);
        static readonly Color ButtonFill = new(0.14f, 0.12f, 0.09f, 0.94f);

        [MenuItem("Tools/Case Fit/Create Main Menu Scene", false, 23)]
        public static void CreateMainMenuScene()
        {
            if (Resources.Load("TMP Settings") == null)
            {
                EditorUtility.DisplayDialog("Case Fit",
                    "TextMeshPro is not imported yet.\nRun Window > TextMeshPro > Import TMP Essential Resources, then try again.",
                    "OK");
                return;
            }

            Scene gameScene = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(gameScene.path))
            {
                EditorUtility.DisplayDialog("Case Fit",
                    "Save the game scene first (Ctrl+S), then run this again.", "OK");
                return;
            }

            if (gameScene.isDirty) EditorSceneManager.SaveScene(gameScene);
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");

            Scene menuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(menuScene);

            GameObject cameraGo = new("Main Camera");
            cameraGo.tag = "MainCamera";
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.07f, 0.05f, 1f);

            GameObject eventSystem = new("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            GameObject canvasGo = new("Menu Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            Transform root = canvasGo.transform;

            GameObject backgroundGo = new("Background  (drop your image here)", typeof(RectTransform));
            backgroundGo.transform.SetParent(root, false);
            Stretch(backgroundGo.GetComponent<RectTransform>());
            Image background = backgroundGo.AddComponent<Image>();
            background.color = new Color(0.13f, 0.11f, 0.08f, 1f);
            background.raycastTarget = false;

            Centered(root, "TitleLabel  (type your game name)", "CASE FIT", 130f,
                     new Vector2(0f, 240f), new Vector2(1400f, 190f), Brass);
            Centered(root, "SubtitleLabel", "Pack every item. Close the case.", 34f,
                     new Vector2(0f, 130f), new Vector2(1200f, 60f), Dim);

            MainMenuController controller = canvasGo.AddComponent<MainMenuController>();

            Button play = MenuButton(root, "PlayButton", "PLAY", new Vector2(0f, -10f));
            Button settings = MenuButton(root, "SettingsButton", "SETTINGS", new Vector2(0f, -110f));
            Button quit = MenuButton(root, "QuitButton", "QUIT", new Vector2(0f, -210f));

            GameObject settingsPanel = BuildSettingsPanel(root, controller, out Slider volumeSlider, out TMP_Text volumeValue);

            UnityEventTools.AddPersistentListener(play.onClick, controller.StartGame);
            UnityEventTools.AddPersistentListener(settings.onClick, controller.OpenSettings);
            UnityEventTools.AddPersistentListener(quit.onClick, controller.QuitGame);

            SerializedObject so = new(controller);
            so.FindProperty("gameSceneName").stringValue = gameScene.name;
            so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            so.FindProperty("volumeSlider").objectReferenceValue = volumeSlider;
            so.FindProperty("volumeValueLabel").objectReferenceValue = volumeValue;
            so.ApplyModifiedPropertiesWithoutUndo();

            settingsPanel.SetActive(false);

            EditorSceneManager.SaveScene(menuScene, ScenePath);
            SceneManager.SetActiveScene(gameScene);
            EditorSceneManager.CloseScene(menuScene, true);

            PutFirstInBuildSettings(ScenePath, gameScene.path);

            EditorUtility.DisplayDialog("Case Fit",
                "Main menu created at " + ScenePath +
                "\n\nIt is now the first scene in the build list, so the game starts there." +
                "\n\nTo make it yours:" +
                "\n- select 'Background' and drop a sprite into Source Image, then set Color to white" +
                "\n- select 'TitleLabel' and type your game name", "OK");
            Debug.Log("[Case Fit] Main menu created at " + ScenePath);
        }

        static GameObject BuildSettingsPanel(Transform root, MainMenuController controller,
                                             out Slider volumeSlider, out TMP_Text volumeValue)
        {
            GameObject panel = new("SettingsPanel", typeof(RectTransform));
            panel.transform.SetParent(root, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(760f, 400f);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = Panel;

            Centered(panel.transform, "PanelTitle", "SETTINGS", 48f,
                     new Vector2(0f, 130f), new Vector2(600f, 70f), Brass);
            Centered(panel.transform, "VolumeLabel", "MASTER VOLUME", 26f,
                     new Vector2(-150f, 30f), new Vector2(340f, 40f), Dim);

            volumeValue = Centered(panel.transform, "VolumeValue", "100%", 30f,
                                   new Vector2(270f, -30f), new Vector2(140f, 44f), Bone);

            volumeSlider = MakeSlider(panel.transform, "VolumeSlider", new Vector2(-40f, -30f), new Vector2(460f, 30f));

            Button close = MenuButton(panel.transform, "CloseButton", "CLOSE", new Vector2(0f, -140f));
            UnityEventTools.AddPersistentListener(close.onClick, controller.CloseSettings);

            return panel;
        }

        static Slider MakeSlider(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            Slider slider = go.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            GameObject backgroundGo = new("Background", typeof(RectTransform));
            backgroundGo.transform.SetParent(go.transform, false);
            RectTransform backgroundRect = backgroundGo.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.25f);
            backgroundRect.anchorMax = new Vector2(1f, 0.75f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            Image backgroundImage = backgroundGo.AddComponent<Image>();
            backgroundImage.color = new Color(0.22f, 0.20f, 0.15f, 1f);

            GameObject fillArea = new("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5f, 0f);
            fillAreaRect.offsetMax = new Vector2(-15f, 0f);

            GameObject fillGo = new("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.sizeDelta = new Vector2(10f, 0f);
            Image fillImage = fillGo.AddComponent<Image>();
            fillImage.color = Brass;

            GameObject handleArea = new("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            GameObject handleGo = new("Handle", typeof(RectTransform));
            handleGo.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handleGo.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            handleRect.sizeDelta = new Vector2(26f, 0f);
            Image handleImage = handleGo.AddComponent<Image>();
            handleImage.color = Bone;

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            return slider;
        }

        static void PutFirstInBuildSettings(string firstPath, string otherPath)
        {
            List<EditorBuildSettingsScene> list = new();
            list.Add(new EditorBuildSettingsScene(firstPath, true));

            foreach (EditorBuildSettingsScene entry in EditorBuildSettings.scenes)
            {
                if (entry.path == firstPath) continue;
                list.Add(entry);
            }

            if (!list.Exists(entry => entry.path == otherPath))
                list.Add(new EditorBuildSettingsScene(otherPath, true));

            EditorBuildSettings.scenes = list.ToArray();
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static TMP_Text Centered(Transform parent, string name, string text, float fontSize,
                                 Vector2 position, Vector2 size, Color color)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        static Button MenuButton(Transform parent, string name, string caption, Vector2 position)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(360f, 78f);
            rect.anchoredPosition = position;

            Image background = go.AddComponent<Image>();
            background.color = ButtonFill;

            Button button = go.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.30f, 0.26f, 0.15f, 1f);
            button.colors = colors;

            TMP_Text label = Centered(go.transform, "Text", caption, 30f, Vector2.zero, Vector2.zero, Brass);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
        }
    }
}
