using UnityEngine;

namespace ParkourLegion.Player
{
    public abstract class PlayerState
    {
        protected PlayerController controller;
        protected string stateName;

        public PlayerState(PlayerController controller, string stateName)
        {
            this.controller = controller;
            this.stateName = stateName;
        }

        public string StateName => stateName;

        public abstract void Enter();
        public abstract void Update();
        public abstract void Exit();
        public abstract void CheckTransitions();
    }
}
