using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Pathfinding;
using System;
using System.Linq;

namespace ZeroIn
{
    public class RoamPathMainView : RoamPathView
    {
        private TextView _pointsText;
        private TextView _isLoopingText;
        private TextView _isReversedText;
        private TextView _isLockedText;
        private TextView _playfieldText;
        private TextInputView _priorityNames;
        private TextInputView _ignoredNames;
        private TextInputView _fileName;

        public RoamPathMainView(string path, RoamPathWindow window) : base(path, window)
        {
            try
            {
                if (Root.FindChild("AddPoint", out Button addPoint)) { addPoint.Clicked = AddPointClick; }
                if (Root.FindChild("RemovePoint", out Button removePoint)) { removePoint.Clicked = RemovePointClick; }
                if (Root.FindChild("ReversePath", out Button reversePath)) { reversePath.Clicked = ReversePathClick; }
                if (Root.FindChild("RunPath", out Button runPath)) { runPath.Clicked = RunPathClicked; }
                if (Root.FindChild("PickupPoint", out Button pickupPoint)) { pickupPoint.Clicked = PickupPointClick; }
                if (Root.FindChild("PlacePoint", out Button placePoint)) { placePoint.Clicked = PlacePointClick; }
                if (Root.FindChild("SelectPoint", out Button selectPoint)) { selectPoint.Clicked = SelectPointClick; }
                if (Root.FindChild("UnselectPoint", out Button unselectPoint)) { unselectPoint.Clicked = UnselectPointClick; }
                if (Root.FindChild("SplitPath", out Button splitPath)) { splitPath.Clicked = SplitPointClick; }
                if (Root.FindChild("ToggleLock", out Button toggleLock)) { toggleLock.Clicked = ToggleLockClick; }
                if (Root.FindChild("ToggleLoop", out Button toggleLoop)) { toggleLoop.Clicked = ToggleLoopClick; }
                if (Root.FindChild("ClearPath", out Button clearPath)) { clearPath.Clicked = ClearClick; }
                if (Root.FindChild("Points", out _pointsText)) { _pointsText.Text = ZeroIn.RoamPath.SPath.Waypoints.Count.ToString(); }
                if (Root.FindChild("IsLooping", out _isLoopingText)) { _isLoopingText.Text = ZeroIn.RoamPath.SPath.IsLooping.ToString(); }
                if (Root.FindChild("IsReversed", out _isReversedText)) { _isReversedText.Text = ZeroIn.RoamPath.SPath.IsReversed.ToString(); }
                if (Root.FindChild("IsLocked", out _isLockedText)) { _isLockedText.Text = ZeroIn.RoamPath.SPath.IsLocked.ToString(); }
                if (Root.FindChild("Playfield", out _playfieldText)) 
                {
                    _playfieldText.Text = Playfield.ModelIdentity.Instance.ToString();
                    SetPlayfieldTextColor(ZeroIn.RoamPath.SPath.PlayfieldId == Playfield.ModelIdentity.Instance); 
                }
                if (Root.FindChild("IgnoredNames", out _ignoredNames)) { _ignoredNames.Text = ZeroIn.RoamPath.Rules.IgnoredNames?.Count == 0 ? "Leetzor\nDraculeet\nNerdleet" : string.Join("\n", ZeroIn.RoamPath.Rules.IgnoredNames); }
                if (Root.FindChild("PriorityNames", out _priorityNames)) { _priorityNames.Text = ZeroIn.RoamPath.Rules.PriorityNames?.Count == 0 ? "Pinkleet\nBlueLeet\nRedLeet" : string.Join("\n", ZeroIn.RoamPath.Rules.PriorityNames); }
                if (Root.FindChild("Save", out Button save)) { save.Clicked += SaveClick; }
                if (Root.FindChild("FileName", out _fileName))
                {
                    if (string.IsNullOrEmpty(ZeroIn.Config.RoamPath) || ZeroIn.RoamPath.SPath.PlayfieldId != Playfield.ModelIdentity.Instance || ZeroIn.RoamPath.SPath.Waypoints.Count() == 0)
                        _fileName.Text = "";
                    else
                        _fileName.Text = System.IO.Path.GetFileNameWithoutExtension(ZeroIn.Config.RoamPath);
                }

            }
            catch (Exception ex)
            {
                ZeroIn.Log.Warning(ex.Message);
            }
        }

        public string GetIgnoredText()
        {
            return _ignoredNames.Text;
        }

        public string GetPriorityText()
        {
            return _priorityNames.Text;
        }

