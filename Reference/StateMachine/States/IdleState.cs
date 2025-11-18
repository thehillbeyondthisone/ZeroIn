using AOSharp.Core;
using System;
using AOSharp.Core.Inventory;
using AOSharp.Pathfinding;
using AOSharp.Common.GameData;

namespace AutomatonRoamba
{
    public class IdleState : FSMProvider<State, Trigger, RoamContext>, IState
    {
        public IdleState(FSM<State, Trigger, RoamContext> stateMachine) : base(stateMachine)
        {
        }

        public void OnStateEnter()
        {
            if (SMovementController.IsNavigating())
                SMovementController.Halt();

            if (DynelManager.LocalPlayer.MovementState != MovementState.Sit)
                SMovementController.SetMovement(MovementAction.SwitchToSit);
        }

        public void OnStateExit()
        {
            if (DynelManager.LocalPlayer.MovementState == MovementState.Sit)
                SMovementController.SetMovement(MovementAction.LeaveSit);
        }

        public void Tick()
        {
            if (StateMachine.Context.IsInCombat() || !StateMachine.Context.HealthOrNanoTooLow())
            {
                StateMachine.Fire(Trigger.Recovered);
            }
        }
    }
}