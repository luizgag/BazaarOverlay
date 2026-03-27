using BazaarOverlay.Infrastructure.Ocr;
using Shouldly;

namespace BazaarOverlay.Tests.Infrastructure;

public class TooltipNameExtractorTests
{
    private readonly TooltipNameExtractor _extractor = new();

    [Fact]
    public void ExtractName_SingleLine_ReturnsLine()
    {
        var lines = new[] { "Pigomorph" };

        var result = _extractor.ExtractName(lines);

        result.ShouldBe("Pigomorph");
    }

    [Fact]
    public void ExtractName_MultipleLines_ReturnsFirstNonEmpty()
    {
        var lines = new[] { "Pigomorph", "Tier: Gold", "+50% damage to Monsters" };

        var result = _extractor.ExtractName(lines);

        result.ShouldBe("Pigomorph");
    }

    [Fact]
    public void ExtractName_FirstLineEmpty_SkipsToNext()
    {
        var lines = new[] { "", "  ", "Pigomorph", "Some description" };

        var result = _extractor.ExtractName(lines);

        result.ShouldBe("Pigomorph");
    }

    [Fact]
    public void ExtractName_EmptyArray_ReturnsNull()
    {
        var lines = Array.Empty<string>();

        var result = _extractor.ExtractName(lines);

        result.ShouldBeNull();
    }

    [Fact]
    public void ExtractName_AllEmptyLines_ReturnsNull()
    {
        var lines = new[] { "", "  ", "   " };

        var result = _extractor.ExtractName(lines);

        result.ShouldBeNull();
    }

    [Fact]
    public void ExtractName_TrimsWhitespace()
    {
        var lines = new[] { "  Pigomorph  " };

        var result = _extractor.ExtractName(lines);

        result.ShouldBe("Pigomorph");
    }

    [Fact]
    public void ExtractName_SkipsLineStartingWithSells()
    {
        var lines = new[] { "Sells Medium Items", "Mittel" };

        var result = _extractor.ExtractName(lines);

        result.ShouldBe("Mittel");
    }

    [Fact]
    public void ExtractName_SkipsSingleWordSells()
    {
        var lines = new[] { "Sells", "Mittel" };

        var result = _extractor.ExtractName(lines);

        result.ShouldBe("Mittel");
    }

    [Fact]
    public void ExtractName_SkipsLineContainingColon()
    {
        var lines = new[] { "Tier: Gold", "Pigomorph" };

        var result = _extractor.ExtractName(lines);

        result.ShouldBe("Pigomorph");
    }

    [Fact]
    public void ExtractName_SkipsStatModifierLines()
    {
        var lines = new[] { "+50% damage", "Pigomorph" };

        var result = _extractor.ExtractName(lines);

        result.ShouldBe("Pigomorph");
    }

    [Fact]
    public void ExtractName_SkipsNumericLines()
    {
        var lines = new[] { "2650", "Pigomorph" };

        var result = _extractor.ExtractName(lines);

        result.ShouldBe("Pigomorph");
    }

    [Fact]
    public void ExtractName_MultiWordName()
    {
        var lines = new[] { "Ray Vahn", "Sells Large Items" };

        var result = _extractor.ExtractName(lines);

        result.ShouldBe("Ray Vahn");
    }

    [Fact]
    public void ExtractName_AllDescriptionLines_FallsBackToFirst()
    {
        var lines = new[] { "Sells Medium Items", "Tier: Gold" };

        var result = _extractor.ExtractName(lines);

        result.ShouldBe("Sells Medium Items");
    }

}
