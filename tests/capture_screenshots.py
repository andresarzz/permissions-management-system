#!/usr/bin/env python3
"""Capture real ASP.NET UI screenshots from the ephemeral integration-test database."""
import os
from pathlib import Path
from playwright.sync_api import sync_playwright

base = os.environ.get("SMOKE_BASE_URL", "http://127.0.0.1:5050").rstrip("/")
user = "smoke_" + os.environ["GITHUB_RUN_ID"]
password = os.environ["SMOKE_TEST_PASSWORD"]
out = Path("docs/screenshots")
out.mkdir(parents=True, exist_ok=True)

with sync_playwright() as pw:
    browser = pw.chromium.launch(headless=True, args=["--no-sandbox"])
    page = browser.new_page(viewport={"width": 1440, "height": 900}, device_scale_factor=1)
    page.goto(base + "/Account/Login", wait_until="networkidle")
    page.screenshot(path=str(out / "login.png"), full_page=True)
    page.locator("#UsernameOrEmail").fill(user)
    page.locator("#Password").fill(password)
    page.locator('button[type="submit"]').click()
    page.wait_for_url("**/Docente/Dashboard", timeout=15000)
    page.screenshot(path=str(out / "docente-dashboard.png"), full_page=True)
    page.goto(base + "/Permisos/Index", wait_until="networkidle")
    page.screenshot(path=str(out / "permisos.png"), full_page=True)
    browser.close()
print("Captured three genuine UI screenshots against disposable test data.")
