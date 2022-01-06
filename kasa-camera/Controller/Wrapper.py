import subprocess
import base64
import asyncio
import time
import HealthChecker
import WrapperState

class FfmpegWrapper:
    def startProcess(self):
        print(f'[Wrapper {self.camera["cameraname"]}] Starting Ffmpeg.')
        startCommand = [
            'ffmpeg',
            '-nostdin',
            '-loglevel',
            'warning',
            '-reconnect',
            '1',
            '-reconnect_at_eof',
            '1',
            '-reconnect_streamed',
            '1',
            '-reconnect_delay_max',
            '60',
            '-headers',
            f'Authorization: Basic {self.authToken}',
            '-f',
            'h264',
            '-i',
            f'https://{self.camera["cameraip"]}:19443/https/stream/mixed?video=h264&audio=g711&resolution=hd',
            '-map',
            '0',
            '-vcodec',
            'libx264',
            '-preset',
            'veryfast',
            '-f',
            'flv',
            '' if self.camera.get("videofilter") is None else '-vf',
            self.camera.get("videofilter") or '',
            f'rtmp://localhost/live/{self.camera["cameraname"]}',
            '-map',
            '0',
            '-r',
            '1/5',
            '-update',
            '1',
            '-y',
            f'/tmp/streaming/thumbnails/{self.camera["cameraname"]}.jpg'
        ]

        # Remove any empty strings from the command
        startCommand = list(filter(lambda x: x is not None and x != '', startCommand))
        
        self.ffmpegProcess = subprocess.Popen(startCommand)
        self.state.isRunning = True

    def stopProcess(self):
        # TODO - Perhaps killall ffmpeg would be better?
        print(f'[Wrapper {self.camera["cameraname"]}] Killing Ffmpeg.')
        self.ffmpegProcess.kill()
        self.state.isRunning = False

    def restartProcess(self):
        print(f'[Wrapper {self.camera["cameraname"]}] Restarting Ffmpeg.')
        self.stopProcess()
        time.sleep(self.controller.config["retrysleep"])
        self.startProcess()

    def buildAuthToken(self):
        encodedPassword = self.encode(self.controller.config['kasapassword'])
        encodedPair = self.encode(f"{self.controller.config['kasausername']}:{encodedPassword}")
        return encodedPair

    def encode(self, input):
        encodedBytes = base64.b64encode(input.encode("utf-8"))
        encodedString = str(encodedBytes, "utf-8")
        return encodedString

    def stateHandler(self, event):
        if event.changedProperty == 'isCameraEnabled':
            # Start process if the camera is enabled but not currently running
            if event.state.isCameraEnabled and not(self.state.isRunning):
                self.startProcess()
            # Stop process if the camera is currently running but not enabled
            elif not(event.state.isCameraEnabled) and self.state.isRunning:
                self.stopProcess()
        elif event.changedProperty == 'errorCount':
            isCameraEnabled = self.controller.state.isCameraEnabled
            errorCount = event.state.errorCount
            retryLimit = self.controller.config["retrylimit"]
            if errorCount == 0 or not(isCameraEnabled):
                # Return since the camera either shouldn't be on, or no error is detected.
                return
            else:
                # Things are currently broken
                if errorCount <= retryLimit or retryLimit == -1:
                    # If we haven't reached the retry limit or the user specified unlimited retries, we should restart Ffmpeg.
                    print(f'[Wrapper {self.camera["cameraname"]}] Restarting Ffmpeg. Retry: {errorCount}.')
                    self.restartProcess()
                else:
                    # We should exit
                    self.controller.shutdown()

    def __init__(self, controller, camera, healthCheckSleepInterval):
        self.controller = controller
        self.authToken = self.buildAuthToken()
        self.camera = camera

        # Create State object
        self.state = WrapperState.WrapperState()

        # Setup state listeners
        self.controller.state.bind_to(self.stateHandler)
        self.state.bind_to(self.stateHandler)

        # Setup Health Checker
        self.healthChecker = HealthChecker.HealthChecker(self, healthCheckSleepInterval)

