using Microsoft.Extensions.Time.Testing;
using VMCI.AdSkip.Core;
using VMCI.AdSkip.Tests.Unit.Fakes;

namespace VMCI.AdSkip.Tests.Unit;

public class AdSkipperTests
{
    private readonly FakeTimeProvider _time = new();
    private readonly ListLog _log = new();
    private readonly AdSkipSettings _settings = TestData.Settings();

    private AdSkipper CreateSkipper(FakeWindowSource source) => new(source, _settings, _log, _time);

    [Fact]
    public void Presses_skip_button_via_invoke_and_logs_advertiser()
    {
        var button = new FakeButton();
        var player = new FakePlayer(TestData.PlayerWithAd, new FakeButton("My Ad Centre", "ytp-ad-button"), button)
        {
            AdvertiserLinkName = "baloise.be This link opens in new tab",
        };
        var skipper = CreateSkipper(new FakeWindowSource(new FakeWindow(1, players: player)));

        Assert.True(skipper.Tick());

        Assert.Equal(1, button.InvokeCount);
        Assert.Equal(0, button.ClickCount);
        Assert.Equal(1, skipper.SkippedCount);
        Assert.Equal("Skipped ad #1 (baloise.be) via invoke", Assert.Single(_log.Infos));
    }

    [Fact]
    public void Falls_back_to_mouse_click_when_invoke_is_unsupported()
    {
        var button = new FakeButton(supportsInvoke: false);
        var skipper = CreateSkipper(new FakeWindowSource(new FakeWindow(1, players: new FakePlayer(TestData.PlayerWithAd, button))));

        skipper.Tick();

        Assert.Equal(1, button.ClickCount);
        Assert.Equal("Skipped ad #1 via mouse", Assert.Single(_log.Infos));
    }

    [Fact]
    public void Advertiser_lookup_failure_does_not_block_the_skip()
    {
        var player = new FakePlayer(TestData.PlayerWithAd, new FakeButton()) { AdvertiserError = new InvalidOperationException("gone") };
        var skipper = CreateSkipper(new FakeWindowSource(new FakeWindow(1, players: player)));

        Assert.True(skipper.Tick());
        Assert.Equal("Skipped ad #1 via invoke", Assert.Single(_log.Infos));
    }

    [Fact]
    public void Uses_configured_process_names_and_player_id()
    {
        _settings.ProcessNames = ["brave"];
        _settings.PlayerAutomationId = "my_player";
        var window = new FakeWindow(1);
        var source = new FakeWindowSource(window);

        CreateSkipper(source).Tick();

        Assert.Equal(["brave"], source.RequestedProcessNames);
        Assert.Equal("my_player", window.RequestedPlayerId);
    }

    [Fact]
    public void Does_nothing_without_ad_or_button()
    {
        var player = new FakePlayer(TestData.PlayerWithoutAd, new FakeButton("Play (k)", "ytp-play-button ytp-button"));
        var skipper = CreateSkipper(new FakeWindowSource(new FakeWindow(1, players: player)));

        Assert.False(skipper.Tick());
        Assert.Equal(0, skipper.SkippedCount);
        Assert.Empty(_log.Infos);
        Assert.Empty(_log.Warnings);
    }

    [Fact]
    public void Ad_without_button_logs_once_then_warns_after_deadline()
    {
        var player = new FakePlayer(TestData.PlayerWithAd);
        var skipper = CreateSkipper(new FakeWindowSource(new FakeWindow(1, "Video - Google Chrome", player)));

        skipper.Tick();
        _time.Advance(TimeSpan.FromSeconds(5));
        skipper.Tick();

        Assert.Equal("Ad playing in 'Video - Google Chrome', waiting for skip button...", Assert.Single(_log.Infos));
        Assert.Empty(_log.Warnings);

        _time.Advance(TimeSpan.FromSeconds(10));
        skipper.Tick();
        skipper.Tick();

        Assert.Contains("no skip button found", Assert.Single(_log.Warnings));
    }

    [Fact]
    public void Skip_button_appearing_later_is_pressed()
    {
        var player = new FakePlayer(TestData.PlayerWithAd);
        var skipper = CreateSkipper(new FakeWindowSource(new FakeWindow(1, players: player)));

        Assert.False(skipper.Tick());
        var button = new FakeButton();
        player.Buttons.Add(button);
        Assert.True(skipper.Tick());

        Assert.Equal(1, button.InvokeCount);
        Assert.Equal(2, _log.Infos.Count);
    }

