# -*- coding: utf-8 -*-
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / "stitch_soraeru_mnemonic_vocabulary_app"
OUT = Path(__file__).resolve().parent / "assets" / "screens"
OUT.mkdir(parents=True, exist_ok=True)

SCREENS = [
    ("l00_splash_screen", "L00_splash"),
    ("l01_login_screen", "L01_login"),
    ("l04_onboarding_screen_mvp_rev", "L04_onboarding"),
    ("l05_home_screen_mvp_rev", "L05_home"),
    ("l06_word_input_screen", "L06_input"),
    ("l07_image_pick_screen", "L07_image"),
    ("l08_ocr_select_screen", "L08_ocr"),
    ("l09_analyzing_screen", "L09_analyzing"),
    ("l10_analysis_result_mvp_rev", "L10_result"),
    ("l11_notebook_list_mvp_rev", "L11_notebook"),
    ("l12_notebook_detail_screen", "L12_detail"),
    ("l13_settings_screen_mvp_rev", "L13_settings"),
]


def main():
    with sync_playwright() as p:
        browser = p.chromium.launch()
        page = browser.new_page(
            viewport={"width": 390, "height": 844},
            device_scale_factor=2,
        )
        for folder, name in SCREENS:
            html = BASE / folder / "code.html"
            url = html.resolve().as_uri()
            print(f"capturing {name} ...")
            page.goto(url, wait_until="networkidle", timeout=120000)
            page.wait_for_timeout(1800)
            page.screenshot(path=str(OUT / f"{name}.png"), full_page=False)
        browser.close()
    print(f"done -> {OUT}")


if __name__ == "__main__":
    main()
