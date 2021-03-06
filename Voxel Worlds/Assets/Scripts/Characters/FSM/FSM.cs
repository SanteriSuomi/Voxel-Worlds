﻿using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Voxel.Characters.AI
{
    public enum TickType
    {
        Delta,
        Fixed
    }

    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Correct abbreviation")]
    public class FSM : MonoBehaviour
    {
        [SerializeField]
        private TickType tickType = default;
        private WaitForFixedUpdate update;

        private IState currentState;
        public IState CurrentState
        {
            get => currentState;
            set
            {
                if (value.Equals(currentState))
                {
                    return;
                }

                currentState?.Exit();
                currentState = value;
                currentState?.Enter();
            }
        }

        private Coroutine tickCoroutine;

        private void Awake()
        {
            if (tickType == TickType.Fixed)
            {
                update = new WaitForFixedUpdate();
            }
        }

        /// <summary>
        /// Start the FSM update.
        /// </summary>
        /// <param name="actions">Methods that will be executed independently after each tick.</param>
        public void StartTick(params Action[] actions)
        {
            TryStopTickCoroutine();
            tickCoroutine = StartCoroutine(Tick(actions));
        }

        private IEnumerator Tick(params Action[] actions)
        {
            while (true)
            {
                ExecuteActions(actions);
                currentState?.Tick();
                yield return update;
            }
        }

        private static void ExecuteActions(Action[] actions)
        {
            for (int i = 0; i < actions.Length; i++)
            {
                actions[i]();
            }
        }

        public void StopTick() => TryStopTickCoroutine();

        private void TryStopTickCoroutine()
        {
            if (tickCoroutine != null)
            {
                StopCoroutine(tickCoroutine);
            }
        }
    }
}