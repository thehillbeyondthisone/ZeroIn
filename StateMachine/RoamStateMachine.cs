using AOSharp.Core.Misc;
using AOSharp.Core;
using AOSharp.Pathfinding;
using System;
using AOSharp.Common.GameData;
using System.Linq;
using AOSharp.Core.Inventory;
using ZeroIn.Scanner;

namespace ZeroIn
{
    public enum State
    {
        Roam,
        Idle,
        Loot,
        PathToMob,
        Fight
    }

    public enum Trigger
    {
        TargetIsHittable,
        TargetOutRange,
        AliveTargetFound,
        LootTargetFound,
        ChangedTarget,
        RetriggerOnEntry,
        TargetNull,
        Recovered,
        TooLowOnStats,
        LootingTarget
    }

    public class RoamContext
    {
        public SimpleChar NextTarget;
        public DateTime LastFightTime;
        public MobTargeting MobTargeting;
        public CharacterScanner Scanner;
        public ScanMap Map;

        public RoamContext(MobTargeting mobTargeting, CharacterScanner scanner, ScanMap map)
        {
            MobTargeting = mobTargeting;
            Scanner = scanner;
            Map = map;
        }

        public bool IsInCombat()
        {
            if (!Team.IsInTeam)
                return DynelManager.NPCs.Any(x => x.FightingTarget?.Identity == DynelManager.LocalPlayer.Identity);
            else
                return DynelManager.NPCs.Any(x => Team.Members.Where(c => c.Character != null).Select(c => c.Identity).Any(c => c == x.FightingTarget?.Identity));
        }

        public bool HealthOrNanoTooLow()
        {
            if (ZeroIn.Config.PathingConfig.DisableIfNp && DynelManager.LocalPlayer.NanoPercent < ZeroIn.Config.PathingConfig.NanoPercent)
                return true;

            if (ZeroIn.Config.PathingConfig.DisableIfHp && DynelManager.LocalPlayer.HealthPercent < ZeroIn.Config.PathingConfig.HealthPercent)
                return true;

            return false;
        }

        public bool DisableIfAttacked() => ZeroIn.Config.PathingConfig.DisableIfAttacked && DynelManager.Characters.Any(x => x.FightingTarget?.Identity == DynelManager.LocalPlayer.Identity);

    }

    public class RoamStateMachine : FSM<State, Trigger, RoamContext>
    {
        private AutoResetInterval _fsmTickRate;
        public const int UpdateRate = 10;
        private bool _enabled = false;

        public bool IsEnabled => _enabled;

        public RoamStateMachine(MobTargeting mobTargeting, CharacterScanner scanner, ScanMap map, bool enabled = false) : base(State.Roam, new RoamContext(mobTargeting, scanner, map))
        {
            _enabled = enabled;
            _fsmTickRate = new AutoResetInterval(1000 / UpdateRate);
            Game.OnUpdate += OnUpdate;
            SMovementController.DestinationReached += OnDestinationReached;
            Inventory.ContainerOpened += OnCorpseOpened;
        }

        public void SetStatus(bool enabled)
        {
            _enabled = enabled;

            if (enabled)
                return;

            if (DynelManager.LocalPlayer.IsAttacking)
                DynelManager.LocalPlayer.StopAttack(false);

            Fire(Trigger.TargetNull);
        }

        private void OnDestinationReached(Vector3 vector)
        {
            if (!_enabled)
                return;

            var sPath = ZeroIn.RoamPath.SPath;

            if (sPath.Waypoints.Count == 0)
                return;

            var startPos = sPath.Waypoints.FirstOrDefault();
            var endPos = sPath.Waypoints.LastOrDefault();

            if (Vector3.Distance(startPos, vector) < 0.05f || Vector3.Distance(endPos, vector) < 0.05f)
            {
                if (!sPath.IsLooping)
                    sPath.Reverse();

                ZeroIn.SetPath(sPath);
                return;
            }
        }

        private void OnCorpseOpened(object sender, Container container)
        {
            if (!_enabled)
                return;

            if (container.Identity.Type != IdentityType.Corpse)
                return;

            Fire(Trigger.TargetNull);
        }

        private void OnUpdate(object sender, float e)
        {
            if (!_fsmTickRate.Elapsed)
                return;

            if (!_enabled)
                return;

            if (Playfield.ModelIdentity.Instance != ZeroIn.RoamPath?.SPath?.PlayfieldId)
            {
                _enabled = false;
                return;
            }
            Tick();
        }

        protected override void ConfigureStateMachine()
        {
            OnTransitioned(OnTransitioned);

            AddState(State.Roam, typeof(RoamState))
                .Permit(Trigger.AliveTargetFound, State.PathToMob)
                .Permit(Trigger.LootTargetFound, State.Loot)
                .Permit(Trigger.TooLowOnStats, State.Idle)
                .PermitReentry(Trigger.TargetNull);

            AddState(State.Idle, typeof(IdleState))
                .Permit(Trigger.Recovered, State.Roam)
                .PermitReentry(Trigger.TargetNull);

            AddState(State.Loot, typeof(LootState))
                .Permit(Trigger.TargetNull, State.Roam)
                .PermitReentry(Trigger.LootingTarget);

            AddState(State.PathToMob, typeof(PathToMobState))
                .Permit(Trigger.TargetNull, State.Roam)
                .Permit(Trigger.TargetIsHittable, State.Fight)
                .PermitReentry(Trigger.AliveTargetFound)
                .PermitReentry(Trigger.ChangedTarget);

            AddState(State.Fight, typeof(FightState))
                .Permit(Trigger.TargetNull, State.Roam)
                .Permit(Trigger.TooLowOnStats, State.Idle)
                .Permit(Trigger.TargetOutRange, State.PathToMob)
                .PermitReentry(Trigger.AliveTargetFound)
                .PermitReentry(Trigger.ChangedTarget);
        }

        private void OnTransitioned(Transition transition)
        {
            ZeroIn.Log.Information($"Transitioned from {transition.Source} to {transition.Destination}. Trigger {transition.Trigger}");
        }
    }
}