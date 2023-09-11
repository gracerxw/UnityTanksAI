using UnityEngine;

using Debug = UnityEngine.Debug;

namespace CE6127.Tanks.AI
{
    /// <summary>
    /// Class <c>RepositioningState</c> represents the state of the tank when it is idle.
    /// </summary>
    internal class RepositioningState : BaseState
    {
        private TankSM m_TankSM; // Reference to the tank state machine.

        /// <summary>
        /// Constructor <c>RepositioningState</c> is the constructor of the class.
        /// </summary>
        public RepositioningState(TankSM tankStateMachine) : base("Repositioning", tankStateMachine) => m_TankSM = (TankSM)m_StateMachine;

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

            // try to move away from Player
            // or move to good waypoints
            // TODO HERE
        }
    }
}
