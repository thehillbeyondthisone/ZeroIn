using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Pathfinding;
using System;
using System.IO;
using ZeroIn.Config;
using ZeroIn.GridPattern;
using ZeroIn.StateMachine;

namespace ZeroIn
{
    /// <summary>
    /// ZeroIn - AFK Character Mapper for Anarchy Online
    /// Maps AFK characters across play areas using grid search patterns
    /// </summary>
    public class ZeroIn : AOPluginEntry
    {
        private static ZeroInConfig _config;
        private static ScanStateMachine _stateMachine;
        private static ScanContext _context;
        private static string _configPath;

        public override void Run()
        {
            try
            {
                Chat.WriteLine("ZeroIn loaded!", ChatColor.Green);

                // Initialize movement controller (required for pathfinding)
                SMovementController.Set();

                // Register chat commands FIRST - don't do anything complex yet
                Chat.RegisterCommand("zeroin", HandleCommand);
                Chat.RegisterCommand("zi", HandleCommand);

                // Defer initialization to first command or game update
                // This avoids accessing DynelManager.LocalPlayer too early
                Game.OnUpdate += OnUpdate;

                Chat.WriteLine("ZeroIn commands registered - Type /zeroin help", ChatColor.Green);
            }
            catch (Exception e)
            {
                Chat.WriteLine($"ZeroIn error: {e.Message}", ChatColor.Red);
            }
        }

