#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
#endif

namespace DecentM.AutoDeploy
{
    public static class AutoDeployMenu
    {
#if UNITY_EDITOR
        [MenuItem("DecentM/AutoDeploy/Login And Publish")]
        public static void OnLoginAndPublish()
        {
            Core.Build();

            Core.Login((LoginState state) =>
            {
                if (state != LoginState.LoggedIn)
                {
                    Debug.LogError($"Login failed, login state ended as {state}");
                    return;
                }

                Core.Upload();
            });
        }

        [MenuItem("DecentM/AutoDeploy/Log in")]
        public static void OnLogin()
        {
            Core.Login((LoginState state) =>
            {
                if (state != LoginState.LoggedIn)
                {
                    Debug.LogError($"Login failed, login state ended as {state}");
                    return;
                }

                Debug.Log($"Login state is now {state}");
            });
        }

        [MenuItem("DecentM/AutoDeploy/Build")]
        public static void OnBuild()
        {
            Core.Build();
        }

        [MenuItem("DecentM/AutoDeploy/Upload last build")]
        public static void OnUploadLastBuild()
        {
            Core.Upload();
        }
#endif
    }
}
