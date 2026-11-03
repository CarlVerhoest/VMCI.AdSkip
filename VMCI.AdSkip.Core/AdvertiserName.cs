namespace VMCI.AdSkip.Core;

public static class AdvertiserName
{
    /// <summary>
    /// Extracts the advertiser from the accessible name of YouTube's "visit advertiser" link:
    /// "baloise.be This link opens in new tab" becomes "baloise.be".
    /// </summary>
    public static string? FromLinkName(string? linkName)
    {
        if (string.IsNullOrWhiteSpace(linkName)) return null;
        return linkName.Trim().Split(' ', 2)[0];
    }
}
