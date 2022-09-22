#if UNITY_EDITOR
using UnityEngine;
using UnityEditor.SceneManagement;
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
                    return;
                }

                Core.Build();
                Core.Upload();
            });
        }
    }
#endif
}