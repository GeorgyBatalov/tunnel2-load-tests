using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace Tunnel2.LoadTests;

/// <summary>
/// NBomber load testing runner for Tunnel2
/// Roadmap: LOAD_TESTING_ROADMAP.md in tunnel2-integration-tests
/// </summary>
internal static class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Tunnel2 Load Tests Runner");
        Console.WriteLine("========================");
        Console.WriteLine();
        Console.WriteLine("NOTE: This is a minimal setup. See LOAD_TESTING_ROADMAP.md for full implementation plan.");
        Console.WriteLine();

        // Example basic HTTP scenario
        // This is a placeholder - real scenarios will be implemented according to roadmap Phase 1.2
        var httpClient = new HttpClient();

        var scenario = Scenario.Create("basic_http_scenario", async context =>
        {
            var request = Http.CreateRequest("GET", "http://localhost:8080/get")
                .WithHeader("Accept", "application/json");

            var response = await Http.Send(httpClient, request);

            return response;
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(5))
        .WithLoadSimulations(
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1))
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        httpClient.Dispose();
    }
}
