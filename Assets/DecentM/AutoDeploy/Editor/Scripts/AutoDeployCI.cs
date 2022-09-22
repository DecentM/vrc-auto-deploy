#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
#endif

namespace DecentM.AutoDeploy
{
#if UNITY_EDITOR
    public static class CI
    {
        public static void BuildLoginUpload()
        {
            Core.Build();

            Core.Login((LoginState state) =>
            {
                if (state != LoginState.LoggedIn)
                {
                    Debug.LogError($"Login failed, after retries.");
                    return;
                }

                Core.Upload();
            });
        }
    }
#endif
}