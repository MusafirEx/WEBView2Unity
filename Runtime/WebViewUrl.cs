using System;

namespace MusafirEx.WEBView2Unity
{
    /// <summary>HTTP(S) navigation helper; rejects file and scripting schemes.</summary>
    public static class WebViewUrl
    {
        public static bool TryNormalize(string input, out string normalized, out string error)
        {
            normalized = null;
            error = null;
            if (string.IsNullOrWhiteSpace(input))
            {
                error = "Enter a URL.";
                return false;
            }

            string value = input.Trim();
            if (!value.Contains("://") && !value.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
                && !value.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                && !value.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
                value = "https://" + value;

            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                string.IsNullOrWhiteSpace(uri.Host))
            {
                error = "Only absolute HTTP/HTTPS URLs are supported.";
                return false;
            }

            normalized = uri.AbsoluteUri;
            return true;
        }
    }
}
