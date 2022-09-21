#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;
using VRC.SDKBase.Editor;
using VRC.Core;
#endif

namespace DecentM.AutoDeploy
{
    public static class AutoDeployMenu
    {
#if UNITY_EDITOR
        [MenuItem("DecentM/AutoDeploy/Publish Using Current Settings")]
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
