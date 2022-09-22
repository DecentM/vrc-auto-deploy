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
        public static void Deploy()
        {
            Core.Login((LoginState state) =>
            {
                if (state != LoginState.LoggedIn)
                {
                    Debug.LogError($"Login failed, after retries.");
                    return;
                }

                Core.BuildAndUpload();
            });
        }
    }
#endif
}