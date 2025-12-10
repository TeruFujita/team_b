# #!/usr/bin/python
# # -*- coding: utf-8 -*-

# import sys
# import hakoniwa_pdu.apps.drone.hakosim as hakosim
# import time
# import math
# import numpy
# import pprint

# def transport(client, baggage_pos, transfer_pos):
#     client.moveToPosition(baggage_pos['x'], baggage_pos['y'], 3, 5, -90)
#     client.moveToPosition(baggage_pos['x'], baggage_pos['y'], 3, 5)
#     client.moveToPosition(baggage_pos['x'], baggage_pos['y'], 3, 5, 0)
#     client.moveToPosition(baggage_pos['x'], baggage_pos['y'], 0.3, 5, 0)
#     client.grab_baggage(True)
#     client.moveToPosition(baggage_pos['x'], baggage_pos['y'], 3, 5)
#     client.moveToPosition(transfer_pos['x'], transfer_pos['y'], 3, 5)
#     client.moveToPosition(transfer_pos['x'], transfer_pos['y'], transfer_pos['z'], 5)
#     client.grab_baggage(False)
#     client.moveToPosition(transfer_pos['x'], transfer_pos['y'], 3, 5)

# def debug_pos(client):
#     pose = client.simGetVehiclePose()
#     print(f"POS  : {pose.position.x_val} {pose.position.y_val} {pose.position.z_val}")
#     roll, pitch, yaw = hakosim.hakosim_types.Quaternionr.quaternion_to_euler(pose.orientation)
#     print(f"ANGLE: {math.degrees(roll)} {math.degrees(pitch)} {math.degrees(yaw)}")

# def parse_lidarData(data):

#     # reshape array of floats to array of [X,Y,Z]
#     points = numpy.array(data.point_cloud, dtype=numpy.dtype('f4'))
#     points = numpy.reshape(points, (int(points.shape[0]/3), 3))
    
#     return points


# def main():
#     if len(sys.argv) != 2:
#         print(f"Usage: {sys.argv[0]} <config_path>")
#         return 1

#     # connect to the HakoSim simulator
#     client = hakosim.MultirotorClient(sys.argv[1], "Drone")
#     client.confirmConnection()
#     client.enableApiControl(True)
#     client.armDisarm(True)

#     lidarData = client.getLidarData()
#     if (len(lidarData.point_cloud) < 3):
#         print("\tNo points received from Lidar data")
#     else:
#         print(f"len: {len(lidarData.point_cloud)}")
#         points = parse_lidarData(lidarData)
#         print("\tReading: time_stamp: %d number_of_points: %d" % (lidarData.time_stamp, len(points)))
#         print("\t\tlidar position: %s" % (pprint.pformat(lidarData.pose.position)))
#         print("\t\tlidar orientation: %s" % (pprint.pformat(lidarData.pose.orientation)))
    
#         #lidar_z = lidarData.pose.position.z_val
#         condition = numpy.logical_and(points <= 2, points > 0)
#         filtered_points = points[numpy.any(condition, axis=1)]

#         print(filtered_points)

#     client.takeoff(0.5)

#     client.moveToPosition(1, 0, 0.5, 1)
#     debug_pos(client)
#     client.grab_baggage(True)

#     time.sleep(3)

#     client.moveToPosition(0, 0, 0.5, 1)
#     debug_pos(client)
#     time.sleep(3)
#     client.grab_baggage(False)

#     time.sleep(3)

#     client.moveToPosition(-0.5, 0, 0.25, 1)
#     debug_pos(client)

#     time.sleep(3)

#     lidarData = client.getLidarData()
#     if (len(lidarData.point_cloud) < 3):
#         print("\tNo points received from Lidar data")
#     else:
#         print(f"len: {len(lidarData.point_cloud)}")
#         points = parse_lidarData(lidarData)
#         print("\tReading: time_stamp: %d number_of_points: %d" % (lidarData.time_stamp, len(points)))
#         print("\t\tlidar position: %s" % (pprint.pformat(lidarData.pose.position)))
#         print("\t\tlidar orientation: %s" % (pprint.pformat(lidarData.pose.orientation)))
    
#         #lidar_z = lidarData.pose.position.z_val
#         condition = numpy.logical_and(points <= 2, points > 0)
#         filtered_points = points[numpy.any(condition, axis=1)]

#         print(filtered_points)


#     client.simSetCameraOrientation("0",0)

#     png_image = client.simGetImage("0", hakosim.ImageType.Scene)
#     if png_image:
#         with open("scene.png", "wb") as f:
#             f.write(png_image)

#     client.simSetCameraOrientation("0",-90)

#     client.land()
#     debug_pos(client)

#     return 0

# if __name__ == "__main__":
#     sys.exit(main())

import sys
import hakoniwa_pdu.apps.drone.hakosim as hakosim
import time
import math
import numpy
import pprint

