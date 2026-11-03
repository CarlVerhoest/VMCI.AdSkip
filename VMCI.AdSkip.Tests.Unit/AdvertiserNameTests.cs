using VMCI.AdSkip.Core;

namespace VMCI.AdSkip.Tests.Unit;

public class AdvertiserNameTests
{
    [Theory]
    [InlineData("baloise.be This link opens in new tab", "baloise.be")]
    [InlineData("lekkervanbijons.be Deze link wordt geopend in een nieuw tabblad", "lekkervanbijons.be")]
    [InlineData("  madeiraeverythingyouareinto.visi...  ", "madeiraeverythingyouareinto.visi...")]
    [InlineData("example.com", "example.com")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void FromLinkName_returns_first_word(string? linkName, string? expected) =>
        Assert.Equal(expected, AdvertiserName.FromLinkName(linkName));
}
