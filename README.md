# SaveFromSocialMediaTgBot

Telegram-бот для скачивания видео из социальных сетей. Отправьте боту ссылку — он скачает и отправит видео прямо в чат.

## Поддерживаемые платформы

- Instagram (Reels, посты)
- TikTok
- Twitter / X
- Youtube

## Стек технологий

- .NET 9 (Worker Service)
- Telegram.Bot
- Redis — кэширование
- Chromium (headless) — скрапинг контента
- Docker / Docker Compose
- Graylog + OpenSearch + MongoDB — централизованное логирование (Serilog → GELF)

## Быстрый старт (без клона репозитория)

### 1) Создайте файл `.env`

```env
TOKEN=<Telegram Bot Token>
TWITTER_TOKEN=<Twitter Bearer Token>
RETRY_COUNT=3
INST_LOGIN=<Instagram login>
INST_PASSWORD=<Instagram password>
INST_COOKIE_SESSION_ID=<Instagram session cookie>
REDIS_CONNECTION_STRING=redis:6379,defaultDatabase=0,abortConnect=false,connectTimeout=20000,syncTimeout=20000
GRAYLOG_HOST=
```

### 2) Создайте файл `docker-compose.yml`

```yaml
version: "3.9"

services:
  savefromsocialmediatgbot:
    image: ghcr.io/tialexsey/save-video-tg-bot:latest
    depends_on:
      redis:
        condition: service_healthy
    env_file:
      - .env
    networks:
      - bot_network
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    command: ["redis-server", "--appendonly", "yes"]
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 3s
      retries: 20
    networks:
      - bot_network
    restart: unless-stopped

volumes:
  redis_data:

networks:
  bot_network:
    driver: bridge
```

## Архитектура

```
┌────────────┐     ┌──────────────────────┐     ┌───────────┐
│  Telegram  │────▶│  TelegramBotWorker   │────▶│  Scrapers │
│  (пользов.)│◀────│  TelegramBotService  │     │  (IG/TT/  │
└────────────┘     │  ScraperService      │     │   YT/TW)  │
                   └──────────┬───────────┘     └───────────┘
                              │
                   ┌──────────▼───────────┐
                   │   Redis (кэш)        │
                   └──────────────────────┘
                   ┌──────────────────────┐
                   │   Graylog (логи)     │
                   │   (опционально)      │
                   └──────────────────────┘
```

## Структура проекта

```
SaveFromSocialMediaTgBot/
├── Data/
│   ├── Constants/       # Константы (команды, сообщения, паттерны)
│   └── Models/          # Модели данных
├── Exceptions/          # Кастомные исключения
├── Extensions/          # Extension-методы
├── Interfaces/          # Интерфейсы сервисов
├── Logging/             # Контекст запроса для Serilog
├── Services/
│   ├── Scraper/         # Скраперы для каждой платформы
│   ├── CacheService.cs  # Работа с Redis
│   ├── ScraperService.cs
│   └── TelegramBotService.cs
├── TelegramBotWorker.cs # Background-сервис бота
├── Program.cs           # Точка входа
└── Dockerfile
```

## Лицензия

Проект предназначен для личного использования.
