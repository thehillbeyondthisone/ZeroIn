// CharacterScanner.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ZeroIn.Scanner
{
    /// <summary>
    /// CharacterScanner that maintains TimesSpotted and previous position snapshots.
    /// Remains AOSharp-free � call OnCharacterSeen from AOSharp-aware adapter.
    /// </summary>
    public class CharacterScanner
    {
        private readonly Dictionary<uint, DetectedCharacter> _tracked = new Dictionary<uint, DetectedCharacter>();
        private readonly ZeroInConfig _config;
        private TimeSpan _staleAfter;

        public CharacterScanner(ZeroInConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            int staleSeconds = 10;

            // reflective fallback: try to read config.ScannerStaleSeconds if it exists
            try
            {
                var prop = _config.GetType().GetProperty("ScannerStaleSeconds", BindingFlags.Public | BindingFlags.Instance);
                if (prop != null)
                {
                    var val = prop.GetValue(_config);
                    if (val is int i && i >= 1) staleSeconds = i;
                    else if (val is long l && l >= 1) staleSeconds = (int)l;
                }
            }
            catch
            {
                // ignore and use default
            }

            _staleAfter = TimeSpan.FromSeconds(Math.Max(1, staleSeconds));
        }

        public void Clear() => _tracked.Clear();

        public void Scan() => PurgeStale();

        public List<DetectedCharacter> GetDetectedCharacters()
        {
            return _tracked.Values.Select(Clone).ToList();
        }

        public List<DetectedCharacter> GetAFKCharacters(int minStationaryCount = 5, double maxSecondsWithoutMovement = 60)
        {
            return _tracked.Values.Where(c => c.IsLikelyAFK(minStationaryCount, maxSecondsWithoutMovement)).Select(Clone).ToList();
        }

        public string GetSummary()
        {
            var now = DateTime.UtcNow;
            var sb = new StringBuilder();
            sb.AppendLine($"Tracked: {_tracked.Count}");
            foreach (var c in _tracked.Values.OrderByDescending(x => x.LastSeen).Take(6))
            {
                var age = (now - c.LastSeen).TotalSeconds;
                sb.AppendLine($"{c.Name} ({c.CharId}) lastSeen={age:0.0}s hp={c.Health} dist={c.Distance:0.0} spotted={c.TimesSpotted}");
            }

            return sb.ToString();
        }

        public int Count => _tracked.Count;

        /// <summary>
        /// Adapter call � provide primitive values from AOSharp-aware code.
        /// This method snapshots previous position, increments TimesSpotted, and updates fields.
        /// </summary>
        public void OnCharacterSeen(int instanceId, string name, float posX, float posY, float posZ, int health = 0, float distance = 0f, int playfieldId = 0, string playfieldName = null, string side = "Neutral")
        {
            uint id = unchecked((uint)instanceId);

            if (_tracked.TryGetValue(id, out var existing))
            {
                // record previous pos so HasMoved can work
                existing.SnapshotPreviousPosition();

                // update fields
                existing.TimesSpotted++;
                existing.Name = name ?? existing.Name;
                existing.Side = side ?? existing.Side;
                existing.PositionX = posX;
                existing.PositionY = posY;
                existing.PositionZ = posZ;
                existing.Distance = distance;
                existing.Health = health;
                existing.LastSeen = DateTime.UtcNow;

                // Update playfield info if provided
                if (playfieldId != 0)
                    existing.PlayfieldId = playfieldId;
                if (!string.IsNullOrEmpty(playfieldName))
                    existing.PlayfieldName = playfieldName;

                // Track movement for AFK detection
                existing.UpdateMovementTracking();

                _tracked[id] = existing;
            }
            else
            {
                var d = new DetectedCharacter
                {
                    CharId = id,
                    Name = name ?? string.Empty,
                    Side = side ?? "Neutral",
                    PositionX = posX,
                    PositionY = posY,
                    PositionZ = posZ,
                    PrevPositionX = posX,
                    PrevPositionY = posY,
                    PrevPositionZ = posZ,
                    Distance = distance,
                    Health = health,
                    TimesSpotted = 1,
                    FirstSeen = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow,
                    PlayfieldId = playfieldId,
                    PlayfieldName = playfieldName ?? string.Empty
                };

                _tracked.Add(id, d);
            }
        }

        public bool Remove(int instanceId) => _tracked.Remove(unchecked((uint)instanceId));

        private void PurgeStale()
        {
            var now = DateTime.UtcNow;
            var stale = _tracked.Where(kvp => (now - kvp.Value.LastSeen) > _staleAfter)
                                .Select(kvp => kvp.Key)
                                .ToList();
            foreach (var k in stale) _tracked.Remove(k);
        }

        private static DetectedCharacter Clone(DetectedCharacter src)
        {
            return new DetectedCharacter
            {
                CharId = src.CharId,
                Name = src.Name,
                Side = src.Side,
                TimesSpotted = src.TimesSpotted,
                PlayfieldId = src.PlayfieldId,
                PlayfieldName = src.PlayfieldName,
                PositionX = src.PositionX,
                PositionY = src.PositionY,
                PositionZ = src.PositionZ,
                PrevPositionX = src.PrevPositionX,
                PrevPositionY = src.PrevPositionY,
                PrevPositionZ = src.PrevPositionZ,
                Distance = src.Distance,
                Health = src.Health,
                FirstSeen = src.FirstSeen,
                LastSeen = src.LastSeen,
                TotalDistanceMoved = src.TotalDistanceMoved,
                StationaryCount = src.StationaryCount,
                LastMovementTime = src.LastMovementTime
            };
        }
    }
}
