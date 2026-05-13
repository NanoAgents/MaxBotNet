# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.6.2-alpha] - 2026-05-14

### Added

- **Chat admin permissions**: Добавлены недостающие значения `ChatAdminPermission` для актуального контракта MAX API.
- **Video metadata**: Добавлены поля модели `Video` для расширенного media-контракта.

### Changed

- **Callback and media contracts**: Обновлены модели callback/message и media/video под актуальные payload-форматы MAX API.
- **Contact attachments**: `ContactAttachment` теперь разбирается из `attachments[].payload.vcf_info`, `max_info` и `hash`.

### Fixed

- **Attachment payload parsing**: Исправлен разбор payload tokens для media attachments.
- **MaxClient disposal contract**: `MaxClient` теперь явно реализует `IDisposable`, чтобы публичный `Dispose()` был частью контракта типа.

## [0.6.1-alpha] - 2026-04-28

### Breaking Changes

- **Chats API membership contract**: `GetChatMembershipAsync()` теперь возвращает `ChatMember` вместо `Chat`, а `GetChatMembersAsync()` использует параметры `marker` / `count` и ответ `ChatMembersResponse`.
- **Edit message request DTOs**: `EditMessageRequest.Attachments` теперь использует `AttachmentRequest[]`, а `Link` — `NewMessageLink`, чтобы соответствовать формату `PUT /messages`.
- **Upload result contract**: `FileUploadResult` приведён к официально подтверждённому token-only формату; неподтверждённые `FileId`, `Photos` и `PhotoSizeToken` удалены.

### Added

- **.NET 10 support**: Библиотека, тесты и examples теперь таргетят `net10.0`, `net9.0` и `net8.0`.
- **Chat members response**: Добавлена модель `ChatMembersResponse` для ответов endpoints участников и администраторов чата.
- **MAX API model fields**: Добавлены актуальные поля в `User`, `ChatMember`, `Contact` и `ContactAttachment`, включая `contact.hash`.
- **Bot started payload**: `Update`, `BotStartedUpdate` и `UpdateJsonConverter` теперь поддерживают `payload` для deep-link запуска бота.
- **Chat admin permissions**: Добавлены недостающие значения `ChatAdminPermission` из актуального MAX API.

### Changed

- **MAX API model audit**: DTO, converters и request/response модели приведены к официальным схемам MAX Bot API там, где они подтверждены документацией.
- **Chats and messages API contracts**: `ChatsApi`, `MessagesApi`, upload flow и attachment handling синхронизированы с актуальными response/request shapes.
- **CI/CD release flow**: GitHub Actions обновлены под multi-target `.NET 10 / 9 / 8`, релизные проверки и автоматическое создание GitHub Release.

### Fixed

- **Contact vCard parsing**: Исправлен парсинг поля `N:` в `ContactHelpers`, которое ошибочно матчило `BEGIN:VCARD`.
- **HTTP errors and logging**: Улучшено поведение `MaxHttpClient` при невалидном error body, retry и диагностическом логировании.
- **Regression tests**: Обновлены тесты сериализации, API responses, samples и networking под актуальные контракты.

## [0.5.6-alpha] - 2026-04-28

### Fixed

- **Attachment send race after upload**: `SendMessageWithAttachmentAsync()` и `SendMessageWithAttachmentsAsync()` теперь автоматически повторяют `POST /messages`, если API временно возвращает `400 Bad Request` сразу после `UploadFileDataAsync()`. Это убирает необходимость в ручном `Thread.Sleep(...)` перед отправкой файла.
- **Regression coverage**: Добавлен unit test, который проверяет успешный повторный вызов после временного `400 Bad Request` при отправке сообщения с вложением.

## [0.5.5-alpha] - 2026-04-15

### Added

- Добавлена поддержка .NET 8.

## [0.5.4-alpha] - 2026-04-10

### Fixed

