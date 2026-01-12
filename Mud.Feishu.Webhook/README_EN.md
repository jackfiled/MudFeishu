# Mud.Feishu.Webhook

A webhook component for Feishu event subscription and handling, providing complete Feishu event receiving, validation, decryption, and distribution functionality.

**🚀 New Feature: Minimal API** - Complete service registration with one line of code, ready to use!

## Features

- ✅ **Minimal API**: Complete service registration with one line of code, ready to use
- ✅ **Flexible Configuration**: Supports configuration files, code configuration, and builder pattern
- ✅ **Automatic Event Routing**: Automatically distributes events to corresponding handlers based on event type
- ✅ **Security Validation**: Supports event subscription validation, request signature validation, and timestamp validation
- ✅ **Encryption/Decryption**: Built-in AES-256-CBC decryption, automatically handles Feishu encrypted events
- ✅ **Usage Mode**: Supports middleware mode
- ✅ **Dependency Injection**: Fully integrated with .NET dependency injection container
- ✅ **Exception Handling**: Comprehensive exception handling and logging
- ✅ **Performance Monitoring**: Optional performance metrics collection and monitoring
- ✅ **Health Checks**: Built-in health check endpoint
- ✅ **Async Processing**: Fully async event handling mechanism
- ✅ **Concurrency Control**: Configurable concurrent event processing limit with hot reload support
- ✅ **Distributed Support**: Provides distributed deduplication interface, supports Redis and other external storage
- ✅ **Configuration Hot Reload**: Supports runtime configuration changes without service restart
- ✅ **Rate Limiting**: Built-in sliding window rate limiting middleware to prevent malicious requests
- ✅ **Multi-Bot Support**: Supports multiple Feishu bots sharing the same Webhook endpoint
- ✅ **Background Processing**: Supports async background processing to avoid Feishu timeout retries
- ✅ **Security Hardening**: Enhanced IP validation, signature validation, and encryption key security checks
- ✅ **Cross-Platform**: Supports .NET Standard 2.0, .NET 6.0, .NET 8.0, .NET 10.0

## Quick Start

### 1. Install NuGet Package

```bash
dotnet add package Mud.Feishu.Webhook
```

### 2. Minimal Configuration (One Line)

In `Program.cs`:

```csharp
using Mud.Feishu.Webhook;

var builder = WebApplication.CreateBuilder(args);

// One line to register Webhook service (requires at least one event handler)
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .Build();

var app = builder.Build();
app.UseFeishuWebhook(); // Add middleware
app.Run();
```

### 3. Complete Configuration (Add Event Handlers)

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .AddHandler<UserEventHandler>()
    .Build();

var app = builder.Build();
app.UseFeishuWebhook();
app.Run();
```

### 4. Configuration File

```json
{
  "FeishuWebhook": {
    "VerificationToken": "your_verification_token",
    "EncryptKey": "your_encrypt_key",
    "RoutePrefix": "feishu/Webhook",
    "AutoRegisterEndpoint": true,
    "EnableRequestLogging": true,
    "EnableExceptionHandling": true,
    "EventHandlingTimeoutMs": 30000,
    "MaxConcurrentEvents": 10,
    "EnablePerformanceMonitoring": false,
    "AllowedHttpMethods": [ "POST" ],
    "MaxRequestBodySize": 10485760,
    "ValidateSourceIP": false,
    "AllowedSourceIPs": [],
    "EnforceHeaderSignatureValidation": true,
    "EnableBodySignatureValidation": true,
    "TimestampToleranceSeconds": 300,
    "EnableBackgroundProcessing": false,
    "MultiAppEncryptKeys": {
      "cli_a1b2c3d4e5f6g7h8": "your_app1_encrypt_key_32_bytes_long",
      "cli_h8g7f6e5d4c3b2a1": "your_app2_encrypt_key_32_bytes_long"
    },
    "DefaultAppId": "cli_a1b2c3d4e5f6g7h8",
    "RateLimit": {
      "EnableRateLimit": true,
      "WindowSizeSeconds": 60,
      "MaxRequestsPerWindow": 100,
      "EnableIpRateLimit": true,
      "TooManyRequestsStatusCode": 429,
      "TooManyRequestsMessage": "Too many requests, please try again later",
      "WhitelistIPs": [ "127.0.0.1", "::1" ]
    }
  }
}
```

## 🏗️ Service Registration Methods

### 🚀 Register from Configuration File (Recommended)

```csharp
// One line to complete basic configuration (requires at least one event handler)
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageReceiveEventHandler>()
    .Build();
