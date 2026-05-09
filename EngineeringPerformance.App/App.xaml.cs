using System.Windows;
using DevExpress.Xpf.Core;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.App.Views.Forms;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Models;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using EngineeringPerformance.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Application = System.Windows.Application;

namespace EngineeringPerformance.App;

public partial class App : Application
{
    private IHost? _host;
    private UserDto? _currentUser;

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        ConfigureSerilog();
        DispatcherUnhandledException += (s, args) =>
        {
            Log.Error(args.Exception, "Unhandled UI exception");

            // Критические ошибки (невозможность работы БД, ошибки безопасности, OOM)
            // не должны быть «съедены» — приложение в неконсистентном состоянии.
            if (IsFatal(args.Exception))
            {
                MessageBox.Show(
                    "Критическая ошибка, приложение будет закрыто. Подробности — в логах.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = false;
                Shutdown(1);
                return;
            }

            MessageBox.Show($"Произошла ошибка: {args.Exception.Message}",
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                Log.Fatal(ex, "Unhandled non-UI exception (terminating={Terminating})", args.IsTerminating);
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            Log.Error(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };

        try
        {
            await StartupAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fatal error during application startup");
            MessageBox.Show($"Критическая ошибка: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private static bool IsFatal(Exception ex) =>
        ex is OutOfMemoryException
            or StackOverflowException
            or AccessViolationException
            or System.Threading.ThreadAbortException;

    private async Task StartupAsync()
    {
        ApplicationThemeHelper.ApplicationThemeName = "LightGray";

        var splashWindow = new SplashWindow();
        splashWindow.Show();

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();

        await _host.StartAsync();

        await InitializeDatabaseAsync();

        if (_host.Services.GetRequiredService<IAppSettings>().AutoLoginEnabled)
            _currentUser = await TryAutoLoginAsync();

        splashWindow.Close();

        if (_currentUser is null)
            _currentUser = await RunLoginFlowAsync();

        if (_currentUser is null)
            return;

        var currentUserHolder = _host.Services.GetRequiredService<CurrentUserHolder>();
        currentUserHolder.User = _currentUser;

        var settingsHelper = _host.Services.GetRequiredService<UserSettingsHelper>();
        settingsHelper.SetUser(_currentUser.Id);

        await ShowMainWindowAsync();
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            await using var scope = _host!.Services.CreateAsyncScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using var context = await contextFactory.CreateDbContextAsync();

            // Шаг 1: миграции. Отдельный try/catch — миграция не должна автоматически
            // запускаться без согласия пользователя на проде, поэтому управляется флагом окружения.
            // По умолчанию (Dev/локальная разработка) — применяется.
            var skipMigrations = string.Equals(
                Environment.GetEnvironmentVariable("EP_SKIP_MIGRATIONS"),
                "true", StringComparison.OrdinalIgnoreCase);

            if (!skipMigrations)
            {
                await DatabaseSeeder.MigrateAsync(context);
            }

            // Шаг 2: сидинг (идемпотентен).
            await DatabaseSeeder.SeedAsync(context);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Database seeding failed, will continue with login");
        }
    }

    private async Task<UserDto?> TryAutoLoginAsync()
    {
        try
        {
            var authService = _host!.Services.GetRequiredService<IAuthenticationService>();
            return await authService.TryAutoLoginAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Auto-login failed due to database error");
            return null;
        }
    }

    private Task<UserDto?> RunLoginFlowAsync()
    {
        var authService = _host!.Services.GetRequiredService<IAuthenticationService>();
        var windowsUsername = authService.GetCurrentWindowsUsername();

        var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
        loginWindow.ViewModel?.SetWindowsUsername(windowsUsername);
        loginWindow.ViewModel!.OpenDbSettingsWindow = () =>
        {
            var dbSettingsWindow = _host.Services.GetRequiredService<DbSettingsWindow>();
            dbSettingsWindow.Owner = loginWindow;
            dbSettingsWindow.ShowDialog();
        };

        var loginResult = loginWindow.ShowDialog();
        if (loginResult != true)
        {
            Shutdown();
            return Task.FromResult<UserDto?>(null);
        }

        var user = loginWindow.ViewModel?.AuthenticatedUser;
        if (user is null)
        {
            MessageBox.Show("Не удалось получить данные пользователя после авторизации.", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return Task.FromResult<UserDto?>(null);
        }

        return Task.FromResult<UserDto?>(user);
    }

    private async Task ShowMainWindowAsync()
    {
        try
        {
            var mainWindow = _host!.Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;

            var wireup = new DialogWireup(_host.Services, mainWindow);
            wireup.Apply();
            wireup.RegisterResetOnClose();

            mainWindow.Closed += (s, args) => Shutdown();

            mainWindow.Show();

            // Прогрев кешей справочников в фоне: к моменту, когда пользователь
            // откроет соответствующий раздел, данные уже в памяти, и первый
            // показ не упирается в холодный SELECT по большой таблице.
            _ = Task.Run(WarmUpCachesAsync);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to show main window");
            MessageBox.Show($"Ошибка при открытии главного окна: {ex.Message}", "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }

        await Task.CompletedTask;
    }

    private async Task WarmUpCachesAsync()
    {
        try
        {
            var pvCache = _host!.Services.GetRequiredService<IDirectoryCache<PhysicalVolume>>();
            var pvTypeCache = _host.Services.GetRequiredService<IDirectoryCache<PhysicalVolumeType>>();
            var classifierCache = _host.Services.GetRequiredService<IDirectoryCache<IicWorkDirection>>();
            var unitCache = _host.Services.GetRequiredService<IDirectoryCache<UnitOfMeasurement>>();

            await Task.WhenAll(
                pvTypeCache.GetAllAsync(),
                classifierCache.GetAllAsync(),
                unitCache.GetAllAsync(),
                pvCache.GetAllAsync());
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Cache warm-up failed; will lazy-load on demand");
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IAppSettings, AppSettings>();

        services.AddDbContextFactory<AppDbContext>((sp, options) =>
            options
                .UseSqlServer(sp.GetRequiredService<IAppSettings>().ConnectionString));

        services.AddSingleton<IAccessControlService, AccessControlService>();
        services.AddSingleton<IProjectVisibilityService, ProjectVisibilityService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IDbConnectionService, DbConnectionService>();
        services.AddSingleton<IUserActionLogService, UserActionLogService>();

        services.AddSingleton<CurrentUserHolder>();

        services.AddSingleton<IDirectoryService<User>, UserDirectoryService>();
        services.AddSingleton<IDirectoryService<Role>, RoleDirectoryService>();
        services.AddSingleton<IDirectoryService<Project>, ProjectDirectoryService>();
        services.AddSingleton<PhysicalVolumeDirectoryService>();
        services.AddSingleton<IDirectoryService<PhysicalVolume>>(sp => sp.GetRequiredService<PhysicalVolumeDirectoryService>());
        services.AddSingleton<IPhysicalVolumeLookupService>(sp => sp.GetRequiredService<PhysicalVolumeDirectoryService>());
        services.AddSingleton<IicWorkDirectionDirectoryService>();
        services.AddSingleton<IDirectoryService<IicWorkDirection>>(sp => sp.GetRequiredService<IicWorkDirectionDirectoryService>());
        services.AddSingleton<IIicWorkDirectionLookupService>(sp => sp.GetRequiredService<IicWorkDirectionDirectoryService>());
        services.AddSingleton<IDirectoryService<PhysicalVolumeType>, PhysicalVolumeTypeDirectoryService>();
        services.AddSingleton<IDirectoryService<UnitOfMeasurement>, UnitOfMeasurementDirectoryService>();
        services.AddSingleton<IDirectoryService<LaborIntensity>, LaborIntensityDirectoryService>();
        services.AddSingleton<ProjectCharacteristicService>();
        services.AddSingleton<IDirectoryService<ProjectCharacteristic>>(sp => sp.GetRequiredService<ProjectCharacteristicService>());
        services.AddSingleton<ProjectCharacteristicSelectionService>();
        services.AddSingleton<ProjectAnalysisVersionDirectoryService>();
        services.AddSingleton<IDirectoryService<ProjectAnalysisVersion>>(sp => sp.GetRequiredService<ProjectAnalysisVersionDirectoryService>());
        services.AddSingleton<IProjectAnalysisVersionQueryService>(sp => sp.GetRequiredService<ProjectAnalysisVersionDirectoryService>());

        services.AddSingleton(typeof(IDirectoryCache<>), typeof(DirectoryCache<>));

        services.AddSingleton<IicWorkDirectionNormService>();
        services.AddSingleton<IRegisterService<IicWorkDirectionNorm>>(sp => sp.GetRequiredService<IicWorkDirectionNormService>());
        services.AddSingleton<IDateFilteredRegisterService<IicWorkDirectionNorm>>(sp => sp.GetRequiredService<IicWorkDirectionNormService>());
        services.AddSingleton<ProjectPhysicalVolumeService>();
        services.AddSingleton<IDataTableService<ProjectPhysicalVolume>>(sp => sp.GetRequiredService<ProjectPhysicalVolumeService>());
        services.AddSingleton<ProjectLaborIntensityService>();
        services.AddSingleton<IDataTableService<ProjectLaborIntensity>>(sp => sp.GetRequiredService<ProjectLaborIntensityService>());
        services.AddSingleton<IPerformanceService, PerformanceService>();
        services.AddSingleton<IProjectAnalysisService, ProjectAnalysisService>();
        services.AddSingleton<IProjectVersionAnalysisService, ProjectVersionAnalysisService>();
        services.AddSingleton<IProjectVersionAnalysisExportService, ProjectVersionAnalysisExportService>();
        services.AddSingleton<IProjectForecastExportService, ProjectForecastExportService>();
        services.AddSingleton<IDataImportService, DataImportService>();
        services.AddSingleton<IProjectForecastService, ProjectForecastService>();
        services.AddSingleton<IForecastPlannedVolumePersistenceService, ForecastPlannedVolumePersistenceService>();

        services.AddTransient<MainWindow>();
        services.AddTransient<LoginWindow>();
        services.AddTransient<DbSettingsWindow>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DbSettingsViewModel>();

        services.AddSingleton<UsersViewModel>();
        services.AddTransient<UsersWindow>();
        services.AddTransient<UserEditViewModel>();
        services.AddTransient<UserEditWindow>();
        services.AddSingleton<IUserEditDialogService, UserEditDialogService>();
        services.AddSingleton<ProjectsViewModel>();
        services.AddTransient<ProjectsWindow>();
        services.AddTransient<ProjectEditViewModel>();
        services.AddTransient<ProjectEditWindow>();
        services.AddSingleton<IProjectEditDialogService, ProjectEditDialogService>();
        services.AddSingleton<IProjectAdditionalFieldsDialogService, ProjectAdditionalFieldsDialogService>();
        services.AddTransient<ProjectPickerDialogViewModel>();
        services.AddTransient<ProjectPickerDialog>();
        services.AddSingleton<IProjectPickerDialogService, ProjectPickerDialogService>();
        services.AddTransient<ProjectDataDialogViewModel>();
        services.AddTransient<ProjectDataDialog>();
        services.AddSingleton<IProjectDataDialogService, ProjectDataDialogService>();
        services.AddTransient<ProjectParticipantsDialogViewModel>();
        services.AddTransient<ProjectParticipantsDialog>();
        services.AddSingleton<IProjectParticipantsDialogService, ProjectParticipantsDialogService>();
        services.AddSingleton<PhysicalVolumesViewModel>();
        services.AddTransient<PhysicalVolumesWindow>();
        services.AddTransient<PhysicalVolumeEditViewModel>();
        services.AddTransient<PhysicalVolumeEditWindow>();
        services.AddSingleton<IPhysicalVolumeEditDialogService, PhysicalVolumeEditDialogService>();
        services.AddSingleton<PhysicalVolumeTypesViewModel>();
        services.AddTransient<PhysicalVolumeTypesWindow>();
        services.AddTransient<PhysicalVolumeTypeEditViewModel>();
        services.AddTransient<PhysicalVolumeTypeEditWindow>();
        services.AddSingleton<IPhysicalVolumeTypeEditDialogService, PhysicalVolumeTypeEditDialogService>();
        services.AddSingleton<UnitOfMeasurementsViewModel>();
        services.AddTransient<UnitOfMeasurementsWindow>();
        services.AddTransient<UnitOfMeasurementEditViewModel>();
        services.AddTransient<UnitOfMeasurementEditWindow>();
        services.AddSingleton<IUnitOfMeasurementEditDialogService, UnitOfMeasurementEditDialogService>();
        services.AddSingleton<LaborIntensitiesViewModel>();
        services.AddTransient<LaborIntensitiesWindow>();
        services.AddTransient<LaborIntensityEditViewModel>();
        services.AddTransient<LaborIntensityEditWindow>();
        services.AddSingleton<ILaborIntensityEditDialogService, LaborIntensityEditDialogService>();
        services.AddSingleton<ProjectCharacteristicsViewModel>();
        services.AddTransient<ProjectCharacteristicsWindow>();
        services.AddTransient<ProjectCharacteristicEditViewModel>();
        services.AddTransient<ProjectCharacteristicEditWindow>();
        services.AddSingleton<IProjectCharacteristicEditDialogService, ProjectCharacteristicEditDialogService>();
        services.AddTransient<ProjectCharacteristicsDialogViewModel>();
        services.AddTransient<ProjectCharacteristicsDialog>();
        services.AddSingleton<IProjectCharacteristicsDialogService, ProjectCharacteristicsDialogService>();
        services.AddTransient<ProjectCharacteristicSelectionEditViewModel>();
        services.AddTransient<ProjectCharacteristicSelectionEditWindow>();
        services.AddSingleton<IProjectCharacteristicSelectionEditDialogService, ProjectCharacteristicSelectionEditDialogService>();
        services.AddSingleton<IicWorkDirectionsViewModel>();
        services.AddTransient<IicWorkDirectionsWindow>();
        services.AddTransient<IicWorkDirectionEditViewModel>();
        services.AddTransient<IicWorkDirectionEditWindow>();
        services.AddSingleton<IIicWorkDirectionEditDialogService, IicWorkDirectionEditDialogService>();
        services.AddSingleton<ProjectAnalysisVersionsViewModel>();
        services.AddTransient<ProjectAnalysisVersionsWindow>();
        services.AddTransient<ProjectAnalysisVersionEditViewModel>();
        services.AddTransient<ProjectAnalysisVersionEditWindow>();
        services.AddSingleton<IProjectAnalysisVersionEditDialogService, ProjectAnalysisVersionEditDialogService>();
        services.AddTransient<ProjectSelectionDialogViewModel>();
        services.AddTransient<ProjectSelectionDialog>();
        services.AddSingleton<IProjectSelectionDialogService, ProjectSelectionDialogService>();
        services.AddTransient<ProjectVersionPickerDialogViewModel>();
        services.AddTransient<ProjectVersionPickerDialog>();
        services.AddSingleton<IProjectVersionPickerDialogService, ProjectVersionPickerDialogService>();
        services.AddSingleton<IicWorkDirectionNormsViewModel>();
        services.AddTransient<IicWorkDirectionNormsWindow>();
        services.AddTransient<IicWorkDirectionNormEditViewModel>();
        services.AddTransient<IicWorkDirectionNormEditWindow>();
        services.AddSingleton<IIicWorkDirectionNormEditDialogService, IicWorkDirectionNormEditDialogService>();
        services.AddTransient<ProjectParticipantEditViewModel>();
        services.AddTransient<ProjectParticipantEditWindow>();
        services.AddSingleton<IProjectParticipantEditDialogService, ProjectParticipantEditDialogService>();
        services.AddSingleton<ProjectPhysicalVolumesViewModel>();
        services.AddSingleton<ProjectLaborIntensitiesViewModel>();
        services.AddTransient<ProjectPhysicalVolumesWindow>();
        services.AddTransient<ProjectLaborIntensitiesWindow>();

        services.AddSingleton<ProjectDashboardViewModel>();
        services.AddTransient<ProjectDashboardView>();

        services.AddSingleton<ProjectsPageViewModel>();
        services.AddSingleton<ProjectsPageView>();

        services.AddSingleton<AssessmentFormViewModel>();
        services.AddSingleton<PlanningFormViewModel>();
        services.AddTransient<ProjectDataUploadForm>();
        services.AddTransient<AssessmentForm>();
        services.AddTransient<PlanningForm>();
        services.AddTransient<VersionComparisonForm>();
        services.AddTransient<GroupAnalysisForm>();
        services.AddTransient<GroupTrendForm>();

        services.AddTransient<ReferencesPageView>();
        services.AddSingleton<ReferencesPageViewModel>();

        services.AddTransient<DataPageView>();
        services.AddSingleton<DataPageViewModel>();

        services.AddTransient<SettingsPageView>();
        services.AddSingleton<SettingsPageViewModel>();

        services.AddTransient<AccessRightsWindow>();
        services.AddSingleton<AccessRightsViewModel>();

        services.AddTransient<UserActionLogView>();
        services.AddSingleton<UserActionLogViewModel>();

        services.AddTransient<InterpretationSettingsView>();
        services.AddSingleton<InterpretationSettingsViewModel>();

        services.AddTransient<ProjectVersionAnalysisWindow>();
        services.AddTransient<ProjectVersionAnalysisViewModel>();
        services.AddTransient<ProjectForecastReportWindow>();
        services.AddTransient<ProjectForecastReportViewModel>();

        services.AddTransient<DataImportDialogViewModel>();
        services.AddTransient<DataImportDialog>();
        services.AddSingleton<IDataImportDialogService, DataImportDialogService>();

        services.AddSingleton<IUserSettingsService, UserSettingsService>();
        services.AddSingleton<UserSettingsHelper>();

        services.AddTransient<IterationDeleteDialogViewModel>();
        services.AddTransient<IterationDeleteDialog>();
        services.AddSingleton<IIterationDeleteDialogService, IterationDeleteDialogService>();

        services.AddTransient<ProjectPhysicalVolumeQuickEditViewModel>();
        services.AddTransient<ProjectPhysicalVolumeQuickEditDialog>();
        services.AddTransient<PhysicalVolumeQuickEditViewModel>();
        services.AddTransient<PhysicalVolumeQuickEditDialog>();
        services.AddTransient<IicNormQuickEditViewModel>();
        services.AddTransient<IicNormQuickEditDialog>();
    }

    private static void ConfigureSerilog()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File("logs/engineering-performance-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Debug()
            .CreateLogger();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            try
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Host stop failed during exit");
            }
            finally
            {
                _host.Dispose();
            }
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
