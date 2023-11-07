using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Speedometer
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private enum DisplayMode
        {
            None,
            MpS,
            KmH,
            MpH
        }

        private static Image _speedBar;
        private static TextMeshProUGUI _speedLabel;
        private static TextMeshProUGUI _zipLabel;
        private static ConfigFile _config;
        private static ConfigEntry<bool> _useTotalSpeed;
        private static ConfigEntry<float> _customSpeedCap;
        private static float _customSpeedCapKmh;
        private static ConfigEntry<DisplayMode> _displayMode;
        private static ConfigEntry<Color> _speedBarColor;
        private static ConfigEntry<bool> _displayOverMaxColor;
        private static ConfigEntry<Color> _overMaxColor;
        private static ConfigEntry<bool> _outlineEnabled;
        private static Material _outlineMaterial;
        private static ConfigEntry<bool> _zipSpeedEnabled;

        private const string MpS = "{0.0} M/S";
        private const string KmhFormat = "{0.0} KM/H";
        private const float KmhFactor = 3.6f;
        private const string MphFormat = "{0.0} MPH";
        private const float MphFactor = 2.236936f;

        private static bool _speedOverMax;

        private const string SettingsHeader = "Settings";
        private const string CosmeticHeader = "Cosmetic/Visual";
        private const string TotalSpeedSetting = "Whether to use the lateral (forward) speed or the total speed of movement.";
        private const string ZipSpeedSetting = "Whether to display the stored speed for a billboard zip glitch or not.";
        private const string CustomCapSetting = "When set to above 0, the speed bar will use this value instead of the game's max speed value to calculate the fill percentage (in km/h).\nIf you use Movement Plus, a value of 400 is a good starting point.";
        private const string DisplaySetting = "How to display the speed as text.\nMpS = Meters per second\nKmH = Kilometers per hour\nMpH = Miles per hour";
        private const string OverMaxSetting = "Whether to change the speedometers color when going over the maximum speed.";
        private const string OutlineSetting = "Enables an outline around the speed and trick counter label, for better readability.";

        private static Plugin _instance;

        private void Awake()
        {
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            _instance = this;

            _config = Config;
            _useTotalSpeed = Config.Bind(SettingsHeader, "Use Total Speed?", true, TotalSpeedSetting);
            _customSpeedCap = Config.Bind(SettingsHeader, "Custom Speed Cap", 0.0f, CustomCapSetting);
            _zipSpeedEnabled = Config.Bind(SettingsHeader, "Display Stored Zip Speed?", false, ZipSpeedSetting);
            _displayMode = Config.Bind(CosmeticHeader, "Speed Display Mode", DisplayMode.KmH, DisplaySetting);
            _speedBarColor = _config.Bind(CosmeticHeader, "Speedometer Color", new Color(0.839f, 0.349f, 0.129f));
            _displayOverMaxColor = _config.Bind(CosmeticHeader, "Display Threshold Color?", true, OverMaxSetting);
            _overMaxColor = _config.Bind(CosmeticHeader, "Threshold Color", new Color(0.898f, 0.098f, 0.443f));
            _outlineEnabled = _config.Bind(CosmeticHeader, "Enable Text Outline?", true, OutlineSetting);

            _customSpeedCapKmh = _customSpeedCap.Value > 0.001f ? _customSpeedCap.Value / KmhFactor : 0.0f;

            Harmony patches = new Harmony("sgiygas.speedometer");
            patches.PatchAll();
        }

        public static void InitializeUI(Transform speedBarBackground, Image speedBar, TextMeshProUGUI tricksLabel)
        {
            // Make the speed bar line up with the original boost bar because I like UI design
            speedBarBackground.position += Vector3.down * 7.5f;
            speedBarBackground.localScale = new Vector3(1.02f, 1.16f, 1.0f);

            _speedBar = speedBar;
            _speedBar.color = _speedBarColor.Value;

            if (_outlineEnabled.Value)
            {
                _outlineMaterial = tricksLabel.fontMaterial;
                _outlineMaterial.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
                _outlineMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.075f);
                tricksLabel.fontSharedMaterial = _outlineMaterial;
            }

            if (_displayMode.Value == DisplayMode.None)
            {
                return;
            }

            // Need to move the original trick label up because it doesn't always display and would look ugly otherwise
            _speedLabel = Instantiate(tricksLabel, tricksLabel.transform.parent);
            _speedLabel.transform.localPosition = tricksLabel.transform.localPosition;
            tricksLabel.transform.localPosition += Vector3.up * 32.0f;

            if (_zipSpeedEnabled.Value)
            {
                _zipLabel = Instantiate(tricksLabel, tricksLabel.transform.parent);
                _zipLabel.transform.localPosition = tricksLabel.transform.localPosition;
                UpdateLastSpeed(0.0f);
                tricksLabel.transform.localPosition += Vector3.up * 32.0f;
            }

            if (_outlineEnabled.Value)
            {
                // I don't like this but for some reason instantiated TextMeshProUGUI instances need a frame to let their properties be modified
                _instance.StartCoroutine(SetLabelOutlines());
            }
        }

        private static IEnumerator SetLabelOutlines()
        {
            yield return new WaitForEndOfFrame();

            _speedLabel.fontSharedMaterial = _outlineMaterial;
            if (_zipSpeedEnabled.Value)
            {
                _zipLabel.fontSharedMaterial = _outlineMaterial;
            }
        }

        public static void UpdateSpeedBar(Reptile.Player player)
        {
            float maxSpeed = _customSpeedCapKmh > 0.001f ? _customSpeedCapKmh : player.maxMoveSpeed;
            float speed = _useTotalSpeed.Value ? player.GetTotalSpeed() : player.GetForwardSpeed();

            //Subtract a small amount from max speed so that the fill amount actually reaches 1.0
            _speedBar.fillAmount = speed / (maxSpeed - 0.01f);

            if (_displayOverMaxColor.Value)
            {
                // Don't assign color directly with an if statement because it dirties the vertices
                // and as such forces a rebuild of the UI mesh/canvas
                bool isOverMax = speed > maxSpeed + 0.01f;
                if (isOverMax)
                {
                    if (!_speedOverMax)
                    {
                        _speedOverMax = true;
                        _speedBar.color = _overMaxColor.Value;
                    }
                }
                else
                {
                    if (_speedOverMax)
                    {
                        _speedOverMax = false;
                        _speedBar.color = _speedBarColor.Value;
                    }
                }
            }

            // Speed label will be null when _displayMode is set to DisplayMode.None
            if (_speedLabel == null)
            {
                return;
            }

            SetSpeedLabelFormatted(speed, _speedLabel);
        }

        public static void UpdateLastSpeed(float speed)
        {
            if (!_zipSpeedEnabled.Value || _zipLabel == null)
            {
                return;
            }

            SetSpeedLabelFormatted(speed, _zipLabel);
        }

        private static void SetSpeedLabelFormatted(float speed, TextMeshProUGUI label)
        {
            if (_displayMode.Value == DisplayMode.MpS)
            {
                label.SetText(MpS, speed);
            }
            else if (_displayMode.Value == DisplayMode.KmH)
            {
                float speedKmh = speed * KmhFactor;
                label.SetText(KmhFormat, speedKmh);
            }
            else
            {
                float speedKmh = speed * MphFactor;
                label.SetText(MphFormat, speedKmh);
            }
        }
    }
}
