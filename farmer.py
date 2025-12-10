#!/usr/bin/python
# -*- coding: utf-8 -*-

"""
Farmer 実行ファイル
ドローン・コースの生成からミッション実行までを司る
"""

from __future__ import annotations

import os
import sys
import time

import hakoniwa_pdu.apps.drone.hakosim as hakosim

from course import Course
from drone import Drone
from location import Location


class Farmer:
    """
    Drone / Course を組み合わせてミッションを実行する上位クラス
    """

    def __init__(self, name: str, config_path: str, vehicle_name: str = "Drone"):
        self.name = name
        self.vehicle_name = vehicle_name
        self.client = hakosim.MultirotorClient(config_path, vehicle_name)
        self._setup_client()
        self.drone = Drone(self.client, self._read_base_location(), vehicle_name)

    # ------------------------------------------------------------------ #
    def _setup_client(self) -> None:
        self.client.confirmConnection()
        self.client.enableApiControl(True)
        self.client.armDisarm(True)

    def _read_base_location(self) -> Location:
        """
        ベース位置を固定で(0, 0, 0)にする
        Unityシーンの原点上をホームポジションとして扱う
        """
        location = Location(x=0.0, y=0.0, z=0.0)
        print(f"[Farmer] Initial base location (fixed): X={location.x:.3f}, Y={location.y:.3f}, Z={location.z:.3f}")
        return location

    # ------------------------------------------------------------------ #
    def create_grid_course(self) -> Course:
        """
        グリッドを巡回するサンプルコースを生成
        """
        course = Course.build_grid(
            x_range=(-6.0, 6.0),  # Xの範囲を -6.0 ～ 6.0 に固定
            y_range=(-6.0, 6.0),  # Yの範囲を -6.0 ～ 6.0 に固定
            step=2.0,
            altitude=2.0,  # 高度を2.0mに固定
            alternating_actions=("water", "feed"),
        )

        # 特定の位置(4.7, -4.7)を追加
        course.add_waypoint(Location(x=4.7, y=-4.7, z=2.0), "water")

        # 最後に必ず原点(0, 0)上空に戻るウェイポイントを追加
        course.add_waypoint(Location(x=0.0, y=0.0, z=2.0), None)

        print(f"[Farmer] Generated course with {len(course)} waypoints")
        first = course.waypoints()[:3]
        last = course.waypoints()[-3:]
        print(f"[Farmer] First waypoints: {first}")
        print(f"[Farmer] Last waypoints : {last}")

        return course

    def run_mission(self, course: Course) -> None:
        print(f"[Farmer] Mission start with {len(course)} waypoints")

        if not self.drone.takeoff(height=2.0):  # 離陸高度を2.0mに戻す
            print("[Farmer] Takeoff failed, aborting.")
            return

        time.sleep(1.0)

        result = self.drone.execute_course(course)
        print(f"[Farmer] Course result: {result}")

        if not self.drone.return_to_base():
            print("[Farmer] Failed to return to base")

        self.drone.land()
        print("[Farmer] Mission completed")


def main() -> int:
    if len(sys.argv) != 2:
        print(f"Usage: {sys.argv[0]} <config_path>")
        return 1

    config_path = sys.argv[1]

    try:
        farmer = Farmer("Farmer1", config_path)
        # グリッド生成順（Z字パターン）のまま実行し、挙動を素直にする
        course = farmer.create_grid_course()
        # ここでの最適化は一旦行わず、「見た目どおりの順番」で巡回する
        # course.optimize(start=farmer.drone.current_location)
        farmer.run_mission(course)
        return 0
    except Exception:
        import traceback

        traceback.print_exc()
        return 1


if __name__ == "__main__":
    sys.exit(main())

