# Сборка D&D Encounters

Требуется установленный .NET 9 SDK.

- Windows x64: запустить `build-windows.bat` двойным щелчком. Исполняемый файл будет в `publish\win-x64\DndEncounters.exe`.
- Linux x64: `./build.sh linux-x64`
- macOS Apple Silicon: `./build.sh osx-arm64`
- macOS Intel: `./build.sh osx-x64`
- Windows ARM64: `./build.sh win-arm64`

Сборка выполняется для выбранной ОС и архитектуры. Создать универсальный один exe, который напрямую работает на Linux/macOS, невозможно: для каждой платформы нужен свой исполняемый файл.
