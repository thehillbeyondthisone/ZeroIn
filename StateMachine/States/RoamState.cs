using AOSharp.Core;
using AOSharp.Pathfinding;

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

            if (StateMachine.Context.HealthOrNanoTooLow() && !StateMachine.Context.IsInCombat())
            {
                StateMachine.Fire(Trigger.TooLowOnStats);
                return;
            }

            if (StateMachine.Context.MobTargeting.TryGetNextCorpse(out Corpse corpse))
            {
                SMovementController.SetDestination(corpse.Position);
                StateMachine.Fire(Trigger.LootTargetFound);
                return;
            }

            if (StateMachine.Context.MobTargeting.TryGetNextTarget(out SimpleChar target, out _, out _))
            {
                StateMachine.Context.NextTarget = target;
                StateMachine.Fire(Trigger.AliveTargetFound);
                return;
            }

            StateMachine.Context.NextTarget = null;

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

            // Scan all nearby players
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
                    distance
                );
            }
        }
    }
}
