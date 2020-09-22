import json
import requests
import asyncio

class HealthChecker:
    # TODO - There should probably be a limit on the number of retry attempts
    
    # An initial delay is useful since the server won't start returning "200" for some time after starting ffmpeg
    INITIAL_DELAY = 30

    def checkHealth(self):        
        # Make request to HLS endpoint. Typically we get a 404 if things are broken.
        hlsResponse = requests.get(url=f'http://localhost/hls/{self.controller.config["cameraname"]}.m3u8')
        return hlsResponse.status_code == 404

    async def start(self):
        await asyncio.sleep(self.INITIAL_DELAY)
        self.running = True
        while self.running:
            isErrored = self.checkHealth()
            print(f'[HEALTH CHECK] Error: {isErrored}')
            self.controller.state.isErrored = isErrored
            await asyncio.sleep(self.sleepInterval)

    def stop(self):
        self.running = False

    def __init__(self, controller, sleepInterval):
        self.controller = controller
        self.sleepInterval = sleepInterval
        self.running = False

