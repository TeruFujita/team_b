#!/usr/bin/python
# -*- coding: utf-8 -*-

"""
Location クラス
ROS 座標系 (x, y, z) を表現する単純な値オブジェクト
"""

from __future__ import annotations

from dataclasses import dataclass
from math import sqrt
from typing import Tuple


@dataclass(frozen=True)
class Location:
    """
    ROS座標系の位置情報を保持する不変クラス
    """

    x: float  # ROS: 前方(+X奥方向)
    y: float  # ROS: 左右(+Y左方向)
    z: float  # ROS: 上下(+Z上方向)

    def to_ros_tuple(self) -> Tuple[float, float, float]:
        """hakosim APIにそのまま渡せるタプルを返す"""
        return (self.x, self.y, self.z)

    def distance_to(self, other: "Location") -> float:
        """2点間のユークリッド距離を返す"""
        return sqrt(
            (self.x - other.x) ** 2 +
            (self.y - other.y) ** 2 +
            (self.z - other.z) ** 2
        )

    @staticmethod
    def from_pose(pose) -> "Location":
        """
        hakosim Pose から Location を生成するヘルパー
        """
        return Location(
            x=pose.position.x_val,
            y=pose.position.y_val,
            z=pose.position.z_val,
        )
    
    @staticmethod
    def from_unity_coords(unity_x: float, unity_y: float, unity_z: float) -> "Location":
        """
        Unity座標系からROS座標系に変換
        Unity座標系 (x, y, z) → ROS座標系 (x, y, z)
        変換式: ros_x = unity_z, ros_y = -unity_x, ros_z = unity_y
        """
        return Location(
            x=unity_z,      # ROS X = Unity Z
            y=-unity_x,     # ROS Y = -Unity X
            z=unity_y,      # ROS Z = Unity Y
        )
    
    def to_unity_coords(self) -> Tuple[float, float, float]:
        """
        ROS座標系からUnity座標系に変換
        ROS座標系 (x, y, z) → Unity座標系 (x, y, z)
        変換式: unity_x = -ros_y, unity_y = ros_z, unity_z = ros_x
        """
        return (-self.y, self.z, self.x)

