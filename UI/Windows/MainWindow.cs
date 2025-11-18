using AOSharp.Common.GameData.UI;
using AOSharp.Core.UI;
using AOSharp.Core;
using AOSharp.Core.Misc;
using ZeroIn.IPCMessages;
using AOSharp.Pathfinding;
using AOSharp.Common.GameData;
using System;
using System.Linq;
using Buddy.Shared.UI;

namespace ZeroIn
{
    public class MainWindow : BuddyBaseWindow
    {
        //Per plugin adjustable
        public override string CoreViewRootName => "CoreViewRoot";

        //Per plugin adjustable
        public override string InfoText => "ZeroIn - AFK Player Scanner\n\n" +
            "Setup Scan Area:\n" +
            "1. Click 'Setup Scan Area'\n" +
            "2. Move to 4 corners of the zone\n" +
            "3. Click 'Set Corner' at each location\n" +
            "4. Click 'Generate Grid Pattern'\n\n" +
            "Scan Settings:\n" +
            " - Scan Spacing: Distance between grid lines (default 40m)\n" +
            " - Detection Range: How far to detect players (default 50m)\n" +
            " - Only AFK: Only save players who don't move\n" +
            " - AFK Time: Seconds to observe before marking AFK\n\n" +
            "Output Formats:\n" +
            " - JSON: Full character data\n" +
            " - CSV: Spreadsheet format\n" +
            " - Console: Print detections to chat\n\n" +
            "Start Scanning:\n" +
            "1. Press 'Start' to begin the scan\n" +
            "2. The bot will follow the grid pattern\n" +
            "3. All detected players are automatically logged\n" +
            "4. Press 'Start' again to stop\n\n" +
            "View Results:\n" +
            " - Click 'View Details' to see scan results\n" +
            " - Check output folder for saved files\n" +
            " - Use /scan command for quick status";


        public ScanSettingsView ScanSettingsView;

        private RoamPathWindow _roamPathWindow;
        private AutoResetInterval _uiUpdateTick;
        private Button _saveConfig;
        private TextView _detectedCount;
        private TextView _afkCount;
        private string _roamWindowPath;
        private string _roamInitViewPath;
        private string _roamMainViewPath;
        private string _scanSettingsViewPath;

        public MainWindow(string windowName, string baseWindowPath, string infoWindowPath, string roamWindowPath, string coreViewPath, string scanSettingsViewPath, string roamInitViewPath, string roamMainViewPath, WindowStyle windowStyle = WindowStyle.Default, WindowFlags flags = WindowFlags.AutoScale | WindowFlags.NoFade) : base(windowName, baseWindowPath, infoWindowPath, coreViewPath, windowStyle, flags)
        {
            _uiUpdateTick = new AutoResetInterval(100);
            _roamWindowPath = roamWindowPath;
            _roamInitViewPath = roamInitViewPath;
            _roamMainViewPath = roamMainViewPath;
            _scanSettingsViewPath = scanSettingsViewPath;
        }

