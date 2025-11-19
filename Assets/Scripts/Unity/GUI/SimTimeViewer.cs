using UnityEngine;
using UnityEngine.UI;

namespace hakoniwa.objects.core
{
    public class SimTimeViewer : MonoBehaviour
    {
        public GameObject myObject;
        public GameObject simtime_obj;
        private ISimTime isimtime;
        Text simTimeText;

        void Start()
        {
            simTimeText = myObject.GetComponent<Text>();
            isimtime = simtime_obj.GetComponentInChildren<ISimTime>();
        }

        // Update is called once per frame
        void Update()
        {
            long stime = isimtime.GetWorldTime();
            double t = ((double)stime) / 1000000.0f;
            if (stime <= 1)
            {
                simTimeText.text = "0.000";
            }
            else
            {
                long tl = (long)(t * 1000);
                t = (double)tl / 1000;
                simTimeText.text = t.ToString();
            }
        }

    }
}

