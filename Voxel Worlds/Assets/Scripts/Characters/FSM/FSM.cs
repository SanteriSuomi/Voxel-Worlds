using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Voxel.Characters.AI
{
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Correct abbreviation")]
    public class FSM : MonoBehaviour
    {
        private IState currentState;
        public IState CurrentState
        {
            get => currentState;
            set
            {
                currentState?.Exit();
                currentState = value;
                currentState?.Enter();
            }
        }

        private Coroutine tickCoroutine;

        public void StartTick()
        {
            TryStopTickCoroutine();
            tickCoroutine = StartCoroutine(Tick());
        }

        private IEnumerator Tick()
        {
            while (true)
            {
                currentState?.Tick();
                yield return null;
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