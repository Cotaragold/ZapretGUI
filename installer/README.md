# Установщик Zapret GUI

Собирает `.exe`-инсталлятор на базе [Inno Setup](https://jrsoftware.org/isdl.php).

## Сборка

```powershell
powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1
```

Скрипт делает всё сам:
1. `dotnet publish` — **self-contained** win-x64 (юзеру НЕ нужен установленный .NET) → `installer\publish\`.
2. Находит `ISCC.exe` (Inno Setup). Если нет — ставит через `winget` (JRSoftware.InnoSetup).
3. Компилирует `ZapretGUI.iss` → `installer\output\ZapretGUI-Setup-<версия>.exe`.

## Результат

- `installer\output\ZapretGUI-Setup-1.0.0.exe` (~52 MB).
- Ставится в `Program Files\ZapretGUI`, ярлык в меню Пуск (+ опционально на рабочий стол).
- Запуск с правами администратора (нужно для winws/WinDivert) — UAC при каждом старте.

## Версия

Менять `MyAppVersion` в начале `ZapretGUI.iss`. `AppId` НЕ трогать — иначе обновление
поставится второй копией вместо замены.

## Важно

Инсталлятор ставит **только GUI**. Сам Zapret (winws.exe + general*.bat + папка lists)
не входит — при первом запуске приложение попросит указать папку Zapret.
