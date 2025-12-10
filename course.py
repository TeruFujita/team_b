#!/usr/bin/python
# -*- coding: utf-8 -*-

"""
Course クラス
ウェイポイント（位置 + アクション）を直列に管理する
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Iterable, List, Optional, Tuple

from location import Location


@dataclass(frozen=True)
class Waypoint:
    location: Location
    action: Optional[str] = None  # "water", "feed", など


class Course:
    """
    ドローンが辿るウェイポイント列を表現するクラス
    """

    def __init__(self, waypoints: Optional[Iterable[Waypoint]] = None):
        self._waypoints: List[Waypoint] = list(waypoints or [])

    def __len__(self) -> int:
        return len(self._waypoints)

    def __iter__(self):
        return iter(self._waypoints)

    def add_waypoint(self, location: Location, action: Optional[str] = None) -> None:
        self._waypoints.append(Waypoint(location, action))

    def waypoints(self) -> List[Waypoint]:
        """内部リストのコピーを返す"""
        return list(self._waypoints)

    def optimize(self, start: Optional[Location] = None) -> None:
        """
        最近傍法による簡易的な巡回順序最適化
        """
        if len(self._waypoints) <= 2:
            return

        remaining = self._waypoints.copy()
        optimized: List[Waypoint] = []

        current_location = start or remaining[0].location
        while remaining:
            nearest_index = min(
                range(len(remaining)),
                key=lambda idx: current_location.distance_to(remaining[idx].location),
            )
            nearest_wp = remaining.pop(nearest_index)
            optimized.append(nearest_wp)
            current_location = nearest_wp.location

        self._waypoints = optimized

    @staticmethod
    def build_grid(
        x_range: Tuple[float, float],
        y_range: Tuple[float, float],
        step: float,
        altitude: float,
        alternating_actions: Tuple[str, str] = ("water", "feed"),
    ) -> "Course":
        """
        シンプルなZ字パターンのグリッドコースを生成する
        """
        course = Course()
        x_min, x_max = x_range
        y_min, y_max = y_range

        y_values = Course._frange(y_min, y_max, step)
        x_values = Course._frange(x_min, x_max, step)

        action_cycle = list(alternating_actions)
        action_index = 0

        for row, y in enumerate(y_values):
            xs = x_values if row % 2 == 0 else list(reversed(x_values))
            for x in xs:
                location = Location(x=x, y=y, z=altitude)
                action = action_cycle[action_index % len(action_cycle)]
                action_index += 1
                course.add_waypoint(location, action)

        return course

    @staticmethod
    def _frange(start: float, stop: float, step: float) -> List[float]:
        values = []
        current = start
        # 浮動小数点誤差を考慮して少し余裕を持たせる
        while current <= stop + 1e-6:
            values.append(round(current, 6))
            current += step
        return values

