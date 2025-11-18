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
        public override string InfoText => "Channel - Used to sync Start / Stop across clients\n\n" +
            "Attack Mobs - Attacks any non ignored mob within attack range\n" +
            "Padding - Applies padding to the attack range (weapon range - padding)\n\n" +
            "Path to corpses - Paths to corpses (for looting purposes) \n\n" +
            "Path to mobs - Paths to any non ignored mob\n" +
            "Path Range - At which range should we path to attack distance\n\n" +
            "Taunt items - Uses taunt on any non ignored mob\n" +
            "Pull Range - At which range should we use taunt items\n\n" +
            "Override Attack - Override automatic attack range calculation\n" +
            "Attack Range - At which minimal distance to attack target\n\n" +
            "Wander Limit - At which distance should we ignore mobs\n\n" +
            "Fight Timeout - How long until I ignore a particular mob\n\n" +
            "Loot Timeout - How long until I ignore a particular corpse\n\n" +
            "Follow - Follow a target name (if available ) while obeying to the path\n\n" +
            "Don't Roam If - Temporarily stop roaming if all checked conditions are met\n\n" +
            " - Setup Ignored / Priority names in Path Editor\n" +
            " - Make sure to save your path in the Path Editor to avoid losing data.\n" +
            " - Last saved / loaded path will be loaded on every injection.\n" +
            " - Pressing 'Start' will sync the active chars path across all characters\n\n" +
            "Path Editor - Allows creation of paths (looping / back and forth)\n\n" +
            "Core Operations\n" +
            "   Add - Adds a point at players location\n" +
            "   Remove - Remove closest point (green circle)\n" +
            "   Reverse - Reverses the path\n" +
            "   Test / Stop - Test run / stop navigating\n\n" +
            "Edit Operations\n" +
            "   Pickup - picks up closest point (green circle)\n" +
            "   Place - Places picked up point\n\n" +
            "Split Operations\n" +
            "   Select - Selects or unselects closest circle\n" +
            "   Unselect - Unselects closest circle\n" +
            "   Split - Splits two adjacent selected points\n\n" +
            "Misc Operations\n" +
            "   Toggle Lock - Locks the path in place\n" +
            "   Toggle Loop - Loops the path\n" +
            "   Clear - Clears";


        public PathSettingsView PathSettingsView;

        private RoamPathWindow _roamPathWindow;
        private AutoResetInterval _uiUpdateTick;
        private Button _saveConfig;
        private string _roamWindowPath;
        private string _roamInitViewPath;
        private string _roamMainViewPath;
        private string _pathSettingsViewPath;

        public MainWindow(string windowName, string baseWindowPath, string infoWindowPath, string roamWindowPath, string coreViewPath, string pathSettingsViewPath, string roamInitViewPath, string roamMainViewPath, WindowStyle windowStyle = WindowStyle.Default, WindowFlags flags = WindowFlags.AutoScale | WindowFlags.NoFade) : base(windowName, baseWindowPath, infoWindowPath, coreViewPath, windowStyle, flags)
        {
            _uiUpdateTick = new AutoResetInterval(100);
            _roamWindowPath = roamWindowPath;
            _roamInitViewPath = roamInitViewPath;
            _roamMainViewPath = roamMainViewPath;
            _pathSettingsViewPath = pathSettingsViewPath;
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

                Chat.WriteLine($"[MainWindow] Finding RangeSettingsRoot view...", ChatColor.White);
                if (!Window.FindView("RangeSettingsRoot", out View rangeSettingsRoot))
                {
                    Chat.WriteLine($"[MainWindow] ERROR: Could not find RangeSettingsRoot view!", ChatColor.Red);
                    return;
                }
                Chat.WriteLine($"[MainWindow] Found RangeSettingsRoot", ChatColor.Green);

                Chat.WriteLine($"[MainWindow] Creating PathSettingsView from {_pathSettingsViewPath}...", ChatColor.White);
                PathSettingsView = new PathSettingsView(_pathSettingsViewPath);

                if (PathSettingsView == null || PathSettingsView.Root == null)
                {
                    Chat.WriteLine($"[MainWindow] ERROR: PathSettingsView or its Root is null!", ChatColor.Red);
                    return;
                }
                Chat.WriteLine($"[MainWindow] PathSettingsView created successfully", ChatColor.Green);

                Chat.WriteLine($"[MainWindow] Adding PathSettingsView to RangeSettingsRoot...", ChatColor.White);
                rangeSettingsRoot.AddChild(PathSettingsView.Root, true);
                Chat.WriteLine($"[MainWindow] PathSettingsView added successfully", ChatColor.Green);

                //Populate UI with config file data
                Chat.WriteLine($"[MainWindow] Setting PathSettingsView data...", ChatColor.White);
                PathSettingsView.SetData(ZeroIn.Config.PathingConfig);

                Chat.WriteLine($"[MainWindow] Setting CoreSettingsView data...", ChatColor.White);
                if (CoreSettingsView == null)
                {
                    Chat.WriteLine($"[MainWindow] ERROR: CoreSettingsView is null!", ChatColor.Red);
                    return;
                }
                CoreSettingsView.SetData(ZeroIn.Config.CoreConfig);
                Chat.WriteLine($"[MainWindow] Data set successfully", ChatColor.Green);

                Chat.WriteLine($"[MainWindow] Setting up button event handlers...", ChatColor.White);
                if (Window.FindView("PathCreator", out Button pathCreator))
                {
                    pathCreator.Clicked += PathCreatorClick;
                    Chat.WriteLine($"[MainWindow] PathCreator button event handler attached", ChatColor.White);
                }
                else
                {
                    Chat.WriteLine($"[MainWindow] WARNING: PathCreator button not found", ChatColor.Yellow);
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
            ZeroIn.Config.PathingConfig = PathSettingsView.GetData();
            ZeroIn.Config.Save();

            if (ZeroIn.Ipc.ChannelId != ZeroIn.Config.CoreConfig.ChannelId)
                ZeroIn.Ipc.SetChannelId((byte)ZeroIn.Config.CoreConfig.ChannelId);

            if (displayMsg)
                ZeroIn.Log.Information("Config saved! (use the Path Editor to save / export your path)", ChatColor.Green);
        }

        private void PathCreatorClick(object sender, ButtonBase e)
        {
            if (_roamPathWindow != null && _roamPathWindow.Window.IsValid)
                return;

            _roamPathWindow = new RoamPathWindow("AutomatonRoamba Path", _roamWindowPath, _roamInitViewPath, _roamMainViewPath);
            _roamPathWindow.Show();
        }

        private void MainWindowUpdate(object sender, float e)
        {
            if (!_uiUpdateTick.Elapsed)
                return;

            if (_roamPathWindow != null && !_roamPathWindow.Window.IsValid)
            {
                ZeroIn.RoamPath.SPath.IsLocked = true;
                _roamPathWindow = null;
            }
        }
    }
}