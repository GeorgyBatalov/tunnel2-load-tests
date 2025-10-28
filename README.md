# Tunnel2 Load Tests

NBomber-based load testing suite for Tunnel2 tunneling system.

## Overview

This project contains load tests for the Tunnel2 split-plane tunneling system. It uses [NBomber](https://nbomber.com/) - a modern load testing framework for .NET applications.

## Roadmap

For detailed implementation roadmap, see [LOAD_TESTING_ROADMAP.md](https://github.com/your-org/xtunnel/blob/main/tunnel2-integration-tests/LOAD_TESTING_ROADMAP.md) in the main xtunnel repository.

**Implementation Phases:**
- **Phase 1: Foundation** (1-2 weeks) - Basic infrastructure, HTTP scenarios, baseline metrics
- **Phase 2: Stress & Capacity** (2-3 weeks) - Ramp-up, stress testing, large files, concurrent connections
- **Phase 3: Reliability** (2-3 weeks) - Soak tests, spike tests, connection churn
- **Phase 4: Advanced** (3-4 weeks, optional) - Mixed workload, CI/CD integration, monitoring

## Project Structure

```
tunnel2-load-tests/
├── src/Tunnel2.LoadTests/
│   ├── Scenarios/          # Load test scenarios (e.g., BasicHttpScenario.cs)
│   ├── Config/             # Configuration files
│   ├── Reports/            # Generated reports (gitignored)
│   ├── Program.cs          # Main entry point
│   └── Tunnel2.LoadTests.csproj
├── scripts/                # Bash scripts for running tests (e.g., run-baseline.sh)
├── reports/                # Test reports output directory (gitignored)
└── README.md
```

## Prerequisites

- .NET 8.0 SDK
- Docker and Docker Compose (for running Tunnel2 stack)
- NBomber 6.1.2+

## Quick Start

### 1. Start Tunnel2 Stack

Before running load tests, ensure the Tunnel2 stack is running:

```bash
# From xtunnel root directory
cd xtunnel
docker compose -f tunnel2-deploy/docker-compose-localhost.yml up -d
```

### 2. Start Tunnel Client

Start the tunnel client with a fixed dev SessionId:

```bash
cd tunnel2-client/src/Tunnel.ClientApp
dotnet run -- \
  --dev-session-id=a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d \
  --backend-host=localhost \
  --backend-port=12005 \
  --tunnel-host=localhost \
  --tunnel-port=12002
```

**Note:** Backend port 12005 is httpbin running in Docker.

### 3. Run Baseline Test

```bash
cd tunnel2-load-tests
./scripts/run-baseline.sh
```

### 4. Run POST Body Tests

```bash
# Run all POST scenarios (1KB, 10KB, 100KB)
./scripts/run-post-body.sh all

# Or run specific body size
./scripts/run-post-body.sh 10kb
```

### 5. View Reports

Reports are generated in the `reports/` directory (HTML, TXT, MD formats).

## Current Status

✅ **Phase 1: Foundation** - COMPLETED
- ✅ Project structure created (Phase 1.1)
- ✅ BasicHttpScenario implemented (Phase 1.2)
- ✅ run-baseline.sh script created (Phase 1.2)
- ✅ BASELINE_METRICS.md template created (Phase 1.3)
- ✅ PostWithBodyScenario implemented (Phase 1.4)
- ✅ run-post-body.sh script created (Phase 1.4)
- ✅ GitHub Actions workflow added

**Ready for Phase 2:** Stress & Capacity Testing

**Next Steps:**
- Run baseline tests and collect metrics
- Implement RampUpScenario (Phase 2.1)
- Implement StressScenario (Phase 2.2)
- Test large file transfers (Phase 2.3)

## Technology Stack

- **Framework:** .NET 8.0
- **Load Testing:** NBomber 6.1.2
- **HTTP Client:** NBomber.Http
- **Reporting:** HTML reports (built-in)

## Development

### Adding a New Scenario

1. Create scenario class in `Scenarios/` directory
2. Implement scenario using NBomber API:
   ```csharp
   var scenario = Scenario.Create("scenario_name", async context =>
   {
       // Test logic here
       var response = await Http.Send(httpClient, request);
       return response;
   })
   .WithLoadSimulations(/* ... */);
   ```
3. Register scenario in `Program.cs`
4. Create run script in `scripts/`

### Running Specific Scenario

```bash
# Will be implemented in Phase 1
dotnet run -c Release -- --scenario=BasicHttp
```

## Configuration

Configuration will be stored in `Config/` directory:
- `baseline.json` - Baseline test configuration
- `stress.json` - Stress test configuration
- `soak.json` - Soak test configuration

## Contributing

See main [xtunnel](https://github.com/your-org/xtunnel) repository for contribution guidelines.

## References

- [NBomber Documentation](https://nbomber.com/docs)
- [NBomber GitHub](https://github.com/PragmaticFlow/NBomber)
- [Load Testing Best Practices](https://docs.microsoft.com/en-us/azure/architecture/best-practices/load-testing)
- [Tunnel2 Architecture](https://github.com/your-org/xtunnel)

## License

Same as main xtunnel project.

---

**Last Updated:** 2025-10-28
**Status:** Phase 0 - Initial Setup
**Owner:** TBD
