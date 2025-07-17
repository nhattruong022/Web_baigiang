# Use the official .NET 8 SDK image as the base image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["Lecture_web.csproj", "./"]
RUN dotnet restore "Lecture_web.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet build "Lecture_web.csproj" -c Release -o /app/build

# Publish the application
RUN dotnet publish "Lecture_web.csproj" -c Release -o /app/publish

# Build the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Install SQL Server tools for health checks
RUN apt-get update && apt-get install -y curl gnupg2
RUN curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add -
RUN curl https://packages.microsoft.com/config/ubuntu/20.04/prod.list > /etc/apt/sources.list.d/mssql-release.list
RUN apt-get update && ACCEPT_EULA=Y apt-get install -y mssql-tools unixodbc-dev

# Set the working directory
WORKDIR /app

# Copy the published application
COPY --from=build /app/publish .

# Create directory for images
RUN mkdir -p /app/wwwroot/images

# Expose port 8080
EXPOSE 8080

# Set the entry point
ENTRYPOINT ["dotnet", "Lecture_web.dll"] 