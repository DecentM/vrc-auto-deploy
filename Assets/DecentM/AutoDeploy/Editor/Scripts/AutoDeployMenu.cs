#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DecentM.AutoDeploy
{
    public static class AutoDeployMenu
    {
#if UNITY_EDITOR
        [MenuItem("DecentM/AutoDeploy/Login And Publish")]
        public static void LoginAndPublish()
        {
            Core.LoginAndDeploy();
        }

        [MenuItem("DecentM/AutoDeploy/Publish Using Current Login")]
        public static void OnPublishUsingCurrentLogin()
        {
            Core.Deploy();
        }
#endif
    }
}
