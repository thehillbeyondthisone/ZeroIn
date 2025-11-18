using AOSharp.Common.GameData;
using AOSharp.Core;
using AOSharp.Core.Misc;
using AOSharp.Pathfinding;
using System;
namespace AutomatonRoamba
{
    public class LootState : FSMProvider<State, Trigger, RoamContext>, IState
    {
        private DateTime _lootTimeoutPeriod;

        public LootState(FSM<State, Trigger, RoamContext> stateMachine) : base(stateMachine)
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
            if (!StateMachine.Context.MobTargeting.TryGetNextCorpse(out Corpse corpse))
            {
                StateMachine.Fire(Trigger.TargetNull);
                return;
            }

            bool farAwayFromCorpse = Vector3.Distance(corpse.Position, DynelManager.LocalPlayer.Position) > 1;

            if (!SMovementController.IsNavigating() && farAwayFromCorpse)
            {
                StateMachine.Fire(Trigger.TargetNull);
                return;
            }

            if (farAwayFromCorpse)
            {
                _lootTimeoutPeriod = DateTime.Now;
            }

            if (_lootTimeoutPeriod != null && DateTime.Now > _lootTimeoutPeriod.AddSeconds(AutomatonRoamba.Config.PathingConfig.LootTimeoutPeriod))
            {
                StateMachine.Context.MobTargeting.RemoveCorpse(corpse.Identity);
                StateMachine.Fire(Trigger.TargetNull);
                return;
            }
        }
    }
}