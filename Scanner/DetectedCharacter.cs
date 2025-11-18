// DetectedCharacter.cs
using System;
using System.Globalization;
using System.Text;

namespace ZeroIn.Scanner
{
    /// <summary>
    /// Canonical DetectedCharacter used across ZeroIn.
    /// Implements helpers used by ScanMap and other modules.
    /// Enhanced with robust AFK detection and map overlay data.
    /// </summary>
    public class DetectedCharacter
    {
        // ID / identity
        public uint CharId { get; set; }
        public int InstanceId
        {
            get => unchecked((int)CharId);
            set => CharId = unchecked((uint)value);
        }

        // Metadata
        public string Name { get; set; } = string.Empty;
        public int TimesSpotted { get; set; } = 0;
        public int PlayfieldId { get; set; } = 0;
        public string PlayfieldName { get; set; } = string.Empty;

        // Spatial (current)
        public float PositionX { get; set; } = 0f;
        public float PositionY { get; set; } = 0f;
        public float PositionZ { get; set; } = 0f;

        // Spatial (previous snapshot) - used to determine movement
        public float PrevPositionX { get; set; } = 0f;
        public float PrevPositionY { get; set; } = 0f;
        public float PrevPositionZ { get; set; } = 0f;

        // Computed or sourced
        public float Distance { get; set; } = 0f;
        public int Health { get; set; } = 0;

        // Timestamps
        public DateTime FirstSeen { get; set; } = DateTime.MinValue;
        public DateTime LastSeen { get; set; } = DateTime.MinValue;

        // AFK Detection tracking
        public float TotalDistanceMoved { get; set; } = 0f;
        public int StationaryCount { get; set; } = 0; // How many times we've seen them without movement
        public DateTime LastMovementTime { get; set; } = DateTime.MinValue;

        // Utility: update "previous" snapshot (call before updating positions)
        public void SnapshotPreviousPosition()
        {
            PrevPositionX = PositionX;
            PrevPositionY = PositionY;
            PrevPositionZ = PositionZ;
        }

        /// <summary>
        /// Update movement tracking when position changes.
        /// Call this after updating position to track movement statistics.
        /// </summary>
        public void UpdateMovementTracking(float threshold = 0.25f)
        {
            var dx = PositionX - PrevPositionX;
            var dy = PositionY - PrevPositionY;
            var dz = PositionZ - PrevPositionZ;
            var distSq = dx * dx + dy * dy + dz * dz;
            var dist = (float)Math.Sqrt(distSq);

            if (distSq >= (threshold * threshold))
            {
                // Player moved
                TotalDistanceMoved += dist;
                LastMovementTime = DateTime.UtcNow;
                StationaryCount = 0;
            }
            else
            {
                // Player didn't move
                StationaryCount++;
            }
        }

        /// <summary>
        /// Returns whether the character has moved since the previous snapshot.
        /// Uses a small epsilon threshold so tiny float noise is ignored.
        /// </summary>
        /// <param name="threshold">distance threshold to consider as movement (default 0.25)</param>
        public bool HasMoved(float threshold = 0.25f)
        {
            var dx = PositionX - PrevPositionX;
            var dy = PositionY - PrevPositionY;
            var dz = PositionZ - PrevPositionZ;
            var distSq = dx * dx + dy * dy + dz * dz;
            return distSq >= (threshold * threshold);
        }

