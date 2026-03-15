#!/bin/bash

# FrontCRM Docker Compose Startup Script
# Execute this on the server

echo ""
echo "╔════════════════════════════════════════════════════════════╗"
echo "║        FrontCRM Docker Compose Startup Script              ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""

# Navigate to deployment directory
cd /home/ubuntu/frontcrm_deploy

if [ ! -f "docker-compose.yml" ]; then
    echo "❌ Error: docker-compose.yml not found!"
    echo "Make sure you are in the correct directory."
    exit 1
fi

echo "✓ Found docker-compose.yml"
echo ""

# Start containers
echo "Starting Docker containers..."
echo ""

docker-compose up -d

if [ $? -eq 0 ]; then
    echo ""
    echo "✓ Containers started successfully!"
    echo ""
    
    # Wait for containers to be ready
    sleep 3
    
    echo "Container Status:"
    echo "──────────────────────────────────────────────────────────"
    docker-compose ps
    echo "──────────────────────────────────────────────────────────"
    echo ""
    
    # Show network info
    echo "Service URLs:"
    echo "  Frontend: http://129.226.161.3"
    echo "  Backend API: http://129.226.161.3:5000"
    echo "  Postgres: localhost:5432"
    echo ""
    
    echo "View logs:"
    echo "  All services: docker-compose logs -f"
    echo "  Backend only: docker-compose logs -f backend"
    echo "  Frontend only: docker-compose logs -f frontend"
    echo "  Database only: docker-compose logs -f postgres"
    echo ""
    
    echo "Stop services:"
    echo "  docker-compose down"
    echo ""
    
else
    echo ""
    echo "❌ Error: Failed to start containers!"
    echo ""
    echo "Check logs for details:"
    docker-compose logs
    exit 1
fi

echo "✓ Startup completed!"
echo ""
