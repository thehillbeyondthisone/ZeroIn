using AOSharp.Core;
using AOSharp.Core.Movement;
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
        private bool _pathStarted;

        public ScanningState(ScanStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
            _pathStarted = false;
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

            // Convert to SPath
            context.CurrentPath = gridGenerator.GenerateSPath(context.Waypoints);

            // Estimate time
            float estimatedTime = gridGenerator.EstimateTime(context.Waypoints);
            Console.WriteLine($"[ZeroIn] Estimated scan time: {TimeSpan.FromSeconds(estimatedTime):mm\\:ss}");

            // Start movement
            _pathStarted = false;
        }

        public void OnExit()
        {
            Console.WriteLine("[ZeroIn] Scan stopped");

            var context = _stateMachine.Context;

            // Stop movement
            MovementController.Instance.Stop();

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

            // Start path on first tick
            if (!_pathStarted)
            {
                Console.WriteLine("[ZeroIn] Beginning grid movement...");
                SMovementController.Set(context.CurrentPath);
                _pathStarted = true;
            }

            // Scan for characters
            context.Scanner.Scan();

            // Check if we've reached the end
            if (MovementController.Instance.IsNavigating == false)
            {
                // Check if scan is complete or if we should loop
                if (context.Config.ContinuousScanning)
                {
                    Console.WriteLine("[ZeroIn] Restarting scan loop...");
                    _pathStarted = false;
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
}
