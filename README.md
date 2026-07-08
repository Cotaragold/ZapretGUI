<div align="center">
  <img src="Assets/logo.png" width="128" alt="Zapret GUI" />
  <h1>Zapret GUI</h1>
  <p>Неоновая WPF-обёртка над <a href="https://github.com/bol-van/zapret">Zapret</a> (winws) для обхода DPI: подбор рабочей стратегии в один клик и авто-обновление списка заблокированных IP.</p>
</div>

## Возможности

- **Авто-подбор стратегии.** Кнопка «Авто (подбор)» по очереди запускает каждый `general*.bat`, проверяет доступность популярных заблокированных сайтов (TCP + TLS-handshake с SNI) и выбирает ту стратегию, у которой открылось больше всего сайтов. Выбранная назначается рабочей (LastGood) и запускается.
- **Авто-список заблокированных IP.** При старте (фоном) скачивает публичный список подсетей (по умолчанию [antifilter.download](https://antifilter.download)) и дописывает его в `lists\ipset-all.txt` — с дедупликацией и резервной копией `.bak`. Есть и кнопка ручного обновления на вкладке Service.
- **Управление списками.** Вкладка Lists: вставляешь текст — приложение разбирает его на IP/CIDR, Google-домены и обычные домены и раскладывает по нужным файлам в `lists\`.
- **Запуск/остановка winws, открытие `service.bat`, просмотр логов** в реальном времени.

## Требования

- Windows 10/11 x64
- Права администратора (нужно для winws/WinDivert)
- Дистрибутив [Zapret](https://github.com/bol-van/zapret) (папка с `general.bat`, `service.bat`, `bin\winws.exe` и `lists\`)

> Приложение — **только GUI**. Сам Zapret в него не входит: при первом запуске укажи папку с Zapret.

## Установка

Скачай `ZapretGUI-Setup-x.y.z.exe` из [Releases](../../releases) и запусти. Ставится в `Program Files`, ярлык в меню Пуск. Рантайм .NET не нужен — сборка self-contained.

При первом запуске выбери папку Zapret (в ней должны быть `general.bat`, `service.bat` и папка `lists`).

## Сборка из исходников

```powershell
# сборка
dotnet build ZapretGUI.csproj -c Release

# установщик (self-contained publish + Inno Setup)
powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1
```

Требуется .NET 8 SDK. Скрипт установщика сам поставит Inno Setup через winget, если его нет.

## Настройка

Файл `appsettings.json` рядом с exe (создаётся автоматически):

| Ключ | По умолчанию | Описание |
|------|--------------|----------|
| `ZapretFolder` | — | Путь к папке Zapret |
| `LastGoodStrategy` | `null` | Последняя выбранная рабочая стратегия |
| `TestHosts` | `null` | Свой список сайтов для проверки (`null` = встроенный) |
| `TestWarmupMs` | `4000` | Пауза (мс) на запуск winws перед проверкой |
| `TestConnectTimeoutMs` | `5000` | Таймаут (мс) на подключение к сайту |
| `IpListUrls` | `null` | Свои URL списков IP (`null` = antifilter allyouneed) |
| `AutoUpdateIpOnStartup` | `true` | Обновлять `ipset-all.txt` при старте |

## Стек

.NET 8 · WPF (MVVM) · Inno Setup

## Лицензия

MIT
