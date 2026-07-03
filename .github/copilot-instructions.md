# Copilot Instructions for QBTracker

## Build

```powershell
dotnet build QBTracker.sln
```

Build a single project when narrowing feedback:

```powershell
dotnet build QBTracker\QBTracker.csproj
dotnet build QBTracker.Plugin.Jira\QBTracker.Plugin.Jira.csproj
```

The solution projects target `net10.0-windows` and the desktop/plugin projects use `win-x64`. The main app embeds the published updater executable from `QBTracker.AutomaticUpdader\bin\Release\win-x64\publish\QBTrackerAutomaticUpdader.exe`, so publish the updater before publishing the app:

```powershell
dotnet publish QBTracker.AutomaticUpdader\QBTracker.AutomaticUpdader.csproj -c Release -r win-x64 -p:PublishDir=bin\Release\win-x64\publish\
dotnet publish QBTracker\QBTracker.csproj -c Release -r win-x64 -p:PublishProfile=FolderProfile1
```

## Architecture

This is a WPF desktop time tracker using MVVM with LiteDB as the embedded document database.

### Projects

- `QBTracker` - main WPF app; references the updater, plugin contracts, and Jira plugin.
- `QBTracker.AutomaticUpdader` - console updater that checks GitHub Releases RSS and is embedded into the main app for release builds.
- `QBTracker.Plugin.Contracts` - shared plugin interfaces: `IPluginTaskProvider` and `IPluginConfigRepository`.
- `QBTracker.Plugin.Jira` - WPF/dynamic-loading Jira task provider plugin. Its output path is the main app output folder so `PluginService` can discover `QBTracker.Plugin.*.dll`.
- `QBTrackerCloudSync` - Azure Functions v3 / .NET 5 project outside `QBTracker.sln`.

### MVVM and navigation

- Models in `QBTracker\Model\` are LiteDB POCOs: `Project`, `Task`, `TimeRecord`, `Settings`, `TimeAggregate`, and `LogEntry`.
- ViewModels in `QBTracker\ViewModels\` inherit from `ValidatableModel`, which provides `INotifyPropertyChanged`, `INotifyDataErrorInfo`, and DataAnnotations validation.
- Views in `QBTracker\Views\` are XAML controls wired by parent XAML/view models rather than a ViewModelLocator.
- `MainWindow` hosts a Material Design `Transitioner`; page indexes live in `ViewModels\Pages.cs`, and navigation is driven by `MainWindowViewModel.SelectedTransitionIndex`. `GoBack()` uses an internal `navigationHistory` stack.

### Data access and state

`Repository` wraps LiteDB's `LiteRepository` and is registered as a XAML resource in `Resources\Resources.xaml`. ViewModels and converters retrieve it from application resources:

```csharp
Repository = (IRepository)Application.Current.Resources["Repository"];
```

Debug data is stored in `App_Data\QBData.db`; release data is stored under `%AppData%\QBTracker\QBData.db`. `Settings` are cached in `Repository`, and application startup reads them before applying the Material Design `BundledTheme`.

### Domain flow

- Projects contain Tasks; Tasks have TimeRecords with UTC start/end timestamps.
- `TimeRecord.EndTime == null` means a timer is actively recording.
- Projects and Tasks are soft-deleted with `IsDeleted`; repository queries filter deleted rows.
- `TimeAggregate` stores cached daily/monthly totals and is recalculated when time records are added, updated, deleted, or cleared.
- Export and invoice flows are view-model driven and use `ObservableRangeCollection<T>` for bulk UI updates.

### Plugins

`PluginService` is a XAML resource that scans the app base directory for `QBTracker.Plugin.*.dll`, loads them through `PluginLoadContext`, and instantiates non-abstract `IPluginTaskProvider` implementations. Plugin configuration is stored per project/plugin in LiteDB via `PluginConfigRepository` and the `PluginConfigs` collection. The Jira plugin supplies its own configuration view and imports tasks through Jira REST API calls.

## Key conventions

- Use `RelayCommand` and `AsyncRelayCommand` from `QBTracker\Util\`; they hook WPF `CommandManager.RequerySuggested` and log execution errors through the repository resource when available.
- Call `NotifyOfPropertyChange()` from ViewModels; pass a property name only when notifying dependent properties.
- Use `ObservableRangeCollection<T>.AddRange()` / `ReplaceRange()` for bulk collection updates instead of repeated single-item notifications.
- Store `TimeRecord` values in UTC and convert at ViewModel boundaries with `.ToLocalTime()` and `.ToUniversalTime()`.
- Register reusable converters in `Resources\Resources.xaml`; converters that need data access follow the repository-resource pattern.
- Keep Material Design theme changes synchronized with the `Settings` LiteDB record.
