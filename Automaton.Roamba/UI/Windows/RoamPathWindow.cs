using AOSharp.Common.GameData.UI;
using AOSharp.Core.UI;
using System;

namespace AutomatonRoamba
{
    public class RoamPathWindow : AOSharpWindow
    {
        private RoamPathInitView _initView;
        private RoamPathMainView _mainView;
        private View _root;

        private string _initViewPath;
        private string _mainViewPath;

        public RoamPathWindow(string windowName, string windowPath, string initViewPath, string mainViewPath, WindowStyle windowStyle = WindowStyle.Default, WindowFlags flags = WindowFlags.AutoScale | WindowFlags.NoFade) : base(windowName, windowPath, windowStyle, flags)
        {
            _initViewPath = initViewPath;
            _mainViewPath = mainViewPath;
        }

        protected override void OnWindowCreating()
        {
            try
            {
                if (Window.FindView("Root", out _root))
                    LoadInitView();
            }
            catch (Exception ex)
            {
                AutomatonRoamba.Log.Warning(ex.Message);
            }
        }

        public void LoadInitView()
        {
            _initView = new RoamPathInitView(_initViewPath, this);
        }

        public void LoadMainView()
        {
            _mainView = new RoamPathMainView(_mainViewPath, this);
        }

        public void AddChild(View view)
        {
            _root.AddChild(view, true);
        }

        public void RemoveChild(View view)
        {
            _root.RemoveChild(view);
        }
    }
}