# (既存の transport, debug_pos, parse_lidarData 関数は省略)

def debug_pos(client):
    pose = client.simGetVehiclePose()
    print(f"POS  : {pose.position.x_val} {pose.position.y_val} {pose.position.z_val}")
    roll, pitch, yaw = hakosim.hakosim_types.Quaternionr.quaternion_to_euler(pose.orientation)
    print(f"ANGLE: {math.degrees(roll)} {math.degrees(pitch)} {math.degrees(yaw)}")

def parse_lidarData(data):
    # reshape array of floats to array of [X,Y,Z]
    points = numpy.array(data.point_cloud, dtype=numpy.dtype('f4'))
    points = numpy.reshape(points, (int(points.shape[0]/3), 3))
    return points

def main():
    if len(sys.argv) != 2:
        print(f"Usage: {sys.argv[0]} <config_path>")
        return 1

    # connect to the HakoSim simulator
    client = hakosim.MultirotorClient(sys.argv[1], "Drone")
    client.confirmConnection()
    client.enableApiControl(True)
    client.armDisarm(True)

    # --- 1. 初期LIDARデータの取得  ---
    lidarData = client.getLidarData()
    if (len(lidarData.point_cloud) < 3):
        print("\tNo points received from Lidar data")
    else:
        points = parse_lidarData(lidarData)
        print(f"len: {len(lidarData.point_cloud)}")
        print("\tReading: time_stamp: %d number_of_points: %d" % (lidarData.time_stamp, len(points)))
        condition = numpy.logical_and(points <= 2, points > 0)
        filtered_points = points[numpy.any(condition, axis=1)]
        print(filtered_points)

    # --- 2. 飛行プランの実行  ---
    
    # 飛行パラメーターを定義
    FLIGHT_HEIGHT = 1.0  # 地面から1.0m
    FLIGHT_SPEED = 5.0   # 巡航速度を5.0m/sに設定 (移動点が多いので速度をさらに上げる)
    
    # グリッド座標を定義: [-6, 6] の範囲を 1.0m 間隔にする
    GRID_MIN = -6.0
    GRID_MAX = 6.0
    GRID_STEP = 1.0      # 間隔を1.0mに設定
    
    # XとYの座標リスト: [-6.0, -5.0, ..., 5.0, 6.0] (13点)
    # np.arange(start, stop+step, step) の形式で生成
    x_coords = numpy.arange(GRID_MIN, GRID_MAX + GRID_STEP/2, GRID_STEP)
    y_coords = numpy.arange(GRID_MIN, GRID_MAX + GRID_STEP/2, GRID_STEP)

    # 離陸
    print("\n--- TAKEOFF ---")
    client.takeoff(FLIGHT_HEIGHT) 
    
    # 巡回ルートのリスト
    waypoints = []
    
    # Z字型パターンで巡回するようウェイポイントを作成 (計 13x13 = 169点)
    for i, y in enumerate(y_coords):
        if i % 2 == 0:
            # 偶数行: Xを昇順 (-6.0 -> 6.0)
            x_sequence = x_coords
        else:
            # 奇数行: Xを降順 (6.0 -> -6.0)
            x_sequence = x_coords[::-1] 
            
        for x in x_sequence:
            waypoints.append((x, y, FLIGHT_HEIGHT))

    print(f"\n--- WAYPOINTS (Total {len(waypoints)} points) ---")
    print(f"First 5: {waypoints[:5]}")
    print(f"Last 5: {waypoints[-5:]}")

    # 各ウェイポイントへ移動 (ホバリングなし)
    for x, y, z in waypoints:
        # ポイントに到達したらすぐに次のポイントへ移動
        client.moveToPosition(x, y, z, FLIGHT_SPEED)
        # debug_pos(client) # デバッグログは頻繁に出力されるため、コメントアウトを推奨

    # 最初の位置 (-6.0, -6.0) に戻る
    print("\n--- Returning to Start (-6.0, -6.0, 1.0) ---")
    client.moveToPosition(GRID_MIN, GRID_MIN, FLIGHT_HEIGHT, FLIGHT_SPEED)
    debug_pos(client)
    time.sleep(2) # 最後の位置確認のために少し待機
    
    # --- 3. カメラ画像の取得と着陸 ---
    print("\n--- LANDING AND IMAGE CAPTURE ---")
    
    client.simSetCameraOrientation("0", 0) # カメラを正面に

    # 画像取得（scene.png）
    png_image = client.simGetImage("0", hakosim.ImageType.Scene)
    if png_image:
        with open("scene.png", "wb") as f:
            f.write(png_image)
            print("Captured scene.png")

    client.simSetCameraOrientation("0", -90) # カメラを真下に

    client.land()
    debug_pos(client)

    return 0

if __name__ == "__main__":
    sys.exit(main())