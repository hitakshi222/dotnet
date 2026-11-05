#!/bin/bash
set -e

cd "$(dirname "$0")"

echo "Building and testing API..."
dotnet test api.tests
echo "API tests passed."

echo "Building web app..."
cd ../frontend
npm ci
npm run build
echo "Web app built successfully."

echo "CI passed."
