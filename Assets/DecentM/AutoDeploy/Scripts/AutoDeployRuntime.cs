using UnityEngine;

#if COMPILER_UDONSHARP
using UdonSharp;
#endif

#if UNITY_EDITOR
using UnityEngine.SceneManagement;
using VRC.Core;
using VRCSDK2;
#endif

namespace DecentM.AutoDeploy
{
    internal enum PipelineReadiness
    {
        NotReady,
        Ready,
        Errored,
    }

#if COMPILER_UDONSHARP
    public class AutoDeployRuntime : UdonSharpBehaviour
#else
    public class AutoDeployRuntime : MonoBehaviour
#endif
    {
#if UNITY_EDITOR
        private const float PrepareTimeoutSeconds = 30f;

        private static PipelineReadiness GetPipelineReadiness()
        {
            Scene scene = SceneManager.GetActiveScene();
            PipelineManager pipelineManager = null;

            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                pipelineManager = rootObject.GetComponentInChildren<PipelineManager>();

                if (pipelineManager != null)
                    break;
            }

            if (pipelineManager == null)
            {
                Debug.LogError("[DecentM.AutoDeploy] Pipeline manager component not found in active scene.");
                return PipelineReadiness.Errored;
            }

            return pipelineManager.completedSDKPipeline ? PipelineReadiness.Ready : PipelineReadiness.NotReady;
        }

        private static RuntimeWorldCreation GetWorldCreation()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] rootObjects = scene.GetRootGameObjects();
            RuntimeWorldCreation creation = null;

            foreach (GameObject rootObject in rootObjects)
            {
                RuntimeWorldCreation candidate = rootObject.gameObject.GetComponentInChildren<RuntimeWorldCreation>();

                if (candidate != null)
                {
                    creation = candidate;
                    break;
                }
            }

            return creation;
        }

        private static void SubmitUploadForm()
        {
            RuntimeWorldCreation creation = GetWorldCreation();

            // TODO: read these from settings
            creation.blueprintName.text = "AutoDeploy Test World";
            creation.blueprintDescription.text = "Testing deploying worlds from a CI";
            creation.worldCapacity.text = "64";
            creation.contentSex.isOn = false;
            creation.contentViolence.isOn = false;
            creation.contentGore.isOn = false;
            creation.contentOther.isOn = false;
            creation.releasePublic.isOn = false;
            creation.contentFeatured.isOn = false;
            creation.contentSDKExample.isOn = false;

            // Assuming from how the inspector looks at the build stage, the "Toggle Warrant"
            // checkbox just enables the upload button without calling into any scripts.

            creation.SetupUpload();
        }

        private bool IsSDKPrepared()
        {
            RuntimeWorldCreation creation = GetWorldCreation();

            // Wait for world creation component
            if (creation == null)
                return false;

            // Wait for pipeline to bea ready (world settings loaded into the form)
            PipelineReadiness pipelineReadiness = GetPipelineReadiness();

            switch (pipelineReadiness)
            {
                case PipelineReadiness.Errored:
                    Debug.LogError("[DecentM.AutoDeploy] VRChat SDK pipeline errored, cannot continue building.");
                    this.enabled = false;
                    return false;

                case PipelineReadiness.NotReady:
                    return false;

                case PipelineReadiness.Ready:
                default:
                    this.enabled = false;
                    break;
            }

            return true;
        }

        private float elapsed = 0;

        void Update()
        {
            if (!IsSDKPrepared())
            {
                this.elapsed += Time.deltaTime;

                if (this.elapsed > PrepareTimeoutSeconds)
                {
                    this.elapsed = 0;
                    Debug.LogError($"[DecentM.AutoDeploy] The SDK could not prepare in {PrepareTimeoutSeconds} seconds, timeout.");
                    this.enabled = false;
                    return;
                }

                return;
            }

            SubmitUploadForm();
        }
#endif
    }
}
