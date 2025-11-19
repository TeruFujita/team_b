using System.Collections.Generic;
using hakoniwa.objects.core.sensors;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace hakoniwa.objects.ui
{
    public class MonitorCameraUIArrays : MonoBehaviour
    {
        private MonitorCameraManager cameraManager;
        public List<RawImage> cameraDisplays;

        private void Initialize()
        {
            cameraManager = MonitorCameraManager.Instance;
        }

        void Start()
        {
            Initialize();
        }
        private void Update()
        {
            int i = 0;
            foreach (var camera_name in cameraManager.GetCameraNames())
            {
                var texture = cameraManager.GetCameraRenderTexture(camera_name);
                if (texture != null)
                {
                    cameraDisplays[i].texture = texture;
                }
                i++;
            }
        }

    }
}
