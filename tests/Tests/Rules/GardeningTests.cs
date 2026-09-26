namespace MmoGame3d.Tests.Rules;

using MmoGame3d.Rules.Gardening;

public class GardeningTests
{
    [Fact]
    public void ADesignComesBackFromItsText()
    {
        PlantDesign design = new PlantDesign { Pot = "pot_B_large" };
        design.Pieces.Add(new PlantPiece { Id = "monstera_leaf_large_A", X = 0.3f, Z = -0.2f, Yaw = 1.5f, Tilt = 0.4f, Scale = 1.2f });
        design.Pieces.Add(new PlantPiece { Id = "cactus_B" });

        PlantDesign? read = PlantDesign.Parse(design.Format());

        Assert.NotNull(read);
        Assert.Equal("pot_B_large", read!.Pot);
        Assert.Equal(2, read.Pieces.Count);
        Assert.Equal(1.2f, read.Pieces[0].Scale, 3);
    }

    [Theory]
    [InlineData("pot_A_small")]
    [InlineData("no_such_pot;cactus_A,0,0,0,0,1")]
    [InlineData("pot_A_small;no_such_piece,0,0,0,0,1")]
    [InlineData("pot_A_small;cactus_A,0.9,0.9,0,0,1")]
    [InlineData("pot_A_small;cactus_A,0,0,0,2,1")]
    [InlineData("pot_A_small;cactus_A,0,0,0,0,9")]
    [InlineData("pot_A_small;cactus_A,0,0,0,0,1;cactus_A,0,0,0,0,1;cactus_A,0,0,0,0,1;cactus_A,0,0,0,0,1;cactus_A,0,0,0,0,1;cactus_A,0,0,0,0,1")]
    public void ADesignOffTheRulesIsRefused(string text)
    {
        Assert.Null(PlantDesign.Parse(text));
    }
}
