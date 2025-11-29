using System;
using System.Collections.Generic;
using AOSharp.Core;
using AOSharp.Common.GameData;

namespace ZeroIn.Scanner
{
    /// <summary>
    /// Path-based player detection (original ZeroIn/Roamba method).
    /// Detects players while following a configured path with waypoints.
    /// </summary>
    public class RoambaPathDetector : IPlayerDetector
    {
        private readonly ZeroInConfig _config;

        public string DetectorName => "Roamba";
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Detection range in meters
        /// </summary>
        public float DetectionRange => _config?.PlayerDetectionRange ?? 50f;

        public RoambaPathDetector(ZeroInConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            IsEnabled = false; // Must be explicitly enabled
        }

        public void Update(float deltaTime)
        {
            // Path-based detection is passive - scanning happens during movement
            // No per-frame updates needed
        }

        public List<DetectedPlayerInfo> Scan()
        {
            var results = new List<DetectedPlayerInfo>();
            if (!IsEnabled) return results;

            try
            {
                var me = DynelManager.LocalPlayer;
                if (me == null || !me.IsValid) return results;

                var myPos = me.Position;
                var playfieldId = (int)Playfield.ModelIdentity.Instance;
                var playfieldName = Playfield.Name;
                var detectionRange = DetectionRange;

                // Scan all characters in range
                foreach (var character in DynelManager.Characters)
                {
                    if (character == null || !character.IsValid) continue;
                    if (character.Identity == me.Identity) continue; // Skip self

                    try
                    {
                        // Check if player (not NPC)
                        if (!SafeIsPlayer(character))
                            continue;

                        var distance = Vector3.Distance(myPos, character.Position);

                        // Check if within detection range
                        if (distance > detectionRange)
                            continue;

                        var info = new DetectedPlayerInfo
                        {
                            InstanceId = character.Identity.Instance,
                            Name = character.Name ?? "Unknown",
                            PositionX = character.Position.X,
                            PositionY = character.Position.Y,
                            PositionZ = character.Position.Z,
                            Distance = distance,
                            Health = character.Health,
                            PlayfieldId = playfieldId,
                            PlayfieldName = playfieldName,
                            DetectorSource = DetectorName
                        };

                        results.Add(info);
                    }
                    catch (Exception ex)
                    {
                        ZeroIn.Log?.Warning($"RoambaPath: Error scanning character: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                ZeroIn.Log?.Error($"RoambaPath: Scan failed: {ex}");
            }

            return results;
        }

        private static bool SafeIsPlayer(SimpleChar character)
        {
            try
            {
                return character.IsPlayer;
            }
            catch
            {
                try
                {
                    return !character.IsNpc;
                }
                catch
                {
                    // If we can't determine, assume it's a player to be safe
                    return true;
                }
            }
        }
    }
}
