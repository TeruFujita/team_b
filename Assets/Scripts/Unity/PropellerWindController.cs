using UnityEngine;

namespace hakoniwa.objects.core
{
    public class PropellerWindController : MonoBehaviour
    {
        public ParticleSystem windParticle;

        [Header("Wind Parameters")]
        public float speedFactor = 2.0f;
        public float lifetimeBase = 0.5f;
        public float lifetimeReductionFactor = 0.05f;
        public float minLifetime = 0.1f;
        public float maxLifetime = 1.0f;

        [Header("Wind Position Offset")]
        public float offsetDistance = 0.1f;

        [Header("Wind Threshold")]
        public float windThreshold = 0.05f; // この値以下で停止

        private Vector3 baseLocalPosition;
        private Vector3 windVelocity = Vector3.zero;

        private Transform parentTransform;

        void Start()
        {
            parentTransform = transform.parent;
            baseLocalPosition = transform.localPosition;
        }

        public void SetWindVelocityFromRos(Vector3 rosVelocity)
        {
            // ROS ENU (x, y, z) → Unity (x, z, -y)
            windVelocity = new Vector3(rosVelocity.x, rosVelocity.z, -rosVelocity.y);
        }

        void Update()
        {
            float windStrength = windVelocity.magnitude;

            if (windStrength < windThreshold)
            {
                StopWind();
                return;
            }

            UpdateWindPositionAndDirection();
            UpdateParticleSettings(windStrength);
            PlayWind();
        }

        private void UpdateWindPositionAndDirection()
        {
            Vector3 baseWorldPosition = parentTransform != null
                ? parentTransform.TransformPoint(baseLocalPosition)
                : baseLocalPosition;

            Vector3 worldDirection = windVelocity.normalized;

            transform.rotation = Quaternion.LookRotation(worldDirection);
            transform.position = baseWorldPosition - worldDirection * offsetDistance;
        }

        private void UpdateParticleSettings(float windStrength)
        {
            var main = windParticle.main;
            main.startSpeed = windStrength * speedFactor;
            main.startLifetime = Mathf.Clamp(lifetimeBase - windStrength * lifetimeReductionFactor, minLifetime, maxLifetime);
        }

        private void StopWind()
        {
            if (windParticle.isPlaying)
            {
                windParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            // 位置はベース位置に戻す
            Vector3 baseWorldPosition = parentTransform != null
                ? parentTransform.TransformPoint(baseLocalPosition)
                : baseLocalPosition;
            transform.position = baseWorldPosition;
        }

        private void PlayWind()
        {
            if (!windParticle.isPlaying)
            {
                windParticle.Play();
            }
        }
    }
}
