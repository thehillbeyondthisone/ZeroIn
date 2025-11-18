using AOSharp.Core;
using AOSharp.Core.UI;
using AOSharp.Pathfinding;
using System.Linq;

namespace ZeroIn
{
    public class RoamState : FSMProvider<State, Trigger, RoamContext>, IState
    {
        public RoamState(FSM<State, Trigger, RoamContext> stateMachine) : base(stateMachine)
        {
        }

        public void OnStateEnter()
        {
        }

        public void OnStateExit()
        {
        }

        public void Tick()
        {
            // Scan for nearby players (ZeroIn functionality)
            ScanForPlayers();

            // Optional combat behavior - enabled via UI settings
            if (ZeroIn.Config.EnableHealthCheck)
            {
                if (StateMachine.Context.HealthOrNanoTooLow() && !StateMachine.Context.IsInCombat())
                {
                    StateMachine.Fire(Trigger.TooLowOnStats);
                    return;
                }
            }

            if (ZeroIn.Config.EnableLooting)
            {
                if (StateMachine.Context.MobTargeting.TryGetNextCorpse(out Corpse corpse))
                {
                    SMovementController.SetDestination(corpse.Position);
                    StateMachine.Fire(Trigger.LootTargetFound);
                    return;
                }
            }

            if (ZeroIn.Config.EnableCombat)
            {
                if (StateMachine.Context.MobTargeting.TryGetNextTarget(out SimpleChar target, out _, out _))
                {
                    StateMachine.Context.NextTarget = target;
                    StateMachine.Fire(Trigger.AliveTargetFound);
                    return;
                }
            }

            StateMachine.Context.NextTarget = null;

            // Follow the path and scan
            ZeroIn.SetPath(ZeroIn.RoamPath.SPath);
        }

        private void ScanForPlayers()
        {
            var scanner = StateMachine.Context.Scanner;
            if (scanner == null) return;

            scanner.Scan(); // Purge stale entries

            var localPlayer = DynelManager.LocalPlayer;
            if (localPlayer == null) return;

            var localPos = localPlayer.Position;

            if (ZeroIn.Config.VerboseDebug)
                Chat.WriteLine($"[ZeroIn] Scanning {DynelManager.Players.Count()} players in area...", ChatColor.White);

            // Scan all nearby players
            int scanned = 0;
            foreach (var player in DynelManager.Players)
            {
                if (player == null || !player.IsValid) continue;

                // Skip self
                if (player.Identity == localPlayer.Identity) continue;

                // Calculate distance
                float distance = AOSharp.Common.GameData.Vector3.Distance(localPos, player.Position);

                // Track all players (no range limit for mapping purposes)
                scanner.OnCharacterSeen(
                    (int)player.Identity.Instance,
                    player.Name,
                    player.Position.X,
                    player.Position.Y,
                    player.Position.Z,
                    player.Health,
                    distance,
                    Playfield.ModelIdentity.Instance,
                    Playfield.Name
                );
                scanned++;

                if (ZeroIn.Config.VerboseDebug)
                    Chat.WriteLine($"[ZeroIn]   → {player.Name} @ {distance:F1}m", ChatColor.White);
            }

            if (ZeroIn.Config.VerboseDebug && scanned > 0)
                Chat.WriteLine($"[ZeroIn] Scanned {scanned} players, total tracked: {scanner.Count}", ChatColor.White);
        }
    }
}
