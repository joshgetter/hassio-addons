import asyncio
import asyncws
import requests
import json

class HAListener:
    HA_WEBSOCKET_URL = "http://supervisor/core/websocket"

    async def websocketHandler(self, websocket):
        while True:
            message = await websocket.recv()
            if message is None:
                break
            parsedMessage = json.loads(message)
            if(parsedMessage["type"] == "event"):
                if(parsedMessage["event"]["data"]["entity_id"] == self.controller.config["toggleentity"]):
                    toggleState = parsedMessage["event"]["data"]["new_state"]["state"]
                    if(toggleState == "off"):
                        print("Toggle is off")
                        self.controller.state.isCameraEnabled = False
                    else:
                        print("Toggle is on")
                        self.controller.state.isCameraEnabled = True

    async def start(self):
        print("Starting HA Listener")
        
        print("Creating websocket")
        # Create WebSocket
        websocket = await asyncws.connect(self.HA_WEBSOCKET_URL)
        print("WebSocket created")

        # Auth
        print("Sending auth request")
        await websocket.send(json.dumps({'type': 'auth', 'access_token': self.controller.haToken}))
        print("Auth complete")

        # Register listener
        print("Registering listener")
        await websocket.send(json.dumps({'id': 1, 'type': 'subscribe_events', 'event_type': 'state_changed'}))
        print("Listener registered")

        # Start looping
        await self.websocketHandler(websocket)

    def __init__(self, controller):
        self.controller = controller