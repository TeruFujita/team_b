using System.Collections;
using hakoniwa.objects.core.sensors;
using UnityEngine;
using UnityEngine.InputSystem;

public class ImageCapture : MonoBehaviour
{
    private MonitorCameraManager cameraManager;
    public string imageName = "can";
    public GameObject rotationContainer;
    private float current_degree = 0;
    public float deltaDegree = 36;

    void Start()
    {
        cameraManager = this.GetComponentInChildren<MonitorCameraManager>();
        StartCoroutine(RotateAndCapture());
    }
    IEnumerator RotateAndCapture()
    {
        while (current_degree < 100)
        {
            rotationContainer.transform.rotation = Quaternion.Euler(0, current_degree, 0);
            yield return new WaitForEndOfFrame(); // 描画フレームをまたぐ

            cameraManager.GetAndSaveCameraImages(imageName + current_degree);
            Debug.Log($"Captured at {current_degree} degrees");

            current_degree += deltaDegree;
            yield return new WaitForSeconds(0.1f); // 少し待って安定させる
        }
    }

}
