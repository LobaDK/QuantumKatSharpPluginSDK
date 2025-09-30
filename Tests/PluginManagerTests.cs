using System.Reflection;
using QuantumKat.PluginSDK.Core;

namespace Tests;

public class PluginManagerTests
{
    [Theory]
    [InlineData("1.2.3", "", true)]
    [InlineData("1.2.3", null, true)]
    [InlineData("1.2.3", "==1.2.3", true)]
    [InlineData("1.2.3", "==1.2.4", false)]
    [InlineData("1.2.3", ">1.2.2", true)]
    [InlineData("1.2.3", ">1.2.3", false)]
    [InlineData("1.2.3", "<1.2.4", true)]
    [InlineData("1.2.3", "<1.2.3", false)]
    [InlineData("1.2.3", ">=1.2.3", true)]
    [InlineData("1.2.3", ">=1.2.2", true)]
    [InlineData("1.2.3", ">=1.2.4", false)]
    [InlineData("1.2.3", "<=1.2.3", true)]
    [InlineData("1.2.3", "<=1.2.4", true)]
    [InlineData("1.2.3", "<=1.2.2", false)]
    [InlineData("1.2.3", "1.2.3", true)]
    [InlineData("1.2.3", "1.2.4", false)]
    public void CheckVersion_VariousCases_ReturnsExpected(string actual, string requirement, bool expected)
    {
        bool result = PluginManager.CheckVersion(actual, requirement);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CheckVersion_WhitespaceRequirement_ReturnsTrue()
    {
        Assert.True(PluginManager.CheckVersion("1.2.3", "   "));
    }

    [Fact]
    public void CheckVersion_UnknownOperator_TreatedAsExact()
    {
        Assert.False(PluginManager.CheckVersion("1.2.3", "!=1.2.3"));
    }
}