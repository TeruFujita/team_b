using hakoniwa.pdu.interfaces;
using hakoniwa.pdu.msgs.sensor_msgs;
using hakoniwa.pdu.unity;
using hakoniwa.sim;
using System;
using UnityEngine;

namespace hakoniwa.objects.core.sensors
{
    public struct PointCloudFieldType
    {
        public string name;
        public uint offset;
        /*
            uint8 INT8    = 1
            uint8 UINT8   = 2
            uint8 INT16   = 3
            uint8 UINT16  = 4
            uint8 INT32   = 5
            uint8 UINT32  = 6
            uint8 FLOAT32 = 7
            uint8 FLOAT64 = 8
         */
        public byte datatype; /* FLOAT32 */
        public uint count; /* 1 */
        public PointCloudFieldType(string n, uint off, byte type, uint c)
        {
            this.name = n;
            this.offset = off;
            this.datatype = type;
            this.count = c;
        }
    }
    public struct LiDAR3DParams
    {
        public bool Enabled;
        public int NumberOfChannels;
        public int RotationsPerSecond;
        public int PointsPerSecond;
        public float MaxDistance;
        public float VerticalFOVUpper;
        public float VerticalFOVLower;
        public float HorizontalFOVStart;
        public float HorizontalFOVEnd;
        public bool DrawDebugPoints;

    }
    public interface ILiDAR3DController
    {
        public bool SetParams(LiDAR3DParams param);
        public LiDAR3DParams GetParams();
        public void DoInitialize(string robot_name, IPduManager pduManager);
        public void DoControl(IPduManager pduManager);
    }

    public class Default3DLiDARController : MonoBehaviour, ILiDAR3DController
    {
        public bool Enabled = true;
        public int NumberOfChannels = 16;
        public int RotationsPerSecond = 10;
        public int PointsPerSecond = 10000;
        public float MaxDistance = 10;
        public float VerticalFOVUpper = -15f;
        public float VerticalFOVLower = -25f;
        public float HorizontalFOVStart = -20f;
        public float HorizontalFOVEnd = 20f;
        public bool DrawDebugPoints = true;


        /*
         * パラメータ経緯(MaxHeight, MaxWidth)：
         *  周波数：5Hz
         *  垂直：-30° 〜 30°
         *  水平：-90° 〜 90°
         *  分解能：1° 
         */
        public const int MaxHeight = 61;
        public const int MaxWidth = 181;

        public float deg_interval_h = 1f;
        public float deg_interval_v = 1f;

        public int height = 61;
        public int width = 181;
        public int update_cycle = 1;

        public int PointsPerRotation;
        public int HorizontalPointsPerRotation;
        public float HorizontalRanges;
        public float VerticalRanges;
        public float SecondsPerRotation;

        public bool SetParams(LiDAR3DParams param)
        {
            PointsPerRotation = param.PointsPerSecond / param.RotationsPerSecond;
            HorizontalPointsPerRotation = PointsPerRotation / param.NumberOfChannels;
            HorizontalRanges = param.HorizontalFOVEnd - param.HorizontalFOVStart;
            VerticalRanges = param.VerticalFOVUpper - param.VerticalFOVLower;
            SecondsPerRotation = 1.0f / (float)param.RotationsPerSecond;

            if (param.NumberOfChannels > MaxHeight)
            {
                Debug.LogError("NumberOfChannels is invalid: " + param.NumberOfChannels);
                return false;
            }
            if (HorizontalPointsPerRotation > MaxWidth)
            {
                Debug.LogError("PointsPerRotation(" + PointsPerRotation + ") / NumberOfChannels(" + param.NumberOfChannels + ") is invalid: " + HorizontalPointsPerRotation);
                return false;
            }

            this.height = param.NumberOfChannels;
            this.width = HorizontalPointsPerRotation;
            this.deg_interval_h = HorizontalRanges / HorizontalPointsPerRotation;
            this.deg_interval_v = VerticalRanges / param.NumberOfChannels;
            this.update_cycle = Mathf.RoundToInt(SecondsPerRotation / Time.fixedDeltaTime);

            this.Enabled = param.Enabled;
            this.NumberOfChannels = param.NumberOfChannels;
            this.RotationsPerSecond = param.RotationsPerSecond;
            this.PointsPerSecond = param.PointsPerSecond;
            this.MaxDistance = param.MaxDistance;
            this.VerticalFOVLower = param.VerticalFOVLower;
            this.VerticalFOVUpper = param.VerticalFOVUpper;
            this.HorizontalFOVStart = param.HorizontalFOVStart;
            this.HorizontalFOVEnd = param.HorizontalFOVEnd;
            this.DrawDebugPoints = param.DrawDebugPoints;
            return true;
        }
        public LiDAR3DParams GetParams()
        {
            LiDAR3DParams param = new LiDAR3DParams
            {
                Enabled = this.Enabled,
                NumberOfChannels = this.NumberOfChannels,
                RotationsPerSecond = this.RotationsPerSecond,
                PointsPerSecond = this.PointsPerSecond,
                MaxDistance = this.MaxDistance,
                VerticalFOVUpper = this.VerticalFOVUpper,
                VerticalFOVLower = this.VerticalFOVLower,
                HorizontalFOVStart = this.HorizontalFOVStart,
                HorizontalFOVEnd = this.HorizontalFOVEnd,
                DrawDebugPoints = this.DrawDebugPoints
            };
            return param;
        }

