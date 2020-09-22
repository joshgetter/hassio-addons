#!/usr/bin/env bashio

# Start nginx
echo "Starting Nginx"
nginx
echo "Nginx Started"

echo "Starting controller"
#python3 /Controller/Controller.py

# TESTING - keeps docker container running
tail -f /dev/null