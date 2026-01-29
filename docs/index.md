---
_layout: landing
---

# QuantumKat Plugin SDK

A comprehensive .NET plugin framework for building extensible applications with Discord.NET integration.

## Welcome

The QuantumKat Plugin SDK provides a robust foundation for building modular, plugin-based applications in C#. It enables you to:

- **Build Extensible Applications**: Load and manage plugins dynamically at runtime
- **Isolate Plugin Contexts**: Each plugin runs in its own isolated assembly load context to prevent version conflicts
- **Share Services**: Seamlessly share services between your main application and plugins through dependency injection
- **Handle Events**: Use a flexible event registry for inter-component communication
- **Manage Lifecycle**: Handle complex plugin initialization, startup, and shutdown scenarios

## Getting Started

Start with these key documentation pages:

1. [Plugin Interfaces](markdown/plugin-interfaces.md) - Learn the core IPlugin and IThreadedPlugin interfaces
2. [Plugin Lifecycle](markdown/plugin-lifecycle.md) - Understand how plugins are loaded and managed
3. [Dependency Injection](markdown/dependency-injection.md) - Learn how to use the shared service provider
5. [Discord Integration](markdown/discord-integration.md) - Build Discord bot plugins (if using Discord.NET)

## Key Features

### Isolated Plugin Loading
Plugins are loaded in their own `AssemblyLoadContext`, preventing version conflicts and enabling memory management.

### Built-in Dependency Injection
Plugins register their services with the main application's service provider, enabling seamless integration.

### Event-Driven Architecture
Subscribe to application events using the `IPluginEventRegistry` for loose coupling between components.

### Async Support
Full async/await support with `IThreadedPlugin` for background services and long-running operations.

## API Reference

For detailed API documentation, see the [API Reference](markdown/api-reference.md) guide.