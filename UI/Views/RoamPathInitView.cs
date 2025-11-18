using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Pathfinding;
using System;
using System.IO;
using Path = System.IO.Path;

namespace ZeroIn
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
                    if (!string.IsNullOrEmpty(ZeroIn.Config.RoamPath))
                        _pathName.SetText(Path.GetFileNameWithoutExtension(ZeroIn.Config.RoamPath));

                    var allFiles = Directory.GetFiles(ZeroIn.Config.RoamPathFolder);

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
                ZeroIn.Log.Warning(ex.Message);
            }
        }

        private void EditCurrentPathClicked(object sender, ButtonBase e)
        {
            if (ZeroIn.RoamPath.SPath == null)
            {
                ZeroIn.Log.Information("No path to edit");
                return;
            }

            if (ZeroIn.RoamPath.SPath.PlayfieldId != Playfield.ModelIdentity.Instance)
            {
                ZeroIn.Log.Information($"Cannot edit path saved for a different playfield.");
                return;
            }

            Load();
        }

        private void LoadClicked(object sender, ButtonBase e)
        {
            var fullPath = $"{CommonParameters.PluginDataPath}\\RoamPath\\" + _pathName.GetText() + ".json";

            if (!File.Exists(fullPath))
            {
                ZeroIn.Log.Information($"File not found '{fullPath}'");
                return;
            }

            if (ZeroIn.RoamPath.SPath != null)
                ZeroIn.RoamPath.SPath.Delete();

            var roamPath = RoamPath.Load(fullPath);

            ZeroIn.RoamPath.SPath = roamPath.SPath;
            ZeroIn.RoamPath.Rules = roamPath.Rules;

            if (ZeroIn.RoamPath.SPath.PlayfieldId != Playfield.ModelIdentity.Instance)
            {
                ZeroIn.Log.Information($"Cannot load a path saved for a different playfield.");
                ZeroIn.RoamPath.SPath.Delete();
                ZeroIn.RoamPath.SPath = null;
                return;
            }

            ZeroIn.Config.RoamPath = fullPath;
            ZeroIn.Config.Save();
            Load();
        }

        private void CreateNewPathClicked(object sender, ButtonBase e)
        {
            if (ZeroIn.RoamPath.SPath != null)
                ZeroIn.RoamPath.SPath.Delete();

            ZeroIn.RoamPath.SPath = SPath.Create();
            ZeroIn.RoamPath.Rules = new TargetingRules();

            Load();
        }

        private void Load()
        {
            ZeroIn.RoamPath.SPath.IsLocked = ZeroIn.RoamPath.SPath.Waypoints.Count != 0;
            Dispose();
            Parent.LoadMainView();
        }
    }
}