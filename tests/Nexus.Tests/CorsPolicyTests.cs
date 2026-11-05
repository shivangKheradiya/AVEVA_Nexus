using AVEVA_Nexus.Http;

namespace Nexus.Tests
{
    public static class CorsPolicyTests
    {
        public static void RunAll()
        {
            Program.Run("CorsPolicy_without_token_refuses_any_request_that_declares_an_origin", () =>
            {
                // Review finding: with no token, open CORS let any web page read the model through the browser.
                Check.True(!CorsPolicy.IsOriginAllowed(null, "https://evil.example"), "null token");
                Check.True(!CorsPolicy.IsOriginAllowed("", "https://evil.example"), "empty token");
            });

            Program.Run("CorsPolicy_without_token_allows_requests_with_no_origin", () =>
            {
                Check.True(CorsPolicy.IsOriginAllowed(null, null), "no origin header");
                Check.True(CorsPolicy.IsOriginAllowed("", ""), "empty origin header");
            });

            Program.Run("CorsPolicy_with_token_allows_browser_clients", () =>
            {
                Check.True(CorsPolicy.IsOriginAllowed("secret", "https://app.example"), "origin allowed");
                Check.True(CorsPolicy.IsOriginAllowed("secret", null), "no origin");
            });

            Program.Run("CorsPolicy_sends_CORS_headers_only_when_a_token_protects_the_api", () =>
            {
                Check.True(CorsPolicy.SendCorsHeaders("secret"), "token set");
                Check.True(!CorsPolicy.SendCorsHeaders(null), "null token");
                Check.True(!CorsPolicy.SendCorsHeaders(""), "empty token");
            });
        }
    }
}
