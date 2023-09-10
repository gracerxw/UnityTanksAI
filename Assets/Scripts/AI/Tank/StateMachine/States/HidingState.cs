using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CE6127.Tanks.AI
{
    internal class HidingState : BaseState
    {
        private TankSM m_TankSM; // Reference to the tank state machine.
        public HidingState(TankSM tankStateMachine) : base("Hiding", tankStateMachine) => m_TankSM = (TankSM)m_StateMachine;
        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        public override void Update()
        {
            base.Update();
            
        }
    }

}