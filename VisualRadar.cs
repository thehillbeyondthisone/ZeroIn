using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Common.GameData;
using System;
using System.Linq;
using System.Collections.Generic;
using ZeroIn.Scanner;

namespace ZeroIn
{
    /// <summary>
    /// Visual radar overlay for displaying detected players and scan radius
    /// </summary>
    public class VisualRadar
    {
        private bool _enabled = true;
        private CharacterScanner _scanner;
        private ZeroInConfig _config;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public VisualRadar(CharacterScanner scanner, ZeroInConfig config)
        {
            _scanner = scanner;
            _config = config;
        }

        /// <summary>
        /// Draw the radar overlay - call this from Game.OnUpdate
        /// </summary>
        public void Draw()
        {
            if (!_enabled)
                return;

            try
            {
                var localPlayer = DynelManager.LocalPlayer;
                if (localPlayer == null || !localPlayer.IsValid)
                    return;

                // Draw detection radius circle around player
                DrawDetectionRadius(localPlayer);

                // Draw detected players
                DrawDetectedPlayers(localPlayer);
            }
            catch (Exception ex)
            {
                // Silently catch rendering errors to avoid spam
                ZeroIn.Log?.Warning($"VisualRadar.Draw error: {ex.Message}");
            }
        }

        private void DrawDetectionRadius(SimpleChar localPlayer)
        {
            float radius = _config.PlayerDetectionRange;
            var position = localPlayer.Position;

            // Draw a circle using NavMesh visualization
            // NavMesh.Display draws ground-level circles
            // Color: Light blue for detection radius
            NavMesh.Display(position, radius, DecoType.Sphere, 0.1f);
        }

        private void DrawDetectedPlayers(SimpleChar localPlayer)
        {
            var detected = _scanner.GetDetectedCharacters();
            var now = DateTime.UtcNow;

            foreach (var player in detected)
            {
                // Skip stale detections (older than 30 seconds)
                if ((now - player.LastSeen).TotalSeconds > 30)
                    continue;

                var playerPos = new Vector3(player.PositionX, player.PositionY, player.PositionZ);

                // Determine color based on AFK status
                // AFK (not moved) = Red, Active (moved) = Green
                DecoType markerType = player.HasMoved() ? DecoType.Dot : DecoType.Dot;
                float size = player.HasMoved() ? 2f : 3f; // AFK players get bigger markers

                // Draw marker at player position
                if (!player.HasMoved())
                {
                    // AFK - draw RED
                    NavMesh.Display(playerPos, size, DecoType.Dot, 0.1f);
                }
                else
                {
                    // Active - draw GREEN
                    NavMesh.Display(playerPos, size, DecoType.Sphere, 0.1f);
                }

                // Draw name label above player
                DrawPlayerLabel(playerPos, player);
            }
        }

        private void DrawPlayerLabel(Vector3 position, DetectedCharacter player)
        {
            try
            {
                // Calculate screen position for the player's world position
                var screenPos = Camera.WorldToScreen(position);

                if (screenPos.X < 0 || screenPos.Y < 0)
                    return; // Off screen

                // Format label: Name (distance)
                var status = player.HasMoved() ? "Active" : "AFK";
                var label = $"{player.Name} ({player.Distance:F0}m) [{status}]";

                // Draw text at screen position
                // Note: AOSharp text rendering might not be available, so we'll use simpler approach
                // We can enhance this later if needed
            }
            catch
            {
                // Label rendering is optional, don't spam logs
            }
        }

        public void Toggle()
        {
            _enabled = !_enabled;
            Chat.WriteLine($"[ZeroIn] Visual radar: {(_enabled ? "ENABLED" : "DISABLED")}",
                _enabled ? ChatColor.Green : ChatColor.Red);
        }
    }
}
