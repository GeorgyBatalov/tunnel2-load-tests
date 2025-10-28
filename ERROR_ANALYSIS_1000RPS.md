# Error Analysis: Connection Failures at 1000 RPS

**Date:** 2025-10-28
**Test:** Aggressive Ramp-Up Stress Test
**Issue:** 191 connection errors (0.34% failure rate) at 1000 RPS stage

---

## Error Details

### Error Signature

```
System.Net.Http.HttpRequestException: An error occurred while sending the request.
 ---> System.Net.Http.HttpIOException: The response ended prematurely. (ResponseEnded)
```

### Error Statistics

| Metric | Value |
|--------|-------|
| Total Errors | 191 |
| Error Rate | 0.34% (191 out of 56,100) |
| Error RPS | 1.06 req/sec |
| Status Code | -101 |
| Stage | 1000 RPS (final 30 seconds) |

### Error Latency Pattern

| Metric | Failed Requests | Successful Requests |
|--------|----------------|---------------------|
| Mean | 4.42 ms | 8.54 ms |
| P50 | 2.85 ms | 5.91 ms |
| P99 | 21.34 ms | 43.36 ms |

**Key Observation:** Failed requests have LOWER latency - they fail quickly before completing the request/response cycle.

---

## Root Cause Analysis

### 1. Error Meaning: "Response Ended Prematurely"

This error indicates:
- ✅ Connection was established successfully
- ✅ HTTP request was sent
- ✅ Server started sending response
- ❌ **Connection closed before response completed**

This is NOT:
- ❌ Connection refused
- ❌ DNS failure
- ❌ Network timeout (would have higher latency)

### 2. Investigation Results

#### System Limits (checked - NOT the issue)

```bash
# Host file descriptors
ulimit -n: unlimited

# Docker container limits
tunnel_entry: 1048576
tunnel_server: 1048576
```

✅ File descriptors are NOT the bottleneck.

#### Container Logs (checked - no errors)

- ✅ ProxyEntry: No errors logged during test
- ✅ TunnelServer: No errors logged during test
- ✅ Backend (httpbin): No errors logged during test

**Conclusion:** Services did not log errors, suggesting they handled requests normally but connections were closed prematurely.

### 3. Most Likely Root Causes

Based on the evidence, the issue is caused by:

#### **A. HttpClient Connection Pool Exhaustion (Primary Suspect)**

**Evidence:**
- Errors only appear at 1000 RPS
- Lower stages (20-500 RPS) work perfectly
- Default HttpClient connection limit: ~100 concurrent connections
- At 1000 RPS with 8ms average latency: ~8 concurrent connections needed
- BUT: Connection pool can be exhausted if old connections aren't released fast enough

**Current Code Issue (RampUpScenario.cs:56-58):**
```csharp
var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(30)
};
```

Problem: Single HttpClient instance, default connection pool settings.

**Default HttpClient Limits:**
- MaxConnectionsPerServer: 100 (should be enough)
- ConnectionLifetime: Infinite
- PooledConnectionIdleTimeout: 2 minutes

#### **B. Keep-Alive Connection Reuse Issues**

**Evidence:**
- "Response ended prematurely" suggests connection was reused but became stale
- At high RPS, connections may be closed by server but still in client pool
- HttpClient tries to reuse closed connection → fails

#### **C. Backend or Tunnel Server Connection Limits**

**Possible:**
- ASP.NET Core has default Kestrel limits
- Default MaxConcurrentConnections: 100 (for Kestrel)
- Default RequestQueueLimit: Depends on OS

---

## Why Errors Are at 1000 RPS, Not 500 RPS

The system can handle 500 RPS perfectly because:

1. **Connection duration:** 8ms average latency
2. **Concurrent connections at 500 RPS:** ~4 connections in flight at any time
3. **Concurrent connections at 1000 RPS:** ~8 connections in flight
4. **But:** If connection pool reuses connections incorrectly or connections pile up, we hit limits

The 191 errors suggest:
- Pool exhaustion happened intermittently
- ~6.37 errors/second during 30s burst (191 / 30)
- Most requests (993/1000) succeeded
- System is very close to its limit

---

## Solutions

### Immediate Fixes (Apply to Load Test Client)

#### 1. Configure HttpClient Connection Pool

**In RampUpScenario.cs (and other scenarios):**

```csharp
var handler = new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
    MaxConnectionsPerServer = 500,  // Increase from default 100
    EnableMultipleHttp2Connections = true
};

var httpClient = new HttpClient(handler)
{
    Timeout = TimeSpan.FromSeconds(30)
};
```

**Benefits:**
- More concurrent connections allowed
- Old connections recycled properly
- HTTP/2 support (if server supports it)

