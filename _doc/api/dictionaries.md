# Словарь lookups

Дата обновления: 2026-08-24.  
Код: [`DictionariesController`](../../backend/EngineeringPerformance.Api/Controllers/DictionariesController.cs).

`GET /api/dictionaries/all` — право `References` Read.

Один ответ со всеми справочными списками для форм (в т.ч. `physicalVolumeTypes` для создания версии). SPA кэширует по **ETag**; повтор с `If-None-Match` даёт **304**.

После мутаций directories этот кэш на клиенте нужно сбросить (как делает UI).
