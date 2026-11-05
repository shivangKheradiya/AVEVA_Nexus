"""Cross-origin protection when no NEXUS_TOKEN is configured (the local, token-less setup).

Skipped when the tests run against a token-protected Nexus (NEXUS_TOKEN set).
"""
import json
import os
import urllib.error
import urllib.request

import pytest

pytestmark = pytest.mark.skipif(bool(os.environ.get("NEXUS_TOKEN")), reason="token-protected session")


def raw(base_url, method, path, headers=None, body=None):
    data = None if body is None else json.dumps(body).encode("utf-8")
    req = urllib.request.Request(base_url + path, data=data, method=method)
    for k, v in (headers or {}).items():
        req.add_header(k, v)
    if data is not None:
        req.add_header("Content-Type", "application/json")
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            return resp.status, dict(resp.headers), resp.read().decode("utf-8")
    except urllib.error.HTTPError as err:
        return err.code, dict(err.headers), err.read().decode("utf-8")


def has_header(headers, name):
    return name.lower() in {k.lower() for k in headers}


def test_cross_origin_requests_are_refused_without_a_token(base_url):
    evil = {"Origin": "https://evil.example"}
    for method, path, body in (
        ("GET", "/api/Element/AttributeList?type=PIPE", None),
        ("POST", "/api/Element/Query", {"type": "SITE", "limit": 1}),
        ("GET", "/api/Health", None),
    ):
        status, headers, text = raw(base_url, method, path, evil, body)
        assert status == 403, (method, path)
        assert not has_header(headers, "Access-Control-Allow-Origin")
        assert "Cross-origin" in json.loads(text)["Error"]


def test_preflight_is_refused_without_a_token(base_url):
    headers = {
        "Origin": "https://evil.example",
        "Access-Control-Request-Method": "POST",
        "Access-Control-Request-Headers": "content-type",
    }
    status, resp_headers, _ = raw(base_url, "OPTIONS", "/api/Element/Query", headers)
    assert status == 403
    assert not has_header(resp_headers, "Access-Control-Allow-Origin")


def test_requests_without_origin_work_and_carry_no_cors_headers(base_url):
    status, headers, _ = raw(base_url, "GET", "/api/Health")
    assert status == 200
    assert not has_header(headers, "Access-Control-Allow-Origin")
    status, _, _ = raw(base_url, "POST", "/api/Element/Query", body={"type": "SITE", "limit": 1})
    assert status == 200
