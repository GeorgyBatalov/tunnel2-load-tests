using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace Tunnel2.LoadTests.Scenarios;

/// <summary>
/// Basic HTTP GET scenario for baseline performance testing
/// Phase 1.2 from LOAD_TESTING_ROADMAP.md
///
/// Test parameters:
/// - Endpoint: /get (httpbin)
/// - Warm-up: 10 seconds
/// - Load: 10 RPS for 2 minutes
///
/// Metrics collected:
/// - Latency: P50, P95, P99
/// - Throughput: requests/sec
/// - Error rate: %
/// </summary>
public static class BasicHttpScenario
{
    /// <summary>
    /// Creates basic HTTP GET scenario
    /// </summary>
    /// <param name="tunnelUrl">Base URL of the tunnel endpoint (e.g., http://session-id-e1.tunnel.local:12000)</param>
    /// <returns>NBomber scenario</returns>
    public static NBomber.Contracts.ScenarioProps Create(string tunnelUrl)
    {
        var httpClient = new HttpClient();

        var scenario = Scenario.Create("basic_http_get", async context =>
        {
            var request = Http.CreateRequest("GET", $"{tunnelUrl}/get")
                .WithHeader("Accept", "application/json")
                .WithHeader("User-Agent", "NBomber-LoadTest/1.0");

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
    /// Creates basic HTTP GET scenario with custom parameters
    /// </summary>
    /// <param name="tunnelUrl">Base URL of the tunnel endpoint</param>
    /// <param name="warmupSeconds">Warm-up duration in seconds</param>
    /// <param name="rps">Requests per second</param>
    /// <param name="durationMinutes">Test duration in minutes</param>
    /// <returns>NBomber scenario</returns>
    public static NBomber.Contracts.ScenarioProps Create(
        string tunnelUrl,
        int warmupSeconds,
        int rps,
        int durationMinutes)
    {
        var httpClient = new HttpClient();

        var scenario = Scenario.Create("basic_http_get", async context =>
        {
            var request = Http.CreateRequest("GET", $"{tunnelUrl}/get")
                .WithHeader("Accept", "application/json")
                .WithHeader("User-Agent", "NBomber-LoadTest/1.0");

            var response = await Http.Send(httpClient, request);

            return response;
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(warmupSeconds))
        .WithLoadSimulations(
            Simulation.Inject(
                rate: rps,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(durationMinutes)
            )
        );

        return scenario;
    }
}
