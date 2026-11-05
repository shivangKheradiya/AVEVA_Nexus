"""PML endpoints. Execute needs a session where PML execution is allowed (writable)."""
import concurrent.futures

import pytest

pytestmark = pytest.mark.writable


def execute(api, command):
    return api("POST", "/api/PML/Execute", {"command": command})


def test_execute_valid_ok(api):
    status, body = execute(api, "!!nxA = |x|")
    assert status == 200
    assert body["Success"] is True
    status, body = api("GET", "/api/PML/Variable/String/nxA")
    assert (status, body["Data"]) == (200, "x")


def test_execute_reports_pml_error(api):
    status, body = execute(api, "var !!nxT current MDB")
    assert status == 400
    assert body["Success"] is False
    assert "PML command failed" in body["Error"]


def test_execute_missing_command_is_400(api):
    status, body = api("POST", "/api/PML/Execute", {})
    assert status == 400


def test_array_keeps_order(api):
    assert execute(api, "var !!nxArr collect all SITE")[0] == 200
    for i in (1, 2, 3):
        assert execute(api, f"!!nxA{i} = !!nxArr[{i}]")[0] == 200
    expected = [api("GET", f"/api/PML/Variable/String/nxA{i}")[1]["Data"] for i in (1, 2, 3)]
    status, body = api("GET", "/api/PML/Variable/Array/nxArr")
    assert status == 200
    assert body["Data"][:3] == expected


def test_variable_name_with_pml_injection_is_rejected(api):
    status, body = api("GET", "/api/PML/Variable/String/x%20)%3B%20x")
    assert status == 400


def test_parallel_executes_all_succeed(api):
    def work(i):
        return i, execute(api, f"!!nxP{i} = |{i}|")[0]

    with concurrent.futures.ThreadPoolExecutor(max_workers=10) as pool:
        results = list(pool.map(work, range(20)))
    assert all(status == 200 for _, status in results)
    for i in range(20):
        status, body = api("GET", f"/api/PML/Variable/String/nxP{i}")
        assert (status, body["Data"]) == (200, str(i))
    assert api("GET", "/api/Health")[0] == 200
