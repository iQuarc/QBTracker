# Copilot Instructions for QBTracker

## Build

```powershell
dotnet build QBTracker.sln
```

The main app targets `net9.0-windows` with `win-x64` runtime identifier. The auto-updater's published exe is embedded into the main app as a resource (see the `EmbeddedResource` in `QBTracker.csproj`), so a release build of the updater must exist before publishing the main app:

```powershell
dotnet publish QBTracker.AutomaticUpdader -c Release
dotnet publish QBTracker -c Release
```

There are no tests in this solution.

## Architecture

This is a WPF desktop time tracker using **MVVM** with **LiteDB** as an embedded document database.

### Projects

- **QBTracker** — Main WPF application (.NET 9.0-windows)
- **QBTracker.AutomaticUpdader** — Console app that handles self-update via GitHub Releases RSS feed. Its compiled exe is embedded into the main app binary.
- **QBTracker.Plugin.Contracts** — Interfaces (`IPluginTaskProvider`, `IPluginConfigRepository`) for a plugin system (not yet wired up)
- **QBTrackerCloudSync** — Azure Function (v3, .NET 5.0) for cloud sync (separate deployment, not referenced by main app)

### MVVM Pattern

- **Models** are in `QBTracker/Model/` — plain POCOs stored directly in LiteDB: `Project`, `Task`, `TimeRecord`, `Settings`, `TimeAggregate`
- **ViewModels** are in `QBTracker/ViewModels/` — inherit from `ValidatableModel` (in `Util/`), which provides `INotifyPropertyChanged` and `INotifyDataErrorInfo` with `DataAnnotations`-based validation
- **Views** are in `QBTracker/Views/` — XAML UserControls. Each view's `DataContext` is set in the parent XAML, not via a ViewModelLocator

### Data Access

`Repository` is a single class in `DataAccess/` that wraps `LiteRepository` (LiteDB). It is instantiated as a **XAML resource** in `Resources/Resources.xaml` and retrieved in ViewModels via:

```csharp
Repository = (IRepository)Application.Current.Resources["Repository"];
```

In Debug mode, the database file is at `App_Data/QBData.db`. In Release, it's in `%AppData%/QBTracker/QBData.db`.

### Navigation

The app uses a single `MainWindow` with a Material Design `Transitioner` control. Page indices are defined as constants in `ViewModels/Pages.cs` (e.g., `Pages.MainView = 0`, `Pages.CreateProject = 1`). Navigation is driven by setting `MainWindowViewModel.SelectedTransitionIndex`. A `navigationHistory` stack supports back-navigation via `GoBack()`.

### Domain Model

- **Project** → has many **Tasks** → time is tracked as **TimeRecords** (start/end timestamps)
- `TimeRecord.EndTime` is `null` while actively recording
- `TimeAggregate` caches per-day and per-month totals, updated on every time record change
- Soft deletes via `IsDeleted` flag on Projects and Tasks

## Key Conventions

- **Commands**: Use custom `RelayCommand` and `AsyncRelayCommand` (both in `Util/`, using primary constructors). These hook into WPF's `CommandManager.RequerySuggested` for automatic CanExecute refresh.
- **Property change notifications**: Call `NotifyOfPropertyChange()` (no argument needed — uses `[CallerMemberName]`). Do not use `OnPropertyChanged`.
- **Collections**: Use `ObservableRangeCollection<T>` (in `Util/`) for bulk-add via `AddRange()`/`ReplaceRange()` instead of adding items one at a time.
- **Time handling**: `TimeRecord` stores UTC times. Convert with `.ToLocalTime()` / `.ToUniversalTime()` when displaying/saving.
- **Theming**: Material Design in XAML with `BundledTheme`. Theme settings (dark/light, primary/secondary colors) persist in the `Settings` LiteDB collection.
- **Converters** are in `QBTracker/Converters/` and registered as XAML resources in `Resources/Resources.xaml`.
