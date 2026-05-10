# ==========================================
# STAGE 1: Build environment
# ==========================================
# Use the official .NET 10 SDK image to build and publish the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first to cache NuGet dependencies
# Replace 'BhHybridScraper.csproj' with your actual project file name if it differs
COPY ["src/BhHybridScraper.Core/BhHybridScraper.Core.csproj", "./"]
RUN dotnet restore "./BhHybridScraper.Core.csproj"

# Copy the rest of the application code
COPY . .

# Build and publish the application in Release mode
RUN dotnet publish "src/BhHybridScraper.Core/BhHybridScraper.Core.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# STAGE 2: Runtime environment
# ==========================================
# Use the lightweight .NET 10 runtime image for the final container
# We don't need Playwright browsers here because we connect to the Host's Chrome via CDP!
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

# Copy the published files from the build stage
COPY --from=build /app/publish .

# Create the output directory inside the container
# This is where our C# app will save the JSON files before they are mapped to the host
RUN mkdir -p /app/out

# Set an environment variable so our C# code knows it's running inside Docker.
# This tells the script to use 'http://host.docker.internal:9222' instead of localhost.
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Define the entry point for the container. 
# You can override the default argument ("reviews") when running the container.
ENTRYPOINT ["dotnet", "BhHybridScraper.Core.dll"]
