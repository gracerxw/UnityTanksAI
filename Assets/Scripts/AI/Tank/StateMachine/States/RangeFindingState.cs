using System.Collections;
using UnityEngine;

using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;

namespace CE6127.Tanks.AI
{
    /// <summary>
    /// Class <c>RangeFindingState</c> represents the state of the tank when it is idle.
    /// </summary>
    internal class RangeFindingState : BaseState
    {
        private TankSM m_TankSM; // Reference to the tank state machine.
        private Vector3 m_Destination;  // Destination for the tank to move to.
        private Vector3 directionTowardsTarget; // direction to target tank 
        private Vector3 directionToMoveAway; // direction for tank to move away

        /// <summary>
        /// Constructor <c>RangeFinding</c> is the constructor of the class.
        /// </summary>
        public RangeFindingState(TankSM tankStateMachine) : base("RangeFinding", tankStateMachine) => m_TankSM = (TankSM)m_StateMachine;

        /// <summary>
        /// Method <c>Enter</c> is called when the state is entered.
        /// </summary>
        public override void Enter() 
        {
            base.Enter();

            m_TankSM.StartCoroutine(RangeFinding());
        }

        /// <summary>
        /// Method <c>Update</c> is called each frame.
        /// </summary>
        public override void Update()
        {
            base.Update();
            m_TankSM.HyperAggression();

            Debug.Log("In range finding state:");

            // Go Grace!

            // once good range is reached ie max StopDistance away (22f), go back to Chasing state
            if (m_TankSM.DistanceToTarget >= m_TankSM.StopDistance) // StopDistance = 22f
            {
                Debug.Log("changing state");
                m_StateMachine.ChangeState(m_TankSM.m_States.Chasing);
                return; 
            }

            // update rangefinding destination
            Debug.Log("update destination");
            m_TankSM.NavMeshAgent.SetDestination(m_Destination);

            // old code 
            // if (Time.time >= m_TankSM.NavMeshUpdateDeadline)
            // {
            //     Debug.Log("update destination");
            //     // no delay 
            //     m_TankSM.NavMeshUpdateDeadline = Time.time; // + m_TankSM.PatrolNavMeshUpdate; 
            //     m_TankSM.NavMeshAgent.SetDestination(m_Destination);
            // }
        }

        /// <summary>
        /// Method <c>Exit</c> on exiting RangeFinding State.
        /// </summary>
        public override void Exit()
        {
            base.Exit();

            Debug.Log("exit coroutine");
            m_TankSM.StopCoroutine(RangeFinding());
        }

        /// <summary>
        /// Coroutine <c>Patrolling</c> patrolling coroutine.
        /// </summary>
        IEnumerator RangeFinding()
        {
            Debug.Log("coroutine start");
            while (true) 
            {
                // direction AI tank has been moving in (towards enemy)
                directionTowardsTarget = m_TankSM.Target.position - m_TankSM.transform.position; 
                directionTowardsTarget = directionTowardsTarget.normalized;
                Debug.Log("calc directionTowardsTarget");
                // amount to move in the other direction 
                directionToMoveAway = -1 * directionTowardsTarget;
                Debug.Log("calc directionToMoveAway");

                // destination for moving in the other direction 
                m_Destination = m_TankSM.Target.position + directionToMoveAway*m_TankSM.StopDistance; // maximum move 22f dist away (max StopDistance)
                // TEST
                // m_Destination = m_TankSM.Target.position + directionToMoveAway*100f; // maximum move 22f dist away (max StopDistance)
                Debug.Log("calc m_Destination");

                // old code
                // float waitInSec = Random.Range(m_TankSM.PatrolWaitTime.x, m_TankSM.PatrolWaitTime.y); // time delay set to the same as patrolling time delay
                // TEST
                // float waitInSec = Random.Range(0.01f, 0.2f); // time delay set to the same as patrolling time delay
                // yield return new WaitForSeconds(waitInSec);
                
                // Resume execution after All Update functions have been called, on the next frame.
                yield return null; 
            }
        }
    }
}
