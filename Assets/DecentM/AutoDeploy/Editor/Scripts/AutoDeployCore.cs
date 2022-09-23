#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using VRC.Core;
using OtpNet;
using VRC.SDKBase.Editor.BuildPipeline;
using VRC.SDKBase.Editor;
using UnityEngine.SceneManagement;
using JetBrains.Annotations;
using UnityEditor.SceneManagement;
using VRC.SDK3.Editor;
using VRC.SDK3.Editor.Builder;
#endif

namespace DecentM.AutoDeploy
{
    public enum LoginState
    {
        LoggedOut,
        LoggedIn,
        Requires2FA,
        Errored,
    }

    public static class Core
    {
#if UNITY_EDITOR
        private static void Log(string message)
        {
            Debug.Log($"[DecentM.AutoDeploy] {message}");
        }

        private static void LogError(string message)
        {
            Debug.LogError($"[DecentM.AutoDeploy] {message}");
        }

        private static string GenerateAuthCode(string token)
        {
            byte[] secret = Base32Encoding.ToBytes(token.Replace(" ", ""));
            Totp totp = new Totp(secret, totpSize: 6);

            return totp.ComputeTotp(DateTime.UtcNow);
        }

        #region Authentication

        private const int MAX_LOGIN_TRIES = 3;

        [PublicAPI]
        public static void Login(Action<LoginState> OnFinish, int tries = 0)
        {
            LoginAttempt((LoginState state) =>
            {
                if (state == LoginState.LoggedIn)
                {
                    OnFinish(state);
                    return;
                }

                if (tries > MAX_LOGIN_TRIES)
                {
                    LogError($"Failed to log in after {tries + 1} tries. Giving up.");
                    OnFinish(LoginState.Errored);
                    return;
                }

                Login(OnFinish, tries + 1);
            });
        }

        private static void LoginAttempt(Action<LoginState> OnFinish)
        {
            Log("Logging in...");

            if (APIUser.CurrentUser != null)
            {
                Log("Already logged in!");
                OnLoginSuccess(OnFinish);
                return;
            }

            AutoDeploySettings settings = AutoDeploySettings.GetOrCreate();
            AuthSettings authSettings = settings.authSettings;

            if (string.IsNullOrEmpty(authSettings.username) || string.IsNullOrEmpty(authSettings.password))
            {
                LogError("Both username and password must be set to a non-empty value!");
                return;
            }

            Log("Contacting VRChat servers...");

            APIUser.Login(
                authSettings.username,
                authSettings.password,
                (ApiModelContainer<APIUser> login) => OnLoginSuccess(login, OnFinish),
                (ApiModelContainer<APIUser> login) => OnLoginError(login, OnFinish),
                (ApiModelContainer<API2FA> mfa) => OnTwoFactorRequired(mfa, OnFinish)
            );
        }

        private static void OnLoginError(ApiModelContainer<APIUser> login, Action<LoginState> OnFinish)
        {
            LogError($"Login failed. Error code: {login.Code}, Text: {login.Text}, Message: {login.Error}");

            APIUser.Logout();
            OnFinish(LoginState.Errored);
        }

        private static void OnLoginSuccess(Action<LoginState> OnFinish)
        {
            Log($"Login succeeded without user (already logged in)");

            OnFinish(LoginState.LoggedIn);
        }

        private static void OnLoginSuccess(ApiModelContainer<APIUser> login, Action<LoginState> OnFinish)
        {
            Log($"Login succeeded!");

            AutoDeploySettings settings = AutoDeploySettings.GetOrCreate();
            AuthSettings authSettings = settings.authSettings;

            APIUser user = login.Model as APIUser;

            if (login.Cookies.ContainsKey("auth"))
                ApiCredentials.Set(user.username, authSettings.username, "vrchat", login.Cookies["auth"]);
            else
                ApiCredentials.SetHumanName(user.username);

            OnFinish(LoginState.LoggedIn);
        }

        private static void OnTwoFactorRequired(ApiModelContainer<API2FA> login, Action<LoginState> OnFinish)
        {
            Log("Login requires 2FA");

            AutoDeploySettings settings = AutoDeploySettings.GetOrCreate();
            AuthSettings authSettings = settings.authSettings;

            if (string.IsNullOrEmpty(authSettings.otpToken))
            {
                LogError("The configured account has two factor authentication enabled, but there's no OTP token provided. Check the documentation for 2FA setup instructions!");
                return;
            }

            API2FA mfa = login.Model as API2FA;

            if (login.Cookies.ContainsKey("auth"))
                ApiCredentials.Set(authSettings.username, authSettings.username, "vrchat", login.Cookies["auth"]);

            string authCode = GenerateAuthCode(authSettings.otpToken);

            Log("Contacting VRChat servers...");

            APIUser.VerifyTwoFactorAuthCode(
                authCode,
                API2FA.TIME_BASED_ONE_TIME_PASSWORD_AUTHENTICATION,
                authSettings.username,
                authSettings.password,
                (ApiDictContainer response) => OnLoginSuccess(response, OnFinish),
                (ApiContainer response) => OnLoginError(response, OnFinish)
            );
        }

