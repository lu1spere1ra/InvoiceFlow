using System.Reflection;

namespace InvoiceFlow.Architecture.Tests;

/// <summary>
/// Enforces the Clean Architecture dependency rule using only reflection:
/// Api → Infrastructure → Application → Domain, never the other way round.
/// </summary>
public sealed class LayerDependencyTests
{
    private const string Domain = "InvoiceFlow.Domain";
    private const string Application = "InvoiceFlow.Application";
    private const string Infrastructure = "InvoiceFlow.Infrastructure";
    private const string Api = "InvoiceFlow.Api";

    [Theory]
    [InlineData(Domain, Application)]
    [InlineData(Domain, Infrastructure)]
    [InlineData(Domain, Api)]
    [InlineData(Application, Infrastructure)]
    [InlineData(Application, Api)]
    [InlineData(Infrastructure, Api)]
    public void Layer_does_not_reference_outer_layer(string layer, string forbidden) =>
        ReferencedAssemblies(layer).ShouldNotContain(forbidden);

    [Fact]
    public void Domain_has_no_package_dependencies() =>
        ReferencedAssemblies(Domain)
            .Where(name => !IsFrameworkAssembly(name))
            .ShouldBeEmpty();

    [Theory]
    [InlineData(Domain)]
    [InlineData(Application)]
    public void Inner_layers_do_not_depend_on_persistence_or_web(string layer)
    {
        var references = ReferencedAssemblies(layer);

        references.ShouldNotContain(name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        references.ShouldNotContain(name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    private static string[] ReferencedAssemblies(string assemblyName) =>
        Assembly.Load(assemblyName)
            .GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .ToArray();

    private static bool IsFrameworkAssembly(string name) =>
        name is "netstandard" or "mscorlib" ||
        name.StartsWith("System", StringComparison.Ordinal) ||
        name.StartsWith("Microsoft.Win32", StringComparison.Ordinal);
}
