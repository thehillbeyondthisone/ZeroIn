using AOSharp.Common.GameData.UI;
using AOSharp.Core.UI;
using System;

namespace ZeroIn
{
    public class RoamPathView
    {
        protected View Root;
        public RoamPathWindow Parent;

        public RoamPathView(string viewPath, RoamPathWindow parent)
        {
            Root = View.CreateFromXml(viewPath);
            Parent = parent;
            Parent.AddChild(Root);
        }

        public void Dispose()
        {
            Parent.RemoveChild(Root);   
        }
    }
}