        private static void OnLoginError(ApiContainer login, Action<LoginState> OnFinish)
        {
            LogError($"2FA login failed. Error code: {login.Code}, Text: {login.Text}, Message: {login.Error}");

            APIUser.Logout();
            OnFinish(LoginState.Errored);
        }

        private static void OnLoginSuccess(ApiDictContainer login, Action<LoginState> OnFinish)
        {
            Log($"2FA Login succeeded!");

            AutoDeploySettings settings = AutoDeploySettings.GetOrCreate();
            AuthSettings authSettings = settings.authSettings;

            APIUser user = login.Model as APIUser;
            ApiCredentials.Set(user.username, authSettings.username, "vrchat", login.Cookies["auth"], login.Cookies["twoFactorAuth"]);

            OnFinish(LoginState.LoggedIn);
        }

        #endregion

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
                LogError("The vrc sdk window was not found");
                return;
            }

            sdkWindow.Focus();
        }

        [PublicAPI]
        public static void Build(Action<bool> OnFinish)
        {
            Log("Building...");

            FocusSdkWindow();
            EditorCoroutine.Start(BuildCoroutine(OnFinish));
        }

        public static void BuildWithRuntime()
        {
            Log("Building with runtime...");

            FocusSdkWindow();

            if (tmpObject == null)
                return;

            AutoDeployRuntime runtime = tmpObject.GetComponent<AutoDeployRuntime>();

            if (runtime == null)
                return;

            runtime.BuildAndUpload();
        }

        private static void DirSearch(string sDir)
        {
            foreach (string d in Directory.GetDirectories(sDir))
            {
                foreach (string f in Directory.GetFiles(d))
                {
                    Log(f);
                }

                DirSearch(d);
            }
        }

        private static IEnumerator BuildCoroutine(Action<bool> OnFinish)
        {
            yield return new WaitForSeconds(5);

            bool buildChecks = VRCBuildPipelineCallbacks.OnVRCSDKBuildRequested(VRCSDKRequestedBuildType.Scene);

            if (!buildChecks)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                LogError("At least one build check failed. Investigate the log output above to debug the issue, then build again.");
                OnFinish(false);
                yield return null;
            }

            EnvConfig.ConfigurePlayerSettings();
            EditorPrefs.SetBool("VRC.SDKBase_StripAllShaders", false);

            // I think "shouldBuildUnityPackage" is gonna be the "Future Proof Publishing" option in the UI
            VRC_SdkBuilder.shouldBuildUnityPackage = false;
            VRC_SdkBuilder.PreBuildBehaviourPackaging();

            AssetBundleBuild build = new AssetBundleBuild();
            build.assetNames = new string[] { "Assets/Scenes/MainScene.unity" };
            build.assetBundleName = "scene-StandaloneWindows64-MainScene.vrcw";

            string outputDir = "BuiltScenes";
            string outputPath = $"{outputDir}/{build.assetBundleName}";

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            AssetExporter.DoPreExportShaderReplacement();
            AssetDatabase.RemoveUnusedAssetBundleNames();
            BuildPipeline.BuildAssetBundles(outputDir, new AssetBundleBuild[] { build }, BuildAssetBundleOptions.ForceRebuildAssetBundle, EditorUserBuildSettings.activeBuildTarget);

            EditorPrefs.SetString("lastVRCPath", outputPath);

            OnFinish(true);
            yield return null;
        }

        public static void Upload()
        {
            Log("Uploading...");

            FocusSdkWindow();
            if (APIUser.CurrentUser == null)
            {
                // CurrentUser is null if the user is logged in, but isn't looking at the SDK window.
                // If logged in using the above functions, this should not happen.

                LogError($"No user found in the SDK. Did authentication fail perhaps?");
                return;
            }

            bool hasPermission = APIUser.CurrentUser.canPublishWorlds;

            if (!hasPermission)
            {
                LogError("The currently logged in user does not have permission to upload worlds.");
                return;
            }

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // Make sure the SDK window is focused. Otherwise the temp scene will be marked as dirty o.O
            FocusSdkWindow();

            EditorCoroutine.Start(UploadCoroutine());
        }

        private static IEnumerator UploadCoroutine()
        {
            yield return new WaitForSeconds(5);

            VRC_SdkBuilder.UploadLastExportedSceneBlueprint();

            yield return null;
        }

        #region Runtime game object

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

        #endregion
#endif // #if UNITY_EDITOR
    }
}
