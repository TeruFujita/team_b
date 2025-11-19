using UnityEngine;

namespace hakoniwa.objects.core
{
    public class SimpleBaggageGrabber : MonoBehaviour, IBaggageGrabber
    {
        public Magnet magnet;

        public void Grab(bool forceOn)
        {
            if (forceOn)
            {
                magnet.TurnOnForce();
            }
            else
            {
                magnet.TurnOn();
            }
        }

        public bool IsGrabbed()
        {
            return magnet.IsConntactOn();
        }

        public void Release()
        {
            magnet.TurnOff();
        }
    }
}
