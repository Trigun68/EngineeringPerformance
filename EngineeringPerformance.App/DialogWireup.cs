using System.Windows;
using DevExpress.Xpf.Core;
using EngineeringPerformance.App.Services;
using EngineeringPerformance.App.ViewModels;
using EngineeringPerformance.App.Views;
using EngineeringPerformance.Core.Services;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace EngineeringPerformance.App;

/// <summary>
/// Связывает singleton ViewModels со своими dialog services и кешами справочников.
/// Раньше эта логика жила прямо в App.xaml.cs ~100 строками императивных лямбд —
/// после рефакторинга вся склейка собрана здесь по единому паттерну и регистрирует
/// Reset-обработчики для очистки состояния VM при закрытии главного окна (A3).
/// </summary>
internal sealed class DialogWireup
{
    private readonly IServiceProvider _services;
    private readonly Window _owner;
    private readonly List<Action> _resetActions = [];

    public DialogWireup(IServiceProvider services, Window owner)
    {
        _services = services;
        _owner = owner;
    }

    public void Apply()
    {
        WireSettingsHelper();
        WireUsers();
        WireProjects();
        WirePhysicalVolumes();
        WirePhysicalVolumeTypes();
        WireIicWorkDirections();
        WireProjectAnalysisVersions();
        WireUnitsOfMeasurement();
        WireIicWorkDirectionNorms();
        WireLaborIntensities();
        WireProjectCharacteristics();
    }

    /// <summary>Регистрирует Reset-обработчики на закрытие главного окна (A3).</summary>
    public void RegisterResetOnClose()
    {
        _owner.Closed += (_, _) =>
        {
            foreach (var action in _resetActions)
            {
                try { action(); } catch { /* shutting down */ }
            }
        };
    }

    private void WireSettingsHelper()
    {
        var helper = _services.GetRequiredService<UserSettingsHelper>();

        _services.GetRequiredService<UsersViewModel>().SettingsHelper = helper;
        _services.GetRequiredService<ProjectsViewModel>().SettingsHelper = helper;
        _services.GetRequiredService<PhysicalVolumesViewModel>().SettingsHelper = helper;
        _services.GetRequiredService<PhysicalVolumeTypesViewModel>().SettingsHelper = helper;
        _services.GetRequiredService<IicWorkDirectionsViewModel>().SettingsHelper = helper;
        _services.GetRequiredService<ProjectAnalysisVersionsViewModel>().SettingsHelper = helper;
        _services.GetRequiredService<UnitOfMeasurementsViewModel>().SettingsHelper = helper;
        _services.GetRequiredService<LaborIntensitiesViewModel>().SettingsHelper = helper;
        _services.GetRequiredService<ProjectCharacteristicsViewModel>().SettingsHelper = helper;

        var pageView = _services.GetRequiredService<Views.ProjectsPageView>();
        pageView.SetSettingsHelper(helper);
    }

    private void WireUsers()
    {
        var svc = _services.GetRequiredService<IUserEditDialogService>();
        svc.SetOwnerWindow(_owner);
        var vm = _services.GetRequiredService<UsersViewModel>();
        vm.OwnerWindow = _owner;
        vm.OpenEditWindow = async user =>
        {
            if (!await svc.ShowEditDialogAsync(user))
                return;

            if (user?.Id > 0)
                await vm.RefreshItemFromServiceAsync(user.Id);
            else
                await vm.ReloadFromDatabaseAsync();
        };
        _resetActions.Add(vm.Reset);
    }

    private void WireProjects()
    {
        var svc = _services.GetRequiredService<IProjectEditDialogService>();
        svc.SetOwnerWindow(_owner);
        var vm = _services.GetRequiredService<ProjectsViewModel>();
        vm.OwnerWindow = _owner;
        vm.OpenEditWindow = async project =>
        {
            var projectId = project?.Id ?? 0;
            await svc.ShowEditDialogAsync(project);
            if (projectId > 0) await vm.RefreshItemFromServiceAsync(projectId);
            else await vm.ReloadFromDatabaseAsync();
        };
        _resetActions.Add(vm.Reset);
    }

    private void WirePhysicalVolumes()
    {
        var svc = _services.GetRequiredService<IPhysicalVolumeEditDialogService>();
        svc.SetOwnerWindow(_owner);
        var vm = _services.GetRequiredService<PhysicalVolumesViewModel>();
        vm.OwnerWindow = _owner;
        vm.OpenEditWindow = async item =>
        {
            var savedId = await svc.ShowEditDialogAsync(item);
            if (savedId is int id)
                await vm.RefreshSingleItemAsync(id);
        };
        _resetActions.Add(vm.Reset);
    }

