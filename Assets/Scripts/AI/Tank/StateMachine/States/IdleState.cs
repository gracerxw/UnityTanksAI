using UnityEngine;

using Debug = UnityEngine.Debug;

namespace CE6127.Tanks.AI
{
    /// <summary>
    /// Class <c>IdleState</c> represents the state of the tank when it is idle.
    /// </summary>
    internal class IdleState : BaseState
    {
        private TankSM m_TankSM; // Reference to the tank state machine.

        /// <summary>
        /// Constructor <c>IdleState</c> is the constructor of the class.
        /// </summary>
        public IdleState(TankSM tankStateMachine) : base("Idle", tankStateMachine) => m_TankSM = (TankSM)m_StateMachine;

        /// <summary>
        /// Method <c>Enter</c> is called when the state is entered.
        /// </summary>
        public override void Enter() => base.Enter();

        /// <summary>
        /// Method <c>Update</c> is called each frame.
        /// </summary>
        public override void Update()
        {
            base.Update();

            // if there is a target
            if (m_TankSM.Target != null)
            {
                
                m_TankSM.HyperAggression();
                if (m_TankSM.isLowHealth){
                    m_StateMachine.ChangeState(m_TankSM.m_States.Hiding);
                    return;
                }

                // if not in range, switch to patrolling
                if (m_TankSM.DistanceToTarget > m_TankSM.TargetDistance)
                    m_StateMachine.ChangeState(m_TankSM.m_States.Patrolling);
            }
            
        }
    }
}
