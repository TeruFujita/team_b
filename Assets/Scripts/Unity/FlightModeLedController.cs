using UnityEngine;

namespace hakoniwa.objects.core
{
    public class FlightModeLedController : MonoBehaviour
    {
        public enum FlightMode
        {
            ATTI,
            GPS
        }

        [Header("LED Settings")]
        [SerializeField]
        private Renderer ledRenderer;

        [Header("Mode Materials")]
        public Material attiMaterial;
        public Material gpsMaterial;
        public Material offMaterial;

        [Header("Blink Settings")]
        public float attiBlinkInterval = 0.5f;
        public float gpsBlinkInterval = 0.5f;

        private FlightMode currentMode = FlightMode.GPS;
        private float blinkTimer = 0f;
        private bool ledOn = false;

        private void Start()
        {
            UpdateLedImmediate();
        }

        public void SetMode(FlightMode mode)
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
                case FlightMode.ATTI:
                    UpdateBlink(attiBlinkInterval, attiMaterial);
                    break;

                case FlightMode.GPS:
                    UpdateBlink(gpsBlinkInterval, gpsMaterial);
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
                case FlightMode.ATTI:
                    SetLedOn(false, attiMaterial);
                    break;
                case FlightMode.GPS:
                    SetLedOn(false, gpsMaterial);
                    break;
            }
        }

        private void SetLedOn(bool on, Material modeMaterial)
        {
            if (ledRenderer == null) return;

            if (on)
            {
                ledRenderer.sharedMaterial = modeMaterial;
            }
            else
            {
                ledRenderer.sharedMaterial = offMaterial;  // 消灯
            }
        }
    }
}
