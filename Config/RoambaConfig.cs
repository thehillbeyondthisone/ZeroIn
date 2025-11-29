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

        // Detector settings (Roamba path vs Omega radar)
        public bool EnableRoambaDetector = true; // Path-based scanning (original ZeroIn method)
        public bool EnableOmegaDetector = false; // Omega-style proximity radar (OFF by default)
        public float OmegaDetectionRange = 50f; // Omega radar detection range in meters
        public float OmegaScanFrequency = 5f; // Omega radar scan frequency in Hz (5 scans per second)

        // Visual radar settings
        public float AFKMarkerSize = 5f; // AFK players get larger markers
        public float ActiveMarkerSize = 2f; // Active players get smaller markers
        public string PlayerMarkerShape = "cross"; // cross, circle, square, diamond
        public bool ShowDetectionRadius = true; // Show circular detection radius
        public bool ShowPlayerMarkers = true; // Show player position markers
        public bool ShowAFKPaths = true; // Show paths to AFK players
        public bool ShowActivePlayerPaths = true; // Show paths to active (non-AFK) players
        public bool ShowMarkerLines = false; // If true, show line shapes (cross/diamond/etc). If false, show minimal waypoint markers (DEFAULT)

        // Debug settings
        public bool VerboseDebug = false; // Enable verbose debug output to console

        // Combat targeting settings (simple version for ZeroIn)
        public bool HostileMobsOnly = true; // Only target hostile/aggressive mobs
        public int MinMobLevel = 1;
        public int MaxMobLevel = 220;
        public System.Collections.Generic.List<string> MobBlacklist = new System.Collections.Generic.List<string>(); // Mob names to never attack

        public ZeroInConfig()
        {
            CoreConfig = new BuddyCoreConfig();
            PathingConfig = new PathConfig();
            RoamPath = "";
            WindowCoords = Vector2.Zero;
        }
    }
}
