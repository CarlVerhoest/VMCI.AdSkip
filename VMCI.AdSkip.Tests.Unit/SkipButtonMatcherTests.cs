using VMCI.AdSkip.Core;
using VMCI.AdSkip.Tests.Unit.Fakes;

namespace VMCI.AdSkip.Tests.Unit;

public class SkipButtonMatcherTests
{
    private readonly SkipButtonMatcher _matcher = new(TestData.Settings());

    [Theory]
    [InlineData(TestData.PlayerWithAd, true)]
    [InlineData(TestData.PlayerWithoutAd, false)]
    [InlineData("AD-SHOWING", true)]
    [InlineData("  html5-video-player   ad-showing  ", true)]
    [InlineData("html5-video-player ad-showing-later", false)]
    [InlineData("no-ad-showing", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsAdShowing_matches_whole_class_only(string? playerClass, bool expected) =>
        Assert.Equal(expected, _matcher.IsAdShowing(playerClass));

    [Theory]
    [InlineData("ytp-ad-skip-button-modern ytp-button ")]
    [InlineData("ytp-ad-skip-button")]
    [InlineData("ytp-skip-ad-button")]
    [InlineData("YTP-AD-SKIP-BUTTON-MODERN")]
    public void Class_match_is_trusted_even_without_ad(string className) =>
        Assert.True(_matcher.IsSkipButton(new FakeButton(name: "", className: className), adShowing: false));

    [Theory]
    [InlineData("Skip")]
    [InlineData("Skip Ad")]
    [InlineData("skip ads")]
    [InlineData("  Skip")]
    [InlineData("Overslaan")]
    [InlineData("Advertentie overslaan", false)]
    [InlineData("Jump ahead", false)]
    public void Name_fallback_matches_while_ad_is_showing(string name, bool expected = true) =>
        Assert.Equal(expected, _matcher.IsSkipButton(new FakeButton(name: name, className: "unknown-class"), adShowing: true));

    [Fact]
    public void Name_fallback_is_ignored_without_ad() =>
        Assert.False(_matcher.IsSkipButton(new FakeButton(name: "Skip navigation", className: "unknown-class"), adShowing: false));

    [Fact]
    public void Invisible_button_never_matches() =>
        Assert.False(_matcher.IsSkipButton(new FakeButton(isVisible: false), adShowing: true));

    [Fact]
    public void Unrelated_button_does_not_match() =>
        Assert.False(_matcher.IsSkipButton(new FakeButton(name: "My Ad Centre", className: "ytp-ad-button ytp-ad-clickable"), adShowing: true));

    [Fact]
    public void Uses_configured_ad_showing_class()
    {
        var settings = TestData.Settings();
        settings.AdShowingClass = "ad-playing";
        var matcher = new SkipButtonMatcher(settings);

        Assert.True(matcher.IsAdShowing("player ad-playing"));
        Assert.False(matcher.IsAdShowing("player ad-showing"));
    }

    [Theory]
    [InlineData(TestData.PlayerWithPausedAd, true)]
    [InlineData(TestData.PlayerWithAd, false)]
    [InlineData("player paused-mode-x", false)]
    [InlineData(null, false)]
    public void IsPaused_matches_whole_class(string? className, bool expected) =>
        Assert.Equal(expected, _matcher.IsPaused(className));

    [Fact]
    public void Works_with_only_name_patterns()
    {
        var settings = TestData.Settings();
        settings.SkipButtonClassPatterns = [];
        var matcher = new SkipButtonMatcher(settings);

        Assert.True(matcher.IsSkipButton(new FakeButton(name: "Skip"), adShowing: true));
        Assert.False(matcher.IsSkipButton(new FakeButton(name: "Skip"), adShowing: false));
    }
}
