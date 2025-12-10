#!/usr/bin/python
# -*- coding: utf-8 -*-

"""
Drone クラス
hakosim の MultirotorClient をラップし、コース遂行や散布制御を担当する
"""

from __future__ import annotations

import sys
import os
import time
from typing import Optional

from location import Location
from course import Course, Waypoint

# hakoniwa_pdu 側のブロッキング待ちを短くするためのパッチ適用
import hakoniwa_pdu.apps.drone.hakosim as hakosim


current_dir = os.path.dirname(os.path.abspath(__file__))
if current_dir not in sys.path:
    sys.path.insert(0, current_dir)


def _patched_wait_res(self, pdu_name, conv_pdu_to_py, conv_py_to_pdu, timeout_sec: float = -1.0):
    """
    hakosim.MultirotorClient._wait_res の簡易版
    - timeout_sec < 0 の場合でも、短いデフォルトタイムアウトで失敗させる
    - PDU応答が来ないときに永遠にブロックしないようにする
    """
    # ユーザの意図に合わせて「すぐ失敗」に近い挙動にする
    if timeout_sec is None or timeout_sec < 0:
        timeout_sec = 5.0  # デフォルト5秒でタイムアウト

    start_time = time.time()
    vehicle_name = self.get_vehicle_name(self.default_drone_name)

    while True:
        # PDUマネージャのポーリング
        try:
            self.pdu_manager.run_nowait()
        except Exception:
            # PDUマネージャ未初期化などの場合は即失敗
            print(f"[patched_wait_res] pdu_manager.run_nowait() failed for {pdu_name}")
            return False

        raw_data = self._read_carefully(vehicle_name, pdu_name)
        if raw_data is not None and len(raw_data) > 0:
            try:
                py_obj = conv_pdu_to_py(raw_data)
                header = getattr(py_obj, "header", None)
                if header is not None and getattr(header, "result", 0) == 1:
                    header.result = 0
                    # 応答をクリアしておく
                    try:
                        self.pdu_manager.flush_pdu_raw_data_nowait(
                            vehicle_name, pdu_name, conv_py_to_pdu(py_obj)
                        )
                    except Exception:
                        pass
                    print("DONE")
                    return True
            except Exception as exc:
                print(f"[patched_wait_res] Failed to parse PDU for {pdu_name}: {exc}")
                # 続行せず失敗扱い
                return False

        # タイムアウト判定
        if time.time() - start_time > timeout_sec:
            print(f"[patched_wait_res] Timeout reached for {pdu_name}: {timeout_sec} seconds")
            return False

        # 応答待ちのスリープ（細かくポーリングする）
        time.sleep(0.1)


# 元の _wait_res を上書き（このプロセス内のみ有効）
hakosim.MultirotorClient._wait_res = _patched_wait_res


