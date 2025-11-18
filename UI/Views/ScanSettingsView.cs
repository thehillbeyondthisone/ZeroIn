using AOSharp.Common.GameData.UI;
using AOSharp.Core;
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
            }
            catch (System.Exception ex)
            {
                Chat.WriteLine($"[ScanSettingsView] Error updating config: {ex.Message}", ChatColor.Red);
            }
        }
    }
}
