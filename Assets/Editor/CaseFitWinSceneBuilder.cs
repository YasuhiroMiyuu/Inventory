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
    public static class CaseFitWinSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Win.unity";

        static readonly Color White = Color.white;
        static readonly Color SoftWhite = new(1f, 1f, 1f, 0.72f);

        // Button face: 3B3B3B, with a lighter hover and a darker press.
        static readonly Color ButtonNormal = new Color32(0x3B, 0x3B, 0x3B, 0xFF);
        static readonly Color ButtonHover = new Color32(0x55, 0x55, 0x55, 0xFF);
        static readonly Color ButtonPressed = new Color32(0x2A, 0x2A, 0x2A, 0xFF);

        [MenuItem("Tools/Case Fit/Create Win Scene", false, 22)]
        public static void CreateWinScene()
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
                    "Save the current scene first (Ctrl+S), then run this again.", "OK");
                return;
            }

            if (gameScene.isDirty) EditorSceneManager.SaveScene(gameScene);
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");

            Scene winScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(winScene);

            GameObject cameraGo = new("Main Camera");
            cameraGo.tag = "MainCamera";
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.07f, 0.05f, 1f);

            GameObject eventSystem = new("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            GameObject musicGo = new("Music");
            musicGo.AddComponent<AudioSource>();
            MusicPlayer music = musicGo.AddComponent<MusicPlayer>();
            SerializedObject musicSo = new(music);
            musicSo.FindProperty("trackId").stringValue = "bgm";
            musicSo.FindProperty("keepPlayingBetweenScenes").boolValue = true;
            musicSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject canvasGo = new("Win Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            Transform root = canvasGo.transform;

            // Full-screen picture. Drop a sprite into Source Image and the placeholder grey goes away.
            GameObject backgroundGo = new("Background  (drop your image here)", typeof(RectTransform));
            backgroundGo.transform.SetParent(root, false);
            Stretch(backgroundGo.GetComponent<RectTransform>());
            Image background = backgroundGo.AddComponent<Image>();
            background.color = new Color(0.13f, 0.11f, 0.08f, 1f);
            background.raycastTarget = false;

            // Dark veil so white text stays readable over a busy photo.
            GameObject veilGo = new("Overlay  (lower Alpha for a brighter image)", typeof(RectTransform));
            veilGo.transform.SetParent(root, false);
            Stretch(veilGo.GetComponent<RectTransform>());
            Image veil = veilGo.AddComponent<Image>();
            veil.color = new Color(0f, 0f, 0f, 0.55f);
            veil.raycastTarget = false;

            Centered(root, "Title", "YOU WIN", 120f, new Vector2(0f, 150f), new Vector2(1200f, 180f), White);
            Centered(root, "Subtitle", "Every item packed. Case closed.", 34f,
                     new Vector2(0f, 40f), new Vector2(1200f, 60f), SoftWhite);
            TMP_Text stars = Centered(root, "StarsLabel", "0 / 9 STARS", 54f,
                                      new Vector2(0f, -50f), new Vector2(1200f, 90f), White);

            CaseFitWinScreen screen = canvasGo.AddComponent<CaseFitWinScreen>();
            SerializedObject screenSo = new(screen);
            screenSo.FindProperty("starsLabel").objectReferenceValue = stars;
            screenSo.FindProperty("gameSceneName").stringValue = gameScene.name;
            screenSo.ApplyModifiedPropertiesWithoutUndo();

            Button again = CenteredButton(root, "PlayAgainButton", "PLAY AGAIN", new Vector2(-120f, -200f));
            Button quit = CenteredButton(root, "QuitButton", "QUIT", new Vector2(120f, -200f));
            UnityEventTools.AddPersistentListener(again.onClick, screen.PlayAgain);
            UnityEventTools.AddPersistentListener(quit.onClick, screen.QuitGame);

            EditorSceneManager.SaveScene(winScene, ScenePath);
            SceneManager.SetActiveScene(gameScene);
            EditorSceneManager.CloseScene(winScene, true);

            EnsureInBuildSettings(gameScene.path, ScenePath);

            EditorUtility.DisplayDialog("Case Fit",
                "Win scene created at " + ScenePath +
                "\n\nBoth scenes were added to the build list." +
                "\nIn LevelRunner, 'Win Scene Name' must be: Win" +
                "\n\nTo make it yours:" +
                "\n- select 'Background', drop a sprite into Source Image, set Color to white" +
                "\n- select 'Overlay' and drag Alpha down if the image looks too dark" +
                "\n- select 'Music' and drop an audio file into Track", "OK");
            Debug.Log("[Case Fit] Win scene created at " + ScenePath);
        }

        static void EnsureInBuildSettings(params string[] paths)
        {
            List<EditorBuildSettingsScene> list = new(EditorBuildSettings.scenes);

            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                if (list.Exists(entry => entry.path == path)) continue;
                list.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = list.ToArray();
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
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

        static Button CenteredButton(Transform parent, string name, string caption, Vector2 position)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(220f, 70f);
            rect.anchoredPosition = position;

            // The Image stays white; the Button's own colour states paint 3B3B3B over it,
            // which is what lets the hover state be lighter rather than darker.
            Image background = go.AddComponent<Image>();
            background.color = Color.white;

            Button button = go.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = ButtonNormal;
            colors.highlightedColor = ButtonHover;
            colors.pressedColor = ButtonPressed;
            colors.selectedColor = ButtonNormal;
            colors.disabledColor = new Color32(0x2A, 0x2A, 0x2A, 0x80);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.targetGraphic = background;

            TMP_Text label = Centered(go.transform, "Text", caption, 26f, Vector2.zero, Vector2.zero, White);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
        }
    }
}
