using System.Collections.Generic;
using UnityEngine;

namespace hakoniwa.drone
{
    public interface IDroneDisturbableObject
    {
        void ApplyDisturbance(float temperature, Vector3 windVector);
        void ResetDisturbance();
    }

    [RequireComponent(typeof(Collider))]
    public class DisturbanceArea : MonoBehaviour
    {
        public float temperature = 20.0f;
        public Vector3 windVector = new Vector3(1f, 0f, 0f);


        private void OnTriggerEnter(Collider other)
        {
            var target = other.GetComponentInParent<IDroneDisturbableObject>();
            if (target != null)
            {
                target.ApplyDisturbance(temperature, windVector);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var target = other.GetComponentInParent<IDroneDisturbableObject>();
            if (target != null)
            {
                target.ResetDisturbance();
            }
        }

    }


}
