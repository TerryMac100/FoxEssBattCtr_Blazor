# FoxESS battery control Blazor Application

<p>This application is built using Blazor Server, NetDaemon and SQLite for data storage. 
It provides a user-friendly interface for managing the FoxESS battery systems.</p>

<p>The application runs as an add on to Home Assistant can be configured modify preset battery schedule 
in response to boolean flags in Home Assistant e.g. the cheap rate or saving session flag</p>

## Features
	- Gives easy access to FoxESS battery control settings in 30 minutes increments
	- Works directly with the Fox Cloud API so no additional hardware required
	- Can be deployed and run as an Add On to Home Assistant
	- Can easily be configured link with Home Assistant so it can modify schedules
	- Designed to make minimal calls to the Fox Cloud API

## Prerequisites
	Visual Studio 2022 or later with .NET 9.0 SDK or later installed.

## Build and Deployment
TBuild and deploy the application, follow these steps:

1. **Clone the Repository**: Start by cloning the repository to your local machine.

2. **Create an appsettings.json** 
Create a appsettings.json based on the appsettings-sample.json file in the root directory of the project and set up the following items:
	- HomeAssistant Host I/P address
	- HomeAssistant Long-Lived Access Token
	- FoxESS ApiKey - Get this from your FoxESS Web login under User Profile -> API Key 
	- FoxESS DeviceSN - Get this from your FoxESS Web login under Device -> Inverters

3. **Build/Run/Debug the application**
Check that the application connects to the Home Assistant instance, if successful two flags will be created on the Helpers group.
While running the application in debug calls to the Fox Cloud API will be disabled.

4. **Deploy to Home Assistant**
Update the Publish Profile to copy the release build to a suitable named folder under "addons" on the target machine this will required Samba Share to be installed if you don't aready have it.


