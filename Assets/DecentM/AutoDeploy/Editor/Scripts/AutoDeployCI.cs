#if UNITY_EDITOR
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

namespace DecentM.AutoDeploy
{
#if UNITY_EDITOR
    public static class CI
    {
        public static void Deploy()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);

            Debug.Log($"[AutoDeploy CI] Open scene: {EditorSceneManager.GetActiveScene().name}");

            Core.Login((LoginState state) =>
            {
                if (state != LoginState.LoggedIn)
                {
                    Debug.LogError($"Login failed, after retries.");
                    EditorApplication.Exit(1);
                    return;
                }

                Core.Build();
                Core.Upload();
            });
        }
    }
#endif
}