        /// <summary>
        /// Determines if player is likely AFK based on STRICT criteria.
        /// RULE: If player moves AT ALL, they are immediately NOT AFK.
        /// A player is considered AFK ONLY if:
        /// - They have been observed multiple times (prevents false positives)
        /// - AND they haven't moved for the configured time threshold
        /// </summary>
        public bool IsLikelyAFK(int minStationaryCount = 5, double maxSecondsWithoutMovement = 60)
        {
            // STRICT RULE: If player moved recently, they're NOT AFK
            if (LastMovementTime != DateTime.MinValue)
            {
                var secondsSinceMovement = (DateTime.UtcNow - LastMovementTime).TotalSeconds;
                // If they moved within the threshold, definitely not AFK
                if (secondsSinceMovement < maxSecondsWithoutMovement)
                    return false;
            }

            // Must be observed multiple times to be considered AFK
            // This prevents marking someone AFK from a single sighting
            if (TimesSpotted < minStationaryCount)
                return false;

            // Criteria 1: Never observed any movement across multiple detections
            if (LastMovementTime == DateTime.MinValue)
                return true;

            // Criteria 2: Stationary for many consecutive observations
            if (StationaryCount >= minStationaryCount)
                return true;

            // Criteria 3: Haven't moved for the configured time threshold
            if (LastMovementTime != DateTime.MinValue)
            {
                var secondsSinceMovement = (DateTime.UtcNow - LastMovementTime).TotalSeconds;
                if (secondsSinceMovement >= maxSecondsWithoutMovement)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Get AFK confidence as a percentage (0-100).
        /// Higher confidence means more likely to be AFK.
        /// </summary>
        public int GetAFKConfidence()
        {
            int confidence = 0;

            // Factor 1: Stationary count (up to 30 points)
            confidence += Math.Min(StationaryCount * 6, 30);

            // Factor 2: Time without movement (up to 30 points)
            if (LastMovementTime != DateTime.MinValue)
            {
                var secondsSinceMovement = (DateTime.UtcNow - LastMovementTime).TotalSeconds;
                confidence += (int)Math.Min(secondsSinceMovement / 2, 30);
            }
            else if (TimesSpotted > 3)
            {
                confidence += 30; // Never seen moving and spotted multiple times
            }

            // Factor 3: Low total distance moved (up to 20 points)
            var observationDuration = Math.Max((LastSeen - FirstSeen).TotalSeconds, 1);
            var metersPerSecond = TotalDistanceMoved / (float)observationDuration;
            if (metersPerSecond < 0.1f) // Moving less than 0.1 m/s
                confidence += 20;

            // Factor 4: Multiple sightings (up to 20 points)
            confidence += Math.Min(TimesSpotted * 2, 20);

            return Math.Min(confidence, 100);
        }

        /// <summary>
        /// CSV header used by ScanMap.
        /// Enhanced with AFK detection and map overlay data.
        /// </summary>
        public static string GetCsvHeader()
        {
            return "CharId,InstanceId,Name,PlayfieldId,PlayfieldName,TimesSpotted,FirstSeen,LastSeen," +
                   "PosX,PosY,PosZ,Distance,Health," +
                   "TotalDistanceMoved,StationaryCount,LastMovementTime,IsAFK,AFKConfidence";
        }

        /// <summary>
        /// Produce a CSV line for this character.
        /// Enhanced with comprehensive data for map overlay visualization.
        /// </summary>
        public string ToCsv()
        {
            // Use invariant culture for decimal formatting
            var sb = new StringBuilder();
            sb.Append(CharId).Append(',');
            sb.Append(InstanceId).Append(',');
            sb.Append(EscapeCsv(Name)).Append(',');
            sb.Append(PlayfieldId).Append(',');
            sb.Append(EscapeCsv(PlayfieldName)).Append(',');
            sb.Append(TimesSpotted).Append(',');
            sb.Append(FirstSeen == DateTime.MinValue ? "" : FirstSeen.ToString("o")).Append(',');
            sb.Append(LastSeen == DateTime.MinValue ? "" : LastSeen.ToString("o")).Append(',');
            sb.Append(PositionX.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(PositionY.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(PositionZ.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(Distance.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(Health).Append(',');
            sb.Append(TotalDistanceMoved.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(StationaryCount).Append(',');
            sb.Append(LastMovementTime == DateTime.MinValue ? "" : LastMovementTime.ToString("o")).Append(',');
            sb.Append(IsLikelyAFK()).Append(',');
            sb.Append(GetAFKConfidence());
            return sb.ToString();
        }

        private static string EscapeCsv(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
            {
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }
            return s;
        }

        public override string ToString()
        {
            var age = LastSeen == DateTime.MinValue ? "n/a" : $"{(DateTime.UtcNow - LastSeen).TotalSeconds:0.0}s";
            return $"{Name} ({CharId}) last={age} hp={Health} spotted={TimesSpotted} dist={Distance:0.0}";
        }
    }
}
