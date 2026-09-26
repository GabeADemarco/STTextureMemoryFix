using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace STTextureMemoryFix
{
    [BepInPlugin(
        "com.sttexturememoryfix.plugin",
        "ST Texture Memory Fix",
        "1.1.0")]
    public class TextureMemoryFix : BaseUnityPlugin
    {
        private ConfigEntry<int> mipmapLimit;
        private ConfigEntry<KeyCode> decreaseKey;
        private ConfigEntry<KeyCode> increaseKey;

        private PropertyInfo mipLimitProperty;
        private int currentLimit = 1;

        private string notificationText = "";
        private float notificationUntil = 0f;

        private GUIStyle notificationStyle;
        private Texture2D notificationBackground;

        private void Awake()
        {
            Logger.LogInfo("==========================================");
            Logger.LogInfo("ST Texture Memory Fix 1.1.0 loaded");

            mipmapLimit = Config.Bind(
                "Texture Quality",
                "MipmapLimit",
                1,
                "Global Unity texture mipmap limit. 0 = vanilla/full resolution, 1 = half resolution (recommended), 2 = quarter resolution (aggressive memory saving). Values outside 0-2 are clamped."
            );

            decreaseKey = Config.Bind(
                "Hotkeys",
                "DecreaseMipmapLimit",
                KeyCode.F6,
                "Increase texture quality / use more memory. Default: F6. Moves 2 -> 1 -> 0."
            );

            increaseKey = Config.Bind(
                "Hotkeys",
                "IncreaseMipmapLimit",
                KeyCode.F7,
                "Reduce texture quality / use less memory. Default: F7. Moves 0 -> 1 -> 2."
            );

            mipLimitProperty = typeof(QualitySettings).GetProperty(
                "globalTextureMipmapLimit",
                BindingFlags.Public | BindingFlags.Static
            );

            if (mipLimitProperty == null ||
                !mipLimitProperty.CanRead ||
                !mipLimitProperty.CanWrite)
            {
                Logger.LogError(
                    "QualitySettings.globalTextureMipmapLimit is unavailable or not writable. No texture setting was changed."
                );
                Logger.LogInfo("==========================================");
                return;
            }

            int requestedLimit = Mathf.Clamp(mipmapLimit.Value, 0, 2);

            if (requestedLimit != mipmapLimit.Value)
            {
                Logger.LogWarning(
                    "MipmapLimit " + mipmapLimit.Value +
                    " is outside the supported range 0-2; using " +
                    requestedLimit + "."
                );

                mipmapLimit.Value = requestedLimit;
                Config.Save();
            }

            ApplyMipmapLimit(requestedLimit, false);

            Logger.LogInfo(
                "Hotkeys: " +
                decreaseKey.Value +
                " = better texture quality / more memory, " +
                increaseKey.Value +
                " = lower texture quality / less memory."
            );

            Logger.LogInfo(
                "This plugin does not modify mipmap streaming, autosave, or gameplay logic."
            );

            Logger.LogInfo("==========================================");
        }

        private void Update()
        {
            if (mipLimitProperty == null)
                return;

            if (Input.GetKeyDown(decreaseKey.Value))
            {
                SetLimitFromHotkey(currentLimit - 1);
            }

            if (Input.GetKeyDown(increaseKey.Value))
            {
                SetLimitFromHotkey(currentLimit + 1);
            }
        }

        private void SetLimitFromHotkey(int newLimit)
        {
            newLimit = Mathf.Clamp(newLimit, 0, 2);

            if (newLimit == currentLimit)
            {
                ShowNotification(GetSettingDescription(currentLimit));
                return;
            }

            ApplyMipmapLimit(newLimit, true);

            mipmapLimit.Value = currentLimit;
            Config.Save();
        }

        private void ApplyMipmapLimit(int requestedLimit, bool showNotification)
        {
            try
            {
                int oldValue = Convert.ToInt32(
                    mipLimitProperty.GetValue(null, null)
                );

                Logger.LogInfo(
                    "QualitySettings.globalTextureMipmapLimit BEFORE: " +
                    oldValue
                );

                mipLimitProperty.SetValue(null, requestedLimit, null);

                currentLimit = Convert.ToInt32(
                    mipLimitProperty.GetValue(null, null)
                );

                Logger.LogInfo(
                    "QualitySettings.globalTextureMipmapLimit AFTER: " +
                    currentLimit
                );

                Logger.LogInfo(GetSettingDescription(currentLimit));

                if (showNotification)
                {
                    ShowNotification(GetSettingDescription(currentLimit));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    "Failed to apply globalTextureMipmapLimit=" +
                    requestedLimit + ": " + ex
                );

                ShowNotification(
                    "Texture Memory Fix: failed to change texture setting."
                );
            }
        }

        private string GetSettingDescription(int limit)
        {
            if (limit <= 0)
            {
                return "Texture Memory Fix: 0 - Full resolution (vanilla)";
            }

            if (limit == 1)
            {
                return "Texture Memory Fix: 1 - Half resolution (recommended)";
            }

            return "Texture Memory Fix: 2 - Quarter resolution (aggressive)";
        }

        private void ShowNotification(string text)
        {
            notificationText = text;
            notificationUntil = Time.realtimeSinceStartup + 3.0f;
        }

        private void EnsureNotificationStyle()
        {
            if (notificationStyle != null)
                return;

            notificationBackground = new Texture2D(1, 1);
            notificationBackground.SetPixel(
                0,
                0,
                new Color(0f, 0f, 0f, 0.72f)
            );
            notificationBackground.Apply();

            notificationStyle = new GUIStyle(GUI.skin.box);
            notificationStyle.alignment = TextAnchor.MiddleCenter;
            notificationStyle.fontSize = 18;
            notificationStyle.normal.textColor = Color.white;
            notificationStyle.normal.background = notificationBackground;
            notificationStyle.padding = new RectOffset(14, 14, 8, 8);
        }

        private void OnGUI()
        {
            if (Time.realtimeSinceStartup >= notificationUntil ||
                string.IsNullOrEmpty(notificationText))
            {
                return;
            }

            EnsureNotificationStyle();

            const float width = 560f;
            const float height = 44f;

            Rect rect = new Rect(
                (Screen.width - width) * 0.5f,
                24f,
                width,
                height
            );

            GUI.Box(rect, notificationText, notificationStyle);
        }

        private void OnDestroy()
        {
            if (notificationBackground != null)
            {
                Destroy(notificationBackground);
                notificationBackground = null;
            }
        }
    }
}