class Drone:
    """
    コース進行と散布を担うラッパークラス
    """

    def __init__(self, client, base_location: Location, vehicle_name: str = "Drone"):
        self.client = client
        self.vehicle_name = vehicle_name
        self.base_location = base_location
        self.current_location: Location = base_location
        self.status = "idle"
        self.active_liquid: Optional[str] = None
        self._is_spraying = False

    # --------------------------------------------------------------------- #
    # 基本操作
    # --------------------------------------------------------------------- #
    def takeoff(self, height: float = 1.0) -> bool:
        self.status = "takeoff"
        print(f"[Drone] Takeoff to {height}m")
        if not self.client.takeoff(height, self.vehicle_name):
            self.status = "idle"
            print("[Drone] Takeoff failed")
            return False

        time.sleep(0.5)
        self._refresh_location()
        print(f"[Drone] Initial position after takeoff:")
        self.debug_position()
        self.status = "flying"
        return True

    def land(self) -> bool:
        self.status = "landing"
        print("[Drone] Landing")
        result = self.client.land(self.vehicle_name)
        self.status = "idle"
        return result

    def return_to_base(self, speed: float = 5.0) -> bool:
        print("[Drone] Returning to base")
        # ベース位置の高度を現在の高度に合わせる（離陸前の位置は高度0の可能性があるため）
        base_with_current_height = Location(
            x=self.base_location.x,
            y=self.base_location.y,
            z=self.current_location.z  # 現在の高度を維持
        )
        # api_control_sample.pyと同様に、タイムアウトなしで直接moveToPositionを呼ぶ
        success = self.client.moveToPosition(
            base_with_current_height.x,
            base_with_current_height.y,
            base_with_current_height.z,
            speed,
            vehicle_name=self.vehicle_name,
        )
        if success:
            self.current_location = base_with_current_height
            print(f"  ✓ Returned to base {base_with_current_height}")
        else:
            print("  ✗ Failed to return to base")
        return success

    # --------------------------------------------------------------------- #
    # コース実行
    # --------------------------------------------------------------------- #
    def execute_course(self, course: Course, speed: float = 5.0) -> str:
        if self.status not in {"idle", "flying"}:
            return f"Drone is busy (status={self.status})"

        if len(course) == 0:
            return "Course is empty"

        self.status = "flying"
        print(f"[Drone] Executing course ({len(course)} waypoints)")

        failures = []
        for index, waypoint in enumerate(course, start=1):
            print(f"\n[Drone] Waypoint {index}/{len(course)} -> {waypoint.location}")

            if not self._move_to(waypoint.location, speed):
                failures.append(index)
                continue

            self._execute_action(waypoint)

        self.status = "idle"
        if failures:
            return f"Completed with failures at waypoints {failures}"
        return "Course completed successfully"

    # --------------------------------------------------------------------- #
    # 内部ユーティリティ
    # --------------------------------------------------------------------- #
    def _move_to(self, location: Location, speed: float, timeout: float = 30.0, retries: int = 3) -> bool:
        for attempt in range(1, retries + 1):
            print(f"  └ moveToPosition attempt {attempt}/{retries}")
            success = self.client.moveToPosition(
                location.x,
                location.y,
                location.z,
                speed,
                timeout_sec=timeout,
                vehicle_name=self.vehicle_name,
            )
            if success:
                # 実際の位置を取得して表示（Unity座標との比較用）
                self._refresh_location()
                actual = self.current_location
                # Unity座標系に変換
                target_unity = (-location.y, location.z, location.x)
                actual_unity = (-actual.y, actual.z, actual.x)
                print(f"  ✓ Reached target:")
                print(f"    ROS: X={location.x:.2f}, Y={location.y:.2f}, Z={location.z:.2f}")
                print(f"    Unity: X={target_unity[0]:.2f}, Y={target_unity[1]:.2f}, Z={target_unity[2]:.2f}")
                print(f"    Actual ROS: X={actual.x:.2f}, Y={actual.y:.2f}, Z={actual.z:.2f}")
                print(f"    Actual Unity: X={actual_unity[0]:.2f}, Y={actual_unity[1]:.2f}, Z={actual_unity[2]:.2f}")
                print(f"    Difference ROS: X={actual.x - location.x:.2f}, Y={actual.y - location.y:.2f}, Z={actual.z - location.z:.2f}")
                return True

            print("  ✗ moveToPosition failed, retrying...")
            time.sleep(1.0)

        print("  ✗ Failed to reach waypoint after retries")
        return False

    def _refresh_location(self) -> None:
        try:
            pose = self.client.simGetVehiclePose(self.vehicle_name)
            if pose:
                self.current_location = Location.from_pose(pose)
        except Exception as exc:
            print(f"[Drone] Failed to refresh location: {exc}")
    
    def debug_position(self) -> None:
        """
        現在の位置を詳細に表示（Unity座標との比較用）
        """
        try:
            pose = self.client.simGetVehiclePose(self.vehicle_name)
            if pose:
                ros_x = pose.position.x_val
                ros_y = pose.position.y_val
                ros_z = pose.position.z_val
                # Unity座標系に変換: unity_x = -ros_y, unity_y = ros_z, unity_z = ros_x
                unity_x = -ros_y
                unity_y = ros_z
                unity_z = ros_x
                print(f"[DEBUG] Current Position:")
                print(f"  ROS Coordinates: X={ros_x:.3f}, Y={ros_y:.3f}, Z={ros_z:.3f}")
                print(f"  Unity Coordinates: X={unity_x:.3f}, Y={unity_y:.3f}, Z={unity_z:.3f}")
                print(f"  Location object: {self.current_location}")
            else:
                print("[DEBUG] Failed to get pose")
        except Exception as exc:
            print(f"[DEBUG] Error getting position: {exc}")

    def _execute_action(self, waypoint: Waypoint) -> None:
        if waypoint.action is None:
            return

        if waypoint.action:
            print(f"  (Action `{waypoint.action}` skipped for now)")

    def _start_and_stop_spray(self, target_liquid: str) -> None:
        return

