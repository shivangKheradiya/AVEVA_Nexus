"""POST /api/PML/Run. Needs a session started with NEXUS_READONLY=0 (pytest -m writable)."""
import pytest

pytestmark = pytest.mark.writable


def run(api, script, ret=None):
    return api("POST", "/api/PML/Run", {"script": script, "return": ret or []})


def test_run_returns_requested_variables_with_types(api):
    status, body = run(api, "!!nxT = |abc|\n!!nxN = 42\n!!nxB = true", ["nxT", "nxN", "nxB"])
    assert status == 200 and body["Success"] is True
    data = body["Data"]
    assert data["Ok"] is True and data["Errors"] == []
    assert data["Values"] == {"nxT": "abc", "nxN": 42, "nxB": True}


def test_run_collects_array(api):
    status, body = run(api, "var !!nxArr collect all ZONE for =201346360/1", ["nxArr"])
    assert status == 200
    values = body["Data"]["Values"]["nxArr"]
    assert isinstance(values, list) and len(values) >= 1
    assert all(v.startswith("=") for v in values)


def test_run_reports_errors_and_continues(api):
    status, body = run(api, "var !!nxE current MDB\n!!nxOk = |y|", ["nxOk", "nxE"])
    assert status == 200  # errors are data for the caller
    data = body["Data"]
    assert data["Ok"] is False
    assert len(data["Errors"]) == 1 and "var !!nxE current MDB" in data["Errors"][0]
    assert data["Values"]["nxOk"] == "y"
    assert data["Values"]["nxE"] is None


def test_run_undefined_return_is_null(api):
    status, body = run(api, "!!nxD = |1|", ["nxNeverDefined"])
    assert status == 200
    assert body["Data"]["Values"]["nxNeverDefined"] is None


def test_run_input_validation(api):
    assert api("POST", "/api/PML/Run", {})[0] == 400
    assert run(api, "   ")[0] == 400
    assert run(api, "!!nxX = |1|", ["x ) ; y"])[0] == 400
    assert api("GET", "/api/PML/Run")[0] == 405
    assert api("GET", "/api/Health")[0] == 200