        private GameObject root;
        private GameObject sensor;
        private string root_name;

        readonly public int max_data_array_size = 176656;
        private int point_step = 16;
        private int row_step = 0;
        private bool is_bigendian = false;
        private PointCloudFieldType[] fields =
        {
            new PointCloudFieldType("x", 0, 7, 1),
            new PointCloudFieldType("y", 4, 7, 1),
            new PointCloudFieldType("z", 8, 7, 1),
            new PointCloudFieldType("intensity", 12, 7, 1),
        };
        private byte[] data;

        public float view_cycle_h = 2;
        public float view_cycle_v = 2;

        private float GetSensorValue(float degreeYaw, float degreePitch, bool debug)
        {
            // センサーの基本の前方向を取得
            Vector3 forward = sensor.transform.forward;

            // Quaternionを使用してヨー、ピッチ、ロールを一度に計算
            Quaternion yawRotation = Quaternion.AngleAxis(degreeYaw, sensor.transform.up);
            Quaternion pitchRotation = Quaternion.AngleAxis(degreePitch, yawRotation * sensor.transform.right);

            // 最終的な回転を適用
            Quaternion finalRotation = yawRotation * pitchRotation;
            Vector3 finalDirection = finalRotation * forward;

            RaycastHit hit;

            if (Physics.Raycast(sensor.transform.position, finalDirection, out hit, MaxDistance))
            {
                if (debug)
                {
                    Debug.DrawRay(sensor.transform.position, finalDirection * hit.distance, Color.red, 0.05f, false);
                }
                return hit.distance;
            }
            else
            {
                if (debug)
                {
                    Debug.DrawRay(sensor.transform.position, finalDirection * MaxDistance, Color.green, 0.05f, false);
                }
                return MaxDistance;
            }
        }


