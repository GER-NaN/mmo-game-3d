namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Movement;

public class WalkingTests
{
    private const float Precision = 0.0001f;

    [Fact]
    public void ForwardAtHeadingZeroIsMinusZ()
    {
        Walking.Direction(0f, 1f, 0f, out float x, out float z);

        Assert.Equal(0f, x, Precision);
        Assert.Equal(-1f, z, Precision);
    }

    [Fact]
    public void AQuarterTurnLeftWalksTowardsMinusX()
    {
        Walking.Direction(MathF.PI / 2f, 1f, 0f, out float x, out float z);

        Assert.Equal(-1f, x, Precision);
        Assert.Equal(0f, z, Precision);
    }

    [Fact]
    public void DiagonalIsNoFasterThanStraight()
    {
        Walking.Direction(0.3f, 1f, 1f, out float x, out float z);

        Assert.Equal(1f, MathF.Sqrt((x * x) + (z * z)), Precision);
    }

    [Fact]
    public void TurningForOneSecondTurnsTheTurnRate()
    {
        Assert.Equal(Walking.TurnRate, Walking.Turn(0f, 1f, 1f), Precision);
    }

    [Fact]
    public void NotANumberIsRefused()
    {
        Assert.False(Walking.IsValid(float.NaN, 0f, 0f));
        Assert.True(Walking.IsValid(0.5f, -0.5f, 1f));
    }
}
