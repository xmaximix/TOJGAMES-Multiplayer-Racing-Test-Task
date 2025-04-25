using UnityEngine;

namespace TojGamesTask.Modules.Gameplay.Core
{
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(0f, 5f, -10f);
        [SerializeField] [Min(0f)] private float smoothSpeed = 5f;

        public void SetTarget(Transform target)
        {
            this.target = target;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            UpdatePosition();
            UpdateRotation();
        }

        private void UpdatePosition()
        {
            var desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                smoothSpeed * Time.deltaTime
            );
        }

        private void UpdateRotation()
        {
            var lookAtPoint = target.position + Vector3.up * 1.5f;
            transform.LookAt(lookAtPoint);
        }
    }
}