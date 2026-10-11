# Max.Bot — C# библиотека для Max Messenger Bot API

[![.NET](https://img.shields.io/badge/.NET-10%20%7C%209%20%7C%208-purple.svg)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/vpre/MaxMessenger.Bot)](https://www.nuget.org/packages/MaxMessenger.Bot)
[![Build](https://img.shields.io/github/actions/workflow/status/NanoAgents/MaxBotNet/ci.yml?label=CI)](https://github.com/NanoAgents/MaxBotNet/actions)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

Полнофункциональная библиотека для работы с [Max Messenger Bot API](https://dev.max.ru/docs-api) на .NET 10, .NET 9 и .NET 8. Проект фокусируется на типобезопасности, удобстве интеграции и масштабируемости корпоративных ботов.

## 📌 Основные возможности

- Асинхронный `MaxClient` с полной реализацией REST-эндпоинтов Max Messenger.
- Модульные API-клиенты (`BotApi`, `MessagesApi`, `ChatsApi`, `UsersApi`, `FilesApi`, `SubscriptionsApi`) и строгие модели данных.
- Надёжный HTTP-стек c retry/backoff, логированием, обработкой rate-limit и ошибок сети.
- Два механизма получения обновлений: long polling и безопасный webhook конвейер.
- Полная XML-документация всех публичных типов + автотесты, проверяющие наличие комментариев.

## ⚡ Быстрый старт

```powershell
dotnet add package MaxMessenger.Bot --prerelease
```

```csharp
using Max.Bot;
using Max.Bot.Configuration;
using Max.Bot.Polling;
using Max.Bot.Types.Enums;

using var client = new MaxClient(new MaxBotOptions
{
    Token = Environment.GetEnvironmentVariable("MAX_BOT_TOKEN")!
});

var me = await client.Bot.GetMeAsync();
Console.WriteLine($"Hello from {me.Username}");

var handler = new DelegatingUpdateHandler(
    onMessage: async (context, ct) =>
    {
        var update = context.Update;
        if (update.Type != UpdateType.MessageCreated || update.ChatId is not { } chatId)
        {
            return;
        }

        // update.ChatType: Dialog, Chat или Channel (берётся из message.recipient)
        await context.Api.Messages.SendMessageAsync(chatId, $"Echo: {update.Message?.Text}", ct);
    });

await client.StartPollingAsync(handler);
Console.ReadLine();
await client.StopPollingAsync();
```

✅ Полноценные примеры (echo-бот, команды, кнопки, файлы, контакты) живут в `examples/Max.Bot.Examples` и доступны как `RunAsync()` сценарии.

## 📎 Файлы и вложения

```csharp
using Max.Bot.Types.Requests;

// Картинка по URL — MAX скачает её сам
await client.Messages.SendMessageWithAttachmentAsync(
    AttachmentRequest.ImageFromUrl("https://example.com/cat.jpg"), chatId: chatId, text: "Котик");

// Загрузка файла: UploadAsync сам получит upload URL и токен (для image — из photos, для video/audio — из POST /uploads)
await using var stream = File.OpenRead("report.pdf");
var upload = await client.Files.UploadAsync(UploadType.File, stream, "report.pdf");
await client.Messages.SendMessageWithAttachmentAsync(AttachmentRequest.FromUpload(UploadType.File, upload), chatId: chatId);
```

Также доступны `AttachmentRequest.Image(token)`, `Video`, `Audio`, `File`, `Sticker`, `Contact`, `Location`, `Share` и `Keyboard`.
Если MAX ещё обрабатывает только что загруженный файл, API отвечает `attachment.not.ready`: отправка/редактирование сообщения
повторяются автоматически, а при исчерпании попыток выбрасывается `MaxAttachmentNotReadyException`.

## 🔘 Callback-кнопки

```csharp
var callback = update.Callback!;
await client.Messages.AnswerCallbackQueryAsync(
    callback.CallbackId!,
    new AnswerCallbackQueryRequest { Notification = "Принято!" });
```

## 🌐 Webhook

Библиотека больше не добавляет эндпоинт в приложение автоматически. Подключите его явно:

```csharp
builder.Services.AddSingleton(new MaxBotOptions
{
    Token = builder.Configuration["Max:Token"]!,
    Webhook = { Endpoint = "https://bot.example.com/api/max/webhook", SecretToken = builder.Configuration["Max:WebhookSecret"] }
});
builder.Services.AddSingleton(sp => new MaxClient(sp.GetRequiredService<MaxBotOptions>()));
builder.Services.AddSingleton<IUpdatePipeline>(sp => sp.GetRequiredService<MaxClient>());
builder.Services.AddSingleton<IUpdateHandler, MyUpdateHandler>();

var app = builder.Build();
app.MapMaxWebhook(); // POST /api/max/webhook, проверяет заголовок X-Max-Bot-Api-Secret

// URL и секрет берутся из MaxBotOptions.Webhook, если не переданы явно
await app.Services.GetRequiredService<MaxClient>().ConfigureWebhookAsync();
```

Для MVC-проектов можно унаследоваться от абстрактного `WebhookController` и пометить наследника `[Route(...)]`.

## 🧠 Архитектурные ориентиры

- [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) — структура клиента, подход к документации и примерам.
- [VkNet](https://github.com/vknet/vk) — богатые модели, расширяемые API и контроль ошибок.

Полный roadmap находится в [`DEVELOPMENT_PLAN.md`](DEVELOPMENT_PLAN.md).

## 🧪 Тесты и качество

- `dotnet build -warnaserror` — проверяет, что все публичные API имеют XML-доки.
- `dotnet test` — 690+ модульных тестов, включая регрессионные тесты по issue и проверки сериализации на реальных ответах MAX API.
- GitHub Actions workflow `.github/workflows/ci.yml` (Ubuntu + Windows) гоняет форматирование, `dotnet format analyzers`, покрытие (минимум 60 % линий, целимся в 85 %) и `dotnet pack`, загружая артефакты для ревью.
- Smoke-тесты для примеров используют `LoopbackSampleRuntime` и проходят весь `SampleRegistry`, повторяя подход Telegram.Bot и VkNet к офлайн-интеграциям, чтобы код примеров и документация не расходились.
- Фикстуры API (`tests/Max.Bot.Tests/Integration/Fixtures`) помогают детерминированно воспроизводить ответы `/subscriptions` и `/updates` без реального HTTP.

## 🚀 Release Flow

1. Обновите `CHANGELOG.md`, при необходимости скорректируйте версию в `src/Max.Bot/Max.Bot.csproj`.
2. Выполните локальный прогон `dotnet format --verify-no-changes`, `dotnet format analyzers --verify-no-changes --no-restore` и `dotnet test -c Release /p:CollectCoverage=true`.
3. Создайте тег `vX.Y.Z` (SemVer) и запушьте его.
4. Workflow `.github/workflows/release.yml` повторно выполнит сборку/тесты, упакует с `/p:ContinuousIntegrationBuild=true`, отправит `.nupkg`/`.snupkg` на NuGet через секрет `NUGET_API_KEY` и создаст GitHub Release из `CHANGELOG.md`.
5. После зелёного workflow проверьте запись на NuGet.org и GitHub Release. Подробный чеклист в `RELEASING.md`.

## 📚 Документация

- Автогенерируемый XML-файл: `src/Max.Bot/bin/Debug/<target-framework>/Max.Bot.xml` (включён в NuGet).
- Официальный Max Messenger API: [dev.max.ru/docs-api](https://dev.max.ru/docs-api).
- Другие реализации для сравнения и проверки совместимости:
  - TypeScript — [max-bot-api-client-ts](https://github.com/max-messenger/max-bot-api-client-ts)
  - Go — [max-bot-api-client-go](https://github.com/max-messenger/max-bot-api-client-go)

### 🔐 Аутентификация

Библиотека использует заголовок `Authorization: <token>` для аутентификации, как указано в официальной документации MAX API. Токен передаётся через заголовок, а не через query-параметры (старый способ больше не поддерживается API).

### ✅ Совместимость

Реализация проверена на соответствие официальной документации MAX API и поддерживает совместимость с реализациями на TypeScript и Go. Все модели типов используют snake_case для JSON-полей, соответствующих API, а query-параметры также используют snake_case согласно документации.

## 🧩 Примеры

| Проект | Описание |
| ------ | -------- |
| `EchoBotExample` | Минимальный echo-бот, показывает базовый цикл обработки. |
| `CommandBotExample` | Командный роутер (inspired by Telegram.Bot CommandHandlers). |
| `KeyboardBotExample` | Reply/inline клавиатуры + работа с callback query. |
| `FileBotExample` | Загрузка и отправка файлов, пересылка медиа. |
| `ContactBotSample` | Обработка контактных вложений и vCard. |

Каждый пример конфигурируется через переменные окружения (`MAX_BOT_TOKEN`, `MAX_WEBHOOK_URL`, и т.д.) и не хранит секреты в исходниках.

## 📦 Установка

1. Установите .NET 10 SDK или SDK целевого TFM (`net10.0`, `net9.0`, `net8.0`).
2. В проекте выполните:
   ```powershell
   dotnet add package MaxMessenger.Bot --prerelease
   ```
3. Создайте `MaxClient` вручную или зарегистрируйте через DI (например, в ASP.NET Core).
4. Токен и webhook URL держите в Secret Manager/Key Vault. Добавьте `.env`/секретные файлы в `.gitignore`.

## 🧭 Дорожная карта

1. ✅ Подготовка инфраструктуры  
2. ✅ Базовый HTTP клиент  
3. ✅ Базовые модели данных  
4. ✅ Core API методы  
5. ✅ Расширенные модели  
6. ✅ Полный API Max  
7. ✅ Обработка событий (polling + webhook)  
8. ✅ Документация, примеры, README  
9. ✅ CI/CD и публикация
10. ⏳ Доработки по отзывам

## 🤝 Как внести вклад

1. Форкните репозиторий и создайте ветку (`feature/awesome-bot`).
2. Соберите и протестируйте (`dotnet build -warnaserror`, `dotnet test`).
3. Обновите `CHANGELOG.md` + соответствующие разделы документации/примеров.
4. Откройте PR, указав связанные задачи и изменения API.

## 📝 Лицензия

Apache License 2.0 — см. [LICENSE](LICENSE).

---

**Версия:** 0.7.0-alpha  
**Статус:** Активная разработка  
**Контакты:** issues/PR в репозитории