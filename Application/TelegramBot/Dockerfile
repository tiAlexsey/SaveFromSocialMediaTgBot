FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# Устанавливаем полноценный браузер для ARM64: пакет Chromium в Ubuntu — только Snap-обёртка.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates wget \
    && wget -q https://dl.google.com/linux/direct/google-chrome-stable_current_arm64.deb -O /tmp/google-chrome.deb \
    && apt-get install -y --no-install-recommends /tmp/google-chrome.deb \
    && rm -f /tmp/google-chrome.deb \
    && rm -rf /var/lib/apt/lists/*

USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Application/TelegramBot/TelegramBot.csproj", "Application/TelegramBot/"]
COPY ["Services/Services.csproj", "Services/"]
COPY ["Abstract/Abstract.csproj", "Abstract/"]
RUN dotnet restore "Application/TelegramBot/TelegramBot.csproj"
COPY . .
WORKDIR "/src/Application/TelegramBot"
RUN dotnet build "./TelegramBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TelegramBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TelegramBot.dll"]
