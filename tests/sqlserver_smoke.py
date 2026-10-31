#!/usr/bin/env python3
"""HTTP smoke test against a disposable SQL Server-backed ASP.NET app."""
import http.cookiejar
import html
import os
import re
import sys
import urllib.error
import urllib.parse
import urllib.request

BASE = os.environ.get("SMOKE_BASE_URL", "http://127.0.0.1:5050").rstrip("/")
jar = http.cookiejar.CookieJar()
opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar))
def call(path, fields=None):
    data = urllib.parse.urlencode(fields).encode() if fields is not None else None
    req = urllib.request.Request(BASE + path, data=data)
    try:
        with opener.open(req, timeout=15) as resp:
            return resp.status, resp.url, resp.read().decode("utf-8", errors="replace")
    except urllib.error.HTTPError as exc:
        return exc.code, exc.url, exc.read().decode("utf-8", errors="replace")
def token(page):
    m = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page)
    if not m:
        m = re.search(r'value="([^"]+)"[^>]*name="__RequestVerificationToken"', page)
    if not m:
        raise AssertionError("Antiforgery token missing")
    return html.unescape(m.group(1))
def check(name, condition, details=""):
    if not condition:
        raise AssertionError(f"{name} FAILED: {details}")
    print("PASS:", name, flush=True)

status, url, page = call("/Account/Register")
check("register page available", status == 200, str(status))
user = "smoke_" + os.environ.get("GITHUB_RUN_ID", "local")
email = user + "@example.invalid"
password = os.environ["SMOKE_TEST_PASSWORD"]
registration = {
    "__RequestVerificationToken": token(page),
    "Nombre": "Smoke",
    "Apellidos": "Test",
    "NombreUsuario": user,
    "Email": email,
    "Password": password,
    "ConfirmPassword": password,
    "Rol": "Admin",  # deliberately forged, must be ignored by the server
}
status, url, page = call("/Account/Register", registration)
check("registration accepted", status == 200 and "/Account/Login" in url,
      f"status={status} location={url}")

status, url, page = call("/Account/Login")
check("login page", status == 200)
status, url, page = call("/Account/Login", {
    "__RequestVerificationToken": token(page),
    "UsernameOrEmail": email,
    "Password": password,
    "RememberMe": "false",
})
check("login works", status == 200 and "/Docente/Dashboard" in url,
      f"status={status} location={url}")

status, url, page = call("/Admin/Dashboard")
check("forged Admin role denied", "/Account/AccessDenied" in url or status == 403,
      f"status={status} location={url}")

status, url, page = call("/Permisos/Create")
check("authenticated request form available", status == 200 and "/Permisos/Create" in url)
status, url, page = call("/Permisos/Create", {
    "__RequestVerificationToken": token(page),
    "TipoPermiso": "Asuntos Personales",
    "FechaInicio": "2026-11-10",
    "FechaFin": "2026-11-11",
    "Motivo": "Prueba automatizada de permisos",
    "JornadaCompleta": "true",
})
check("create request persists and redirects", status == 200 and "/Permisos/Index" in url,
      f"status={status} location={url}")

status, url, page = call("/Reportes/Create")
check("report form available", status == 200 and "/Reportes/Create" in url)
status, url, page = call("/Reportes/Create", {
    "__RequestVerificationToken": token(page),
    "TipoDano": "Hardware",
    "Descripcion": "Prueba automatizada de un reporte de daño",
    "FechaReporte": "2026-11-10",
    "Equipo": "Equipo de prueba",
    "Estado": "Pendiente",
})
check("report creation persists and redirects", status == 200 and "/Reportes/Index" in url,
      f"status={status} location={url}")
print("All SQL Server smoke tests passed.", flush=True)
