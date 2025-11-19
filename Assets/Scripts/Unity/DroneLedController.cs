using UnityEngine;

namespace hakoniwa.objects.core
{
    public class DroneLedController : MonoBehaviour
    {
        public enum DroneMode
        {
            DISARM,
            TAKEOFF,
            HOVER,
            LANDING
        }

        [Header("LED Settings")]
        [SerializeField]
        private Renderer ledRenderer;

        [Header("Mode Materials")]
        public Material disarmMaterial;
        public Material takeoffMaterial;
        public Material hoverMaterial;
        public Material landingMaterial;

        [Header("Blink Settings")]
        public float takeoffBlinkInterval = 0.5f;
        public float landingBlinkInterval = 0.5f;

        private DroneMode currentMode = DroneMode.DISARM;
        private float blinkTimer = 0f;
        private bool ledOn = false;

        private void Start()
        {
            UpdateLedImmediate();
        }

        public void SetMode(DroneMode mode)
        {
            if (currentMode != mode)
            {
                currentMode = mode;
                blinkTimer = 0f;
                ledOn = false;
                UpdateLedImmediate();
            }
        }

        private void Update()
        {
            switch (currentMode)
            {
                case DroneMode.DISARM:
                    SetLedOn(false, disarmMaterial);
                    break;

                case DroneMode.HOVER:
                    SetLedOn(true, hoverMaterial);
                    break;

                case DroneMode.TAKEOFF:
                    UpdateBlink(takeoffBlinkInterval, takeoffMaterial);
                    break;

                case DroneMode.LANDING:
                    UpdateBlink(landingBlinkInterval, landingMaterial);
                    break;
            }
        }

        private void UpdateBlink(float interval, Material modeMaterial)
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= interval)
            {
                blinkTimer = 0f;
                ledOn = !ledOn;
                SetLedOn(ledOn, modeMaterial);
            }
        }

        private void UpdateLedImmediate()
        {
            switch (currentMode)
            {
                case DroneMode.DISARM:
                    SetLedOn(false, disarmMaterial);
                    break;

                case DroneMode.HOVER:
                    SetLedOn(true, hoverMaterial);
                    break;

                case DroneMode.TAKEOFF:
                case DroneMode.LANDING:
                    SetLedOn(false, takeoffMaterial); // 初期は OFF
                    break;
            }
        }

        private void SetLedOn(bool on, Material modeMaterial)
        {
            if (ledRenderer == null) return;

            if (on)
            {
                if (ledRenderer.sharedMaterial != modeMaterial)
                {
                    ledRenderer.sharedMaterial = modeMaterial;
                }
            }
            else
            {
                if (ledRenderer.sharedMaterial != disarmMaterial)
                {
                    ledRenderer.sharedMaterial = disarmMaterial; // OFF時は DISARM 用マテリアル
                }
            }
        }
    }
}
