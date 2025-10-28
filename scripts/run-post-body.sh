#!/bin/bash

# POST with body load test script for Tunnel2
# Phase 1.4 from LOAD_TESTING_ROADMAP.md
#
# Tests POST requests with different body sizes: 1KB, 10KB, 100KB
#
# Prerequisites:
# - Docker stack running: docker compose -f tunnel2-deploy/docker-compose-localhost.yml up -d
# - Tunnel client running with SessionId: 22222222-2222-2222-2222-222222222222
# - Backend (httpbin) running on port 12005
#
# Usage:
#   ./run-post-body.sh [body-size] [tunnel-url]
#
# body-size: 1kb, 10kb, 100kb, or all (default: all)
#
# Example:
#   ./run-post-body.sh 10kb
#   ./run-post-body.sh all http://localhost:12000/session/22222222-2222-2222-2222-222222222222

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/../src/Tunnel2.LoadTests"
REPORTS_DIR="$SCRIPT_DIR/../reports"

# Parse arguments
BODY_SIZE="${1:-all}"
TUNNEL_URL="${2:-http://localhost:12000/session/22222222-2222-2222-2222-222222222222}"

echo "========================================"
echo "Tunnel2 POST Body Load Test"
echo "========================================"
echo ""
echo "Body Size: $BODY_SIZE"
echo "Tunnel URL: $TUNNEL_URL"
echo "Reports will be saved to: $REPORTS_DIR"
echo ""

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
    echo "  1. Docker stack is running"
    echo "  2. Tunnel client is running with SessionId: 22222222-2222-2222-2222-222222222222"
    echo ""
    exit 1
fi

echo ""
echo "Starting POST body test..."
echo "  - Warm-up: 10 seconds"
echo "  - Load: 10 RPS for 2 minutes"
echo "  - Body size(s): $BODY_SIZE"
echo ""

# Run the load test
cd "$PROJECT_DIR"

case "$BODY_SIZE" in
    1kb)
        dotnet run -c Release -- \
            --scenario post-1kb \
            --tunnel-url "$TUNNEL_URL" \
            --reports-path "$REPORTS_DIR"
        ;;
    10kb)
        dotnet run -c Release -- \
            --scenario post-10kb \
            --tunnel-url "$TUNNEL_URL" \
            --reports-path "$REPORTS_DIR"
        ;;
    100kb)
        dotnet run -c Release -- \
            --scenario post-100kb \
            --tunnel-url "$TUNNEL_URL" \
            --reports-path "$REPORTS_DIR"
        ;;
    all)
        echo "Running all POST body size scenarios..."
        dotnet run -c Release -- \
            --scenario post-all \
            --tunnel-url "$TUNNEL_URL" \
            --reports-path "$REPORTS_DIR"
        ;;
    *)
        echo "ERROR: Invalid body size: $BODY_SIZE"
        echo "Valid options: 1kb, 10kb, 100kb, all"
        exit 1
        ;;
esac

echo ""
echo "========================================"
echo "POST body test completed!"
echo "========================================"
echo ""
echo "View reports:"
echo "  HTML: $REPORTS_DIR/*.html"
echo "  Text: $REPORTS_DIR/*.txt"
echo ""
