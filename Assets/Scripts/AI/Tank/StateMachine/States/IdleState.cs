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
                // calculate distance to see if tank in range
                var dist = Vector3.Distance(m_TankSM.transform.position, m_TankSM.Target.position);

                // if not in range, switch to patrolling
                if (dist > m_TankSM.TargetDistance)
                    m_StateMachine.ChangeState(m_TankSM.m_States.Patrolling);
                else
                    m_TankSM.LaunchProjectile();

                // ... Just for demonstration purposes; more to be implemented.
                
                // testing - this should always launch projectile
                // m_TankSM.LaunchProjectile();
            }

            var lookPos = m_TankSM.Target.position - m_TankSM.transform.position;
            lookPos.y = 0f;
            var rot = Quaternion.LookRotation(lookPos);
            m_TankSM.transform.rotation = Quaternion.Slerp(m_TankSM.transform.rotation, rot, m_TankSM.OrientSlerpScalar);
        }
    }
}
