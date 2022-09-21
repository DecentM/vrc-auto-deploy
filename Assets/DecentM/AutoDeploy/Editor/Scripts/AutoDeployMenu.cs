#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;
using VRC.SDKBase.Editor;
using VRC.Core;
using OtpNet;
using Codice.ThemeImages;
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
