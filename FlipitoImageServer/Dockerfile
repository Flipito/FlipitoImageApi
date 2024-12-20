# Stage 1: Base Image for Running the Application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 8081

# Stage 2: Build the Application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only the project file and restore dependencies
COPY FlipitoImageServer.csproj ./ 
RUN dotnet restore "./FlipitoImageServer.csproj"

# Copy the rest of the application files
COPY . ./

# Build the project
RUN dotnet build "./FlipitoImageServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish the Application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FlipitoImageServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final Image for Running the Application
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set the application to listen on port 80
ENV ASPNETCORE_URLS=http://+:80

# Entry point for the application
ENTRYPOINT ["dotnet", "FlipitoImageServer.dll"]
