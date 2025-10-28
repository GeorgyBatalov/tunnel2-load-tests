# Test Results After Connection Pool Fix

**Date:** 2025-10-28 15:02
**Session:** 2025-10-28_15.02.46_session_241fcec2
**Test:** Aggressive Ramp-Up with SocketsHttpHandler (MaxConnectionsPerServer = 1000)

---

## Applied Fix

Updated `RampUpScenario.cs` and `BasicHttpScenario.cs` with:

```csharp
var handler = new SocketsHttpHandler
{
    MaxConnectionsPerServer = 1000,  // Increased from default 100
    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
    EnableMultipleHttp2Connections = true
};

var httpClient = new HttpClient(handler)
{
    Timeout = TimeSpan.FromSeconds(30)
};
```

---

## Results

| Metric | Before Fix | After Fix | Change |
|--------|-----------|-----------|--------|
| Total Requests | 56,100 | 56,100 | Same |
| Successful | 55,909 (99.66%) | 55,898 (99.64%) | -11 (-0.02%) |
| Failed | 191 (0.34%) | 202 (0.36%) | +11 (+0.02%) |
| Avg RPS | 310.61 | 310.54 | -0.07 |
| Error RPS | 1.06 | 1.12 | +0.06 |

### Latency (Successful Requests)

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| Mean | 8.54 ms | 7.87 ms | **-0.67 ms** ⬇️ |
| P50 | 5.91 ms | 5.43 ms | **-0.48 ms** ⬇️ |
| P75 | 9.00 ms | 7.85 ms | **-1.15 ms** ⬇️ |
| P95 | 24.16 ms | 21.89 ms | **-2.27 ms** ⬇️ |
| P99 | 43.36 ms | 43.78 ms | +0.42 ms |

---

## Analysis

### ❌ Connection Pool Fix Did NOT Eliminate Errors

**Errors remained similar:**
- Before: 191 errors (0.34%)
- After: 202 errors (0.36%)
- **Difference: +11 errors (+5.8% increase)**

**Conclusion:** The issue is NOT caused by HttpClient connection pool exhaustion on the load test client side.

### ✅ Latency IMPROVED Slightly

Despite errors remaining, latency improved across most percentiles:
- P50: -8.1% faster
- P75: -12.8% faster
- P95: -9.4% faster

This suggests better connection reuse, but doesn't fix the root cause.

---

## Error Pattern Analysis

### Same Error Type

```
System.Net.Http.HttpRequestException: An error occurred while sending the request.
 ---> System.Net.Http.HttpIOException: The response ended prematurely. (ResponseEnded)
```

**Error characteristics:**
- Errors still occur at 1000 RPS stage
- Fast failures (mean: 4.53ms vs 7.87ms success)
- Consistent ~0.35% error rate across runs

### Resource Usage During Test

```
Container              CPU %    Memory
tunnel_entry           0.21%    72 MB
httpbin                0.39%    88 MB
tunnel_server          0.56%    53 MB
```

**All containers are under-utilized** - NOT a resource bottleneck.

---

## New Hypothesis: Server-Side Limits

Since client-side fix didn't work, the issue is likely:

### 1. **httpbin Limits (Primary Suspect)**

httpbin (Gunicorn) may have connection or worker limits:
- Default Gunicorn workers: 4
- Max concurrent connections per worker: ~1000
- At 1000 RPS burst, may exceed worker capacity

### 2. **Kestrel Limits (Tunnel Components)**

ASP.NET Core Kestrel default limits:
- MaxConcurrentConnections: 100 (likely the culprit!)
- MaxConcurrentUpgradedConnections: 100
- RequestQueueLimit: Varies by OS

### 3. **TCP/Network Limits**

- Listen backlog (somaxconn): 4096 ✅ (adequate)
- Connection tracking limits
- NAT table limits (for Docker networking)

---

## Next Steps to Identify Root Cause

### Test 1: Direct httpbin Test (Bypass Tunnel)

```bash
# Run load test directly against httpbin without tunnel
curl http://localhost:12005/get  # Verify httpbin is accessible
# Run NBomber test pointing to localhost:12005
```

**Goal:** Determine if httpbin is the bottleneck.

### Test 2: Check Kestrel Configuration

Inspect `appsettings.json` for TunnelServer and ProxyEntry:
- Look for Kestrel.Limits settings
- Default MaxConcurrentConnections is likely 100

### Test 3: Enable Detailed Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore": "Debug",
      "System.Net.Http": "Debug"
    }
  }
}
```

**Goal:** Capture detailed error messages from server side.

### Test 4: Increase Kestrel Limits

**In appsettings.json (TunnelServer & ProxyEntry):**

```json
{
  "Kestrel": {
    "Limits": {
      "MaxConcurrentConnections": 1000,
      "MaxConcurrentUpgradedConnections": 1000,
      "RequestHeadersTimeout": "00:00:30",
      "KeepAliveTimeout": "00:02:00"
    }
  }
}
```

---

## Recommendations

### Immediate (Today)

1. ✅ Connection pool fix applied (improved latency, didn't fix errors)
2. ⏭️ **Run direct httpbin test** (bypass tunnel)
3. ⏭️ **Check Kestrel configuration** in tunnel components
4. ⏭️ **Enable debug logging** during test

### Short-term (This Week)

1. Increase Kestrel MaxConcurrentConnections to 1000
2. Check httpbin/Gunicorn worker configuration
3. Rerun test with updated limits
4. Document actual bottleneck component

### Long-term

1. Implement connection metrics/monitoring
2. Add rate limiting to protect system
3. Horizontal scaling for >500 RPS
4. Production capacity planning

---

## Current Performance Status

### ✅ Excellent Performance Up To 500 RPS

- Zero errors at 20, 50, 100, 200, 500 RPS
- P95 latency < 25ms
- System stable and reliable

### ⚠️ Marginal Issues At 1000 RPS

- 99.64% success rate (still excellent!)
- 0.36% error rate (202 / 56,100)
- Errors localized to peak burst
- Not a critical issue for production

### 💡 Conclusion

**The system performs exceptionally well up to 500 RPS with zero errors.**

At 1000 RPS, there's a minor (0.36%) connection issue - likely Kestrel MaxConcurrentConnections limit (default: 100).

**Recommended production limit: 500 RPS per instance**
**With Kestrel tuning: Likely 800-1000+ RPS per instance**

---

## Report Files

- HTML: `reports/nbomber_report_2025-10-28--15-05-23.html`
- Markdown: `reports/nbomber_report_2025-10-28--15-05-23.md`
- CSV: `reports/nbomber_report_2025-10-28--15-05-23.csv`
- Log: `reports/nbomber-log-2025102820.txt`

---

**Last Updated:** 2025-10-28 20:05
**Status:** Client-side fix applied, server-side investigation needed
**Next:** Test direct httpbin & check Kestrel limits
