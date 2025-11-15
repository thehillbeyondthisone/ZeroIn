using AOSharp.Core;
using AOSharp.Pathfinding;
using AOSharp.Core.Inventory;
using AOSharp.Common.GameData;

namespace AutomatonRoamba
{
    public class PathToMobState : FSMProvider<State, Trigger, RoamContext>, IState
    {
        public PathToMobState(FSM<State, Trigger, RoamContext> stateMachine) : base(stateMachine)
        {
        }

        public void OnStateEnter()
        {
        }

        public void OnStateExit()
        {
            if (SMovementController.IsNavigating())
                SMovementController.Halt();
        }

        public void Tick()
        {
            if (StateMachine.Context.MobTargeting.TryGetNextCorpse(out _))
            {
                StateMachine.Fire(Trigger.TargetNull);
                return;
            }

            if (!StateMachine.Context.MobTargeting.TryGetNextTarget(out SimpleChar target, out bool shouldAttack, out Item tauntItem))
            {
                StateMachine.Fire(Trigger.TargetNull);
                return;
            }

            if (target.Identity != StateMachine.Context.NextTarget.Identity)
            {
                StateMachine.Context.NextTarget = target;
                StateMachine.Fire(Trigger.ChangedTarget);
                return;
            }

            var isInPathRange = StateMachine.Context.MobTargeting.IsInPathRange(StateMachine.Context.NextTarget);

            if (tauntItem != null)
            {
                Targeting.SetTarget(target);

                if (!DynelManager.LocalPlayer.Cooldowns.ContainsKey(Stat.Psychology) && !Item.HasPendingUse)
                    tauntItem.Use();

                if (!isInPathRange && SMovementController.IsNavigating())
                    SMovementController.Halt();
            }

            if (StateMachine.Context.MobTargeting.IsInWeaponsRange(StateMachine.Context.NextTarget) || StateMachine.Context.DisableIfAttacked())
            {
                if (shouldAttack) 
                {
                    StateMachine.Fire(Trigger.TargetIsHittable);
                    return;
                }

                if (SMovementController.IsNavigating())
                    SMovementController.Halt();

                return;
            }

            if (isInPathRange)
            {
                SMovementController.SetDestination(StateMachine.Context.NextTarget.GetPathPos());
            }
        }
    }
}