using System.Timers;
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
        private const float PipelineReadinessDelaySeconds = 0.5f;

        private static float pipelineReadinessElapsed = 0;

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

            if (!pipelineManager.completedSDKPipeline)
                return PipelineReadiness.NotReady;

            if (pipelineManager.user == null)
                return PipelineReadiness.NotReady;

            pipelineReadinessElapsed += Time.deltaTime;

            if (pipelineReadinessElapsed < PipelineReadinessDelaySeconds)
                return PipelineReadiness.NotReady;

            pipelineReadinessElapsed = 0;

            return PipelineReadiness.Ready;
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

        private static bool isSubmitting = false;

        private static void SubmitUploadForm()
        {
            isSubmitting = true;

            RuntimeWorldCreation creation = GetWorldCreation();
            AutoDeploySettings settings = AutoDeploySettings.GetOrCreate();

            creation.blueprintName.text = settings.worldName;
            creation.blueprintDescription.text = settings.worldDescription;
            creation.worldCapacity.text = settings.worldCapacity;
            creation.contentSex.isOn = settings.contentSex;
            creation.contentViolence.isOn = settings.contentViolence;
            creation.contentGore.isOn = settings.contentGore;
            creation.contentOther.isOn = settings.contentOther;
            creation.releasePublic.isOn = settings.releasePublic;

            // These are disabled in the UI when uploading via the SDK.
            // Maybe they're for world authors to be able to remove tags added by vrc staff?
            creation.contentFeatured.isOn = false;
            creation.contentSDKExample.isOn = false;

            // From how the inspector looks at the build stage, it looks like the "Toggle Warrant"
            // checkbox just enables the upload button without calling into any scripts.

            creation.SetupUpload();
        }

        private bool IsSDKPrepared()
        {
            RuntimeWorldCreation creation = GetWorldCreation();

            // Wait for world creation component
            if (creation == null)
                return false;

            // Wait for pipeline to be ready (world settings loaded into the form)
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

            if (!isSubmitting)
                SubmitUploadForm();
        }
#endif
    }
}
