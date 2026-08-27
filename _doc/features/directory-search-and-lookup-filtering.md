# Поиск в справочниках и фильтрация lookup-комбобоксов

**Статус:** ✅ Реализовано
**Последнее обновление:** 2026-05-11
**Связанные коммиты:** `19a4317`, `00dee2e`, `7a97ceb`, `6b98b07`, `1d976be`, рефакторинг в `Style x:Key="LookupComboBoxEdit"`

## Что это и зачем

В приложении два механизма быстрого поиска в больших справочниках:

1. **Поисковая строка над таблицей справочника** — для самих окон-списков (Номенклатура ФО, Трудоёмкости, Единицы измерений, Группы физ. объёмов IIC, Нормы групп IIC).
2. **Фильтрация-по-набору в lookup-комбобоксах** — для полей в карточках редактирования и в диалоге создания/копирования строки прогнозирования.

Цель — не заставлять пользователя прокручивать списки на тысячи позиций; вместо этого он печатает любой фрагмент кода или наименования и сразу видит совпадения.

## Руководство пользователя

### Поиск в справочниках

Открой любой справочник из меню (Номенклатура ФО, Трудоёмкости, Единицы измерений, Группы физ. объёмов IIC, Нормы групп IIC). Над таблицей видна поисковая строка.

- Печатай фрагмент кода или наименования — записи фильтруются по всем видимым колонкам.
- Найденные совпадения подсвечиваются жёлтым фоном.
- Сочетание `Ctrl+F` тоже открывает поиск (стандартное поведение DevExpress).
- Очистка строки — крестик справа или `Esc`.

### Фильтрация в lookup-полях карточек

В следующих местах поля выбора из справочника поддерживают ввод текста с инкрементной фильтрацией:

| Окно | Поля |
|---|---|
| Карточка «Номенклатура ФО» (`PhysicalVolumeEditWindow`) | Классификатор, Родитель, Ед. изм. |
| Быстрая карточка «Номенклатура ФО» (`PhysicalVolumeQuickEditDialog`) | Классификатор |
| Карточка «Трудоёмкость» (`LaborIntensityEditWindow`) | Родитель, Группа IIC |
| Карточка «Группа IIC» (`IicWorkDirectionEditWindow`) | Ед. измерения, Родитель |
| Карточка «Норма IIC» (`IicWorkDirectionNormEditWindow`) | Группа |
| Прогнозирование → Детально → ПКМ → Добавить копированием / Добавить строку (`ProjectPhysicalVolumeCreateDialog`) | Номенклатура ФО |

**Как пользоваться:**
1. Кликни в поле — popup открывается сразу (`ImmediatePopup="True"`).
2. Печатай **любую подстроку** искомого значения (поиск по `Contains`, не `StartsWith`).
3. В popup остаются только записи, чей текст содержит набранное.
4. Стрелками ↑/↓ или мышью выбери нужный пункт.

Поиск нечувствителен к регистру. Декоративные символы иерархии (`└`, отступы) игнорируются — печатай только сам код или название.

## Руководство админа

Отдельных операций для администратора эта функциональность не требует — она встроена в существующие справочники и активна для всех пользователей. Дополнительная конфигурация не нужна.

## Техническая документация

### Архитектура

- **Поиск над таблицей справочника**: на `dxg:TreeListView` / `dxg:TableView` включены `ShowSearchPanelMode="Always"` и `SearchPanelHighlightResults="True"`. Это нативный механизм DevExpress, кода в VM не требуется.
- **Фильтрация в lookup-комбобоксах**: на `dxe:ComboBoxEdit` применяется именованный стиль `LookupComboBoxEdit` из `App.xaml`, который инкапсулирует 5 атрибутов поведения. Уникальные биндинги (`ItemsSource`, `EditValue`/`SelectedItem`, `ValueMember`, `DisplayMember`, `NullText`) задаются на месте:

```xml
<dxe:ComboBoxEdit ItemsSource="{Binding Items}"
                  EditValue="{Binding SelectedId}"
                  ValueMember="Id"
                  DisplayMember="DisplayText"
                  Style="{StaticResource LookupComboBoxEdit}"
                  NullText="Не выбрано" />
```

Определение стиля в `App.xaml`:

```xml
<Style x:Key="LookupComboBoxEdit" TargetType="dxe:ComboBoxEdit">
    <Setter Property="IncrementalFiltering" Value="True" />
    <Setter Property="FilterCondition" Value="Contains" />
    <Setter Property="AutoComplete" Value="True" />
    <Setter Property="ImmediatePopup" Value="True" />
    <Setter Property="ValidateOnTextInput" Value="False" />
</Style>
```

