using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace Tunnel2.LoadTests.Scenarios;

/// <summary>
/// Ramp-up stress test scenario to find performance limits
/// Phase 2.1 from LOAD_TESTING_ROADMAP.md
///
/// Test parameters:
/// - Endpoint: /get (httpbin)
/// - Ramp-up stages: 10 → 50 → 100 → 200 → 500 RPS
/// - Each stage duration: configurable (default 3 minutes for fast test, 5 minutes for full)
///
/// Goal: Find the point where latency starts to degrade or errors appear
///
/// Metrics collected:
/// - Latency trends across load levels
/// - Error rate at each stage
/// - System behavior under increasing load
/// </summary>
public static class RampUpScenario
{
    /// <summary>
    /// Creates a fast ramp-up scenario for quick stress testing
    /// Stages: 10 → 50 → 100 → 200 → 500 RPS, 1 minute each (total ~5 min)
    /// </summary>
    /// <param name="tunnelUrl">Base URL of the tunnel endpoint</param>
    /// <returns>NBomber scenario</returns>
    public static NBomber.Contracts.ScenarioProps CreateFast(string tunnelUrl)
    {
        return Create(tunnelUrl, stageDurationMinutes: 1);
    }

    /// <summary>
    /// Creates a standard ramp-up scenario (Phase 2.1 spec)
    /// Stages: 10 → 50 → 100 → 200 → 500 RPS, 5 minutes each (total ~25 min)
    /// </summary>
    /// <param name="tunnelUrl">Base URL of the tunnel endpoint</param>
    /// <returns>NBomber scenario</returns>
    public static NBomber.Contracts.ScenarioProps CreateStandard(string tunnelUrl)
    {
        return Create(tunnelUrl, stageDurationMinutes: 5);
    }

    /// <summary>
    /// Creates a custom ramp-up scenario with specified stage duration
    /// </summary>
    /// <param name="tunnelUrl">Base URL of the tunnel endpoint</param>
    /// <param name="stageDurationMinutes">Duration of each stage in minutes</param>
    /// <returns>NBomber scenario</returns>
    public static NBomber.Contracts.ScenarioProps Create(string tunnelUrl, int stageDurationMinutes)
    {
        // Configure connection pool to handle high load
        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 1000,  // Increase from default 100
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
            EnableMultipleHttp2Connections = true
        };

        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30) // Increased timeout for high load
        };

        var scenario = Scenario.Create("ramp_up_stress_test", async context =>
        {
            var request = Http.CreateRequest("GET", $"{tunnelUrl}/get")
                .WithHeader("Accept", "application/json")
                .WithHeader("User-Agent", $"NBomber-LoadTest-RampUp/1.0-Stage{context.ScenarioInfo.ThreadNumber}");

            var response = await Http.Send(httpClient, request);

            return response;
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            // Stage 1: 10 RPS (baseline)
            Simulation.Inject(
                rate: 10,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(stageDurationMinutes)
            ),

            // Stage 2: 50 RPS
            Simulation.Inject(
                rate: 50,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(stageDurationMinutes)
            ),

            // Stage 3: 100 RPS
            Simulation.Inject(
                rate: 100,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(stageDurationMinutes)
            ),

            // Stage 4: 200 RPS
            Simulation.Inject(
                rate: 200,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(stageDurationMinutes)
            ),

            // Stage 5: 500 RPS (stress level)
            Simulation.Inject(
                rate: 500,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(stageDurationMinutes)
            )
        );

        return scenario;
    }

    /// <summary>
    /// Creates an aggressive ramp-up scenario with very short stages
    /// Stages: 20 → 50 → 100 → 200 → 500 → 1000 RPS, 30 seconds each (total ~3 min)
    /// Use for quick smoke testing to find approximate limits
    /// </summary>
    /// <param name="tunnelUrl">Base URL of the tunnel endpoint</param>
    /// <returns>NBomber scenario</returns>
    public static NBomber.Contracts.ScenarioProps CreateAggressive(string tunnelUrl)
    {
        // Configure connection pool to handle high load
        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 1000,  // Increase from default 100
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
            EnableMultipleHttp2Connections = true
        };

        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        var scenario = Scenario.Create("aggressive_ramp_up", async context =>
        {
            var request = Http.CreateRequest("GET", $"{tunnelUrl}/get")
                .WithHeader("Accept", "application/json")
                .WithHeader("User-Agent", "NBomber-LoadTest-Aggressive/1.0");

            var response = await Http.Send(httpClient, request);

            return response;
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            // 30 seconds per stage
            Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: 200, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: 500, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: 1000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
        );

        return scenario;
    }
}
