using hakoniwa.ar.bridge;
using hakoniwa.ar.bridge.sharesim;
using hakoniwa.drone;
using hakoniwa.drone.service;
using hakoniwa.objects.core;
using hakoniwa.objects.core.sensors;
using hakoniwa.pdu.core;
using hakoniwa.pdu.interfaces;
using hakoniwa.pdu.msgs.geometry_msgs;
using hakoniwa.pdu.msgs.hako_mavlink_msgs;
using hakoniwa.pdu.msgs.hako_msgs;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class DronePlayerDevice : MonoBehaviour, IHakoniwaArObject, IDroneDisturbableObject
{
    public GameObject body;
    public int debuff_duration_msec = 100;
    private DroneCollision my_collision;
    private DroneControl drone_control;
    private DronePropeller drone_propeller;
    private IHakoniwaArBridge ibridge;
    private BaggageGrabber baggage_grabber;
    private ShareSimClient sharesim_client;
    public bool enable_debuff = false;
    public bool useWebServer = true;

    public bool enable_data_logger = false;
    public string debug_logpath = null;

    public string robotName = "Drone";
    public string pdu_name_propeller = "motor";
    public string pdu_name_pos = "pos";
    private ICameraController camera_controller;
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


    private async void FlushPduPos(UnityEngine.Vector3 unity_pos)
    {
        var pdu_manager = ARBridge.Instance.Get();
        if (pdu_manager == null)
        {
            return;
        }
        /*
         * Position
         */
        INamedPdu npdu_pos = pdu_manager.CreateNamedPdu(robotName, pdu_name_pos);
        if (npdu_pos == null || npdu_pos.Pdu == null)
        {
            throw new Exception($"Can not find npdu: {robotName} {pdu_name_pos}");
        }
        Twist pos = new Twist(npdu_pos.Pdu);
        SetPosition(pos, body.transform.position, body.transform.eulerAngles);
        pdu_manager.WriteNamedPdu(npdu_pos);
        var ret = await pdu_manager.FlushNamedPdu(npdu_pos);
        //Debug.Log("Flush result: " + ret);
    }
    private async void FlushPduPropeller(float c1, float c2, float c3, float c4)
    {
        var pdu_manager = ARBridge.Instance.Get();
        if (pdu_manager == null)
        {
            return;
        }

        /*
         * Propeller
         */
        INamedPdu npdu = pdu_manager.CreateNamedPdu(robotName, pdu_name_propeller);
        if (npdu == null || npdu.Pdu == null)
        {
            throw new Exception($"Can not find npdu: {robotName} {pdu_name_propeller}");
        }

        HakoHilActuatorControls actuator = new HakoHilActuatorControls(npdu.Pdu);
        float[] controls = new float[16];
        controls[0] = c1;
        controls[1] = c2;
        controls[2] = c3;
        controls[3] = c4;
        actuator.controls = controls;
        //float[] temp = npdu.Pdu.GetDataArray<float>("controls");
        //Debug.Log("size: " + temp.Length);
        //Debug.Log("controls[0]: " + temp[0]);

        pdu_manager.WriteNamedPdu(npdu);
        var ret = await pdu_manager.FlushNamedPdu(npdu);
        //Debug.Log("Flush result: " + ret);
    }
    public async Task DeclarePduAsync(string type_name, string robot_name)
    {
        var pdu_manager = ARBridge.Instance.Get();
        if (pdu_manager == null)
        {
            throw new Exception("Can not get Pdu Manager");
        }
        //this.robotName = robot_name;
        var ret = await pdu_manager.DeclarePduForWrite(robotName, pdu_name_pos);
        Debug.Log("declare pdu pos: " + ret);
        ret = await pdu_manager.DeclarePduForWrite(robotName, pdu_name_propeller);
        Debug.Log("declare pdu propeller: " + ret);
    }

    void Start()
    {
        if (useWebServer)
        {
            sharesim_client = ShareSimClient.Instance;
            ibridge = HakoniwaArBridgeDevice.Instance;
        }
        my_collision = this.GetComponentInChildren<DroneCollision>();
        if (my_collision == null) {
            throw new Exception("Can not found collision");
        }
        drone_control = this.GetComponentInChildren<DroneControl>();
        if (drone_control == null)
        {
            throw new Exception("Can not found drone control");
        }
        drone_propeller = this.GetComponentInChildren<DronePropeller>();
        if (drone_propeller == null)
        {
            throw new Exception("Can not found drone propeller");
        }
        my_collision.SetIndex(0);
        baggage_grabber = this.GetComponentInChildren<BaggageGrabber>();
        if (baggage_grabber == null)
        {
            Debug.LogWarning("Can not found BaggageGrabber");
        }

        string droneConfigText = LoadTextFromResources("config/drone/rc/drone_config_0");
        string controllerConfigText = LoadTextFromResources("config/controller/param-api-mixer");

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
         * Camera
         */
        camera_controller = GetComponentInChildren<ICameraController>();
        if (camera_controller != null)
        {
            Debug.Log("Camera is enabled");
            camera_controller.Initialize();
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
        if (ibridge != null && ibridge.GetState() == BridgeState.POSITIONING)
        {
            return;
        }
        drone_control.HandleInput();
        if (camera_controller != null)
        {
            drone_control.HandleCameraControl(camera_controller, null);
        }
    }

    private bool isGrabProcessing = false;
    private bool isReleaseProcessing = false;
    private bool isGrabbed = false;
    private async Task GrabControlAsync()
    {
        var pdu_manager = ARBridge.Instance.Get();
        if (pdu_manager == null)
        {
            return;
        }

        if (drone_control.IsMagnetOn())
        {
            if (!isGrabbed)
            {
                if (!isGrabProcessing) // すでに処理中でなければ実行
                {
                    isGrabProcessing = true;
                    var result = await baggage_grabber.RequestGrab(sharesim_client.GetOwnerId(), pdu_manager);

                    if (result == BaggageGrabber.GrabResult.Success)
                    {
                        isGrabbed = true; // 取得成功時のみ `isGrabbed` を更新
                    }
                    isGrabProcessing = false; // 確実に解除
                }
            }
        }
        else
        {
            if (isGrabbed)
            {
                if (!isReleaseProcessing) // すでに処理中でなければ実行
                {
                    isReleaseProcessing = true;
                    var result = await baggage_grabber.RequestRelease(sharesim_client.GetOwnerId(), pdu_manager);
                    isReleaseProcessing = false; // 確実に解除

                    if (result == BaggageGrabber.ReleaseResult.Success)
                    {
                        isGrabbed = false; // リリース成功時のみ `isGrabbed` を更新
                    }
                }
            }
        }
    }
    void LateUpdate()
    {
        if (camera_controller != null)
        {
            this.camera_controller.UpdateCameraAngle();
        }
    }
    private UnityEngine.Vector3 rosWind;
    private double temperature;
    private async void FixedUpdate()
    {
        DroneServiceRC.PutDisturbance(0, temperature, rosWind.x, rosWind.y, rosWind.z);
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
            if (useWebServer)
            {
                FlushPduPos(unity_pos);
            }
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

        double c1, c2, c3, c4, c5, c6, c7, c8;
        ret = DroneServiceRC.GetControls(0, out c1, out c2, out c3, out c4, out c5, out c6, out c7, out c8);
        if (ret == 0)
        {
            drone_propeller.Rotate((float)c1, (float)c2, (float)c3, (float)c4);
            if (useWebServer)
            {
                FlushPduPropeller((float)c1, (float)c2, (float)c3, (float)c4);
            }
        }
        if (baggage_grabber != null)
        {
            await GrabControlAsync();
        }
        /*
         * Leds
         */
        int internal_state;
        int flight_mode;
        DroneServiceRC.GetInternalState(0, out internal_state);
        DroneServiceRC.GetFlightMode(0, out flight_mode);
        if (leds.Length > 0)
        {
            if (c1 > 0)
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

    private void OnApplicationQuit()
    {
        int ret = DroneServiceRC.Stop();
        Debug.Log("Stop: ret = " + ret);
    }

    private UnityEngine.Vector3 UnityToRos(UnityEngine.Vector3 unityVec)
    {
        return new UnityEngine.Vector3(
            unityVec.x,
            -unityVec.z,
            unityVec.y
        );
    }

    public void ApplyDisturbance(float temp, UnityEngine.Vector3 windVector)
    {
        Debug.Log("ApplyDisturbance: " + windVector);
        rosWind = UnityToRos(windVector);
        temperature = temp;
    }

    public void ResetDisturbance()
    {
        Debug.Log("ResetDisturbance: ");
        rosWind.x = 0;
        rosWind.y = 0;
        rosWind.z = 0;
        temperature = 20;
    }
}
