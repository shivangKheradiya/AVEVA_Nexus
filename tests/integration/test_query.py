"""POST /api/Element/Query. Read-only; works in any mode.

These tests assume the CAR project (MDB 001-REFINERIA-CAR): zone =201346360/2 holds pipes.
"""
ZONE_WITH_PIPES = "=201346360/2"


def query(api, **body):
    return api("POST", "/api/Element/Query", body)


def test_query_all_sites_has_total_and_rows(api):
    status, body = query(api, type="SITE", attributes=["NAME"], limit=5)
    assert status == 200 and body["Success"] is True
    data = body["Data"]
    assert data["Total"] >= 5
    assert len(data["Rows"]) == 5
    assert data["Columns"] == ["dbref", "NAME"]
    for row in data["Rows"]:
        assert row["dbref"].startswith("=")
        assert row["NAME"].startswith("/")


def test_query_scope_and_where(api):
    status, body = query(api, type="PIPE", scope=ZONE_WITH_PIPES, attributes=["NAME", "BORE"], limit=3)
    assert status == 200
    assert body["Data"]["Total"] >= 1
    assert all(r["dbref"] for r in body["Data"]["Rows"])

    status, body = query(api, type="PIPE", scope=ZONE_WITH_PIPES, where="(NAME EQ '/DOES-NOT-EXIST')")
    assert status == 200
    assert body["Data"]["Total"] == 0 and body["Data"]["Rows"] == []


def test_query_where_filters_rows(api):
    _, first = query(api, type="PIPE", scope=ZONE_WITH_PIPES, attributes=["NAME"], limit=1)
    name = first["Data"]["Rows"][0]["NAME"]
    status, body = query(api, type="PIPE", scope=ZONE_WITH_PIPES, attributes=["NAME"], where=f"(NAME EQ '{name}')")
    assert status == 200
    assert body["Data"]["Total"] == 1
    assert body["Data"]["Rows"][0]["NAME"] == name


def test_query_unknown_type_is_400(api):
    status, body = query(api, type="FOO")
    assert status == 400
    assert body["Success"] is False
    assert "FOO" in body["Error"]
    assert api("GET", "/api/Health")[0] == 200  # the session survived


def test_query_unknown_attribute_in_where_is_400(api):
    status, body = query(api, type="SITE", where="(NOSUCHATTR EQ 1)")
    assert status == 400
    assert "PML" in body["Error"]


def test_query_injection_is_400(api):
    for where in ("1 EQ 1; SAVEWORK", "NAME EQ 'x'\nSAVEWORK", "$M/evil", "(NAME EQ 'x'"):
        status, _ = query(api, type="SITE", where=where)
        assert status == 400, where
    status, _ = query(api, type="SITE; SAVEWORK")
    assert status == 400
    status, _ = query(api, type="SITE", scope="/A;B")
    assert status == 400


def test_query_offset_beyond_total(api):
    _, base = query(api, type="SITE", limit=1)
    total = base["Data"]["Total"]
    status, body = query(api, type="SITE", offset=100000, limit=5)
    assert status == 200
    assert body["Data"]["Rows"] == []
    assert body["Data"]["Total"] == total


def test_query_unknown_attribute_is_null(api):
    status, body = query(api, type="SITE", attributes=["NAME", "NOSUCHATTR"], limit=2)
    assert status == 200
    for row in body["Data"]["Rows"]:
        assert row["NAME"].startswith("/")
        assert row["NOSUCHATTR"] is None


def test_query_limit_is_capped(api):
    status, body = query(api, type="SITE", limit=99999)
    assert status == 200
    assert body["Data"]["Limit"] == 1000
    assert len(body["Data"]["Rows"]) <= 1000


def test_query_default_limit_is_100(api):
    status, body = query(api, type="SITE")
    assert status == 200
    assert body["Data"]["Limit"] == 100
    assert len(body["Data"]["Rows"]) == min(100, body["Data"]["Total"])


def test_query_nonexistent_scope_is_404(api):
    # Review finding: PML answers an unknown scope with an empty result, so callers concluded "no pipes here".
    assert query(api, type="PIPE", scope="=0/0")[0] == 404
    status, body = query(api, type="PIPE", scope="/NO-SUCH-ELEMENT-NAME")
    assert status == 404 and "not found" in body["Error"]
    assert api("GET", "/api/Health")[0] == 200


def test_query_scope_by_name_works(api):
    # /CAR-480-PI is the name of site =201346360/1, which holds zones
    status, body = query(api, type="ZONE", scope="/CAR-480-PI")
    assert status == 200 and body["Data"]["Total"] >= 1
    status, by_ref = query(api, type="ZONE", scope="=201346360/1")
    assert by_ref["Data"]["Total"] == body["Data"]["Total"]


def test_parallel_queries_do_not_interleave(api):
    # Shared PML globals (!!nexusQ ...) must never mix results of simultaneous queries.
    import concurrent.futures

    cases = [
        {"type": "SITE", "attributes": ["NAME"], "limit": 50},
        {"type": "ZONE", "scope": "=201346360/1", "attributes": ["NAME"]},
        {"type": "PIPE", "scope": ZONE_WITH_PIPES, "attributes": ["NAME", "BORE"], "limit": 50},
    ]
    serial = [query(api, **c)[1]["Data"] for c in cases]
    assert len({d["Total"] for d in serial}) == 3  # the three cases really differ

    def work(i):
        status, body = query(api, **cases[i % 3])
        return status, body["Data"]

    with concurrent.futures.ThreadPoolExecutor(max_workers=10) as pool:
        results = list(pool.map(work, range(30)))
    for i, (status, data) in enumerate(results):
        assert status == 200
        assert data == serial[i % 3], f"result {i} differs from its serial run"
    assert api("GET", "/api/Health")[0] == 200


def test_query_requires_post_and_body(api):
    assert api("GET", "/api/Element/Query")[0] == 405
    assert api("POST", "/api/Element/Query", {})[0] == 400
