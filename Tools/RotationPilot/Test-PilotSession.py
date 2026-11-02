#!/usr/bin/env python3
"""Validate a saved pilot session locally; never changes the session files."""
import argparse
import collections
import csv
import json
import math
import pathlib
import sys


def validate(folder, allow_synthetic=False):
    issues = []
    needed = ("session.json", "session_status.json", "results.txt", "motion.tsv", "events.tsv", "data.txt")
    missing = [name for name in needed if not (folder / name).is_file()]
    if missing:
        return ["Missing files: " + ", ".join(missing)], {}
    try:
        metadata = json.loads((folder / "session.json").read_text(encoding="utf-8-sig"))
        status = json.loads((folder / "session_status.json").read_text(encoding="utf-8-sig"))
        with (folder / "results.txt").open(encoding="utf-8-sig", newline="") as source:
            rows = [row for row in csv.reader(source, delimiter="\t") if row]
        with (folder / "events.tsv").open(encoding="utf-8-sig", newline="") as source:
            events = list(csv.DictReader(source, delimiter="\t"))
        last_motion = {}
        with (folder / "motion.tsv").open(encoding="utf-8-sig", newline="") as source:
            for row in csv.DictReader(source, delimiter="\t"):
                last_motion[int(row["trial"])] = float(row["real_yaw_degrees"])
        expected = int(metadata["expectedMeasuredTrials"])
        repetitions = int(metadata["repetitions"])
        conditions_expected = int(metadata["steps"]) * int(metadata["steps2"])
        valid_responses = {int(value) for value in metadata["responseScale"].split(",")}
    except (OSError, ValueError, TypeError, KeyError, json.JSONDecodeError) as error:
        return ["Could not read session data: " + str(error)], {}

    synthetic = bool(metadata.get("softwareSimulation"))
    if synthetic and not allow_synthetic:
        issues.append("Synthetic output is not a participant pilot dataset.")
    if not str(metadata.get("participantId", "")).strip():
        issues.append("Participant ID is missing.")
    if not str(metadata.get("scaleInterpretation", "")).strip():
        issues.append("Response instructions are missing.")
    if len(rows) != expected or status.get("completedMeasuredTrials") != expected or not status.get("complete"):
        issues.append("The session does not have the expected number of completed measured trials.")
    if status.get("expectedMeasuredTrials") != expected:
        issues.append("Session and status metadata disagree on trial count.")
    conditions = collections.Counter()
    for index, row in enumerate(rows):
        if len(row) != 4:
            issues.append("Result row {} does not have four columns.".format(index))
            continue
        try:
            if int(row[3]) not in valid_responses:
                issues.append("Trial {} has a response outside its recorded scale.".format(index))
            gain, gain2 = float(row[0]), float(row[1])
            if not math.isfinite(gain) or not math.isfinite(gain2):
                raise ValueError("non-finite gain")
        except ValueError:
            issues.append("Trial {} has an invalid gain or response.".format(index))
            continue
        conditions[(row[0], row[1])] += 1
    if len(conditions) != conditions_expected or any(count != repetitions for count in conditions.values()):
        issues.append("Gain conditions do not match the configured repetition count.")
    if not last_motion or (folder / "data.txt").stat().st_size == 0:
        issues.append("Motion or legacy data is empty.")

    if metadata.get("directionSchedule") == "alternate_left_first_across_practice_and_measured":
        by_trial = collections.defaultdict(list)
        for event in events:
            try:
                by_trial[int(event["trial"])].append(event["event"])
            except (KeyError, TypeError, ValueError):
                issues.append("An event has an invalid trial index or event name.")
                break
        directed = collections.Counter()
        practice = int(metadata.get("practiceTrials", 0))
        per_direction = int(metadata.get("repetitionsPerDirection", 0))
        min_virtual_degrees = math.degrees(float(metadata.get("virtualAngleRadians", 0)) - math.pi / 8)
        for index, row in enumerate(rows):
            if len(row) != 4:
                continue
            direction = "LEFT" if (practice + index) % 2 == 0 else "RIGHT"
            cue, done = "TURN_DIRECTION_" + direction, "TURN_COMPLETED_" + direction
            relevant = [e for e in by_trial[index] if e.startswith("TURN_DIRECTION_") or e.startswith("TURN_COMPLETED_")]
            if relevant != [cue, done]:
                issues.append("Trial {} has missing, duplicate, or incorrect direction events.".format(index))
            try:
                signed_turn = last_motion[index] * (-1 if direction == "LEFT" else 1)
                if not math.isfinite(signed_turn) or signed_turn * float(row[0]) < min_virtual_degrees - 0.1:
                    issues.append("Trial {} has insufficient motion in the indicated direction.".format(index))
            except (KeyError, ValueError):
                issues.append("Trial {} has no valid motion sample.".format(index))
            directed[(row[0], row[1], direction)] += 1
        if per_direction < 1 or len(directed) != conditions_expected * 2 or any(v != per_direction for v in directed.values()):
            issues.append("Gain and direction pairs are not balanced as configured.")
    summary = {"rows": len(rows), "conditions": len(conditions), "synthetic": synthetic,
               "complete": bool(status.get("complete")), "protocol": metadata.get("protocolStatus", "unknown")}
    return issues, summary


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("session", type=pathlib.Path, help="The folder containing session.json")
    parser.add_argument("--allow-synthetic", action="store_true", help="Allow explicitly synthetic QA sessions")
    args = parser.parse_args()
    issues, summary = validate(args.session, args.allow_synthetic)
    print(json.dumps({"passed": not issues, **summary, "issues": issues}, indent=2))
    return 1 if issues else 0


if __name__ == "__main__":
    sys.exit(main())
