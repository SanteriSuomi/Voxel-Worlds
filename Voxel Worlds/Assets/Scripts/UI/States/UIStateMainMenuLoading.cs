using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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

        private void Awake()
        {
            loadingBar = uiStateComponents[0].GetComponent<Slider>();
            loadingWaitForSeconds = new WaitForSeconds(loadingUpdateInterval);
        }

        protected override void OnStateEnable()
        {
            StartCoroutine(BuildWorldLoading());
        }

        private IEnumerator BuildWorldLoading()
        {
            while (loadingBar.value < 100)
            {
                loadingBar.value = WorldManager.Instance.BuildWorldProgress;
                yield return loadingWaitForSeconds;
            }
            
            UIManager.Instance.ActivateState(gameMenuState);
        }

        protected override void OnStateDisable()
        {
            loadingBar.value = 0;
            loadingBar.gameObject.SetActive(false);
        }
    }
}