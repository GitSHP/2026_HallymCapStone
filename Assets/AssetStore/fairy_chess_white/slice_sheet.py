#!/usr/bin/env python3
"""
Fairy chess sprite sheet slicer
- 시트(PNG)를 기물별 PNG로 잘라 저장합니다.
- 같은 폴더에 JSON이 있으면 JSON의 좌표/이름을 사용하고, 없으면 16x32 격자로 자릅니다.

사용법:
    python slice_sheet.py fairy_chess_white_sheet.png
    python slice_sheet.py fairy_chess_white_sheet.png --out pieces --scale 4
    python slice_sheet.py my_sheet.png --cell 16x32        (JSON 없이 격자로)

필요: Pillow  (pip install pillow)
"""
import argparse
import json
import os
import sys

from PIL import Image

DEFAULT_NAMES = [
    "amazon", "chancellor", "archbishop", "ferz", "alfil", "centaur",
    "zebra", "dabbaba", "giraffe", "wazir", "cannon", "nightrider",
    "mann", "grasshopper", "camel", "wildebeest", "dragon_king", "dragon_horse",
]


def frames_from_json(path):
    with open(path, encoding="utf-8") as f:
        data = json.load(f)
    out = []
    for key, fr in data["frames"].items():
        r = fr["frame"]
        out.append((fr.get("index", len(out)), os.path.splitext(key)[0],
                    (r["x"], r["y"], r["x"] + r["w"], r["y"] + r["h"])))
    out.sort()
    return [(name, box) for _, name, box in out]


def frames_from_grid(im, cw, ch):
    cols, rows = im.width // cw, im.height // ch
    out = []
    for i in range(cols * rows):
        x, y = (i % cols) * cw, (i // cols) * ch
        name = DEFAULT_NAMES[i] if i < len(DEFAULT_NAMES) else f"piece_{i:02d}"
        out.append((name, (x, y, x + cw, y + ch)))
    return out


def main():
    ap = argparse.ArgumentParser(description="Slice a sprite sheet into PNGs")
    ap.add_argument("sheet")
    ap.add_argument("--json", help="frame JSON (default: <sheet>.json if present)")
    ap.add_argument("--out", default="pieces", help="output folder")
    ap.add_argument("--cell", default="16x32", help="cell size when no JSON, e.g. 16x32")
    ap.add_argument("--scale", type=int, default=1, help="nearest-neighbour upscale (1 = original)")
    ap.add_argument("--no-index", action="store_true", help="omit 01_ prefix in filenames")
    a = ap.parse_args()

    im = Image.open(a.sheet).convert("RGBA")
    js = a.json or os.path.splitext(a.sheet)[0] + ".json"
    if os.path.exists(js):
        frames = frames_from_json(js)
        print(f"using {js}")
    else:
        cw, ch = (int(v) for v in a.cell.lower().split("x"))
        frames = frames_from_grid(im, cw, ch)
        print(f"using grid {cw}x{ch}")

    os.makedirs(a.out, exist_ok=True)
    n = 0
    for i, (name, box) in enumerate(frames):
        tile = im.crop(box)
        if tile.getbbox() is None:   # 빈 칸은 건너뜀
            continue
        if a.scale > 1:
            tile = tile.resize((tile.width * a.scale, tile.height * a.scale), Image.NEAREST)
        fn = f"{name}.png" if a.no_index else f"{i + 1:02d}_{name}.png"
        tile.save(os.path.join(a.out, fn))
        n += 1
    print(f"saved {n} sprites -> {a.out}/")


if __name__ == "__main__":
    sys.exit(main())
