#!/usr/bin/env bashio

# Start nginx
echo "Starting Nginx"
nginx
echo "Nginx Started"

echo "Starting controller"

cd /app
dotnet KasaStreamer.dll

# # TESTING - keeps docker container running
# tail -f /dev/null