using AOSharp.Common.GameData;
using Newtonsoft.Json;
using Shared;

namespace AutomatonRoamba
{
    public class AutomatonRoambaConfig : BuddyBaseConfig<AutomatonRoambaConfig>
    {
        [JsonIgnore]
        public override string FileName => "AutomatonRoambaConfig";

        [JsonIgnore]
        public override AutomatonRoambaConfig LoadDefaults => new AutomatonRoambaConfig();

        [JsonIgnore]
        public string RoamPathFolder;

        public BuddyCoreConfig CoreConfig;

        public PathConfig PathingConfig;

        public string RoamPath;

        public Vector2 WindowCoords;

        public AutomatonRoambaConfig()
        {
            CoreConfig = new BuddyCoreConfig();
            PathingConfig = new PathConfig();
            RoamPath = "";
            WindowCoords = Vector2.Zero;
        }
    }
}
