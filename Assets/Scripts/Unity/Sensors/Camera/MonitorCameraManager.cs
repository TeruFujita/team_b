using System;
using System.Collections.Generic;
using System.IO;
using hakoniwa.objects.core.frame;
using UnityEngine;

namespace hakoniwa.objects.core.sensors
{
    public class MonitorCameraManager : MonoBehaviour
    {
        public static MonitorCameraManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            hakoCameras = new Dictionary<string, HakoCamera>();  // ここで初期化
            SetCameras();

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public string monitorCameraConfigPath = "./monitor_camera_config.json";
        public GameObject cameraPrefab;
        private Dictionary<string, HakoCamera> hakoCameras;

        public string saveDirPath = ".";
        public void GetAndSaveCameraImages(string image_name)
        {
            foreach (var camera_name in hakoCameras.Keys)
            {
                var camera_data = hakoCameras[camera_name].GetImage("png");
                string filePath = Path.Combine(saveDirPath, $"{image_name}_{camera_name}.png");
                try
                {
                    File.WriteAllBytes(filePath, camera_data);
                    Debug.Log($"Image saved: {filePath}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to save image: {e.Message}");
                }

            }
        }
        public List<string> GetCameraNames()
        {
            return new List<string>(hakoCameras.Keys);
        }

        public RenderTexture GetCameraRenderTexture(string cameraName)
        {
            if (hakoCameras.TryGetValue(cameraName, out var camera))
            {
                return camera.GetRenderTexture();
            }
            return null;
        }
        public string GetEncodeType(string cameraName)
        {
            if (hakoCameras.TryGetValue(cameraName, out var camera))
            {
                return camera.GetEncodeType();
            }
            return null;
        }
        public byte[] GetImage(string cameraName, string encode_type)
        {
            if (hakoCameras.TryGetValue(cameraName, out var camera))
            {
                return camera.GetImage(encode_type);
            }
            return null;
        }
        public (Vector3 position, Vector3 rotation, float fov) GetCameraInfo(string cameraName)
        {
            if (hakoCameras.TryGetValue(cameraName, out var camera))
            {
                return (camera.transform.position, camera.transform.eulerAngles, camera.GetFov());
            }
            return (Vector3.zero, Vector3.zero, 0);
        }


        public void ReloadCameras()
        {
            RemoveCamers();
            SetCameras();
        }
        private void RemoveCamers()
        {
            foreach (var hakoCamera in hakoCameras.Values)
            {
                if (hakoCamera != null)
                {
                    Destroy(hakoCamera.gameObject);
                }
            }
            hakoCameras.Clear();
        }
        private void SetCameras()
        {
            var cameraConfig = CameraConfigLoader.LoadConfig(monitorCameraConfigPath);

            if (cameraConfig == null)
            {
                Debug.LogError("Failed to load camera configuration.");
                return;
            }
            if (cameraPrefab == null)
            {
                Debug.LogError("Camera prefab is not set.");
                return;
            }
            foreach (var camData in cameraConfig.monitor_cameras)
            {
                GameObject newCamera = Instantiate(cameraPrefab);
                HakoCamera hakoCamera = newCamera.GetComponent<HakoCamera>();

                if (hakoCamera == null)
                {
                    Debug.LogError($"Failed to get HakoCamera component for {camData.pdu_info.robot_name}");
                    Destroy(newCamera);
                    continue;
                }

                newCamera.transform.SetParent(this.transform);
                newCamera.transform.name = camData.pdu_info.robot_name;

                Vector3 position = new Vector3(camData.coordinate_system.position.x, camData.coordinate_system.position.y, camData.coordinate_system.position.z);
                Vector3 rotation = new Vector3(camData.coordinate_system.orientation.roll, camData.coordinate_system.orientation.pitch, camData.coordinate_system.orientation.yaw);
                position = FrameConvertor.PosRos2Unity(position);
                rotation = FrameConvertor.EulerRosDeg2UnityDeg(rotation);

                hakoCamera.ConfigureCamera(
                    camData.pdu_info.robot_name,
                    camData.camera_type,
                    camData.encode_type,
                    camData.coordinate_system.type,
                    camData.coordinate_system.target,
                    position,
                    rotation,
                    camData.fov.horizontal,
                    camData.resolution.width,
                    camData.resolution.height
                );

                hakoCameras[camData.pdu_info.robot_name] = hakoCamera;
            }
        }


    }
}