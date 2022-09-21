#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;
using VRC.SDKBase.Editor;
using VRC.Core;
using OtpNet;
using System.Text;
#endif

namespace DecentM.AutoDeploy
{
    public static class AutoDeployMenu
    {
#if UNITY_EDITOR
        private static string GenerateAuthCode(string token)
        {
            Totp totp = new Totp(Encoding.ASCII.GetBytes(token), totpSize: 4);

            return totp.ComputeTotp();
        }

        [MenuItem("DecentM/AutoDeploy/Login And Publish")]
        public static void LoginAndRun()
        {
            if (APIUser.CurrentUser != null)
                APIUser.Logout();

            AutoDeploySettings settings = AutoDeploySettings.GetOrCreate();

            if (settings.storePlaintextCredentials)
            {
                if (!string.IsNullOrEmpty(settings.username) && !string.IsNullOrEmpty(settings.password) && !settings.use2fa)
                {
                    APIUser.Login(settings.username, settings.password, OnLoginSuccess, OnLoginError);
                    return;
                }

                if (!string.IsNullOrEmpty(settings.username) && !string.IsNullOrEmpty(settings.password) && settings.use2fa && !string.IsNullOrEmpty(settings.otpToken))
                {
                    string authCode = GenerateAuthCode(settings.otpToken);
                    APIUser.VerifyTwoFactorAuthCode(authCode, API2FA.ONE_TIME_PASSWORD_AUTHENTICATION, settings.username, settings.password, OnLoginSuccess, OnLoginError);
                    return;
                }
            }

            string envUsername = Environment.GetEnvironmentVariable("VRC_USERNAME");
            string envPassword = Environment.GetEnvironmentVariable("VRC_PASSWORD");
            string envOtpToken = Environment.GetEnvironmentVariable("VRC_OTP_TOKEN");

            if (!string.IsNullOrEmpty(envUsername) && !string.IsNullOrEmpty(envPassword) && !settings.use2fa)
            {
                APIUser.Login(envUsername, settings.password, OnLoginSuccess, OnLoginError);
                return;
            }

            if (!string.IsNullOrEmpty(envUsername) && !string.IsNullOrEmpty(envPassword) && settings.use2fa && !string.IsNullOrEmpty(envOtpToken))
            {
                string authCode = GenerateAuthCode(envOtpToken);
                APIUser.VerifyTwoFactorAuthCode(authCode, API2FA.ONE_TIME_PASSWORD_AUTHENTICATION, envUsername, envUsername, OnLoginSuccess, OnLoginError);
                return;
            }

            Debug.LogError("[DecentM.AutoDeploy] Misconfiguration detected. If \"Store Plaintext Credentials\" is checked, you must fill in the username and password fields. If 2FA is on, you must provide the OTP token.");
        }

        private static void OnLoginError(ApiModelContainer<APIUser> login)
        {
            Debug.LogError($"Login failed: {login.Error}");
        }

        private static void OnLoginSuccess(ApiModelContainer<APIUser> login)
        {
            Debug.Log("Login succeeded!");
            OnPublishUsingCurrentSettings();
        }

        private static void OnLoginError(ApiContainer login)
        {
            Debug.LogError($"2FA Login failed: {login.Error}");
        }

        private static void OnLoginSuccess(ApiDictContainer login)
        {
            Debug.Log("2FA Login succeeded!");
            OnPublishUsingCurrentSettings();
        }

        [MenuItem("DecentM/AutoDeploy/Publish Using Current Login")]
        public static void OnPublishUsingCurrentSettings()
        {
            Debug.Log("OnPublishUsingCurrentSettings();");

            if (APIUser.CurrentUser == null)
            {
                // TODO: CurrentUser is null if the user is logged in, but isn't looking at the SDK window.

                Debug.LogError($"No user found in the SDK. Did authentication fail perhaps?");
                return;
            }

            bool hasPermission = APIUser.CurrentUser.canPublishWorlds;

            if (!hasPermission)
            {
                Debug.LogError("The currently logged in user does not have permission to upload worlds.");
                return;
            }

            bool buildChecks = VRCBuildPipelineCallbacks.OnVRCSDKBuildRequested(VRCSDKRequestedBuildType.Scene);

            if (!buildChecks)
            {
                EditorUtility.DisplayDialog("Build failed", "At least one build check failed. Investigate the log output above to debug the issue, then build again.", "Ok");
                return;
            }

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            EnvConfig.ConfigurePlayerSettings();
            EditorPrefs.SetBool("VRC.SDKBase_StripAllShaders", false);

            // "shouldBuildUnityPackage" is called future proof publishing in the SDK UI
            VRC_SdkBuilder.shouldBuildUnityPackage = false;

            VRC_SdkBuilder.PreBuildBehaviourPackaging();
            VRC_SdkBuilder.ExportAndUploadSceneBlueprint();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                // The SDK switched to play mode before starting upload (kinda weird, why not just have all the UI in the inspector?)
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.EnteredPlayMode:
                    CreateAndAttachRuntimeObject();
                    EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                    break;
            }
        }

        private static GameObject tmpObject
        {
            get
            {
                Scene scene = SceneManager.GetActiveScene();
                GameObject[] roots = scene.GetRootGameObjects();

                AutoDeployRuntime runtime = null;

                foreach (GameObject root in roots)
                {
                    runtime = root.GetComponent<AutoDeployRuntime>();

                    if (runtime != null)
                        break;

                    runtime = root.GetComponentInChildren<AutoDeployRuntime>();

                    if (runtime != null)
                        break;
                }

                if (runtime == null)
                    return null;

                return runtime.gameObject;
            }
        }

        private static void CreateAndAttachRuntimeObject()
        {
            if (tmpObject != null)
                CleanRuntimeObject();

            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.AddComponent<AutoDeployRuntime>();
            Component.DestroyImmediate(obj.GetComponent<MeshRenderer>());
            Component.DestroyImmediate(obj.GetComponent<BoxCollider>());
            Component.DestroyImmediate(obj.GetComponent<MeshFilter>());

            obj.name = "AutoDeployRuntime";
        }

        private static void CleanRuntimeObject()
        {
            if (tmpObject == null)
                return;

            GameObject.DestroyImmediate(tmpObject);
        }
#endif
    }
}
