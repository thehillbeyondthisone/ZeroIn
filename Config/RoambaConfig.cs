using AOSharp.Common.GameData;
using Newtonsoft.Json;
using Shared;
using Buddy.Shared.UI;

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

        // ZeroIn Scanner properties
        public float ScanSpacing = 20f;
        public float PlayerDetectionRange = 100f;
        public bool ContinuousScanning = false;
        public bool OnlyAFK = false;
        public int AFKCheckTimeSeconds = 30;
        public string OutputFolder = "";
        public bool LogToConsole = true;
        public bool SaveToJson = true;
        public bool SaveToCsv = false;
        public bool IgnoreSelf = true;
        public System.Collections.Generic.List<string> IgnoreNames = new System.Collections.Generic.List<string>();
        public int ScannerStaleSeconds = 10;

        // Combat settings
        public bool EnableCombat = false;
        public bool EnableLooting = false;
        public bool EnableHealthCheck = false;

        // Visual radar settings
        public float AFKMarkerSize = 5f; // AFK players get larger markers
        public float ActiveMarkerSize = 2f; // Active players get smaller markers
        public string PlayerMarkerShape = "cross"; // cross, circle, square, diamond

        public ZeroInConfig()
        {
            CoreConfig = new BuddyCoreConfig();
            PathingConfig = new PathConfig();
            RoamPath = "";
            WindowCoords = Vector2.Zero;
        }
    }
}
