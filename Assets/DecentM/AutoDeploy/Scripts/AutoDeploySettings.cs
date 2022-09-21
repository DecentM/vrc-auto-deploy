#if UNITY_EDITOR
using System.Collections.Generic;
using System.Configuration;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using Newtonsoft.Json;
#endif

namespace DecentM.AutoDeploy
{
#if UNITY_EDITOR
    public struct AuthSettings
    {
        public AuthSettings(string username, string password, string otpToken)
        {
            this.username = username;
            this.password = password;
            this.otpToken = otpToken;
        }

        public string username;
        public string password;
        public string otpToken;
    }

    public class AutoDeploySettings : ScriptableObject
    {
        // Auth
        public bool storePlaintextCredentials = false;
        public string username = string.Empty;
        public string password = string.Empty;
        public bool use2fa = false;
        public string otpToken = string.Empty;

        public AuthSettings authSettings
        {
            get
            {
                string path = Path.Combine(Application.dataPath, ".ignored", "auth.json");
                Debug.Log($"[DecentM.AutoDeploy] Looking for auth file at {path}");

                if (File.Exists(path))
                {
                    Debug.Log("[DecentM.AutoDeploy] Found auth data, reading from disk...");
                    return JsonConvert.DeserializeObject<AuthSettings>(File.ReadAllText(path));
                }

                Debug.Log("[DecentM.AutoDeploy] Auth data doesn't exist, reading from project settings");
                return new AuthSettings(
                    this.username,
                    this.password,
                    this.otpToken
                );
            }
        }

        // World
        public string worldName = "My World";
        public string worldDescription = string.Empty;
        public string worldCapacity = "64";
        public bool contentSex = false;
        public bool contentViolence = false;
        public bool contentGore = false;
        public bool contentOther = false;
        public bool releasePublic = false;
        public bool contentFeatured = false;
        public bool contentSDKExample = false;

        private static AutoDeploySettings _settings;

        private const string SettingsPath = "Assets/Editor/DecentM/AutoDeploy/Settings.asset";

        private static AutoDeploySettings CreateSettings()
        {
            if (!AssetDatabase.IsValidFolder(Path.GetDirectoryName(SettingsPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));

            _settings = CreateInstance<AutoDeploySettings>();
            AssetDatabase.CreateAsset(_settings, SettingsPath);
            AssetDatabase.SaveAssets();

            return _settings;
        }

        public static AutoDeploySettings GetOrCreate()
        {
            AutoDeploySettings settings = AssetDatabase.LoadAssetAtPath<AutoDeploySettings>(SettingsPath);

            if (settings == null)
                settings = CreateSettings();

            return settings;
        }
    }

    internal sealed class AutoDeploySettingsProvider
    {
        [UnityEditor.SettingsProvider]
        public static UnityEditor.SettingsProvider CreateSettingsProvider()
        {
            UnityEditor.SettingsProvider provider = new UnityEditor.SettingsProvider("Project/DecentM/AutoDeploy", SettingsScope.Project)
            {
                label = "AutoDeploy",
                keywords = new HashSet<string>(new string[] { "DecentM", "CI", "Deploy", "Auto" }),
                guiHandler = (searchContext) =>
                {
                    AutoDeploySettings settings = AutoDeploySettings.GetOrCreate();
                    SerializedObject settingsObject = new SerializedObject(settings);

                    EditorGUI.BeginChangeCheck();

                    // Authentication settings
                    EditorGUILayout.LabelField("Authentication", EditorStyles.boldLabel);

                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(AutoDeploySettings.storePlaintextCredentials)), new GUIContent("Store plaintext credentials in project"));

                    if (settings.storePlaintextCredentials)
                    {
                        EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(AutoDeploySettings.username)), new GUIContent("Username/E-mail address"));
                        EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(AutoDeploySettings.password)), new GUIContent("Password"));

                        EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(AutoDeploySettings.use2fa)), new GUIContent("Use 2-factor authentication"));

                        if (settings.use2fa)
                        {
                            EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(AutoDeploySettings.otpToken)), new GUIContent("2FA Token (OTP only)"));
                        } else
                        {
                            settings.otpToken = string.Empty;
                        }
                    } else
                    {
                        settings.username = string.Empty;
                        settings.password = string.Empty;
                        settings.otpToken = string.Empty;
                    }

                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("World settings", EditorStyles.boldLabel);

                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(AutoDeploySettings.worldName)), new GUIContent("Name"));
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(AutoDeploySettings.worldDescription)), new GUIContent("Description"));
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(AutoDeploySettings.worldCapacity)), new GUIContent("Capacity"));
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(AutoDeploySettings.contentSex)), new GUIContent("Content warning: Sex"));
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(AutoDeploySettings.contentGore)), new GUIContent("Content warning: Gore"));
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(AutoDeploySettings.contentViolence)), new GUIContent("Content warning: Violence"));
                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(AutoDeploySettings.contentOther)), new GUIContent("Content warning: Other"));

                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(settingsObject.FindProperty(nameof(AutoDeploySettings.releasePublic)), new GUIContent("Public release"));

                    if (settings.releasePublic)
                        EditorGUILayout.HelpBox("If this world is a private world, it will be submitted to Community Labs.", MessageType.Info);
                    else
                        EditorGUILayout.HelpBox("If this world is a public world, it will be privated and removed from public lists.", MessageType.Info);

                    EditorGUILayout.Space();

                    if (EditorGUI.EndChangeCheck())
                    {
                        settingsObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(settings);
                    }
                },
            };

            return provider;
        }
    }
#endif
}

