using AOSharp.Core.UI;
using AOSharp.Core;
using AOSharp.Common.GameData;
using Buddy.Shared.UI;

namespace ZeroIn
{
    public class ScanSettingsView : CustomView
    {
        private TextInputView _scanSpacing;
        private TextInputView _detectionRange;
        private Checkbox _onlyAFK;
        private TextInputView _afkCheckTime;
        private Checkbox _saveToJson;
        private Checkbox _saveToCsv;
        private Checkbox _logToConsole;
        private Checkbox _continuousScanning;
        private Button _toggleDetectionRadius;
        private Button _togglePlayerMarkers;
        private Button _toggleAFKPaths;
        private Button _toggleActivePlayerPaths;
        private Button _toggleTagOnly;
        private Checkbox _enableCombat;
        private Checkbox _enableLooting;
        private Checkbox _enableHealthCheck;

        // Public properties for access from other classes
        public float ScanSpacing
        {
            get
            {
                if (_scanSpacing != null && float.TryParse(_scanSpacing.Text, out float value))
                    return value;
                return 40f; // Default
            }
        }

        public ScanSettingsView(string xmlPath) : base(xmlPath)
        {
            try
            {
                if (Root != null)
                {
                    Root.FindChild("ScanSpacingValue", out _scanSpacing);
                    Root.FindChild("DetectionRangeValue", out _detectionRange);
                    Root.FindChild("OnlyAFK", out _onlyAFK);
                    Root.FindChild("AFKCheckTimeValue", out _afkCheckTime);
                    Root.FindChild("SaveToJson", out _saveToJson);
                    Root.FindChild("SaveToCsv", out _saveToCsv);
                    Root.FindChild("LogToConsole", out _logToConsole);
                    Root.FindChild("ContinuousScanning", out _continuousScanning);
                    Root.FindChild("ToggleDetectionRadius", out _toggleDetectionRadius);
                    Root.FindChild("TogglePlayerMarkers", out _togglePlayerMarkers);
                    Root.FindChild("ToggleAFKPaths", out _toggleAFKPaths);
                    Root.FindChild("ToggleActivePlayerPaths", out _toggleActivePlayerPaths);
                    Root.FindChild("ToggleTagOnly", out _toggleTagOnly);
                    Root.FindChild("EnableCombat", out _enableCombat);
                    Root.FindChild("EnableLooting", out _enableLooting);
                    Root.FindChild("EnableHealthCheck", out _enableHealthCheck);

                    // Wire up button click handlers with instant feedback
                    if (_toggleDetectionRadius != null)
                    {
                        _toggleDetectionRadius.Clicked = (s, e) =>
                        {
                            ZeroIn.Config.ShowDetectionRadius = !ZeroIn.Config.ShowDetectionRadius;
                            Chat.WriteLine($"[ZeroIn] Detection radius: {(ZeroIn.Config.ShowDetectionRadius ? "ON" : "OFF")}",
                                ZeroIn.Config.ShowDetectionRadius ? ChatColor.Green : ChatColor.Red);
                        };
                    }

                    if (_togglePlayerMarkers != null)
                    {
                        _togglePlayerMarkers.Clicked = (s, e) =>
                        {
                            ZeroIn.Config.ShowPlayerMarkers = !ZeroIn.Config.ShowPlayerMarkers;
                            Chat.WriteLine($"[ZeroIn] Player markers: {(ZeroIn.Config.ShowPlayerMarkers ? "ON" : "OFF")}",
                                ZeroIn.Config.ShowPlayerMarkers ? ChatColor.Green : ChatColor.Red);
                        };
                    }

                    if (_toggleAFKPaths != null)
                    {
                        _toggleAFKPaths.Clicked = (s, e) =>
                        {
                            ZeroIn.Config.ShowAFKPaths = !ZeroIn.Config.ShowAFKPaths;
                            Chat.WriteLine($"[ZeroIn] AFK player paths: {(ZeroIn.Config.ShowAFKPaths ? "ON" : "OFF")}",
                                ZeroIn.Config.ShowAFKPaths ? ChatColor.Green : ChatColor.Red);
                        };
                    }

                    if (_toggleActivePlayerPaths != null)
                    {
                        _toggleActivePlayerPaths.Clicked = (s, e) =>
                        {
                            ZeroIn.Config.ShowActivePlayerPaths = !ZeroIn.Config.ShowActivePlayerPaths;
                            Chat.WriteLine($"[ZeroIn] Active player paths: {(ZeroIn.Config.ShowActivePlayerPaths ? "ON" : "OFF")}",
                                ZeroIn.Config.ShowActivePlayerPaths ? ChatColor.Green : ChatColor.Red);
                        };
                    }

                    if (_toggleTagOnly != null)
                    {
                        _toggleTagOnly.Clicked = (s, e) =>
                        {
                            ZeroIn.Config.ShowMarkerLines = !ZeroIn.Config.ShowMarkerLines;
                            Chat.WriteLine($"[ZeroIn] Line mode: {(ZeroIn.Config.ShowMarkerLines ? "ON (shapes)" : "OFF (minimal waypoints)")}",
                                ZeroIn.Config.ShowMarkerLines ? ChatColor.Green : ChatColor.Red);
                        };
                    }
                }
            }
            catch (System.Exception ex)
            {
                Chat.WriteLine($"[ScanSettingsView] Error initializing: {ex.Message}", ChatColor.Red);
            }
        }

