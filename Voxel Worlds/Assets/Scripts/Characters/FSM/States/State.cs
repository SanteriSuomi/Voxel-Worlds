﻿using UnityEngine;

namespace Voxel.Characters.AI
{
    public class State : MonoBehaviour, IState
    {
        protected FSM fsm;
        protected Base baseState;

        private void Awake()
        {
            fsm = GetComponentInChildren<FSM>();
            if (fsm == null)
            {
                #if UNITY_EDITOR
                Debug.Log("No FSM found in this GameObject! State is useless.");
                #endif
            }

            baseState = GetComponentInChildren<Base>();
        }

        public virtual void Enter(){}
        public virtual void Exit(){}
        public virtual void Tick(){}
    }
}