        protected override void OnWindowCreating()
        {
            try
            {
                Chat.WriteLine($"[MainWindow] OnWindowCreating starting...", ChatColor.White);

                //Make sure to call the base first in order to init all the core views
                Chat.WriteLine($"[MainWindow] Calling base.OnWindowCreating()...", ChatColor.White);
                base.OnWindowCreating();
                Chat.WriteLine($"[MainWindow] Base OnWindowCreating completed", ChatColor.Green);

                Chat.WriteLine($"[MainWindow] Finding ScanSettingsRoot view...", ChatColor.White);
                if (!Window.FindView("ScanSettingsRoot", out View scanSettingsRoot))
                {
                    Chat.WriteLine($"[MainWindow] ERROR: Could not find ScanSettingsRoot view!", ChatColor.Red);
                    return;
                }
                Chat.WriteLine($"[MainWindow] Found ScanSettingsRoot", ChatColor.Green);

                Chat.WriteLine($"[MainWindow] Creating ScanSettingsView from {_scanSettingsViewPath}...", ChatColor.White);
                ScanSettingsView = new ScanSettingsView(_scanSettingsViewPath);

                if (ScanSettingsView == null || ScanSettingsView.Root == null)
                {
                    Chat.WriteLine($"[MainWindow] ERROR: ScanSettingsView or its Root is null!", ChatColor.Red);
                    return;
                }
                Chat.WriteLine($"[MainWindow] ScanSettingsView created successfully", ChatColor.Green);

                Chat.WriteLine($"[MainWindow] Adding ScanSettingsView to ScanSettingsRoot...", ChatColor.White);
                scanSettingsRoot.AddChild(ScanSettingsView.Root, true);
                Chat.WriteLine($"[MainWindow] ScanSettingsView added successfully", ChatColor.Green);

                //Populate UI with config file data
                Chat.WriteLine($"[MainWindow] Setting ScanSettingsView data...", ChatColor.White);
                ScanSettingsView.SetData(ZeroIn.Config);

                Chat.WriteLine($"[MainWindow] Setting CoreSettingsView data...", ChatColor.White);
                if (CoreSettingsView == null)
                {
                    Chat.WriteLine($"[MainWindow] ERROR: CoreSettingsView is null!", ChatColor.Red);
                    return;
                }
                CoreSettingsView.SetData(ZeroIn.Config.CoreConfig);
                Chat.WriteLine($"[MainWindow] Data set successfully", ChatColor.Green);

                Chat.WriteLine($"[MainWindow] Setting up button event handlers...", ChatColor.White);
                if (Window.FindView("AreaSetup", out Button areaSetup))
                {
                    areaSetup.Clicked += OnAreaSetupClick;
                    Chat.WriteLine($"[MainWindow] AreaSetup button event handler attached", ChatColor.White);
                }
                else
                {
                    Chat.WriteLine($"[MainWindow] WARNING: AreaSetup button not found", ChatColor.Yellow);
                }

                if (Window.FindView("GenerateGrid", out Button generateGrid))
                {
                    generateGrid.Clicked += OnGenerateGridClick;
                    Chat.WriteLine($"[MainWindow] GenerateGrid button event handler attached", ChatColor.White);
                }
                else
                {
                    Chat.WriteLine($"[MainWindow] WARNING: GenerateGrid button not found", ChatColor.Yellow);
                }

                if (Window.FindView("ViewResults", out Button viewResults))
                {
                    viewResults.Clicked += OnViewResultsClick;
                    Chat.WriteLine($"[MainWindow] ViewResults button event handler attached", ChatColor.White);
                }
                else
                {
                    Chat.WriteLine($"[MainWindow] WARNING: ViewResults button not found", ChatColor.Yellow);
                }

                if (Window.FindView("SaveConfig", out _saveConfig))
                {
                    _saveConfig.Clicked = OnSaveConfigClick;
                    Chat.WriteLine($"[MainWindow] SaveConfig button event handler attached", ChatColor.White);
                }
                else
                {
                    Chat.WriteLine($"[MainWindow] WARNING: SaveConfig button not found", ChatColor.Yellow);
                }

                // Find result display text views
                if (Window.FindView("DetectedCount", out _detectedCount))
                {
                    Chat.WriteLine($"[MainWindow] DetectedCount text view found", ChatColor.White);
                }

                if (Window.FindView("AFKCount", out _afkCount))
                {
                    Chat.WriteLine($"[MainWindow] AFKCount text view found", ChatColor.White);
                }

                Chat.WriteLine($"[MainWindow] Setting window position...", ChatColor.White);
                var screenSize = Window.GetScreenSize();

                if (ZeroIn.Config.WindowCoords.X > screenSize.X || ZeroIn.Config.WindowCoords.Y > screenSize.Y)
                    Window.MoveToCenter();
                else if (ZeroIn.Config.WindowCoords.X != 0 && ZeroIn.Config.WindowCoords.Y != 0)
                    Window.MoveTo(ZeroIn.Config.WindowCoords.X, ZeroIn.Config.WindowCoords.Y);

                Chat.WriteLine($"[MainWindow] Attaching EnabledButton click handler...", ChatColor.White);
                if (CoreSettingsView.EnabledButton == null)
                {
                    Chat.WriteLine($"[MainWindow] ERROR: CoreSettingsView.EnabledButton is null!", ChatColor.Red);
                    return;
                }
                CoreSettingsView.EnabledButton.Clicked += OnEnable;

                Chat.WriteLine($"[MainWindow] Registering Game.OnUpdate handler...", ChatColor.White);
                Game.OnUpdate += MainWindowUpdate;

                Chat.WriteLine($"[MainWindow] OnWindowCreating completed successfully!", ChatColor.Green);
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"[MainWindow] EXCEPTION in OnWindowCreating!", ChatColor.Red);
                Chat.WriteLine($"[MainWindow] Error: {ex.Message}", ChatColor.Red);
                Chat.WriteLine($"[MainWindow] Type: {ex.GetType().Name}", ChatColor.Red);
                Chat.WriteLine($"[MainWindow] Stack trace:", ChatColor.Red);
                string[] lines = ex.StackTrace?.Split('\n') ?? new string[0];
                foreach (var line in lines.Take(10))
                {
                    Chat.WriteLine($"  {line.Trim()}", ChatColor.Red);
                }
                if (ex.InnerException != null)
                {
                    Chat.WriteLine($"[MainWindow] Inner exception: {ex.InnerException.Message}", ChatColor.Red);
                }
            }
        }

