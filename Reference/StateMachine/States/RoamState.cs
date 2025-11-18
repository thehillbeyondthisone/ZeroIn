using AOSharp.Core;
using AOSharp.Pathfinding;

namespace AutomatonRoamba
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

            AutomatonRoamba.SetPath(AutomatonRoamba.RoamPath.SPath);
        }
    }
}