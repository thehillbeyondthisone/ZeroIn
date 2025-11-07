using AOSharp.Core;
using AOSharp.Core.Movement;
using System;

namespace ZeroIn.StateMachine
{
    /// <summary>
    /// State machine for managing the scanning process
    /// </summary>
    public class ScanStateMachine
    {
        public enum State
        {
            Idle,
            Scanning
        }

        public State CurrentState { get; private set; }
        public ScanContext Context { get; private set; }

        private States.IdleState _idleState;
        private States.ScanningState _scanningState;

        public ScanStateMachine(ScanContext context)
        {
            Context = context;
            CurrentState = State.Idle;

            _idleState = new States.IdleState(this);
            _scanningState = new States.ScanningState(this);
        }

        /// <summary>
        /// Updates the state machine
        /// </summary>
        public void Tick()
        {
            try
            {
                switch (CurrentState)
                {
                    case State.Idle:
                        _idleState.Tick();
                        break;

                    case State.Scanning:
                        _scanningState.Tick();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ZeroIn] State machine error: {ex.Message}");
            }
        }

        /// <summary>
        /// Transitions to a new state
        /// </summary>
        public void TransitionTo(State newState)
        {
            if (CurrentState == newState)
                return;

            Console.WriteLine($"[ZeroIn] State: {CurrentState} -> {newState}");

            // Exit current state
            switch (CurrentState)
            {
                case State.Idle:
                    _idleState.OnExit();
                    break;
                case State.Scanning:
                    _scanningState.OnExit();
                    break;
            }

            CurrentState = newState;

            // Enter new state
            switch (CurrentState)
            {
                case State.Idle:
                    _idleState.OnEnter();
                    break;
                case State.Scanning:
                    _scanningState.OnEnter();
                    break;
            }
        }

        /// <summary>
        /// Starts a scan
        /// </summary>
        public void StartScan()
        {
            if (CurrentState == State.Scanning)
            {
                Console.WriteLine("[ZeroIn] Scan already in progress");
                return;
            }

            TransitionTo(State.Scanning);
        }

        /// <summary>
        /// Stops the current scan
        /// </summary>
        public void StopScan()
        {
            if (CurrentState == State.Idle)
                return;

            TransitionTo(State.Idle);
        }
    }
}