    [Fact]
    public void New_ad_after_previous_one_ended_is_reported_again()
    {
        var player = new FakePlayer(TestData.PlayerWithAd);
        var skipper = CreateSkipper(new FakeWindowSource(new FakeWindow(1, players: player)));

        skipper.Tick();
        player.ClassName = TestData.PlayerWithoutAd;
        skipper.Tick();
        player.ClassName = TestData.PlayerWithAd;
        skipper.Tick();

        Assert.Equal(2, _log.Infos.Count(i => i.StartsWith("Ad playing")));
    }

    [Fact]
    public void Name_fallback_only_used_while_ad_is_showing()
    {
        var button = new FakeButton("Skip", "some-renamed-class");
        var player = new FakePlayer(TestData.PlayerWithoutAd, button);
        var skipper = CreateSkipper(new FakeWindowSource(new FakeWindow(1, players: player)));

        Assert.False(skipper.Tick());
        player.ClassName = TestData.PlayerWithAd;
        Assert.True(skipper.Tick());
        Assert.Equal(1, button.InvokeCount);
    }

    [Fact]
    public void Skips_ads_in_several_windows_and_counts_them()
    {
        var first = new FakeButton();
        var second = new FakeButton();
        var skipper = CreateSkipper(new FakeWindowSource(
            new FakeWindow(1, players: new FakePlayer(TestData.PlayerWithAd, first)),
            new FakeWindow(2, players: new FakePlayer(TestData.PlayerWithAd, second))));

        skipper.Tick();

        Assert.Equal(1, first.InvokeCount);
        Assert.Equal(1, second.InvokeCount);
        Assert.Equal(2, skipper.SkippedCount);
        Assert.Equal("Skipped ad #2 via invoke", _log.Infos[^1]);
    }

    [Fact]
    public void Error_in_one_window_does_not_stop_the_others()
    {
        var button = new FakeButton();
        var skipper = CreateSkipper(new FakeWindowSource(
            new FakeWindow(1) { Error = new InvalidOperationException("Element not available") },
            new FakeWindow(2, players: new FakePlayer(TestData.PlayerWithAd, button))));

        Assert.True(skipper.Tick());
        Assert.Equal(1, button.InvokeCount);
        Assert.Equal("UI Automation error: Element not available", Assert.Single(_log.Warnings));
    }

    [Fact]
    public void Repeated_error_is_reported_once_until_it_goes_away()
    {
        var window = new FakeWindow(1) { Error = new InvalidOperationException("boom") };
        var skipper = CreateSkipper(new FakeWindowSource(window));

        skipper.Tick();
        skipper.Tick();
        Assert.Single(_log.Warnings);

        window.Error = null;
        skipper.Tick();
        window.Error = new InvalidOperationException("boom");
        skipper.Tick();
        Assert.Equal(2, _log.Warnings.Count);
    }

    [Fact]
    public void Same_error_in_two_windows_is_reported_once()
    {
        var skipper = CreateSkipper(new FakeWindowSource(
            new FakeWindow(1) { Error = new InvalidOperationException("boom") },
            new FakeWindow(2) { Error = new InvalidOperationException("boom") }));

        skipper.Tick();

        Assert.Single(_log.Warnings);
    }

    [Fact]
    public void Error_listing_windows_is_reported_and_not_thrown()
    {
        var source = new FakeWindowSource { Error = new InvalidOperationException("UIA unavailable") };
        var skipper = CreateSkipper(source);

        Assert.False(skipper.Tick());
        Assert.Equal("UI Automation error: UIA unavailable", Assert.Single(_log.Warnings));
    }

    [Fact]
    public void HasWebContent_is_true_when_any_window_has_content()
    {
        var skipper = CreateSkipper(new FakeWindowSource(
            new FakeWindow(1) { Error = new InvalidOperationException("boom") },
            new FakeWindow(2) { WebContent = false },
            new FakeWindow(3) { WebContent = true }));

        Assert.True(skipper.HasWebContent());
    }

    [Fact]
    public void HasWebContent_is_false_without_content()
    {
        var skipper = CreateSkipper(new FakeWindowSource(new FakeWindow(1) { WebContent = false }));

        Assert.False(skipper.HasWebContent());
        Assert.False(CreateSkipper(new FakeWindowSource()).HasWebContent());
    }
}
