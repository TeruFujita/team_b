using hakoniwa.drone.service;
using hakoniwa.objects.core;
using hakoniwa.pdu.msgs.geometry_msgs;
using hakoniwa.pdu.msgs.hako_msgs;
using System;
using System.Reflection;
using UnityEngine;

namespace hakoniwa.drone
{
    public class DronePlayer : MonoBehaviour, IDroneBatteryStatus, ISimTime, IMovableObject
    {
        public GameObject body;
        public int debuff_duration_msec = 100;
        private DroneCollision my_collision;
        private DroneControl drone_control;
        private DronePropeller drone_propeller;
        public bool enable_debuff = false;

        public bool enable_data_logger = false;
        public string debug_logpath = null;

        public string robotName = "Drone";
        public string pdu_name_propeller = "motor";
        public string pdu_name_pos = "pos";
        public DroneLedController[] leds;
        public FlightModeLedController[] flight_mode_leds;
        public PropellerWindController[] propeller_winds;

        private void SetPosition(Twist pos, UnityEngine.Vector3 unity_pos, UnityEngine.Vector3 unity_rot)
        {
            pos.linear.x = unity_pos.z;
            pos.linear.y = -unity_pos.x;
            pos.linear.z = unity_pos.y;

            pos.angular.x = -Mathf.Deg2Rad * unity_rot.z;
            pos.angular.y = Mathf.Deg2Rad * unity_rot.x;
            pos.angular.z = -Mathf.Deg2Rad * unity_rot.y;
        }

        void Start()
        {
            my_collision = this.GetComponentInChildren<DroneCollision>();
            if (my_collision != null)
            {
                my_collision.SetIndex(0);
            }
            drone_control = this.GetComponentInChildren<DroneControl>();
            if (drone_control == null)
            {
                throw new Exception("Can not found drone control");
            }
            drone_propeller = this.GetComponentInChildren<DronePropeller>();
            if (drone_propeller == null)
            {
                Debug.Log("Can not found drone propeller");
            }

            //string droneConfigText = LoadTextFromResources("config/drone/mujoco/drone_config_0");
            //string controllerConfigText = LoadTextFromResources("config/controller/param-api-mixer-mujoco");
            string droneConfigText = LoadTextFromResources("config/drone/rc/drone_config_0");
            string filename = "param-api-mixer";
            string controllerConfigText = LoadTextFromResources("config/controller/" + filename);

            if (string.IsNullOrEmpty(droneConfigText))
            {
                throw new Exception("Failed to load droneConfigText from Resources.");
            }

            if (string.IsNullOrEmpty(controllerConfigText))
            {
                throw new Exception("Failed to load controllerConfigText from Resources.");
            }
            int ret = -1;
            if (debug_logpath.Length == 0)
            {
                ret = DroneServiceRC.InitSingle(droneConfigText, controllerConfigText, enable_data_logger, null);

            }
            else
            {
                ret = DroneServiceRC.InitSingle(droneConfigText, controllerConfigText, enable_data_logger, debug_logpath);

            }
            if (enable_debuff)
            {
                DroneServiceRC.SetDebuffOnCollision(0, debuff_duration_msec);
                Debug.Log("InitSingle: ret = " + ret);

                if (ret != 0)
                {
                    throw new Exception("Can not Initialize DroneService RC with InitSingle: debug_logpath= " + debug_logpath);
                }
            }
            /*
             * Leds
             */
            if (leds.Length > 0)
            {
                foreach (var led in leds)
                {
                    led.SetMode(DroneLedController.DroneMode.DISARM);
                }
            }
            if (flight_mode_leds.Length > 0)
            {
                foreach (var led in flight_mode_leds)
                {
                    led.SetMode(FlightModeLedController.FlightMode.GPS);
                }
            }
            /*
             * Propeller Winds
             */
            if (propeller_winds.Length > 0)
            {
                foreach (var wind in propeller_winds)
                {
                    wind.SetWindVelocityFromRos(UnityEngine.Vector3.zero);
                }
            }
            // DroneServiceRC.Startの呼び出し
            ret = DroneServiceRC.Start();
            Debug.Log("Start: ret = " + ret);

            if (ret != 0)
            {
                throw new Exception("Can not Start DroneService RC");
            }
        }

        private string LoadTextFromResources(string resourcePath)
        {
            TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
            return textAsset != null ? textAsset.text : null;
        }

        // Update is called once per frame
        void Update()
        {
            drone_control.HandleInput();
        }




