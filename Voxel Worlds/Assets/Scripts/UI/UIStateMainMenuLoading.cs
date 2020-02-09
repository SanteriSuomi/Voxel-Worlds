using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Voxel.vWorld;

namespace Voxel.UI
{
    public class UIStateMainMenuLoading : UIState
    {
        private Slider loadingBar;
        private WaitForSeconds loadingWaitForSeconds;
        [SerializeField]
        private float loadingUpdateInterval = 0.25f;

        private void Awake()
        {
            loadingBar = uiStateComponents[0].GetComponent<Slider>();
            loadingWaitForSeconds = new WaitForSeconds(loadingUpdateInterval);
        }

        protected override void OnStateEnable()
        {
            StartCoroutine(WorldBuildLoading());
        }

        private IEnumerator WorldBuildLoading()
        {
            while (loadingBar.value < 100)
            {
                loadingBar.value = World.Instance.BuildWorldProgress
                                 / World.Instance.TotalChunks
                                 * 100;
                yield return loadingWaitForSeconds;
            }

            loadingBar.value = 0;
            loadingBar.gameObject.SetActive(false);
        }
    }
}