- **Long polling events not delivered**: Корректно исправлена регрессия в `UpdatePoller`, из-за которой `GET /updates` отправлял заголовок `Authorization` в неверном формате. Polling снова использует `Authorization: <token>` вместо ошибочного `Authorization: Bearer <token>`.
- **Incoming bot events restored**: Восстановлена доставка входящих событий при long polling, включая `message_created` и `message_callback` (текстовые сообщения и нажатия на inline-кнопки).
- **Regression coverage**: Добавлен unit test на формат заголовка авторизации для polling-запросов.

### Notes

- **Supersedes 0.5.3-alpha**: Версия `0.5.3-alpha` была опубликована с неправильного тега и не содержала фактический фикс polling-регрессии. Используйте `0.5.4-alpha`.

## [0.5.3-alpha] - 2026-04-10

### Notes

- **Incorrect release tag**: Версия была опубликована с неправильного коммита и не содержала ожидаемый фикс long polling. Заменена версией `0.5.4-alpha`.

## [0.5.2-alpha] - 2026-04-09

### Breaking Changes

#### Attachment Models — Flat Format
Все attachment-модели переведены на плоский формат (данные прямо на уровне attachment). Это соответствует реальному формату JSON от Max Bot API.

- `PhotoAttachment.Photo` удалено → используй `photoAttachment.FileId`, `photoAttachment.Url`, `photoAttachment.Width`, `photoAttachment.Height` напрямую.
- `VideoAttachment.Video` удалено → используй `videoAttachment.Duration`, `videoAttachment.MimeType`, `videoAttachment.Url` напрямую.
- `AudioAttachment.Audio` удалено → используй `audioAttachment.Duration`, `audioAttachment.MimeType`, `audioAttachment.Url` напрямую.
- `DocumentAttachment.Document` удалено → используй `documentAttachment.FileName`, `documentAttachment.MimeType`, `documentAttachment.Url` напрямую.
- `ContactAttachment.Payload` удалено → используй `contactAttachment.VcfInfo` и `contactAttachment.MaxInfo` напрямую.
- `ImageAttachment` удалён: объединён с `PhotoAttachment` (оба использовали `type="image"`).
- `ContactHelpers.GetPhoneNumber()` и `GetFullName()` теперь принимают `ContactAttachment` вместо `Contact?`.

#### Chats API — ChatMember вместо User
- `GetChatMembersAsync()` и `GetChatAdminsAsync()` теперь возвращают `ChatMember[]` вместо `User[]`. Новый тип `ChatMember` содержит расширенные поля: `is_owner`, `is_admin`, `join_time`, `avatar_url`, `permissions` и др.

#### MaxClient — Dual HttpClient
- `MaxClient` принимает дополнительный параметр `pollingHttpClient` для отдельного клиента long polling.

### Added

- **`ChatMember`**: Новая модель для членов чата (ответ `GET /chats/{chatId}/members`). Содержит `user_id`, `name`, `first_name`, `is_owner`, `is_admin`, `join_time`, `avatar_url`, `full_avatar_url`, `permissions`.
- **`ChatAdminPermission`**: Enum для админ-разрешений в чате.

### Changed

- **`AttachmentJsonConverter`**: Маршрутизация упрощена — теперь по полю `type` (primary), а не по наличию вложенных полей.
- **`MaxClient`**: Два независимых `HttpClient` — API-клиент (30s timeout) для обычных запросов и polling-клиент (`LongPollingTimeout + 10s`) исключительно для `GET /updates`.
- **HttpClient timeout respect**: `MaxClient` больше не перезаписывает `HttpClient.Timeout` пользовательских клиентов.
- **`UpdatePoller`**: `pollClient` опционален — создаётся автоматически с правильным таймаутом, если не передан.
- **Dispose safety**: `MaxClient` и `UpdatePoller` дизпозят только созданные ими HTTP-клиенты. Переданные снаружи клиенты никогда не дизпозятся.
- **`MaxHttpClient`**: Добавлено логгирование на уровне Debug при отключённом Detailed Logging.

### Fixed

