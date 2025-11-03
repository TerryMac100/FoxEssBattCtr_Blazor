# FoxESS battery control Blazor Application

<p>This application is built using Blazor Server, NetDaemon and SQLite for data storage. 
It provides a user-friendly interface for managing and monitoring FoxESS battery systems.</p>

## Features
	- Gives easy access to FoxESS battery control settings in 30 minutes increments
	- Works directly with the Fox Cloud API so no additional hardware required
	- Can be deployed and run as an Add On to Home Assistant
	- Can easily be configured link with Home Assistant so it can modify schedules
	- Designed to make minimal calls to the Fox Cloud API

## Prerequisites
	Visual Studio 2022 or later with .NET 9.0 SDK or later installed.

## Build and Deployment
To build and deploy the application, follow these steps:

1. **Clone the Repository**: Start by cloning the repository to your local machine.

2. **Create an appsettings.json** file in the root directory of the project from the sample json file and set the following items:
	- HomeAssistant Host I/P address
	- HomeAssistant Long-Lived Access Token
	- FoxESS ApiKey - Get this from your FoxESS Web login under User Profile -> API Key 
	- FoxESS DeviceSN - Get this from your FoxESS Web login under Device -> Inverters 
