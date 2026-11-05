"""GET /api/Session/Info. Assumes the CAR project."""


def test_session_info_has_project_mdb_and_module(api):
    status, body = api("GET", "/api/Session/Info")
    assert status == 200 and body["Success"] is True
    info = body["Data"]
    assert info["Project"] == "CAR"
    assert info["Mdb"] == "001-REFINERIA-CAR"
    assert info["User"]
    assert info["Module"]
    assert info["E3dVersion"]
    assert isinstance(info["ReadOnly"], bool)


def test_session_info_requires_get(api):
    assert api("POST", "/api/Session/Info", {})[0] == 405
