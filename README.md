# FoxESS battery control Blazor Application

This application is built using Blazor Server, NetDaemon and SQLite for data storage. 
It provides a user-friendly interface for managing the FoxESS battery settings.

The application runs as an add on to Home Assistant can be configured modify preset battery schedule 
in response to boolean flags in Home Assistant e.g. the cheap rate or saving session flag

## Features
The application:
	- Gives easy access to FoxESS battery control settings in 30 minutes increments
	- Works directly with the Fox Cloud API so no additional hardware required
	- Is deployed and run as an Add On to Home Assistant
	- Can easily be configured link with Home Assistant so it can modify schedules
	- Designed to make minimal calls to the Fox Cloud API

## Build and Deployment
Build/Debug and deploy the application using Visual Studio or VS code using the following steps:

1. **Clone the Repository**: Start by cloning the repository to your local machine.

2. **Create an appsettings.json** 
Create a appsettings.json based on the appsettings-sample.json file in the root directory of the project and set up the following items:
	- HomeAssistant Host I/P address
	- HomeAssistant Long-Lived Access Token
	- FoxESS ApiKey  
	- FoxESS DeviceSN

A guide to creating a HomeAssistant Long-Lived Access Token can be found at the following link:
https://community.home-assistant.io/t/how-to-get-long-lived-access-token/162159

To find your FoxESS ApiKey Log in to your account on the FoxEss Cloud website (https://www.foxesscloud.com) and navigate to User Profile -> API Key to generate or view the key.

3. **Build/Run/Debug the application**

Build the application and run it in debug from the development machine and check that the application connects to the Home Assistant instance, if successful two flags will be created on the Helpers group.

The two flags are:
	- input_boolean.netdaemon_fox_batt_control_api_enable
	- input_boolean.netdaemon_blazor_batt_control_fox_ess_fox_battery_control

The api_enable flag can be used to enable/disable calls to the Fox Cloud API, this is useful for debugging the application without making multiple calls to the Fox Cloud API. However when the application is run in debug calls will be disabled by default.

The battery_control flag can be used to enable/disable the battery control functionality of the application by stopping the main state machine running

4. **Deploy to Home Assistant**
Update the Publish Profile to copy the release build to a suitable named folder under "addons" on the target machine. The simplest way to do this is to install the Samba Share add on if you don't ready have it.

A guide to deploying your own Home Assistant add-on can be found at the following link: https://developers.home-assistant.io/docs/add-ons/tutorial/