        private void SaveClick(object sender, ButtonBase e)
        {
            if (!PathPlayfieldCheck())
                return;

            if (string.IsNullOrEmpty(_fileName.Text))
            {
                ZeroIn.Log.Warning("Please provide a file name");
                return;
            }

            var savePath = $"{CommonParameters.PluginDataPath}\\RoamPath\\{_fileName.Text}.json";

            if (!string.IsNullOrEmpty(_ignoredNames.Text))
                ZeroIn.RoamPath.Rules.IgnoredNames = _ignoredNames.Text.Split('\n').Select(x => x.Trim()).ToList();

            if (!string.IsNullOrEmpty(_priorityNames.Text))
                ZeroIn.RoamPath.Rules.PriorityNames = _priorityNames.Text.Split('\n').Select(x => x.Trim()).ToList();

            ZeroIn.RoamPath.SPath.Name = _fileName.Text;
            ZeroIn.RoamPath.SPath.PlayfieldId = Playfield.ModelIdentity.Instance;
            ZeroIn.RoamPath.Save(savePath);
            ZeroIn.Config.RoamPath = savePath;
            ZeroIn.Config.Save();
        }

        private void SetPlayfieldTextColor(bool isSameAsPlayer)
        {
            uint color = isSameAsPlayer ? (uint)0x0022FF22 : (uint)0x00FF2222;
            _playfieldText.SetColor(color);
        }

        private bool PathPlayfieldCheck()
        {
            var pfCheck = ZeroIn.RoamPath.SPath.PlayfieldId == Playfield.ModelIdentity.Instance;

            SetPlayfieldTextColor(pfCheck);

            if (!pfCheck)
                ZeroIn.Log.Information("Cannot edit path created for a different playfield.");

            return pfCheck;
        }

        private void AddPointClick(object sender, ButtonBase e)
        {
            if (!PathPlayfieldCheck())
                return;

            if (ZeroIn.RoamPath.SPath.Waypoints.Count < 2 && ZeroIn.RoamPath.SPath.IsLooping)
            {
                ZeroIn.RoamPath.SPath.ToggleLoop();
                _isLoopingText.Text = ZeroIn.RoamPath.SPath.IsLooping.ToString();
            }

            ZeroIn.RoamPath.SPath.AddPoint();
            _pointsText.Text = ZeroIn.RoamPath.SPath.Waypoints.Count.ToString();
        }

        private void RemovePointClick(object sender, ButtonBase e)
        {
            if (!PathPlayfieldCheck())
                return;

            ZeroIn.RoamPath.SPath.RemovePoint(); 
            _pointsText.Text = ZeroIn.RoamPath.SPath.Waypoints.Count.ToString();
        }

        private void PickupPointClick(object sender, ButtonBase e)
        {
            if (!PathPlayfieldCheck())
                return;

            ZeroIn.RoamPath.SPath.PickupPoint();
        }

        private void PlacePointClick(object sender, ButtonBase e)
        {
            if (!PathPlayfieldCheck())
                return;

            ZeroIn.RoamPath.SPath.PlacePoint();
        }

        private void SelectPointClick(object sender, ButtonBase e)
        {
            if (!PathPlayfieldCheck())
                return;

            ZeroIn.RoamPath.SPath.SelectPoint();
        }

        private void UnselectPointClick(object sender, ButtonBase e)
        {
            if (!PathPlayfieldCheck())
                return;

            ZeroIn.RoamPath.SPath.UnselectPoint();
        }

        private void RunPathClicked(object sender, ButtonBase e)
        {
            if (ZeroIn.RoamPath.SPath == null)
                return;

            if (!PathPlayfieldCheck())
                return;

            if (SMovementController.IsNavigating())
            {
                SMovementController.Halt();
                return;
            }

            if (!SMovementController.IsLoaded())
                SMovementController.Set();

            ZeroIn.SetPath(ZeroIn.RoamPath.SPath);
        }

        private void SplitPointClick(object sender, ButtonBase e)
        {
            if (!PathPlayfieldCheck())
                return;

            if (!ZeroIn.RoamPath.SPath.Split())
                return;

            _pointsText.Text = ZeroIn.RoamPath.SPath.Waypoints.Count.ToString();
        }

        private void ToggleLockClick(object sender, ButtonBase e)
        {
            if (!PathPlayfieldCheck())
                return;

            ZeroIn.RoamPath.SPath.ToggleLock();
            _isLockedText.Text = ZeroIn.RoamPath.SPath.IsLocked.ToString();
        }

        private void ToggleLoopClick(object sender, ButtonBase e)
        {
            if (!PathPlayfieldCheck())
                return;

            if (ZeroIn.RoamPath.SPath.Waypoints.Count < 2)
                return;

            ZeroIn.RoamPath.SPath.ToggleLoop();
            _isLoopingText.Text = ZeroIn.RoamPath.SPath.IsLooping.ToString();
        }

        private void ClearClick(object sender, ButtonBase e)
        {
            if (!PathPlayfieldCheck())
                return;

            ZeroIn.RoamPath.SPath.Clear();
            ZeroIn.RoamPath.SPath.IsLocked = false;
        }

        private void ReversePathClick(object sender, ButtonBase e)
        {
            if (!PathPlayfieldCheck())
                return;

            ZeroIn.RoamPath.SPath.Reverse();
            _isReversedText.Text = ZeroIn.RoamPath.SPath.IsReversed.ToString();
        }
    }
}