- **PhotoAttachment.Photo = null**: Критический баг — десериализация фото-аттачментов всегда возвращала `null`, т.к. API отдаёт данные плоско, а модель ожидала `{"photo": {...}}`.
- **SendMessageAsync HTTP 404**: `SendMessageRequest.Attachments` теперь инициализируется `Array.Empty<AttachmentRequest>()` — сервер требовал присутствия этого поля.
- **User-Agent header**: Добавлен обязательный заголовок `User-Agent: MaxMessenger.Bot/0.5.1-alpha` (требование API).
- **`MessageTests.MessageBody_ShouldDeserialize`**: Обновлён тест на плоский формат.
- **`ContactBotSample`**: Обновлён пример — `.Payload?.VcfInfo` → `.VcfInfo`.

## [0.5.1-alpha] - 2026-03-09

### Changed

- **Chore**: Добавлен `.pi/` в `.gitignore` для исключения файлов Pi Coding Agent из репозитория.

---

## [0.5.0-alpha] - 2026-03-09

### Added

- **`FileUploadResult`**: Новая модель для результата загрузки файла, содержащая `Token`, `FileId`, `PhotoId` и `Photos`.
- **`ContactInfo.PhoneNumber`**: Добавлено свойство для прямого доступа к номеру телефона контакта.
- **`ContactInfo.FullName`**: Добавлено свойство для полного имени контакта.
- **`ContactInfo.Name`**: Добавлено свойство-алиас для совместимости с разными форматами API.
- **`ContactAttachment.PhoneNumber`**: Convenience property для получения телефона из контакта (приоритет: `MaxInfo.PhoneNumber` → парсинг `VcfInfo`).
- **`ContactAttachment.FullName`**: Convenience property для получения имени из контакта (приоритет: `MaxInfo.FullName` → парсинг `VcfInfo`).
- **`ContactHelpers`**: Новый helper-класс с методами для работы с контактами:
  - `ContactHelpers.ParseVcf(string? vcfInfo)` — парсит vCard и извлекает телефон, имя, фамилию.
  - `ContactHelpers.GetPhoneNumber(Contact? contact)` — получает телефон из контакта.
  - `ContactHelpers.GetFullName(Contact? contact)` — получает полное имя из контакта.
- **Пример `ContactBotSample`**: Добавлен пример бота для обработки контактов.
- **`LocationAttachment`**: Поддержка вложений местоположения (широта/долгота).
- **`ImageAttachment`**: Поддержка вложений изображений с множественной отправкой фото.
- **`UpdateTypeJsonConverter`**: Кастомный JSON конвертер для enum `UpdateType`.
- **`UpdateTypeHelper`**: Helper-класс для конвертации `UpdateType` между string и enum.
- **`IMaxHttpClient.Dispose()`**: Реализация `IDisposable` для корректного освобождения ресурсов HTTP клиента.

### Fixed

- **`UploadFileDataAsync` / `UploadFileResumableAsync`**: Теперь возвращают `FileUploadResult` вместо `object`. Это позволяет легко получать токен или ID файла после загрузки без ручного парсинга JSON.
- **Контакты (ContactAttachment)**: Теперь телефон и имя контакта можно получить напрямую через `contactAttachment.PhoneNumber` и `contactAttachment.FullName` без необходимости ручного парсинга vCard.
- Исправлена проблема, при которой данные vCard (особенно номер телефона) были недоступны в удобном виде в обработчике update.
- Исправлена десериализация vCard для сложных случаев (параметры, folded lines, экранирование).
- **`BaseApi.ExecuteRequestAsync<T>()`**: Исправлена обработка ошибок при пустом response body — теперь генерируется `MaxNetworkException` с корректным сообщением.
- **`MaxHttpClient.HandleHttpResponseError()`**: Исправлена логика определения типа исключения по статус коду:
  - `HttpStatusCode.RequestTimeout` (408) теперь корректно выбрасывает `MaxNetworkException`.
  - Server errors (5xx) корректно классифицируются как `MaxNetworkException`.
- **`UpdatePoller`**: Исправлена фильтрация update types — теперь корректно работает с `UpdateTypeHelper.ToStringValues()`.
- **`MaxJsonSerializer`**: Исправлена обработка nullable типов в `ErrorResponse` и `Error` моделях.
- **`SubscriptionsApi`**: Улучшена валидация параметров для `GetUpdatesAsync` и `SetWebhookAsync`.
- **`DeleteWebhook`**: Endpoint теперь принимает `url` только как query parameter (согласно спецификации Max API).

