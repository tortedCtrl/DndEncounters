using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;

namespace DndEncounters;

public partial class MainWindow : Window
{
    private readonly AppDataStore _store = new();
    private List<Monster> _monsters = [];
    private List<Hero> _heroes = [];
    private List<Encounter> _encounters = [];
    private readonly Dictionary<string, Control> _monsterFields = [];
    private Monster? _editingMonster;
    private Encounter? _editingEncounter;
    private List<EncounterMember> _draftHeroes = [];
    private List<EncounterMember> _draftMonsters = [];
    private List<string> _pendingImages = [];
    private int _openMenu = -1;
    private bool _isLightTheme;
    private string? _startupError;
    private TextBlock? _status;
    private TextBlock? _challengeHints;

    private static readonly string[] MenuNames = ["Создание", "Коллекция", "Библиотека"];
    private static readonly string[][] MenuItems =
    [
        ["Монстр", "Персонаж", "Энкаунтер", "Магический предмет", "Предыстория",
            "Черта", "Раса", "Заклинание", "Класс", "Подкласс", "Кампания"],
        ["Монстр", "Персонаж", "Энкаунтер", "Магический предмет", "Предыстория",
            "Черта", "Раса", "Заклинание", "Класс", "Подкласс", "Кампания"],
        ["Бестиарий", "Список заклинаний", "Магические предметы", "Черты", "Расы", "Классы"]
    ];

    public MainWindow()
    {
        InitializeComponent();
        ThemeButton.Click += (_, _) => ToggleTheme();
        try
        {
            _monsters = _store.Load<Monster>("monsters.json");
            _heroes = _store.Load<Hero>("heroes.json");
            _encounters = _store.Load<Encounter>("encounters.json");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            _startupError = $"Не удалось загрузить данные: {exception.Message}";
        }

        BuildNavigation();
        ShowHome();
    }

    private void BuildNavigation()
    {
        NavigationPanel.Children.Clear();
        var homeButton = new Button { Content = "Главная", Classes = { "nav" } };
        homeButton.Click += (_, _) =>
        {
            _openMenu = -1;
            BuildMenu();
            ShowHome();
        };
        NavigationPanel.Children.Add(homeButton);
        for (var index = 0; index < MenuNames.Length; index++)
        {
            var menuIndex = index;
            var button = new Button
            {
                Content = MenuNames[index],
                Classes = { "nav" }
            };
            button.Click += (_, _) =>
            {
                _openMenu = _openMenu == menuIndex ? -1 : menuIndex;
                if (_openMenu >= 0)
                    _lastMenuIndex = menuIndex;
                BuildMenu();
            };
            NavigationPanel.Children.Add(button);
        }
        BuildMenu();
    }

    private void BuildMenu()
    {
        MenuPanel.Children.Clear();
        MenuPanel.IsVisible = _openMenu >= 0;
        if (_openMenu < 0)
            return;

        foreach (var item in MenuItems[_openMenu])
        {
            var available = _openMenu switch
            {
                0 => item is "Монстр" or "Энкаунтер",
                1 => item is "Монстр" or "Энкаунтер",
                _ => false
            };
            var button = new Button
            {
                Content = item,
                Classes = { available ? "quiet" : "nav" },
                IsEnabled = available,
                Opacity = available ? 1 : 0.45
            };
            if (available)
            {
                button.Click += (_, _) =>
                {
                    _openMenu = -1;
                    BuildMenu();
                    if (_lastMenuIndex == 0)
                    {
                        if (item == "Монстр")
                            ShowMonsterCreationChoice();
                        else
                            ShowEncounterEditor(null);
                    }
                    else if (item == "Монстр")
                        ShowMonsterCollection();
                    else
                        ShowEncounterCollection();
                };
            }
            MenuPanel.Children.Add(button);
        }
    }

    private int _lastMenuIndex;

    private void ToggleTheme()
    {
        _isLightTheme = !_isLightTheme;
        var palette = _isLightTheme
            ? new Dictionary<string, string>
            {
                ["WindowBrush"] = "#F2F1ED", ["HeaderBrush"] = "#E6E4DD", ["CardBrush"] = "#FFFFFF",
                ["InputBrush"] = "#F7F7F4", ["RaisedBrush"] = "#ECEAE3", ["TextBrush"] = "#20232A",
                ["MutedBrush"] = "#676B74", ["InputTextBrush"] = "#20232A", ["BorderBrush"] = "#D2D0C9",
                ["AccentBrush"] = "#D7A54E"
            }
            : new Dictionary<string, string>
            {
                ["WindowBrush"] = "#10131B", ["HeaderBrush"] = "#151923", ["CardBrush"] = "#191E29",
                ["InputBrush"] = "#171B25", ["RaisedBrush"] = "#202633", ["TextBrush"] = "#E9E7E2",
                ["MutedBrush"] = "#9299A6", ["InputTextBrush"] = "#F2F0EA", ["BorderBrush"] = "#303746",
                ["AccentBrush"] = "#C69A53"
            };

        foreach (var entry in palette)
            Resources[entry.Key] = Brush.Parse(entry.Value);
        Background = Brush.Parse(palette["WindowBrush"]);
        if (Application.Current is { } application)
            application.RequestedThemeVariant = _isLightTheme ? ThemeVariant.Light : ThemeVariant.Dark;
        ThemeButton.Content = _isLightTheme ? "Тёмная тема" : "Светлая тема";
    }

    private void ShowHome()
    {
        var page = NewPage();
        page.Children.Add(Heading("Ваши приключения", "Создавайте монстров и собирайте боевые сцены."));
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        buttons.Children.Add(ActionButton("Создать монстра", ShowMonsterCreationChoice, primary: true));
        buttons.Children.Add(ActionButton("Создать энкаунтер", () => ShowEncounterEditor(null)));
        buttons.Children.Add(ActionButton("Коллекция монстров", () => ShowMonsterCollection()));
        buttons.Children.Add(ActionButton("Энкаунтеры", () => ShowEncounterCollection()));
        page.Children.Add(buttons);
        page.Children.Add(Card(
            $"В коллекции: {_monsters.Count} монстров · {_encounters.Count} энкаунтеров · {_heroes.Count} героев",
            "Все изменения сохраняются локально в папке данных приложения."));
        if (_startupError is not null)
            page.Children.Add(new TextBlock { Text = _startupError, Foreground = Brush.Parse("#E58C7A"), TextWrapping = TextWrapping.Wrap });
        SetPage(page);
    }

    private void ShowMonsterCreationChoice()
    {
        var page = NewPage();
        page.Children.Add(Heading("Создание монстра", "Начните с пустой формы или возьмите за основу монстра из коллекции."));
        page.Children.Add(ActionButton("Создать с нуля", () => ShowMonsterEditor(null), primary: true));
        if (_monsters.Count == 0)
        {
            page.Children.Add(Card("Коллекция пока пуста", "Создайте первого монстра с нуля."));
        }
        else
        {
            var selector = Select(_monsters.Select(monster => $"{monster.Name} · опасность {monster.ChallengeRating}"),
                "Выберите монстра для копирования");
            page.Children.Add(selector);
            page.Children.Add(ActionButton("Создать на основе выбранного", () =>
            {
                if (selector.SelectedIndex < 0 || selector.SelectedIndex >= _monsters.Count)
                {
                    ShowStatus("Выберите монстра из списка.", isError: true);
                    return;
                }
                ShowMonsterEditor(CloneMonster(_monsters[selector.SelectedIndex]));
            }));
        }
        _status = new TextBlock { IsVisible = false, TextWrapping = TextWrapping.Wrap, FontSize = 13 };
        page.Children.Add(_status);
        SetPage(page);
    }

    private static Monster CloneMonster(Monster monster) => new()
    {
        Id = Guid.NewGuid(),
        Name = $"{monster.Name} (копия)",
        Size = monster.Size,
        Type = monster.Type,
        Alignment = monster.Alignment,
        Description = monster.Description,
        ChallengeRating = monster.ChallengeRating,
        ArmorClass = monster.ArmorClass,
        ArmorType = monster.ArmorType,
        AverageHitPoints = monster.AverageHitPoints,
        HitDiceCount = monster.HitDiceCount,
        HitDie = monster.HitDie,
        HitPointModifier = monster.HitPointModifier,
        Abilities = new Dictionary<string, int>(monster.Abilities),
        Speed = monster.Speed,
        InitiativeBonus = monster.InitiativeBonus,
        PassivePerception = monster.PassivePerception,
        SavingThrows = monster.SavingThrows.ToList(),
        Skills = monster.Skills.ToList(),
        Vulnerabilities = monster.Vulnerabilities,
        Resistances = monster.Resistances,
        DamageImmunities = monster.DamageImmunities,
        ConditionImmunities = monster.ConditionImmunities,
        Senses = monster.Senses,
        Languages = monster.Languages,
        Habitat = monster.Habitat,
        Traits = monster.Traits,
        Actions = monster.Actions,
        BonusActions = monster.BonusActions,
        Reactions = monster.Reactions,
        LegendaryActions = monster.LegendaryActions,
        Lair = monster.Lair,
        ImagePaths = monster.ImagePaths.ToList(),
        SelectedImageIndex = monster.SelectedImageIndex
    };

