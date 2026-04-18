using UnityEngine;

namespace ljk
{
    public class CameraFollow : MonoBehaviour
    {
        public enum OffsetMode
        {
            Local = 0,
            World = 1
        }

        [Header("跟随目标")]
        public Transform target;
        public Vector3 worldOffset = new Vector3(0f, 5f, -10f);
        public OffsetMode offsetMode = OffsetMode.Local;
        public bool useTargetYaw = true;

        [Header("跟随参数")]
        public float followSpeed = 5f;
        public float lookAtSpeed = 10f;
        public bool lockRotation = true;
        public bool smoothFollow = true;
        public bool lookAtTarget = true;

        [Header("边界限制")]
        public bool useBounds = false;
        public Vector3 minBounds = new Vector3(-100f, 0f, -100f);
        public Vector3 maxBounds = new Vector3(100f, 50f, 100f);

        private Vector3 desiredPosition;
        private Quaternion initialRotation;

        private void Start()
        {
            initialRotation = transform.rotation;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            desiredPosition = target.position + ResolveOffset();
            desiredPosition = ClampToBounds(desiredPosition);
            transform.position = smoothFollow
                ? Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime)
                : desiredPosition;

            ApplyRotation();
        }

        public void SetCameraOffset(Vector3 newOffset)
        {
            worldOffset = newOffset;
        }

        private Vector3 ResolveOffset()
        {
            if (offsetMode == OffsetMode.World)
            {
                return worldOffset;
            }

            Quaternion offsetRotation = useTargetYaw
                ? Quaternion.Euler(0f, target.eulerAngles.y, 0f)
                : target.rotation;

            return offsetRotation * worldOffset;
        }

        private Vector3 ClampToBounds(Vector3 position)
        {
            if (!useBounds)
            {
                return position;
            }

            position.x = Mathf.Clamp(position.x, minBounds.x, maxBounds.x);
            position.y = Mathf.Clamp(position.y, minBounds.y, maxBounds.y);
            position.z = Mathf.Clamp(position.z, minBounds.z, maxBounds.z);
            return position;
        }

        private void ApplyRotation()
        {
            if (lookAtTarget)
            {
                Vector3 lookDirection = target.position - transform.position;
                if (lookDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                    if (lockRotation)
                    {
                        Vector3 euler = lookRotation.eulerAngles;
                        lookRotation = Quaternion.Euler(initialRotation.eulerAngles.x, euler.y, initialRotation.eulerAngles.z);
                    }

                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, lookAtSpeed * Time.deltaTime);
                }

                return;
            }

            if (lockRotation)
            {
                transform.rotation = initialRotation;
            }
        }
    }
}
