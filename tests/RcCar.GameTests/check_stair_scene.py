#!/usr/bin/env python3
"""Exercise the live Unity mod in a disposable copy of the RC car stair test scene.

Fixture: ground at height 4, a finished north/south stair at (63, 51, 4),
finished platforms at x=61..65, y=48..50, and unfinished platforms at y=47.
Build with -p:RcCarGameTests=true; see README.md for launch instructions.
"""
import argparse
import json
from pathlib import Path
import time


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", type=Path, default=Path.home() /
                        "Documents/Timberborn/Mods/RcCar/generated/game-tests")
    args = parser.parse_args()
    directory = args.directory
    directory.mkdir(parents=True, exist_ok=True)
    results = []

    def command(text):
        response = directory / "response.txt"
        response.unlink(missing_ok=True)
        pending = directory / "pending.txt"
        pending.write_text(text)
        pending.replace(directory / "request.txt")
        deadline = time.monotonic() + 10
        while time.monotonic() < deadline:
            if response.exists():
                value = response.read_text()
                if value.startswith("ERROR"):
                    raise RuntimeError(value)
                return tuple(map(float, value.split()))
            time.sleep(0.02)
        raise TimeoutError("No live game test response to: " + text)

    def drive(start, displacement, count=60, steering=0):
        command("position " + " ".join(map(str, start)))
        # Separate frames reattempt movement after first touching a collider's skin.
        for _ in range(count):
            position = command("move " + " ".join(map(str, (*displacement, steering))))
        return position

    def check(name, position, condition):
        results.append(dict(name=name, position=position, passed=bool(condition)))
        print(json.dumps(results[-1]), flush=True)
        assert condition, name + " failed: " + str(position)

    p = drive((63.5, 4.36, 53, 180), (0, -0.08))
    check("ascend_and_clear_stair_exit", p, abs(p[1] - 5.36) < .01 and p[2] < 49)
    p = drive((63.5, 5.36, 50.99, 180), (0, -0.04), 40)
    check("regression_rear_still_over_stair", p, abs(p[1] - 5.36) < .01 and p[2] < 49.5)
    p = drive((63.5, 5.36, 49, 0), (0, .08))
    check("descend_stair", p, abs(p[1] - 4.36) < .01 and p[2] > 53)
    p = drive((62.5, 4.36, 53, 180), (0, -.08))
    check("neighboring_stair_does_not_allow_platform_climb", p,
          abs(p[1] - 4.36) < .01 and p[2] > 51)
    p = drive((62.5, 5.36, 49, 0), (0, .08))
    check("platform_edge_without_stair_blocks_descent", p,
          abs(p[1] - 5.36) < .01 and p[2] < 51)
    p = drive((63.5, 5.36, 49, 180), (0, -.08))
    check("unfinished_platform_blocks_progress", p,
          abs(p[1] - 5.36) < .01 and p[2] >= 48.7)

    # Temporary Unity BoxColliders exercise the same sweeps as game walls. These
    # are test objects, never serialized buildings, and are removed even on failure.
    command("clear-walls")
    try:
        command("wall 60 5 56 6 3 1")
        p = drive((60, 4.36, 53, 0), (0, .08))
        check("head_on_wall_slips_right_without_climbing", p,
              p[0] > 60.3 and p[2] <= 54.76 and abs(p[1] - 4.36) < .01)
        p = drive((60, 4.36, 53, 0), (0, .08), steering=-1)
        check("left_steering_selects_left_slip", p,
              p[0] < 59.7 and p[2] <= 54.76 and abs(p[1] - 4.36) < .01)
        p = drive((60, 4.36, 54.74, 0), (0, 0), count=5)
        check("stationary_car_does_not_drift", p,
              abs(p[0] - 60) < .001 and abs(p[2] - 54.74) < .001)
        p = drive((60, 4.36, 54.70, 0), (-.04, .08), count=20)
        check("glancing_contact_keeps_natural_slide_direction", p,
              p[0] < 59.6 and p[2] <= 54.76 and abs(p[1] - 4.36) < .01)
        command("wall 61 5 55 1 3 6")
        p = drive((60, 4.36, 53, 0), (0, .08))
        check("corner_blocks_slip_without_penetration", p,
              59.99 <= p[0] <= 60.03 and p[2] <= 54.76 and abs(p[1] - 4.36) < .01)
    finally:
        command("clear-walls")
    # Leave the test car safely on the completed landing for visual inspection.
    command("position 63.5 4.36 53 180")
    for _ in range(40):
        command("move 0 -0.08")
    (directory / "results.json").write_text(json.dumps(results, indent=2) + "\n")


if __name__ == "__main__":
    main()
