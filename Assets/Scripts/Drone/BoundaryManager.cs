// BoundaryManager.cs  (Unity 2021.3+)
// Attach to an empty GameObject.  All vectors are Unity world coords (+X right, +Y up, +Z forward).
// If pushToApi is true, the nearest plane is sent each frame to DroneServiceRC in ROS ENU.

using UnityEngine;
using hakoniwa.drone.service;         // <-- namespace where DroneServiceRC lives

namespace hakoniwa.drone
{
    public class BoundaryManager : MonoBehaviour
    {
        [Header("Scene References")]
        public GameObject drone;                  // Drag your drone root here
        public GameObject[] boundaryPlanes;       // Drag every wall/plane here

        [Header("Plane Settings")]
        [Tooltip("Local axis that represents the plane's outward normal")]
        public Vector3 localNormalAxis = Vector3.up;

        [Header("API Settings")]
        public bool pushToApi = true;
        public int droneIndex = 0;                // Index given to DroneServiceRC

        /* ------------------------------------------------------------------------- */
        void Update()
        {
            if (drone == null || boundaryPlanes == null || boundaryPlanes.Length == 0) return;

            Vector3 dronePos = drone.transform.position;

            float minDist = float.MaxValue;
            Vector3 bestNormal = Vector3.zero;
            Vector3 bestPlanePt = Vector3.zero;
            bool found = false;

            foreach (var plane in boundaryPlanes)
            {
                if (plane == null) continue;

                /* --------------------------------------------------------------
                 * Python: wall_pos / rotation / size
                 * ------------------------------------------------------------ */
                Vector3 centerW = GetPlaneCenterWorld(plane);                    // ★ position
                Vector3 normalW = GetPlaneNormalWorld(plane, localNormalAxis);   // ★ local_normal_axis → world

                /* ★ Flip normal so it faces the drone (compute_wall_normal_from_view) */
                Vector3 toDrone = dronePos - centerW;
                if (Vector3.Dot(toDrone, normalW) < 0f) normalW = -normalW;

                /* ★ intersect_ray_with_plane */
                float tSigned = Vector3.Dot(toDrone, normalW);
                Vector3 hitPt = dronePos - tSigned * normalW;                  // intersection_point
                float distAbs = Mathf.Abs(tSigned);

                /* ★ tangent / bitangent axes (rotation applied to [1,0,0] & [0,1,0]) */
                Vector3 tangentW, bitangentW;
                if (localNormalAxis == Vector3.up)      // Plane (+Y)
                {
                    tangentW = plane.transform.right;     // local +X
                    bitangentW = plane.transform.forward;   // local +Z
                }
                else                                     // Quad (+Z) など
                {
                    tangentW = plane.transform.right;     // local +X
                    bitangentW = plane.transform.up;        // local +Y
                }

                /* ★ width / height (world) */
                Vector2 sizeW = GetPlaneSizeWorld(plane);   // [width, height]
                float halfW = sizeW.x * 0.5f;
                float halfH = sizeW.y * 0.5f;

                /* ★ project hit point to local plane axes (x_proj, y_proj) */
                Vector3 d = hitPt - centerW;
                float xProj = Vector3.Dot(d, tangentW);
                float yProj = Vector3.Dot(d, bitangentW);

                /* ★ is_point_in_wall_rectangle */
                if (Mathf.Abs(xProj) <= halfW && Mathf.Abs(yProj) <= halfH)
                {
                    if (distAbs < minDist)
                    {
                        minDist = distAbs;
                        bestNormal = normalW;
                        bestPlanePt = centerW;
                        found = true;
                    }
                }
            }

            /* --------------------------------------------------------------
             * Push nearest plane to Hakoniwa API
             * ------------------------------------------------------------ */
            if (found && pushToApi)
            {
                //Debug.Log("boundary: " + bestPlanePt + " rosNormal: " + bestNormal);
                Vector3 rosPoint = UnityToRos(bestPlanePt);
                Vector3 rosNormal = UnityToRos(bestNormal).normalized;
                DroneServiceRC.PutDisturbanceBoundary(droneIndex, rosPoint, rosNormal);
            }
        }


        /* ---------- helpers ----------------------------------------------------- */

        // Returns half-extent of the mesh along local axis (0=X, 1=Y).
        Vector3 GetPlaneCenterWorld(GameObject plane)
        {
            var mf = plane.GetComponent<MeshFilter>();
            Vector3 localCenter = mf != null ? mf.sharedMesh.bounds.center : Vector3.zero;
            return plane.transform.TransformPoint(localCenter);
        }

        Vector2 GetPlaneSizeWorld(GameObject plane)
        {
            var mf = plane.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return Vector2.zero;

            // ローカル extents → ワールド
            Vector3 ext = mf.sharedMesh.bounds.extents;     // half-size (x,y,z) in mesh space
            Vector3 ls = plane.transform.lossyScale;
            float width = Mathf.Abs(ext.x * 2f * ls.x);    // X 方向
            float height = Mathf.Abs(ext.z * 2f * ls.z);    // Y 方向  (Quad)  ※Planeなら ext.z を使う
            //Debug.Log("Size: height: " + height + " width: " + width);
            return new Vector2(width, height);
        }

        Vector3 GetPlaneNormalWorld(GameObject plane, Vector3 localNormalAxis)
        {
            return plane.transform.TransformDirection(localNormalAxis).normalized;
        }


        static Vector3 UnityToRos(Vector3 v) => new Vector3(v.z, -v.x, v.y);
    }

}
