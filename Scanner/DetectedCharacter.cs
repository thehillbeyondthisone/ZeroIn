using AOSharp.Common.GameData;
using System;

namespace ZeroIn.Scanner
{
    /// <summary>
    /// Represents a detected character during scanning
    /// </summary>
    public class DetectedCharacter
    {
        public string Name { get; set; }
        public uint CharId { get; set; }
        public Vector3 Position { get; set; }
        public Profession Profession { get; set; }
        public Breed Breed { get; set; }
        public int Level { get; set; }
        public Side Faction { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public Vector3 LastPosition { get; set; }
        public bool HasMoved { get; set; }
        public int TimesSpotted { get; set; }

        public DetectedCharacter()
        {
            FirstSeen = DateTime.Now;
            LastSeen = DateTime.Now;
            LastPosition = Vector3.Zero;
            HasMoved = false;
            TimesSpotted = 1;
        }

        /// <summary>
        /// Updates the character's position and checks for movement
        /// </summary>
        public void Update(Vector3 newPosition)
        {
            LastSeen = DateTime.Now;
            TimesSpotted++;

            // Check if character has moved (with small tolerance for position jitter)
            if (LastPosition != Vector3.Zero)
            {
                float distance = Vector3.Distance(LastPosition, newPosition);
                if (distance > 2f) // More than 2m = has moved
                {
                    HasMoved = true;
                }
            }

            LastPosition = newPosition;
            Position = newPosition;
        }

        /// <summary>
        /// Gets time elapsed since first detection
        /// </summary>
        public TimeSpan TimeObserved()
        {
            return LastSeen - FirstSeen;
        }

        /// <summary>
        /// Checks if character appears to be AFK
        /// </summary>
        public bool IsLikelyAFK(int afkCheckTimeSeconds)
        {
            return !HasMoved && TimeObserved().TotalSeconds >= afkCheckTimeSeconds;
        }

        public override string ToString()
        {
            string status = HasMoved ? "Moving" : "Stationary";
            return $"{Name} (L{Level} {Profession} {Breed}) - {status} at ({Position.X:F1}, {Position.Y:F1}, {Position.Z:F1})";
        }

        /// <summary>
        /// Gets CSV format output
        /// </summary>
        public string ToCsv()
        {
            return $"{Name},{CharId},{Level},{Profession},{Breed},{Faction}," +
                   $"{Position.X:F2},{Position.Y:F2},{Position.Z:F2}," +
                   $"{FirstSeen:yyyy-MM-dd HH:mm:ss},{LastSeen:yyyy-MM-dd HH:mm:ss}," +
                   $"{HasMoved},{TimesSpotted}";
        }

        /// <summary>
        /// Gets CSV header
        /// </summary>
        public static string GetCsvHeader()
        {
            return "Name,CharId,Level,Profession,Breed,Faction," +
                   "X,Y,Z,FirstSeen,LastSeen,HasMoved,TimesSpotted";
        }
    }
}
