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

            Core.Login((LoginState state) =>
            {
                if (state != LoginState.LoggedIn)
                {
                    Debug.LogError($"Login failed, after retries.");
                    EditorApplication.Exit(1);
                    return;
                }

                Core.Build((bool success) =>
                {
                    if (!success)
                    {
                        Debug.LogError($"Build failed, check log output above to diagnose your issue!");
                        return;
                    }

                    Core.Upload();
                });
            });
        }
    }
#endif
}