import os
import json
import HAListener
import Wrapper
import asyncio
import State
import requests
from concurrent.futures import ThreadPoolExecutor

class Controller:
    HA_REST_URL = "http://supervisor/core/api"

    def loadConfiguration(self, path):
        with open(path) as configFile:
            parsedConfig = json.load(configFile)
            print("Parsed Config:")
            print(parsedConfig)
            return parsedConfig

    def getToggleValue(self):
        url = f'{self.HA_REST_URL}/states/{self.config["toggleentity"]}'
        headers = {"Authorization": f"Basic {self.haToken}"}
        print(f"Making GET request to {url}")
        response = requests.get(url=url, headers=headers).json()
        isEnabled = response["state"] == "on"
        print(f'Initial toggle status: {isEnabled}')
        return isEnabled


    def stateHandler(self, event):
        if event.changedProperty == 'isCameraEnabled':
            # Start process if the camera is enabled but not currently running
            if event.state.isCameraEnabled and not(event.state.isRunning):
                self.ffmpegWrapper.startProcess()
            # Stop process if the camera is currently running but not enabled
            elif not(event.state.isCameraEnabled) and event.state.isRunning:
                self.ffmpegWrapper.stopProcess()
        elif event.changedProperty == 'isErrored':
            # If the camera is enabled but errored then it should be restarted
            if event.state.isErrored and event.state.isCameraEnabled:
                self.ffmpegWrapper.restartProcess()

    def run(self):
        # Set variables
        self.config = self.loadConfiguration("/data/options.json")
        self.haToken = os.environ['SUPERVISOR_TOKEN']

        # Create loop
        loop = asyncio.get_event_loop()

        # Create Ffmpeg wrapper
        self.ffmpegWrapper = Wrapper.FfmpegWrapper(self, 10)

        # Create State object
        self.state = State.State()

        # Register observer
        self.state.bind_to(self.stateHandler)

        # Get initial toggle value
        if self.getToggleValue():
            self.ffmpegWrapper.startProcess()
        
        # Setup home assistant listener
        self.haListener = HAListener.HAListener(self)
        asyncio.ensure_future(self.haListener.start())

        print("Controller is running")
        loop.run_forever()
        print("Exiting")

def main():
   controller = Controller()
   controller.run()

if __name__ == '__main__':
    main()