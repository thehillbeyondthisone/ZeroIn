using AOSharp.Core;
using System;
using AOSharp.Core.Inventory;
using AOSharp.Core.UI;

namespace ZeroIn
{
    public class FightState : FSMProvider<State, Trigger, RoamContext>, IState
    {
        public FightState(FSM<State, Trigger, RoamContext> stateMachine) : base(stateMachine)
        {
        }

        public void OnStateEnter()
        {
            StateMachine.Context.LastFightTime = DateTime.Now;
        }

        public void OnStateExit()
        {
        }

        public void Tick()
        {
            if (DateTime.Now > StateMachine.Context.LastFightTime.AddSeconds(ZeroIn.Config.PathingConfig.FightTimeoutPeriod))
            {
                StateMachine.Context.MobTargeting.AddToIgnoreList(StateMachine.Context.NextTarget.Identity);
                ZeroIn.Log.Information($"Adding '{StateMachine.Context.NextTarget.Identity}' to ignore list");
                StateMachine.Fire(Trigger.TargetNull);
                return;
            }

            if (!StateMachine.Context.MobTargeting.TryGetNextTarget(out SimpleChar target, out _, out _))
            {
                StateMachine.Fire(Trigger.TargetNull);
                return;
            }

            if (StateMachine.Context.HealthOrNanoTooLow() && !StateMachine.Context.IsInCombat())
            {
                StateMachine.Fire(Trigger.TooLowOnStats);
                return;
            }

            if (target.Identity != StateMachine.Context.NextTarget.Identity)
            {
                StateMachine.Context.NextTarget = target;
                StateMachine.Fire(Trigger.ChangedTarget);
                return;
            }

            if (!StateMachine.Context.MobTargeting.IsInWeaponsRange(StateMachine.Context.NextTarget) && !StateMachine.Context.DisableIfAttacked())
            {
                if (DynelManager.LocalPlayer.IsAttacking && !FightTargetIsNextTarget())
                    DynelManager.LocalPlayer.StopAttack(false);

                StateMachine.Fire(Trigger.TargetOutRange);
                return;
            }

            if (!DynelManager.LocalPlayer.IsAttackPending && (!DynelManager.LocalPlayer.IsAttacking || !FightTargetIsNextTarget()))
            {
                DynelManager.LocalPlayer.Attack(StateMachine.Context.NextTarget, false);
            }
        }

        private bool FightTargetIsNextTarget() => DynelManager.LocalPlayer.FightingTarget?.Identity == StateMachine.Context.NextTarget.Identity;
    }
}