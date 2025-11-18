using AOSharp.Core;
using System;
using AOSharp.Pathfinding;
using Serilog.Core;
using AOSharp.Core.UI;
using System.IO;
using System.Linq;
using AOSharp.Common.GameData;

namespace AutomatonRoamba
{
    public class AutomatonRoamba : AOPluginEntry
    {
        public static AutomatonRoambaConfig Config;
        public static RoamPath RoamPath;
        public static Logger Log;
        public static RoamStateMachine StateMachine;
        public static IPC Ipc;
        public static MainWindow MainWindow;

        public override void Run()
        {
            try
            {
                Logger.Information("Loaded!");

                SMovementController.Set();

                Log = Logger;

                CommonParameters.Init("AutomatonRoamba");   // Init plugin name to create all relative directories

                string folderPath  = $"{CommonParameters.PluginDataPath}\\RoamPath";

                Directory.CreateDirectory(folderPath);
                XmlPath.Set(PluginDirectory);   // Set our xml paths for windows / views

                Config = AutomatonRoambaConfig.LoadConfig($"{CommonParameters.PlayerSettingsPath}"); // Per character config containing all UI related settings
                Config.RoamPathFolder = folderPath;
                RoamPath = RoamPath.Load(Config.RoamPath);  // RoamPath config which contains SPath and Targeting Rules

                OpenMainWindow();

                Chat.RegisterCommand("AutomatonRoamba", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    OpenMainWindow();
                });

                StateMachine = new RoamStateMachine(new MobTargeting(Config), Config.CoreConfig.OnInjectEnable);
                Ipc = new IPC(Config.CoreConfig.ChannelId);
            }
            catch (Exception e)
            {
                Logger.Warning(e.ToString());
            }
        }

        private void OpenMainWindow()
        {
            MainWindow = new MainWindow("AutomatonRoamba",
                $"{XmlPath.WindowsRootDir}\\MainWindow.xml",
                $"{XmlPath.WindowsRootDir}\\InfoWindow.xml",
                $"{XmlPath.WindowsRootDir}\\RoamPathWindow.xml",
                $"{XmlPath.ViewsRootDir}\\BuddyCoreView.xml",
                $"{XmlPath.ViewsRootDir}\\PathSettingsView.xml",
                $"{XmlPath.ViewsRootDir}\\RoamPathInitView.xml",
                $"{XmlPath.ViewsRootDir}\\RoamPathMainView.xml");

            MainWindow.Show();
        }

        public static void SetPath(SPath path)
        {
            var charNames = Config.PathingConfig.FollowTargetName.Split('\n').Select(x => x.Trim());
            var followTarget = DynelManager.Players.FirstOrDefault(x => charNames.Any(y => y.Equals(x.Name, StringComparison.OrdinalIgnoreCase)));

            if (followTarget != null)
            {
                SMovementController.SetPath(path, followTarget.Position, false);
            }
            else
            {
                if (path.Waypoints.Count == 1 && Vector3.Distance(DynelManager.LocalPlayer.Position, path.Waypoints[0]) > 1)
                    SMovementController.SetPath(path, true);
                else if (path.Waypoints.Count > 1)
                    SMovementController.SetPath(path, true);
            }
        }
    }
}
