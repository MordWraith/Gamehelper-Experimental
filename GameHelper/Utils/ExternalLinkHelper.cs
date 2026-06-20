namespace GameHelper.Utils
{
    using System;
    using System.Diagnostics;

    internal static class ExternalLinkHelper
    {
        internal static bool TryOpen(string? url, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(url) ||
                !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                error = "Invalid URL.";
                return false;
            }

            try
            {
                Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
