# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy the main project and its dependencies logically based on solution structure
COPY ["Tabatine.Worker/Tabatine.Worker.csproj", "Tabatine.Worker/"]
COPY ["Tabatine.Infrastructure/Tabatine.Infrastructure.csproj", "Tabatine.Infrastructure/"]
COPY ["Tabatine.Core/Tabatine.Core.csproj", "Tabatine.Core/"]
COPY ["Tabatine.Omie.Client/Tabatine.Omie.Client.csproj", "Tabatine.Omie.Client/"]

# Restore dependencies
RUN dotnet restore "./Tabatine.Worker/Tabatine.Worker.csproj"

# Copy the rest of the source code
COPY . .

WORKDIR "/src/Tabatine.Worker"
RUN dotnet build "./Tabatine.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Tabatine.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tabatine.Worker.dll"]
