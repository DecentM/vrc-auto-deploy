#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using VRC.Core;
using OtpNet;
using VRC.SDKBase.Editor.BuildPipeline;
using VRC.SDKBase.Editor;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Collections;
#endif

namespace DecentM.AutoDeploy
{
    public static class Core
    {
#if UNITY_EDITOR
        private static string GenerateAuthCode(string token)
        {
            byte[] secret = Base32Encoding.ToBytes(token.Replace(" ", ""));
            Totp totp = new Totp(secret, totpSize: 6);

            return totp.ComputeTotp(DateTime.UtcNow);
        }

        public static void LoginAndDeploy()
        {
            if (APIUser.CurrentUser != null)
                APIUser.Logout();

            AutoDeploySettings settings = AutoDeploySettings.GetOrCreate();
            AuthSettings authSettings = settings.authSettings;

            if (string.IsNullOrEmpty(authSettings.username) || string.IsNullOrEmpty(authSettings.password))
            {
                Debug.LogError("[DecentM.AutoDeploy] Both username and password must be set to a non-empty value!");
                return;
            }

            APIUser.Login(authSettings.username, authSettings.password, OnLoginSuccess, OnLoginError, OnTwoFactorRequired);
        }

        private static void OnLoginError(ApiModelContainer<APIUser> login)
        {
            Debug.LogError($"Login failed. Error code: {login.Code}, Text: {login.Text}, Message: {login.Error}");

            APIUser.Logout();
            EditorApplication.Exit(1);
        }

        private static void OnLoginSuccess(ApiModelContainer<APIUser> login)
        {
            Debug.Log($"Login succeeded! Auth: {login.Cookies["auth"]}");

            AutoDeploySettings settings = AutoDeploySettings.GetOrCreate();
            AuthSettings authSettings = settings.authSettings;

            APIUser user = login.Model as APIUser;

            if (login.Cookies.ContainsKey("auth"))
                ApiCredentials.Set(user.username, authSettings.username, "vrchat", login.Cookies["auth"]);
            else
                ApiCredentials.SetHumanName(user.username);

            Deploy();
        }

        private static void OnTwoFactorRequired(ApiModelContainer<API2FA> login)
        {
            Debug.Log("Login requires 2FA");

            AutoDeploySettings settings = AutoDeploySettings.GetOrCreate();
            AuthSettings authSettings = settings.authSettings;

            if (string.IsNullOrEmpty(authSettings.otpToken))
            {
                Debug.LogError("[Decentm.AutoDeploy] The configured account has two factor authentication enabled, but there's no OTP token provided. Check the documentation for 2FA setup instructions!");
                return;
            }

            API2FA mfa = login.Model as API2FA;

            if (login.Cookies.ContainsKey("auth"))
                ApiCredentials.Set(authSettings.username, authSettings.username, "vrchat", login.Cookies["auth"]);

            string authCode = GenerateAuthCode(authSettings.otpToken);
            APIUser.VerifyTwoFactorAuthCode(authCode, API2FA.TIME_BASED_ONE_TIME_PASSWORD_AUTHENTICATION, authSettings.username, authSettings.password, OnLoginSuccess, OnLoginError);
        }

        private static void OnLoginError(ApiContainer login)
        {
            Debug.LogError($"2FA login failed. Error code: {login.Code}, Text: {login.Text}, Message: {login.Error}");

            APIUser.Logout();
            EditorApplication.Exit(1);
        }

        private static void OnLoginSuccess(ApiDictContainer login)
        {
            Debug.Log("2FA Login succeeded!");

            AutoDeploySettings settings = AutoDeploySettings.GetOrCreate();
            AuthSettings authSettings = settings.authSettings;

            APIUser user = login.Model as APIUser;
            ApiCredentials.Set(user.username, authSettings.username, "vrchat", login.Cookies["auth"], login.Cookies["twoFactorAuth"]);

            Deploy();
        }

        private static EditorWindow GetSdkWindow()
        {
            var editorAsm = typeof(VRCSdkControlPanel).Assembly;
            return EditorWindow.GetWindow(editorAsm.GetType("VRCSdkControlPanel"));
        }

        private static void FocusSdkWindow()
        {
            EditorWindow sdkWindow = GetSdkWindow();

            if (sdkWindow == null)
            {
                Debug.LogError("The vrc sdk window was not found");
                return;
            }

            sdkWindow.Focus();
        }

        public static void Deploy()
        {
            if (APIUser.CurrentUser == null)
            {
                // CurrentUser is null if the user is logged in, but isn't looking at the SDK window.
                // If logged in using the above functions, this should not happen.

                Debug.LogError($"No user found in the SDK. Did authentication fail perhaps?");
                return;
            }

            bool hasPermission = APIUser.CurrentUser.canPublishWorlds;

            if (!hasPermission)
            {
                Debug.LogError("The currently logged in user does not have permission to upload worlds.");
                return;
            }

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // Make sure the SDK window is focused. Otherwise the temp scene will be marked as dirty o.O
            FocusSdkWindow();

            // Usually the UI calls this, but we can't go through the UI as it'd necessitate auto-clicking on buttons if that's even possible.
            // We make sure the build hooks work still, to prevent anything bad from getting uploaded.
            bool buildChecks = VRCBuildPipelineCallbacks.OnVRCSDKBuildRequested(VRCSDKRequestedBuildType.Scene);

            if (!buildChecks)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                EditorUtility.DisplayDialog("Build failed", "At least one build check failed. Investigate the log output above to debug the issue, then build again.", "Ok");
                return;
            }

            // Loosely follow what the SDK UI does to make sure that we comply with its process as much as we can
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
                // The SDK switches to play mode before starting upload (kinda weird, why not just have all the UI in the inspector?)
                case PlayModeStateChange.EnteredPlayMode:
                case PlayModeStateChange.ExitingEditMode:
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
