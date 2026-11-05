"""Element/Get, Element/Children, Element/AttributeList (+ the original Element/Attribute).

Assumes the CAR project: site =201346360/1 holds zones.
"""
SITE_WITH_ZONES = "=201346360/1"


def first_site_refs(api, n):
    _, body = api("POST", "/api/Element/Query", {"type": "SITE", "limit": n})
    return [row["dbref"] for row in body["Data"]["Rows"]]


def test_get_batch_returns_rows_in_input_order(api):
    refs = first_site_refs(api, 3)[::-1]  # deliberately reversed
    status, body = api("POST", "/api/Element/Get", {"refs": refs, "attributes": ["NAME", "TYPE"]})
    assert status == 200 and body["Success"] is True
    rows = body["Data"]["Rows"]
    assert [r["dbref"] for r in rows] == refs
    assert all(r["TYPE"] == "SITE" and r["NAME"].startswith("/") for r in rows)
    assert body["Data"]["Columns"] == ["dbref", "NAME", "TYPE"]
    assert body["Data"]["Total"] == 3


def test_get_unknown_attribute_is_null(api):
    refs = first_site_refs(api, 1)
    status, body = api("POST", "/api/Element/Get", {"refs": refs, "attributes": ["NAME", "NOSUCHATTR"]})
    assert status == 200
    assert body["Data"]["Rows"][0]["NOSUCHATTR"] is None


def test_get_invalid_ref_is_404_not_500(api):
    assert api("POST", "/api/Element/Get", {"refs": ["=0/0"], "attributes": ["NAME"]})[0] == 404
    ok_ref = first_site_refs(api, 1)[0]
    assert api("POST", "/api/Element/Get", {"refs": [ok_ref, "=0/0"], "attributes": ["NAME"]})[0] == 404
    assert api("POST", "/api/Element/Get", {"refs": ["garbage"], "attributes": ["NAME"]})[0] == 400
    assert api("GET", "/api/Health")[0] == 200


def test_get_requires_refs_and_post(api):
    assert api("POST", "/api/Element/Get", {"refs": [], "attributes": ["NAME"]})[0] == 400
    assert api("POST", "/api/Element/Get", {})[0] == 400
    assert api("GET", "/api/Element/Get")[0] == 405


def test_children_of_site_are_zones(api):
    status, body = api("GET", f"/api/Element/Children?dbref={SITE_WITH_ZONES}")
    assert status == 200
    data = body["Data"]
    assert data["Owner"].startswith("=")
    assert len(data["Children"]) >= 1
    for child in data["Children"]:
        assert child["type"] == "ZONE"
        assert child["dbref"].startswith("=") and child["name"].startswith("/")

    status, body = api("GET", f"/api/Element/Children?dbref={SITE_WITH_ZONES}&type=ZONE")
    assert status == 200 and len(body["Data"]["Children"]) == len(data["Children"])

    status, body = api("GET", f"/api/Element/Children?dbref={SITE_WITH_ZONES}&type=PIPE")
    assert status == 200 and body["Data"]["Children"] == []


def test_children_invalid_inputs(api):
    assert api("GET", "/api/Element/Children?dbref==0/0")[0] == 404
    assert api("GET", "/api/Element/Children?dbref=garbage")[0] == 400
    assert api("GET", "/api/Element/Children")[0] == 400
    assert api("GET", f"/api/Element/Children?dbref={SITE_WITH_ZONES}&type=FOO")[0] == 400
    assert api("GET", "/api/Health")[0] == 200


def test_attributelist_pipe_contains_system_and_uda_names(api):
    status, body = api("GET", "/api/Element/AttributeList?type=PIPE")
    assert status == 200
    names = body["Data"]
    assert "NAME" in names and "BORE" in names
    assert any(n.startswith(":") for n in names)
    assert len(names) == len(set(names))


def test_attributelist_unknown_or_missing_type_is_400(api):
    assert api("GET", "/api/Element/AttributeList?type=FOO")[0] == 400
    assert api("GET", "/api/Element/AttributeList")[0] == 400
    assert api("GET", "/api/Health")[0] == 200


def test_original_single_attribute_endpoint_still_works(api):
    status, body = api("GET", f"/api/Element/Attribute?dbref={SITE_WITH_ZONES}&attribute=NAME")
    assert status == 200
    assert body["Data"].startswith("/")
