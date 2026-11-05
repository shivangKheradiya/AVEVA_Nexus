"""Read-only enforcement. Needs a session started WITHOUT NEXUS_READONLY=0 (pytest -m readonly)."""
import pytest

pytestmark = pytest.mark.readonly

MESSAGE = "Nexus is in read-only mode (set NEXUS_READONLY=0 to allow PML execution)"


def test_readonly_blocks_execute_and_run(api):
    status, body = api("POST", "/api/PML/Execute", {"command": "!!nxRo = |1|"})
    assert status == 403 and body["Error"] == MESSAGE
    status, body = api("POST", "/api/PML/Run", {"script": "!!nxRo = |1|", "return": ["nxRo"]})
    assert status == 403 and body["Error"] == MESSAGE
    # nothing was executed
    assert api("GET", "/api/PML/Variable/String/nxRo")[0] == 404


def test_readonly_allows_queries_and_reads(api):
    assert api("POST", "/api/Element/Query", {"type": "SITE", "limit": 1})[0] == 200
    assert api("GET", "/api/Element/AttributeList?type=PIPE")[0] == 200
    assert api("GET", "/api/Session/Info")[1]["Data"]["ReadOnly"] is True
    assert api("GET", "/api/Health")[0] == 200
