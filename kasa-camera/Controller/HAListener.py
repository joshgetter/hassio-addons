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
                    print(f"[HAListener] toggle state: {toggleState}.")
                    if(toggleState == "off"):
                        self.controller.state.isCameraEnabled = False
                    else:
                        self.controller.state.isCameraEnabled = True

    async def start(self):
        if self.controller.config.get("toggleentity") is None:
            # There's no need to start the HA Listener if there's not toggle entity to listen to.
            return
        
        print("[HAListener] Starting HA Listener.")
        
        # Create WebSocket
        websocket = await asyncws.connect(self.HA_WEBSOCKET_URL)

        # Auth
        await websocket.send(json.dumps({'type': 'auth', 'access_token': self.controller.haToken}))

        # Register listener
        await websocket.send(json.dumps({'id': 1, 'type': 'subscribe_events', 'event_type': 'state_changed'}))

        # Start looping
        print("[HAListener] Now listening on HA WebSocket.")
        await self.websocketHandler(websocket)

    def __init__(self, controller):
        self.controller = controller