        private void ScanEnvironment()
        {
            int totalPoints = height * width;
            int dataIndex = 0;
            float fixedIntensity = 1.0f;

            bool debug_h = false;
            bool debug_v = false;
            int i_h = 0;
            int i_v = 0;
            for (float pitch = VerticalFOVLower; pitch <= VerticalFOVUpper; pitch += deg_interval_v)
            {
                debug_v = ((i_v % view_cycle_v) == 0);
                i_v++;
                i_h = 0;
                for (float yaw = HorizontalFOVStart; yaw <= HorizontalFOVEnd; yaw += deg_interval_h)
                {
                    debug_h = ((i_h % view_cycle_h) == 0);
                    i_h++;
                    float distance = GetSensorValue(yaw, pitch, (DrawDebugPoints && debug_h && debug_v));
                    Vector3 point = CalculatePoint(distance, yaw, pitch);

                    Buffer.BlockCopy(BitConverter.GetBytes(point.z), 0, data, dataIndex, 4);//x
                    Buffer.BlockCopy(BitConverter.GetBytes(-point.x), 0, data, dataIndex + 4, 4);//y
                    Buffer.BlockCopy(BitConverter.GetBytes(point.y), 0, data, dataIndex + 8, 4);//z
                    Buffer.BlockCopy(BitConverter.GetBytes(fixedIntensity), 0, data, dataIndex + 12, 4);

                    dataIndex += point_step;
                }
            }
        }
        private Vector3 CalculatePoint(float distance, float degreeYaw, float degreePitch)
        {
            // ユーラー角を四元数に変換
            Quaternion rotation = Quaternion.Euler(degreePitch, degreeYaw, 0);

            // ローカル座標系での前方ベクトル
            Vector3 forwardInLocal = rotation * this.sensor.transform.forward;

            // 衝突点の計算
            Vector3 collisionPoint = forwardInLocal * distance;
            return collisionPoint;
        }
        private void UpdateLidarPdu(hakoniwa.pdu.msgs.sensor_msgs.PointCloud2 point_cloud2)
        {
            TimeStamp.Set(point_cloud2.header);
            point_cloud2.header.frame_id = "front_lidar_frame";
            point_cloud2.height = (uint)this.height;
            point_cloud2.width = (uint)this.width;
            point_cloud2.is_bigendian = this.is_bigendian;
            point_cloud2.fields = pointFields;
            point_cloud2.point_step = (uint)this.point_step;
            point_cloud2.row_step = (uint)this.row_step;
            point_cloud2.data = this.data;
            point_cloud2.is_dense = true;
        }
        public const string pdu_name_lidar_point_cloud = "lidar_points";
        public const string pdu_name_lidar_pos = "lidar_pos";
        private string robotName;
        private PointField[] pointFields;
        public void DoInitialize(string robot_name, IPduManager pduManager)
        {
            this.robotName = robot_name;
            this.sensor = this.gameObject;
            this.width = Mathf.CeilToInt((HorizontalFOVEnd - HorizontalFOVStart) / deg_interval_h) + 1;
            this.height = Mathf.CeilToInt((VerticalFOVUpper - VerticalFOVLower) / deg_interval_v) + 1;
            this.row_step = this.width * this.point_step;

            if ((this.row_step * this.height) > this.max_data_array_size)
            {
                throw new ArgumentException("ERROR: oveflow data size: " + (this.row_step * this.height) + " max: " + this.max_data_array_size);
            }
            this.data = new byte[this.row_step * this.height];

            INamedPdu pdu = pduManager.CreateNamedPdu(robotName, pdu_name_lidar_point_cloud);
            if (pdu == null)
            {
                throw new ArgumentException($"ERROR: can not find pdu({robotName}/{pdu_name_lidar_point_cloud})");
            }
            var point_cloud2 = new hakoniwa.pdu.msgs.sensor_msgs.PointCloud2(pdu);
            pointFields = new PointField[this.fields.Length];
            for (int i = 0; i < this.fields.Length; i++)
            {
                PointField field = new PointField(pduManager.CreatePduByType("fields", "sensor_msgs", "PointField"));
                field.name = this.fields[i].name;
                field.offset = (uint)this.fields[i].offset;
                field.datatype = (byte)this.fields[i].datatype;
                field.count = (uint)this.fields[i].count;
                pointFields[i] = field;
            }
            point_cloud2.fields = pointFields;
            pduManager.WriteNamedPdu(pdu);
            pduManager.FlushNamedPdu(pdu);
        }
        private int count = 0;
        public void DoControl(IPduManager pduManager)
        {
            if (this.Enabled == false)
            {
                return;
            }
            this.count++;
            if (this.count < this.update_cycle)
            {
                return;
            }
            this.count = 0;
            this.ScanEnvironment();


            INamedPdu pdu_point_cloud2 = pduManager.CreateNamedPdu(robotName, pdu_name_lidar_point_cloud);
            if (pdu_point_cloud2 == null)
            {
                throw new ArgumentException($"ERROR: can not find pdu({robotName}/{pdu_name_lidar_point_cloud})");
            }
            var point_cloud2 = new hakoniwa.pdu.msgs.sensor_msgs.PointCloud2(pdu_point_cloud2);
            this.UpdateLidarPdu(point_cloud2);
            pduManager.WriteNamedPdu(pdu_point_cloud2);
            pduManager.FlushNamedPdu(pdu_point_cloud2);


            INamedPdu pdu_lidar_pos = pduManager.CreateNamedPdu(robotName, pdu_name_lidar_pos);
            if (pdu_lidar_pos == null)
            {
                throw new ArgumentException($"ERROR: can not find pdu({robotName}/{pdu_name_lidar_pos})");
            }
            var lidar_pos = new hakoniwa.pdu.msgs.geometry_msgs.Twist(pdu_lidar_pos);
            this.UpdatePosPdu(lidar_pos);
            pduManager.WriteNamedPdu(pdu_lidar_pos);
            pduManager.FlushNamedPdu(pdu_lidar_pos);
        }

        private void UpdatePosPdu(hakoniwa.pdu.msgs.geometry_msgs.Twist lidar_pos)
        {
            //Unity FRAME TO ROS FRAME
            lidar_pos.linear.x = (double)this.sensor.transform.position.z;
            lidar_pos.linear.y = -(double)this.sensor.transform.position.x;
            lidar_pos.linear.z = (double)this.sensor.transform.position.y;

            var euler = this.sensor.transform.transform.eulerAngles;
            lidar_pos.angular.x = -(double)((MathF.PI / 180) * euler.z);
            lidar_pos.angular.y = (double)((MathF.PI / 180) * euler.x);
            lidar_pos.angular.z = -(double)((MathF.PI / 180) * euler.y);
        }
    }

}
