using AOSharp.Core;
using AOSharp.Core.IPC;
using AOSharp.Core.UI;
using ZeroIn.IPCMessages;

namespace ZeroIn
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

            ZeroIn.MainWindow.Show();

            if (enabledIpc.PathConfig.SyncSettings)
            {
                ZeroIn.Config.PathingConfig = enabledIpc.PathConfig;
                ZeroIn.Config.Save();
                // Scanner settings are configured locally, not synced
            }

            if (!string.IsNullOrEmpty(enabledIpc.RoamPath) && ZeroIn.RoamPath.SPath != null)
            {
                ZeroIn.RoamPath.SPath.Delete();
                ZeroIn.RoamPath = RoamPath.Load(enabledIpc.RoamPath);
                ZeroIn.Config.RoamPath = enabledIpc.RoamPath;
                ZeroIn.Config.Save();
            }

            if (ZeroIn.RoamPath.SPath.PlayfieldId != Playfield.ModelIdentity.Instance)
                return;

            ZeroIn.MainWindow.OnEnabledPress(enabledIpc.SetEnabled);
        }
    }
}
