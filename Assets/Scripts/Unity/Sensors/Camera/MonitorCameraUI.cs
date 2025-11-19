using System.Collections.Generic;
using hakoniwa.objects.core.sensors;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace hakoniwa.objects.ui
{
    public class MonitorCameraUI : MonoBehaviour
    {
        private MonitorCameraManager cameraManager;
        public RawImage cameraDisplay;
        public TMP_Dropdown cameraDropdown;
        public Button reloadButton;

        private void Initialize()
        {
            cameraManager = MonitorCameraManager.Instance;
            UpdateCameraList();
            cameraDropdown.onValueChanged.AddListener(OnCameraSelected);
            reloadButton.onClick.AddListener(() => {
                cameraManager.ReloadCameras();
                UpdateCameraList();
            });
        }

        void Start()
        {
            Initialize();
        }

        void UpdateCameraList()
        {
            cameraDropdown.ClearOptions();
            List<string> cameraNames = cameraManager.GetCameraNames();
            cameraDropdown.AddOptions(cameraNames);
            if (cameraNames.Count > 0)
            {
                cameraDropdown.value = 0;
                OnCameraSelected(0);
            }
        }
        void OnCameraSelected(int index)
        {
            string selectedCamera = cameraDropdown.options[index].text;
            var texture = cameraManager.GetCameraRenderTexture(selectedCamera);
            if (texture != null)
            {
                cameraDisplay.texture = texture;
            }

        }
    }
}
