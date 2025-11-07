using System;

namespace ZeroIn.StateMachine.States
{
    /// <summary>
    /// Idle state - waiting for scan to start
    /// </summary>
    public class IdleState
    {
        private ScanStateMachine _stateMachine;

        public IdleState(ScanStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public void OnEnter()
        {
            _stateMachine.Context.IsScanning = false;
        }

        public void OnExit()
        {
        }

        public void Tick()
        {
            // Idle state does nothing - waits for user to start scan
        }
    }
}