```

### ⚙️ Code Configuration

```csharp
builder.Services.AddFeishuWebhookServiceBuilder(options =>
{
    options.VerificationToken = "your_verification_token";
    options.EncryptKey = "your_encrypt_key";
    options.RoutePrefix = "feishu/Webhook";
    options.EnableRequestLogging = true;
}).AddHandler<MessageEventHandler>()
    .Build();
```

### 🔧 Advanced Builder Pattern

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .ConfigureFrom(configuration)
    .EnableHealthChecks()
    .EnableMetrics()
    .AddHandler<MessageReceiveEventHandler>()
    .Build();
```

## Usage Modes

### Middleware Mode

```csharp
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .Build();

var app = builder.Build();
app.UseFeishuWebhook(); // Automatically handles requests under route prefix
app.Run();
```

> 💡 **Note**: Webhook service currently only supports middleware mode. Customize the route path by configuring `RoutePrefix`.

## Creating Event Handlers

```csharp
using Microsoft.Extensions.Logging;
using Mud.Feishu.Abstractions;

public class MessageEventHandler : IFeishuEventHandler
{
    private readonly ILogger<MessageEventHandler> _logger;

    public MessageEventHandler(ILogger<MessageEventHandler> logger)
    {
        _logger = logger;
    }

    public string SupportedEventType => "im.message.receive_v1";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received message event: {EventId}", eventData.EventId);
        
        // Handle message logic
        var messageData = JsonSerializer.Deserialize<object>(
            eventData.Event?.ToString() ?? string.Empty);
        
        // Your business logic...
        
        await Task.CompletedTask;
    }
}
```

## Configuration Options

### Basic Configuration

| Option | Type | Default | Description |
| --- | --- | --- | --- |
| `VerificationToken` | string | - | Feishu event subscription verification token |
| `EncryptKey` | string | - | Feishu event encryption key (32 bytes) |
| `RoutePrefix` | string | "feishu/Webhook" | Webhook route prefix |
| `AutoRegisterEndpoint` | bool | true | Whether to auto-register endpoint |

### Multi-Bot Configuration

| Option | Type | Default | Description |
| --- | --- | --- | --- |
| `MultiAppEncryptKeys` | Dictionary\<string, string\> | - | Multi-bot encryption keys (AppId -> EncryptKey) |
| `DefaultAppId` | string | - | Default app ID (fallback for multi-bot scenarios) |

### Security Configuration

| Option | Type | Default | Description |
| --- | --- | --- | --- |
| `ValidateSourceIP` | bool | false | Whether to validate source IP |
| `AllowedSourceIPs` | HashSet\<string\> | - | Allowed source IP addresses |
| `AllowedHttpMethods` | HashSet\<string\> | ["POST"] | Allowed HTTP methods |
| `MaxRequestBodySize` | long | 10MB | Max request body size |
| `EnforceHeaderSignatureValidation` | bool | true | Whether to enforce header signature validation |
| `EnableBodySignatureValidation` | bool | true | Whether to validate request body signature at service layer |
| `TimestampToleranceSeconds` | int | 300 | Timestamp validation tolerance (seconds) |

### Performance Configuration

| Option | Type | Default | Description |
| --- | --- | --- | --- |
| `MaxConcurrentEvents` | int | 10 | Max concurrent events, supports hot reload |
| `EventHandlingTimeoutMs` | int | 30000 | Event handling timeout (milliseconds) |
| `EnablePerformanceMonitoring` | bool | false | Whether to enable performance monitoring |
| `EnableBackgroundProcessing` | bool | false | Whether to enable async background processing |

### Logging Configuration

| Option | Type | Default | Description |
| --- | --- | --- | --- |
| `EnableRequestLogging` | bool | true | Whether to enable request logging |
| `EnableExceptionHandling` | bool | true | Whether to enable exception handling |

### Rate Limiting Configuration