### Changed

- **BREAKING**: `IMaxHttpClient` теперь реализует `IDisposable`. Все кастомные реализации должны добавить метод `Dispose()`.
- **BREAKING**: `MaxHttpClient.SendAsync<TResponse>()` теперь всегда вызывает `SendAsyncRaw()` и десериализует ответ централизованно.
- **BREAKING**: Обработка ошибок в `MaxHttpClient` изменена — метод `HandleHttpResponseError()` теперь sync вместо async.
- **BREAKING**: `FileUploadResult` больше не содержит свойство `PhotoId` (удалено как устаревшее).
- Обновлены тесты `AttachmentTests.cs`, `FilesApiTests.cs`, `BotApiTests.cs`, `ChatsApiTests.cs`, `SubscriptionsApiTests.cs`.
- Добавлены новые тесты `ContactHelpersTests.cs`, `FileUploadResultTests.cs`, `UpdateTypeJsonConverterTests.cs`, `UpdateTypeHelperTests.cs`.
- Увеличено покрытие тестами HTTP клиент логики (retry, logging, error handling).
- **Refactor**: Удалены дублирующиеся using директивы и оптимизирован код `MaxHttpClient.cs`.

### Removed

- **`FileUploadResult.PhotoId`**: Удалено как устаревшее свойство (используйте `FileId` или `Photos` коллекцию).

---

## [0.4.1-alpha] - 2025-03-09

### Added

- **Location Attachment**: Поддержка вложений местоположения (`LocationAttachment`) с полями `Latitude` и `Longitude`.
- **Image Attachment**: Поддержка вложений изображений (`ImageAttachment`) с возможностью отправки нескольких фото одновременно.
- **Contact Attachment**: Полная поддержка вложений контактов (`ContactAttachment`) с автоматическим парсингом vCard данных.

### Fixed

- **DeleteWebhook URL**: Endpoint `DeleteWebhook` теперь корректно принимает `url` только как query parameter (согласно спецификации Max API).

### Changed

- Обновлены тесты для новых типов вложений.

---

## [0.4.0-alpha] - 2025-03-08

### Added

- **NewMessageLink Type**: Добавлен тип `NewMessageLink` с полями `ChatId`, `MessageId`, `Text`.
- **Validation Attributes**: Добавлена валидация для `NewMessageLink` (Required, MaxLength, Range).

### Fixed

- **NewMessageLink JSON Field**: Изменено имя JSON поля с `id` на `mid` для совместимости со спецификацией Max API.
- **ChatId Validation**: Удалена валидация `Range` для `ChatId` в `NewMessageLink` для поддержки отрицательных ID групповых чатов.
- **MessageId Type**: Изменён тип `MessageId` с `long` на `string` в `ForwardMessageAsync` и `ReplyToMessageAsync` для совместимости с API.

### Changed

- **Breaking**: Тип `MessageId` изменён с `long` на `string` в методах работы с сообщениями.

---

## [0.3.8-alpha] - 2025-03-07

### Changed

- Version bump для republication на NuGet.

---

## [0.3.6-alpha] - 2025-03-06

### Fixed

- **ChatId Validation**: Удалена валидация `ChatId = 0` для поддержки direct messages.
- **UserId Validation**: Централизована валидация `UserId` через общий метод валидации.

---

## [0.3.5-alpha] - 2025-03-05

### Fixed

- **Critical**: Исправлена валидация `ChatId = 0` для direct messages (DM).

---

## [0.3.4-alpha] - 2025-03-04

### Added

- **MessageRecipient Tests**: Добавлены unit тесты для сериализации/десериализации `MessageRecipient`.

### Fixed

- **Direct Messages**: Исправлена валидация `ChatId` и `UserId` для поддержки direct messages.

---

## [0.3.3-alpha] - 2025-03-03

### Added

- **Inline Button Intents**: Добавлена поддержка интентов для inline кнопок.
- **BotCommand Model**: Введена новая модель `BotCommand` с полями `Name` и `Description`.
- **SetCommandsAsync**: Новый метод в `BotApi` для установки команд бота.

