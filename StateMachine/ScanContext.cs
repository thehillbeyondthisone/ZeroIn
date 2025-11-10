using System.Collections.Generic;
using ZeroIn.Config;
using ZeroIn.GridPattern;
using ZeroIn.Scanner;

namespace ZeroIn.StateMachine
{
    /// <summary>
    /// Context for the scanning state machine
    /// </summary>
    public class ScanContext
    {
        public ZeroInConfig Config { get; set; }
        public CharacterScanner Scanner { get; set; }
        public ScanMap Map { get; set; }
        public List<GridWaypoint> Waypoints { get; set; }
        public int CurrentWaypointIndex { get; set; }
        public bool ScanComplete { get; set; }
        public bool IsScanning { get; set; }

        public ScanContext(ZeroInConfig config)
        {
            Config = config;
            Scanner = new CharacterScanner(config);
            Map = new ScanMap(config);
            Waypoints = new List<GridWaypoint>();
            CurrentWaypointIndex = 0;
            ScanComplete = false;
            IsScanning = false;
        }

        /// <summary>
        /// Resets the scan context for a new scan
        /// </summary>
        public void Reset()
        {
            CurrentWaypointIndex = 0;
            ScanComplete = false;
            IsScanning = false;
            Scanner.Clear();
        }
    }
}
