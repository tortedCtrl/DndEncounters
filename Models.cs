using System.Text.Json.Serialization;

namespace DndEncounters;

public sealed class Monster
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Size { get; set; } = "";
    public string Type { get; set; } = "";
    public string Alignment { get; set; } = "";
    public string Description { get; set; } = "";
    public string ChallengeRating { get; set; } = "0";
    public int? ArmorClass { get; set; }
    public string ArmorType { get; set; } = "";
    public int? AverageHitPoints { get; set; }
    public int? HitDiceCount { get; set; }
    public string HitDie { get; set; } = "d8";
    public int? HitPointModifier { get; set; }
    public Dictionary<string, int> Abilities { get; set; } = new();
    public string Speed { get; set; } = "";
    public int? InitiativeBonus { get; set; }
    public int? PassivePerception { get; set; }
    public List<string> SavingThrows { get; set; } = [];
    public List<string> Skills { get; set; } = [];
    public string Vulnerabilities { get; set; } = "";
    public string Resistances { get; set; } = "";
    public string DamageImmunities { get; set; } = "";
    public string ConditionImmunities { get; set; } = "";
    public string Senses { get; set; } = "";
    public string Languages { get; set; } = "";
    public string Habitat { get; set; } = "";
    public string Traits { get; set; } = "";
    public string Actions { get; set; } = "";
    public string BonusActions { get; set; } = "";
    public string Reactions { get; set; } = "";
    public string LegendaryActions { get; set; } = "";
    public string Lair { get; set; } = "";
    public List<string> ImagePaths { get; set; } = [];
    public int? SelectedImageIndex { get; set; }

    [JsonIgnore]
    public int ProficiencyBonus => MonsterRules.RecommendationFor(ChallengeRating).ProficiencyBonus;

    [JsonIgnore]
    public int EffectiveHitPoints => AverageHitPoints ??
        (MonsterRules.RecommendationFor(ChallengeRating).MinimumHitPoints +
         MonsterRules.RecommendationFor(ChallengeRating).MaximumHitPoints) / 2;
}

public sealed class Hero
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public int? HitPoints { get; set; }
    public int? Level { get; set; }
    public int InitiativeBonus { get; set; }
}

public sealed class EncounterMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? MonsterId { get; set; }
    public Guid? HeroId { get; set; }
    public string Name { get; set; } = "";
    public string ChallengeRating { get; set; } = "0";
    public int Count { get; set; } = 1;
    public int? HitPoints { get; set; }
    public int InitiativeBonus { get; set; }
    public int Initiative { get; set; }
    public string ImagePath { get; set; } = "";
    public bool Concentrating { get; set; }
}

public sealed class Encounter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public List<EncounterMember> Heroes { get; set; } = [];
    public List<EncounterMember> Monsters { get; set; } = [];
    public bool CombatStarted { get; set; }
    public int Round { get; set; } = 1;
    public int CurrentTurn { get; set; }
}

public sealed record ChallengeRecommendation(
    string ChallengeRating,
    int ProficiencyBonus,
    int ArmorClass,
    int MinimumHitPoints,
    int MaximumHitPoints,
    int AttackBonus,
    int MinimumDamagePerRound,
    int MaximumDamagePerRound,
    int SaveDc,
    int Experience);

public static class MonsterRules
{
    public static readonly string[] ChallengeRatings =
    [
        "0", "1/8", "1/4", "1/2",
        .. Enumerable.Range(1, 30).Select(level => level.ToString())
    ];

    public static readonly string[] AbilityNames =
        ["Сила", "Ловкость", "Телосложение", "Интеллект", "Мудрость", "Харизма"];

    // Benchmarks transcribed from the 2014 Dungeon Master's Guide, p. 274.
    private static readonly ChallengeRecommendation[] Recommendations =
    [
        R("0", 2, 13, 1, 6, 3, 0, 1, 13, 10),
        R("1/8", 2, 13, 7, 35, 3, 2, 3, 13, 25),
        R("1/4", 2, 13, 36, 49, 3, 4, 5, 13, 50),
        R("1/2", 2, 13, 50, 70, 3, 6, 8, 13, 100),
        R("1", 2, 13, 71, 85, 3, 9, 14, 13, 200),
        R("2", 2, 13, 86, 100, 3, 15, 20, 13, 450),
        R("3", 2, 13, 101, 115, 4, 21, 26, 13, 700),
        R("4", 2, 14, 116, 130, 5, 27, 32, 14, 1100),
        R("5", 3, 15, 131, 145, 6, 33, 38, 15, 1800),
        R("6", 3, 15, 146, 160, 6, 39, 44, 15, 2300),
        R("7", 3, 15, 161, 175, 6, 45, 50, 15, 2900),
        R("8", 3, 16, 176, 190, 7, 51, 56, 16, 3900),
        R("9", 4, 16, 191, 205, 7, 57, 62, 16, 5000),
        R("10", 4, 17, 206, 220, 7, 63, 68, 16, 5900),
        R("11", 4, 17, 221, 235, 8, 69, 74, 17, 7200),
        R("12", 4, 17, 236, 250, 8, 75, 80, 17, 8400),
        R("13", 5, 18, 251, 265, 8, 81, 86, 18, 10000),
        R("14", 5, 18, 266, 280, 8, 87, 92, 18, 11500),
        R("15", 5, 18, 281, 295, 8, 93, 98, 18, 13000),
        R("16", 5, 18, 296, 310, 9, 99, 104, 18, 15000),
        R("17", 6, 19, 311, 325, 10, 105, 110, 19, 18000),
        R("18", 6, 19, 326, 340, 10, 111, 116, 19, 20000),
        R("19", 6, 19, 341, 355, 10, 117, 122, 19, 22000),
        R("20", 6, 19, 356, 400, 10, 123, 140, 19, 25000),
        R("21", 7, 19, 401, 445, 11, 141, 158, 20, 33000),
        R("22", 7, 19, 446, 490, 11, 159, 176, 20, 41000),
        R("23", 7, 19, 491, 535, 11, 177, 194, 20, 50000),
        R("24", 7, 19, 536, 580, 12, 195, 212, 21, 62000),
        R("25", 8, 19, 581, 625, 12, 213, 230, 21, 75000),
        R("26", 8, 19, 626, 670, 12, 231, 248, 21, 90000),
        R("27", 8, 19, 671, 715, 13, 249, 266, 22, 105000),
        R("28", 8, 19, 716, 760, 13, 267, 284, 22, 120000),
        R("29", 9, 19, 761, 805, 13, 285, 302, 23, 135000),
        R("30", 9, 19, 806, 850, 14, 303, 320, 23, 155000)
    ];

