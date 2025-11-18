using AOSharp.Core;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using AutomatonRoamba.IPCMessages;

namespace AutomatonRoamba
{
    public class IPC : IPCChannelBase
    {
        protected override int _localDynelId => DynelManager.LocalPlayer.Identity.Instance;
        internal byte ChannelId;

        public IPC(byte channelId) : base(channelId)
        {
            ChannelId = channelId;
            RegisterCallback((int)IPCOpcode.Enabled, OnEnabledMessage);
        }

        public new void SetChannelId(byte channelId)
        {
            ChannelId = channelId;
            base.SetChannelId(channelId);
        }

        private void OnEnabledMessage(int arg1, IPCMessage message)
        {
            EnabledIPCMessage enabledIpc = (EnabledIPCMessage)message;

            AutomatonRoamba.MainWindow.Show();

            if (enabledIpc.PathConfig.SyncSettings)
            {
                AutomatonRoamba.Config.PathingConfig = enabledIpc.PathConfig;
                AutomatonRoamba.Config.Save();
                AutomatonRoamba.MainWindow.PathSettingsView.SetData(enabledIpc.PathConfig);
            }

            if (!string.IsNullOrEmpty(enabledIpc.RoamPath) && AutomatonRoamba.RoamPath.SPath != null)
            {
                AutomatonRoamba.RoamPath.SPath.Delete();
                AutomatonRoamba.RoamPath = RoamPath.Load(enabledIpc.RoamPath);
                AutomatonRoamba.Config.RoamPath = enabledIpc.RoamPath;
                AutomatonRoamba.Config.Save();
            }

            if (AutomatonRoamba.RoamPath.SPath.PlayfieldId != Playfield.ModelIdentity.Instance)
                return;

            AutomatonRoamba.MainWindow.OnEnabledPress(enabledIpc.SetEnabled);
        }
    }
}
