using AOSharp.Common.GameData;
using AOSharp.Core;
using System;
using System.Reflection;
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

            // Try to stop movement via reflection (supports multiple AOSharp versions)
            TryStopMovement();

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

            // Navigate to waypoint using reflection so code compiles against multiple SDK versions
            try
            {
                var mcType = Type.GetType("AOSharp.Core.Movement.MovementController, AOSharp.Core");
                if (mcType != null)
                {
                    // get static Instance property if present
                    var instProp = mcType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    var instance = instProp?.GetValue(null);

                    if (instance != null)
                    {
                        // Try common method names
                        var navMethod = mcType.GetMethod("NavigateTo", new Type[] { typeof(Vector3) });
                        if (navMethod != null)
                        {
                            navMethod.Invoke(instance, new object[] { waypoint.Position });
                            return;
                        }

                        var setMethod = mcType.GetMethod("SetMovement");
                        if (setMethod != null)
                        {
                            var parms = setMethod.GetParameters();
                            if (parms.Length == 1)
                            {
                                var pType = parms[0].ParameterType;

                                // If it accepts Vector3, pass it directly
                                if (pType == typeof(Vector3))
                                {
                                    setMethod.Invoke(instance, new object[] { waypoint.Position });
                                    return;
                                }

                                // If it expects a MovementAction, try to construct one
                                var maType = Type.GetType("AOSharp.Common.GameData.MovementAction, AOSharp.Common");
                                if (maType != null && pType.IsAssignableFrom(maType))
                                {
                                    var ma = Activator.CreateInstance(maType);
                                    // attempt to set fields/properties named X/Y/Z or Parameter1.. if present
                                    var fx = maType.GetField("X") ?? maType.GetField("Parameter1");
                                    var fy = maType.GetField("Y") ?? maType.GetField("Parameter2");
                                    var fz = maType.GetField("Z") ?? maType.GetField("Parameter3");
                                    try { fx?.SetValue(ma, waypoint.Position.X); } catch { }
                                    try { fy?.SetValue(ma, waypoint.Position.Y); } catch { }
                                    try { fz?.SetValue(ma, waypoint.Position.Z); } catch { }

                                    setMethod.Invoke(instance, new object[] { ma });
                                    return;
                                }
                            }
                        }
                    }
                }

                // Fallback: try AOSharpSDK.Nav NavmeshMovementController
                var navType = Type.GetType("NavmeshMovementController, NavmeshMovementController");
                if (navType != null)
                {
                    var instProp = navType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    var instance = instProp?.GetValue(null);
                    var nav = navType.GetMethod("NavigateTo", new Type[] { typeof(Vector3) }) ?? navType.GetMethod("NavigateTo");
                    nav?.Invoke(instance, new object[] { waypoint.Position });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZeroIn] Navigation error: {ex.Message}");
            }
        }

        private void TryStopMovement()
        {
            try
            {
                var mcType = Type.GetType("AOSharp.Core.Movement.MovementController, AOSharp.Core");
                if (mcType != null)
                {
                    var instProp = mcType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    var instance = instProp?.GetValue(null);
                    if (instance != null)
                    {
                        var stop = mcType.GetMethod("Stop") ?? mcType.GetMethod("StopNavigation") ?? mcType.GetMethod("ClearMovement");
                        if (stop != null && stop.GetParameters().Length == 0)
                        {
                            stop.Invoke(instance, null);
                            return;
                        }

                        var setMethod = mcType.GetMethod("SetMovement");
                        if (setMethod != null && setMethod.GetParameters().Length == 1)
                        {
                            var p = setMethod.GetParameters()[0].ParameterType;
                            if (p == typeof(Vector3))
                            {
                                // set to current position to stop
                                var pos = DynelManager.LocalPlayer.Position;
                                setMethod.Invoke(instance, new object[] { pos });
                                return;
                            }
                            else
                            {
                                var maType = Type.GetType("AOSharp.Common.GameData.MovementAction, AOSharp.Common");
                                if (maType != null && p.IsAssignableFrom(maType))
                                {
                                    var ma = Activator.CreateInstance(maType);
                                    setMethod.Invoke(instance, new object[] { ma });
                                    return;
                                }
                            }
                        }
                    }
                }

                // Try NavmeshMovementController stop variants
                var navType = Type.GetType("NavmeshMovementController, NavmeshMovementController");
                if (navType != null)
                {
                    var instProp = navType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    var instance = instProp?.GetValue(null);
                    var stop = navType.GetMethod("Stop") ?? navType.GetMethod("StopNavigation");
                    stop?.Invoke(instance, null);
                }
            }
            catch { }
        }
    }
}