    private static readonly Dictionary<int, (int Easy, int Medium, int Hard, int Deadly)> PartyThresholds = new()
    {
        [1] = (25, 50, 75, 100), [2] = (50, 100, 150, 200), [3] = (75, 150, 225, 400),
        [4] = (125, 250, 375, 500), [5] = (250, 500, 750, 1100), [6] = (300, 600, 900, 1400),
        [7] = (350, 750, 1100, 1700), [8] = (450, 900, 1400, 2100), [9] = (550, 1100, 1600, 2400),
        [10] = (600, 1200, 1900, 2800), [11] = (800, 1600, 2400, 3600), [12] = (1000, 2000, 3000, 4500),
        [13] = (1100, 2200, 3400, 5100), [14] = (1250, 2500, 3800, 5700), [15] = (1400, 2800, 4300, 6400),
        [16] = (1600, 3200, 4800, 7200), [17] = (2000, 3900, 5900, 8800), [18] = (2100, 4200, 6300, 9500),
        [19] = (2400, 4900, 7300, 10900), [20] = (2800, 5700, 8500, 12700)
    };

    public static ChallengeRecommendation RecommendationFor(string rating) =>
        Recommendations.FirstOrDefault(item => item.ChallengeRating == rating) ?? Recommendations[0];

    public static int AbilityModifier(int score) => (int)Math.Floor((score - 10) / 2d);

    public static int RecommendedPassivePerception(int wisdom, bool proficient, int proficiencyBonus) =>
        10 + AbilityModifier(wisdom) + (proficient ? proficiencyBonus : 0);

    public static string EncounterDifficulty(IEnumerable<EncounterMember> monsters, IEnumerable<int> partyLevels)
    {
        var members = monsters.ToList();
        var levels = partyLevels.ToList();
        if (members.Count == 0 || levels.Count == 0)
            return "Добавьте героев и монстров для расчёта.";

        var adjustedXp = (int)members.GroupBy(member => member.MonsterId).Sum(group =>
        {
            var member = group.First();
            var count = group.Sum(entry => Math.Max(1, entry.Count));
            var multiplier = count switch
            {
                <= 1 => 1d,
                2 => 1.5,
                <= 6 => 2d,
                <= 10 => 2.5,
                <= 14 => 3d,
                _ => 4d
            };
            if (levels.Count < 3)
                multiplier = multiplier switch { 1 => 1.5, 1.5 => 2, 2 => 2.5, 2.5 => 3, _ => 4 };
            else if (levels.Count > 5)
                multiplier = multiplier switch { 4 => 3, 3 => 2.5, 2.5 => 2, 2 => 1.5, _ => 1 };
            return RecommendationFor(member.ChallengeRating).Experience * count * multiplier;
        });
        var easy = levels.Sum(level => PartyThresholds[Math.Clamp(level, 1, 20)].Easy);
        var medium = levels.Sum(level => PartyThresholds[Math.Clamp(level, 1, 20)].Medium);
        var hard = levels.Sum(level => PartyThresholds[Math.Clamp(level, 1, 20)].Hard);
        var deadly = levels.Sum(level => PartyThresholds[Math.Clamp(level, 1, 20)].Deadly);
        var difficulty = adjustedXp < easy ? "Лёгкая" :
            adjustedXp < medium ? "Средняя" :
            adjustedXp < hard ? "Тяжёлая" :
            adjustedXp < deadly ? "Смертельно опасная" : "Выше смертельно опасной";
        return $"{difficulty} · {adjustedXp:N0} скорректированного опыта";
    }

    private static ChallengeRecommendation R(string cr, int proficiency, int ac, int minHp, int maxHp,
        int attack, int minDamage, int maxDamage, int dc, int xp) =>
        new(cr, proficiency, ac, minHp, maxHp, attack, minDamage, maxDamage, dc, xp);
}
