// DetectedCharacter.cs
using System;
using System.Globalization;
using System.Text;

namespace ZeroIn.Scanner
{
    /// <summary>
    /// Canonical DetectedCharacter used across ZeroIn.
    /// Implements helpers used by ScanMap and other modules.
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

        // Utility: update "previous" snapshot (call before updating positions)
        public void SnapshotPreviousPosition()
        {
            PrevPositionX = PositionX;
            PrevPositionY = PositionY;
            PrevPositionZ = PositionZ;
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
        /// CSV header used by ScanMap.
        /// </summary>
        public static string GetCsvHeader()
        {
            return "CharId,InstanceId,Name,TimesSpotted,FirstSeen,LastSeen,PosX,PosY,PosZ,PrevPosX,PrevPosY,PrevPosZ,Distance,Health";
        }

        /// <summary>
        /// Produce a CSV line for this character.
        /// </summary>
        public string ToCsv()
        {
            // Use invariant culture for decimal formatting
            var sb = new StringBuilder();
            sb.Append(CharId).Append(',');
            sb.Append(InstanceId).Append(',');
            sb.Append(EscapeCsv(Name)).Append(',');
            sb.Append(TimesSpotted).Append(',');
            sb.Append(FirstSeen == DateTime.MinValue ? "" : FirstSeen.ToString("o")).Append(',');
            sb.Append(LastSeen == DateTime.MinValue ? "" : LastSeen.ToString("o")).Append(',');
            sb.Append(PositionX.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(PositionY.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(PositionZ.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(PrevPositionX.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(PrevPositionY.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(PrevPositionZ.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(Distance.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append(Health);
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
