using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Freizeichen.App.Views;
using Freizeichen.AppLogic.Tests;
using Freizeichen.AppLogic.ViewModels;
using Freizeichen.Domain;

namespace Freizeichen.App.Tests;

// Freizeichen opens from inside Lagebuch and wears its "dispatch console" theme. A renamed
// resource or a font file that did not get embedded falls back to Fluent's defaults without an
// error, so these pin what the eye would otherwise have to catch.
public sealed class ThemeTests
{
    [AvaloniaFact]
    public void Runs_dark_like_lagebuch()
    {
        Assert.Equal(ThemeVariant.Dark, Application.Current?.RequestedThemeVariant);
    }

    [AvaloniaTheory]
    [InlineData("AppBackgroundBrush")]
    [InlineData("SignalGradientBrush")]
    [InlineData("ErrorBrush")]
    [InlineData("ActiveBrush")]
    [InlineData("FocusBrush")]
    public void Resolves_the_lagebuch_tokens(string key)
    {
        Assert.True(Application.Current!.TryGetResource(key, ThemeVariant.Dark, out var value), $"{key} is missing.");
        Assert.IsAssignableFrom<IBrush>(value);
    }

    [AvaloniaTheory]
    [InlineData("DisplayFont", "Oswald")]
    [InlineData("BodyFont", "Barlow")]
    [InlineData("MonoFont", "JetBrains Mono")]
    public void Embeds_the_lagebuch_fonts(string key, string family)
    {
        Assert.True(Application.Current!.TryGetResource(key, ThemeVariant.Dark, out var value), $"{key} is missing.");
        var font = Assert.IsType<FontFamily>(value);
        Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(font), out var glyphs), $"{family} is not embedded.");
        Assert.Equal(family, glyphs.FamilyName);
    }

    [AvaloniaFact]
    public void The_primary_actions_are_signal_red()
    {
        var dial = new DialWindow { DataContext = new DialViewModel("tel:0891234567", DialSettings.Default with { PortName = "COM5" }, new FakeDialer()) };
        var settings = new SettingsWindow { DataContext = new SettingsViewModel(new FakeSettingsStore(DialSettings.Default), new FakeDialer(), () => [], new FakeRegistration()) };

        Assert.Contains("signal", dial.FindControl<Button>("DialButton")!.Classes);
        Assert.Contains("signal", settings.FindControl<Button>("SaveButton")!.Classes);
    }

    [AvaloniaFact]
    public void The_number_is_set_in_the_mono_font()
    {
        var dial = new DialWindow { DataContext = new DialViewModel("tel:0891234567", DialSettings.Default with { PortName = "COM5" }, new FakeDialer()) };

        Assert.Contains("metric", dial.FindControl<Control>("NumberText")!.Classes);
    }
}