    private void WirePhysicalVolumeTypes()
    {
        var svc = _services.GetRequiredService<IPhysicalVolumeTypeEditDialogService>();
        svc.SetOwnerWindow(_owner);
        var cache = _services.GetRequiredService<IDirectoryCache<PhysicalVolumeType>>();
        var vm = _services.GetRequiredService<PhysicalVolumeTypesViewModel>();
        vm.OwnerWindow = _owner;
        vm.OpenEditWindow = async item =>
        {
            if (await svc.ShowEditDialogAsync(item))
            {
                cache.Invalidate();
                await vm.ReloadFromDatabaseAsync();
            }
        };
        _resetActions.Add(vm.Reset);
    }

    private void WireIicWorkDirections()
    {
        var svc = _services.GetRequiredService<IIicWorkDirectionEditDialogService>();
        svc.SetOwnerWindow(_owner);
        var cache = _services.GetRequiredService<IDirectoryCache<IicWorkDirection>>();
        var vm = _services.GetRequiredService<IicWorkDirectionsViewModel>();
        vm.OwnerWindow = _owner;
        vm.OpenEditWindow = async item =>
        {
            if (await svc.ShowEditDialogAsync(item))
            {
                cache.Invalidate();
                await vm.ReloadFromDatabaseAsync();
            }
        };
        _resetActions.Add(vm.Reset);
    }

    private void WireProjectAnalysisVersions()
    {
        var svc = _services.GetRequiredService<IProjectAnalysisVersionEditDialogService>();
        svc.SetOwnerWindow(_owner);
        var vm = _services.GetRequiredService<ProjectAnalysisVersionsViewModel>();
        vm.OwnerWindow = _owner;
        vm.OpenEditWindow = async (version, preselectedProjectId) =>
        {
            if (await svc.ShowEditDialogAsync(version, preselectedProjectId))
                await vm.ReloadFromDatabaseAsync();
        };
        _resetActions.Add(vm.Reset);
    }

    private void WireUnitsOfMeasurement()
    {
        var svc = _services.GetRequiredService<IUnitOfMeasurementEditDialogService>();
        svc.SetOwnerWindow(_owner);
        var cache = _services.GetRequiredService<IDirectoryCache<UnitOfMeasurement>>();
        var vm = _services.GetRequiredService<UnitOfMeasurementsViewModel>();
        vm.OwnerWindow = _owner;
        vm.OpenEditWindow = async item =>
        {
            if (await svc.ShowEditDialogAsync(item))
            {
                cache.Invalidate();
                await vm.ReloadFromDatabaseAsync();
            }
        };
        _resetActions.Add(vm.Reset);
    }

    private void WireIicWorkDirectionNorms()
    {
        var svc = _services.GetRequiredService<IIicWorkDirectionNormEditDialogService>();
        svc.SetOwnerWindow(_owner);
        var vm = _services.GetRequiredService<IicWorkDirectionNormsViewModel>();
        vm.OwnerWindow = _owner;
        vm.OpenEditWindow = async (item, preselectedWorkDirectionId) =>
            await svc.ShowEditDialogAsync(item, preselectedWorkDirectionId);
        _resetActions.Add(vm.Reset);
    }

    private void WireLaborIntensities()
    {
        var svc = _services.GetRequiredService<ILaborIntensityEditDialogService>();
        svc.SetOwnerWindow(_owner);
        var vm = _services.GetRequiredService<LaborIntensitiesViewModel>();
        vm.OwnerWindow = _owner;
        vm.OpenEditWindow = async item =>
        {
            if (await svc.ShowEditDialogAsync(item))
                await vm.ReloadFromDatabaseAsync();
        };
        _resetActions.Add(vm.Reset);
    }

    private void WireProjectCharacteristics()
    {
        var svc = _services.GetRequiredService<IProjectCharacteristicEditDialogService>();
        svc.SetOwnerWindow(_owner);

        // Owner для модалок выбора характеристик из ProjectEditViewModel.SetDialogOwner.
        _services.GetRequiredService<IProjectCharacteristicsDialogService>().SetOwnerWindow(_owner);
        _services.GetRequiredService<IProjectCharacteristicSelectionEditDialogService>().SetOwnerWindow(_owner);

        var vm = _services.GetRequiredService<ProjectCharacteristicsViewModel>();
        vm.OwnerWindow = _owner;
        vm.OpenEditWindow = async item =>
        {
            if (await svc.ShowEditDialogAsync(item))
                await vm.ReloadFromDatabaseAsync();
        };
        _resetActions.Add(vm.Reset);
    }
}
