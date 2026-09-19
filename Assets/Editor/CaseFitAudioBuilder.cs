using CaseFit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CaseFitEditor
{
    public static class CaseFitAudioBuilder
    {
        [MenuItem("Tools/Case Fit/Add Music To This Scene", false, 30)]
        public static void AddMusic()
        {
            MusicPlayer existing = Object.FindFirstObjectByType<MusicPlayer>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                EditorUtility.DisplayDialog("Case Fit",
                    "This scene already has a Music object.\nIt is selected now - drop your audio file into 'Track'.", "OK");
                return;
            }

            GameObject go = new("Music");
            go.AddComponent<AudioSource>();
            MusicPlayer player = go.AddComponent<MusicPlayer>();

            SerializedObject so = new(player);
            so.FindProperty("trackId").stringValue = "bgm";
            so.FindProperty("keepPlayingBetweenScenes").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(go, "Add Music");
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[Case Fit] Music object added. Drop an audio file into its 'Track' field.", go);
        }
    }
}
