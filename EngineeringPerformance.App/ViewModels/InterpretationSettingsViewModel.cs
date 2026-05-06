using CommunityToolkit.Mvvm.ComponentModel;

namespace EngineeringPerformance.App.ViewModels;

/// <summary>
/// VM справочной вкладки «Интерпретация» — содержимое статическое, без привязок к данным.
/// Нужен, чтобы SettingsPageViewModel мог единообразно разрешать типы через DI.
/// </summary>
public sealed partial class InterpretationSettingsViewModel : ObservableObject
{
}
