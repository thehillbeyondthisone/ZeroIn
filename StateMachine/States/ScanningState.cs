using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Pathfinding;
using System;
using ZeroIn.Config;
using ZeroIn.GridPattern;

namespace ZeroIn.StateMachine.States
{
    /// <summary>
    /// Scanning state - actively moving through grid and detecting characters
    /// </summary>
    public class ScanningState
    {
        private ScanStateMachine _stateMachine;
        private const float WAYPOINT_REACH_DISTANCE = 5f; // Consider waypoint reached within 5m
        private SPath _currentPath; // The path the character is following

        public ScanningState(ScanStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public void OnEnter()
        {
            Console.WriteLine("[ZeroIn] Starting scan...");

            var context = _stateMachine.Context;
            context.IsScanning = true;
            context.Reset();

            // Get current area
            ZoneArea area = context.Config.GetCurrentArea();
            if (area == null || !area.IsValid())
            {
                Console.WriteLine("[ZeroIn] Error: No valid area configured");
                _stateMachine.TransitionTo(ScanStateMachine.State.Idle);
                return;
            }

            // Generate grid pattern
            var gridGenerator = new GridGenerator(area, context.Config.ScanSpacing);
            context.Waypoints = gridGenerator.GeneratePattern();

            if (context.Waypoints.Count == 0)
            {
                Console.WriteLine("[ZeroIn] Error: Failed to generate grid pattern");
                _stateMachine.TransitionTo(ScanStateMachine.State.Idle);
                return;
            }

            // Estimate time
            float estimatedTime = gridGenerator.EstimateTime(context.Waypoints);
            Console.WriteLine($"[ZeroIn] Estimated scan time: {TimeSpan.FromSeconds(estimatedTime):mm\\:ss}");

            // Create SPath with all waypoints
            _currentPath = SPath.Create();
            foreach (var waypoint in context.Waypoints)
            {
                _currentPath.Waypoints.Add(waypoint.Position);
            }

            // Start at first waypoint
            context.CurrentWaypointIndex = 0;
            StartMovingAlongPath();
        }

        public void OnExit()
        {
            Console.WriteLine("[ZeroIn] Scan stopped");

            var context = _stateMachine.Context;

            // Stop movement
            SMovementController.StopFollow();

            // Save and display results
            var characters = context.Scanner.GetDetectedCharacters();

            if (context.Config.OnlyAFK)
            {
                characters = context.Scanner.GetAFKCharacters();
            }

            if (characters.Count > 0)
            {
                ZoneArea area = context.Config.GetCurrentArea();
                context.Map.SaveResults(characters, area?.Name ?? "Unknown");

                if (context.Config.LogToConsole)
                {
                    context.Map.PrintResults(characters);
                }
            }

            context.IsScanning = false;
            context.ScanComplete = true;
        }

        public void Tick()
        {
            var context = _stateMachine.Context;

            // Scan for characters
            context.Scanner.Scan();

            // Detect nearby players
            ScanForNearbyPlayers(context);

            // Check if we've reached current waypoint by checking if we're close enough
            bool atWaypoint = false;
            try
            {
                var waypoint = context.Waypoints[context.CurrentWaypointIndex];
                float distance = Vector3.Distance(DynelManager.LocalPlayer.Position, waypoint.Position);
                atWaypoint = distance <= WAYPOINT_REACH_DISTANCE;
            }
            catch { atWaypoint = true; } // If error checking position, assume we're there to prevent stalling

            if (atWaypoint)
            {
                // Move to next waypoint
                context.CurrentWaypointIndex++;

                if (context.CurrentWaypointIndex >= context.Waypoints.Count)
                {
                    // Finished all waypoints
                    if (context.Config.ContinuousScanning)
                    {
                        Console.WriteLine("[ZeroIn] Restarting scan loop...");
                        context.CurrentWaypointIndex = 0;
                        StartMovingAlongPath();
                    }
                    else
                    {
                        Console.WriteLine("[ZeroIn] Scan complete!");
                        Console.WriteLine($"[ZeroIn] {context.Scanner.GetSummary()}");
                        _stateMachine.TransitionTo(ScanStateMachine.State.Idle);
                    }
                }
            }
        }

        private void ScanForNearbyPlayers(ScanContext context)
        {
            try
            {
                var localPlayer = DynelManager.LocalPlayer;
                if (localPlayer == null)
                    return;

                var localPos = localPlayer.Position;
                float detectionRange = context.Config.PlayerDetectionRange;

                // Get all nearby players
                foreach (var dynel in DynelManager.Players)
                {
                    if (dynel == null || !dynel.IsValid)
                        continue;

                    // Skip self if configured
                    if (context.Config.IgnoreSelf && dynel.Identity == localPlayer.Identity)
                        continue;

                    // Skip if in ignore list
                    if (context.Config.IgnoreNames.Contains(dynel.Name))
                        continue;

                    // Calculate distance
                    float distance = Vector3.Distance(localPos, dynel.Position);

                    // Only track if within detection range
                    if (distance <= detectionRange)
                    {
                        context.Scanner.OnCharacterSeen(
                            (int)dynel.Identity.Instance,
                            dynel.Name,
                            dynel.Position.X,
                            dynel.Position.Y,
                            dynel.Position.Z,
                            dynel.Health,
                            distance
                        );

                        // Log to console if configured
                        if (context.Config.LogToConsole)
                        {
                            var detectedChars = context.Scanner.GetDetectedCharacters();
                            var detectedChar = detectedChars.Find(c => c.CharId == dynel.Identity.Instance);
                            if (detectedChar != null && detectedChar.TimesSpotted == 1)
                            {
                                Console.WriteLine($"[ZeroIn] Detected: {dynel.Name} at ({dynel.Position.X:F1}, {dynel.Position.Y:F1}, {dynel.Position.Z:F1}) - {distance:F1}m");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZeroIn] Error scanning for players: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts the character moving along the planned path
        /// </summary>
        private void StartMovingAlongPath()
        {
            var context = _stateMachine.Context;

            if (_currentPath == null || _currentPath.Waypoints.Count == 0)
            {
                Console.WriteLine("[ZeroIn] Error: No path to follow!");
                _stateMachine.TransitionTo(ScanStateMachine.State.Idle);
                return;
            }

            // Log progress
            int progress = (int)((float)context.CurrentWaypointIndex / context.Waypoints.Count * 100);
            Console.WriteLine($"[ZeroIn] Progress: {progress}% ({context.CurrentWaypointIndex}/{context.Waypoints.Count})");

            // Use SMovementController to follow the path (like Roamba does)
            // The second parameter 'true' means loop the path
            SMovementController.SetPath(_currentPath, false);

            Console.WriteLine("[ZeroIn] Character now following roomba path!");
        }
    }
}
