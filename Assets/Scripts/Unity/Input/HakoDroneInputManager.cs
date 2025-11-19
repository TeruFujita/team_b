using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace hakoniwa.objects.core
{
    public class HakoDroneInputManager : MonoBehaviour, IDroneInput
    {
        public static HakoDroneInputManager Instance { get; private set; }
        private DroneInputActions inputActions;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject); // シーンをまたいで保持
            inputActions = new DroneInputActions();
            inputActions.Gameplay.Enable();
        }

        public Vector2 GetLeftStickInput()
        {
            return inputActions.Gameplay.LeftStick.ReadValue<Vector2>();
        }

        public Vector2 GetRightStickInput()
        {
            return inputActions.Gameplay.RightStick.ReadValue<Vector2>();
        }

        public bool IsAButtonPressed()
        {
            return inputActions.Gameplay.Sbutton.WasPressedThisFrame();
        }

        public bool IsAButtonReleased()
        {
            return inputActions.Gameplay.Sbutton.WasReleasedThisFrame();
        }
        public bool IsBButtonPressed()
        {
            return inputActions.Gameplay.Ebutton.WasPressedThisFrame();
        }
        public bool IsBButtonReleased()
        {
            return inputActions.Gameplay.Ebutton.WasReleasedThisFrame();
        }
        public bool IsXButtonPressed()
        {
            return inputActions.Gameplay.Nbutton.WasPressedThisFrame();
        }
        public bool IsXButtonReleased()
        {
            return inputActions.Gameplay.Nbutton.WasReleasedThisFrame();
        }
        public bool IsYButtonPressed()
        {
            return inputActions.Gameplay.Wbutton.WasPressedThisFrame();
        }

        public bool IsYButtonReleased()
        {
            return inputActions.Gameplay.Wbutton.WasReleasedThisFrame();
        }
        private void OnDestroy()
        {
            inputActions.Gameplay.Disable();
        }

        public bool IsUpButtonPressed()
        {
            return inputActions.Gameplay.Up.WasPressedThisFrame();
        }

        public bool IsUpButtonReleased()
        {
            return inputActions.Gameplay.Up.WasReleasedThisFrame() ;
        }

        public bool IsDownButtonPressed()
        {
            return inputActions.Gameplay.Down.WasPressedThisFrame();
        }

        public bool IsDownButtonReleased()
        {
            return inputActions.Gameplay.Down.WasReleasedThisFrame();
        }


        public void DoVibration(bool isRightHand, float frequency, float amplitude, float durationSec)
        {
            if (Gamepad.current != null)
            {
                float lowFreq = isRightHand ? 0.0f : amplitude;   // 左手→低周波
                float highFreq = isRightHand ? amplitude : 0.0f;  // 右手→高周波

                Gamepad.current.SetMotorSpeeds(lowFreq, highFreq);
                StartCoroutine(StopVibrationAfter(durationSec));
            }
        }

        private IEnumerator StopVibrationAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            if (Gamepad.current != null)
            {
                Gamepad.current.SetMotorSpeeds(0.0f, 0.0f);
            }
        }
    }
}
