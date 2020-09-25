# Kasa-Camera

## Introduction
This add-on allows you to access the stream of certain Kasa cameras that don't explicitly expose or support streaming on their own.

## Supported Hardware
This should at least work with the KC100 and KC105 cameras, however I have only personally tested this on a KC105 camera.

## Configuration
### General
When you initially set up a Kasa camera, the Kasa app will have you create an account. You will need to provide these credentials to allow the add-on to access the stream.

The following is a description of each configuration item.

`kasausername` - **Required**. The username for your Kasa account. This should be an email address.

`kasapassword` - **Required**. The password for your Kasa account.

`cameraip` - **Required**. The IP address of the Kasa camera. _Note:_ Ideally the camera will have a reserved / static IP so that this doesn't need to be updated.

`cameraname` - Default: "kasacam". The name of the camera. This will impact the URL of the output streams.

`retrylimit` - Default: 5. The maximum number of consecutive attempts to restart a failed stream. A single success will reset this counter. A value of `-1` will disable any limit on retry attempts.

`toggleentity` - Optional. You can provide an entity ID of a Home Assistant toggle. If provided the add-on will observe the toggle and enable / disable camera streaming based on it's value.

Example configuration:
``` yaml
kasausername: user@example.com
kasapassword: password1234
cameraip: 192.168.1.2
cameraname: livingroom
retrylimit: 5
toggleentity: input_boolean.kasa_camera_enabled
```
### Network
The add-on exposes two ports. One for RTMP video output, and one for HTTP output.  By default they will be mapped in the following way, so that they don't conflict with the typical HTTP and RTMP ports on your host system.  Feel free to change these as needed.

```
1935/tcp: 43331 (exposes RTMP on port 43331 on the host)
80/tcp: 43330 (exposes HTTP on port 43330 on the host)
```
## Output
The add-on will expose the camera video as a stream in the following formats:
* HLS - `http://<HOST IP>:43330/hls/<CAMERA NAME>.m3u8`
* RTMP - `rtmp://<HOST IP>:43331/live/<CAMERA NAME>`

Additonally the add-on will intermittently generate thumbnail images from the camera. The latest thumbnail image can be accessed at:
* Thumbnail - `http://<HOST IP>:43330/thumbnails/<CAMERA NAME>.jpg`

## Adding Camera to Home Assistant
You can add the camera to Home Assistant using the above output streams.  This demonstrates how you'd add a camera based on the example configuration above:

``` yaml
camera:
  - platform: generic
    name: "Living Room Camera"
    still_image_url: "http://192.168.1.2:43330/thumbnails/livingroom.jpg"
    stream_source: "http://192.168.1.2:43330/hls/livingroom.m3u8"
```