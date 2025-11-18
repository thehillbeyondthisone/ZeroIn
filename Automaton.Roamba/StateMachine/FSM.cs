using AOSharp.Core.Misc;
using AOSharp.Core;
using AOSharp.Pathfinding;
using Stateless;
using System.Collections.Generic;
using System;
using System.Runtime.Remoting.Contexts;
using AOSharp.Core.UI;

namespace AutomatonRoamba
{
    public interface IState
    {
        void Tick();
        void OnStateEnter();
        void OnStateExit();
    }

    public class FSMProvider<TState, TTrigger, TContext>
    {
        public readonly FSM<TState, TTrigger, TContext> StateMachine;

        public FSMProvider(FSM<TState, TTrigger, TContext> stateMachine)
        {
            StateMachine = stateMachine;
        }
    }

    public abstract class FSM<TState, TTrigger, TContext> : StateMachine<TState, TTrigger>
    {
        public TContext Context;
        private IState _executingState = null;
        private Dictionary<TState, Type> _states = new Dictionary<TState, Type>();
        private bool _initialized = false;

        public FSM(TState defaultState, TContext context) : base(defaultState)
        {
            ConfigureStateMachine();
            Context = context;
        }

        public void Tick()
        {
            if (!_initialized)
            {
                EnterState(State);
                _initialized = true;
            }

            _executingState.Tick();
        }

        protected abstract void ConfigureStateMachine();

        private void EnterState(TState state)
        {
            _executingState = (IState)Activator.CreateInstance(_states[state], this);
            _executingState.OnStateEnter();
        }

        private void ExitState()
        {
            _executingState.OnStateExit();
        }

        public void SetDefaultState(TState state)
        {
            EnterState(state);
        }

        public StateConfiguration AddState(TState state, Type stateType)
        {
            if (!_states.ContainsKey(state))
                _states.Add(state, stateType);

            return Configure(state).OnEntry(() => EnterState(state)).OnExit(() => ExitState());
        }

        public void AddGlobalPermit(TTrigger trigger, TState destinationState)
        {
            foreach (TState state in Enum.GetValues(typeof(TState)))
            {
                if (state.Equals(destinationState))
                    Configure(state).Ignore(trigger);
                else
                    Configure(state).Permit(trigger, destinationState);
            }
        }
    }
}