        private void FixedUpdate()
        {
            // 現在位置を記録
            for (int i = 0; i < 20; i++)
            {
                DroneServiceRC.Run();
            }

            double x, y, z;
            int ret = DroneServiceRC.GetPosition(0, out x, out y, out z);
            if (ret == 0)
            {
                UnityEngine.Vector3 unity_pos = new UnityEngine.Vector3();
                unity_pos.z = (float)x;
                unity_pos.x = -(float)y;
                unity_pos.y = (float)z;
                body.transform.position = unity_pos;
            }
            double roll, pitch, yaw;
            ret = DroneServiceRC.GetAttitude(0, out roll, out pitch, out yaw);
            if (ret == 0)
            {
                float rollDegrees = Mathf.Rad2Deg * (float)roll;
                float pitchDegrees = Mathf.Rad2Deg * (float)pitch;
                float yawDegrees = Mathf.Rad2Deg * (float)yaw;

                UnityEngine.Quaternion rotation = UnityEngine.Quaternion.Euler(pitchDegrees, -yawDegrees, -rollDegrees);
                body.transform.rotation = rotation;
            }

            float propellerRotation = 0;
            if (drone_propeller != null)
            {
                double c1, c2, c3, c4, c5, c6, c7, c8;
                ret = DroneServiceRC.GetControls(0, out c1, out c2, out c3, out c4, out c5, out c6, out c7, out c8);
                if (ret == 0)
                {
                    drone_propeller.Rotate((float)c1, (float)c2, (float)c3, (float)c4);
                }
                propellerRotation = (float)c1;
            }
            RunBatteryStatus();
            /*
             * Leds
             */
            int internal_state;
            int flight_mode;
            DroneServiceRC.GetInternalState(0, out internal_state);
            DroneServiceRC.GetFlightMode(0, out flight_mode);
            if (leds.Length > 0)
            {
                if (propellerRotation > 0)
                {
                    foreach (var led in leds)
                    {
                        switch (internal_state)
                        {
                            case 0:
                                led.SetMode(DroneLedController.DroneMode.TAKEOFF);
                                break;
                            case 1:
                                led.SetMode(DroneLedController.DroneMode.HOVER);
                                break;
                            case 2:
                                led.SetMode(DroneLedController.DroneMode.LANDING);
                                break;
                            default:
                                break;
                        }
                    }
                }
                else
                {
                    foreach (var led in leds)
                    {
                        led.SetMode(DroneLedController.DroneMode.DISARM);
                    }
                }
            }
            if (flight_mode_leds.Length > 0)
            {
                foreach (var led in flight_mode_leds)
                {
                    if (flight_mode == 0)
                    {
                        led.SetMode(FlightModeLedController.FlightMode.ATTI);
                    }
                    else
                    {
                        led.SetMode(FlightModeLedController.FlightMode.GPS);
                    }
                }
            }
            /*
             * Propeller wind
             */
            if (propeller_winds.Length > 0)
            {
                UnityEngine.Vector3 rosWind = UnityEngine.Vector3.zero;
                DroneServiceRC.GetPropellerWind(0, out rosWind);
                //Debug.Log("rosWind: " + rosWind);
                foreach (var wind in propeller_winds)
                {
                    wind.SetWindVelocityFromRos(rosWind);
                }
            }
        }
        private hakoniwa.drone.service.DroneServiceRC.BatteryStatus battery_status;
        private void RunBatteryStatus()
        {
            var ret = DroneServiceRC.TryGetBatteryStatus(0, out battery_status);
            if (!ret)
            {
                Debug.LogWarning("Can not read battery status");
            }

        }

        private void OnApplicationQuit()
        {
            int ret = DroneServiceRC.Stop();
            Debug.Log("Stop: ret = " + ret);
        }

        public double get_full_voltage()
        {
            return battery_status.FullVoltage;
        }

        public double get_curr_voltage()
        {
            return battery_status.CurrentVoltage;
        }

        public uint get_status()
        {
            return battery_status.Status;
        }

        public uint get_cycles()
        {
            return battery_status.ChargeCycles;
        }

        public double get_temperature()
        {
            return battery_status.CurrentTemperature;
        }

        public long GetWorldTime()
        {
            return (long)DroneServiceRC.GetTimeUsec(0);
        }

        public UnityEngine.Vector3 GetPosition()
        {
            return body.transform.position;
        }
        public UnityEngine.Vector3 GetEulerDeg()
        {
            return body.transform.eulerAngles;
        }

        public double get_atmospheric_pressure()
        {
            return 1.0;
        }
    }
}
