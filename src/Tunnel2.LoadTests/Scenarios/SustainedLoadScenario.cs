using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace Tunnel2.LoadTests.Scenarios;

/// <summary>
/// Sustained load test scenario for regression testing baseline performance
/// Tests continuous load without cooldown periods to identify realistic limits
///
/// Test approach:
/// - Gradual ramp-up with sustained load at each level
/// - No cooldown between stages (simulates real traffic patterns)
/// - Stops at first sign of degradation (errors or high latency)
///
/// Goal: Establish baseline RPS for regression testing
///
/// Metrics collected:
/// - Error-free RPS threshold
/// - Latency stability under continuous load
/// - System behavior when queues accumulate
/// </summary>
public static class SustainedLoadScenario
{
    /// <summary>
    /// Creates a sustained load scenario for finding error-free RPS threshold
    /// Stages: 100 → 200 → 300 → 400 → 500 → 600 → 700 → 800 RPS
    /// Duration: 1 minute per stage (total ~8 minutes)
    ///
    /// Based on empirical testing:
    /// - With cooldown: 1100 RPS error-free
    /// - Without cooldown: 500 RPS error-free
    /// This scenario tests continuous load to find sustainable threshold
    /// </summary>
    /// <param name="tunnelUrl">Base URL of the tunnel endpoint</param>
    /// <returns>NBomber scenario</returns>
    public static NBomber.Contracts.ScenarioProps Create(string tunnelUrl)
    {
        // Configure connection pool for sustained high load
        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 1000,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            EnableMultipleHttp2Connections = true
        };

        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        var scenario = Scenario.Create("sustained_load", async context =>
        {
            var request = Http.CreateRequest("GET", $"{tunnelUrl}/get")
                .WithHeader("Accept", "application/json")
                .WithHeader("User-Agent", "NBomber-SustainedLoad/1.0");

            var response = await Http.Send(httpClient, request);

            return response;
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            // Gradual ramp-up: 100 → 800 RPS in 100 RPS increments
            // 1 minute per stage to allow queues to accumulate
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.Inject(rate: 200, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.Inject(rate: 300, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.Inject(rate: 400, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.Inject(rate: 500, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.Inject(rate: 600, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.Inject(rate: 700, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            Simulation.Inject(rate: 800, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1))
        );

        return scenario;
    }

    /// <summary>
    /// Creates a quick sustained load scenario for fast regression tests
    /// Stages: 100 → 300 → 500 → 700 RPS
    /// Duration: 30 seconds per stage (total ~2 minutes)
    /// </summary>
    /// <param name="tunnelUrl">Base URL of the tunnel endpoint</param>
    /// <returns>NBomber scenario</returns>
    public static NBomber.Contracts.ScenarioProps CreateQuick(string tunnelUrl)
    {
        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 1000,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            EnableMultipleHttp2Connections = true
        };

        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        var scenario = Scenario.Create("sustained_load_quick", async context =>
        {
            var request = Http.CreateRequest("GET", $"{tunnelUrl}/get")
                .WithHeader("Accept", "application/json")
                .WithHeader("User-Agent", "NBomber-SustainedLoadQuick/1.0");

            var response = await Http.Send(httpClient, request);

            return response;
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            // Quick check: 100 → 700 RPS in 200 RPS increments
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: 300, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: 500, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: 700, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30))
        );

        return scenario;
    }

    /// <summary>
    /// Creates a baseline verification scenario for CI/CD regression testing
    /// Fixed load: 400 RPS for 2 minutes
    ///
    /// Success criteria:
    /// - 0 errors
    /// - p99 latency < 3 seconds
    /// - Average latency < 1 second
    ///
    /// Based on empirical testing showing 400-500 RPS as sustainable threshold
    /// </summary>
    /// <param name="tunnelUrl">Base URL of the tunnel endpoint</param>
    /// <returns>NBomber scenario</returns>
    public static NBomber.Contracts.ScenarioProps CreateBaselineCheck(string tunnelUrl)
    {
        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 500,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            EnableMultipleHttp2Connections = true
        };

        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        var scenario = Scenario.Create("baseline_check", async context =>
        {
            var request = Http.CreateRequest("GET", $"{tunnelUrl}/get")
                .WithHeader("Accept", "application/json")
                .WithHeader("User-Agent", "NBomber-BaselineCheck/1.0");

            var response = await Http.Send(httpClient, request);

            return response;
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            // Fixed 400 RPS for 2 minutes - baseline regression test
            Simulation.Inject(rate: 400, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2))
        );

        return scenario;
    }
}
