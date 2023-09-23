using UnityEngine;

using Debug = UnityEngine.Debug;

namespace CE6127.Tanks.AI
{
    /// <summary>
    /// Class <c>GettingCenterState</c> represents the state of the tank when it is idle.
    /// </summary>
    internal class GettingCenterState : BaseState
    {
        private TankSM m_TankSM; // Reference to the tank state machine.
        Vector3 Center1 = new Vector3(13.91f, 0.0f, -10.33f);
        Vector3 Center2 = new Vector3(-13.01f, 0.0f, -1.5f);
        float threshold_for_reaching = 4f;

        /// <summary>
        /// Constructor <c>GettingCenterState</c> is the constructor of the class.
        /// </summary>
        public GettingCenterState(TankSM tankStateMachine) : base("GettingCenter", tankStateMachine) => m_TankSM = (TankSM)m_StateMachine;

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
            // Debug.Log("In Getting Center phase");

            // if low health, move to hiding
            if (m_TankSM.isLowHealth){
                m_StateMachine.ChangeState(m_TankSM.m_States.Hiding);
                return;
            }

            // if enemy within range, start chasing
            if (m_TankSM.DistanceToTarget <= m_TankSM.TargetDistance){
                m_TankSM.targetLastSeen = m_TankSM.Target.position;
                m_StateMachine.ChangeState(m_TankSM.m_States.Chasing);
                return;
            }

            float dist_center1 = Vector3.Magnitude(m_TankSM.transform.position - Center1);
            float dist_center2 = Vector3.Magnitude(m_TankSM.transform.position - Center2);
            if(dist_center1 <= threshold_for_reaching || dist_center2 <= threshold_for_reaching ){
                m_StateMachine.ChangeState(m_TankSM.m_States.Patrolling);
            }

            if(dist_center1 <  dist_center2){
                m_TankSM.NavMeshAgent.SetDestination(Center1);
            } else {
                m_TankSM.NavMeshAgent.SetDestination(Center2);
            }
        }
    }
}
