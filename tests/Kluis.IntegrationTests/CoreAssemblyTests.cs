using Kluis.Core;

namespace Kluis.IntegrationTests;

public class CoreAssemblyTests
{
    [Fact]
    public void Core_assembly_is_referenced_under_its_expected_name()
    {
        Assert.Equal(CoreAssembly.Name, typeof(CoreAssembly).Assembly.GetName().Name);
    }
}