        private void OnEnable(object sender, ButtonBase e)
        {
            try
            {
                SaveConfig(false);

                if (ZeroIn.RoamPath == null || ZeroIn.RoamPath.SPath == null)
                {
                    ZeroIn.Log.Information("No paths loaded");
                    return;
                }

                if (ZeroIn.RoamPath.SPath.PlayfieldId != Playfield.ModelIdentity.Instance)
                {
                    CoreSettingsView.IsButtonEnabled = false;
                    CoreSettingsView.SetButtonState(false);
                    ZeroIn.Log.Information("Cannot start a path saved for a different playfield.");
                    return;
                }

                if (CoreSettingsView.IsButtonEnabled && ZeroIn.RoamPath.SPath.Waypoints.Count == 0)
                {
                    CoreSettingsView.IsButtonEnabled = false;
                    CoreSettingsView.SetButtonState(false);
                    ZeroIn.Log.Information("Path needs to have at least one waypoint");   
                }
                else
                {
                    ZeroIn.Ipc.Broadcast(new EnabledIPCMessage
                    {
                        SetEnabled = CoreSettingsView.IsButtonEnabled,
                        RoamPath = ZeroIn.Config.RoamPath,
                        PathConfig = ZeroIn.Config.PathingConfig,
                    });

                    OnEnabledPress(CoreSettingsView.IsButtonEnabled);
                }

            }
            catch (Exception ex)
            {
                ZeroIn.Log.Information(ex.Message);
            }
        }

        public void OnEnabledPress(bool result)
        {
            CoreSettingsView.SetButtonState(result);

            ZeroIn.StateMachine.SetStatus(result);

            if (SMovementController.IsNavigating())
                SMovementController.Halt();

            ZeroIn.Log.Information(result ? "Starting" : "Stopping");
        }

        private void OnSaveConfigClick(object sender, ButtonBase e)
        {
            SaveConfig();
        }

        private void SaveConfig(bool displayMsg = true)
        {
            ZeroIn.Config.CoreConfig = CoreSettingsView.GetData();
            ZeroIn.Config.WindowCoords = new Vector2(Window.GetFrame().MinX, Window.GetFrame().MinY);
            ScanSettingsView.UpdateConfig(ZeroIn.Config);
            ZeroIn.Config.Save();

            if (ZeroIn.Ipc.ChannelId != ZeroIn.Config.CoreConfig.ChannelId)
                ZeroIn.Ipc.SetChannelId((byte)ZeroIn.Config.CoreConfig.ChannelId);

            if (displayMsg)
                ZeroIn.Log.Information("Scanner config saved!", ChatColor.Green);
        }

        private void OnAreaSetupClick(object sender, ButtonBase e)
        {
            if (_roamPathWindow != null && _roamPathWindow.Window.IsValid)
                return;

            _roamPathWindow = new RoamPathWindow("ZeroIn Scan Area Setup", _roamWindowPath, _roamInitViewPath, _roamMainViewPath);
            _roamPathWindow.Show();
        }

        private void OnGenerateGridClick(object sender, ButtonBase e)
        {
            try
            {
                SaveConfig(false);

                // TODO: Implement grid pattern generation
                // This will create a lawnmower pattern based on scan area corners and spacing
                Chat.WriteLine("[ZeroIn] Grid generation coming soon!", ChatColor.Yellow);
                Chat.WriteLine("[ZeroIn] For now, use 'Setup Scan Area' to create a path manually", ChatColor.White);
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"[ZeroIn] Error generating grid: {ex.Message}", ChatColor.Red);
            }
        }

        private void OnViewResultsClick(object sender, ButtonBase e)
        {
            try
            {
                var detected = ZeroIn.Scanner.GetDetectedCharacters();
                Chat.WriteLine($"=== ZeroIn Scan Results ===", ChatColor.Yellow);
                Chat.WriteLine($"Total detected: {detected.Count}", ChatColor.LightBlue);

                var afkPlayers = detected.Where(p => !p.HasMoved).ToList();
                Chat.WriteLine($"AFK players: {afkPlayers.Count}", ChatColor.LightBlue);

                if (detected.Count > 0)
                {
                    Chat.WriteLine($"\nTop 10 recent detections:", ChatColor.White);
                    foreach (var player in detected.OrderByDescending(p => p.LastSeen).Take(10))
                    {
                        var status = player.HasMoved ? "Active" : "AFK";
                        var age = (DateTime.UtcNow - player.LastSeen).TotalSeconds;
                        Chat.WriteLine($"  [{status}] {player.Name} - seen {age:F0}s ago (spotted {player.TimesSpotted}x)", ChatColor.White);
                    }

                    Chat.WriteLine($"\nFull results saved to: {ZeroIn.Config.OutputFolder}", ChatColor.Green);
                }
                else
                {
                    Chat.WriteLine($"No players detected yet. Start scanning to find players!", ChatColor.White);
                }
            }
            catch (Exception ex)
            {
                Chat.WriteLine($"[ZeroIn] Error viewing results: {ex.Message}", ChatColor.Red);
            }
        }

        private void MainWindowUpdate(object sender, float e)
        {
            if (!_uiUpdateTick.Elapsed)
                return;

            // Update scan result counts
            if (_detectedCount != null && _afkCount != null && ZeroIn.Scanner != null)
            {
                var detected = ZeroIn.Scanner.GetDetectedCharacters();
                var afkPlayers = detected.Where(p => !p.HasMoved).ToList();

                _detectedCount.Text = $"#Detected: {detected.Count}";
                _afkCount.Text = $"#AFK: {afkPlayers.Count}";
            }

            if (_roamPathWindow != null && !_roamPathWindow.Window.IsValid)
            {
                ZeroIn.RoamPath.SPath.IsLocked = true;
                _roamPathWindow = null;
            }
        }
    }
}