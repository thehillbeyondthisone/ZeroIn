using AOSharp.Core;
using System;
using AOSharp.Pathfinding;
using Serilog.Core;
using AOSharp.Core.UI;
using System.IO;
using System.Linq;
using AOSharp.Common.GameData;
using ZeroIn.Scanner;

namespace ZeroIn
{
    public class ZeroIn : AOPluginEntry
    {
        public static ZeroInConfig Config;
        public static RoamPath RoamPath;
        public static Logger Log;
        public static RoamStateMachine StateMachine;
        public static IPC Ipc;
        public static MainWindow MainWindow;
        public static CharacterScanner Scanner;
        public static ScanMap Map;

        public override void Run()
        {
            try
            {
                Logger.Information("Loaded!");

                SMovementController.Set();

                Log = Logger;

                CommonParameters.Init("ZeroIn");   // Init plugin name to create all relative directories

                string folderPath  = $"{CommonParameters.PluginDataPath}\\RoamPath";

                Directory.CreateDirectory(folderPath);
                XmlPath.Set(PluginDirectory);   // Set our xml paths for windows / views

                Config = ZeroInConfig.LoadConfig($"{CommonParameters.PlayerSettingsPath}"); // Per character config containing all UI related settings
                Config.RoamPathFolder = folderPath;
                RoamPath = RoamPath.Load(Config.RoamPath);  // RoamPath config which contains SPath and Targeting Rules

                // Initialize Scanner for player detection
                Scanner = new CharacterScanner(Config);
                Map = new ScanMap(CommonParameters.PluginDataPath);

                OpenMainWindow();

                Chat.RegisterCommand("ZeroIn", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    OpenMainWindow();
                });

                // Add command to show detected players
                Chat.RegisterCommand("scan", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    var detected = Scanner.GetDetectedCharacters();
                    Chat.WriteLine($"=== ZeroIn Player Scanner ===", ChatColor.Yellow);
                    Chat.WriteLine($"Detected {detected.Count} players:", ChatColor.LightBlue);
                    foreach (var player in detected.OrderByDescending(p => p.LastSeen).Take(20))
                    {
                        var age = (DateTime.UtcNow - player.LastSeen).TotalSeconds;
                        Chat.WriteLine($"  {player.Name} - {player.Distance:F1}m away, seen {age:F0}s ago (spotted {player.TimesSpotted}x)", ChatColor.White);
                    }
                });

                StateMachine = new RoamStateMachine(new MobTargeting(Config), Scanner, Map, Config.CoreConfig.OnInjectEnable);
                Ipc = new IPC((byte)Config.CoreConfig.ChannelId);
            }
            catch (Exception e)
            {
                Logger.Warning(e.ToString());
            }
        }

        private void OpenMainWindow()
        {
            MainWindow = new MainWindow("ZeroIn",
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
