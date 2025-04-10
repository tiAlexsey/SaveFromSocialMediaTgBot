﻿FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Обновляем список пакетов и устанавливаем Chromium для ARM64
RUN apt-get update && apt-get install -y chromium

# Секретные переменные из GitHub
ENV TOKEN=${TOKEN}
ENV BOT_NAME=${BOT_NAME}
ENV TWITTER_TOKEN=${TWITTER_TOKEN}
ENV RETRY_COUNT=${RETRY_COUNT}
ENV INST_LOGIN=${INST_LOGIN}
ENV INST_PASSWORD=${INST_PASSWORD}
ENV INST_COOKIE_SESSION_ID=${INST_COOKIE_SESSION_ID}

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SaveFromSocialMediaTgBot/SaveFromSocialMediaTgBot.csproj", "SaveFromSocialMediaTgBot/"]
RUN dotnet restore "SaveFromSocialMediaTgBot/SaveFromSocialMediaTgBot.csproj"
COPY . .
WORKDIR "/src/SaveFromSocialMediaTgBot"
RUN dotnet build "SaveFromSocialMediaTgBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SaveFromSocialMediaTgBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SaveFromSocialMediaTgBot.dll"]
