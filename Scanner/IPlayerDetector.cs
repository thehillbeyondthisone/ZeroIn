using System.Collections.Generic;

namespace ZeroIn.Scanner
{
    /// <summary>
    /// Interface for player detection systems.
    /// Allows multiple detection sources (Roamba path scanning, Omega radar, etc.)
    /// </summary>
    public interface IPlayerDetector
    {
        /// <summary>
        /// Unique identifier for this detector (e.g., "Roamba", "Omega")
        /// </summary>
        string DetectorName { get; }

        /// <summary>
        /// Whether this detector is currently enabled and running
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Scan for players and return detected player data
        /// </summary>
        List<DetectedPlayerInfo> Scan();

        /// <summary>
        /// Update/tick method called each frame
        /// </summary>
        void Update(float deltaTime);
    }

    /// <summary>
    /// Player information from a detector
    /// </summary>
    public class DetectedPlayerInfo
    {
        public int InstanceId { get; set; }
        public string Name { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public float Distance { get; set; }
        public int Health { get; set; }
        public int PlayfieldId { get; set; }
        public string PlayfieldName { get; set; }
        public string DetectorSource { get; set; }
    }
}
