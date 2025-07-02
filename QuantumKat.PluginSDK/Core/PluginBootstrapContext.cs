using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuantumKat.PluginSDK.Core.Interfaces;

namespace QuantumKat.PluginSDK.Core;

public class PluginBootstrapContext
{
    public required IConfiguration Configuration { get; init; }
    public required ILogger Logger { get; init; }
    public required IServiceProvider CoreServices { get; init; }
    public required string PluginDirectory { get; init; }
    public IPluginEventRegistry? EventRegistry { get; init; }
}