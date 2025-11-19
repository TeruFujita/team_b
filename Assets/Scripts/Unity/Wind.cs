using UnityEngine;

namespace hakoniwa.objects.core
{
    public class Wind : MonoBehaviour
    {
        public GameObject stick;
        public GameObject drone;
        public GameObject wind_state;
        public GameObject wind_spinner;
        public float maxSpinSpeed = 360f;
        public Vector3 wind_direction;

        public float baseRotateSpeed = 2.0f;
        public float maxRotateSpeed = 10.0f;
        public float maxWindStrength = 2.0f;

        void Update()
        {
            if (stick == null || drone == null || wind_state == null)
                return;

            float windStrength = wind_direction.magnitude;
            float speedRatio = Mathf.Clamp01(windStrength / maxWindStrength);
            float currentRotateSpeed = Mathf.Lerp(baseRotateSpeed, maxRotateSpeed, speedRatio);


            if (windStrength == 0f)
            {
            }
            else
            {
                Quaternion targetRotation;
                Transform droneTransform = drone.transform;
                Vector3 localWindDir = droneTransform.InverseTransformDirection(wind_direction.normalized);
                // 水平方向に投影（Y軸成分を無視）
                localWindDir.y = 0f;
                if (localWindDir.sqrMagnitude > 0.0001f)
                {
                    localWindDir.Normalize();
                    targetRotation = Quaternion.LookRotation(localWindDir, Vector3.up);
                    Quaternion currentRotation = stick.transform.localRotation;

                    // YawのみにするためにY軸回転だけ抽出・合成
                    float targetYaw = targetRotation.eulerAngles.y;
                    Vector3 currentEuler = currentRotation.eulerAngles;
                    Quaternion newRotation = Quaternion.Euler(currentEuler.x, targetYaw, currentEuler.z);

                    stick.transform.localRotation = Quaternion.Slerp(
                        currentRotation,
                        newRotation,
                        Time.deltaTime * currentRotateSpeed
                    );


                    float spinSpeed = maxSpinSpeed * speedRatio;
                    float direction = 1f;

                    // Y軸で回転（風向きに応じて回転方向を切り替え）
                    wind_spinner.transform.Rotate(Vector3.up, direction * spinSpeed * Time.deltaTime, Space.Self);
                }
            }

        }
    }

}
