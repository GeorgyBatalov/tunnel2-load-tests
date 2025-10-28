#!/bin/bash

# Ramp-up stress test script for Tunnel2
# Phase 2.1 from LOAD_TESTING_ROADMAP.md
#
# Tests progressive load increase to find performance limits:
# - Fast: 10→50→100→200→500 RPS, 1 min each (~5 min total)
# - Aggressive: 20→50→100→200→500→1000 RPS, 30s each (~3 min total)
# - Standard: 10→50→100→200→500 RPS, 5 min each (~25 min total)
#
# Prerequisites:
# - Docker stack running: docker compose -f tunnel2-deploy/docker-compose-localhost.yml up -d
# - Tunnel client running with SessionId: 22222222-2222-2222-2222-222222222222
# - Backend (httpbin) running on port 12005
#
# Usage:
#   ./run-ramp-up.sh [variant] [tunnel-url]
#
# variant: fast, aggressive, standard (default: fast)
#
# Examples:
#   ./run-ramp-up.sh fast
#   ./run-ramp-up.sh aggressive
#   ./run-ramp-up.sh standard http://localhost:12000/session/22222222-2222-2222-2222-222222222222

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/../src/Tunnel2.LoadTests"
REPORTS_DIR="$SCRIPT_DIR/../reports"

# Parse arguments
VARIANT="${1:-fast}"
TUNNEL_URL="${2:-http://localhost:12000/session/22222222-2222-2222-2222-222222222222}"

echo "========================================"
echo "Tunnel2 Ramp-Up Stress Test"
echo "========================================"
echo ""
echo "Variant: $VARIANT"
echo "Tunnel URL: $TUNNEL_URL"
echo "Reports will be saved to: $REPORTS_DIR"
echo ""

# Validate variant
case "$VARIANT" in
    fast|aggressive|standard)
        ;;
    *)
        echo "ERROR: Invalid variant: $VARIANT"
        echo "Valid options: fast, aggressive, standard"
        exit 1
        ;;
esac

# Create reports directory if it doesn't exist
mkdir -p "$REPORTS_DIR"

# Check if tunnel is accessible
echo "Checking tunnel connectivity..."
if curl -s --max-time 5 "$TUNNEL_URL/get" > /dev/null; then
    echo "✓ Tunnel is accessible"
else
    echo "✗ ERROR: Cannot reach tunnel at $TUNNEL_URL"
    echo ""
    echo "Make sure:"
    echo "  1. Docker stack is running: docker compose -f tunnel2-deploy/docker-compose-localhost.yml up -d"
    echo "  2. Tunnel client is running with SessionId: 22222222-2222-2222-2222-222222222222"
    echo ""
    exit 1
fi

echo ""
echo "Starting ramp-up stress test ($VARIANT)..."

case "$VARIANT" in
    fast)
        echo "  - Stages: 10 → 50 → 100 → 200 → 500 RPS"
        echo "  - Duration: 1 minute per stage"
        echo "  - Total time: ~5 minutes"
        ;;
    aggressive)
        echo "  - Stages: 20 → 50 → 100 → 200 → 500 → 1000 RPS"
        echo "  - Duration: 30 seconds per stage"
        echo "  - Total time: ~3 minutes"
        ;;
    standard)
        echo "  - Stages: 10 → 50 → 100 → 200 → 500 RPS"
        echo "  - Duration: 5 minutes per stage"
        echo "  - Total time: ~25 minutes"
        ;;
esac

echo ""
echo "This test will help identify:"
echo "  - Maximum sustainable RPS"
echo "  - Latency degradation points"
echo "  - Error rate under stress"
echo ""

# Warn for long tests
if [ "$VARIANT" = "standard" ]; then
    echo "⚠️  WARNING: Standard test will take ~25 minutes"
    echo "   Press Ctrl+C within 5 seconds to cancel..."
    sleep 5
fi

# Run the load test
cd "$PROJECT_DIR"
dotnet run -c Release -- \
    --scenario "ramp-up-$VARIANT" \
    --tunnel-url "$TUNNEL_URL" \
    --reports-path "$REPORTS_DIR"

echo ""
echo "========================================"
echo "Ramp-up stress test completed!"
echo "========================================"
echo ""
echo "View reports:"
echo "  HTML: $REPORTS_DIR/*.html"
echo "  Text: $REPORTS_DIR/*.txt"
echo "  Markdown: $REPORTS_DIR/*.md"
echo ""
echo "Analyze the reports to find:"
echo "  - At which RPS level latency started increasing"
echo "  - At which RPS level errors appeared"
echo "  - Maximum sustainable throughput"
echo ""
