def test_health_ok(api):
    status, body = api("GET", "/api/Health")
    assert status == 200
    assert body["Success"] is True