        public void SetData(ZeroInConfig config)
        {
            if (config == null) return;

            try
            {
                if (_scanSpacing != null)
                    _scanSpacing.Text = config.ScanSpacing.ToString();

                if (_detectionRange != null)
                    _detectionRange.Text = config.PlayerDetectionRange.ToString();

                if (_onlyAFK != null)
                    _onlyAFK.SetValue(config.OnlyAFK);

                if (_afkCheckTime != null)
                    _afkCheckTime.Text = config.AFKCheckTimeSeconds.ToString();

                if (_saveToJson != null)
                    _saveToJson.SetValue(config.SaveToJson);

                if (_saveToCsv != null)
                    _saveToCsv.SetValue(config.SaveToCsv);

                if (_logToConsole != null)
                    _logToConsole.SetValue(config.LogToConsole);

                if (_continuousScanning != null)
                    _continuousScanning.SetValue(config.ContinuousScanning);

                // Button labels are static in XML - toggles provide chat feedback only

                if (_enableCombat != null)
                    _enableCombat.SetValue(config.EnableCombat);

                if (_enableLooting != null)
                    _enableLooting.SetValue(config.EnableLooting);

                if (_enableHealthCheck != null)
                    _enableHealthCheck.SetValue(config.EnableHealthCheck);
            }
            catch (System.Exception ex)
            {
                Chat.WriteLine($"[ScanSettingsView] Error setting data: {ex.Message}", ChatColor.Red);
            }
        }

        public void UpdateConfig(ZeroInConfig config)
        {
            if (config == null) return;

            try
            {
                if (_scanSpacing != null && float.TryParse(_scanSpacing.Text, out float spacing))
                    config.ScanSpacing = spacing;

                if (_detectionRange != null && float.TryParse(_detectionRange.Text, out float range))
                    config.PlayerDetectionRange = range;

                if (_onlyAFK != null)
                    config.OnlyAFK = _onlyAFK.IsChecked;

                if (_afkCheckTime != null && int.TryParse(_afkCheckTime.Text, out int afkTime))
                    config.AFKCheckTimeSeconds = afkTime;

                if (_saveToJson != null)
                    config.SaveToJson = _saveToJson.IsChecked;

                if (_saveToCsv != null)
                    config.SaveToCsv = _saveToCsv.IsChecked;

                if (_logToConsole != null)
                    config.LogToConsole = _logToConsole.IsChecked;

                if (_continuousScanning != null)
                    config.ContinuousScanning = _continuousScanning.IsChecked;

                // Visual settings are managed by button clicks, no need to update from buttons here
                // They update config directly when clicked

                if (_enableCombat != null)
                    config.EnableCombat = _enableCombat.IsChecked;

                if (_enableLooting != null)
                    config.EnableLooting = _enableLooting.IsChecked;

                if (_enableHealthCheck != null)
                    config.EnableHealthCheck = _enableHealthCheck.IsChecked;
            }
            catch (System.Exception ex)
            {
                Chat.WriteLine($"[ScanSettingsView] Error updating config: {ex.Message}", ChatColor.Red);
            }
        }
    }
}
