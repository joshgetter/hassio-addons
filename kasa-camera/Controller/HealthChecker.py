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
        while True:
            isErrored = self.checkHealth()
            print(f'[HEALTH CHECK] Error: {isErrored}')
            self.controller.state.isErrored = isErrored
            await asyncio.sleep(self.sleepInterval)

    def stop(self):
        # Cancel the running health check task
        self.task.cancel()

    def stateHandler(self, event):
        if event.changedProperty == 'isRunning':
            if event.state.isRunning:
                print("Starting Health Checker")
                self.task = asyncio.ensure_future(self.start())
            else:
                print("Stopping Health Checker")
                self.stop()

    def __init__(self, controller, sleepInterval):
        self.controller = controller
        self.sleepInterval = sleepInterval
        self.running = False
        
        # Bind to state changes
        self.controller.state.bind_to(self.stateHandler)

