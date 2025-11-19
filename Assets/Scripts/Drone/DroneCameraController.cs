using hakoniwa.drone.sim;
using hakoniwa.objects.core.sensors;
using hakoniwa.pdu.interfaces;
using System;
using UnityEngine;

namespace hakoniwa.drone
{
    public class DroneCameraController : MonoBehaviour
    {
        private ICameraController controller;

        private void Awake()
        {
            this.controller = this.GetComponentInChildren<ICameraController>();
            if (this.controller == null)
            {
                throw new Exception("Can not find ICameraController");
            }
            controller.Initialize();
        }

        void LateUpdate()
        {
            this.controller.UpdateCameraAngle();
        }

        public void DoControl(IPduManager pduManager)
        {
            /*
             * Camera Image Request
             */
            this.controller.CameraImageRequest(pduManager);
            /*
             * Camera Move Request
             */
            this.controller.CameraMoveRequest(pduManager);

            this.controller.UpdateCameraImageTexture();
        }
        public ICameraController GetCameraController()
        {
            return this.controller;
        }
    }

}
