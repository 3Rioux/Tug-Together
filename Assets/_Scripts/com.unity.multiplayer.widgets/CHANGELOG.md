# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2024-12-16

### General
- Adjusted Layout Group on Text Chat Widget so it can also be manipulated on the root GameObject.

## [1.0.0] - 2024-09-26

### General
- Importing the Widgets Quickstart UI from the Multiplayer Center will populate the `MaxPlayers` and `ConnectionType` fields of the newly created `WidgetConfiguration` based on the choices made in the Multiplayer Center.
- Widgets Quickstart Section now offers an option to import the Widgets Quickstart UI directly into a Multiplayer Setup created via Multiplayer Center.
- Add an option to leave a Session to the Widgets Quickstart UI.
- The Widgets Quickstart UI is now saved as a Scene into the Project.

### Vivox
- Fixed an issue where text chat was unusable after leaving and joining a new session.

## [1.0.0-pre.2] - 2024-08-01

### Vivox
- Fixed an exception when joining the same Session again.

## [1.0.0-pre.1] - 2024-07-31

### General
- Minor UX improvements.

## [0.5.0] - 2024-07-26

### General
- Support custom intitialization of Services via the ProjectSettings (`Widgets/Use Custom Service Initialization`).
- Renamed `ExitSession` to `LeaveSession`.

### Session
- Added `publish ip` and `listen ip` to the `WidgetConfiguration` to mirror the functionality of the `Multiplayer Services Package`.

### Vivox
- Fixed a bug where the voice indicator in a Session Player List Item was always shown, even when voice chat was disabled .

## [0.4.0] - 2024-07-17

### General
- Fix namespace clash in `WidgetConfigurationEditor`.

## [0.3.0] - 2024-07-15

### General
- Support for Multiplayer Service Package 0.6.0.
- Widgets are center aligned by default.

### Vivox
- Added `Text Chat` Widget.
- Added info box to install Vivox in WidgetConfigurationManager if not installed to access all features.

## [0.2.0] - 2024-07-03

### General
- Quickstart sample now opens in a new Scene instead of adding to the active Scene.

### Session
- Added `Session Player List` Widget.
- Added `Exit Session` Widget.
- Added `Quick Join Session` Widget.
- Added new `ConnectionType` value `None` to the `WidgetConfiguration` asset for non-netcode use-cases.
- Added new `ConnectionType` value `DistributedAuthority` to the `WidgetConfiguration` asset for the new Distributed Authority feature (only available in NGO 2.0 or higher).

### Vivox
- Voice chat will automatically be joined when enabled via the `WidgetConfiguration`.
- Added `Select Input Audio Device` and `Select Output Audio Device` Widget.
- Added setting `Players Can Be Muted` to the `Session Player List` Widget.
- Added voice indicator icon to the `Session Player List` Widget.

## [0.1.1] - 2024-04-17

### General
- Fixed an integration issue with Multiplayer Center.

## [0.1.0] - 2024-03-13

### This is the first release of *Unity Package com.unity.multiplayer.widgets*.