    private StackPanel NewPage()
    {
        _status = null;
        _challengeHints = null;
        var page = new StackPanel
        {
            MaxWidth = 1080,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 20,
            Margin = new Thickness(32, 28, 32, 40)
        };
        var scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = page
        };
        return PageStackProxy(page, scroll);
    }

    private void SetPage(StackPanel page) => ContentHost.Content = page.Tag as Control ?? page;

    private static StackPanel PageStackProxy(StackPanel panel, Control scroll)
    {
        panel.Tag = scroll;
        return panel;
    }

    private void ShowStatus(string message, bool isError = false)
    {
        if (_status is null)
            return;
        _status.Text = message;
        _status.Foreground = Brush.Parse(isError ? "#E58C7A" : "#9ACB9A");
        _status.IsVisible = true;
    }

    private StackPanel Heading(string title, string subtitle)
    {
        var block = new StackPanel { Spacing = 5 };
        block.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 30,
            FontWeight = FontWeight.Bold
        });
        block.Children.Add(new TextBlock
        {
            Text = subtitle,
            FontSize = 14,
            Foreground = Brush.Parse(_isLightTheme ? "#676B74" : "#9299A6"),
            TextWrapping = TextWrapping.Wrap
        });
        return block;
    }

    private Border Card(string title, string? subtitle = null)
    {
        var stack = new StackPanel { Spacing = 7 };
        stack.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeight.SemiBold });
        if (!string.IsNullOrWhiteSpace(subtitle))
            stack.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = 13,
                Foreground = Brush.Parse(_isLightTheme ? "#676B74" : "#9299A6"),
                TextWrapping = TextWrapping.Wrap
            });
        var border = new Border
        {
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(12),
            Child = stack
        };
        border.Classes.Add("dynamic-card");
        border.BorderThickness = new Thickness(1);
        return border;
    }

    private Button ActionButton(string text, Action action, bool primary = false)
    {
        var button = new Button { Content = text, Classes = { primary ? "primary" : "quiet" } };
        button.Click += (_, _) => action();
        return button;
    }

    private StackPanel Label(string text, bool required = false)
    {
        var label = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3 };
        label.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse(_isLightTheme ? "#3D4149" : "#C7CAD1")
        });
        if (required)
            label.Children.Add(new TextBlock
            {
                Text = "*",
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                Foreground = Brush.Parse("#E58C7A")
            });
        return label;
    }

    private StackPanel Field(string title, Control input, bool required = false)
    {
        var field = new StackPanel { Spacing = 7 };
        field.Children.Add(Label(title, required));
        field.Children.Add(input);
        return field;
    }

    private TextBox Input(string watermark = "", bool multiline = false, string? text = null)
    {
        var input = new TextBox
        {
            Watermark = watermark,
            Text = text ?? "",
            AcceptsReturn = multiline,
            TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            MinHeight = multiline ? 90 : 42,
            VerticalContentAlignment = multiline ? VerticalAlignment.Top : VerticalAlignment.Center
        };
        return input;
    }

    private ComboBox Select(IEnumerable<string> values, string placeholder)
    {
        var combo = new ComboBox
        {
            ItemsSource = values.ToArray(),
            PlaceholderText = placeholder,
            MinHeight = 42,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        return combo;
    }

    private Border Section(string title, string subtitle)
    {
        var content = new StackPanel { Spacing = 14 };
        content.Children.Add(new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeight.SemiBold });
        content.Children.Add(new TextBlock
        {
            Text = subtitle,
            FontSize = 12,
            Foreground = Brush.Parse(_isLightTheme ? "#676B74" : "#8992A1"),
            TextWrapping = TextWrapping.Wrap
        });
        var border = new Border
        {
            Padding = new Thickness(22),
            CornerRadius = new CornerRadius(14),
            BorderThickness = new Thickness(1),
            Child = content
        };
        border.Classes.Add("dynamic-card");
        return border;
    }

    private void ShowMonsterEditor(Monster? monster)
    {
        _editingMonster = monster;
        _pendingImages = [];
        _monsterFields.Clear();
        var page = NewPage();
        page.Children.Add(Heading(monster is null ? "Создать монстра" : $"Редактировать: {monster.Name}",
            "Пустые значения рекомендаций будут рассчитаны по таблице DMG 2014, стр. 274."));
        var overview = Section("Основное", "Поля со звёздочкой обязательны.");
        var main = (StackPanel)overview.Child!;
        var mainGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("2*,*,*"), RowDefinitions = new RowDefinitions("Auto,Auto"), ColumnSpacing = 12, RowSpacing = 14 };
        var name = AddTextField(mainGrid, "Имя", "Например, древний красный дракон", "Name", true, 0, 0, monster?.Name);
        var size = AddComboField(mainGrid, "Размер", ["Крошечный", "Маленький", "Средний", "Большой", "Огромный", "Гигантский"], "Выберите размер", "Size", true, 1, 0, monster?.Size);
        var type = AddComboField(mainGrid, "Тип", ["Аберрация", "Зверь", "Небожитель", "Конструкт", "Дракон", "Элементаль", "Фея", "Исчадие", "Великан", "Гуманоид", "Монстр", "Слизь", "Растение", "Нежить"], "Выберите тип", "Type", true, 2, 0, monster?.Type);
        AddComboField(mainGrid, "Мировоззрение",
            ["Законно-доброе", "Нейтрально-доброе", "Хаотично-доброе", "Законно-нейтральное", "Нейтральное", "Хаотично-нейтральное", "Законно-злое", "Нейтрально-злое", "Хаотично-злое", "Без мировоззрения"],
            "Выберите мировоззрение", "Alignment", false, 0, 1, monster?.Alignment);
        main.Children.Add(mainGrid);
        page.Children.Add(overview);

        var challenge = Section("Опасность, защита и хиты",
            "Рекомендации соответствуют таблице характеристик монстров DMG 2014, стр. 274.");
        var challengeBody = (StackPanel)challenge.Child!;
        _challengeHints = new TextBlock
        {
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush.Parse(_isLightTheme ? "#676B74" : "#9299A6")
        };
        challengeBody.Children.Add(_challengeHints);
        var challengeGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*"), ColumnSpacing = 12 };
        var cr = AddComboField(challengeGrid, "Опасность", MonsterRules.ChallengeRatings, "Выберите опасность", "ChallengeRating", true, 0, 0, monster?.ChallengeRating ?? "0");
        AddTextField(challengeGrid, "Класс доспеха", "Рекомендуется 13", "ArmorClass", true, 1, 0, monster?.ArmorClass?.ToString());
        AddTextField(challengeGrid, "Тип защиты", "Например, природный доспех", "ArmorType", false, 2, 0, monster?.ArmorType);
        challengeBody.Children.Add(challengeGrid);
        var hpGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("3*,*,*,*"), ColumnSpacing = 12, Margin = new Thickness(0, 12, 0, 0) };
        AddTextField(hpGrid, "Среднее количество хитов", "Рекомендуется 4", "AverageHitPoints", true, 0, 0, monster?.AverageHitPoints?.ToString());
        AddTextField(hpGrid, "Количество костей", "Например, 1", "HitDiceCount", true, 1, 0, monster?.HitDiceCount?.ToString());
        AddComboField(hpGrid, "Вид кости", ["d4", "d6", "d8", "d10", "d12", "d20"], "Выберите кость", "HitDie", true, 2, 0, monster?.HitDie ?? "d8");
        AddTextField(hpGrid, "Модификатор хитов", "Необязательно", "HitPointModifier", false, 3, 0, monster?.HitPointModifier?.ToString());
        challengeBody.Children.Add(hpGrid);
        page.Children.Add(challenge);
        cr.SelectionChanged += (_, _) =>
        {
            UpdateChallengeHints(cr);
            UpdateDerivedHints();
        };
        UpdateChallengeHints(cr);
        if (_monsterFields["HitDie"] is ComboBox hitDieSelector)
            hitDieSelector.SelectionChanged += (_, _) => UpdateDerivedHints();

        var abilitiesSection = Section("Характеристики", "Значения от 1 до 30; модификаторы рассчитываются автоматически.");
        var abilities = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*"), RowDefinitions = new RowDefinitions("Auto,Auto"), ColumnSpacing = 12, RowSpacing = 12 };
        for (var i = 0; i < MonsterRules.AbilityNames.Length; i++)
        {
            var nameIndex = i;
            var key = $"Ability:{MonsterRules.AbilityNames[i]}";
            var value = monster?.Abilities.GetValueOrDefault(MonsterRules.AbilityNames[i], 10).ToString() ?? "";
            var input = Input("10", text: value);
            input.TextChanged += (_, _) =>
            {
                UpdateAbilityModifier(nameIndex, input);
                UpdateDerivedHints();
            };
            var field = new StackPanel { Spacing = 6 };
            field.Children.Add(Label(MonsterRules.AbilityNames[i], true));
            _monsterFields[key] = input;
            field.Children.Add(input);
            var hint = new TextBlock { FontSize = 11, Foreground = Brush.Parse("#8992A1") };
            hint.Name = $"ModifierHint{i}";
            field.Children.Add(hint);
            Grid.SetColumn(field, i % 3);
            Grid.SetRow(field, i / 3);
            abilities.Children.Add(field);
            UpdateAbilityHint(hint, input);
        }
        ((StackPanel)abilitiesSection.Child!).Children.Add(abilities);
        page.Children.Add(abilitiesSection);

        var combatSection = Section("Боевые параметры", "Спасброски и навыки можно выбрать множественным выбором.");
        var combatFields = (StackPanel)combatSection.Child!;
        var combatGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*"), ColumnSpacing = 12 };
        AddTextField(combatGrid, "Скорость", "Например, 9 м, полёт 18 м", "Speed", false, 0, 0, monster?.Speed);
        AddMultiSelect(combatGrid, "Спасброски", MonsterRules.AbilityNames, "SavingThrows", 1, 0, monster?.SavingThrows);
        AddMultiSelect(combatGrid, "Навыки", ["Акробатика", "Анализ", "Атлетика", "Внимательность", "Выживание",
            "Выступление", "Запугивание", "История", "Ловкость рук", "Магия", "Медицина", "Обман",
            "Природа", "Проницательность", "Религия", "Скрытность", "Убеждение", "Уход за животными"],
            "Skills", 2, 0, monster?.Skills);
        combatFields.Children.Add(combatGrid);
        var combatOptions = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 12, Margin = new Thickness(0, 12, 0, 0) };
        AddTextField(combatOptions, "Пассивное восприятие", "Рекомендуется 10 + модификатор Мудрости + владение Внимательностью", "PassivePerception", false, 0, 0, monster?.PassivePerception?.ToString());
        AddTextField(combatOptions, "Бонус к инициативе", "По умолчанию равен модификатору Ловкости", "InitiativeBonus", false, 1, 0, monster?.InitiativeBonus?.ToString());
        combatFields.Children.Add(combatOptions);
        UpdateDerivedHints();
        page.Children.Add(combatSection);

        var specialSection = Section("Особые свойства", "Сопротивления, иммунитеты, чувства, языки и среда обитания.");
        var special = (StackPanel)specialSection.Child!;
        var specialGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto"), ColumnSpacing = 12, RowSpacing = 12 };
        string[] damageTypes = ["Дробящий", "Колющий", "Рубящий", "Кислота", "Холод", "Огонь", "Сила", "Электричество", "Некротический", "Яд", "Психический", "Излучение", "Гром"];
        string[] conditions = ["Ослепление", "Очарование", "Глухота", "Истощение", "Испуг", "Захват", "Недееспособность", "Невидимость", "Паралич", "Окаменение", "Отравление", "Сбит с ног", "Опутан", "Ошеломление", "Без сознания"];
        string[] languages = ["Общий", "Дварфийский", "Эльфийский", "Великаний", "Гномий", "Гоблинский", "Полуросликов", "Орочий", "Бездны", "Небесный", "Драконий", "Глубинная речь", "Инфернальный", "Первичный", "Сильван", "Подземный", "Телепатия"];
        AddMultiSelect(specialGrid, "Уязвимости к урону", damageTypes, "Vulnerabilities", 0, 0, SplitChoices(monster?.Vulnerabilities));
        AddMultiSelect(specialGrid, "Сопротивления урону", damageTypes, "Resistances", 0, 1, SplitChoices(monster?.Resistances));
        AddMultiSelect(specialGrid, "Иммунитеты к урону", damageTypes, "DamageImmunities", 0, 2, SplitChoices(monster?.DamageImmunities));
        AddMultiSelect(specialGrid, "Иммунитеты к состояниям", conditions, "ConditionImmunities", 1, 0, SplitChoices(monster?.ConditionImmunities));
        AddTextField(specialGrid, "Чувства", "Например, тёмное зрение 36 м", "Senses", false, 1, 1, monster?.Senses);
        AddMultiSelect(specialGrid, "Языки", languages, "Languages", 1, 2, SplitChoices(monster?.Languages));
        AddTextField(specialGrid, "Среда обитания", "Например, горы", "Habitat", false, 0, 3, monster?.Habitat, columnSpan: 2);
        special.Children.Add(specialGrid);
        page.Children.Add(specialSection);

        var descriptionSection = Section("Описание и действия", "Пустые разделы не появятся в статблоке.");
        var descriptions = (StackPanel)descriptionSection.Child!;
        var descriptionGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), RowDefinitions = new RowDefinitions("Auto,Auto,Auto"), ColumnSpacing = 12, RowSpacing = 12 };
        AddTextField(descriptionGrid, "Особенности", "Описание особенностей", "Traits", false, 0, 0, monster?.Traits, multiline: true);
        AddTextField(descriptionGrid, "Действия", "Описание действий", "Actions", false, 1, 0, monster?.Actions, multiline: true);
        AddTextField(descriptionGrid, "Бонусные действия", "Необязательно", "BonusActions", false, 0, 1, monster?.BonusActions, multiline: true);
        AddTextField(descriptionGrid, "Реакции", "Необязательно", "Reactions", false, 1, 1, monster?.Reactions, multiline: true);
        AddTextField(descriptionGrid, "Легендарные действия", "Необязательно", "LegendaryActions", false, 0, 2, monster?.LegendaryActions, multiline: true);
        AddTextField(descriptionGrid, "Логово", "Необязательно", "Lair", false, 1, 2, monster?.Lair, multiline: true);
        _monsterFields["Description"] = Input("Внешность, повадки или заметные детали", multiline: true, text: monster?.Description);
        descriptions.Children.Add(descriptionGrid);
        descriptions.Children.Add(Field("Краткое описание", (TextBox)_monsterFields["Description"]));
        page.Children.Add(descriptionSection);

        var imagesSection = Section("Изображения", "Добавьте любое количество изображений и выберите главное для списка монстров.");
        var imagesBody = (StackPanel)imagesSection.Child!;
        var imageStatus = new TextBlock
        {
            Text = monster?.ImagePaths.Count > 0 ? $"Загружено изображений: {monster.ImagePaths.Count}" : "Изображения не добавлены",
            Foreground = Brush.Parse(_isLightTheme ? "#676B74" : "#9299A6"),
            FontSize = 12
        };
        imagesBody.Children.Add(imageStatus);
        var selectedImage = Select(
            Enumerable.Range(1, Math.Max(1, monster?.ImagePaths.Count ?? 0)).Select(index => index.ToString(CultureInfo.InvariantCulture)),
            "Выберите изображение списка");
        selectedImage.SelectedIndex = monster?.SelectedImageIndex is int selected && selected >= 0 ? selected : -1;
        _monsterFields["SelectedImageIndex"] = selectedImage;
        imagesBody.Children.Add(ActionButton("Загрузить изображения…", async () =>
        {
            try
            {
                var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Выберите изображения монстра",
                    AllowMultiple = true,
                    FileTypeFilter =
                    [
                        new FilePickerFileType("Изображения")
                        {
                            Patterns = ["*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp"]
                        }
                    ]
                });
                foreach (var file in files)
                {
                    var localPath = file.TryGetLocalPath();
                    if (localPath is not null)
                        _pendingImages.Add(_store.ImportImage(localPath));
                }
                var knownCount = monster?.ImagePaths.Count ?? 0;
                selectedImage.ItemsSource = Enumerable.Range(1, knownCount + _pendingImages.Count)
                    .Select(index => index.ToString(CultureInfo.InvariantCulture)).ToArray();
                selectedImage.SelectedIndex = knownCount + _pendingImages.Count - 1;
                imageStatus.Text = $"Добавлено изображений: {knownCount + _pendingImages.Count}";
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                ShowStatus($"Не удалось загрузить изображение: {exception.Message}", isError: true);
            }
        }));
        imagesBody.Children.Add(Field("Основное изображение", selectedImage));
        page.Children.Add(imagesSection);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        actions.Children.Add(ActionButton("Отмена", () =>
        {
            if (monster is null)
                ShowHome();
            else
                ShowMonsterCollection();
        }));
        actions.Children.Add(ActionButton(monster is null ? "Сохранить монстра" : "Сохранить изменения",
            () => SaveMonster(monster), primary: true));
        page.Children.Add(actions);
        _status = new TextBlock { IsVisible = false, TextWrapping = TextWrapping.Wrap, FontSize = 13 };
        page.Children.Add(_status);
        SetPage(page);
    }

    private TextBox AddTextField(Grid grid, string label, string watermark, string key, bool required,
        int column, int row, string? value = null, bool multiline = false, int columnSpan = 1)
    {
        var input = Input(watermark, multiline, value);
        var field = Field(label, input, required);
        Grid.SetColumn(field, column);
        Grid.SetRow(field, row);
        if (columnSpan > 1)
            Grid.SetColumnSpan(field, columnSpan);
        grid.Children.Add(field);
        if (key != "Description" || !_monsterFields.ContainsKey(key))
            _monsterFields[key] = input;
        return input;
    }

    private ComboBox AddComboField(Grid grid, string label, IEnumerable<string> values, string placeholder,
        string key, bool required, int column, int row, string? selected)
    {
        var combo = Select(values, placeholder);
        if (!string.IsNullOrWhiteSpace(selected))
            combo.SelectedItem = selected;
        var field = Field(label, combo, required);
        Grid.SetColumn(field, column);
        Grid.SetRow(field, row);
        grid.Children.Add(field);
        _monsterFields[key] = combo;
        return combo;
    }

    private void AddMultiSelect(Grid grid, string label, IEnumerable<string> values, string key,
        int column, int row, List<string>? selected)
    {
        var options = values.ToArray();
        var panel = new StackPanel { Spacing = 2, MaxHeight = 120 };
        var checkboxes = new List<CheckBox>();
        foreach (var option in options)
        {
            var checkBox = new CheckBox
            {
                Content = option,
                IsChecked = selected?.Contains(option) == true,
                Foreground = Brush.Parse(_isLightTheme ? "#3D4149" : "#C7CAD1"),
                Padding = new Thickness(2)
            };
            if (key == "Skills")
                checkBox.IsCheckedChanged += (_, _) => UpdateDerivedHints();
            checkboxes.Add(checkBox);
            panel.Children.Add(checkBox);
        }
        var scroll = new ScrollViewer { Content = panel };
        var field = Field(label, scroll);
        Grid.SetColumn(field, column);
        Grid.SetRow(field, row);
        grid.Children.Add(field);
        _monsterFields[key] = new TextBox { Tag = checkboxes, IsVisible = false };
    }

    private void AddTextField(StackPanel panel, string label, string watermark, string key,
        bool required, int column, int row, string? value = null, bool multiline = false)
    {
        var input = Input(watermark, multiline, value);
        panel.Children.Add(Field(label, input, required));
        _monsterFields[key] = input;
    }

    private void UpdateChallengeHints(ComboBox challenge)
    {
        var rating = challenge.SelectedItem?.ToString() ?? "0";
        var recommendation = MonsterRules.RecommendationFor(rating);
        SetWatermark("ArmorClass", $"Рекомендуется {recommendation.ArmorClass}");
        SetWatermark("AverageHitPoints", $"Рекомендуется {(recommendation.MinimumHitPoints + recommendation.MaximumHitPoints) / 2}");
        if (_challengeHints is not null)
            _challengeHints.Text = $"Бонус мастерства {FormatModifier(recommendation.ProficiencyBonus)} · " +
                $"Бонус атаки {FormatModifier(recommendation.AttackBonus)} · " +
                $"Урон за раунд {recommendation.MinimumDamagePerRound}–{recommendation.MaximumDamagePerRound} · " +
                $"СЛ спасброска {recommendation.SaveDc}";
    }

    private void SetWatermark(string key, string text)
    {
        if (_monsterFields.TryGetValue(key, out var control) && control is TextBox input)
            input.Watermark = text;
    }

    private void UpdateDerivedHints()
    {
        if (_monsterFields.TryGetValue("Ability:Мудрость", out var wisdomControl) &&
            wisdomControl is TextBox wisdomInput)
        {
            var wisdom = ParseInt(wisdomInput.Text) ?? 10;
            var rating = GetSelection("ChallengeRating") ?? "0";
            var proficient = GetSelectedOptions("Skills").Contains("Внимательность");
            var passive = MonsterRules.RecommendedPassivePerception(
                wisdom, proficient, MonsterRules.RecommendationFor(rating).ProficiencyBonus);
            SetWatermark("PassivePerception", $"Рекомендуется {passive}");
        }
        var hitDice = _monsterFields.TryGetValue("ChallengeRating", out var challengeControl) &&
                      challengeControl is ComboBox challenge
            ? MonsterRules.RecommendationFor(challenge.SelectedItem?.ToString() ?? "0")
            : MonsterRules.RecommendationFor("0");
        var recommendedHitPoints = (hitDice.MinimumHitPoints + hitDice.MaximumHitPoints) / 2;
        var constitution = _monsterFields.TryGetValue("Ability:Телосложение", out var constitutionControl) &&
                           constitutionControl is TextBox constitutionInput
            ? MonsterRules.AbilityModifier(ParseInt(constitutionInput.Text) ?? 10)
            : 0;
        var dieAverage = _monsterFields.TryGetValue("HitDie", out var hitDieControl) &&
                         hitDieControl is ComboBox dieSelector &&
                         dieSelector.SelectedItem?.ToString() is string die &&
                         int.TryParse(die.TrimStart('d'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var sides)
            ? (sides + 1) / 2d
            : 4.5;
        SetWatermark("HitDiceCount", $"Рекомендуется {Math.Max(1, (int)Math.Ceiling(recommendedHitPoints / Math.Max(1d, dieAverage + constitution)))}");
        if (_monsterFields.TryGetValue("Ability:Ловкость", out var dexterityControl) &&
            dexterityControl is TextBox dexterityInput)
            SetWatermark("InitiativeBonus", $"Рекомендуется {FormatModifier(MonsterRules.AbilityModifier(ParseInt(dexterityInput.Text) ?? 10))}");
    }

    private static void UpdateAbilityModifier(int index, TextBox input)
    {
        var parent = input.Parent as StackPanel;
        if (parent?.Children.OfType<TextBlock>().LastOrDefault() is { } hint)
            UpdateAbilityHint(hint, input);
    }

    private static void UpdateAbilityHint(TextBlock hint, TextBox input)
    {
        if (string.IsNullOrWhiteSpace(input.Text))
        {
            hint.Text = "Рекомендуется 10 · модификатор +0";
            return;
        }
        var score = ParseInt(input.Text);
        hint.Text = score is >= 1 and <= 30
            ? $"Модификатор {FormatModifier(MonsterRules.AbilityModifier(score.Value))}"
            : "Введите значение от 1 до 30";
    }

    private void SaveMonster(Monster? original)
    {
        var name = GetText("Name");
        var size = GetSelection("Size");
        var type = GetSelection("Type");
        var rating = GetSelection("ChallengeRating");
        var ac = ParseInt(GetText("ArmorClass"));
        var averageHp = ParseInt(GetText("AverageHitPoints"));
        var hitDice = ParseInt(GetText("HitDiceCount"));
        var hitDie = GetSelection("HitDie");
        if (string.IsNullOrWhiteSpace(name) || size is null || type is null || rating is null || hitDie is null)
        {
            ShowStatus("Заполните имя, размер, тип и опасность существа.", isError: true);
            return;
        }

        var abilities = new Dictionary<string, int>();
        foreach (var ability in MonsterRules.AbilityNames)
        {
            var rawScore = GetText($"Ability:{ability}");
            var score = string.IsNullOrWhiteSpace(rawScore) ? 10 : ParseInt(rawScore);
            if (score is not (>= 1 and <= 30))
            {
                ShowStatus($"Для характеристики «{ability}» введите целое число от 1 до 30.", isError: true);
                return;
            }
            abilities[ability] = score.Value;
        }

        var recommendation = MonsterRules.RecommendationFor(rating);
        var savingThrows = GetSelectedOptions("SavingThrows");
        var skills = GetSelectedOptions("Skills");
        var wisdomScore = abilities.GetValueOrDefault("Мудрость", 10);
        var dexterityScore = abilities.GetValueOrDefault("Ловкость", 10);
        if (GetText("ArmorClass") is { Length: > 0 } armorText && ParseInt(armorText) is null)
        {
            ShowStatus("Класс доспеха должен быть целым числом.", isError: true);
            return;
        }
        if (GetText("AverageHitPoints") is { Length: > 0 } hpText && ParseInt(hpText) is null)
        {
            ShowStatus("Среднее количество хитов должно быть целым числом.", isError: true);
            return;
        }
        if (GetText("HitDiceCount") is { Length: > 0 } diceText && ParseInt(diceText) is null)
        {
            ShowStatus("Количество костей должно быть целым числом.", isError: true);
            return;
        }
        if (ParseInt(GetText("ArmorClass")) is < 0 or > 30 ||
            ParseInt(GetText("AverageHitPoints")) is <= 0 ||
            ParseInt(GetText("HitDiceCount")) is <= 0)
        {
            ShowStatus("КД не может быть отрицательным, хиты и количество костей должны быть больше нуля.", isError: true);
            return;
        }
        var resolvedHitPoints = ParseInt(GetText("AverageHitPoints")) ??
            (recommendation.MinimumHitPoints + recommendation.MaximumHitPoints) / 2;
        var constitutionModifier = MonsterRules.AbilityModifier(abilities.GetValueOrDefault("Телосложение", 10));
        var resolvedHitDiceCount = ParseInt(GetText("HitDiceCount")) ??
            Math.Max(1, (int)Math.Ceiling(resolvedHitPoints / Math.Max(1d, (int.Parse(hitDie[1..], CultureInfo.InvariantCulture) + 1) / 2d + constitutionModifier)));
        var monster = new Monster
        {
            Id = original?.Id ?? Guid.NewGuid(),
            Name = name.Trim(),
            Size = size,
            Type = type,
            Alignment = GetSelection("Alignment") ?? "",
            Description = GetText("Description"),
            ChallengeRating = rating,
            ArmorClass = ac ?? recommendation.ArmorClass,
            ArmorType = GetText("ArmorType"),
            AverageHitPoints = averageHp ?? resolvedHitPoints,
            HitDiceCount = hitDice ?? resolvedHitDiceCount,
            HitDie = hitDie,
            HitPointModifier = ParseInt(GetText("HitPointModifier")),
            Abilities = abilities,
            Speed = GetText("Speed"),
            SavingThrows = savingThrows,
            Skills = skills,
            InitiativeBonus = ParseInt(GetText("InitiativeBonus")) ?? MonsterRules.AbilityModifier(dexterityScore),
            PassivePerception = ParseInt(GetText("PassivePerception")) ??
                MonsterRules.RecommendedPassivePerception(wisdomScore, skills.Contains("Внимательность"), recommendation.ProficiencyBonus),
            Vulnerabilities = string.Join(", ", GetSelectedOptions("Vulnerabilities")),
            Resistances = string.Join(", ", GetSelectedOptions("Resistances")),
            DamageImmunities = string.Join(", ", GetSelectedOptions("DamageImmunities")),
            ConditionImmunities = string.Join(", ", GetSelectedOptions("ConditionImmunities")),
            Senses = GetText("Senses"),
            Languages = string.Join(", ", GetSelectedOptions("Languages")),
            Habitat = GetText("Habitat"),
            Traits = GetText("Traits"),
            Actions = GetText("Actions"),
            BonusActions = GetText("BonusActions"),
            Reactions = GetText("Reactions"),
            LegendaryActions = GetText("LegendaryActions"),
            Lair = GetText("Lair"),
            ImagePaths = original?.ImagePaths.ToList() ?? [],
            SelectedImageIndex = (GetControl("SelectedImageIndex") as ComboBox)?.SelectedIndex
        };
        monster.ImagePaths.AddRange(_pendingImages);
        try
        {
            var existingIndex = _monsters.FindIndex(item => item.Id == monster.Id);
            if (existingIndex >= 0)
                _monsters[existingIndex] = monster;
            else
                _monsters.Add(monster);
            _store.Save("monsters.json", _monsters);
            ShowMonsterCollection();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            ShowStatus($"Не удалось сохранить монстра: {exception.Message}", isError: true);
        }
    }

    private string GetText(string key) =>
        _monsterFields.TryGetValue(key, out var control) && control is TextBox input ? input.Text?.Trim() ?? "" : "";

    private string? GetSelection(string key) =>
        _monsterFields.TryGetValue(key, out var control) && control is ComboBox combo ? combo.SelectedItem?.ToString() : null;

    private Control? GetControl(string key) => _monsterFields.GetValueOrDefault(key);

    private List<string> GetSelectedOptions(string key)
    {
        if (!_monsterFields.TryGetValue(key, out var control) || control is not TextBox { Tag: List<CheckBox> options })
            return [];
        return options.Where(box => box.IsChecked == true).Select(box => box.Content?.ToString() ?? "").ToList();
    }

    private static List<string> SplitChoices(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();

    private static int? ParseInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : null;

    private static string FormatModifier(int value) => value >= 0 ? $"+{value}" : value.ToString(CultureInfo.InvariantCulture);

    private void ShowMonsterCollection(string? search = null, string? typeFilter = null, string? sizeFilter = null,
        string? challengeFilter = null, string? alignmentFilter = null, string? sort = null)
    {
        var page = NewPage();
        page.Children.Add(Heading("Коллекция монстров", "Сохранённые существа, статблоки и изображения."));
        var controls = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("2*,*,*,*,*,Auto"),
            ColumnSpacing = 8
        };
        var searchBox = Input("Поиск по имени", text: search);
        var types = Select(["Все типы", .. _monsters.Select(monster => monster.Type).Where(value => value.Length > 0).Distinct().Order()], "Тип");
        var sizes = Select(["Все размеры", .. _monsters.Select(monster => monster.Size).Where(value => value.Length > 0).Distinct().Order()], "Размер");
        var challenges = Select(["Любая опасность", .. MonsterRules.ChallengeRatings], "Опасность");
        var alignments = Select(["Любое мировоззрение", .. _monsters.Select(monster => monster.Alignment).Where(value => value.Length > 0).Distinct().Order()], "Мировоззрение");
        var sorting = Select(["По имени", "По опасности"], "Сортировка");
        if (typeFilter is not null) types.SelectedItem = typeFilter;
        if (sizeFilter is not null) sizes.SelectedItem = sizeFilter;
        if (challengeFilter is not null) challenges.SelectedItem = challengeFilter;
        if (alignmentFilter is not null) alignments.SelectedItem = alignmentFilter;
        if (sort is not null) sorting.SelectedItem = sort;
        Grid.SetColumn(searchBox, 0);
        Grid.SetColumn(types, 1);
        Grid.SetColumn(sizes, 2);
        Grid.SetColumn(challenges, 3);
        Grid.SetColumn(alignments, 4);
        Grid.SetColumn(sorting, 5);
        controls.Children.Add(searchBox);
        controls.Children.Add(types);
        controls.Children.Add(sizes);
        controls.Children.Add(challenges);
        controls.Children.Add(alignments);
        controls.Children.Add(sorting);
        var apply = ActionButton("Применить", () =>
        {
            var typeValue = types.SelectedIndex <= 0 ? null : types.SelectedItem?.ToString();
            var sizeValue = sizes.SelectedIndex <= 0 ? null : sizes.SelectedItem?.ToString();
            var challengeValue = challenges.SelectedIndex <= 0 ? null : challenges.SelectedItem?.ToString();
            var alignmentValue = alignments.SelectedIndex <= 0 ? null : alignments.SelectedItem?.ToString();
            ShowMonsterCollection(searchBox.Text, typeValue, sizeValue, challengeValue, alignmentValue, sorting.SelectedItem?.ToString());
        });
        page.Children.Add(controls);
        page.Children.Add(apply);

        IEnumerable<Monster> monsters = _monsters;
        if (!string.IsNullOrWhiteSpace(search))
            monsters = monsters.Where(monster => monster.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase));
        if (!string.IsNullOrWhiteSpace(typeFilter))
            monsters = monsters.Where(monster => monster.Type == typeFilter);
        if (!string.IsNullOrWhiteSpace(sizeFilter))
            monsters = monsters.Where(monster => monster.Size == sizeFilter);
        if (!string.IsNullOrWhiteSpace(challengeFilter))
            monsters = monsters.Where(monster => monster.ChallengeRating == challengeFilter);
        if (!string.IsNullOrWhiteSpace(alignmentFilter))
            monsters = monsters.Where(monster => monster.Alignment == alignmentFilter);
        monsters = sort == "По опасности"
            ? monsters.OrderBy(monster => Array.IndexOf(MonsterRules.ChallengeRatings, monster.ChallengeRating)).ThenBy(monster => monster.Name)
            : monsters.OrderBy(monster => monster.Name, StringComparer.CurrentCultureIgnoreCase);

        var shown = 0;
        foreach (var monster in monsters)
        {
            shown++;
            var summary = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            summary.Children.Add(new TextBlock
            {
                Text = monster.Name,
                FontWeight = FontWeight.SemiBold,
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Center
            });
            summary.Children.Add(new TextBlock
            {
                Text = $"Опасность {monster.ChallengeRating}",
                Foreground = Brush.Parse(_isLightTheme ? "#676B74" : "#C69A53"),
                VerticalAlignment = VerticalAlignment.Center
            });
            var detailPanel = new StackPanel { Spacing = 10, Margin = new Thickness(0, 8, 0, 0) };
            var details = MonsterDetails(monster);
            detailPanel.Children.Add(new TextBlock
            {
                Text = details,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 21
            });
            if (monster.ImagePaths.Count > 0)
            {
                var selected = monster.SelectedImageIndex is int imageIndex && imageIndex >= 0 && imageIndex < monster.ImagePaths.Count
                    ? imageIndex : 0;
                var image = new Image
                {
                    Source = new Avalonia.Media.Imaging.Bitmap(monster.ImagePaths[selected]),
                    Width = 260,
                    Height = 170,
                    Stretch = Stretch.UniformToFill
                };
                detailPanel.Children.Add(image);
            }
            detailPanel.Children.Add(ActionButton("Редактировать", () => ShowMonsterEditor(monster)));
            var expander = new Expander { Header = summary, Content = detailPanel, Margin = new Thickness(0, 0, 0, 8) };
            expander.Classes.Add("dynamic-card");
            expander.BorderThickness = new Thickness(1);
            expander.Padding = new Thickness(14);
            page.Children.Add(expander);
        }
        if (shown == 0)
            page.Children.Add(Card("Монстры не найдены", _monsters.Count == 0 ? "Создайте первого монстра." : "Измените фильтры."));
        page.Children.Add(ActionButton("Создать монстра", () => ShowMonsterEditor(null), primary: true));
        _status = new TextBlock { IsVisible = false, TextWrapping = TextWrapping.Wrap, FontSize = 13 };
        page.Children.Add(_status);
        SetPage(page);
    }

    private static string MonsterDetails(Monster monster)
    {
        var values = new List<string>
        {
            $"{monster.Size} {monster.Type}{(string.IsNullOrWhiteSpace(monster.Alignment) ? "" : $", {monster.Alignment}")}",
            $"Класс доспеха {monster.ArmorClass}{(string.IsNullOrWhiteSpace(monster.ArmorType) ? "" : $" ({monster.ArmorType})")} · Хиты {monster.EffectiveHitPoints} ({monster.HitDiceCount}{monster.HitDie}{FormatModifier(monster.HitPointModifier ?? 0)})",
            $"Бонус мастерства {FormatModifier(monster.ProficiencyBonus)} · Скорость {monster.Speed} · " +
            $"Инициатива {FormatModifier(monster.InitiativeBonus ?? 0)} · Пассивное восприятие {monster.PassivePerception ?? 10}",
            $"Характеристики: {string.Join(", ", monster.Abilities.Select(entry => $"{entry.Key} {entry.Value} ({FormatModifier(MonsterRules.AbilityModifier(entry.Value))})"))}"
        };
        AddDetail(values, "Спасброски", string.Join(", ", monster.SavingThrows));
        AddDetail(values, "Навыки", string.Join(", ", monster.Skills));
        AddDetail(values, "Уязвимости", monster.Vulnerabilities);
        AddDetail(values, "Сопротивления", monster.Resistances);
        AddDetail(values, "Иммунитеты к урону", monster.DamageImmunities);
        AddDetail(values, "Иммунитеты к состояниям", monster.ConditionImmunities);
        AddDetail(values, "Чувства", monster.Senses);
        AddDetail(values, "Языки", monster.Languages);
        AddDetail(values, "Среда обитания", monster.Habitat);
        AddDetail(values, "Описание", monster.Description);
        AddDetail(values, "Особенности", monster.Traits);
        AddDetail(values, "Действия", monster.Actions);
        AddDetail(values, "Бонусные действия", monster.BonusActions);
        AddDetail(values, "Реакции", monster.Reactions);
        AddDetail(values, "Легендарные действия", monster.LegendaryActions);
        AddDetail(values, "Логово", monster.Lair);
        return string.Join(Environment.NewLine, values);
    }

    private static void AddDetail(List<string> values, string label, string? content)
    {
        if (!string.IsNullOrWhiteSpace(content))
            values.Add($"{label}: {content}");
    }

    private void ShowEncounterEditor(Encounter? encounter)
    {
        _editingEncounter = encounter;
        _draftHeroes = encounter?.Heroes.Select(CloneMember).ToList() ?? [];
        _draftMonsters = encounter?.Monsters.Select(CloneMember).ToList() ?? [];
        var page = NewPage();
        page.Children.Add(Heading(encounter is null ? "Создать энкаунтер" : $"Редактировать: {encounter.Name}",
            "Добавьте героев и монстров, затем сохраните боевую сцену."));
        var nameInput = Input("Название сцены", text: encounter?.Name);
        page.Children.Add(Field("Название энкаунтера", nameInput, required: true));

        var difficulty = new TextBlock
        {
            Text = "Сложность: добавьте героев и монстров.",
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse(_isLightTheme ? "#676B74" : "#C69A53")
        };
        var heroesSection = Section("Герои", "Выберите сохранённого героя или создайте нового.");
        var heroesBody = (StackPanel)heroesSection.Child!;
        var heroSelector = Select(_heroes.Select(hero => hero.Name), "Выберите героя");
        var heroName = Input("Имя героя");
        var heroHp = Input("Хиты (необязательно)");
        var heroLevel = Input("Уровень (необязательно)");
        var heroInitiative = Input("Бонус инициативы (необязательно)");
        var unavailableHeroActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        foreach (var label in new[] { "Сортировать героев", "Фильтровать героев" })
        {
            var placeholder = new Button { Content = label, IsEnabled = false, Opacity = 0.45 };
            unavailableHeroActions.Children.Add(placeholder);
        }
        heroesBody.Children.Add(unavailableHeroActions);
        var heroFields = new Grid { ColumnDefinitions = new ColumnDefinitions("2*,*,*,*"), ColumnSpacing = 10 };
        Grid.SetColumn(heroSelector, 0);
        heroFields.Children.Add(heroSelector);
        foreach (var (input, index) in new[] { (heroHp, 1), (heroLevel, 2), (heroInitiative, 3) })
        {
            Grid.SetColumn(input, index);
            heroFields.Children.Add(input);
        }
        heroesBody.Children.Add(heroFields);
        heroesBody.Children.Add(heroName);
        var heroList = new StackPanel { Spacing = 4 };
        RefreshDraftList(heroList, _draftHeroes);
        heroesBody.Children.Add(heroList);
        heroesBody.Children.Add(ActionButton("Добавить героя", () =>
        {
            Hero? hero = heroSelector.SelectedIndex is >= 0 and var selectedIndex && selectedIndex < _heroes.Count
                ? _heroes[selectedIndex] : null;
            var resolvedName = hero?.Name ?? heroName.Text?.Trim();
            if (string.IsNullOrWhiteSpace(resolvedName))
            {
                ShowStatus("Укажите имя героя или выберите его из списка.", isError: true);
                return;
            }
            if (hero is null)
            {
                hero = new Hero
                {
                    Name = resolvedName,
                    HitPoints = ParseInt(heroHp.Text),
                    Level = ParseInt(heroLevel.Text),
                    InitiativeBonus = ParseInt(heroInitiative.Text) ?? 0
                };
                _heroes.Add(hero);
            }
            _draftHeroes.Add(new EncounterMember
            {
                HeroId = hero.Id,
                Name = hero.Name,
                HitPoints = hero.HitPoints,
                InitiativeBonus = hero.InitiativeBonus
            });
            RefreshDraftList(heroList, _draftHeroes);
            UpdateDifficulty();
        }));
        page.Children.Add(heroesSection);

        var monstersSection = Section("Монстры", "Можно добавить группу одного монстра; экземпляры будут пронумерованы в бою.");
        var monstersBody = (StackPanel)monstersSection.Child!;
        var monsterSearch = Input("Поиск по имени");
        var monsterTypeFilter = Select(["Все типы", .. _monsters.Select(monster => monster.Type).Distinct().Order()], "Тип");
        var monsterSizeFilter = Select(["Все размеры", .. _monsters.Select(monster => monster.Size).Distinct().Order()], "Размер");
        var monsterCrFilter = Select(["Любая опасность", .. MonsterRules.ChallengeRatings], "Опасность");
        var monsterSort = Select(["По имени", "По опасности"], "Сортировка");
        var monsterSelector = Select([], "Выберите монстра");
        var count = Input("Количество", text: "1");
        var imageSelector = Select([], "Основное изображение");
        var monsterOptions = new List<Monster>();
        void ApplyEncounterMonsterFilters()
        {
            IEnumerable<Monster> filtered = _monsters;
            if (!string.IsNullOrWhiteSpace(monsterSearch.Text))
                filtered = filtered.Where(monster => monster.Name.Contains(monsterSearch.Text, StringComparison.CurrentCultureIgnoreCase));
            if (monsterTypeFilter.SelectedIndex > 0)
                filtered = filtered.Where(monster => monster.Type == monsterTypeFilter.SelectedItem?.ToString());
            if (monsterSizeFilter.SelectedIndex > 0)
                filtered = filtered.Where(monster => monster.Size == monsterSizeFilter.SelectedItem?.ToString());
            if (monsterCrFilter.SelectedIndex > 0)
                filtered = filtered.Where(monster => monster.ChallengeRating == monsterCrFilter.SelectedItem?.ToString());
            monsterOptions = (monsterSort.SelectedItem?.ToString() == "По опасности"
                    ? filtered.OrderBy(monster => Array.IndexOf(MonsterRules.ChallengeRatings, monster.ChallengeRating)).ThenBy(monster => monster.Name)
                    : filtered.OrderBy(monster => monster.Name, StringComparer.CurrentCultureIgnoreCase))
                .ToList();
            monsterSelector.ItemsSource = monsterOptions.Select(monster => $"{monster.Name} · опасность {monster.ChallengeRating}").ToArray();
            monsterSelector.SelectedIndex = -1;
            imageSelector.ItemsSource = Array.Empty<string>();
        }
        var monsterFilterGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("2*,*,*,*,*"),
            ColumnSpacing = 8
        };
        Grid.SetColumn(monsterSearch, 0);
        Grid.SetColumn(monsterTypeFilter, 1);
        Grid.SetColumn(monsterSizeFilter, 2);
        Grid.SetColumn(monsterCrFilter, 3);
        Grid.SetColumn(monsterSort, 4);
        monsterFilterGrid.Children.Add(monsterSearch);
        monsterFilterGrid.Children.Add(monsterTypeFilter);
        monsterFilterGrid.Children.Add(monsterSizeFilter);
        monsterFilterGrid.Children.Add(monsterCrFilter);
        monsterFilterGrid.Children.Add(monsterSort);
        monstersBody.Children.Add(monsterFilterGrid);
        monstersBody.Children.Add(ActionButton("Применить фильтры", ApplyEncounterMonsterFilters));
        ApplyEncounterMonsterFilters();
        monsterSelector.SelectionChanged += (_, _) =>
        {
            if (monsterSelector.SelectedIndex >= 0 && monsterSelector.SelectedIndex < monsterOptions.Count)
            {
                var selectedMonster = monsterOptions[monsterSelector.SelectedIndex];
                imageSelector.ItemsSource = new[] { "Основное изображение" }.Concat(
                    Enumerable.Range(1, selectedMonster.ImagePaths.Count).Select(index => $"Изображение {index}")).ToArray();
                imageSelector.SelectedIndex = 0;
            }
        };
        var monsterFields = new Grid { ColumnDefinitions = new ColumnDefinitions("3*,*"), ColumnSpacing = 10 };
        Grid.SetColumn(monsterSelector, 0);
        Grid.SetColumn(count, 1);
        monsterFields.Children.Add(monsterSelector);
        monsterFields.Children.Add(count);
        monstersBody.Children.Add(monsterFields);
        monstersBody.Children.Add(Field("Изображение сцены", imageSelector));
        var monsterList = new StackPanel { Spacing = 4 };
        RefreshDraftList(monsterList, _draftMonsters);
        monstersBody.Children.Add(monsterList);
        monstersBody.Children.Add(ActionButton("Добавить монстра", () =>
        {
            if (monsterSelector.SelectedIndex < 0 || monsterSelector.SelectedIndex >= monsterOptions.Count)
            {
                ShowStatus("Выберите монстра из коллекции.", isError: true);
                return;
            }
            var amount = ParseInt(count.Text);
            if (amount is not (>= 1 and <= 100))
            {
                ShowStatus("Количество монстров должно быть от 1 до 100.", isError: true);
                return;
            }
            var monster = monsterOptions[monsterSelector.SelectedIndex];
            var selectedImagePath = imageSelector.SelectedIndex > 0
                ? monster.ImagePaths.ElementAtOrDefault(imageSelector.SelectedIndex - 1) ?? ""
                : monster.ImagePaths.ElementAtOrDefault(monster.SelectedImageIndex ?? 0) ?? "";
            _draftMonsters.Add(new EncounterMember
            {
                MonsterId = monster.Id,
                Name = monster.Name,
                ChallengeRating = monster.ChallengeRating,
                Count = amount.Value,
                HitPoints = monster.EffectiveHitPoints,
                InitiativeBonus = monster.InitiativeBonus ?? MonsterRules.AbilityModifier(monster.Abilities.GetValueOrDefault("Ловкость", 10)),
                ImagePath = selectedImagePath
            });
            RefreshDraftList(monsterList, _draftMonsters);
            UpdateDifficulty();
        }));
        page.Children.Add(monstersSection);

        page.Children.Add(difficulty);
        void UpdateDifficulty()
        {
            var partyLevels = _draftHeroes.Select(member => member.HeroId is Guid heroId
                ? _heroes.FirstOrDefault(hero => hero.Id == heroId)?.Level ?? 1 : 1);
            difficulty.Text = $"Сложность: {MonsterRules.EncounterDifficulty(_draftMonsters, partyLevels)}";
        }
        UpdateDifficulty();

        page.Children.Add(ActionButton("Отмена", ShowEncounterCollection));
        page.Children.Add(ActionButton("Сохранить энкаунтер", () =>
        {
            if (string.IsNullOrWhiteSpace(nameInput.Text))
            {
                ShowStatus("Введите название энкаунтера.", isError: true);
                return;
            }
            if (_draftHeroes.Count == 0 || _draftMonsters.Count == 0)
            {
                ShowStatus("Добавьте хотя бы одного героя и одного монстра.", isError: true);
                return;
            }
            var saved = encounter ?? new Encounter();
            saved.Name = nameInput.Text.Trim();
            saved.Heroes = _draftHeroes.Select(CloneMember).ToList();
            saved.Monsters = _draftMonsters.Select(CloneMember).ToList();
            if (encounter is null)
                _encounters.Add(saved);
            try
            {
                _store.Save("heroes.json", _heroes);
                _store.Save("encounters.json", _encounters);
                ShowEncounterCollection();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
            {
                ShowStatus($"Не удалось сохранить энкаунтер: {exception.Message}", isError: true);
            }
        }, primary: true));
        _status = new TextBlock { IsVisible = false, TextWrapping = TextWrapping.Wrap, FontSize = 13 };
        page.Children.Add(_status);
        SetPage(page);
    }

    private static EncounterMember CloneMember(EncounterMember member) => new()
    {
        Id = member.Id,
        MonsterId = member.MonsterId,
        HeroId = member.HeroId,
        Name = member.Name,
        ChallengeRating = member.ChallengeRating,
        Count = member.Count,
        HitPoints = member.HitPoints,
        InitiativeBonus = member.InitiativeBonus,
        Initiative = member.Initiative,
        ImagePath = member.ImagePath,
        Concentrating = member.Concentrating
    };

    private static void RefreshDraftList(StackPanel panel, List<EncounterMember> members)
    {
        panel.Children.Clear();
        if (members.Count == 0)
        {
            panel.Children.Add(new TextBlock { Text = "Пока никого нет.", FontSize = 12, Foreground = Brush.Parse("#9299A6") });
            return;
        }
        foreach (var member in members)
            panel.Children.Add(new TextBlock { Text = $"{member.Name}{(member.Count > 1 ? $" × {member.Count}" : "")} · хиты {member.HitPoints?.ToString() ?? "—"}" });
    }

    private void ShowEncounterCollection()
    {
        var page = NewPage();
        page.Children.Add(Heading("Энкаунтеры", "Сохранённые сцены и запуск боя."));
        if (_encounters.Count == 0)
            page.Children.Add(Card("Сохранённых энкаунтеров пока нет."));
        foreach (var encounter in _encounters.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            var content = new StackPanel { Spacing = 8 };
            content.Children.Add(new TextBlock { Text = "Герои", FontWeight = FontWeight.Bold });
            foreach (var hero in encounter.Heroes)
                content.Children.Add(new TextBlock
                {
                    Text = $"{hero.Name} · хиты {hero.HitPoints?.ToString() ?? "—"} · уровень {_heroes.FirstOrDefault(item => item.Id == hero.HeroId)?.Level?.ToString() ?? "—"}"
                });
            content.Children.Add(new TextBlock { Text = "Монстры", FontWeight = FontWeight.Bold });
            foreach (var member in encounter.Monsters)
            {
                var monster = _monsters.FirstOrDefault(item => item.Id == member.MonsterId);
                content.Children.Add(new TextBlock
                {
                    Text = $"{member.Name}{(member.Count > 1 ? $" × {member.Count}" : "")} · опасность {member.ChallengeRating} · хиты {member.HitPoints?.ToString() ?? "—"}" +
                        (monster is null ? "" : $"{Environment.NewLine}{MonsterDetails(monster)}"),
                    TextWrapping = TextWrapping.Wrap
                });
            }
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            buttons.Children.Add(ActionButton("Редактировать", () => ShowEncounterEditor(encounter)));
            buttons.Children.Add(ActionButton("Запустить бой", () => ShowCombat(encounter), primary: true));
            buttons.Children.Add(ActionButton("Удалить", () => DeleteEncounter(encounter)));
            content.Children.Add(buttons);
            var expander = new Expander { Header = encounter.Name, Content = content, Padding = new Thickness(14), Margin = new Thickness(0, 0, 0, 8) };
            expander.Classes.Add("dynamic-card");
            expander.BorderThickness = new Thickness(1);
            page.Children.Add(expander);
        }
        page.Children.Add(ActionButton("Создать энкаунтер", () => ShowEncounterEditor(null), primary: true));
        _status = new TextBlock { IsVisible = false, TextWrapping = TextWrapping.Wrap, FontSize = 13 };
        page.Children.Add(_status);
        SetPage(page);
    }

    private void DeleteEncounter(Encounter encounter)
    {
        _encounters.Remove(encounter);
        try
        {
            _store.Save("encounters.json", _encounters);
            ShowEncounterCollection();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowStatus($"Не удалось удалить энкаунтер: {exception.Message}", isError: true);
        }
    }

    private void ShowCombat(Encounter encounter)
    {
        var startChanged = false;
        if (!encounter.CombatStarted)
        {
            encounter.Heroes = encounter.Heroes.Select(CloneMember).ToList();
            encounter.Monsters = encounter.Monsters
                .SelectMany(member => Enumerable.Range(0, Math.Max(1, member.Count)).Select(index =>
                {
                    var instance = CloneMember(member);
                    instance.Id = Guid.NewGuid();
                    instance.Count = 1;
                    if (member.Count > 1)
                        instance.Name = $"{member.Name} {index + 1}";
                    return instance;
                })).ToList();
            RollInitiative(encounter);
            encounter.CombatStarted = true;
            encounter.Round = 1;
            encounter.CurrentTurn = 0;
            startChanged = true;
        }
        else
            SortTurns(encounter);

        var page = NewPage();
        page.Children.Add(Heading(encounter.Name, $"Раунд {encounter.Round} · ход {Math.Min(encounter.CurrentTurn + 1, Combatants(encounter).Count)}"));
        var participants = Combatants(encounter);
        if (participants.Count == 0)
        {
            page.Children.Add(Card("В бою нет участников."));
            SetPage(page);
            if (startChanged)
                SaveCombat(encounter);
            return;
        }
        if (encounter.CurrentTurn >= participants.Count)
            encounter.CurrentTurn = 0;

        for (var index = 0; index < participants.Count; index++)
        {
            var member = participants[index];
            var isCurrent = index == encounter.CurrentTurn;
            var card = new StackPanel { Spacing = 10 };
            card.Children.Add(new TextBlock
            {
                Text = $"{(isCurrent ? "▶ " : "")}{member.Name} · инициатива {member.Initiative}",
                FontWeight = FontWeight.Bold,
                FontSize = 16
            });
            var health = member.HitPoints ?? 0;
            card.Children.Add(new TextBlock { Text = $"Хиты: {health}", FontSize = 13 });
            var controls = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var initiativeInput = Input("Инициатива", text: member.Initiative.ToString(CultureInfo.InvariantCulture));
            controls.Children.Add(initiativeInput);
            controls.Children.Add(ActionButton("Задать инициативу", () =>
            {
                if (ParseInt(initiativeInput.Text) is not int initiative)
                {
                    ShowStatus("Введите целое число инициативы.", isError: true);
                    return;
                }
                member.Initiative = initiative;
                SortTurns(encounter);
                SaveCombat(encounter);
                ShowCombat(encounter);
            }));
            var amount = Input("Количество");
            controls.Children.Add(amount);
            controls.Children.Add(ActionButton("Лечить", () =>
            {
                if (ParseInt(amount.Text) is not int value || value < 0)
                {
                    ShowStatus("Введите неотрицательное целое число хитов.", isError: true);
                    return;
                }
                member.HitPoints = health + value;
                SaveCombat(encounter);
                ShowCombat(encounter);
            }));
            controls.Children.Add(ActionButton("Нанести урон", () =>
            {
                if (ParseInt(amount.Text) is not int value || value < 0)
                {
                    ShowStatus("Введите неотрицательное целое число хитов.", isError: true);
                    return;
                }
                member.HitPoints = Math.Max(0, health - value);
                SaveCombat(encounter);
                ShowCombat(encounter);
            }));
            controls.Children.Add(ActionButton(member.Concentrating ? "Концентрация: да" : "Концентрация",
                () =>
                {
                    member.Concentrating = !member.Concentrating;
                    SaveCombat(encounter);
                    ShowCombat(encounter);
                }, primary: member.Concentrating));
            controls.Children.Add(ActionButton("Удалить", () =>
            {
                encounter.Heroes.Remove(member);
                encounter.Monsters.Remove(member);
                SaveCombat(encounter);
                ShowCombat(encounter);
            }));
            card.Children.Add(controls);
            page.Children.Add(CardWithContent(card));
        }

        page.Children.Add(ActionButton("Следующий ход", () =>
        {
            if (encounter.CurrentTurn + 1 >= Combatants(encounter).Count)
            {
                encounter.CurrentTurn = 0;
                encounter.Round++;
            }
            else
                encounter.CurrentTurn++;
            SaveCombat(encounter);
            ShowCombat(encounter);
        }, primary: true));
        page.Children.Add(ActionButton("Перебросить инициативу", () =>
        {
            RollInitiative(encounter);
            encounter.CurrentTurn = 0;
            SaveCombat(encounter);
            ShowCombat(encounter);
        }));
        var newHeroName = Input("Добавить героя по имени");
        page.Children.Add(newHeroName);
        page.Children.Add(ActionButton("Добавить героя", () =>
        {
            var name = newHeroName.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ShowStatus("Введите имя героя.", isError: true);
                return;
            }
            var member = new EncounterMember { Name = name, HeroId = null, HitPoints = 0 };
            encounter.Heroes.Add(member);
            member.Initiative = Random.Shared.Next(1, 21) + member.InitiativeBonus;
            SortTurns(encounter);
            SaveCombat(encounter);
            ShowCombat(encounter);
        }));
        var addMonster = Select(_monsters.Select(monster => monster.Name), "Добавить монстра");
        page.Children.Add(addMonster);
        page.Children.Add(ActionButton("Добавить монстра в бой", () =>
        {
            if (addMonster.SelectedIndex < 0 || addMonster.SelectedIndex >= _monsters.Count)
            {
                ShowStatus("Выберите монстра из коллекции.", isError: true);
                return;
            }
            var monster = _monsters[addMonster.SelectedIndex];
            var member = new EncounterMember
            {
                MonsterId = monster.Id,
                Name = monster.Name,
                ChallengeRating = monster.ChallengeRating,
                HitPoints = monster.EffectiveHitPoints,
                InitiativeBonus = monster.InitiativeBonus ?? MonsterRules.AbilityModifier(monster.Abilities.GetValueOrDefault("Ловкость", 10)),
                ImagePath = monster.ImagePaths.ElementAtOrDefault(monster.SelectedImageIndex ?? 0) ?? ""
            };
            encounter.Monsters.Add(member);
            member.Initiative = Random.Shared.Next(1, 21) + member.InitiativeBonus;
            SortTurns(encounter);
            SaveCombat(encounter);
            ShowCombat(encounter);
        }));
        page.Children.Add(ActionButton("К списку энкаунтеров", ShowEncounterCollection));
        _status = new TextBlock { IsVisible = false, TextWrapping = TextWrapping.Wrap, FontSize = 13 };
        page.Children.Add(_status);
        SetPage(page);
        if (startChanged)
            SaveCombat(encounter);
    }

    private static Border CardWithContent(Control content)
    {
        var card = new Border
        {
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1),
            Child = content
        };
        card.Classes.Add("dynamic-card");
        return card;
    }

    private static List<EncounterMember> Combatants(Encounter encounter) =>
        encounter.Heroes.Concat(encounter.Monsters).OrderByDescending(member => member.Initiative).ToList();

    private static void RollInitiative(Encounter encounter)
    {
        foreach (var member in encounter.Heroes.Concat(encounter.Monsters))
            member.Initiative = Random.Shared.Next(1, 21) + member.InitiativeBonus;
        SortTurns(encounter);
    }

    private static void SortTurns(Encounter encounter)
    {
        encounter.Heroes = encounter.Heroes.OrderByDescending(member => member.Initiative).ToList();
        encounter.Monsters = encounter.Monsters.OrderByDescending(member => member.Initiative).ToList();
    }

    private void SaveCombat(Encounter encounter)
    {
        try
        {
            _store.Save("encounters.json", _encounters);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowStatus($"Не удалось сохранить состояние боя: {exception.Message}", isError: true);
        }
    }
}
