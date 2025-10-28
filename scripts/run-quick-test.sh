#!/bin/bash
# Quick test to verify system performance

set -e

TUNNEL_URL="${1:-http://localhost:12000/session/22222222-2222-2222-2222-222222222222}"

echo "Quick 500 RPS test for 30 seconds..."
echo "URL: $TUNNEL_URL"

cd /Users/chefbot/RiderProjects/xtunnel/tunnel2-load-tests/src/Tunnel2.LoadTests

# Run quick test: 500 RPS for 30 seconds
dotnet run -c Release -- \
  --scenario quick-test \
  --tunnel-url "$TUNNEL_URL" \
  --reports-path "../../reports" 2>&1 | tail -50
