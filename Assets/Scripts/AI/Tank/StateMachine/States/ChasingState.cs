using UnityEngine;

using Debug = UnityEngine.Debug;

namespace CE6127.Tanks.AI
{
    /// <summary>
    /// Class <c>ChasingState</c> represents the state of the tank when it is idle.
    /// </summary>
    internal class ChasingState : BaseState
    {
        private TankSM m_TankSM; // Reference to the tank state machine.

        /// <summary>
        /// Constructor <c>ChasingState</c> is the constructor of the class.
        /// </summary>
        public ChasingState(TankSM tankStateMachine) : base("Chasing", tankStateMachine) => m_TankSM = (TankSM)m_StateMachine;

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
            m_TankSM.HyperAggression();


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

            //  if under attack, do Evading
            if (m_TankSM.IsUnderAttack()){
                m_StateMachine.ChangeState(m_TankSM.m_States.Evading);
                return;
            }


            // if ally in radius, skeet away to Repositioning
            if(m_TankSM.IsAllyInRadius()){
                m_StateMachine.ChangeState(m_TankSM.m_States.Repositioning);
            }

            


            // try to move away from Player
            // or move to good waypoints
            // TODO HERE
        }
    }
}
