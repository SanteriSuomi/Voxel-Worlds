using DG.Tweening;
using System.Collections;
using UnityEngine;
using Voxel.Utility;
using Voxel.World;

namespace Voxel.Items
{
    public class BlockPickup: MonoBehaviour, IPickupable
    {
        private const float maxTimeAlive = 10;

        private const float upMoveLength = 0.4f;
        private const float upMoveDuration = 2;

        private const float rotationMoveDuration = 4;

        public BlockType BlockType { get; set; }

        private WaitForSeconds blockPickupWFS;

        private void Start()
        {
            blockPickupWFS = ReferenceManager.Instance.GetBlockPickupWFS(maxTimeAlive);
            StartCoroutine(PickupCoroutine());
        }

        private IEnumerator PickupCoroutine()
        {
            ActivateAnimation();
            yield return blockPickupWFS;
            DeactivePickup();
        }

        private void ActivateAnimation()
        {
            Sequence moveSequence = DOTween.Sequence();
            moveSequence.SetLoops(-1, LoopType.Restart);

            // Back and forth movement
            Vector3 moveOffset = new Vector3(0, upMoveLength, 0);
            moveSequence.Append(transform.DOLocalMove(transform.localPosition + moveOffset, upMoveDuration).SetEase(Ease.InOutQuad))
                        .Append(transform.DOLocalMove(transform.localPosition + moveOffset - moveOffset, upMoveDuration).SetEase(Ease.InOutQuad));

            // 360 rotation
            transform.DOLocalRotate(new Vector3(0, 360, 0), rotationMoveDuration, RotateMode.FastBeyond360)
                     .SetRelative()
                     .SetEase(Ease.Linear)
                     .SetLoops(-1, LoopType.Restart);
        }

        public void Pickup()
        {
            DeactivePickup();
        }

        private void DeactivePickup()
        {
            DOTween.Kill(this);
            Destroy(gameObject);
        }
    }
}