| Option | Type | Default | Description |
| --- | --- | --- | --- |
| `RateLimit.EnableRateLimit` | bool | false | Whether to enable rate limiting |
| `RateLimit.WindowSizeSeconds` | int | 60 | Time window size (seconds) |
| `RateLimit.MaxRequestsPerWindow` | int | 100 | Max requests per time window |
| `RateLimit.EnableIpRateLimit` | bool | true | Whether to enable IP-based rate limiting |
| `RateLimit.TooManyRequestsStatusCode` | int | 429 | Status code when rate limit exceeded |
| `RateLimit.TooManyRequestsMessage` | string | "Too many requests, please try again later" | Message when rate limit exceeded |
| `RateLimit.WhitelistIPs` | HashSet\<string\> | [] | Whitelist IPs (exempt from rate limiting) |

## Advanced Features

### Multi-Bot Support

Support multiple Feishu bots sharing the same Webhook endpoint:

```json
{
  "FeishuWebhook": {
    "MultiAppEncryptKeys": {
      "cli_a1b2c3d4e5f6g7h8": "your_app1_encrypt_key_32_bytes_long",
      "cli_h8g7f6e5d4c3b2a1": "your_app2_encrypt_key_32_bytes_long"
    },
    "DefaultAppId": "cli_a1b2c3d4e5f6g7h8"
  }
}
```

### Rate Limiting

Built-in sliding window rate limiting middleware to prevent malicious requests:

```json
{
  "FeishuWebhook": {
    "RateLimit": {
      "EnableRateLimit": true,
      "WindowSizeSeconds": 60,
      "MaxRequestsPerWindow": 100,
      "EnableIpRateLimit": true,
      "WhitelistIPs": [ "127.0.0.1", "::1" ]
    }
  }
}
```

### Background Processing Mode

Enable background processing mode to avoid Feishu timeout retries:

```json
{
  "FeishuWebhook": {
    "EnableBackgroundProcessing": true
  }
}
```

```csharp
// After enabling background processing mode, middleware returns success response immediately
// Then processes events asynchronously in the background, suitable for long-running business logic
builder.Services.CreateFeishuWebhookServiceBuilder(options =>
{
    options.EnableBackgroundProcessing = true;
}).AddHandler<LongRunningEventHandler>()
    .Build();
```

### Concurrency Control Hot Reload

Support runtime dynamic adjustment of concurrency limits without service restart:

```csharp
// Configuration changes are automatically applied with hot reload support
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .Build();

// Modify MaxConcurrentEvents value in configuration file at runtime
// System will automatically detect and apply new concurrency limits
```

## Registering Handlers

```csharp
// Add handlers using chain calls
builder.Services.AddFeishuWebhookServiceBuilder(builder.Configuration)
    .AddHandler<MessageEventHandler>()
    .AddHandler<UserEventHandler>()
    .AddHandler<DepartmentEventHandler>()
    .Build();

// Use builder pattern for complex configuration
builder.Services.CreateFeishuWebhookServiceBuilder(configuration)
    .ConfigureFrom(configuration)
    .AddHandler<MessageEventHandler>()
    .AddHandler<UserEventHandler>()
    .Build();
```

## Supported Event Types

The library supports all Feishu event types, including but not limited to:

- `im.message.receive_v1` - Receive message
- `im.chat.member_user_added_v1` - User joins group chat
- `im.chat.member_user_deleted_v1` - User leaves group chat
- `contact.user.created_v3` - User created
- `contact.user.updated_v3` - User updated
- `contact.user.deleted_v3` - User deleted

## Feishu Platform Configuration

### 1. Create Event Subscription

1. Log in to Feishu Open Platform
2. Go to your application details page
3. Click "Event Subscription"
4. Configure request URL: `https://your-domain.com/feishu/Webhook`
5. Set verification token and encryption key

### 2. Configure Event Types

Select the event types you want to subscribe to:

- Message events
- Group chat events
- User events
- Department events
- etc...

### 3. Publish Application

After configuration is complete, publish the application, and Feishu servers will start pushing events to your endpoint.

## Monitoring and Diagnostics

### Performance Monitoring

```csharp
// Method 1: Enable via builder pattern
builder.Services.AddFeishuWebhookBuilder()
    .ConfigureFrom(configuration)
    .EnableMetrics()
    .Build();

// Method 2: Enable via configuration options
builder.Services.CreateFeishuWebhookServiceBuilder(options =>
{
    options.EnablePerformanceMonitoring = true; // Enable performance monitoring
}).AddHandler<MessageEventHandler>()
    .Build();
```

