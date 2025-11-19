using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace hakoniwa.objects.core
{
    public class BatteryUI : MonoBehaviour
    {
        public List<Image> batteryBars; // BatteryBar1〜10のImageコンポーネントをリストで設定

        private float fullVoltage = 14.8f;
        private float currentVoltage = 9.0f;
        private float temperature = 0.0f;
        private float pressure = 1.0f;
        public Text fullVoltageText;
        public Text currVoltageText;
        public Text percentageText;
        public Text tempText;
        public Text pressureText;
        public GameObject battery;
        private IDroneBatteryStatus drone_battery_status;

        private void Start()
        {
            drone_battery_status = battery.GetComponentInChildren<IDroneBatteryStatus>();
        }


        void Update()
        {
            fullVoltage = (float)drone_battery_status.get_full_voltage();
            currentVoltage = (float)drone_battery_status.get_curr_voltage();
            temperature = (float)drone_battery_status.get_temperature();
            pressure = (float)drone_battery_status.get_atmospheric_pressure();
            float batteryPercentage = currentVoltage / fullVoltage;
            float percentValue = batteryPercentage * 100.0f;
            uint battery_status_level = drone_battery_status.get_status();
            fullVoltageText.text = fullVoltage.ToString("F1");
            currVoltageText.text = currentVoltage.ToString("F1");
            tempText.text = temperature.ToString("F1");
            if (pressureText != null)
            {
                pressureText.text = pressure.ToString("F1");
                //Debug.Log("preassure: " + pressure);
            }
            percentageText.text = percentValue.ToString("F1");
            Color color = Color.white;
            // 温度に応じた色を設定
            if (temperature < 20.0f)
            {
                tempText.color = Color.blue; // 低温（青）
            }
            else if (temperature >= 20.0f && temperature <= 50.0f)
            {
                tempText.color = Color.white; // 通常温度（白）
            }
            else
            {
                tempText.color = Color.red; // 高温（赤）
            }

            // 残量に応じた色を設定
            if (battery_status_level == 0)
                color = Color.green;
            else if (battery_status_level == 1)
                color = Color.yellow;
            else
                color = Color.red;

            double percent = 0;
            // 各バーの色を設定
            for (int i = batteryBars.Count - 1; i >= 0; i--)
            {
                if (percent <= batteryPercentage)
                {
                    batteryBars[i].color = color;
                }
                else
                {
                    batteryBars[i].color = Color.white;
                }
                percent += 0.1;
            }
        }
    }
}
