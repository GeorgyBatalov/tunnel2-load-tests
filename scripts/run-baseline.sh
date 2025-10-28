#!/bin/bash

# Baseline load test script for Tunnel2
# Phase 1.2 from LOAD_TESTING_ROADMAP.md
#
# Prerequisites:
# - Docker stack running: docker compose -f tunnel2-deploy/docker-compose-localhost.yml up -d
# - Tunnel client running with SessionId: a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d
# - Backend (httpbin) running on port 12005
#
# Usage:
#   ./run-baseline.sh [tunnel-url]
#
# Example:
#   ./run-baseline.sh http://localhost:12000/a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/../src/Tunnel2.LoadTests"
REPORTS_DIR="$SCRIPT_DIR/../reports"

# Default tunnel URL (localhost path mapping with fixed dev SessionId)
TUNNEL_URL="${1:-http://localhost:12000/a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d}"

echo "========================================"
echo "Tunnel2 Baseline Load Test"
echo "========================================"
echo ""
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
    echo "  1. Docker stack is running: docker compose -f tunnel2-deploy/docker-compose-localhost.yml up -d"
    echo "  2. Tunnel client is running with SessionId: a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d"
    echo ""
    exit 1
fi

echo ""
echo "Starting baseline test..."
echo "  - Warm-up: 10 seconds"
echo "  - Load: 10 RPS for 2 minutes"
echo ""

# Run the load test
cd "$PROJECT_DIR"
dotnet run -c Release -- \
    --scenario baseline \
    --tunnel-url "$TUNNEL_URL" \
    --reports-path "$REPORTS_DIR"

echo ""
echo "========================================"
echo "Baseline test completed!"
echo "========================================"
echo ""
echo "View reports:"
echo "  HTML: $REPORTS_DIR/*.html"
echo "  Text: $REPORTS_DIR/*.txt"
echo "  Markdown: $REPORTS_DIR/*.md"
echo ""
