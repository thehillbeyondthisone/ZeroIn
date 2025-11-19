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
        private Checkbox _showDetectionRadius;
        private Checkbox _showPlayerMarkers;
        private Checkbox _showAFKPaths;
        private Checkbox _tagOnlyMode;
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
                    Root.FindChild("ShowDetectionRadius", out _showDetectionRadius);
                    Root.FindChild("ShowPlayerMarkers", out _showPlayerMarkers);
                    Root.FindChild("ShowAFKPaths", out _showAFKPaths);
                    Root.FindChild("TagOnlyMode", out _tagOnlyMode);
                    Root.FindChild("EnableCombat", out _enableCombat);
                    Root.FindChild("EnableLooting", out _enableLooting);
                    Root.FindChild("EnableHealthCheck", out _enableHealthCheck);

                    // Wire up event handlers for visual settings with chat feedback
                    if (_showDetectionRadius != null)
                    {
                        _showDetectionRadius.Clicked += (s, e) =>
                        {
                            ZeroIn.Config.ShowDetectionRadius = _showDetectionRadius.IsChecked;
                            Chat.WriteLine($"[ZeroIn] Detection radius: {(ZeroIn.Config.ShowDetectionRadius ? "ON" : "OFF")}",
                                ZeroIn.Config.ShowDetectionRadius ? ChatColor.Green : ChatColor.Red);
                        };
                    }

                    if (_showPlayerMarkers != null)
                    {
                        _showPlayerMarkers.Clicked += (s, e) =>
                        {
                            ZeroIn.Config.ShowPlayerMarkers = _showPlayerMarkers.IsChecked;
                            Chat.WriteLine($"[ZeroIn] Player markers: {(ZeroIn.Config.ShowPlayerMarkers ? "ON" : "OFF")}",
                                ZeroIn.Config.ShowPlayerMarkers ? ChatColor.Green : ChatColor.Red);
                        };
                    }

                    if (_showAFKPaths != null)
                    {
                        _showAFKPaths.Clicked += (s, e) =>
                        {
                            ZeroIn.Config.ShowAFKPaths = _showAFKPaths.IsChecked;
                            Chat.WriteLine($"[ZeroIn] AFK paths: {(ZeroIn.Config.ShowAFKPaths ? "ON" : "OFF")}",
                                ZeroIn.Config.ShowAFKPaths ? ChatColor.Green : ChatColor.Red);
                        };
                    }

                    if (_tagOnlyMode != null)
                    {
                        _tagOnlyMode.Clicked += (s, e) =>
                        {
                            ZeroIn.Config.TagOnlyMode = _tagOnlyMode.IsChecked;
                            Chat.WriteLine($"[ZeroIn] Tag-only mode: {(ZeroIn.Config.TagOnlyMode ? "ON (names only)" : "OFF (shapes visible)")}",
                                ZeroIn.Config.TagOnlyMode ? ChatColor.Green : ChatColor.Red);
                        };
                    }

                    if (_continuousScanning != null)
                    {
                        _continuousScanning.Clicked += (s, e) =>
                        {
                            ZeroIn.Config.ContinuousScanning = _continuousScanning.IsChecked;
                            Chat.WriteLine($"[ZeroIn] Continuous scanning: {(ZeroIn.Config.ContinuousScanning ? "ON" : "OFF")}",
                                ZeroIn.Config.ContinuousScanning ? ChatColor.Green : ChatColor.Red);
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

                if (_showDetectionRadius != null)
                    _showDetectionRadius.SetValue(config.ShowDetectionRadius);

                if (_showPlayerMarkers != null)
                    _showPlayerMarkers.SetValue(config.ShowPlayerMarkers);

                if (_showAFKPaths != null)
                    _showAFKPaths.SetValue(config.ShowAFKPaths);

                if (_tagOnlyMode != null)
                    _tagOnlyMode.SetValue(config.TagOnlyMode);

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

                if (_showDetectionRadius != null)
                    config.ShowDetectionRadius = _showDetectionRadius.IsChecked;

                if (_showPlayerMarkers != null)
                    config.ShowPlayerMarkers = _showPlayerMarkers.IsChecked;

                if (_showAFKPaths != null)
                    config.ShowAFKPaths = _showAFKPaths.IsChecked;

                if (_tagOnlyMode != null)
                    config.TagOnlyMode = _tagOnlyMode.IsChecked;

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
