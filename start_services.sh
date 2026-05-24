#!/bin/bash

# FrontCRM Deployment Script for CloudStudio
# This script starts all services on the remote server

echo "========================================="
echo "Starting FrontCRM Services"
echo "========================================="

# Check if docker-compose.yml exists
if [ ! -f "docker-compose.yml" ]; then
    echo "Error: docker-compose.yml not found!"
    exit 1
fi

# Build and start services
echo "1. Building Docker images..."
docker-compose build --no-cache

echo "2. Starting containers..."
docker-compose up -d

echo "3. Checking container status..."
docker-compose ps

echo "4. Services are now running:"
echo "   - Frontend: http://localhost:80"
echo "   - Backend API: http://localhost:5000"
echo "   - PostgreSQL: localhost:5432"

echo "5. To view logs:"
echo "   docker-compose logs -f"

echo "========================================="
echo "Deployment completed!"
echo "========================================="