### Changed

- Обновлены тесты для новых функций inline кнопок и команд бота.

---

## [0.3.2-alpha] - 2025-03-02

### Fixed

- **EditMessageReplyMarkupAsync**: Исправлена критическая проблема — метод теперь сохраняет вложения сообщения при обновлении клавиатуры.
- **Null Safety**: Улучшена обработка null значений в методах редактирования сообщений.

### Changed

- **Refactor**: Логика редактирования сообщений переработана для сохранения вложений.

---

## [0.3.0-alpha] - 2025-03-01

### Added

- Initial alpha release for Max Messenger Bot API.
- Basic bot API methods: `GetMe`, `SendMessage`, `ForwardMessage`, etc.
- Webhook support: `SetWebhook`, `DeleteWebhook`, `GetWebhookInfo`.
- Update polling: `GetUpdates` with filtering support.
- File management: `UploadFile`, `GetFile`, `DeleteFile`.
- Chat management: `GetChat`, `UpdateChat`.
- Subscription management: `GetSubscriptions`, `CreateSubscription`, `DeleteSubscription`.

### Changed

- Migrated to .NET 9.0.
- Implemented custom JSON serializer (`MaxJsonSerializer`).
- Added comprehensive error handling with custom exceptions (`MaxApiException`, `MaxNetworkException`, etc.).

---

[Unreleased]: https://github.com/NanoAgents/MaxBotNet/compare/v0.6.2-alpha...HEAD
[0.6.2-alpha]: https://github.com/NanoAgents/MaxBotNet/compare/v0.6.1-alpha...v0.6.2-alpha
[0.6.1-alpha]: https://github.com/NanoAgents/MaxBotNet/compare/v0.5.6-alpha...v0.6.1-alpha
[0.5.6-alpha]: https://github.com/NanoAgents/MaxBotNet/compare/v0.5.5-alpha...v0.5.6-alpha
[0.5.5-alpha]: https://github.com/NanoAgents/MaxBotNet/compare/v0.5.4-alpha...v0.5.5-alpha
[0.5.4-alpha]: https://github.com/NanoAgents/MaxBotNet/compare/v0.5.3-alpha...v0.5.4-alpha
[0.5.3-alpha]: https://github.com/NanoAgents/MaxBotNet/compare/v0.5.2-alpha...v0.5.3-alpha
[0.5.2-alpha]: https://github.com/NanoAgents/MaxBotNet/compare/v0.5.1-alpha...v0.5.2-alpha
[0.5.1-alpha]: https://github.com/NanoAgents/MaxBotNet/compare/v0.5.0-alpha...v0.5.1-alpha
[0.5.0-alpha]: https://github.com/NanoAgents/MaxBotNet/compare/v0.4.1-alpha...v0.5.0-alpha
[0.4.1-alpha]: https://github.com/NanoAgents/MaxBotNet/compare/v0.4.0-alpha...v0.4.1-alpha
[0.4.0-alpha]: https://github.com/NanoAgents/MaxBotNet/compare/v0.3.8-alpha...v0.4.0-alpha
[0.3.8-alpha]: https://github.com/NanoAgents/MaxBotNet/compare/v0.3.6-alpha...v0.3.8-alpha
[0.3.6-alpha]: https://github.com/NanoAgents/MaxBotNet/compare/v0.3.5-alpha...v0.3.6-alpha
[0.3.5-alpha]: https://github.com/NanoAgents/MaxBotNet/compare/v0.3.4-alpha...v0.3.5-alpha
[0.3.4-alpha]: https://github.com/NanoAgents/MaxBotNet/compare/v0.3.3-alpha...v0.3.4-alpha
[0.3.3-alpha]: https://github.com/NanoAgents/MaxBotNet/compare/v0.3.2-alpha...v0.3.3-alpha
[0.3.2-alpha]: https://github.com/NanoAgents/MaxBotNet/compare/v0.3.0-alpha...v0.3.2-alpha
[0.3.0-alpha]: https://github.com/NanoAgents/MaxBotNet/releases/tag/v0.3.0-alpha
