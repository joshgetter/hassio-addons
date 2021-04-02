import subprocess
import base64
import asyncio
import time
import HealthChecker

class FfmpegWrapper:
    def startProcess(self):
        print("[Wrapper] Starting Ffmpeg.")
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
            'h264'
        ] + self.buildInput() + self.buildOutput()

        # TESTING
        print(startCommand)
        
        self.ffmpegProcess = subprocess.Popen(startCommand)
        self.controller.state.isRunning = True

    def stopProcess(self):
        # TODO - Perhaps killall ffmpeg would be better?
        print("[Wrapper] Killing Ffmpeg.")
        self.ffmpegProcess.kill()
        self.controller.state.isRunning = False

    def restartProcess(self):
        print("[Wrapper] Restarting Ffmpeg.")
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

    def buildInput(self):
        inputs = []
        for camera in self.controller.config["cameras"]:
            cameraInput = [
                '-i',
                f'https://{camera["cameraip"]}:19443/https/stream/mixed?video=h264&audio=g711&resolution=hd'
            ]
            inputs.extend(cameraInput)
        return inputs

    def buildOutput(self):
        outputs = []
        for index, camera in enumerate(self.controller.config["cameras"]):
            cameraOutput = [
                '-map',
                str(index),
                '-vcodec',
                'copy',
                '-preset',
                'veryfast',
                '-f',
                'flv',
                f'rtmp://localhost/live/{camera["cameraname"]}',
                '-map',
                str(index),
                '-r',
                '1/5',
                '-update',
                '1',
                '-y',
                f'/tmp/streaming/thumbnails/{camera["cameraname"]}.jpg'
            ]
            outputs.extend(cameraOutput)
        return outputs

    def __init__(self, controller, healthCheckSleepInterval):
        self.controller = controller
        self.authToken = self.buildAuthToken()
        
        # Setup Health Checker
        self.healthChecker = HealthChecker.HealthChecker(controller, healthCheckSleepInterval)

