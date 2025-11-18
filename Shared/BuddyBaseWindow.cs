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
            catch (Exception ex)
            {
                Chat.WriteLine($"[CustomView] Failed to create view from {xmlPath}", ChatColor.Red);
                Chat.WriteLine($"[CustomView] Error: {ex.Message}", ChatColor.Red);
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
                Chat.WriteLine($"[BuddyBaseWindow] OnWindowCreating starting...", ChatColor.White);
                Chat.WriteLine($"[BuddyBaseWindow] Looking for view: {CoreViewRootName}", ChatColor.White);

                // Initialize core view
                if (Window.FindView(CoreViewRootName, out View coreViewRoot))
                {
                    Chat.WriteLine($"[BuddyBaseWindow] Found CoreViewRoot, creating BuddyCoreView from {_coreViewPath}", ChatColor.White);
                    CoreSettingsView = new BuddyCoreView(_coreViewPath);

                    if (CoreSettingsView.Root != null)
                    {
                        Chat.WriteLine($"[BuddyBaseWindow] Adding CoreSettingsView as child", ChatColor.White);
                        coreViewRoot.AddChild(CoreSettingsView.Root, true);
                        Chat.WriteLine($"[BuddyBaseWindow] CoreSettingsView added successfully", ChatColor.Green);
                    }
                    else
                    {
                        Chat.WriteLine($"[BuddyBaseWindow] ERROR: CoreSettingsView.Root is null!", ChatColor.Red);
                    }
                }
                else
                {
                    Chat.WriteLine($"[BuddyBaseWindow] ERROR: Could not find view '{CoreViewRootName}'", ChatColor.Red);
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"[BuddyBaseWindow] EXCEPTION in OnWindowCreating!", ChatColor.Red);
                Chat.WriteLine($"[BuddyBaseWindow] Error: {ex.Message}", ChatColor.Red);
                Chat.WriteLine($"[BuddyBaseWindow] Stack trace: {ex.StackTrace}", ChatColor.Red);
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
        private TextInputView _channelId;
        private Checkbox _onInjectEnable;

        public BuddyCoreView(string xmlPath) : base(xmlPath)
        {
            try
            {
                Chat.WriteLine($"[BuddyCoreView] Constructor called, Root is {(Root == null ? "null" : "not null")}", ChatColor.White);

                if (Root != null)
                {
                    Chat.WriteLine($"[BuddyCoreView] Finding child controls...", ChatColor.White);

                    if (Root.FindChild("EnabledButton", out Button enabledButton))
                    {
                        EnabledButton = enabledButton;
                        Chat.WriteLine($"[BuddyCoreView] Found EnabledButton", ChatColor.White);
                    }
                    else
                    {
                        Chat.WriteLine($"[BuddyCoreView] ERROR: EnabledButton not found!", ChatColor.Red);
                    }

                    if (Root.FindChild("ChannelIdValue", out TextInputView channelId))
                    {
                        _channelId = channelId;
                        Chat.WriteLine($"[BuddyCoreView] Found ChannelIdValue", ChatColor.White);
                    }
                    else
                    {
                        Chat.WriteLine($"[BuddyCoreView] ERROR: ChannelIdValue not found!", ChatColor.Red);
                    }

                    if (Root.FindChild("OnInjectEnable", out Checkbox onInjectEnable))
                    {
                        _onInjectEnable = onInjectEnable;
                        Chat.WriteLine($"[BuddyCoreView] Found OnInjectEnable", ChatColor.White);
                    }
                    else
                    {
                        Chat.WriteLine($"[BuddyCoreView] ERROR: OnInjectEnable not found!", ChatColor.Red);
                    }
                }
                else
                {
                    Chat.WriteLine($"[BuddyCoreView] ERROR: Root is null after base constructor!", ChatColor.Red);
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"[BuddyCoreView] EXCEPTION in constructor!", ChatColor.Red);
                Chat.WriteLine($"[BuddyCoreView] Error: {ex.Message}", ChatColor.Red);
            }
        }

        public void SetButtonState(bool state)
        {
            IsButtonEnabled = state;
            // Button label is set in XML, we just track the state here
        }

        public void SetData(BuddyCoreConfig config)
        {
            if (config == null)
            {
                Chat.WriteLine($"[BuddyCoreView.SetData] ERROR: config is null!", ChatColor.Red);
                return;
            }

            try
            {
                Chat.WriteLine($"[BuddyCoreView.SetData] Setting channel ID to {config.ChannelId}", ChatColor.White);
                if (_channelId != null)
                    _channelId.Text = config.ChannelId.ToString();
                else
                    Chat.WriteLine($"[BuddyCoreView.SetData] WARNING: _channelId is null", ChatColor.Yellow);

                Chat.WriteLine($"[BuddyCoreView.SetData] Setting OnInjectEnable to {config.OnInjectEnable}", ChatColor.White);
                if (_onInjectEnable != null)
                {
                    Chat.WriteLine($"[BuddyCoreView.SetData] Calling SetValue({config.OnInjectEnable})", ChatColor.White);
                    _onInjectEnable.SetValue(config.OnInjectEnable);
                    Chat.WriteLine($"[BuddyCoreView.SetData] SetValue completed", ChatColor.White);
                }
                else
                    Chat.WriteLine($"[BuddyCoreView.SetData] WARNING: _onInjectEnable is null", ChatColor.Yellow);
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"[BuddyCoreView.SetData] EXCEPTION!", ChatColor.Red);
                Chat.WriteLine($"[BuddyCoreView.SetData] Error: {ex.Message}", ChatColor.Red);
                Chat.WriteLine($"[BuddyCoreView.SetData] Stack: {ex.StackTrace}", ChatColor.Red);
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
                    config.OnInjectEnable = _onInjectEnable.IsChecked;
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