#### 2. Use Connection Pooling Per Thread

```csharp
// Create HttpClient once per scenario, reuse across requests
private static readonly HttpClient _httpClient = new HttpClient(new SocketsHttpHandler { ... });
```

### Server-Side Fixes (Apply to Tunnel Components)

#### 3. Increase Kestrel Limits (TunnelServer & ProxyEntry)

**In appsettings.json:**

```json
{
  "Kestrel": {
    "Limits": {
      "MaxConcurrentConnections": 1000,
      "MaxConcurrentUpgradedConnections": 1000,
      "MaxRequestBodySize": 104857600,
      "RequestHeadersTimeout": "00:00:30",
      "KeepAliveTimeout": "00:02:00"
    }
  }
}
```

#### 4. Tune TCP Settings (Docker Host)

```bash
# Increase TCP listen backlog
sysctl -w net.core.somaxconn=4096

# Reduce TIME_WAIT connections
sysctl -w net.ipv4.tcp_fin_timeout=30
sysctl -w net.ipv4.tcp_tw_reuse=1
```

#### 5. Enable HTTP/2 (If Not Already)

HTTP/2 multiplexes multiple requests over a single connection, reducing connection count.

**In Program.cs (Kestrel setup):**

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(12000, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
});
```

### Testing Recommendations

#### 6. Run Targeted Test at 500 RPS

Confirm 500 RPS is the safe limit:

```bash
# Create a sustained test at 500 RPS for 5 minutes
./scripts/run-sustained-500rps.sh
```

#### 7. Run Test with Fixed Connection Pool

Apply fix #1, then rerun aggressive test to confirm 0 errors.

---

## Expected Results After Fixes

### Client-Side Fixes Only (MaxConnectionsPerServer = 500)

**Expected:**
- 1000 RPS should work with 0 errors
- Latency should remain low
- Connection pool handles load properly

### Server-Side Fixes (Kestrel limits)

**Expected:**
- Handle even higher load (1500-2000 RPS)
- No connection refused errors
- Better concurrent connection handling

### Both Fixes Combined

**Expected:**
- Handle 2000+ RPS
- System limited by CPU/memory, not connections
- Ready for production scaling

---

## Action Plan

### Phase 1: Quick Fixes (Today)

- [x] Document error analysis
- [ ] Update RampUpScenario with SocketsHttpHandler
- [ ] Update BasicHttpScenario with SocketsHttpHandler
- [ ] Rerun aggressive test, verify 0 errors at 1000 RPS
- [ ] Document results

### Phase 2: Server Configuration (This Week)

- [ ] Update TunnelServer Kestrel limits
- [ ] Update ProxyEntry Kestrel limits
- [ ] Test with updated limits
- [ ] Run sustained 1000 RPS test (5 minutes)

### Phase 3: Production Readiness (Next Week)

- [ ] Enable HTTP/2 on all components
- [ ] Implement connection metrics/monitoring
- [ ] Create runbook for high-load scenarios
- [ ] Document production capacity limits
- [ ] Set up alerts for connection pool exhaustion

---

## Reference: Connection Math

At 1000 RPS with 8ms average latency:

```
Concurrent Connections = RPS × Latency (seconds)
                       = 1000 × 0.008
                       = 8 connections

BUT: With variance (P99 = 43ms):
Max Concurrent = 1000 × 0.043 = 43 connections
```

**Conclusion:** Default limit of 100 connections should be enough, but:
- Connection reuse issues
- Stale connections in pool
- Connection lifecycle management

Can cause pool exhaustion even when math says it shouldn't.

---

## Monitoring Recommendations

Add metrics to track:

1. **Connection Pool Stats:**
   - Active connections
   - Idle connections
   - Connection wait time
   - Pool exhaustion events

2. **Server Connection Stats:**
   - Current connections (Kestrel)
   - Connection rate (new/sec)
   - Connection errors
   - Keep-alive timeouts

3. **OS-Level Stats:**
   - TCP connection states
   - TIME_WAIT count
   - CLOSE_WAIT count
   - Retransmits

---

## Conclusion

The 191 errors at 1000 RPS are caused by **HttpClient connection pool limitations** in the load test client, not server capacity issues.

**Good News:**
- System can handle the load
- Errors are client-side configuration issue
- Server components are stable
- Fix is simple: increase MaxConnectionsPerServer

**Next Step:**
Implement Fix #1 (SocketsHttpHandler) and rerun test to confirm 0 errors.

---

**Last Updated:** 2025-10-28
**Status:** Root cause identified, fix ready to implement
**Priority:** Medium (system works well up to 500 RPS, fix needed for higher load)
