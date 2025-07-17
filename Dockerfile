# Base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Lecture_web/Lecture_web.csproj", "Lecture_web/"]
RUN dotnet restore "Lecture_web/Lecture_web.csproj"
COPY . .
WORKDIR "/src/Lecture_web"
RUN dotnet publish "Lecture_web.csproj" -c Release -o /app/publish

# Final image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Lecture_web.dll"]
