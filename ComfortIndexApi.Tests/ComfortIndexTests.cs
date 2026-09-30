using Xunit;

public class ComfortIndexTests
{
    [Fact]
    public void IdealConditions_ReturnsScoreOf100()
    {
        double score = WeatherService.CalculateComfortIndex(25, 50, 2.78);
        Assert.Equal(100, score, precision: 0);
    }

    [Fact]
public void ExtremeCold_ReturnsLowScore()
{
    double score = WeatherService.CalculateComfortIndex(-10, 50, 2.78);
    Assert.True(score < 70);
}

[Fact]
public void ExtremeHeat_ReturnsLowScore()
{
    double score = WeatherService.CalculateComfortIndex(45, 50, 2.78);
    Assert.True(score < 75, $"Expected score under 75, but got {score}");
}

    [Fact]
    public void Score_NeverGoesBelowZero()
    {
        double score = WeatherService.CalculateComfortIndex(-50, 100, 50);
        Assert.True(score >= 0);
    }

    [Fact]
    public void Score_NeverExceeds100()
    {
        double score = WeatherService.CalculateComfortIndex(24, 50, 2.78);
        Assert.True(score <= 100);
    }
}