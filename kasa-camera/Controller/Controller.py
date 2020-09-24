import os
import json
import HAListener
import Wrapper
import asyncio
import State
import requests
import HealthChecker
from concurrent.futures import ThreadPoolExecutor

class Controller:
    HA_REST_URL = "http://supervisor/core/api"

    def loadConfiguration(self, path):
        print("[Controller] Loading configuration.")
        with open(path) as configFile:
            parsedConfig = json.load(configFile)
            return parsedConfig
        print("[Controller] Configuration loaded.")

    def getToggleValue(self):
        toggleEntity = self.config["toggleentity"]
        if not toggleEntity:
            # If there is no toggle provided we can assume it should always be on
            return True
        
        url = f'{self.HA_REST_URL}/states/{toggleEntity}'
        headers = {"Authorization": f"Basic {self.haToken}"}
        response = requests.get(url=url, headers=headers).json()
        return response["state"] == "on"

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
        print("[Controller] Controller is starting.")

        # Set variables
        self.config = self.loadConfiguration("/data/options.json")
        self.haToken = os.environ['SUPERVISOR_TOKEN']

        # Create State object
        self.state = State.State()

        # Create Ffmpeg wrapper
        self.ffmpegWrapper = Wrapper.FfmpegWrapper(self, 10)

        # Register observer
        self.state.bind_to(self.stateHandler)

        # Setup home assistant listener
        self.haListener = HAListener.HAListener(self)
        
        # Start tasks
        loop = asyncio.get_event_loop()
        listener = asyncio.ensure_future(self.haListener.start())

        # Get initial toggle value
        initialState = self.getToggleValue()
        print(f'[Controller] Initial toggle status: {initialState}.')
        self.state.isCameraEnabled = initialState

        print("[Controller] Controller is running.")
        loop.run_forever()
        print("[Controller] Exiting.")

def main():
   controller = Controller()
   controller.run()

if __name__ == '__main__':
    main()