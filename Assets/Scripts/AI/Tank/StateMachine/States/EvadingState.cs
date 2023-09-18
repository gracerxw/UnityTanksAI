using UnityEngine;

using Debug = UnityEngine.Debug;

namespace CE6127.Tanks.AI
{
    /// <summary>
    /// Class <c>EvadingState</c> represents the state of the tank when it is idle.
    /// </summary>
    internal class EvadingState : BaseState
    {
        private TankSM m_TankSM; // Reference to the tank state machine.

        /// <summary>
        /// Constructor <c>EvadingState</c> is the constructor of the class.
        /// </summary>
        public EvadingState(TankSM tankStateMachine) : base("Evading", tankStateMachine) => m_TankSM = (TankSM)m_StateMachine;

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
            m_TankSM.AvoidEnemy();

            Debug.Log("In Evading State");

            // if low health, move to hiding
            if (m_TankSM.isLowHealth){
                m_StateMachine.ChangeState(m_TankSM.m_States.Hiding);
                return;
            }

            // if enemy not within range, do patrolling
            if (m_TankSM.DistanceToTarget > m_TankSM.TargetDistance){
                m_StateMachine.ChangeState(m_TankSM.m_States.Patrolling);
                return;
            }

            // update tank last seen
            m_TankSM.targetLastSeen = m_TankSM.Target.position;

            // if enemy too close, do RangeFinding
            if (m_TankSM.DistanceToTarget <= m_TankSM.minDistToPlayer){
                m_StateMachine.ChangeState(m_TankSM.m_States.RangeFinding);
                return;
            }

            //  if under attack, do Chasing
            if (!m_TankSM.IsUnderAttack()){
                m_StateMachine.ChangeState(m_TankSM.m_States.Chasing);
                return;
            }


            // if ally in radius, skeet away to Repositioning
            if(m_TankSM.IsAllyInRadius()){
                m_StateMachine.ChangeState(m_TankSM.m_States.Repositioning);
            }
        }
    }
}
