using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Voxel.Utility;
using Voxel.World;

namespace Voxel.UI
{
    public class UIStateMainMenuLoading : UIState
    {
        [SerializeField]
        private UIStateGameMenu gameMenuState = default;
        private Slider loadingBar;
        private WaitForSeconds loadingWaitForSeconds;
        [SerializeField]
        private float loadingUpdateInterval = 1;
        private bool exitingState;

        private void Awake()
        {
            loadingBar = uiStateComponents[0].GetComponent<Slider>();
            loadingWaitForSeconds = new WaitForSeconds(loadingUpdateInterval);
            UnityAction buildWorldComplete = new UnityAction(DisableLoadingBar);
            EventManager.Listen("BuildWorldComplete", buildWorldComplete);
        }

        protected override void OnStateEnable()
        {
            exitingState = false;
            StartCoroutine(BuildWorldLoading());
        }

        private IEnumerator BuildWorldLoading()
        {
            while (!exitingState)
            {
                loadingBar.value = WorldManager.Instance.BuildWorldProgress;
                yield return loadingWaitForSeconds;
            }
            
        }

        private void DisableLoadingBar()
        {
            exitingState = true;
            loadingBar.value = 0;
            loadingBar.gameObject.SetActive(false);
            UIManager.Instance.ActivateState(gameMenuState);
        }
    }
}