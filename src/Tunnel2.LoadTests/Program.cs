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

            case "ramp-up":
            case "rampup":
                RunRampUpScenario(config, "standard");
                break;

            case "ramp-up-fast":
            case "rampup-fast":
                RunRampUpScenario(config, "fast");
                break;

            case "ramp-up-aggressive":
            case "rampup-aggressive":
                RunRampUpScenario(config, "aggressive");
                break;

            case "max-throughput":
            case "maxthroughput":
                RunMaxThroughputScenario(config, "standard");
                break;

            case "max-throughput-ultra":
            case "maxthroughput-ultra":
                RunMaxThroughputScenario(config, "ultra");
                break;

            case "sustained-load":
            case "sustained":
                RunSustainedLoadScenario(config, "standard");
                break;

            case "sustained-load-quick":
            case "sustained-quick":
                RunSustainedLoadScenario(config, "quick");
                break;

            case "baseline-check":
                RunSustainedLoadScenario(config, "baseline");
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

    private static void RunRampUpScenario(ScenarioConfig config, string variant)
    {
        Console.WriteLine($"Running Ramp-Up Stress Test (Phase 2.1) - {variant}");
        Console.WriteLine();

        ScenarioProps scenario = variant.ToLower() switch
        {
            "fast" => RampUpScenario.CreateFast(config.TunnelUrl),
            "aggressive" => RampUpScenario.CreateAggressive(config.TunnelUrl),
            "standard" => RampUpScenario.CreateStandard(config.TunnelUrl),
            _ => RampUpScenario.CreateFast(config.TunnelUrl)
        };

        if (variant.ToLower() == "fast")
        {
            Console.WriteLine("Load stages: 10 → 50 → 100 → 200 → 500 RPS");
            Console.WriteLine("Stage duration: 1 minute each");
            Console.WriteLine("Total duration: ~5 minutes");
        }
        else if (variant.ToLower() == "aggressive")
        {
            Console.WriteLine("Load stages: 20 → 50 → 100 → 200 → 500 → 1000 RPS");
            Console.WriteLine("Stage duration: 30 seconds each");
            Console.WriteLine("Total duration: ~3 minutes");
        }
        else
        {
            Console.WriteLine("Load stages: 10 → 50 → 100 → 200 → 500 RPS");
            Console.WriteLine("Stage duration: 5 minutes each");
            Console.WriteLine("Total duration: ~25 minutes");
        }

        Console.WriteLine();
        Console.WriteLine("Goal: Find performance limits and degradation points");
        Console.WriteLine();

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(config.ReportsPath)
            .Run();

        Console.WriteLine();
        Console.WriteLine($"Reports saved to: {config.ReportsPath}");
    }

    private static void RunMaxThroughputScenario(ScenarioConfig config, string variant)
    {
        Console.WriteLine($"Running Max Throughput Test - {variant}");
        Console.WriteLine();

        ScenarioProps scenario = variant.ToLower() switch
        {
            "ultra" => MaxThroughputScenario.CreateUltraAggressive(config.TunnelUrl),
            "standard" => MaxThroughputScenario.Create(config.TunnelUrl),
            _ => MaxThroughputScenario.Create(config.TunnelUrl)
        };

        if (variant.ToLower() == "ultra")
        {
            Console.WriteLine("Load stages: 1k → 5k → 10k → 20k → 50k → 100k RPS");
            Console.WriteLine("Stage duration: 20 seconds each");
            Console.WriteLine("Total duration: ~2 minutes");
            Console.WriteLine("WARNING: Extreme test! Will push system to absolute limits.");
        }
        else
        {
            Console.WriteLine("Load stages: 500 → 750 → 1000 → 1500 → 2000 → 3000 RPS");
            Console.WriteLine("Stage duration: 1 minute each");
            Console.WriteLine("Total duration: ~6 minutes");
        }

        Console.WriteLine();
        Console.WriteLine("Goal: Find maximum sustainable RPS (breaking point)");
        Console.WriteLine("Success criteria: <1% error rate");
        Console.WriteLine();

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(config.ReportsPath)
            .Run();

        Console.WriteLine();
        Console.WriteLine($"Reports saved to: {config.ReportsPath}");
    }

    private static void RunSustainedLoadScenario(ScenarioConfig config, string variant)
    {
        Console.WriteLine($"Running Sustained Load Test - {variant}");
        Console.WriteLine();

        ScenarioProps scenario = variant.ToLower() switch
        {
            "quick" => SustainedLoadScenario.CreateQuick(config.TunnelUrl),
            "baseline" => SustainedLoadScenario.CreateBaselineCheck(config.TunnelUrl),
            "standard" => SustainedLoadScenario.Create(config.TunnelUrl),
            _ => SustainedLoadScenario.Create(config.TunnelUrl)
        };

        if (variant.ToLower() == "quick")
        {
            Console.WriteLine("Load stages: 100 → 300 → 500 → 700 RPS");
            Console.WriteLine("Stage duration: 30 seconds each");
            Console.WriteLine("Total duration: ~2 minutes");
        }
        else if (variant.ToLower() == "baseline")
        {
            Console.WriteLine("Fixed load: 400 RPS for 2 minutes");
            Console.WriteLine("Success criteria: 0 errors, p99 < 3s, avg < 1s");
            Console.WriteLine("Purpose: CI/CD regression baseline check");
        }
        else
        {
            Console.WriteLine("Load stages: 100 → 200 → 300 → 400 → 500 → 600 → 700 → 800 RPS");
            Console.WriteLine("Stage duration: 1 minute each");
            Console.WriteLine("Total duration: ~8 minutes");
        }

        Console.WriteLine();
        Console.WriteLine("Goal: Find error-free RPS threshold without cooldown");
        Console.WriteLine("Note: No cooldown between stages (simulates continuous traffic)");
        Console.WriteLine();

        NBomberRunner
            .RegisterScenarios(scenario)
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