        private static void EnsureInitialized()
        {
            if (_config != null)
                return; // Already initialized

            try
            {
                // Now it's safe to access DynelManager.LocalPlayer
                if (DynelManager.LocalPlayer == null)
                    return; // Not ready yet, will retry next frame

                // Set up config path
                _configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AOSharp",
                    "AOSP",
                    "ZeroIn",
                    $"{DynelManager.LocalPlayer.Name}_config.json"
                );

                // Ensure directory exists
                string configDir = Path.GetDirectoryName(_configPath);
                if (!Directory.Exists(configDir))
                    Directory.CreateDirectory(configDir);

                // Load config
                _config = ZeroInConfig.Load(_configPath);

                // Initialize state machine
                _context = new ScanContext(_config);
                _stateMachine = new ScanStateMachine(_context);

                Chat.WriteLine($"ZeroIn ready for {DynelManager.LocalPlayer.Name}!", ChatColor.Green);
                Chat.WriteLine("Type /zeroin help for commands", ChatColor.LightBlue);
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"ZeroIn init error: {ex.Message}", ChatColor.Red);
            }
        }

        private static void OnUpdate(object sender, float deltaTime)
        {
            try
            {
                EnsureInitialized();

                if (_stateMachine != null)
                {
                    _stateMachine.Tick();
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"[ZeroIn] Update error: {ex.Message}", ChatColor.Red);
            }
        }

        private static void HandleCommand(string command, string[] args, AOSharp.Core.UI.ChatWindow chatWindow)
        {
            try
            {
                EnsureInitialized();

                if (_config == null)
                {
                    Chat.WriteLine("ZeroIn is still initializing, please wait...", ChatColor.Yellow);
                    return;
                }

                if (args.Length == 0)
                {
                    ShowHelp();
                    return;
                }

                string subCommand = args[0].ToLower();

                switch (subCommand)
                {
                    case "help":
                        ShowHelp();
                        break;

                    case "start":
                        StartScan();
                        break;

                    case "stop":
                        StopScan();
                        break;

                    case "status":
                        ShowStatus();
                        break;

                    case "preview":
                    case "showroute":
                        PreviewRoute();
                        break;

                    case "area":
                        if (args.Length > 1)
                            SetArea(string.Join(" ", args, 1, args.Length - 1));
                        else
                            ListAreas();
                        break;

                    case "config":
                        ShowConfig();
                        break;

                    case "setcorner":
                        if (args.Length > 1 && int.TryParse(args[1], out int corner))
                            SetCorner(corner);
                        else
                            Chat.WriteLine("[ZeroIn] Usage: /zeroin setcorner <1-4>", ChatColor.Yellow);
                        break;

                    case "addarea":
                        if (args.Length > 1)
                            AddArea(string.Join(" ", args, 1, args.Length - 1));
                        else
                            Chat.WriteLine("[ZeroIn] Usage: /zeroin addarea <name>", ChatColor.Yellow);
                        break;

                    case "save":
                        SaveConfig();
                        break;

                    case "reload":
                        ReloadConfig();
                        break;

                    default:
                        Chat.WriteLine($"[ZeroIn] Unknown command: {subCommand}", ChatColor.Red);
                        Chat.WriteLine("[ZeroIn] Type '/zeroin help' for available commands", ChatColor.Yellow);
                        break;
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"[ZeroIn] Command error: {ex.Message}", ChatColor.Red);
            }
        }

        private static void ShowHelp()
        {
            Chat.WriteLine("=".PadRight(80, '='), ChatColor.Yellow);
            Chat.WriteLine(" ZeroIn - AFK Character Mapper - Commands", ChatColor.Yellow);
            Chat.WriteLine("=".PadRight(80, '='), ChatColor.Yellow);
            Chat.WriteLine("", ChatColor.White);
            Chat.WriteLine("  /zeroin start              - Start scanning current area", ChatColor.White);
            Chat.WriteLine("  /zeroin stop               - Stop current scan", ChatColor.White);
            Chat.WriteLine("  /zeroin status             - Show scan status and results", ChatColor.White);
            Chat.WriteLine("  /zeroin preview            - Preview planned route (roomba pattern)", ChatColor.White);
            Chat.WriteLine("", ChatColor.White);
            Chat.WriteLine("  /zeroin area [name]        - Set/list areas", ChatColor.White);
            Chat.WriteLine("  /zeroin addarea <name>     - Add new area with current position", ChatColor.White);
            Chat.WriteLine("  /zeroin setcorner <1-4>    - Set corner N to current position", ChatColor.White);
            Chat.WriteLine("", ChatColor.White);
            Chat.WriteLine("  /zeroin config             - Show current configuration", ChatColor.White);
            Chat.WriteLine("  /zeroin save               - Save current configuration", ChatColor.White);
            Chat.WriteLine("  /zeroin reload             - Reload configuration from file", ChatColor.White);
            Chat.WriteLine("", ChatColor.White);
            Chat.WriteLine("Quick Start:", ChatColor.LightBlue);
            Chat.WriteLine("  1. /zeroin addarea \"My Zone\"", ChatColor.White);
            Chat.WriteLine("  2. Move to corner 1 and /zeroin setcorner 1", ChatColor.White);
            Chat.WriteLine("  3. Repeat for corners 2, 3, 4", ChatColor.White);
            Chat.WriteLine("  4. /zeroin save", ChatColor.White);
            Chat.WriteLine("  5. /zeroin preview  (to see the planned route)", ChatColor.White);
            Chat.WriteLine("  6. /zeroin start", ChatColor.White);
        }

        private static void StartScan()
        {
            if (_stateMachine.CurrentState == ScanStateMachine.State.Scanning)
            {
                Chat.WriteLine("[ZeroIn] Scan already in progress!", ChatColor.Yellow);
                return;
            }

            var area = _config.GetCurrentArea();
            if (area == null || !area.IsValid())
            {
                Chat.WriteLine("[ZeroIn] Cannot start scan: No valid area configured", ChatColor.Red);
                Chat.WriteLine("[ZeroIn] Use '/zeroin addarea <name>' and '/zeroin setcorner <1-4>' to configure an area", ChatColor.Yellow);
                return;
            }

            Chat.WriteLine($"[ZeroIn] Starting scan of area: {area.Name}", ChatColor.Green);
            _stateMachine.StartScan();
        }

        private static void StopScan()
        {
            if (_stateMachine.CurrentState != ScanStateMachine.State.Scanning)
            {
                Chat.WriteLine("[ZeroIn] No scan in progress", ChatColor.Yellow);
                return;
            }

            _stateMachine.StopScan();
        }

        private static void ShowStatus()
        {
            Chat.WriteLine("=".PadRight(80, '='), ChatColor.Yellow);
            Chat.WriteLine($" ZeroIn Status", ChatColor.Yellow);
            Chat.WriteLine("=".PadRight(80, '='), ChatColor.Yellow);
            Chat.WriteLine($" State: {_stateMachine.CurrentState}", ChatColor.White);
            Chat.WriteLine($" Current Area: {_config.GetCurrentArea()?.Name ?? "None"}", ChatColor.White);
            Chat.WriteLine($" {_context.Scanner.GetSummary()}", ChatColor.White);
            Chat.WriteLine("=".PadRight(80, '='), ChatColor.Yellow);

            if (_context.Scanner.Count > 0)
            {
                _context.Map.PrintResults(_context.Scanner.GetDetectedCharacters());
            }
        }

        private static void PreviewRoute()
        {
            var area = _config.GetCurrentArea();
            if (area == null || !area.IsValid())
            {
                Chat.WriteLine("[ZeroIn] Cannot preview route: No valid area configured", ChatColor.Red);
                Chat.WriteLine("[ZeroIn] Use '/zeroin addarea <name>' and '/zeroin setcorner <1-4>' to configure an area", ChatColor.Yellow);
                return;
            }

            // Generate grid pattern
            var gridGenerator = new GridGenerator(area, _config.ScanSpacing);
            var waypoints = gridGenerator.GeneratePattern();

            if (waypoints.Count == 0)
            {
                Chat.WriteLine("[ZeroIn] Error: Failed to generate grid pattern", ChatColor.Red);
                return;
            }

            // Calculate route statistics
            float distance = gridGenerator.EstimateDistance(waypoints);
            float timeSeconds = gridGenerator.EstimateTime(waypoints);
            TimeSpan time = TimeSpan.FromSeconds(timeSeconds);

            var bounds = area.GetBounds();
            float width = bounds.max.X - bounds.min.X;
            float height = bounds.max.Y - bounds.min.Y;

            // Display preview
            Chat.WriteLine("=".PadRight(80, '='), ChatColor.Yellow);
            Chat.WriteLine(" ZeroIn Route Preview - Roomba Pattern", ChatColor.Yellow);
            Chat.WriteLine("=".PadRight(80, '='), ChatColor.Yellow);
            Chat.WriteLine($" Area: {area.Name}", ChatColor.LightBlue);
            Chat.WriteLine($" Dimensions: {width:F1}m x {height:F1}m", ChatColor.White);
            Chat.WriteLine($" Scan Spacing: {_config.ScanSpacing}m", ChatColor.White);
            Chat.WriteLine($" Total Waypoints: {waypoints.Count}", ChatColor.White);
            Chat.WriteLine($" Total Distance: {distance:F1}m", ChatColor.White);
            Chat.WriteLine($" Estimated Time: {time:hh\\:mm\\:ss}", ChatColor.White);
            Chat.WriteLine($" Average Speed: 7 m/s (run speed)", ChatColor.Gray);
            Chat.WriteLine("=".PadRight(80, '='), ChatColor.Yellow);
            Chat.WriteLine("", ChatColor.White);

            // Show first few waypoints as example
            Chat.WriteLine(" Route Pattern (first 10 waypoints):", ChatColor.LightBlue);
            for (int i = 0; i < Math.Min(10, waypoints.Count); i++)
            {
                var wp = waypoints[i];
                string direction = i > 0 ? GetDirection(waypoints[i - 1].Position, wp.Position) : "START";
                Chat.WriteLine($"  {i + 1,3}. ({wp.Position.X:F1}, {wp.Position.Y:F1}) {direction}", ChatColor.White);
            }

            if (waypoints.Count > 10)
            {
                Chat.WriteLine($"  ... ({waypoints.Count - 10} more waypoints)", ChatColor.Gray);
            }

            Chat.WriteLine("", ChatColor.White);
            Chat.WriteLine(" Ready to start? Use '/zeroin start'", ChatColor.Green);
        }

        private static string GetDirection(Vector3 from, Vector3 to)
        {
            float dx = to.X - from.X;
            float dy = to.Y - from.Y;

            if (Math.Abs(dx) > Math.Abs(dy))
            {
                return dx > 0 ? "→ East" : "← West";
            }
            else
            {
                return dy > 0 ? "↑ North" : "↓ South";
            }
        }

        private static void ListAreas()
        {
            Chat.WriteLine($"[ZeroIn] Configured Areas ({_config.ZoneAreas.Count}):", ChatColor.LightBlue);

            for (int i = 0; i < _config.ZoneAreas.Count; i++)
            {
                var area = _config.ZoneAreas[i];
                string marker = (i == _config.CurrentAreaIndex) ? "*" : " ";
                string valid = area.IsValid() ? "V" : "X";
                Chat.WriteLine($"  {marker} [{i}] {area.Name} [{valid}]", ChatColor.White);
            }
        }

        private static void SetArea(string identifier)
        {
            // Try by index first
            if (int.TryParse(identifier, out int index))
            {
                if (index >= 0 && index < _config.ZoneAreas.Count)
                {
                    _config.CurrentAreaIndex = index;
                    Chat.WriteLine($"[ZeroIn] Current area set to: {_config.ZoneAreas[index].Name}", ChatColor.Green);
                    return;
                }
            }

            // Try by name
            for (int i = 0; i < _config.ZoneAreas.Count; i++)
            {
                if (_config.ZoneAreas[i].Name.Equals(identifier, StringComparison.OrdinalIgnoreCase))
                {
                    _config.CurrentAreaIndex = i;
                    Chat.WriteLine($"[ZeroIn] Current area set to: {_config.ZoneAreas[i].Name}", ChatColor.Green);
                    return;
                }
            }

            Chat.WriteLine($"[ZeroIn] Area not found: {identifier}", ChatColor.Red);
        }

        private static void AddArea(string name)
        {
            var newArea = new ZoneArea(name, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero);
            _config.ZoneAreas.Add(newArea);
            _config.CurrentAreaIndex = _config.ZoneAreas.Count - 1;
            Chat.WriteLine($"[ZeroIn] Added new area: {name}", ChatColor.Green);
            Chat.WriteLine($"[ZeroIn] Use '/zeroin setcorner <1-4>' to set corner positions", ChatColor.Yellow);
        }

        private static void SetCorner(int cornerNum)
        {
            if (cornerNum < 1 || cornerNum > 4)
            {
                Chat.WriteLine("[ZeroIn] Corner must be 1-4", ChatColor.Red);
                return;
            }

            var area = _config.GetCurrentArea();
            if (area == null)
            {
                Chat.WriteLine("[ZeroIn] No area selected. Use '/zeroin addarea <name>' first", ChatColor.Red);
                return;
            }

            Vector3 pos = DynelManager.LocalPlayer.Position;

            switch (cornerNum)
            {
                case 1: area.Corner1 = pos; break;
                case 2: area.Corner2 = pos; break;
                case 3: area.Corner3 = pos; break;
                case 4: area.Corner4 = pos; break;
            }

            Chat.WriteLine($"[ZeroIn] Corner {cornerNum} set to: ({pos.X:F1}, {pos.Y:F1}, {pos.Z:F1})", ChatColor.Green);
            string status = area.IsValid() ? "VALID" : "incomplete - set all 4 corners";
            Chat.WriteLine($"[ZeroIn] Area '{area.Name}' is {status}", area.IsValid() ? ChatColor.Green : ChatColor.Yellow);
        }

        private static void ShowConfig()
        {
            Chat.WriteLine("=".PadRight(80, '='), ChatColor.Yellow);
            Chat.WriteLine(" ZeroIn Configuration", ChatColor.Yellow);
            Chat.WriteLine("=".PadRight(80, '='), ChatColor.Yellow);
            Chat.WriteLine($" Scan Spacing: {_config.ScanSpacing}m", ChatColor.White);
            Chat.WriteLine($" Detection Range: {_config.PlayerDetectionRange}m", ChatColor.White);
            Chat.WriteLine($" Continuous Scanning: {_config.ContinuousScanning}", ChatColor.White);
            Chat.WriteLine($" Only AFK: {_config.OnlyAFK}", ChatColor.White);
            Chat.WriteLine($" AFK Check Time: {_config.AFKCheckTimeSeconds}s", ChatColor.White);
            Chat.WriteLine($" Output Folder: {_config.OutputFolder}", ChatColor.White);
            Chat.WriteLine($" Log to Console: {_config.LogToConsole}", ChatColor.White);
            Chat.WriteLine($" Save to JSON: {_config.SaveToJson}", ChatColor.White);
            Chat.WriteLine($" Save to CSV: {_config.SaveToCsv}", ChatColor.White);
            Chat.WriteLine("=".PadRight(80, '='), ChatColor.Yellow);

            var area = _config.GetCurrentArea();
            if (area != null)
            {
                Chat.WriteLine($" Current Area: {area.Name}", ChatColor.LightBlue);
                Chat.WriteLine($"   Corner 1: ({area.Corner1.X:F1}, {area.Corner1.Y:F1}, {area.Corner1.Z:F1})", ChatColor.White);
                Chat.WriteLine($"   Corner 2: ({area.Corner2.X:F1}, {area.Corner2.Y:F1}, {area.Corner2.Z:F1})", ChatColor.White);
                Chat.WriteLine($"   Corner 3: ({area.Corner3.X:F1}, {area.Corner3.Y:F1}, {area.Corner3.Z:F1})", ChatColor.White);
                Chat.WriteLine($"   Corner 4: ({area.Corner4.X:F1}, {area.Corner4.Y:F1}, {area.Corner4.Z:F1})", ChatColor.White);
                Chat.WriteLine($"   Valid: {area.IsValid()}", area.IsValid() ? ChatColor.Green : ChatColor.Red);
            }
        }

        private static void SaveConfig()
        {
            _config.Save(_configPath);
            Chat.WriteLine($"[ZeroIn] Configuration saved", ChatColor.Green);
        }

        private static void ReloadConfig()
        {
            _config = ZeroInConfig.Load(_configPath);
            _context.Config = _config;
            Chat.WriteLine($"[ZeroIn] Configuration reloaded", ChatColor.Green);
        }

        public override void Teardown()
        {
            if (_config != null && _configPath != null)
            {
                Game.OnUpdate -= OnUpdate;
                SaveConfig();
            }
        }
    }
}
