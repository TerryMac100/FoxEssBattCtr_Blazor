FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

# Copy the project file and dependencies
COPY . ./

ENTRYPOINT ["dotnet", "BlazorBattControl.dll"]