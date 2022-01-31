# Changelog
## [2.2] 2022-01-31
### Added
- Configuration option to enable audio support. By default audio support will be disabled since it requires significant CPU usage. To enable audio support set the `enableAudio` to true on the respective camera configuration.

## [2.1] 2022-01-18
### Fixed
- Error if a toggle entity isn't provided.

## [2.0] 2022-01-17
This release includes a re-write of the application. As such I recommend creating a backup of the add-on and/or Home Assistant prior to upgrading.

This update includes audio support. Please see the readme.md file for details on how to get the stream setup in Home Assistant with audio.

As audio is now supported the process requires more processing than it did before. Please note that this version will use more CPU.

### Added
- Audio support.
- Optional "loglevel" configuration setting.

## [1.8] 2022-01-06
### Added
- Support for video filters (for example video rotation)

## [1.7] 2021-04-03
### Removed
- Noisy Ffmpeg command logging