### Health Checks

```csharp
// Enable health checks using builder pattern
builder.Services.CreateFeishuWebhookBuilder()
    .ConfigureFrom(configuration)
    .EnableHealthChecks()
    .Build();

// Add health check endpoint
builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapHealthChecks("/health"); // Health check endpoint
```

### Logging

The library uses the standard .NET logging framework, and you can configure different log levels:

```json
{
  "Logging": {
    "LogLevel": {
      "Mud.Feishu.Webhook": "Information",
      "Mud.Feishu.Webhook.Services": "Debug"
    }
  }
}
```

## Best Practices

### 1. Error Handling

```csharp
public class RobustEventHandler : IFeishuEventHandler
{
    private readonly ILogger<RobustEventHandler> _logger;

    public string SupportedEventType => "im.message.receive_v1";

    public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            // Business logic
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while handling event: {EventId}", eventData.EventId);
            // Do not rethrow exceptions to avoid affecting other handlers
        }
    }
}
```

### 2. Async Processing

```csharp
public async Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
{
    // Use async APIs
    await ProcessMessageAsync(eventData, cancellationToken);
    
    // Avoid blocking calls
    // Do not use .Result or .Wait()
}
```

## Troubleshooting

### Common Issues

1. **Validation Failed**
   - Check if `VerificationToken` is correct
   - Confirm request URL is configured correctly

2. **Decryption Failed**
   - Check if `EncryptKey` is correct
   - Confirm encryption is enabled on Feishu platform

3. **Signature Validation Failed**
   - Check time synchronization
   - Confirm request hasn't been modified by proxy server
   - Ensure `EnforceHeaderSignatureValidation` is set to true in production

4. **Event Handling Failed**
   - Check if event handlers are correctly registered
   - View detailed error information in logs

5. **Distributed Deployment Event Duplication**
   - Default uses in-memory deduplication, multi-instance deployment requires distributed deduplication
   - Refer to `IFeishuNonceDistributedDeduplicator` interface for custom Redis implementation

6. **Timeout Handling**
   - Check if `EventHandlingTimeoutMs` configuration is reasonable
   - Ensure event handling logic supports cancellation tokens

7. **Rate Limiting Issues**
   - Check `RateLimit.EnableRateLimit` configuration
   - Confirm client IP is in whitelist
   - Adjust `MaxRequestsPerWindow` and `WindowSizeSeconds` parameters

8. **Multi-Bot Configuration Issues**
   - Check if `MultiAppEncryptKeys` configuration is correct
   - Confirm AppId to encryption key mapping
   - Verify `DefaultAppId` configuration

### Debugging Tips

```csharp
// Enable verbose logging
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Enable request logging and performance monitoring
builder.Services.CreateFeishuWebhookServiceBuilder(options =>
{
    options.EnableRequestLogging = true;
    options.EnablePerformanceMonitoring = true;
    options.RateLimit.EnableRateLimit = true; // Enable rate limiting debugging
}).AddHandler<MessageEventHandler>()
    .Build();
```

## Quick Reference

### Most Common Registration Methods

```csharp
// Method 1: Minimal (requires at least one event handler)
builder.Services.CreateFeishuWebhookServiceBuilder(configuration)
    .AddHandler<MessageReceiveEventHandler>()
    .Build();

// Method 2: Minimal + Handlers
builder.Services.CreateFeishuWebhookServiceBuilder(configuration)
    .AddHandler<MessageReceiveEventHandler>()
    .Build();

// Method 3: Code Configuration
builder.Services.CreateFeishuWebhookServiceBuilder(options => {
    options.VerificationToken = "your_token";
    options.EncryptKey = "your_key";
}).AddHandler<MessageEventHandler>()
    .Build();

// Method 4: Builder Pattern (complex configuration)
builder.Services.CreateFeishuWebhookServiceBuilder(builder.Configuration)
    .ConfigureFrom(configuration)
    .EnableMetrics()
    .AddHandler<Handler>()
    .Build();
```

---

**🚀 Get started with Feishu Webhook now and build a stable, reliable event handling system!**
