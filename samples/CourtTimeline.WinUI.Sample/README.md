# Приложение для визуальной проверки

Небольшое самостоятельное приложение на WinUI 3. В окне размещён настоящий `CourtTimelineView` из библиотеки. Оболочка календаря LawMatic для запуска не нужна.

## Запуск на Windows

Нужны .NET 10 SDK и Windows App SDK 2.5.1 Runtime. Из корня репозитория:

```powershell
.\Run-Sample.ps1
```

Скрипт собирает и запускает приложение; архитектура x64/ARM64 выбирается автоматически. При необходимости:

```powershell
.\Run-Sample.ps1 -Architecture ARM64 -Configuration Release
```

Без скрипта:

```powershell
dotnet run --project samples/CourtTimeline.WinUI.Sample/CourtTimeline.WinUI.Sample.csproj -p:Platform=x64
```

В Visual Studio откройте `CourtTimeline.slnx`, назначьте `CourtTimeline.WinUI.Sample` стартовым проектом, выберите x64 или ARM64 и запустите с F5. Приложение unpackaged: регистрация MSIX не требуется.

## Что можно проверить

| Набор данных | Проверка |
| --- | --- |
| Демонстрационное дело | Три стадии, восемь событий, выбранное заседание и его детали |
| Много событий в один день | Дополнительные строки, длинные подписи, вертикальная прокрутка |
| Новый год и 29 февраля | Подписи 2027/2028 годов, високосный день, формат дат |
| Только план | Скрытие плана при отсутствии активной стадии; очистка выбора |
| Пустое дело | Шапка дела, нулевые счётчики, отсутствие стадий |
| Нет выбранного дела | Пустое состояние без шапки и старых деталей |

Сверху доступны системная/светлая/тёмная тема, русский/английский язык компонента, показ шапки и деталей. Авторский текст демоданных остаётся русским. Внизу отображается результат события `SelectionChanged`, в том числе когда встроенная панель деталей выключена.

Масштаб, «Весь процесс» и показ плана — элементы самой библиотеки. Переключение набора данных восстанавливает масштаб 1 и включает план; «Сбросить» возвращает все настройки и первое дело. Проверяйте также изменение размеров окна, Tab/Enter/Space и системную высокую контрастность.

## Собрать папку для передачи

На Windows из корня репозитория:

```powershell
dotnet publish samples/CourtTimeline.WinUI.Sample/CourtTimeline.WinUI.Sample.csproj -c Release -r win-x64 -p:Platform=x64 --self-contained true -p:WindowsAppSDKSelfContained=true -o artifacts/demo-win-x64
```

Передайте **всю папку** `artifacts/demo-win-x64`; запускаемый файл — `CourtTimeline.WinUI.Sample.exe`. В неё включаются .NET и Windows App SDK. См. [описание self-contained deployment Microsoft](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/self-contained-deploy/deploy-self-contained-apps).

После успешного запуска [Windows CI](https://github.com/lawlabs/court-timeline-winui/actions/workflows/ci.yml) эта папка доступна как артефакт `CourtTimeline-Demo-win-x64`. Скачайте архив из раздела Artifacts выбранного запуска и распакуйте его целиком. Сборка CI не заменяет визуальную проверку приложения на Windows.
