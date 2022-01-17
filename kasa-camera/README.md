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

`cameras` - Array of camera objects. Each has the following properties:

* `cameraip` - **Required**. The IP address of the Kasa camera. _Note:_ Ideally the camera will have a reserved / static IP so that this doesn't need to be updated.

* `cameraname` - **Required**. The name of the camera. This will impact the URL of the output streams.

* `videofilter` - Optional. You can provide a video filter to ffmpeg. This can be useful if you want to rotate the video for example. Provide any string that can be used as an argument to the `vf` parameter in ffmpeg. In the configuration example below the supplied argument is `transform=clock`. This would rotate the video clockwise 90 degrees. Additional FFmpeg filter [documentation](https://trac.ffmpeg.org/wiki/FilteringGuide).

`retrylimit` - Default: 5. The maximum number of consecutive attempts to restart a failed stream. A single success will reset this counter. A value of `-1` will disable any limit on retry attempts.

`retrysleep` - Default: 30. The amount of time (in seconds) to wait before attempting the next retry. A value of 0 will result in no waiting between retries.  Waiting between retries can be useful, since it allows some time for the camera to recover from errors (usually I/O errors) before a new request comes in.

`toggleentity` - Optional. You can provide an entity ID of a Home Assistant toggle. If provided the add-on will observe the toggle and enable / disable camera streaming based on it's value.

`loglevel` - Default: 2. Adjust the application log level. Enter a value between 0 (most verbose) - 6 (no logging).

Example configuration:
``` yaml
kasausername: user@example.com
kasapassword: password1234
cameras:
  - cameraname: livingroom
    cameraip: 192.168.1.3
  - cameraname: kitchen
    cameraip: 192.168.1.2
    videofilter: transform=clock
retrylimit: 5
retrysleep: 30
toggleentity: input_boolean.kasa_camera_enabled
loglevel: 2
```
### Network
The add-on exposes two ports. One for RTMP video output, and one for HTTP output.  By default they will be mapped in the following way, so that they don't conflict with the typical HTTP and RTMP ports on your host system.  Feel free to change these as needed.

```
1935/tcp: 43331 (exposes RTMP on port 43331 on the host)
80/tcp: 43330 (exposes HTTP on port 43330 on the host)
```
## Output
The add-on will expose the camera video as a stream in the following formats:
* HLS - `http://<HA IP>:43330/hls/<CAMERA NAME>.m3u8`
* RTMP - `rtmp://<HA IP>:43331/live/<CAMERA NAME>`

Additonally the add-on will intermittently generate thumbnail images from the camera. The latest thumbnail image can be accessed at:
* Thumbnail - `http://<HA IP>:43330/thumbnails/<CAMERA NAME>.jpg`

Note that the output will be at `HA IP` which is the IP address of your Home Assistant instance **not** the IP of the camera.

## Adding Camera to Home Assistant
You can add the camera(s) to Home Assistant using the above output streams.  This demonstrates how you'd add a camera based on the example configuration above.


**Note** - Home Assistant doesn't seem to support audio with the HLS stream produced by this add-on. If you need audio use the RTMP stream produced by this add-on instead (see example below).

``` yaml
camera:
  - platform: generic
    name: "Living Room Camera"
    still_image_url: "http://<HA IP>:43330/thumbnails/livingroom.jpg"
    stream_source: "rtmp://<HA IP>:43331/live/livingroom"
  - platform: generic
    name: "Kitchen Camera"
    still_image_url: "http://<HA IP>:43330/thumbnails/kitchen.jpg"
    stream_source: "rtmp://<HA IP>:43331/live/kitchen"
```