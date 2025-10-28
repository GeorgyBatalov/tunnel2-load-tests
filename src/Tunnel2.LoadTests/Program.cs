using NBomber.CSharp;
using NBomber.Contracts;
using Tunnel2.LoadTests.Config;
using Tunnel2.LoadTests.Scenarios;

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

        // Parse command line arguments
        var config = ParseArguments(args);

        Console.WriteLine($"Configuration:");
        Console.WriteLine($"  Tunnel URL: {config.TunnelUrl}");
        Console.WriteLine($"  Backend URL: {config.BackendUrl}");
        Console.WriteLine($"  Reports Path: {config.ReportsPath}");
        Console.WriteLine();

        // Determine which scenario to run
        var scenarioName = GetScenarioName(args);

        switch (scenarioName.ToLower())
        {
            case "baseline":
            case "basic":
                RunBasicHttpScenario(config);
                break;

            case "post-1kb":
                RunPostScenario(config, "1KB");
                break;

            case "post-10kb":
                RunPostScenario(config, "10KB");
                break;

            case "post-100kb":
                RunPostScenario(config, "100KB");
                break;

            case "post-all":
                RunPostAllScenarios(config);
                break;

            default:
                Console.WriteLine($"Running default scenario: BasicHttp");
                RunBasicHttpScenario(config);
                break;
        }
    }

    private static void RunBasicHttpScenario(ScenarioConfig config)
    {
        Console.WriteLine("Running BasicHttpScenario (Phase 1.2)");
        Console.WriteLine("Warm-up: 10 seconds");
        Console.WriteLine("Load: 10 RPS for 2 minutes");
        Console.WriteLine();

        var scenario = BasicHttpScenario.Create(config.TunnelUrl);

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(config.ReportsPath)
            .Run();

        Console.WriteLine();
        Console.WriteLine($"Reports saved to: {config.ReportsPath}");
    }

    private static void RunPostScenario(ScenarioConfig config, string bodySize)
    {
        Console.WriteLine($"Running PostWithBodyScenario (Phase 1.4) - {bodySize}");
        Console.WriteLine("Warm-up: 10 seconds");
        Console.WriteLine("Load: 10 RPS for 2 minutes");
        Console.WriteLine();

        ScenarioProps scenario = bodySize switch
        {
            "1KB" => PostWithBodyScenario.Create1KB(config.TunnelUrl),
            "10KB" => PostWithBodyScenario.Create10KB(config.TunnelUrl),
            "100KB" => PostWithBodyScenario.Create100KB(config.TunnelUrl),
            _ => throw new ArgumentException($"Invalid body size: {bodySize}")
        };

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(config.ReportsPath)
            .Run();

        Console.WriteLine();
        Console.WriteLine($"Reports saved to: {config.ReportsPath}");
    }

    private static void RunPostAllScenarios(ScenarioConfig config)
    {
        Console.WriteLine("Running All POST scenarios (Phase 1.4)");
        Console.WriteLine("Scenarios: 1KB, 10KB, 100KB (concurrent)");
        Console.WriteLine("Warm-up: 10 seconds each");
        Console.WriteLine("Load: 10 RPS for 2 minutes each");
        Console.WriteLine();

        var scenarios = PostWithBodyScenario.CreateAllSizes(config.TunnelUrl);

        NBomberRunner
            .RegisterScenarios(scenarios)
            .WithReportFolder(config.ReportsPath)
            .Run();

        Console.WriteLine();
        Console.WriteLine($"Reports saved to: {config.ReportsPath}");
    }

    private static ScenarioConfig ParseArguments(string[] args)
    {
        var config = new ScenarioConfig();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--tunnel-url" && i + 1 < args.Length)
            {
                config.TunnelUrl = args[i + 1];
                i++;
            }
            else if (args[i] == "--backend-url" && i + 1 < args.Length)
            {
                config.BackendUrl = args[i + 1];
                i++;
            }
            else if (args[i] == "--reports-path" && i + 1 < args.Length)
            {
                config.ReportsPath = args[i + 1];
                i++;
            }
            else if (args[i] == "--verbose")
            {
                config.VerboseLogging = true;
            }
        }

        return config;
    }

    private static string GetScenarioName(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--scenario" && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }

        return "basic";
    }
}
