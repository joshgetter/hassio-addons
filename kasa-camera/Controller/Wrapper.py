import subprocess
import base64
import asyncio
import HealthChecker

class FfmpegWrapper:
    def startProcess(self):
        print("Starting Ffmpeg")
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
            f'https://{self.controller.config["cameraip"]}:19443/https/stream/mixed?video=h264&audio=g711&resolution=hd',
            '-map',
            '0',
            '-vcodec',
            'copy',
            '-preset',
            'veryfast',
            '-f',
            'flv',
            f'rtmp://localhost/live/{self.controller.config["cameraname"]}',
            '-map',
            '0',
            '-r',
            '1/5',
            '-update',
            '1',
            '-y',
            f'/tmp/streaming/thumbnails/{self.controller.config["cameraname"]}.jpg'
        ]
        print("Start Command:")
        print(startCommand)
        self.ffmpegProcess = subprocess.Popen(startCommand)
        self.controller.state.isRunning = True
        self.controller.state.isErrored = False
        asyncio.ensure_future(self.healthChecker.start())
        print("Ffmpeg started")

    def stopProcess(self):
        # TODO - Perhaps killall ffmpeg would be better?
        print("Killing Ffmpeg")
        self.healthChecker.stop()
        self.ffmpegProcess.kill()
        self.controller.state.isRunning = False
        self.controller.state.isErrored = False

    def restartProcess(self):
        print("Restarting Ffmpeg")
        self.stopProcess()
        self.startProcess()

    def buildAuthToken(self):
        encodedPassword = self.encode(self.controller.config['kasapassword'])
        encodedPair = self.encode(f"{self.controller.config['kasausername']}:{encodedPassword}")
        return encodedPair

    def encode(self, input):
        encodedBytes = base64.b64encode(input.encode("utf-8"))
        encodedString = str(encodedBytes, "utf-8")
        return encodedString

    def __init__(self, controller, healthCheckInterval):
        self.controller = controller
        self.authToken = self.buildAuthToken()
        
        # Setup health checker
        self.healthChecker = HealthChecker.HealthChecker(controller, healthCheckInterval)


