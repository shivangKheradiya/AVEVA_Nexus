namespace AVEVA_Nexus.Http
{
    // Cross-origin rules. Pure logic: must not reference Aveva.* (it is compiled by the unit-test runner).
    //
    // With a token configured, browser clients may talk to the API from any origin: the token is the protection.
    // Without a token (the local, token-less setup) nothing stops other web pages from calling the API through the
    // user's browser, so any request that declares an Origin is refused and no CORS headers are sent.
    public static class CorsPolicy
    {
        public static bool SendCorsHeaders(string token)
        {
            return !string.IsNullOrEmpty(token);
        }

        public static bool IsOriginAllowed(string token, string origin)
        {
            return string.IsNullOrEmpty(origin) || SendCorsHeaders(token);
        }
    }
}
