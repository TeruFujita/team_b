using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace hakoniwa.objects.core.sensors
{

    [Serializable]
    public class CameraConfig
    {
        public string pdu_path;
        public List<MonitorCamera> monitor_cameras;
    }

    [Serializable]
    public class MonitorCamera
    {
        public PduInfo pdu_info;
        public UiPosition ui_position;
        public CoordinateSystem coordinate_system;
        public Fov fov;
        public Resolution resolution;
        public string camera_type;
        public string encode_type;
        public string trigger_type;
    }

    [Serializable]
    public class PduInfo
    {
        public string robot_name;
    }

    [Serializable]
    public class CoordinateSystem
    {
        public string type;
        public string target;
        public Position position;
        public Rotation orientation;
    }

    [Serializable]
    public class Position
    {
        public float x, y, z;
    }

    [Serializable]
    public class Rotation
    {
        public float roll, pitch, yaw;
    }

    [Serializable]
    public class Fov
    {
        public float horizontal, vertical;
    }

    [Serializable]
    public class Resolution
    {
        public int width, height;
    }
    [Serializable]
    public class UiPosition
    {
        public int x, y;
    }
    public class CameraConfigLoader
    {

        static public CameraConfig LoadConfig(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                Debug.LogError($"Config file not found: {jsonFilePath}");
                return null;
            }

            string jsonText = File.ReadAllText(jsonFilePath);
            var cameraConfig = JsonUtility.FromJson<CameraConfig>(jsonText);

            Debug.Log($"Loaded {cameraConfig.monitor_cameras.Count} cameras from JSON");
            return cameraConfig;
        }
        static public void DebugPrint(CameraConfig cameraConfig)
        {
            // カメラ設定のデバッグ表示
            Debug.Log($"PDU Path: {cameraConfig.pdu_path}");
            Debug.Log($"Number of Cameras: {cameraConfig.monitor_cameras.Count}");

            foreach (var camera in cameraConfig.monitor_cameras)
            {
                Debug.Log($"Camera: {camera.pdu_info.robot_name}");
                Debug.Log($" - UI Position: ({camera.ui_position.x}, {camera.ui_position.y})");
                Debug.Log($" - Type: {camera.camera_type}");
                Debug.Log($" - Target: {camera.coordinate_system.target}");
                Debug.Log($" - Position: ({camera.coordinate_system.position.x}, {camera.coordinate_system.position.y}, {camera.coordinate_system.position.z})");
                Debug.Log($" - Rotation: (Roll: {camera.coordinate_system.orientation.roll}, Pitch: {camera.coordinate_system.orientation.pitch}, Yaw: {camera.coordinate_system.orientation.yaw})");
                Debug.Log($" - FOV: Horizontal {camera.fov.horizontal}, Vertical {camera.fov.vertical}");
                Debug.Log($" - Resolution: {camera.resolution.width}x{camera.resolution.height}");
                Debug.Log($" - Trigger Type: {camera.trigger_type}");
            }
        }
    }
}
