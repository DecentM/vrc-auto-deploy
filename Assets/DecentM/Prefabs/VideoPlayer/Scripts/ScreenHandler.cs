using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace DecentM.VideoPlayer
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ScreenHandler : UdonSharpBehaviour
    {
        public Renderer[] screens;

        public float GetBrightness()
        {
            if (this.screens == null || this.screens.Length == 0)
                return 1f;

            Renderer screen = this.screens[0];
            return screen.material.GetFloat("_EmissionStrength");
        }

        public void SetAspectRatio(float aspectRatio)
        {
            foreach (Renderer screen in screens)
            {
                screen.material.SetFloat("_TargetAspectRatio", aspectRatio);
            }
        }

        public void SetBrightness(float alpha)
        {
            foreach (Renderer screen in this.screens)
            {
                screen.material.SetFloat("_EmissionStrength", alpha);
            }
        }

        public void SetIsAVPro(bool isAVPro)
        {
            foreach (Renderer screen in this.screens)
            {
                screen.material.SetInt("_IsAVPro", isAVPro ? 1 : 0);
            }
        }

        public void SetTexture(Texture texture)
        {
            foreach (Renderer screen in this.screens)
            {
                screen.material.SetTexture("_MainTex", texture);
            }
        }

        public void SetSize(Vector2 size)
        {
            foreach (Renderer screen in this.screens)
            {
                screen.transform.localScale = new Vector3(
                    size.x,
                    size.y,
                    screen.transform.localScale.z
                );
            }
        }
    }
}
