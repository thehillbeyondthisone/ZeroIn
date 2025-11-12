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

            // Start at first waypoint
            context.CurrentWaypointIndex = 0;
            MoveToNextWaypoint();
        }

        public void OnExit()
        {
            Console.WriteLine("[ZeroIn] Scan stopped");

            var context = _stateMachine.Context;

            // Stop movement (SMovementController handles navigation internally)

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

            // Check if we've reached current waypoint
            if (!SMovementController.IsNavigating())
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
                        MoveToNextWaypoint();
                    }
                    else
                    {
                        Console.WriteLine("[ZeroIn] Scan complete!");
                        Console.WriteLine($"[ZeroIn] {context.Scanner.GetSummary()}");
                        _stateMachine.TransitionTo(ScanStateMachine.State.Idle);
                    }
                }
                else
                {
                    MoveToNextWaypoint();
                }
            }
        }

        private void MoveToNextWaypoint()
        {
            var context = _stateMachine.Context;

            if (context.CurrentWaypointIndex >= context.Waypoints.Count)
                return;

            var waypoint = context.Waypoints[context.CurrentWaypointIndex];

            // Log progress every 10 waypoints
            if (context.CurrentWaypointIndex % 10 == 0)
            {
                int progress = (int)((float)context.CurrentWaypointIndex / context.Waypoints.Count * 100);
                Console.WriteLine($"[ZeroIn] Progress: {progress}% ({context.CurrentWaypointIndex}/{context.Waypoints.Count})");
            }

            // Navigate to waypoint
            SMovementController.SetDestination(waypoint.Position);
        }
    }
}
