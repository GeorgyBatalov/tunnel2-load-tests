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

### 2. Run Load Tests

```bash
cd tunnel2-load-tests/src/Tunnel2.LoadTests
dotnet run -c Release
```

### 3. View Reports

Reports are generated in the `reports/` directory (HTML format).

## Current Status

🚧 **Phase 0: Project Setup** - COMPLETED
- ✅ Project structure created
- ✅ NBomber packages installed
- ✅ Directory layout established
- ⏳ Basic scenario implementation (in progress)

**Next Steps:**
- Implement BasicHttpScenario (Phase 1.2)
- Collect baseline metrics (Phase 1.3)
- Create run scripts

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
