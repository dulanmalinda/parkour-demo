using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParkourLegion.Player
{
    public class PlayerStateMachine
    {
        private PlayerState currentState;
        private Dictionary<Type, PlayerState> states;

        public PlayerStateMachine()
        {
            states = new Dictionary<Type, PlayerState>();
        }

        public PlayerState CurrentState => currentState;

        public void AddState(PlayerState state)
        {
            Type stateType = state.GetType();
            if (!states.ContainsKey(stateType))
            {
                states.Add(stateType, state);
            }
        }

        public void ChangeState<T>() where T : PlayerState
        {
            Type stateType = typeof(T);

            if (!states.ContainsKey(stateType))
            {
                Debug.LogError($"State {stateType.Name} not found in state machine");
                return;
            }

            currentState?.Exit();
            currentState = states[stateType];
            currentState.Enter();
        }

        public void Update()
        {
            currentState?.Update();
            currentState?.CheckTransitions();
        }
    }
}
