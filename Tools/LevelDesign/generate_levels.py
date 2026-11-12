#!/usr/bin/env python3
"""
Match Jam - 1000 level generator (box sort mode: crates of 3 + 5 waiting slots).

Writes Level_0 .. Level_999 (keeps their GUIDs), points LevelStorage to these 1000 levels,
and exports a CSV table for review.

Run from the project root:
    python3 Tools/LevelDesign/generate_levels.py

Difficulty knobs per level (box sort mode, decoys are merged into targets by ToySpawner):
- crates   = totalTriad  : number of crates to fill (toys on field = crates * 3)
- types    = typeUsed    : number of different toy kinds (more kinds = harder to find, waiting slots fill faster)
- duration = seconds     : time pressure (seconds per toy goes down with difficulty)

Curve design:
- Levels 1-10   : onboarding, very easy, grows fast so the player learns
- Levels 11-1000: base difficulty rises fast at first then slowly (exponential approach, no flat plateau)
- Every 10 levels (a "chapter") follows a sawtooth rhythm: warm up -> rise -> hard -> breather -> boss
- Every 50th level is a super hard milestone, the level after it is a reward (easy) level
- Small deterministic jitter so neighbour levels never feel identical
"""
import csv
import json
import math
import os
import random
import re

LEVEL_COUNT = 1000
REPEAT_FROM_LEVEL = 200  # after level 1000, loop levels 201..1000
DATA_DIR = "Assets/Match3D/ScriptableObject/ToyAndLevel/LevelData"
GENERATED_DIR = os.path.join(DATA_DIR, "Generated")
STORAGE_PATH = os.path.join(DATA_DIR, "LevelStorage.asset")
CSV_PATH = "Tools/LevelDesign/levels_1000.csv"
# support toys (lightning bolt, extra time clock) of the original level set, by level index.
# Kept as is: booster tutorials and support toy pacing were designed on these levels
SUPPORT_PATH = "Tools/LevelDesign/support_toys_original.json"

# limits
MIN_CRATES, MAX_CRATES = 4, 52      # max 156 toys on field, keeps physics/perf ok on low devices
MIN_TYPES, MAX_TYPES = 2, 12        # smallest active toy group has 18 kinds (needs > typeUsed)
MIN_DURATION = 90
MIN_DURATION_NEW_PLAYER = 120  # levels 1-20: new players must not fail by time
SEC_PER_TOY_EASY, SEC_PER_TOY_HARD = 5.5, 2.7

# chapter rhythm (position 1..10 inside each block of 10 levels)
CHAPTER_OFFSET = [-0.07, -0.04, -0.01, 0.02, -0.03, 0.03, 0.06, 0.09, -0.09, 0.15]
TAG_BY_POS = ["easy", "normal", "normal", "normal", "easy", "normal", "hard", "hard", "breather", "boss"]
MILESTONE_EVERY = 50
MILESTONE_BONUS = 0.10


def base_difficulty(level):
    """0..1, level is 1-based"""
    if level <= 10:
        return 0.02 + (level - 1) / 9 * 0.16
    # fast early growth, keeps rising slowly until 1000
    return 0.18 + 0.70 * (1 - math.exp(-(level - 10) / 260)) + 0.04 * (level - 10) / 990


def design_level(level, rng):
    d = base_difficulty(level)
    tag = "tutorial" if level <= 10 else TAG_BY_POS[(level - 1) % 10]
    if level > 10:
        # rhythm is softer early, full strength later
        strength = min(1.0, 0.4 + level / 150)
        d += CHAPTER_OFFSET[(level - 1) % 10] * strength
        if level % MILESTONE_EVERY == 0:
            d += MILESTONE_BONUS
            tag = "super_hard"
        elif level % MILESTONE_EVERY == 1 and level > MILESTONE_EVERY:
            d -= 0.12
            tag = "reward"
        d += rng.uniform(-0.015, 0.015)
    d = max(0.0, min(1.0, d))

    crates = round(MIN_CRATES + d * (MAX_CRATES - MIN_CRATES))
    types = round(MIN_TYPES + (d ** 0.85) * (MAX_TYPES - MIN_TYPES))
    if level <= 10:
        crates = [4, 5, 6, 7, 8, 9, 10, 11, 12, 14][level - 1]
        types = [2, 2, 3, 3, 3, 4, 4, 4, 5, 5][level - 1]
    crates = max(MIN_CRATES, min(MAX_CRATES, crates))
    # every kind should have at least 2 crates on average, otherwise the level is too random
    types = max(MIN_TYPES, min(MAX_TYPES, types, crates // 2))

    toys = crates * 3
    sec_per_toy = SEC_PER_TOY_EASY + (SEC_PER_TOY_HARD - SEC_PER_TOY_EASY) * d
    min_duration = MIN_DURATION_NEW_PLAYER if level <= 20 else MIN_DURATION
    duration = max(min_duration, int(round(toys * sec_per_toy / 5.0)) * 5)
    return {
        "level": level,
        "tag": tag,
        "difficulty": round(d, 3),
        "crates": crates,
        "toys": toys,
        "types": types,
        "duration": duration,
        "sec_per_toy": round(duration / toys, 2),
    }


def support_block(support):
    if not support:
        return "  supportToyList: []\n"
    lines = ["  supportToyList:\n"]
    for item in support:
        lines.append(f"  - id: {{fileID: 11400000, guid: {item['guid']}, type: 2}}\n")
        lines.append(f"    amount: {item['amount']}\n")
    return "".join(lines)


def write_level_asset(path, name, row, support):
    text = open(path).read()
    head = text.split("  totalInfo:")[0]
    head = re.sub(r"m_Name: .*", f"m_Name: {name}", head)
    body = (
        "  totalInfo:\n"
        f"    totalTriad: {row['crates']}\n"
        f"    typeUsed: {row['types']}\n"
        "  targetInfo:\n"
        f"    totalTriad: {row['crates']}\n"
        f"    typeUsed: {row['types']}\n"
        + support_block(support) +
        f"  levelDuration: {row['duration']}\n"
    )
    open(path, "w").write(head + body)


def update_storage(level_count):
    text = open(STORAGE_PATH).read()
    start = text.index("  allLevelData:\n") + len("  allLevelData:\n")
    end = text.index("  toyStorage:")
    entries = text[start:end].splitlines(keepends=True)
    text = text[:start] + "".join(entries[:level_count]) + text[end:]
    text = re.sub(r"(\n  repeatFromLevel: )\d+", rf"\g<1>{REPEAT_FROM_LEVEL}", text, count=1)
    open(STORAGE_PATH, "w").write(text)
    return len(entries)


def main():
    rng = random.Random(20261111)
    rows = [design_level(level, rng) for level in range(1, LEVEL_COUNT + 1)]
    supports = {int(k): v for k, v in json.load(open(SUPPORT_PATH)).items()}
    for index, row in enumerate(rows):
        path = os.path.join(GENERATED_DIR, f"Level_{index}.asset")
        support = supports.get(index, [])
        row["support"] = " ".join(f"{'bolt' if s['guid'].startswith('f6e2') else 'clock'}x{s['amount']}" for s in support)
        write_level_asset(path, f"Level_{index}", row, support)
    old_count = update_storage(LEVEL_COUNT)
    with open(CSV_PATH, "w", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)
    print(f"wrote {len(rows)} levels, storage list {old_count} -> {LEVEL_COUNT}, csv: {CSV_PATH}")


if __name__ == "__main__":
    main()
