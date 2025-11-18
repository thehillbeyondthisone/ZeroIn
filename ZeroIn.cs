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
        public static VisualRadar Radar;

        public override void Run()
        {
            try
            {
                Chat.WriteLine("[ZeroIn] Plugin starting...", ChatColor.Green);
                Logger.Information("Loaded!");

                Chat.WriteLine("[ZeroIn] Initializing movement controller...", ChatColor.White);
                SMovementController.Set();

                Log = Logger;

                Chat.WriteLine("[ZeroIn] Initializing paths...", ChatColor.White);
                CommonParameters.Init("ZeroIn");   // Init plugin name to create all relative directories

                string folderPath  = $"{CommonParameters.PluginDataPath}\\RoamPath";
                Chat.WriteLine($"[ZeroIn] Data path: {folderPath}", ChatColor.White);

                Directory.CreateDirectory(folderPath);
                XmlPath.Set(PluginDirectory);   // Set our xml paths for windows / views

                Chat.WriteLine("[ZeroIn] Loading configuration...", ChatColor.White);
                Config = ZeroInConfig.LoadConfig($"{CommonParameters.PlayerSettingsPath}"); // Per character config containing all UI related settings
                Config.RoamPathFolder = folderPath;

                Chat.WriteLine("[ZeroIn] Loading roam path...", ChatColor.White);
                RoamPath = RoamPath.Load(Config.RoamPath);  // RoamPath config which contains SPath and Targeting Rules

                // Initialize Scanner for player detection
                Chat.WriteLine("[ZeroIn] Initializing player scanner...", ChatColor.White);
                Scanner = new CharacterScanner(Config);
                Map = new ScanMap(CommonParameters.PluginDataPath);
                Chat.WriteLine("[ZeroIn] Scanner initialized successfully", ChatColor.Green);

                // Initialize Visual Radar
                Chat.WriteLine("[ZeroIn] Initializing visual radar...", ChatColor.White);
                Radar = new VisualRadar(Scanner, Config);
                Game.OnUpdate += OnUpdate;
                Chat.WriteLine("[ZeroIn] Visual radar initialized successfully", ChatColor.Green);

                Chat.WriteLine("[ZeroIn] Opening main window on startup...", ChatColor.White);
                OpenMainWindow();

                // Register commands (case-insensitive variants)
                Chat.WriteLine("[ZeroIn] Registering commands...", ChatColor.White);
                Chat.RegisterCommand("zeroin", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    Chat.WriteLine("[ZeroIn] Command received: /zeroin", ChatColor.Green);
                    OpenMainWindow();
                });

                Chat.RegisterCommand("ZeroIn", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    Chat.WriteLine("[ZeroIn] Command received: /ZeroIn", ChatColor.Green);
                    OpenMainWindow();
                });

                // Add command to show detected players
                Chat.RegisterCommand("scan", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    Chat.WriteLine("[ZeroIn] Command received: /scan", ChatColor.Green);
                    var detected = Scanner.GetDetectedCharacters();
                    Chat.WriteLine($"=== ZeroIn Player Scanner ===", ChatColor.Yellow);
                    Chat.WriteLine($"Detected {detected.Count} players:", ChatColor.LightBlue);
                    foreach (var player in detected.OrderByDescending(p => p.LastSeen).Take(20))
                    {
                        var age = (DateTime.UtcNow - player.LastSeen).TotalSeconds;
                        Chat.WriteLine($"  {player.Name} - {player.Distance:F1}m away, seen {age:F0}s ago (spotted {player.TimesSpotted}x)", ChatColor.White);
                    }
                });

                // Add command to toggle visual radar
                Chat.RegisterCommand("radar", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    Chat.WriteLine("[ZeroIn] Command received: /radar", ChatColor.Green);
                    Radar.Toggle();
                });

                Chat.WriteLine("[ZeroIn] Commands registered: /zeroin, /ZeroIn, /scan, /radar", ChatColor.Green);

                Chat.WriteLine("[ZeroIn] Initializing state machine...", ChatColor.White);
                StateMachine = new RoamStateMachine(new MobTargeting(Config), Scanner, Map, Config.CoreConfig.OnInjectEnable);

                Chat.WriteLine("[ZeroIn] Initializing IPC...", ChatColor.White);
                Ipc = new IPC((byte)Config.CoreConfig.ChannelId);

                Chat.WriteLine("[ZeroIn] *** PLUGIN LOADED SUCCESSFULLY ***", ChatColor.Green);
                Chat.WriteLine("[ZeroIn] Type /zeroin to open the UI", ChatColor.Yellow);
            }
            catch (Exception e)
            {
                Chat.WriteLine($"[ZeroIn] FATAL ERROR during plugin load!", ChatColor.Red);
                Chat.WriteLine($"[ZeroIn] Error: {e.Message}", ChatColor.Red);
                Chat.WriteLine($"[ZeroIn] Type: {e.GetType().Name}", ChatColor.Red);
                Chat.WriteLine($"[ZeroIn] Stack trace:", ChatColor.Red);
                Chat.WriteLine(e.StackTrace, ChatColor.Red);
                Logger.Warning(e.ToString());
            }
        }

        private void OpenMainWindow()
        {
            try
            {
                Chat.WriteLine($"=== ZeroIn Window Creation Debug ===", ChatColor.Yellow);
                Chat.WriteLine($"Plugin directory: {PluginDirectory}", ChatColor.White);
                Chat.WriteLine($"Windows root: {XmlPath.WindowsRootDir}", ChatColor.White);
                Chat.WriteLine($"Views root: {XmlPath.ViewsRootDir}", ChatColor.White);

                string mainWindowPath = $"{XmlPath.WindowsRootDir}\\MainWindow.xml";
                string infoWindowPath = $"{XmlPath.WindowsRootDir}\\InfoWindow.xml";
                string roamWindowPath = $"{XmlPath.WindowsRootDir}\\RoamPathWindow.xml";
                string coreViewPath = $"{XmlPath.ViewsRootDir}\\BuddyCoreView.xml";
                string scanSettingsPath = $"{XmlPath.ViewsRootDir}\\ScanSettingsView.xml";
                string initViewPath = $"{XmlPath.ViewsRootDir}\\RoamPathInitView.xml";
                string mainViewPath = $"{XmlPath.ViewsRootDir}\\RoamPathMainView.xml";

                Chat.WriteLine($"Checking XML files...", ChatColor.White);
                Chat.WriteLine($"  MainWindow: {(File.Exists(mainWindowPath) ? "FOUND" : "MISSING")} - {mainWindowPath}", ChatColor.White);
                Chat.WriteLine($"  RoamPathWindow: {(File.Exists(roamWindowPath) ? "FOUND" : "MISSING")} - {roamWindowPath}", ChatColor.White);
                Chat.WriteLine($"  BuddyCoreView: {(File.Exists(coreViewPath) ? "FOUND" : "MISSING")} - {coreViewPath}", ChatColor.White);
                Chat.WriteLine($"  ScanSettings: {(File.Exists(scanSettingsPath) ? "FOUND" : "MISSING")} - {scanSettingsPath}", ChatColor.White);
                Chat.WriteLine($"  InitView: {(File.Exists(initViewPath) ? "FOUND" : "MISSING")} - {initViewPath}", ChatColor.White);
                Chat.WriteLine($"  MainView: {(File.Exists(mainViewPath) ? "FOUND" : "MISSING")} - {mainViewPath}", ChatColor.White);

                Chat.WriteLine($"Creating MainWindow object...", ChatColor.White);
                MainWindow = new MainWindow("ZeroIn",
                    mainWindowPath,
                    infoWindowPath,
                    roamWindowPath,
                    coreViewPath,
                    scanSettingsPath,
                    initViewPath,
                    mainViewPath);

                Chat.WriteLine($"Calling MainWindow.Show()...", ChatColor.White);
                MainWindow.Show();
                Chat.WriteLine($"Window shown successfully!", ChatColor.Green);
            }
            catch (FileNotFoundException fnfEx)
            {
                Chat.WriteLine($"[ZeroIn] FILE NOT FOUND ERROR:", ChatColor.Red);
                Chat.WriteLine($"  Missing file: {fnfEx.FileName}", ChatColor.Red);
                Chat.WriteLine($"  Message: {fnfEx.Message}", ChatColor.Red);
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"[ZeroIn] ERROR opening window:", ChatColor.Red);
                Chat.WriteLine($"  Type: {ex.GetType().Name}", ChatColor.Red);
                Chat.WriteLine($"  Message: {ex.Message}", ChatColor.Red);
                Chat.WriteLine($"  Stack trace:", ChatColor.Red);

                string[] lines = ex.StackTrace?.Split('\n') ?? new string[0];
                foreach (var line in lines.Take(5))
                {
                    Chat.WriteLine($"    {line.Trim()}", ChatColor.Red);
                }

                if (ex.InnerException != null)
                {
                    Chat.WriteLine($"  Inner exception: {ex.InnerException.Message}", ChatColor.Red);
                }
            }
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

        private static void OnUpdate(object sender, float deltaTime)
        {
            try
            {
                // Draw visual radar overlay
                Radar?.Draw();
            }
            catch (Exception ex)
            {
                // Silently catch to avoid spam - radar is non-critical
                Log?.Warning($"OnUpdate error: {ex.Message}");
            }
        }
    }
}
