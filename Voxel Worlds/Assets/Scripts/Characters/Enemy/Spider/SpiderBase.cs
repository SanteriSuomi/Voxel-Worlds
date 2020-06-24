using UnityEngine;
using Voxel.Characters.Enemy;

namespace Voxel.Characters.AI
{
    public class SpiderBase : Base
    {
        [SerializeField]
        protected Rigidbody rigidBody = default;
        public Rigidbody RigidBody => rigidBody;
        [SerializeField]
        protected LayerMask detectionLayerMask = default;
        public LayerMask DetectionLayerMask => detectionLayerMask;

        protected readonly RaycastHit[] infrontResults = new RaycastHit[3];
        public const float InfrontDistance = 0.525f;

        public void TryJump()
        {
            int objsInfrontAmount = Physics.RaycastNonAlloc(transform.position, transform.forward, infrontResults, InfrontDistance, detectionLayerMask);
            if (objsInfrontAmount > 0)
            {
                rigidBody.AddForce(Vector3.up * Spider.JumpMultiplier, ForceMode.Impulse);
            }
        }

        public void TryRotate(Quaternion rotation)
        {
            float rotationAngle = Quaternion.Angle(transform.rotation, rotation);
            if (rotationAngle > Spider.MinAngleForRotation)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Spider.RotationSpeedMultiplier * Time.deltaTime);
            }
        }
    }
}