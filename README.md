# FoxESS battery control Blazor Application

<p>This application is built using Blazor Server, NetDaemon and SQLite for data storage. 
It provides a user-friendly interface for managing and monitoring FoxESS battery systems.</p>

## Prerequisites
	Visual Studio 2022 or later with .NET 9.0 SDK or later installed.

## Build and Deployment
To build and deploy the application, follow these steps:

1. **Clone the Repository**: Start by cloning the repository to your local machine.

2. **Create an appsettings.json** file in the root directory of the project from the sample json file and set the following items:
	- HomeAssistant Host I/P address
	- HomeAssistant Long-Lived Access Token

3. **Create a FoxBatteryControlSettings.yaml** file from the sample yaml file and set the following items:
	- FoxESS ApiKey - Get this from your FoxESS Web login under User Profile -> API Key 
	- FoxESS DeviceSN - Get this from your FoxESS Web login under Device -> Inverters 
	- OffPeakFlagEntityID - Set this to the Home Assistant entity ID that indicates off-peak status (e.g., a binary sensor or input boolean) 
	- DefaultSchedule - Set this to your preferred default charging schedule, the supplied schedule is created for Octopus Intelligent Go