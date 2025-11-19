using System.Collections;
using UnityEngine;

namespace hakoniwa.objects.core
{
    public class HakoDroneXrInputManager : MonoBehaviour, IDroneInput
    {
        public static HakoDroneXrInputManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject); // シーンをまたいで保持
        }

        public Vector2 GetLeftStickInput()
        {
            return OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);
        }

        public Vector2 GetRightStickInput()
        {
            return OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);
        }

        public bool IsAButtonPressed()
        {
            return OVRInput.GetDown(OVRInput.RawButton.A);
        }

        public bool IsAButtonReleased()
        {
            return OVRInput.GetUp(OVRInput.RawButton.A);
        }
        public bool IsBButtonPressed()
        {
            return OVRInput.GetDown(OVRInput.RawButton.B);
        }

        public bool IsBButtonReleased()
        {
            return OVRInput.GetUp(OVRInput.RawButton.B);
        }
        public bool IsXButtonPressed()
        {
            return OVRInput.GetDown(OVRInput.RawButton.X);
        }

        public bool IsXButtonReleased()
        {
            return OVRInput.GetUp(OVRInput.RawButton.X);
        }
        public bool IsYButtonPressed()
        {
            return OVRInput.GetDown(OVRInput.RawButton.Y);
        }

        public bool IsYButtonReleased()
        {
            return OVRInput.GetUp(OVRInput.RawButton.Y);
        }

        private void OnDestroy()
        {
        }

        public bool IsUpButtonPressed()
        {
            // 左手トリガーを押した瞬間
            return OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger);
        }

        public bool IsUpButtonReleased()
        {
            // 左手トリガーを離した瞬間
            return OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger);
        }

        public bool IsDownButtonPressed()
        {
            // 右手トリガーを押した瞬間
            return OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger);
        }

        public bool IsDownButtonReleased()
        {
            // 右手トリガーを離した瞬間
            return OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger);
        }

        public void DoVibration(bool isRightHand, float frequency, float amplitude, float durationSec)
        {
            var controller = isRightHand ? OVRInput.Controller.RTouch : OVRInput.Controller.LTouch;
            StartCoroutine(VibrateCoroutine(controller, frequency, amplitude, durationSec));
        }

        private IEnumerator VibrateCoroutine(OVRInput.Controller controller, float frequency, float amplitude, float durationSec)
        {
            OVRInput.SetControllerVibration(frequency, amplitude, controller);
            yield return new WaitForSeconds(durationSec);
            OVRInput.SetControllerVibration(0, 0, controller);
        }

    }

}
