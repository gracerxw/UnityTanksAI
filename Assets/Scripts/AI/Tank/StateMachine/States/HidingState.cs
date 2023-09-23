using UnityEngine;

using Debug = UnityEngine.Debug;

namespace CE6127.Tanks.AI
{
    /// <summary>
    /// Class <c>HidingState</c> represents the state of the tank when it is idle.
    /// </summary>
    internal class HidingState : BaseState
    {
        private TankSM m_TankSM; // Reference to the tank state machine.
        Vector3 HideSpot = new Vector3(43.82f, 0.0f, -39.34f);

        /// <summary>
        /// Constructor <c>HidingState</c> is the constructor of the class.
        /// </summary>
        public HidingState(TankSM tankStateMachine) : base("Hiding", tankStateMachine) => m_TankSM = (TankSM)m_StateMachine;

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
            //Debug.Log("In Hiding phase");
            m_TankSM.CheckHealth();
            if (m_TankSM.isLowHealth){
                m_StateMachine.ChangeState(m_TankSM.m_States.Patrolling);
                return;
            }
            else{
                m_StateMachine.ChangeState(m_TankSM.m_States.Patrolling); 
            }

            if(Vector3.Magnitude(HideSpot -  m_TankSM.transform.position) <= 2f){
                m_TankSM.HyperAggression();
            } else {
                m_TankSM.NavMeshAgent.SetDestination(HideSpot);
            }
        }
    }
}
