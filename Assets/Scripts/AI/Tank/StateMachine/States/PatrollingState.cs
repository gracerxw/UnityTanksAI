using System.Collections;
using UnityEngine;

using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;

namespace CE6127.Tanks.AI
{
    /// <summary>
    /// Class <c>PatrollingState</c> represents the state of the tank when it is patrolling.
    /// </summary>
    internal class PatrollingState : BaseState
    {
        private TankSM m_TankSM;        // Reference to the tank state machine.
        private Vector3 m_Destination;  // Destination for the tank to move to.

        /// <summary>
        /// Constructor <c>PatrollingState</c> constructor.
        /// </summary>
        public PatrollingState(TankSM tankStateMachine) : base("Patrolling", tankStateMachine) => m_TankSM = (TankSM)m_StateMachine;

        /// <summary>
        /// Method <c>Enter</c> on enter.
        /// </summary>
        public override void Enter()
        {
            base.Enter();

            m_TankSM.SetStopDistanceToZero();

            m_TankSM.StartCoroutine(Patrolling());
        }

        /// <summary>
        /// Method <c>Update</c> update logic.
        /// </summary>
        public override void Update()
        {
            base.Update();
            m_TankSM.HyperAggression();

            Debug.Log("In Patrolling State");

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

            if (m_TankSM.IsAllyInRadius()){
                m_StateMachine.ChangeState(m_TankSM.m_States.Repositioning);
                return;
            }



            // update patrolling destination
            if (Time.time >= m_TankSM.NavMeshUpdateDeadline)
            {
                m_TankSM.NavMeshUpdateDeadline = Time.time + m_TankSM.PatrolNavMeshUpdate;
                m_TankSM.NavMeshAgent.SetDestination(m_Destination);
            }
        }

        /// <summary>
        /// Method <c>Exit</c> on exiting PatrollingState.
        /// </summary>
        public override void Exit()
        {
            base.Exit();

            m_TankSM.StopCoroutine(Patrolling());
        }

        /// <summary>
        /// Coroutine <c>Patrolling</c> patrolling coroutine.
        /// </summary>
        IEnumerator Patrolling()
        {
            while (true)
            {
                var patrolOffset = Random.insideUnitCircle * Random.Range(m_TankSM.PatrolMaxDist.x, m_TankSM.PatrolMaxDist.y);
                var vectorOffset = new Vector3(patrolOffset.x, 0f, patrolOffset.y);

                // if don't have last seen
                if(m_TankSM.targetLastSeen == m_TankSM.defaultVector3){
                    m_Destination = m_TankSM.transform.position + vectorOffset;
                } else {
                    // move to enemy last seen
                    m_Destination = m_TankSM.targetLastSeen + vectorOffset;
                }
                
                float waitInSec = Random.Range(m_TankSM.PatrolWaitTime.x, m_TankSM.PatrolWaitTime.y);
                yield return new WaitForSeconds(waitInSec);
            }
        }
    }
}
