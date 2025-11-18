using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Pathfinding;
using System;
using System.IO;
using Path = System.IO.Path;

namespace AutomatonRoamba
{
    public class RoamPathInitView : RoamPathView
    {
        private ComboBox _pathName;

        public RoamPathInitView(string path, RoamPathWindow window) : base(path, window)
        {
            try
            {
                if (Root.FindChild("AllPaths", out _pathName))
                {
                    if (!string.IsNullOrEmpty(AutomatonRoamba.Config.RoamPath))
                        _pathName.SetText(Path.GetFileNameWithoutExtension(AutomatonRoamba.Config.RoamPath));

                    var allFiles = Directory.GetFiles(AutomatonRoamba.Config.RoamPathFolder);

                    for (int i = 0; i < allFiles.Length; i++)
                        _pathName.AppendItem(i + 1, Path.GetFileNameWithoutExtension(allFiles[i]));
                }

                if (Root.FindChild("CreateNewPath", out Button createPath))
                    createPath.Clicked = CreateNewPathClicked;

                if (Root.FindChild("EditCurrentPath", out Button editPath))
                    editPath.Clicked = EditCurrentPathClicked;

                if (Root.FindChild("LoadPath", out Button loadPath))
                    loadPath.Clicked = LoadClicked;
            }
            catch (Exception ex)
            {
                AutomatonRoamba.Log.Warning(ex.Message);
            }
        }

        private void EditCurrentPathClicked(object sender, ButtonBase e)
        {
            if (AutomatonRoamba.RoamPath.SPath == null)
            {
                AutomatonRoamba.Log.Information("No path to edit");
                return;
            }

            if (AutomatonRoamba.RoamPath.SPath.PlayfieldId != Playfield.ModelIdentity.Instance)
            {
                AutomatonRoamba.Log.Information($"Cannot edit path saved for a different playfield.");
                return;
            }

            Load();
        }

        private void LoadClicked(object sender, ButtonBase e)
        {
            var fullPath = $"{CommonParameters.PluginDataPath}\\RoamPath\\" + _pathName.GetText() + ".json";

            if (!File.Exists(fullPath))
            {
                AutomatonRoamba.Log.Information($"File not found '{fullPath}'");
                return;
            }

            if (AutomatonRoamba.RoamPath.SPath != null)
                AutomatonRoamba.RoamPath.SPath.Delete();

            var roamPath = RoamPath.Load(fullPath);

            AutomatonRoamba.RoamPath.SPath = roamPath.SPath;
            AutomatonRoamba.RoamPath.Rules = roamPath.Rules;

            if (AutomatonRoamba.RoamPath.SPath.PlayfieldId != Playfield.ModelIdentity.Instance)
            {
                AutomatonRoamba.Log.Information($"Cannot load a path saved for a different playfield.");
                AutomatonRoamba.RoamPath.SPath.Delete();
                AutomatonRoamba.RoamPath.SPath = null;
                return;
            }

            AutomatonRoamba.Config.RoamPath = fullPath;
            AutomatonRoamba.Config.Save();
            Load();
        }

        private void CreateNewPathClicked(object sender, ButtonBase e)
        {
            if (AutomatonRoamba.RoamPath.SPath != null)
                AutomatonRoamba.RoamPath.SPath.Delete();

            AutomatonRoamba.RoamPath.SPath = SPath.Create();
            AutomatonRoamba.RoamPath.Rules = new TargetingRules();

            Load();
        }

        private void Load()
        {
            AutomatonRoamba.RoamPath.SPath.IsLocked = AutomatonRoamba.RoamPath.SPath.Waypoints.Count != 0;
            Dispose();
            Parent.LoadMainView();
        }
    }
}