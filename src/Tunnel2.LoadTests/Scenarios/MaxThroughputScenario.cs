using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace Tunnel2.LoadTests.Scenarios;

/// <summary>
/// Max Throughput scenario - finds the breaking point (max RPS before errors)
///
/// Strategy: Aggressive binary search approach
/// - Starts at known stable point (500 RPS)
/// - Ramps up aggressively: 500 → 750 → 1000 → 1500 → 2000 → 3000 RPS
/// - Each stage: 1 minute duration
/// - Monitors error rate - if errors > 1%, marks as breaking point
///
/// Goal: Find maximum sustainable RPS with <1% error rate
/// </summary>
public static class MaxThroughputScenario
{
    /// <summary>
    /// Creates max throughput test scenario
    /// Aggressive ramp: 500 → 750 → 1000 → 1500 → 2000 → 3000 RPS
    /// Stage duration: 1 minute each (total ~6 min + warmup)
    /// </summary>
    public static NBomber.Contracts.ScenarioProps Create(string tunnelUrl)
    {
        // Configure for extreme load
        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 5000,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            EnableMultipleHttp2Connections = true,
            ConnectTimeout = TimeSpan.FromSeconds(10)
        };

        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        var scenario = Scenario.Create("max_throughput", async context =>
        {
            var request = Http.CreateRequest("GET", $"{tunnelUrl}/get")
                .WithHeader("Accept", "application/json")
                .WithHeader("User-Agent", "NBomber-MaxThroughput/1.0");

            var response = await Http.Send(httpClient, request);

            return response;
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(15))
        .WithLoadSimulations(
            // Stage 1: 500 RPS (known stable)
            Simulation.Inject(
                rate: 500,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(1)
            ),

            // Stage 2: 750 RPS
            Simulation.Inject(
                rate: 750,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(1)
            ),

            // Stage 3: 1000 RPS
            Simulation.Inject(
                rate: 1000,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(1)
            ),

            // Stage 4: 1500 RPS
            Simulation.Inject(
                rate: 1500,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(1)
            ),

            // Stage 5: 2000 RPS
            Simulation.Inject(
                rate: 2000,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(1)
            ),

            // Stage 6: 3000 RPS (extreme)
            Simulation.Inject(
                rate: 3000,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(1)
            )
        );

        return scenario;
    }

    /// <summary>
    /// Creates ultra-aggressive max throughput test - finds breaking point
    /// Extreme stages: 1k → 5k → 10k → 20k → 50k → 100k RPS
    /// Stage duration: 20 seconds each (total ~2 min)
    /// Test stops when errors appear - finds absolute max RPS
    /// </summary>
    public static NBomber.Contracts.ScenarioProps CreateUltraAggressive(string tunnelUrl)
    {
        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 100000,
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            EnableMultipleHttp2Connections = true,
            ConnectTimeout = TimeSpan.FromSeconds(5)
        };

        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        var scenario = Scenario.Create("max_throughput_ultra", async context =>
        {
            var request = Http.CreateRequest("GET", $"{tunnelUrl}/get")
                .WithHeader("Accept", "application/json")
                .WithHeader("User-Agent", "NBomber-MaxThroughput-Ultra/1.0");

            var response = await Http.Send(httpClient, request);

            return response;
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(10))
        .WithLoadSimulations(
            Simulation.Inject(rate: 1000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate: 5000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate: 10000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate: 20000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate: 50000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate: 100000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20))
        );

        return scenario;
    }
}
