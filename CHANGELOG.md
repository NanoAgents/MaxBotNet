# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.8.0-alpha] - 2026-10-30

Модели приведены к [OpenAPI-схеме MAX Bot API](https://github.com/max-messenger/max-bot-api-client-go/blob/main/schema.yaml): строковые и `object`-поля стали типизированными, а nullability совпадает с документацией — меньше ручного разбора и null-проверок. Спасибо @diquoks за отзыв!

### Breaking Changes

- **Typed enums instead of strings**: `LinkedMessage.Type` — `MessageLinkType?` (было `string`), `MessageRecipient.ChatType` — `ChatType?` (было `string`), `MarkupElement.Type` — новый enum `MarkupType?`. Неизвестное библиотеке значение превращается в `null`, а не роняет разбор апдейта.
- **Typed inline keyboard**: `InlineKeyboardAttachment.Payload` — `InlineKeyboard` с `InlineKeyboardButton[][] Buttons` (было `Dictionary<string, object>` с `JsonElement` внутри); добавлен шорткат `InlineKeyboardAttachment.Buttons`.
- **Typed dictionaries**: `Chat.Participants` — `Dictionary<long, long>?` (user id → время последней активности), `Image.Photos` — `Dictionary<string, PhotoSizeToken>?` (было `object`).
- **Required fields are non-nullable** (по схеме они всегда приходят):
  - `Message.Recipient` (`new()` по умолчанию), `Message.Timestamp` — `long`; `MessageBody.Mid` — `string`, `MessageBody.Seq` — `long`; `LinkedMessage.Message` — `MessageBody`, `LinkedMessage.Mid`/`Seq` — `string`/`long`; `MessageStat.Views` — `int`.
  - `User.FirstName` — `string`, `User.LastActivityTime` — `long`; то же для `ChatMember` плюс `LastAccessTime`, `JoinTime` — `long`.
  - `Chat.LastEventTime` — `long`, `Chat.ParticipantsCount` — `int`; `Image.Url` — `string`; `Subscription.Time` — `long`.
  - `CallbackQuery.CallbackId` — `string`, `CallbackQuery.User` — `User`, `CallbackQuery.Timestamp` — `long`.
  - `Update.Timestamp` и `Timestamp` всех типизированных обёрток — `long`. В обёртках обязательные поля тоже не nullable: `ChatId` (`long`), `User`, `IsChannel` (`bool`), `MessageRemovedUpdate.MessageId`/`UserId`, `ChatTitleChangedUpdate.Title`, `DialogMutedUpdate.MutedUntil`, `MessageChatCreatedUpdate.Chat`/`MessageId`.
  - Вложения: `Url`/`Token` у фото, видео, аудио и файлов — `string`; `DocumentAttachment.FileName` — `string`, `FileSize` — `long`; у стикера `Url`/`Code` — `string`, `Width`/`Height` — `int`. `Video` (`GET /videos/{token}`): `Token` — `string`, `Width`/`Height`/`Duration` — `int`.
  - Массивы в ответах (`GetMessagesResponse.Messages`, `ChatMembersResponse.Members`, `SubscriptionsResponse.Subscriptions`, `GetUpdatesResponse.Updates`) по умолчанию пустые, а не `null`.
- **IChatsApi**: `AddChatMembersAsync` возвращает `ModifyMembersResponse` (наследник `Response`); новые члены интерфейса `GetChatMembersByIdsAsync` и `RemoveChatMemberAsync(chatId, userId, block)` — собственные реализации нужно дополнить.
- **Enum members** больше не помечены `[JsonPropertyName]` — `System.Text.Json` его для enum'ов игнорирует; значение API описано в XML-доке.

### Added

- **Chat button**: `ButtonType.Chat` и поля `InlineKeyboardButton.ChatTitle`, `ChatDescription`, `StartPayload`, `Uuid` (кнопка `chat`, создающая чат).
- **Data attachment**: `DataAttachment` (`type: "data"`) с payload нажатой кнопки reply-клавиатуры — раньше разбирался как пустой `DocumentAttachment`.
- **Add members result**: `ModifyMembersResponse.FailedUserIds` и `FailedUserDetails` (`error_code`, `user_ids`) — кого не удалось добавить и почему.
- **Chat members**: `GetChatMembersByIdsAsync(chatId, userIds)` (`user_ids`) и `RemoveChatMemberAsync(chatId, userId, block)` (`block=true` блокирует участника).
- **`JsonSnakeCaseEnumConverter<TEnum>`** — snake_case-конвертер, которым помечены все enum'ы библиотеки.

### Fixed

- **Enum deserialization without library options**: `MessageLinkType`, `ChatAction`, `UploadType`, `ChatAdminPermission`, `UpdateType` и другие enum'ы не разбирались обычным `JsonSerializer` — например, `JsonSerializer.SerializeToElement("reply").Deserialize<MessageLinkType>()` падал с `JsonException`. Теперь у каждого enum'а есть `[JsonConverter]`, и модели разбираются одинаково с любыми `JsonSerializerOptions`.
- **Inline keyboard parsing**: кнопка с `"payload": null` больше не получает строку `"null"`; поля кнопки неожиданного типа и битый `payload` клавиатуры не роняют разбор всего сообщения.
- **Validation limits**: `InlineKeyboardButton.Text` — до 128 символов, `Payload` — до 1024 (было 64, по схеме MAX).
- **UpdateTypeHelper.FromString** принимает и PascalCase (`"MessageCreated"`).

### Changed

- **Releases**: GitHub Release создаётся как Latest (не pre-release), даже для `-alpha` версий; при ручном запуске это можно отключить параметром `latest`.

## [0.7.1-alpha] - 2026-10-27

### Fixed

- **Bot commands** (#41, #42): `SetCommandsAsync` отправляет запрос на `PATCH /me/commands` вместо `PATCH /me` — MAX больше не принимает `PATCH /me` и отвечал `Path /me is not recognized`. Эндпоинт отвечает только списком команд, поэтому в возвращаемом `User` заполнено лишь `Commands`. Спасибо @diquoks!

## [0.7.0-alpha] - 2026-10-11

Ревью всей библиотеки на соответствие MAX Bot API (схемы официального Go SDK `max-bot-api-client-go`) и исправление открытых issue.

### Breaking Changes

- **Webhook endpoint is opt-in** (#35): `WebhookController` стал `abstract`, поэтому подключение пакета больше не добавляет `POST /api/max/webhook` (и запись в Swagger) в каждое ASP.NET Core приложение. Подключайте эндпоинт явно через `app.MapMaxWebhook()` или унаследуйтесь от `WebhookController` с `[Route(...)]`.
- **Webhook secret header**: `MaxWebhookOptions.SignatureHeaderName` по умолчанию теперь `X-Max-Bot-Api-Secret` — именно этот заголовок присылает MAX. Раньше при заданном `SecretToken` все реальные webhook-запросы отклонялись с 401.
- **Callback answer body**: `NewMessageBody.Attachments` теперь `AttachmentRequest[]`, а `NewMessageBody.Link` — `NewMessageLink` (формат запроса `POST /answers`).
- **AttachmentRequest**: `Payload` стал nullable (у `location` нет payload), добавлены `Latitude`/`Longitude`; с `Payload` снят атрибут `[Required]` (`Type` по-прежнему обязателен).
- **Typed updates**: `MessageRemovedUpdate.Message` и `MessageChatCreatedUpdate.Message` стали nullable — MAX присылает для этих событий идентификаторы, а не сообщение.
- **WebhookController action**: `PostAsync(CancellationToken)` больше не принимает `[FromBody] Update` — контроллер сам читает тело после проверки размера и секрета, поэтому неавторизованные запросы не парсятся.
- **Webhook secret format**: `MaxWebhookOptions.Validate()` и `ConfigureWebhookAsync` отклоняют секрет, не соответствующий формату MAX (5-256 символов `A-Z`, `a-z`, `0-9`, `_`, `-`).
- **Upload result**: `photos` и `file_id` разбираются в типизированные `FileUploadResult.Photos` / `FileId` и больше не попадают в `AdditionalProperties`.
- **New interface members**: `IMessagesApi.GetMessagesAsync(chatId, count, from, to)`, `IMessagesApi.GetMessagesByIdsAsync`, `IChatsApi.GetChatListAsync`, `IChatsApi.GetChatMembersPageAsync`, `IFilesApi.UploadAsync` — собственные реализации этих интерфейсов нужно дополнить.
- **AddChatAdminRequest.UserId** помечен `[Obsolete]`: MAX требует список прав, используйте `Admins` с `ChatAdmin.Permissions`.

### Added

- **Upload helper** (#38): `IFilesApi.UploadAsync(type, stream, fileName)` делает `POST /uploads` + загрузку и возвращает готовый токен (для `image` — из `photos`, для `video`/`audio` — токен из `POST /uploads`).
- **Attachment factories** (#15): `AttachmentRequest.ImageFromUrl`, `Image`, `Video`, `Audio`, `File`, `FromUpload`, `Sticker`, `Contact`, `Location`, `Share`, `Keyboard` и типизированные payload-классы.
- **MapMaxWebhook** (#35): minimal-API эндпоинт `app.MapMaxWebhook(pattern)` с проверкой секрета и лимита размера тела.
- **MaxAttachmentNotReadyException** (#22): отдельное исключение для ошибки `attachment.not.ready`.
- **Messages pagination** (#18): `GetMessagesAsync(chatId, count, from, to)` и `GetMessagesByIdsAsync(messageIds)`.
- **Chats pagination** (#23): `GetChatListAsync(count, marker)` возвращает `ChatListResponse` с `marker` следующей страницы; `GetChatMembersPageAsync(chatId, marker, count)` возвращает участников вместе с `marker`.
- **Inline buttons**: `ButtonType.Clipboard`, кнопка `open_app` (`ButtonType.OpenApp` + `InlineKeyboardButton.WebApp`/`ContactId`) и `Quick` для `request_geo_location`. `OpenApp` без `WebApp` по-прежнему отправляется как `link`.
- **Chat info in updates** (#37): `Update.ChatId`, `Update.Chat` (частично: id и тип) и `Update.ChatType` заполняются из `message.recipient` для `message_created`, `message_edited` и `message_callback`; для `bot_added`/`bot_removed`/`user_added`/`user_removed` `ChatType` вычисляется из `is_channel`.
- **Update fields**: `message_id`, `user_id`, `admin_id`, `title`, `is_channel`, `muted_until`, `start_payload` и соответствующие свойства в типизированных обёртках (`MessageRemovedUpdate`, `UserRemovedUpdate`, `ChatTitleChangedUpdate`, `BotAddedUpdate` и др.).
- **Attachments**: `StickerAttachment`, `ShareAttachment`; `Token` у фото/видео/аудио/файлов, `VideoAttachment.Thumbnail`, `AudioAttachment.Transcription`.
- **Models**: `MessageBody.Markup`, `MessageStat.Views`, `Chat.MessagesCount`, `ChatStatus.Suspended`, `Subscription.Time`/`Version`, `ChatAdmin` для назначения администраторов.

### Fixed

- **Callback answers** (#34): `AnswerCallbackQueryAsync` передаёт `callback_id` вместо устаревшего `callback_query_id`.
- **Image upload token** (#38): `FileUploadResult` снова содержит типизированные `Photos` и `FileId`, а `Token` заполняется из `photos`; XML-ответ CDN (`<retval>1</retval>`) больше не считается токеном.
- **API error parsing** (#22): ошибки формата `{"code": "...", "message": "..."}` разбираются в `MaxApiException.ErrorCode`/`Message` вместо сырого JSON в тексте исключения.
- **Attachment retry**: повтор отправки/редактирования выполняется только для `attachment.not.ready` (раньше повторялся любой `400 Bad Request`); повтор добавлен для `SendMessageAsync(SendMessageRequest)` с вложениями и для `EditMessageAsync`.
- **Get chats** (#23): `GetChatsAsync` корректно разбирает ответ `{"chats": [...], "marker": ...}` вместо исключения `The JSON value could not be converted to Max.Bot.Types.Chat[]`.
- **Pinned message**: `GetPinnedMessageAsync` разбирает ответ `{"message": ...}` и возвращает `null`, если ничего не закреплено.
- **Add chat admins**: `AddChatAdminRequest` сериализуется как `{"admins": [{"user_id", "permissions", "alias"}]}`.
- **Incoming attachments** (#20, #21, #25): поля из `payload` и корня вложения объединяются — у файлов теперь заполняются `FileName`/`FileSize` (`filename`/`size`), у видео — размеры, длительность и превью.
- **Long polling marker**: следующий запрос `GET /updates` использует `marker` из ответа. Раньше подставлялся `update_id`, которого нет в MAX API (всегда `0`).
- **Webhook configuration** (#36): `ConfigureWebhookAsync()` и `DeleteWebhookAsync()` берут URL из `MaxWebhookOptions.Endpoint`, а секрет — из `MaxWebhookOptions.SecretToken`, если они не переданы явно.
- **Upload retries**: при повторе `UploadFileDataAsync` поток перематывается в начало, а не поддерживающие seek потоки один раз буферизуются, чтобы не отправлять пустой файл. Токен, вычисленный из `photos`, не сериализуется обратно (в image payload `token` и `photos` взаимоисключающие). `UploadFileResumableAsync` больше не делает лишний GET после загрузки видео/аудио.
- **Culture-invariant ids**: id чатов/пользователей и числовые query-параметры форматируются через `InvariantCulture` — в культурах с минусом U+2212 (sv-SE, nb-NO, fi-FI) запросы к групповым чатам (отрицательные id) ломались.
- **Poison updates**: апдейт, который не удалось разобрать, пропускается с логированием — раньше он ронял всю пачку, маркер не сдвигался и long polling зацикливался. Вложения с неожиданными типами полей превращаются в «пустое» вложение нужного типа вместо исключения.
- **Polling**: маркер сдвигается только после обработки всей пачки (остановка посреди пачки не теряет апдейты); `StopAsync` дожидается параллельных обработчиков; фильтр `AllowedUsernames` для `message_callback` проверяет нажавшего кнопку, а не автора сообщения-бота.
- **Webhook hardening**: `MapMaxWebhook` не подставляет пустые `IOptions<MaxBotOptions>` (это отключало проверку секрета) и берёт опции из `MaxClient`; `MaxWebhookOptions.HandlerTimeout` теперь применяется; `ConfigureWebhookAsync` обновляет `Endpoint`/`SecretToken` только при `success: true` и всегда сохраняет переданный секрет.
- **Retry-After**: дата в прошлом или отрицательное значение больше не приводят к `ArgumentOutOfRangeException` в цикле повторов.
- **message_chat_created**: `Update.ChatId` и `MessageChatCreatedUpdate.ChatId` заполняются из объекта `chat`.
- **Unknown enum values**: неизвестные статусы чата и права администраторов больше не ломают десериализацию ответа.
- **User-Agent**: заголовок содержит фактическую версию пакета вместо зашитой `0.5.1-alpha`.
- **Build**: `Microsoft.SourceLink.GitHub` обновлён до `10.0.401` — версия `8.0.0` тянула уязвимый `Microsoft.Build.Tasks.Git` (GHSA-23fw-v26w-5fgq), из-за чего `dotnet restore` падал с `NU1902` при `TreatWarningsAsErrors`.
- **Samples**: исправлена испорченная кодировка текстов в примерах; `FileBotSample` использует `UploadAsync`.

## [0.6.3-alpha] - 2026-06-08

### Fixed

- **Image/Video/Audio upload MIME type**: `UploadFileDataAsync` and `UploadFileResumableAsync` no longer hardcode `Content-Type: application/octet-stream`. The MIME type is now auto-detected from the file name extension (e.g. `image/jpeg` for `.jpg`) and falls back to `application/octet-stream` for unknown extensions. This fixes the `"Error reading file from request in direct mode"` error that occurred when uploading images to the `iu.oneme.ru` CDN, which requires a correct MIME type for validation.

### Added

- **MIME type helper**: `Max.Bot.Networking.MimeTypes` maps 18 common file extensions to `Content-Type` values for images, video, and audio.

### Changed

- **Format restrictions documented**: `IFilesApi.UploadFileAsync` XML docs now note that unsupported extensions (e.g. `.html`) return `"File extension is forbidden"` from the Max API, and list supported formats per upload type.


## [0.6.2-alpha] - 2026-05-14

### Added

- **Chat admin permissions**: Добавлены недостающие значения `ChatAdminPermission` для актуального контракта MAX API.
- **Video metadata**: Добавлены поля модели `Video` для расширенного media-контракта.

### Changed

- **Callback and media contracts**: Обновлены модели callback/message и media/video под актуальные payload-форматы MAX API.
- **Contact attachments**: `ContactAttachment` теперь разбирается из `attachments[].payload.vcf_info`, `max_info` и `hash`.

### Fixed

- **Attachment payload parsing**: Исправлен разбор payload tokens для media attachments.
- **Callback message wrapper**: `CallbackQueryUpdate.Message` теперь содержит root-level `message` из `message_callback`, чтобы обработчики могли получить `mid` исходного сообщения.
- **Get updates deserialization**: `GetUpdatesResponse` теперь десериализуется direct-first, без лишнего wrapper-first пути для long polling payload.
- **Inline OpenApp buttons**: `ButtonType.OpenApp` теперь сериализуется как `link`, а сам enum value помечен obsolete в пользу `ButtonType.Link`.
- **Infinite timeouts**: `Timeout.InfiniteTimeSpan` теперь разрешён для execution timeouts в `MaxBotClientOptions.Timeout`, `UpdateHandlingOptions.HandlerTimeout` и `MaxWebhookOptions.HandlerTimeout`.
- **Handler timeout validation**: `UpdateHandlingOptions.HandlerTimeout` теперь отклоняет значения больше диапазона `CancellationTokenSource.CancelAfter`, включая `TimeSpan.MaxValue`.
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

[Unreleased]: https://github.com/NanoAgents/MaxBotNet/compare/v0.7.1-alpha...HEAD
[0.7.1-alpha]: https://github.com/NanoAgents/MaxBotNet/compare/v0.7.0-alpha...v0.7.1-alpha
[0.7.0-alpha]: https://github.com/NanoAgents/MaxBotNet/compare/v0.6.3-alpha...v0.7.0-alpha
[0.6.3-alpha]: https://github.com/NanoAgents/MaxBotNet/compare/v0.6.2-alpha...v0.6.3-alpha
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
