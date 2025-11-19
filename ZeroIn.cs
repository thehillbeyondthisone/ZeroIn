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
                    if (param.Length > 0 && param[0].ToLower() == "help")
                    {
                        Chat.WriteLine("=== ZeroIn Commands ===", ChatColor.Yellow);
                        Chat.WriteLine("/zeroin - Open ZeroIn UI window", ChatColor.White);
                        Chat.WriteLine("/zeroin help - Show this help menu", ChatColor.White);
                        Chat.WriteLine("/status - Show current ZeroIn status and settings", ChatColor.White);
                        Chat.WriteLine("/scan - List all detected players", ChatColor.White);
                        Chat.WriteLine("/radar - Toggle all radar visuals on/off", ChatColor.White);
                        Chat.WriteLine("/radar radius - Toggle detection radius circle", ChatColor.White);
                        Chat.WriteLine("/radar players - Toggle player markers", ChatColor.White);
                        Chat.WriteLine("/radar afk - Toggle AFK player paths", ChatColor.White);
                        Chat.WriteLine("/radar active - Toggle active player paths", ChatColor.White);
                        Chat.WriteLine("/radar tags - Toggle tag-only mode", ChatColor.White);
                        Chat.WriteLine("/radar debug - Toggle radar debug mode", ChatColor.White);
                        Chat.WriteLine("/radar stats - Show radar statistics", ChatColor.White);
                        Chat.WriteLine("/radar help - Show radar command help", ChatColor.White);
                        Chat.WriteLine("/debug - Toggle verbose debug logging", ChatColor.White);
                        return;
                    }
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
                    if (param.Length == 0)
                    {
                        // No parameters - toggle entire radar
                        Radar.Toggle();
                        return;
                    }

                    string subCommand = param[0].ToLower();
                    switch (subCommand)
                    {
                        case "radius":
                            Config.ShowDetectionRadius = !Config.ShowDetectionRadius;
                            Chat.WriteLine($"[ZeroIn] Detection radius: {(Config.ShowDetectionRadius ? "ON" : "OFF")}",
                                Config.ShowDetectionRadius ? ChatColor.Green : ChatColor.Red);
                            break;

                        case "players":
                        case "markers":
                            Config.ShowPlayerMarkers = !Config.ShowPlayerMarkers;
                            Chat.WriteLine($"[ZeroIn] Player markers: {(Config.ShowPlayerMarkers ? "ON" : "OFF")}",
                                Config.ShowPlayerMarkers ? ChatColor.Green : ChatColor.Red);
                            break;

                        case "tags":
                        case "tagonly":
                            Config.TagOnlyMode = !Config.TagOnlyMode;
                            Chat.WriteLine($"[ZeroIn] Tag-only mode: {(Config.TagOnlyMode ? "ON (names only)" : "OFF (shapes visible)")}",
                                Config.TagOnlyMode ? ChatColor.Green : ChatColor.Red);
                            break;

                        case "afk":
                            Config.ShowAFKPaths = !Config.ShowAFKPaths;
                            Chat.WriteLine($"[ZeroIn] AFK paths: {(Config.ShowAFKPaths ? "ON" : "OFF")}",
                                Config.ShowAFKPaths ? ChatColor.Green : ChatColor.Red);
                            break;

                        case "active":
                            Config.ShowActivePlayerPaths = !Config.ShowActivePlayerPaths;
                            Chat.WriteLine($"[ZeroIn] Active player paths: {(Config.ShowActivePlayerPaths ? "ON" : "OFF")}",
                                Config.ShowActivePlayerPaths ? ChatColor.Green : ChatColor.Red);
                            break;

                        case "debug":
                            Radar.DebugMode = !Radar.DebugMode;
                            break;

                        case "stats":
                            Radar.PrintStats();
                            break;

                        case "help":
                            Chat.WriteLine("=== ZeroIn Radar Commands ===", ChatColor.Yellow);
                            Chat.WriteLine("/radar - Toggle all radar visuals", ChatColor.White);
                            Chat.WriteLine("/radar radius - Toggle detection radius circle", ChatColor.White);
                            Chat.WriteLine("/radar players - Toggle player markers", ChatColor.White);
                            Chat.WriteLine("/radar afk - Toggle AFK player paths", ChatColor.White);
                            Chat.WriteLine("/radar active - Toggle active (non-AFK) player paths", ChatColor.White);
                            Chat.WriteLine("/radar tags - Toggle tag-only mode (names only, no shapes)", ChatColor.White);
                            Chat.WriteLine("/radar debug - Toggle detailed debug logging", ChatColor.White);
                            Chat.WriteLine("/radar stats - Show radar statistics and error counts", ChatColor.White);
                            break;

                        default:
                            Chat.WriteLine($"[ZeroIn] Unknown radar option: {subCommand}", ChatColor.Red);
                            Chat.WriteLine("[ZeroIn] Use /radar help for available options", ChatColor.Yellow);
                            break;
                    }
                });

                // Add diagnostic command to debug XML file loading
                Chat.RegisterCommand("debugxml", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    Chat.WriteLine("=== ZeroIn XML Debug ===", ChatColor.Yellow);
                    Chat.WriteLine($"Plugin Directory: {PluginDirectory}", ChatColor.White);
                    Chat.WriteLine($"Windows Root: {XmlPath.WindowsRootDir}", ChatColor.White);
                    Chat.WriteLine($"Views Root: {XmlPath.ViewsRootDir}", ChatColor.White);

                    string coreViewPath = $"{XmlPath.ViewsRootDir}\\BuddyCoreView.xml";
                    Chat.WriteLine($"\nBuddyCoreView.xml path: {coreViewPath}", ChatColor.LightBlue);
                    Chat.WriteLine($"File exists: {File.Exists(coreViewPath)}", ChatColor.White);

                    if (File.Exists(coreViewPath))
                    {
                        try
                        {
                            var lines = File.ReadAllLines(coreViewPath);
                            Chat.WriteLine($"Total lines: {lines.Length}", ChatColor.White);
                            Chat.WriteLine("\nSearching for HLayoutGroup...", ChatColor.Yellow);
                            for (int i = 0; i < lines.Length; i++)
                            {
                                if (lines[i].Contains("HLayoutGroup"))
                                {
                                    Chat.WriteLine($"Line {i + 1}: {lines[i].Trim()}", ChatColor.LightBlue);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Chat.WriteLine($"Error reading file: {ex.Message}", ChatColor.Red);
                        }
                    }

                    string scanSettingsPath = $"{XmlPath.ViewsRootDir}\\ScanSettingsView.xml";
                    Chat.WriteLine($"\nScanSettingsView.xml path: {scanSettingsPath}", ChatColor.LightBlue);
                    Chat.WriteLine($"File exists: {File.Exists(scanSettingsPath)}", ChatColor.White);

                    if (File.Exists(scanSettingsPath))
                    {
                        try
                        {
                            var lines = File.ReadAllLines(scanSettingsPath);
                            Chat.WriteLine($"Total lines: {lines.Length}", ChatColor.White);
                            Chat.WriteLine("\nSearching for HLayoutGroup...", ChatColor.Yellow);
                            for (int i = 0; i < lines.Length; i++)
                            {
                                if (lines[i].Contains("HLayoutGroup"))
                                {
                                    Chat.WriteLine($"Line {i + 1}: {lines[i].Trim()}", ChatColor.LightBlue);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Chat.WriteLine($"Error reading file: {ex.Message}", ChatColor.Red);
                        }
                    }
                });

                // Add command to toggle verbose debug
                Chat.RegisterCommand("debug", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    Config.VerboseDebug = !Config.VerboseDebug;
                    Chat.WriteLine($"[ZeroIn] Verbose debug: {(Config.VerboseDebug ? "ENABLED" : "DISABLED")}",
                        Config.VerboseDebug ? ChatColor.Green : ChatColor.Red);
                    Chat.WriteLine("[ZeroIn] This enables detailed logging for scanning, targeting, and grid generation.", ChatColor.White);
                });

                // Add status command to show current settings
                Chat.RegisterCommand("status", (string command, string[] param, ChatWindow chatWindow) =>
                {
                    Chat.WriteLine("=== ZeroIn Status ===", ChatColor.Yellow);
                    Chat.WriteLine($"Radar: {(Radar.Enabled ? "ON" : "OFF")}", Radar.Enabled ? ChatColor.Green : ChatColor.Red);
                    Chat.WriteLine($"  Detection radius: {(Config.ShowDetectionRadius ? "ON" : "OFF")}", Config.ShowDetectionRadius ? ChatColor.Green : ChatColor.Red);
                    Chat.WriteLine($"  Player markers: {(Config.ShowPlayerMarkers ? "ON" : "OFF")}", Config.ShowPlayerMarkers ? ChatColor.Green : ChatColor.Red);
                    Chat.WriteLine($"  AFK paths: {(Config.ShowAFKPaths ? "ON" : "OFF")}", Config.ShowAFKPaths ? ChatColor.Green : ChatColor.Red);
                    Chat.WriteLine($"  Active player paths: {(Config.ShowActivePlayerPaths ? "ON" : "OFF")}", Config.ShowActivePlayerPaths ? ChatColor.Green : ChatColor.Red);
                    Chat.WriteLine($"  Tag-only mode: {(Config.TagOnlyMode ? "ON" : "OFF")}", Config.TagOnlyMode ? ChatColor.Green : ChatColor.Red);
                    Chat.WriteLine($"Continuous scanning: {(Config.ContinuousScanning ? "ON" : "OFF")}", Config.ContinuousScanning ? ChatColor.Green : ChatColor.Red);

                    var detected = Scanner.GetDetectedCharacters();
                    int afkCount = detected.Count(p => p.IsLikelyAFK());
                    Chat.WriteLine($"Detected players: {detected.Count} ({afkCount} AFK)", ChatColor.LightBlue);

                    Chat.WriteLine($"Combat enabled: {(Config.EnableCombat ? "ON" : "OFF")}", Config.EnableCombat ? ChatColor.Green : ChatColor.Red);
                    Chat.WriteLine($"Verbose debug: {(Config.VerboseDebug ? "ON" : "OFF")}", Config.VerboseDebug ? ChatColor.Green : ChatColor.Red);
                });

                Chat.WriteLine("[ZeroIn] Commands registered: /zeroin, /ZeroIn, /scan, /radar, /status, /debug, /map, /debugxml", ChatColor.Green);

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
                // Check if window already exists and is valid
                if (MainWindow != null && MainWindow.Window != null && MainWindow.Window.IsValid)
                {
                    Chat.WriteLine("[ZeroIn] Window is already open!", ChatColor.Yellow);
                    return;
                }

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

                // Continuous scanning (ONLY when state machine is NOT running)
                // If state machine is running, RoamState.Tick() handles scanning
                if (Config.ContinuousScanning && Scanner != null && StateMachine != null && !StateMachine.IsEnabled)
                {
                    Scanner.Scan(); // Purge stale entries

                    var localPlayer = DynelManager.LocalPlayer;
                    if (localPlayer != null && localPlayer.IsValid)
                    {
                        var localPos = localPlayer.Position;

                        // Scan all nearby players within detection range
                        foreach (var player in DynelManager.Players)
                        {
                            if (player == null || !player.IsValid) continue;
                            if (player.Identity == localPlayer.Identity) continue; // Skip self

                            float distance = AOSharp.Common.GameData.Vector3.Distance(localPos, player.Position);

                            // Only scan players within detection range
                            if (distance <= Config.PlayerDetectionRange)
                            {
                                Scanner.OnCharacterSeen(
                                    (int)player.Identity.Instance,
                                    player.Name,
                                    player.Position.X,
                                    player.Position.Y,
                                    player.Position.Z,
                                    player.Health,
                                    distance,
                                    Playfield.ModelIdentity.Instance,
                                    Playfield.Name
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently catch to avoid spam
                Log?.Warning($"OnUpdate error: {ex.Message}");
            }
        }
    }
}
