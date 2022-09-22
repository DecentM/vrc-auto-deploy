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
            Core.Login((LoginState state) =>
            {
                if (state != LoginState.LoggedIn)
                {
                    Debug.LogError($"Login failed, login state ended as {state}");
                    return;
                }

                Core.BuildAndUpload();
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

        [MenuItem("DecentM/AutoDeploy/Build And Upload")]
        public static void OnBuild()
        {
            Core.BuildAndUpload();
        }
#endif
    }
}
