using UnityEngine;

namespace hakoniwa.objects.core
{
    public class TouchSensor : MonoBehaviour
    {
        public bool isTouched = false;
        public GameObject holder;
        public bool IsTouched()
        {
            return isTouched;
        }
        void Start()
        {
            //nothing to do
        }
        void OnTriggerEnter(Collider t)
        {
            this.isTouched = true;
            var parent = t.gameObject.transform.parent;
            if (parent) {
                Debug.Log("ENTER:" + parent.name);
                Baggage baggage = parent.GetComponentInChildren<Baggage>();
                if (baggage)
                {
                    baggage.Grab(holder);
                }
                Debug.Log("Baggage: " + baggage);
            }

        }
        void OnTriggerStay(Collider t)
        {
            this.isTouched = true;
            //Debug.Log("STAY:" + t.gameObject.name);
        }

        private void OnTriggerExit(Collider t)
        {
            this.isTouched = false;
            Debug.Log("EXIT:" + t.gameObject.name);
        }

    }
}
