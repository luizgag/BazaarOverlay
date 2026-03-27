using BazaarOverlay.Application.ViewModels;
using Shouldly;

namespace BazaarOverlay.Tests.WPF;

public class CardOverlayViewModelTests
{
    private readonly CardOverlayViewModel _vm = new();

    [Fact]
    public void InitialState_IsNotVisible()
    {
        _vm.IsVisible.ShouldBeFalse();
        _vm.CardUrl.ShouldBeNull();
    }

    [Fact]
    public void ShowCard_SetsUrlAndVisibility()
    {
        _vm.ShowCard("https://bazaardb.gg/items/pigomorph", 100, 200);

        _vm.IsVisible.ShouldBeTrue();
        _vm.CardUrl.ShouldBe("https://bazaardb.gg/items/pigomorph");
        _vm.Left.ShouldBe(100);
        _vm.Top.ShouldBe(200);
    }

    [Fact]
    public void Hide_ClearsVisibility()
    {
        _vm.ShowCard("https://bazaardb.gg/items/pigomorph", 100, 200);
        _vm.Hide();

        _vm.IsVisible.ShouldBeFalse();
    }

    [Fact]
    public void ShowCard_WhileAlreadyShowing_UpdatesUrl()
    {
        _vm.ShowCard("https://bazaardb.gg/items/pigomorph", 100, 200);
        _vm.ShowCard("https://bazaardb.gg/skills/fireball", 300, 400);

        _vm.IsVisible.ShouldBeTrue();
        _vm.CardUrl.ShouldBe("https://bazaardb.gg/skills/fireball");
        _vm.Left.ShouldBe(300);
        _vm.Top.ShouldBe(400);
    }

    [Fact]
    public void InitialState_DebugRectIsNotVisible()
    {
        _vm.DebugRectVisible.ShouldBeFalse();
    }

    [Fact]
    public void ShowDebugRect_SetsPositionAndVisibility()
    {
        _vm.ShowDebugRect(50, 100, 400, 450);

        _vm.DebugRectVisible.ShouldBeTrue();
        _vm.DebugRectX.ShouldBe(50);
        _vm.DebugRectY.ShouldBe(100);
        _vm.DebugRectWidth.ShouldBe(400);
        _vm.DebugRectHeight.ShouldBe(450);
    }

    [Fact]
    public void HideDebugRect_ClearsVisibility()
    {
        _vm.ShowDebugRect(50, 100, 400, 450);
        _vm.HideDebugRect();

        _vm.DebugRectVisible.ShouldBeFalse();
    }
}
