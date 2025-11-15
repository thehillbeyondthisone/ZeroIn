using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.UI;
using System;
using System.IO;
using ZeroIn.Config;
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
                // DIAGNOSTIC: Super simple version - just use Logger to see if this even gets called
                Logger.Information("ZeroIn: Plugin Run() method called!");
                Logger.Information("ZeroIn: HELLO FROM ZEROIN!");

                Chat.WriteLine("ZeroIn plugin loaded!", ChatColor.LightBlue);
                Chat.WriteLine("Type /zeroin help for commands", ChatColor.LightBlue);

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
                Logger.Information($"ZeroIn: Config loaded from {_configPath}");

                // Initialize state machine
                _context = new ScanContext(_config);
                _stateMachine = new ScanStateMachine(_context);

                // Register game loop
                Game.OnUpdate += OnUpdate;

                // Register chat commands
                Chat.RegisterCommand("zeroin", HandleCommand);
                Chat.RegisterCommand("zi", HandleCommand);

                Logger.Information("ZeroIn: Plugin initialization complete!");
                Chat.WriteLine("ZeroIn loaded successfully!", ChatColor.Green);
            }
            catch (Exception ex)
            {
                Logger.Error($"ZeroIn ERROR: {ex.Message}");
                Logger.Error($"ZeroIn Stack: {ex.StackTrace}");
                Chat.WriteLine($"ZeroIn failed to load: {ex.Message}", ChatColor.Red);
            }
        }

        private static void OnUpdate(object sender, float deltaTime)
        {
            try
            {
                if (_stateMachine != null)
                {
                    _stateMachine.Tick();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZeroIn] Update error: {ex.Message}");
            }
        }

        private static void HandleCommand(string command, string[] args, AOSharp.Core.UI.ChatWindow chatWindow)
        {
            try
            {
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
                            Console.WriteLine("[ZeroIn] Usage: /zeroin setcorner <1-4>");
                        break;

                    case "addarea":
                        if (args.Length > 1)
                            AddArea(string.Join(" ", args, 1, args.Length - 1));
                        else
                            Console.WriteLine("[ZeroIn] Usage: /zeroin addarea <name>");
                        break;

                    case "save":
                        SaveConfig();
                        break;

                    case "reload":
                        ReloadConfig();
                        break;

                    default:
                        Console.WriteLine($"[ZeroIn] Unknown command: {subCommand}");
                        Console.WriteLine("[ZeroIn] Type '/zeroin help' for available commands");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZeroIn] Command error: {ex.Message}");
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine(" ZeroIn - AFK Character Mapper - Commands");
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine();
            Console.WriteLine("  /zeroin start              - Start scanning current area");
            Console.WriteLine("  /zeroin stop               - Stop current scan");
            Console.WriteLine("  /zeroin status             - Show scan status and results");
            Console.WriteLine();
            Console.WriteLine("  /zeroin area [name]        - Set/list areas");
            Console.WriteLine("  /zeroin addarea <name>     - Add new area with current position");
            Console.WriteLine("  /zeroin setcorner <1-4>    - Set corner N to current position");
            Console.WriteLine();
            Console.WriteLine("  /zeroin config             - Show current configuration");
            Console.WriteLine("  /zeroin save               - Save current configuration");
            Console.WriteLine("  /zeroin reload             - Reload configuration from file");
            Console.WriteLine();
            Console.WriteLine("  /zeroin help               - Show this help");
            Console.WriteLine();
            Console.WriteLine("Quick Start:");
            Console.WriteLine("  1. /zeroin addarea \"My Zone\"");
            Console.WriteLine("  2. Move to corner 1 and /zeroin setcorner 1");
            Console.WriteLine("  3. Repeat for corners 2, 3, 4");
            Console.WriteLine("  4. /zeroin save");
            Console.WriteLine("  5. /zeroin start");
            Console.WriteLine();
        }

        private static void StartScan()
        {
            if (_stateMachine.CurrentState == ScanStateMachine.State.Scanning)
            {
                Console.WriteLine("[ZeroIn] Scan already in progress!");
                return;
            }

            var area = _config.GetCurrentArea();
            if (area == null || !area.IsValid())
            {
                Console.WriteLine("[ZeroIn] Cannot start scan: No valid area configured");
                Console.WriteLine("[ZeroIn] Use '/zeroin addarea <name>' and '/zeroin setcorner <1-4>' to configure an area");
                return;
            }

            Console.WriteLine($"[ZeroIn] Starting scan of area: {area.Name}");
            _stateMachine.StartScan();
        }

        private static void StopScan()
        {
            if (_stateMachine.CurrentState != ScanStateMachine.State.Scanning)
            {
                Console.WriteLine("[ZeroIn] No scan in progress");
                return;
            }

            _stateMachine.StopScan();
        }

        private static void ShowStatus()
        {
            Console.WriteLine();
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine($" ZeroIn Status");
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine($" State: {_stateMachine.CurrentState}");
            Console.WriteLine($" Current Area: {_config.GetCurrentArea()?.Name ?? "None"}");
            Console.WriteLine($" {_context.Scanner.GetSummary()}");
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine();

            if (_context.Scanner.Count > 0)
            {
                _context.Map.PrintResults(_context.Scanner.GetDetectedCharacters());
            }
        }

        private static void ListAreas()
        {
            Console.WriteLine();
            Console.WriteLine($"[ZeroIn] Configured Areas ({_config.ZoneAreas.Count}):");

            for (int i = 0; i < _config.ZoneAreas.Count; i++)
            {
                var area = _config.ZoneAreas[i];
                string marker = (i == _config.CurrentAreaIndex) ? "*" : " ";
                string valid = area.IsValid() ? "✓" : "✗";
                Console.WriteLine($"  {marker} [{i}] {area.Name} {valid}");
            }
            Console.WriteLine();
        }

        private static void SetArea(string identifier)
        {
            // Try by index first
            if (int.TryParse(identifier, out int index))
            {
                if (index >= 0 && index < _config.ZoneAreas.Count)
                {
                    _config.CurrentAreaIndex = index;
                    Console.WriteLine($"[ZeroIn] Current area set to: {_config.ZoneAreas[index].Name}");
                    return;
                }
            }

            // Try by name
            for (int i = 0; i < _config.ZoneAreas.Count; i++)
            {
                if (_config.ZoneAreas[i].Name.Equals(identifier, StringComparison.OrdinalIgnoreCase))
                {
                    _config.CurrentAreaIndex = i;
                    Console.WriteLine($"[ZeroIn] Current area set to: {_config.ZoneAreas[i].Name}");
                    return;
                }
            }

            Console.WriteLine($"[ZeroIn] Area not found: {identifier}");
        }

        private static void AddArea(string name)
        {
            var newArea = new ZoneArea(name, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero);
            _config.ZoneAreas.Add(newArea);
            _config.CurrentAreaIndex = _config.ZoneAreas.Count - 1;
            Console.WriteLine($"[ZeroIn] Added new area: {name}");
            Console.WriteLine($"[ZeroIn] Use '/zeroin setcorner <1-4>' to set corner positions");
        }

        private static void SetCorner(int cornerNum)
        {
            if (cornerNum < 1 || cornerNum > 4)
            {
                Console.WriteLine("[ZeroIn] Corner must be 1-4");
                return;
            }

            var area = _config.GetCurrentArea();
            if (area == null)
            {
                Console.WriteLine("[ZeroIn] No area selected. Use '/zeroin addarea <name>' first");
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

            Console.WriteLine($"[ZeroIn] Corner {cornerNum} set to: ({pos.X:F1}, {pos.Y:F1}, {pos.Z:F1})");
            Console.WriteLine($"[ZeroIn] Area '{area.Name}' is {(area.IsValid() ? "VALID" : "incomplete - set all 4 corners")}");
        }

        private static void ShowConfig()
        {
            Console.WriteLine();
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine(" ZeroIn Configuration");
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine($" Scan Spacing: {_config.ScanSpacing}m");
            Console.WriteLine($" Detection Range: {_config.PlayerDetectionRange}m");
            Console.WriteLine($" Continuous Scanning: {_config.ContinuousScanning}");
            Console.WriteLine($" Only AFK: {_config.OnlyAFK}");
            Console.WriteLine($" AFK Check Time: {_config.AFKCheckTimeSeconds}s");
            Console.WriteLine($" Output Folder: {_config.OutputFolder}");
            Console.WriteLine($" Log to Console: {_config.LogToConsole}");
            Console.WriteLine($" Save to JSON: {_config.SaveToJson}");
            Console.WriteLine($" Save to CSV: {_config.SaveToCsv}");
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine();

            var area = _config.GetCurrentArea();
            if (area != null)
            {
                Console.WriteLine($" Current Area: {area.Name}");
                Console.WriteLine($"   Corner 1: ({area.Corner1.X:F1}, {area.Corner1.Y:F1}, {area.Corner1.Z:F1})");
                Console.WriteLine($"   Corner 2: ({area.Corner2.X:F1}, {area.Corner2.Y:F1}, {area.Corner2.Z:F1})");
                Console.WriteLine($"   Corner 3: ({area.Corner3.X:F1}, {area.Corner3.Y:F1}, {area.Corner3.Z:F1})");
                Console.WriteLine($"   Corner 4: ({area.Corner4.X:F1}, {area.Corner4.Y:F1}, {area.Corner4.Z:F1})");
                Console.WriteLine($"   Valid: {area.IsValid()}");
                Console.WriteLine();
            }
        }

        private static void SaveConfig()
        {
            _config.Save(_configPath);
            Console.WriteLine($"[ZeroIn] Configuration saved to: {_configPath}");
        }

        private static void ReloadConfig()
        {
            _config = ZeroInConfig.Load(_configPath);
            _context.Config = _config;
            Console.WriteLine($"[ZeroIn] Configuration reloaded from: {_configPath}");
        }

        public override void Teardown()
        {
            Game.OnUpdate -= OnUpdate;
            SaveConfig();
            Console.WriteLine("[ZeroIn] Plugin unloaded");
        }
    }
}
