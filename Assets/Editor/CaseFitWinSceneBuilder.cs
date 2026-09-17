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

        static readonly Color Bone = new(0.93f, 0.90f, 0.83f);
        static readonly Color Dim = new(0.59f, 0.56f, 0.49f);
        static readonly Color Brass = new(0.79f, 0.64f, 0.15f);

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

            GameObject canvasGo = new("Win Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            Transform root = canvasGo.transform;

            Centered(root, "Title", "YOU WIN", 120f, new Vector2(0f, 150f), new Vector2(1200f, 180f), Brass);
            Centered(root, "Subtitle", "Every item packed. Case closed.", 34f,
                     new Vector2(0f, 40f), new Vector2(1200f, 60f), Dim);
            TMP_Text stars = Centered(root, "StarsLabel", "0 / 9 STARS", 54f,
                                      new Vector2(0f, -50f), new Vector2(1200f, 90f), Bone);

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
                "\nIn LevelRunner, 'Win Scene Name' must be: Win", "OK");
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

            Image background = go.AddComponent<Image>();
            background.color = new Color(0.16f, 0.14f, 0.10f, 0.95f);

            Button button = go.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.28f, 0.24f, 0.14f, 1f);
            button.colors = colors;

            TMP_Text label = Centered(go.transform, "Text", caption, 26f, Vector2.zero, Vector2.zero, Brass);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
        }
    }
}
