using AOSharp.Common.GameData.UI;
using AOSharp.Core.UI;
using AOSharp.Core;
using AOSharp.Core.Misc;
using AutomatonRoamba.IPCMessages;
using AOSharp.Pathfinding;
using AOSharp.Common.GameData;
using System;
using Buddy.Shared.UI;

namespace AutomatonRoamba
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
            //Make sure to call the base first in order to init all the core views
            base.OnWindowCreating();

            Window.FindView("RangeSettingsRoot", out View rangeSettingsRoot);
            PathSettingsView = new PathSettingsView(_pathSettingsViewPath);
            rangeSettingsRoot.AddChild(PathSettingsView.Root, true);

            //Populate UI with config file data

            PathSettingsView.SetData(AutomatonRoamba.Config.PathingConfig);
            CoreSettingsView.SetData(AutomatonRoamba.Config.CoreConfig);

            if (Window.FindView("PathCreator", out Button pathCreator))
                pathCreator.Clicked += PathCreatorClick;

            if (Window.FindView("SaveConfig", out _saveConfig))
                _saveConfig.Clicked = OnSaveConfigClick;

            var screenSize = Window.GetScreenSize();

            if (AutomatonRoamba.Config.WindowCoords.X > screenSize.X || AutomatonRoamba.Config.WindowCoords.Y > screenSize.Y)
                Window.MoveToCenter();
            else if (AutomatonRoamba.Config.WindowCoords.X != 0 && AutomatonRoamba.Config.WindowCoords.Y != 0)
                Window.MoveTo(AutomatonRoamba.Config.WindowCoords.X, AutomatonRoamba.Config.WindowCoords.Y);

            CoreSettingsView.EnabledButton.Clicked += OnEnable;
            Game.OnUpdate += MainWindowUpdate;
        }

        private void OnEnable(object sender, ButtonBase e)
        {
            try
            {
                SaveConfig(false);

                if (AutomatonRoamba.RoamPath == null || AutomatonRoamba.RoamPath.SPath == null)
                {
                    AutomatonRoamba.Log.Information("No paths loaded");
                    return;
                }

                if (AutomatonRoamba.RoamPath.SPath.PlayfieldId != Playfield.ModelIdentity.Instance)
                {
                    CoreSettingsView.IsButtonEnabled = false;
                    CoreSettingsView.SetButtonState(false);
                    AutomatonRoamba.Log.Information("Cannot start a path saved for a different playfield.");
                    return;
                }

                if (CoreSettingsView.IsButtonEnabled && AutomatonRoamba.RoamPath.SPath.Waypoints.Count == 0)
                {
                    CoreSettingsView.IsButtonEnabled = false;
                    CoreSettingsView.SetButtonState(false);
                    AutomatonRoamba.Log.Information("Path needs to have at least one waypoint");   
                }
                else
                {
                    AutomatonRoamba.Ipc.Broadcast(new EnabledIPCMessage
                    {
                        SetEnabled = CoreSettingsView.IsButtonEnabled,
                        RoamPath = AutomatonRoamba.Config.RoamPath,
                        PathConfig = AutomatonRoamba.Config.PathingConfig,
                    });

                    OnEnabledPress(CoreSettingsView.IsButtonEnabled);
                }

            }
            catch (Exception ex)
            {
                AutomatonRoamba.Log.Information(ex.Message);
            }
        }

        public void OnEnabledPress(bool result)
        {
            CoreSettingsView.SetButtonState(result);

            AutomatonRoamba.StateMachine.SetStatus(result);

            if (SMovementController.IsNavigating())
                SMovementController.Halt();

            AutomatonRoamba.Log.Information(result ? "Starting" : "Stopping");
        }

        private void OnSaveConfigClick(object sender, ButtonBase e)
        {
            SaveConfig();
        }

        private void SaveConfig(bool displayMsg = true)
        {
            AutomatonRoamba.Config.CoreConfig = CoreSettingsView.GetData();
            AutomatonRoamba.Config.WindowCoords = new Vector2(Window.GetFrame().MinX, Window.GetFrame().MinY);
            AutomatonRoamba.Config.PathingConfig = PathSettingsView.GetData();
            AutomatonRoamba.Config.Save();

            if (AutomatonRoamba.Ipc.ChannelId != AutomatonRoamba.Config.CoreConfig.ChannelId)
                AutomatonRoamba.Ipc.SetChannelId(AutomatonRoamba.Config.CoreConfig.ChannelId);

            if (displayMsg)
                AutomatonRoamba.Log.Information("Config saved! (use the Path Editor to save / export your path)", ChatColor.Green);
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
                AutomatonRoamba.RoamPath.SPath.IsLocked = true;
                _roamPathWindow = null;
            }
        }
    }
}