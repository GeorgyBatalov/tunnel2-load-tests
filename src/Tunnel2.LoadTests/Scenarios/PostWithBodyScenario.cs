using System.Text;
using System.Text.Json;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace Tunnel2.LoadTests.Scenarios;

/// <summary>
/// POST requests with JSON body scenario
/// Phase 1.4 from LOAD_TESTING_ROADMAP.md
///
/// Test parameters:
/// - Endpoint: /post (httpbin)
/// - Method: POST
/// - Body sizes: 1KB, 10KB, 100KB
/// - Warm-up: 10 seconds
/// - Load: 10 RPS for 2 minutes
///
/// Metrics collected:
/// - Latency: P50, P95, P99
/// - Throughput: requests/sec
/// - Error rate: %
/// - Body size impact on performance
/// </summary>
public static class PostWithBodyScenario
{
    /// <summary>
    /// Creates POST scenario with 1KB JSON body
    /// </summary>
    public static NBomber.Contracts.ScenarioProps Create1KB(string tunnelUrl)
    {
        return CreateWithBodySize(tunnelUrl, 1024, "post_1kb");
    }

    /// <summary>
    /// Creates POST scenario with 10KB JSON body
    /// </summary>
    public static NBomber.Contracts.ScenarioProps Create10KB(string tunnelUrl)
    {
        return CreateWithBodySize(tunnelUrl, 10 * 1024, "post_10kb");
    }

    /// <summary>
    /// Creates POST scenario with 100KB JSON body
    /// </summary>
    public static NBomber.Contracts.ScenarioProps Create100KB(string tunnelUrl)
    {
        return CreateWithBodySize(tunnelUrl, 100 * 1024, "post_100kb");
    }

    /// <summary>
    /// Creates POST scenario with custom body size
    /// </summary>
    /// <param name="tunnelUrl">Base URL of the tunnel endpoint</param>
    /// <param name="bodySizeBytes">Size of JSON body in bytes</param>
    /// <param name="scenarioName">Name for the scenario</param>
    private static NBomber.Contracts.ScenarioProps CreateWithBodySize(
        string tunnelUrl,
        int bodySizeBytes,
        string scenarioName)
    {
        var httpClient = new HttpClient();

        var scenario = Scenario.Create(scenarioName, async context =>
        {
            // Generate JSON body of specified size
            var payload = GenerateJsonPayload(bodySizeBytes);
            var jsonContent = JsonSerializer.Serialize(payload);

            var request = Http.CreateRequest("POST", $"{tunnelUrl}/post")
                .WithHeader("Accept", "application/json")
                .WithHeader("Content-Type", "application/json")
                .WithHeader("User-Agent", "NBomber-LoadTest/1.0")
                .WithBody(new StringContent(jsonContent, Encoding.UTF8, "application/json"));

            var response = await Http.Send(httpClient, request);

            return response;
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            // 10 RPS for 2 minutes
            Simulation.Inject(
                rate: 10,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(2)
            )
        );

        return scenario;
    }

    /// <summary>
    /// Generates JSON payload of approximately the specified size
    /// </summary>
    /// <param name="targetSizeBytes">Target size in bytes</param>
    /// <returns>Dictionary that will serialize to approximately targetSizeBytes</returns>
    private static Dictionary<string, object> GenerateJsonPayload(int targetSizeBytes)
    {
        var payload = new Dictionary<string, object>
        {
            { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
            { "requestId", Guid.NewGuid().ToString() },
            { "type", "load_test" }
        };

        // Calculate how much data we need to add to reach target size
        // Account for JSON structure overhead (approximately 100 bytes)
        const int overheadBytes = 100;
        var dataNeeded = targetSizeBytes - overheadBytes;

        if (dataNeeded > 0)
        {
            // Generate data field with repeated characters
            // Each character is approximately 1 byte in UTF-8 for ASCII chars
            var dataField = new string('x', dataNeeded);
            payload["data"] = dataField;
        }

        return payload;
    }

    /// <summary>
    /// Creates POST scenario with all three body sizes running concurrently
    /// Useful for comparing performance across different payload sizes
    /// </summary>
    public static NBomber.Contracts.ScenarioProps[] CreateAllSizes(string tunnelUrl)
    {
        return new[]
        {
            Create1KB(tunnelUrl),
            Create10KB(tunnelUrl),
            Create100KB(tunnelUrl)
        };
    }
}
