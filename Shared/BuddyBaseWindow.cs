using AOSharp.Core.UI;
using AOSharp.Common.GameData.UI;
using AOSharp.Core;
using System;

namespace Buddy.Shared.UI
{
    /// <summary>
    /// Base custom view class
    /// </summary>
    public class CustomView
    {
        public View Root { get; protected set; }

        public CustomView(string xmlPath)
        {
            try
            {
                Root = View.CreateFromXml(xmlPath);
            }
            catch (Exception)
            {
                // Failed to create view
            }
        }

        protected CustomView()
        {
        }
    }

    /// <summary>
    /// Base window class for ZeroIn UI
    /// Simplified version without the full Buddy framework
    /// </summary>
    public abstract class BuddyBaseWindow : AOSharpWindow
    {
        public abstract string CoreViewRootName { get; }
        public abstract string InfoText { get; }

        public BuddyCoreView CoreSettingsView { get; protected set; }

        private string _coreViewPath;

        protected BuddyBaseWindow(string windowName, string baseWindowPath, string infoWindowPath, string coreViewPath, WindowStyle windowStyle = WindowStyle.Default, WindowFlags flags = WindowFlags.AutoScale | WindowFlags.NoFade)
            : base(windowName, baseWindowPath, windowStyle, flags)
        {
            _coreViewPath = coreViewPath;
        }

        protected override void OnWindowCreating()
        {
            try
            {
                // Initialize core view
                if (Window.FindView(CoreViewRootName, out View coreViewRoot))
                {
                    CoreSettingsView = new BuddyCoreView(_coreViewPath);
                    if (CoreSettingsView.Root != null)
                    {
                        coreViewRoot.AddChild(CoreSettingsView.Root, true);
                    }
                }
            }
            catch (Exception)
            {
                // Failed to create window
            }
        }
    }

    /// <summary>
    /// Simplified BuddyCoreView
    /// </summary>
    public class BuddyCoreView : CustomView
    {
        public bool IsButtonEnabled { get; set; }
        public Button EnabledButton { get; set; }
        private TextField _channelId;
        private Checkbox _onInjectEnable;

        public BuddyCoreView(string xmlPath) : base(xmlPath)
        {
            try
            {
                if (Root != null)
                {
                    if (Root.FindChild("EnabledButton", out Button enabledButton))
                        EnabledButton = enabledButton;

                    if (Root.FindChild("ChannelIdValue", out TextField channelId))
                        _channelId = channelId;

                    if (Root.FindChild("OnInjectEnable", out Checkbox onInjectEnable))
                        _onInjectEnable = onInjectEnable;
                }
            }
            catch (Exception)
            {
                // Failed to initialize view elements
            }
        }

        public void SetButtonState(bool state)
        {
            IsButtonEnabled = state;
            if (EnabledButton != null)
                EnabledButton.Text = state ? "Stop" : "Start";
        }

        public void SetData(BuddyCoreConfig config)
        {
            if (config == null) return;

            try
            {
                if (_channelId != null)
                    _channelId.Text = config.ChannelId.ToString();

                if (_onInjectEnable != null)
                    _onInjectEnable.Checked = config.OnInjectEnable;
            }
            catch (Exception)
            {
                // Failed to set data
            }
        }

        public BuddyCoreConfig GetData()
        {
            var config = new BuddyCoreConfig();

            try
            {
                if (_channelId != null && int.TryParse(_channelId.Text, out int channelId))
                    config.ChannelId = channelId;

                if (_onInjectEnable != null)
                    config.OnInjectEnable = _onInjectEnable.Checked;
            }
            catch (Exception)
            {
                // Failed to get data, return defaults
            }

            return config;
        }
    }

    /// <summary>
    /// Simplified BuddyCoreConfig
    /// </summary>
    public class BuddyCoreConfig
    {
        public int ChannelId { get; set; }
        public bool OnInjectEnable { get; set; }

        public BuddyCoreConfig()
        {
            ChannelId = 1;
            OnInjectEnable = false;
        }
    }
}
