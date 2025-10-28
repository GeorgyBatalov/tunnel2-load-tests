# Baseline Metrics for Tunnel2 Load Tests

**Purpose:** Track baseline performance metrics over time to detect performance regressions.

**Phase:** 1.3 from LOAD_TESTING_ROADMAP.md

---

## How to Collect Baseline Metrics

1. Start the Docker stack:
   ```bash
   cd xtunnel
   docker compose -f tunnel2-deploy/docker-compose-localhost.yml up -d
   ```

2. Start tunnel client with fixed SessionId:
   ```bash
   cd tunnel2-client/src/Tunnel.ClientApp
   dotnet run -- \
     --dev-session-id=a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d \
     --backend-host=localhost \
     --backend-port=8080 \
     --tunnel-host=localhost \
     --tunnel-port=12002
   ```

3. Run baseline test 3 times:
   ```bash
   cd tunnel2-load-tests
   ./scripts/run-baseline.sh
   ```

4. Record metrics below (average of 3 runs)

---

## Baseline Run History

### Run #1: [Date TBD]

**Environment:**
- CPU: Apple M1 Pro, 10 cores
- RAM: 32GB
- Docker: Settings TBD
- Network: localhost
- OS: macOS 15.6 (Darwin 24.6.0)

**Test Configuration:**
- Scenario: BasicHttpScenario
- Endpoint: /get (httpbin через tunnel)
- Warm-up: 10 seconds
- Load: 10 RPS for 2 minutes
- Total requests: ~1200

**Results:**
```
Latency:
  - P50: TBD ms
  - P75: TBD ms
  - P95: TBD ms
  - P99: TBD ms
  - Mean: TBD ms
  - StdDev: TBD ms

Throughput:
  - Actual RPS: TBD req/sec
  - Target RPS: 10 req/sec

Errors:
  - Error rate: TBD%
  - Total errors: TBD
  - Error types: TBD

Resource Usage (Tunnel components):
  - Tunnel Server CPU: TBD%
  - Tunnel Server Memory: TBD MB
  - Proxy Entry CPU: TBD%
  - Proxy Entry Memory: TBD MB
```

**Notes:**
- [Add any observations, anomalies, or special conditions]

---

### Run #2: [Date TBD]

**Environment:** [Same as Run #1]

**Results:**
```
[To be filled after second run]
```

---

### Run #3: [Date TBD]

**Environment:** [Same as Run #1]

**Results:**
```
[To be filled after third run]
```

---

## Average Baseline (3 runs)

**Latency:**
- P50: TBD ms
- P75: TBD ms
- P95: TBD ms
- P99: TBD ms
- Mean: TBD ms

**Throughput:**
- RPS: TBD req/sec

**Errors:**
- Error rate: TBD%

**Resource Usage:**
- Tunnel Server: TBD% CPU, TBD MB RAM
- Proxy Entry: TBD% CPU, TBD MB RAM

---

## Performance Targets (SLA)

Based on baseline metrics, establish SLA targets for production:

- **Target P95 latency:** < TBD ms (baseline + 20% margin)
- **Target P99 latency:** < TBD ms (baseline + 30% margin)
- **Target error rate:** < 0.5%
- **Target throughput:** > 10 RPS (sustained)

**Note:** These targets will be refined after Phase 2 stress testing.

---

## Regression Detection

Alert if metrics deviate from baseline by:
- **Latency increase:** > 20% (P95)
- **Throughput decrease:** > 10%
- **Error rate increase:** > 1%

---

## Next Steps

- [ ] Complete 3 baseline runs
- [ ] Calculate average metrics
- [ ] Set SLA targets
- [ ] Proceed to Phase 1.4 (POST with body tests)
- [ ] Proceed to Phase 2 (Stress & Capacity Testing)

---

**Last Updated:** 2025-10-28
**Status:** Template created, awaiting first baseline run
**Owner:** TBD
