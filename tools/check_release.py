"""Check the complete authored route, unlock scenes, and compositing bounds."""
import json
import re
from pathlib import Path

from compile_story import BOOK, ROOT


GALLERY_UNLOCKS = [
    ("ep001-delivery", "돌아오지 않는 통로"),
    ("ep001-morning", "월요일의 커피"),
    ("ep002-fall", "비가 오던 베란다"),
    ("ep002-bandage", "난간 위의 구급함"),
    ("ep003-table", "창가의 지도"),
    ("ep005-pizza", "두 번째 베란다"),
    ("ep006-stage", "네 번의 박자"),
    ("ep009-underpass", "철길 아래의 거미"),
    ("ep020-confess", "식탁 위 두 사람"),
    ("ep029-landing", "오래된 동료"),
    ("ep033-intercept", "팬텀"),
    ("ep034-vigil", "기다리는 의자"),
    ("ep039-voice", "검은 거미"),
    ("ep044-copy", "보라색 링"),
    ("ep050-dinner", "두 그릇의 저녁"),
]


def require(ok, message):
    if not ok:
        raise ValueError(message)


def main():
    plan = (ROOT / "STORY_BIBLE.md").read_text(encoding="utf8")
    expected = re.findall(r"^(\d+)\. ([^:\n]+):", plan, flags=re.MULTILINE)
    episodes = json.loads(BOOK.read_text(encoding="utf8"))["episodes"]
    require(len(expected) == 50, "Missing chapter entries in source bible")
    require([(str(i), ep["title"]) for i, ep in enumerate(episodes, 1)] == expected,
            "Runtime chapters do not match the story bible titles and sequence")
    scenes = {s["id"]: s for ep in episodes for s in ep["scenes"]}
    for scene in scenes.values():
        require(scene["pov"] in {"윤", "그웬"}, f"Unsupported narrative POV: {scene['id']}")
    require([(s["id"], s["gallery"]) for s in scenes.values() if s["gallery"]] == GALLERY_UNLOCKS,
            "Gallery unlock IDs, keys or order have changed")
    ends = {ep["id"]: ep["scenes"][-1]["lines"][-1]["id"] for ep in episodes}
    with_layers = [s for s in scenes.values() if s["layers"]]
    require(len(with_layers) >= 10, "Expected reusable costumes across key scenes")

    def lines(key):
        return scenes[key]["lines"]

    def contains(key, who, phrase):
        return any(line["speaker"] == who and phrase in line["text"] for line in lines(key))

    require(contains("ep002-distance", "윤", "손 안 대요"), "Episode 2 must keep the mask on")
    require(contains("ep002-bandage", "", "왼쪽 손목") and
            contains("ep009-underpass", "", "왼쪽 손목"),
            "The fall and identity deduction must use the same wrist")
    require(contains("ep003-stairs", "", "붉은 끈이 두 번 매듭") and
            contains("ep009-underpass", "", "붉은 이중 매듭"),
            "The backpack clue must be established before the deduction")
    require(scenes["ep002-fall"]["layers"][0]["art"] == "gwen_down", "Incorrect first balcony pose")
    require(scenes["ep002-distance"]["layers"][0]["art"] == "gwen_seated", "Incorrect first aid pose")
    require(scenes["ep005-pizza"]["layers"][0]["art"] == "gwen_standing", "Incorrect return pose")
    require(contains("ep009-underpass", "윤", "그래서 맨날 사라졌구나"), "Episode 9 deduction missing")
    require(contains("ep015-name", "윤", "그 금요일 저녁부터 알았어"), "Episode 15 recognition missing")
    require(contains("ep020-confess", "그웬", "나 윤 좋아해"), "Episode 20 confession missing")
    require(contains("ep021-kiss", "윤", "응") and contains("ep021-kiss", "", "입을 맞췄다"),
            "Episode 21 must show consent and the first kiss")
    reveal = lines("ep033-intercept")
    block = next(i for i, line in enumerate(reveal) if "보랏빛이 흰 마스크 앞을" in line["text"])
    name = next(i for i, line in enumerate(reveal)
                if line["speaker"] == "레버넌트" and "팬텀" in line["text"])
    require(block < name, "Phantom identity must be revealed after Yun intervenes")
    require(contains("ep033-rescue", "그웬", "숨은 쉬어요"), "Episode 33 needs Gwen's active rescue")
    require(contains("ep039-call", "", "세 갈래 표식과 그 아래 번호") and
            contains("ep042-share", "", "일련번호"),
            "The symbiote shipment photo must contain the evidence used to identify the lab")
    require(contains("ep041-release", "", "손가락을 접었다") and
            contains("ep042-hands", "그웬", "힘을 쓰기 전에"),
            "Gwen must choose to control the symbiote")
    require(contains("ep032-citizens", "", "마지막 한 명") and
            contains("ep032-realize", "", "왼쪽 손목") and
            contains("ep033-losefoot", "", "왼손목"),
            "The depot rescue and wrist injury must carry into episode 33")
    require(contains("ep044-stairs", "윤", "세 명") and
            contains("ep046-outside", "", "세 명이 모두 나왔다는"),
            "The research facility evacuation count must match")
    require(contains("ep046-outside", "", "두 번째 불") and
            not contains("ep046-outside", "", "세 번째") and
            contains("ep047-coordinates", "", "세 번째"),
            "The second and third flashes must happen once and in separate chapters")
    shutdown = [line["text"] for line in lines("ep047-homeward")]
    moving = next(i for i, text in enumerate(shutdown) if "이동 링이 완전히 멎었다" in text)
    fixed = next(i for i, text in enumerate(shutdown) if "고정 링까지 끄자" in text)
    require(moving < fixed and "사라졌다" in shutdown[fixed], "The fixed ring closes last after escape")
    require(contains("ep048-desk", "윤", "압니다"), "Episode 48 accountability beat missing")
    require(contains("ep048-paper", "", "선고받은 형을 복역") and
            contains("ep049-ticket_back", "", "면담과 보고 의무를 마친 뒤"),
            "The reunion must follow sentencing, incarceration, and supervision")
    require(ends["ep050"] == "ep050-last-050", "Episode 50 must end at the dinner epilogue")
    print(f"PASS: {len(episodes)} titles, {len(scenes)} scenes, {len(with_layers)} layered scenes, "
          f"{len(GALLERY_UNLOCKS)} gallery unlocks, canon anchors")


if __name__ == "__main__":
    main()
