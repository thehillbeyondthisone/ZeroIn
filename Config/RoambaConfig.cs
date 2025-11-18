using AOSharp.Common.GameData;
using Newtonsoft.Json;
using Shared;

namespace ZeroIn
{
    public class ZeroInConfig : BuddyBaseConfig<ZeroInConfig>
    {
        [JsonIgnore]
        public override string FileName => "ZeroInConfig";

        [JsonIgnore]
        public override ZeroInConfig LoadDefaults => new ZeroInConfig();

        [JsonIgnore]
        public string RoamPathFolder;

        public BuddyCoreConfig CoreConfig;

        public PathConfig PathingConfig;

        public string RoamPath;

        public Vector2 WindowCoords;

        public ZeroInConfig()
        {
            CoreConfig = new BuddyCoreConfig();
            PathingConfig = new PathConfig();
            RoamPath = "";
            WindowCoords = Vector2.Zero;
        }
    }
}
