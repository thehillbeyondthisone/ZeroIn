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
        private TextInputView _afkCheckTime;

        // All toggle buttons
        private Button _toggleOnlyAFK;
        private Button _toggleContinuousScanning;
        private Button _toggleSaveJson;
        private Button _toggleSaveCsv;
        private Button _toggleLogConsole;
        private Button _toggleDetectionRadius;
        private Button _togglePlayerMarkers;
        private Button _toggleAFKPaths;
        private Button _toggleActivePlayerPaths;
        private Button _toggleTagOnly;
        private Button _toggleCombat;
        private Button _toggleLooting;
        private Button _toggleHealthCheck;
        private Button _openWebUI;

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
                    // Text inputs
                    Root.FindChild("ScanSpacingValue", out _scanSpacing);
                    Root.FindChild("DetectionRangeValue", out _detectionRange);
                    Root.FindChild("AFKCheckTimeValue", out _afkCheckTime);

                    // All buttons
                    Root.FindChild("ToggleOnlyAFK", out _toggleOnlyAFK);
                    Root.FindChild("ToggleContinuousScanning", out _toggleContinuousScanning);
                    Root.FindChild("ToggleSaveJson", out _toggleSaveJson);
                    Root.FindChild("ToggleSaveCsv", out _toggleSaveCsv);
                    Root.FindChild("ToggleLogConsole", out _toggleLogConsole);
                    Root.FindChild("ToggleDetectionRadius", out _toggleDetectionRadius);
                    Root.FindChild("TogglePlayerMarkers", out _togglePlayerMarkers);
                    Root.FindChild("ToggleAFKPaths", out _toggleAFKPaths);
                    Root.FindChild("ToggleActivePlayerPaths", out _toggleActivePlayerPaths);
                    Root.FindChild("ToggleTagOnly", out _toggleTagOnly);
                    Root.FindChild("ToggleCombat", out _toggleCombat);
                    Root.FindChild("ToggleLooting", out _toggleLooting);
                    Root.FindChild("ToggleHealthCheck", out _toggleHealthCheck);
                    Root.FindChild("OpenWebUI", out _openWebUI);

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

                    // Scanner control buttons
                    if (_toggleOnlyAFK != null)
                    {
                        _toggleOnlyAFK.Clicked = (s, e) =>
                        {
                            ZeroIn.Config.OnlyAFK = !ZeroIn.Config.OnlyAFK;
                            Chat.WriteLine($"[ZeroIn] Only AFK Players: {(ZeroIn.Config.OnlyAFK ? "ON" : "OFF")}",
                                ZeroIn.Config.OnlyAFK ? ChatColor.Green : ChatColor.Red);
                        };
                    }

                    if (_toggleContinuousScanning != null)
                    {
                        _toggleContinuousScanning.Clicked = (s, e) =>
                        {
                            ZeroIn.Config.ContinuousScanning = !ZeroIn.Config.ContinuousScanning;
                            Chat.WriteLine($"[ZeroIn] Continuous Scanning: {(ZeroIn.Config.ContinuousScanning ? "ON" : "OFF")}",
                                ZeroIn.Config.ContinuousScanning ? ChatColor.Green : ChatColor.Red);
                        };
                    }

                    // Output format buttons
                    if (_toggleSaveJson != null)
                    {
                        _toggleSaveJson.Clicked = (s, e) =>
                        {
                            ZeroIn.Config.SaveToJson = !ZeroIn.Config.SaveToJson;
                            Chat.WriteLine($"[ZeroIn] Save to JSON: {(ZeroIn.Config.SaveToJson ? "ON" : "OFF")}",
                                ZeroIn.Config.SaveToJson ? ChatColor.Green : ChatColor.Red);
                        };
                    }

                    if (_toggleSaveCsv != null)
                    {
                        _toggleSaveCsv.Clicked = (s, e) =>
                        {
                            ZeroIn.Config.SaveToCsv = !ZeroIn.Config.SaveToCsv;
                            Chat.WriteLine($"[ZeroIn] Save to CSV: {(ZeroIn.Config.SaveToCsv ? "ON" : "OFF")}",
                                ZeroIn.Config.SaveToCsv ? ChatColor.Green : ChatColor.Red);
                        };
                    }

                    if (_toggleLogConsole != null)
                    {
                        _toggleLogConsole.Clicked = (s, e) =>
                        {
                            ZeroIn.Config.LogToConsole = !ZeroIn.Config.LogToConsole;
                            Chat.WriteLine($"[ZeroIn] Log to Console: {(ZeroIn.Config.LogToConsole ? "ON" : "OFF")}",
                                ZeroIn.Config.LogToConsole ? ChatColor.Green : ChatColor.Red);
                        };
                    }

                    // Combat buttons
                    if (_toggleCombat != null)
                    {
                        _toggleCombat.Clicked = (s, e) =>
                        {
                            ZeroIn.Config.EnableCombat = !ZeroIn.Config.EnableCombat;
                            Chat.WriteLine($"[ZeroIn] Combat (Mob Targeting): {(ZeroIn.Config.EnableCombat ? "ON" : "OFF")}",
                                ZeroIn.Config.EnableCombat ? ChatColor.Green : ChatColor.Red);
                        };
                    }

                    if (_toggleLooting != null)
                    {
                        _toggleLooting.Clicked = (s, e) =>
                        {
                            ZeroIn.Config.EnableLooting = !ZeroIn.Config.EnableLooting;
                            Chat.WriteLine($"[ZeroIn] Looting: {(ZeroIn.Config.EnableLooting ? "ON" : "OFF")}",
                                ZeroIn.Config.EnableLooting ? ChatColor.Green : ChatColor.Red);
                        };
                    }

                    if (_toggleHealthCheck != null)
                    {
                        _toggleHealthCheck.Clicked = (s, e) =>
                        {
                            ZeroIn.Config.EnableHealthCheck = !ZeroIn.Config.EnableHealthCheck;
                            Chat.WriteLine($"[ZeroIn] Auto-Retreat: {(ZeroIn.Config.EnableHealthCheck ? "ON" : "OFF")}",
                                ZeroIn.Config.EnableHealthCheck ? ChatColor.Green : ChatColor.Red);
                        };
                    }

                    // Web UI button
                    if (_openWebUI != null)
                    {
                        _openWebUI.Clicked = (s, e) =>
                        {
                            Chat.WriteLine("[ZeroIn] Opening live map in browser...", ChatColor.Yellow);
                            try
                            {
                                System.Diagnostics.Process.Start("http://localhost:8080");
                                Chat.WriteLine("[ZeroIn] Live map opened at: http://localhost:8080", ChatColor.Green);
                            }
                            catch (System.Exception ex)
                            {
                                Chat.WriteLine($"[ZeroIn] Could not auto-open browser: {ex.Message}", ChatColor.Red);
                                Chat.WriteLine("[ZeroIn] Please open manually: http://localhost:8080", ChatColor.Yellow);
                            }
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

                if (_afkCheckTime != null)
                    _afkCheckTime.Text = config.AFKCheckTimeSeconds.ToString();

                // All settings are now buttons that toggle directly - no need to set initial state
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

                if (_afkCheckTime != null && int.TryParse(_afkCheckTime.Text, out int afkTime))
                    config.AFKCheckTimeSeconds = afkTime;

                // All other settings are buttons that update config directly when clicked
            }
            catch (System.Exception ex)
            {
                Chat.WriteLine($"[ScanSettingsView] Error updating config: {ex.Message}", ChatColor.Red);
            }
        }
    }
}
