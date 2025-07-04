using System.Reflection;
using QuantumKat.PluginSDK.Core;

namespace Tests;

public class PluginManagerTests
{
    private static MethodInfo GetCheckVersionMethod()
        {
            var type = typeof(PluginManager);
            return type.GetMethod("CheckVersion", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new Exception("CheckVersion method not found");
        }

        private static bool InvokeCheckVersion(string actual, string requirement)
        {
            var method = GetCheckVersionMethod();
            var result = method.Invoke(null, [actual, requirement]);
            return result is bool b && b;
        }

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
            bool result = InvokeCheckVersion(actual, requirement);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void CheckVersion_WhitespaceRequirement_ReturnsTrue()
        {
            Assert.True(InvokeCheckVersion("1.2.3", "   "));
        }

        [Fact]
        public void CheckVersion_UnknownOperator_TreatedAsExact()
        {
            Assert.False(InvokeCheckVersion("1.2.3", "!=1.2.3"));
        }
}