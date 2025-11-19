using UnityEngine;

namespace hakoniwa.objects.core.frame
{
    public class FrameConvertor
    {
        public static Vector3 PosRos2Unity(Vector3 rosVec)
        {
            return new Vector3(
                -rosVec.y,
                rosVec.z,
                rosVec.x);
        }
        public static Vector3 EulerRosRad2UnityDeg(Vector3 rosVec)
        {
            return new Vector3(
                rosVec.y * Mathf.Rad2Deg,
                -rosVec.z * Mathf.Rad2Deg,
                -rosVec.x * Mathf.Rad2Deg);
        }
        public static Vector3 EulerRosDeg2UnityDeg(Vector3 rosVec)
        {
            return new Vector3(
                rosVec.y,
                -rosVec.z,
                -rosVec.x);
        }
        public static Vector3 PosUnity2Ros(Vector3 unityVec)
        {
            return new Vector3(
                unityVec.z,   // Z → X
                -unityVec.x,  // X → -Y
                unityVec.y    // Y → Z
            );
        }
        public static Vector3 EulerUnityDeg2RosRad(Vector3 unityVec)
        {
            return new Vector3(
                -unityVec.z * Mathf.Deg2Rad,
                unityVec.x * Mathf.Deg2Rad, 
                -unityVec.y * Mathf.Deg2Rad
            );
        }

    }
}

