namespace Tunnel2.LoadTests.Config;

/// <summary>
/// Configuration for load test scenarios
/// </summary>
public class ScenarioConfig
{
    /// <summary>
    /// Tunnel URL for localhost (path mapping: http://localhost:12000/sessionId)
    /// Example: http://localhost:12000/a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d
    /// Can be overridden via command line argument
    /// </summary>
    public string TunnelUrl { get; set; } = "http://localhost:12000/a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d";

    /// <summary>
    /// Test backend URL (direct connection, bypassing tunnel)
    /// Used for comparison tests
    /// </summary>
    public string BackendUrl { get; set; } = "http://localhost:8080";

    /// <summary>
    /// Reports output directory
    /// </summary>
    public string ReportsPath { get; set; } = "../../../reports";

    /// <summary>
    /// Enable detailed logging
    /// </summary>
    public bool VerboseLogging { get; set; } = false;
}

/// <summary>
/// Baseline test configuration (Phase 1.3)
/// </summary>
public class BaselineConfig
{
    public int WarmupSeconds { get; set; } = 10;
    public int RequestsPerSecond { get; set; } = 10;
    public int DurationMinutes { get; set; } = 2;
}

/// <summary>
/// Stress test configuration (Phase 2.2)
/// </summary>
public class StressConfig
{
    public int InitialRps { get; set; } = 10;
    public double IncreasePercentage { get; set; } = 0.20; // 20%
    public int StepDurationSeconds { get; set; } = 60;
    public double MaxErrorRate { get; set; } = 0.05; // 5%
}

/// <summary>
/// Ramp-up test configuration (Phase 2.1)
/// </summary>
public class RampUpConfig
{
    public int[] RpsLevels { get; set; } = { 10, 50, 100, 200, 500 };
    public int LevelDurationMinutes { get; set; } = 5;
}