### Зачем 5 атрибутов в стиле

| Атрибут | Назначение |
|---|---|
| `IncrementalFiltering="True"` | Включает фильтрацию popup-списка по вводимому тексту. |
| `FilterCondition="Contains"` | Фильтр по подстроке вместо дефолтного `StartsWith`. Критично для иерархических `Display`-строк с декоративными префиксами (отступы, `└ `). |
| `AutoComplete="True"` | Авто-дополнение остатка строки до первого совпадения. По [DevExpress docs](https://docs.devexpress.com/WPF/400504): при определённых `DisplayMember`+`ValueMember` без `AutoComplete` редактор разрешает только выбор из коллекции. |
| `ImmediatePopup="True"` | Popup открывается при клике/фокусе, не дожидаясь нажатия стрелки. |
| `ValidateOnTextInput="False"` | Снимает валидацию ввода на принадлежность коллекции. Без этого редактор отклоняет любой текст, не совпадающий с item-ом, и набираемые символы не отображаются. |

### Анти-паттерны (что **не** работает)

1. **Фейковый item «Не выбрано» с `Id=null` в `ItemsSource` + `SelectedItem`-binding.** Связка с `IncrementalFiltering` + `AutoComplete` блокирует ввод текста — редактор остаётся в режиме «выбран фейковый элемент». Решение: использовать паттерн `EditValue` + `ValueMember` + `NullText`.
   - Прецедент: `ProjectPhysicalVolumeCreateViewModel.BuildPhysicalVolumeDisplayItems` до коммита `00dee2e`.
2. **`IsTextEditable="False"` на больших справочниках.** Полностью блокирует ввод текста — пользователь вынужден прокручивать тысячи строк.
3. **Дефолтный `FilterCondition` (=`StartsWith`) с декоративными префиксами в `Display`.** Items на `depth>0` начинаются с пробелов или `└ `, поэтому ввод любой буквы не находит совпадений у вложенных элементов. Решение: `FilterCondition="Contains"`.

### Где НЕ применяется

Enum-комбобоксы (2-5 значений) намеренно не получили этот паттерн — фильтрация бесполезна:
- «Тип», «Оценка» в `PhysicalVolumeEditWindow`
- «Оценка» в `PhysicalVolumeQuickEditDialog`
- «Статус работы» в `ProjectDataDialog` / `ProjectEditWindow`
- «Характеристика», «Вариант» в `ProjectCharacteristicSelectionEditWindow`
- Фильтры по типу действия в `UserActionLogView`

### Файлы

**XAML с применённым паттерном:**
- `EngineeringPerformance.App/Views/PhysicalVolumesWindow.xaml` (поиск-над-таблицей)
- `EngineeringPerformance.App/Views/LaborIntensitiesWindow.xaml` (поиск-над-таблицей)
- `EngineeringPerformance.App/Views/UnitOfMeasurementsWindow.xaml` (поиск-над-таблицей)
- `EngineeringPerformance.App/Views/IicWorkDirectionsWindow.xaml` (поиск-над-таблицей)
- `EngineeringPerformance.App/Views/IicWorkDirectionNormsWindow.xaml` (поиск-над-таблицей)
- `EngineeringPerformance.App/Views/PhysicalVolumeEditWindow.xaml` (3 поля)
- `EngineeringPerformance.App/Views/PhysicalVolumeQuickEditDialog.xaml` (1 поле)
- `EngineeringPerformance.App/Views/LaborIntensityEditWindow.xaml` (2 поля)
- `EngineeringPerformance.App/Views/IicWorkDirectionEditWindow.xaml` (2 поля)
- `EngineeringPerformance.App/Views/IicWorkDirectionNormEditWindow.xaml` (1 поле)
- `EngineeringPerformance.App/Views/ProjectPhysicalVolumeCreateDialog.xaml` (1 поле)

**ViewModel-изменения:**
- `EngineeringPerformance.App/ViewModels/ProjectPhysicalVolumeCreateViewModel.cs` — убран фейковый item «Не выбрано», `PhysicalVolumeDisplayItem.Id` переведён `int? → int`, удалены `SelectedPhysicalVolumeItem` и `OnSelectedPhysicalVolumeItemChanged`.

### Источники

- [DevExpress WPF — Filtering](https://docs.devexpress.com/WPF/400504)
- [DevExpress WPF — ComboBoxEdit Class](https://docs.devexpress.com/WPF/DevExpress.Xpf.Editors.ComboBoxEdit)
- [LookUpEditBase.IncrementalFiltering Property](https://docs.devexpress.com/WPF/DevExpress.Xpf.Editors.LookUpEditBase.IncrementalFiltering)
