using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Skua.Core.Interfaces;
using Skua.Core.Models.Items;

namespace Tw1st3r
{
    public class Tw1st3rPlugin : ISkuaPlugin
    {
        public string Name => "Tw1st3r Plugin";
        public string Author => "tw1st3rr";
        public string Description => "Class-aware consumable manager. Slot 6 trigger, buff refresh, respawn re-buff, class swap detection.";
        public Version Version => new Version(1, 0, 0);

        public List<IOption> Options => new List<IOption>();

        private IScriptInterface? _bot;
        private Tw1st3rManager? _manager;
        private Tw1st3rWindow? _window;

        public void Load(IServiceProvider serviceProvider, IPluginHelper pluginHelper)
        {
            _bot = serviceProvider.GetService(typeof(IScriptInterface)) as IScriptInterface;
            if (_bot == null) return;

            _manager = new Tw1st3rManager(_bot);

            Application.Current.Dispatcher.Invoke(() =>
            {
                _window = new Tw1st3rWindow(_manager);
                _window.Show();
            });
        }

        public void Unload()
        {
            _manager?.Stop();
            Application.Current.Dispatcher.Invoke(() =>
            {
                _window?.Close();
                _window = null;
            });
        }
    }

    public class EnhancementSet
    {
        public string @class { get; set; } = "";
        public string weapon { get; set; } = "";
        public string helm { get; set; } = "";
        public string cape { get; set; } = "";
    }

    public class BuildSet : EnhancementSet
    {
        public string name { get; set; } = "";
    }

    public class ClassRecommendation
    {
        public string role { get; set; } = "";
        public string description { get; set; } = "";
        public List<string> potions { get; set; } = new List<string>();
        public List<BuildSet> builds { get; set; } = new List<BuildSet>();
        public string tips { get; set; } = "";
        public string tonic { get; set; } = "";
        public string elixir { get; set; } = "";
        public string potion { get; set; } = "";
        public string scroll { get; set; } = "";
        public EnhancementSet enhancements { get; set; } = new EnhancementSet();
    }

    public static class RecommendationStore
    {
        private static Dictionary<string, ClassRecommendation> _map = new(StringComparer.OrdinalIgnoreCase);
        public static IReadOnlyDictionary<string, ClassRecommendation> Map => _map;
        public static string LoadInfo { get; private set; } = "";

        public static void Load()
        {
            try
            {
                var candidates = new List<string>();
                try
                {
                    var dllPath = typeof(RecommendationStore).Assembly.Location;
                    if (!string.IsNullOrEmpty(dllPath))
                    {
                        var dir = Path.GetDirectoryName(dllPath);
                        if (dir != null) candidates.Add(Path.Combine(dir, "class_recommendations.json"));
                    }
                }
                catch { }
                candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Skua", "plugins", "class_recommendations.json"));
                candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Skua", "plugins", "class_recommendations.json"));

                string? jsonPath = candidates.FirstOrDefault(File.Exists);
                if (jsonPath == null)
                {
                    LoadInfo = $"class_recommendations.json not found. Tried: {string.Join("; ", candidates)}";
                    _map = new(StringComparer.OrdinalIgnoreCase);
                    return;
                }
                var raw = File.ReadAllText(jsonPath);
                using var doc = JsonDocument.Parse(raw);
                var dict = new Dictionary<string, ClassRecommendation>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Name.StartsWith("_")) continue;
                    if (prop.Value.ValueKind != JsonValueKind.Object) continue;
                    var rec = new ClassRecommendation();
                    foreach (var f in prop.Value.EnumerateObject())
                    {
                        switch (f.Name.ToLowerInvariant())
                        {
                            case "role":        rec.role = f.Value.GetString() ?? ""; break;
                            case "description": rec.description = f.Value.GetString() ?? ""; break;
                            case "tips":        rec.tips = f.Value.GetString() ?? ""; break;
                            case "tonic":       rec.tonic = f.Value.GetString() ?? ""; break;
                            case "elixir":      rec.elixir = f.Value.GetString() ?? ""; break;
                            case "potion":      rec.potion = f.Value.GetString() ?? ""; break;
                            case "scroll":      rec.scroll = f.Value.GetString() ?? ""; break;
                            case "potions":
                                if (f.Value.ValueKind == JsonValueKind.Array)
                                    foreach (var p in f.Value.EnumerateArray())
                                        rec.potions.Add(p.GetString() ?? "");
                                break;
                            case "builds":
                                if (f.Value.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var bElem in f.Value.EnumerateArray())
                                    {
                                        if (bElem.ValueKind != JsonValueKind.Object) continue;
                                        var bs = new BuildSet();
                                        foreach (var bp in bElem.EnumerateObject())
                                        {
                                            var v = bp.Value.GetString() ?? "";
                                            switch (bp.Name.ToLowerInvariant())
                                            {
                                                case "name":   bs.name = v; break;
                                                case "class":  bs.@class = v; break;
                                                case "weapon": bs.weapon = v; break;
                                                case "helm":   bs.helm = v; break;
                                                case "cape":   bs.cape = v; break;
                                            }
                                        }
                                        rec.builds.Add(bs);
                                    }
                                }
                                break;
                            case "enhancements":
                                if (f.Value.ValueKind == JsonValueKind.Object)
                                {
                                    foreach (var e in f.Value.EnumerateObject())
                                    {
                                        var v = e.Value.GetString() ?? "";
                                        switch (e.Name.ToLowerInvariant())
                                        {
                                            case "class":  rec.enhancements.@class = v; break;
                                            case "weapon": rec.enhancements.weapon = v; break;
                                            case "helm":   rec.enhancements.helm = v; break;
                                            case "cape":   rec.enhancements.cape = v; break;
                                        }
                                    }
                                }
                                break;
                        }
                    }
                    dict[prop.Name] = rec;
                }
                _map = dict;
                LoadInfo = $"Loaded {_map.Count} classes from {jsonPath}";
            }
            catch (Exception ex)
            {
                _map = new(StringComparer.OrdinalIgnoreCase);
                LoadInfo = "Load error: " + ex.Message;
            }
        }

        public static ClassRecommendation? Lookup(string className)
        {
            if (string.IsNullOrWhiteSpace(className)) return null;
            if (_map.TryGetValue(className, out var rec)) return rec;
            foreach (var kvp in _map)
            {
                if (className.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    kvp.Key.IndexOf(className, StringComparison.OrdinalIgnoreCase) >= 0)
                    return kvp.Value;
            }
            return null;
        }
    }

    public class PotionSlot : INotifyPropertyChanged
    {
        public string Label { get; }
        public string[] Keywords { get; }
        public bool IsCustom { get; }
        public ObservableCollection<string> Options { get; } = new ObservableCollection<string>();

        private bool _enabled = true;
        public bool Enabled { get => _enabled; set { _enabled = value; OnChanged(); } }

        private string _selectedItem = "";
        public string SelectedItem { get => _selectedItem; set { _selectedItem = value ?? ""; OnChanged(); } }

        private double _cooldown;
        public double CooldownSeconds { get => _cooldown; set { _cooldown = value; OnChanged(); } }

        private DateTime _lastUsed = DateTime.MinValue;
        public DateTime LastUsed { get => _lastUsed; set { _lastUsed = value; OnChanged(); OnChanged(nameof(LastUsedText)); } }

        public string LastUsedText => _lastUsed == DateTime.MinValue ? "—" : _lastUsed.ToString("HH:mm:ss");

        // Stock floor: stop using this item once inventory hits this quantity.
        // 0 = no reserve (use down to the last one).
        private int _reserveQty = 0;
        public int ReserveQty { get => _reserveQty; set { _reserveQty = Math.Max(0, value); OnChanged(); } }

        public int SkillIndex { get; set; } = 5;

        public PotionSlot(string label, double cd, string[] keywords, bool isCustom = false)
        {
            Label = label;
            CooldownSeconds = cd;
            Keywords = keywords;
            IsCustom = isCustom;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged([CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class Tw1st3rManager : INotifyPropertyChanged
    {
        private readonly IScriptInterface _bot;
        private CancellationTokenSource? _cts;

        private const int EquipSettleMs = 1500;
        private const int TriggerRetryMs = 800;
        private const int MaxTriggerAttempts = 6;

        public PotionSlot Tonic { get; } = new PotionSlot("Tonic", 900, new[] { "tonic" });
        public PotionSlot Elixir { get; } = new PotionSlot("Elixir", 900,
            new[] { "elixir", "brew", "infusion", "concoction", "tincture", "essence", "blessing" });
        public PotionSlot Potion { get; } = new PotionSlot("Potion", 30,
            new[] { "health potion", "hp pot", "mana potion", "mp pot", "potion", "philtre", "draught" });
        public PotionSlot Scroll { get; } = new PotionSlot("Scroll", 15, new[] { "scroll" });
        public PotionSlot Custom { get; } = new PotionSlot("Custom", 60, Array.Empty<string>(), isCustom: true)
        { Enabled = false };

        public IEnumerable<PotionSlot> Slots => new[] { Tonic, Elixir, Potion, Scroll, Custom };

        private static readonly string[] ExcludeKeywords =
        {
            "empty", "bottle", "fragment", "shard", "recipe", "ingredient", "material",
            "gem", "stone", "rune", "dust", "core", "ore", "ash", "bone", "fang",
            "tooth", "scale", "feather", "leather", "hide", "horn", "claw", "wing",
            "token", "badge", "fiend", "treasure", "???", "volcanic"
        };

        private int _hpThreshold = 50;
        public int HpThresholdPercent { get => _hpThreshold; set { _hpThreshold = Math.Clamp(value, 0, 100); OnChanged(); } }

        private bool _useHpThreshold = false;
        public bool UseHpThreshold { get => _useHpThreshold; set { _useHpThreshold = value; OnChanged(); } }

        private bool _potionSpamMode = false;
        public bool PotionSpamMode { get => _potionSpamMode; set { _potionSpamMode = value; OnChanged(); } }

        private bool _scrollSpamMode = false;
        public bool ScrollSpamMode { get => _scrollSpamMode; set { _scrollSpamMode = value; OnChanged(); } }

        private string _combatChoice = "Potion";
        public string CombatChoice { get => _combatChoice; set { _combatChoice = value ?? "Potion"; OnChanged(); } }
        public IEnumerable<string> CombatChoices => new[] { "Potion", "Scroll", "None" };

        private bool _syncWithSkills = false;
        public bool SyncWithSkills { get => _syncWithSkills; set { _syncWithSkills = value; OnChanged(); } }

        private bool _pauseOnRefresh = true;
        public bool PauseAttacksOnRefresh { get => _pauseOnRefresh; set { _pauseOnRefresh = value; OnChanged(); } }

        private bool _skipIfBuffActive = true;
        public bool SkipIfBuffActive { get => _skipIfBuffActive; set { _skipIfBuffActive = value; OnChanged(); } }

        // --- Potion saver: boss-only mode -----------------------------------
        // When on, the combat consumable only fires against bosses (target max
        // HP >= BossHpThreshold), so trash mobs don't drain your supply.
        private bool _bossOnly = false;
        public bool BossOnly { get => _bossOnly; set { _bossOnly = value; OnChanged(); } }

        private int _bossHpThreshold = 100000;
        public int BossHpThreshold { get => _bossHpThreshold; set { _bossHpThreshold = Math.Max(0, value); OnChanged(); } }

        private string _status = "Idle";
        public string Status { get => _status; private set { _status = value; OnChanged(); } }

        private string _phase = "Idle";
        public string Phase { get => _phase; private set { _phase = value; OnChanged(); } }

        private string _currentClass = "—";
        public string CurrentClassDisplay { get => _currentClass; private set { _currentClass = value; OnChanged(); } }

        private string _recommendation = "—";
        public string RecommendationDisplay { get => _recommendation; private set { _recommendation = value; OnChanged(); } }

        public ObservableCollection<string> AvailableBuilds { get; } = new ObservableCollection<string>();

        private string _selectedBuild = "";
        public string SelectedBuild
        {
            get => _selectedBuild;
            set
            {
                var v = value ?? "";
                if (_selectedBuild == v) return;
                _selectedBuild = v;
                OnChanged();
                RefreshRecommendationText();
            }
        }

        private string _classRole = "";
        public string ClassRoleDisplay { get => _classRole; private set { _classRole = value; OnChanged(); } }

        private string _classTips = "";
        public string ClassTipsDisplay { get => _classTips; private set { _classTips = value; OnChanged(); } }

        private string _classDescription = "";
        public string ClassDescriptionDisplay { get => _classDescription; private set { _classDescription = value; OnChanged(); } }

        private string _classPotions = "";
        public string ClassPotionsDisplay { get => _classPotions; private set { _classPotions = value; OnChanged(); } }

        public bool IsRunning { get; private set; }

        public Tw1st3rManager(IScriptInterface bot)
        {
            _bot = bot;
            RecommendationStore.Load();
            RefreshInventory();
            UpdateClassDisplay();
        }

        private string _lastDisplayedClass = "";

        public void UpdateClassDisplay()
        {
            try
            {
                var cls = _bot.Player?.CurrentClass?.Name ?? "—";
                if (cls == _lastDisplayedClass) return;
                _lastDisplayedClass = cls;

                CurrentClassDisplay = cls;
                var rec = RecommendationStore.Lookup(cls);

                if (rec == null)
                {
                    RecommendationDisplay = "no data for this class";
                    ClassRoleDisplay = "";
                    ClassDescriptionDisplay = "";
                    ClassTipsDisplay = "";
                    ClassPotionsDisplay = "";
                    Application.Current?.Dispatcher.BeginInvoke(new Action(() => AvailableBuilds.Clear()));
                    return;
                }

                ClassRoleDisplay = rec.role ?? "";
                ClassDescriptionDisplay = rec.description ?? "";
                ClassTipsDisplay = rec.tips ?? "";
                ClassPotionsDisplay = rec.potions != null && rec.potions.Count > 0
                    ? string.Join("\n", rec.potions.Select(p => "• " + p))
                    : "—";

                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    AvailableBuilds.Clear();
                    if (rec.builds != null && rec.builds.Count > 0)
                        foreach (var b in rec.builds) AvailableBuilds.Add(b.name);
                    else
                        AvailableBuilds.Add("Default");

                    if (string.IsNullOrEmpty(_selectedBuild) || !AvailableBuilds.Contains(_selectedBuild))
                    {
                        _selectedBuild = AvailableBuilds[AvailableBuilds.Count - 1];
                        OnChanged(nameof(SelectedBuild));
                    }
                    RefreshRecommendationText();
                }));
            }
            catch { }
        }

        private void RefreshRecommendationText()
        {
            try
            {
                var rec = RecommendationStore.Lookup(_lastDisplayedClass);
                if (rec == null) { RecommendationDisplay = "no data for this class"; return; }
                EnhancementSet enh = rec.enhancements;
                if (rec.builds != null && rec.builds.Count > 0)
                {
                    var picked = rec.builds.FirstOrDefault(b => string.Equals(b.name, _selectedBuild, StringComparison.OrdinalIgnoreCase));
                    if (picked != null) enh = picked;
                }
                string Show(string s) => string.IsNullOrEmpty(s) ? "—" : s;
                RecommendationDisplay =
                    $"  class · {Show(enh.@class)}\n" +
                    $"  weapon · {Show(enh.weapon)}\n" +
                    $"  helm · {Show(enh.helm)}\n" +
                    $"  cape · {Show(enh.cape)}";
            }
            catch { }
        }

        public void ApplyRecommendation()
        {
            var cls = _bot.Player?.CurrentClass?.Name ?? "";
            var rec = RecommendationStore.Lookup(cls);
            if (rec == null) { Status = $"no recommendation for '{cls}'"; return; }

            string PickPotion(IEnumerable<string> known)
            {
                if (rec.potions != null)
                    foreach (var p in rec.potions)
                        foreach (var k in known)
                            if (p.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0) return p;
                return "";
            }

            var tonic  = !string.IsNullOrEmpty(rec.tonic)  ? rec.tonic  : PickPotion(new[] { "Tonic" });
            var elixir = !string.IsNullOrEmpty(rec.elixir) ? rec.elixir : PickPotion(new[] { "Elixir", "Cordial" });
            var potion = !string.IsNullOrEmpty(rec.potion) ? rec.potion : PickPotion(new[] { "Potion", "Philtre", "Draught" });
            var scroll = !string.IsNullOrEmpty(rec.scroll) ? rec.scroll : PickPotion(new[] { "Scroll" });

            ApplySlot(Tonic,  tonic);
            ApplySlot(Elixir, elixir);
            ApplySlot(Potion, potion);
            ApplySlot(Scroll, scroll);
            Custom.Enabled = false;

            var buildSuffix = string.IsNullOrEmpty(_selectedBuild) ? "" : $" / {_selectedBuild}";
            Status = $"applied · {cls}{buildSuffix}";
        }

        private void ApplySlot(PotionSlot slot, string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                slot.Enabled = false;
                slot.SelectedItem = "";
                return;
            }
            slot.Enabled = true;
            var hit = slot.Options.FirstOrDefault(o => string.Equals(o, itemName, StringComparison.OrdinalIgnoreCase));
            if (hit != null) { slot.SelectedItem = hit; return; }
            hit = slot.Options.FirstOrDefault(o => o.IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0);
            if (hit != null) slot.SelectedItem = hit;
            else slot.SelectedItem = "";
        }

        public void ReloadRecommendations()
        {
            RecommendationStore.Load();
            _lastDisplayedClass = "";
            UpdateClassDisplay();
            Status = RecommendationStore.LoadInfo;
        }

        public void RefreshInventory()
        {
            try
            {
                List<InventoryItem> items;
                try { items = _bot.Inventory?.Items?.Where(i => i != null).ToList() ?? new List<InventoryItem>(); }
                catch { items = new List<InventoryItem>(); }

                int totalMatched = 0;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var slot in Slots)
                    {
                        var prev = slot.SelectedItem;
                        slot.Options.Clear();

                        List<string> hits;
                        if (slot.IsCustom)
                        {
                            hits = items.Select(i => i.Name)
                                .Where(n => !string.IsNullOrWhiteSpace(n))
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .OrderBy(n => n).ToList();
                        }
                        else
                        {
                            hits = items.Where(it => !string.IsNullOrWhiteSpace(it.Name) &&
                                slot.Keywords.Any(k => it.Name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0) &&
                                !ExcludeKeywords.Any(x => it.Name.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0))
                                .Select(it => it.Name)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .OrderBy(n => n).ToList();
                            totalMatched += hits.Count;
                        }

                        foreach (var n in hits) slot.Options.Add(n);

                        if (!string.IsNullOrEmpty(prev) && slot.Options.Contains(prev, StringComparer.OrdinalIgnoreCase))
                            slot.SelectedItem = slot.Options.First(o => string.Equals(o, prev, StringComparison.OrdinalIgnoreCase));
                        else if (slot.Options.Count > 0 && !slot.IsCustom && string.IsNullOrEmpty(prev))
                            slot.SelectedItem = slot.Options[0];
                    }
                });

                Status = items.Count == 0
                    ? "inventory empty"
                    : $"refreshed · {totalMatched} / {items.Count}";
            }
            catch (Exception ex) { Status = "refresh err · " + ex.Message; }
        }

        public void Start()
        {
            if (IsRunning) return;
            _cts = new CancellationTokenSource();
            IsRunning = true;
            OnChanged(nameof(IsRunning));
            Status = "starting";
            Task.Run(() => MainLoop(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            IsRunning = false;
            Phase = "Idle";
            Status = "stopped";
            OnChanged(nameof(IsRunning));
        }

        public void UseSlotNow(PotionSlot slot) => Task.Run(() => FireSlotOnce(slot, CancellationToken.None, ignoreCooldown: true));

        private DateTime _lastRefreshAttempt = DateTime.MinValue;
        private bool _wasAlive = true;
        private string _lastClassName = string.Empty;

        private async Task MainLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && (!_bot.Player.LoggedIn || !_bot.Player.Alive))
                {
                    Phase = "wait · login";
                    await Task.Delay(1500, ct);
                }
                bool skillsWereRunning = false;
                if (SyncWithSkills)
                {
                    try { if (IsSkillsRunning()) { skillsWereRunning = true; _bot.Skills.Pause(); await Task.Delay(300, ct); } } catch { }
                }
                await BuffSequence(ct);
                PotionSlot? combatSlot = GetCombatSlot();
                if (combatSlot != null && combatSlot.Enabled && !string.IsNullOrWhiteSpace(combatSlot.SelectedItem))
                {
                    Phase = $"equip · {combatSlot.Label}";
                    EquipItem(combatSlot);
                    await Task.Delay(EquipSettleMs, ct);
                }
                if (SyncWithSkills)
                {
                    try { if (skillsWereRunning) _bot.Skills.Resume(); else if (!IsSkillsRunning()) _bot.Skills.Start(); } catch { }
                }
                _wasAlive = _bot.Player.Alive;
                _lastClassName = SafeGetClassName();
                Phase = "combat-ready";

                while (!ct.IsCancellationRequested)
                {
                    UpdateClassDisplay();
                    string currentClass = SafeGetClassName();
                    if (!string.IsNullOrEmpty(currentClass) && !string.IsNullOrEmpty(_lastClassName)
                        && !string.Equals(currentClass, _lastClassName, StringComparison.OrdinalIgnoreCase))
                    {
                        Phase = $"class swap · {currentClass}";
                        Tonic.LastUsed = Elixir.LastUsed = Scroll.LastUsed = DateTime.MinValue;
                        _lastRefreshAttempt = DateTime.MinValue;
                        await Task.Delay(1500, ct);
                        await RefreshBuffs(ct);
                        var c2 = GetCombatSlot();
                        if (c2 != null && c2.Enabled && !string.IsNullOrWhiteSpace(c2.SelectedItem))
                        { EquipItem(c2); await Task.Delay(EquipSettleMs, ct); }
                        Phase = "combat-ready";
                    }
                    _lastClassName = currentClass;
                    bool aliveNow = _bot.Player.Alive;
                    if (!_wasAlive && aliveNow)
                    {
                        Phase = "respawn · re-buff";
                        Tonic.LastUsed = Elixir.LastUsed = Scroll.LastUsed = DateTime.MinValue;
                        _lastRefreshAttempt = DateTime.MinValue;
                        await Task.Delay(1500, ct);
                        await RefreshBuffs(ct);
                        var cs2 = GetCombatSlot();
                        if (cs2 != null && cs2.Enabled && !string.IsNullOrWhiteSpace(cs2.SelectedItem))
                        { EquipItem(cs2); await Task.Delay(EquipSettleMs, ct); }
                        Phase = "combat-ready";
                    }
                    _wasAlive = aliveNow;
                    if (!aliveNow) { Phase = "dead · wait"; await Task.Delay(700, ct); continue; }

                    bool skillsActive = !SyncWithSkills || IsSkillsRunning();
                    PotionSlot? cs = GetCombatSlot();
                    // Loops now ALWAYS require combat. PotionSpamMode = "Loop / ignore HP",
                    // ScrollSpamMode = "Loop" - both just loop the slot on cooldown while
                    // in combat. The only difference vs. the default is that PotionSpamMode
                    // skips the HP threshold gate.
                    bool combatGate = _bot.Player.InCombat && (!BossOnly || IsFightingBoss());
                    if (skillsActive && cs != null && cs.Enabled && _bot.Player.Alive && combatGate)
                    {
                        bool gateOk = true;
                        if (cs == Potion && UseHpThreshold && !PotionSpamMode)
                        {
                            var maxHp = _bot.Player.MaxHealth;
                            if (maxHp > 0)
                            {
                                int pct = (_bot.Player.Health * 100) / maxHp;
                                gateOk = pct <= HpThresholdPercent;
                            }
                        }
                        // Stock floor: don't fire if we'd dip to/below the reserve.
                        if (gateOk && HasStock(cs) &&
                            (DateTime.Now - cs.LastUsed).TotalSeconds >= cs.CooldownSeconds)
                        {
                            if (TriggerSlot(cs.SkillIndex)) cs.LastUsed = DateTime.Now;
                        }
                    }
                    bool refreshAllowed = (DateTime.Now - _lastRefreshAttempt).TotalSeconds >= 30;
                    bool needsRefresh = refreshAllowed && (
                        (Tonic.Enabled && IsExpired(Tonic)) ||
                        (Elixir.Enabled && IsExpired(Elixir)) ||
                        (Scroll.Enabled && IsExpired(Scroll)));
                    if (needsRefresh)
                    {
                        await RefreshBuffs(ct);
                        var combatRe = GetCombatSlot();
                        if (combatRe != null && combatRe.Enabled && !string.IsNullOrWhiteSpace(combatRe.SelectedItem))
                        { EquipItem(combatRe); await Task.Delay(EquipSettleMs, ct); }
                        Phase = "combat-ready";
                    }
                    if (Custom.Enabled && !string.IsNullOrWhiteSpace(Custom.SelectedItem) && IsExpired(Custom))
                        _ = FireSlotOnce(Custom, ct);
                    await Task.Delay(700, ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Status = "loop err · " + ex.Message; }
        }

        private bool IsExpired(PotionSlot slot)
            => slot.LastUsed == DateTime.MinValue ||
               (DateTime.Now - slot.LastUsed).TotalSeconds >= slot.CooldownSeconds;

        private PotionSlot? GetCombatSlot()
        {
            if (string.Equals(CombatChoice, "Potion", StringComparison.OrdinalIgnoreCase)) return Potion;
            if (string.Equals(CombatChoice, "Scroll", StringComparison.OrdinalIgnoreCase)) return Scroll;
            return null;
        }

        private async Task BuffSequence(CancellationToken ct)
        {
            bool wasAttacking = false;
            if (PauseAttacksOnRefresh)
            {
                wasAttacking = !_bot.Combat.StopAttacking;
                _bot.Combat.StopAttacking = true;
                await Task.Delay(200, ct);
            }
            var combatSlot = GetCombatSlot();
            try
            {
                if (Tonic.Enabled && !string.IsNullOrWhiteSpace(Tonic.SelectedItem))
                { Phase = "apply · tonic"; await FireSlotOnce(Tonic, ct); }
                if (Elixir.Enabled && !string.IsNullOrWhiteSpace(Elixir.SelectedItem))
                { Phase = "apply · elixir"; await FireSlotOnce(Elixir, ct); }
                if (Scroll.Enabled && !string.IsNullOrWhiteSpace(Scroll.SelectedItem) && combatSlot != Scroll)
                { Phase = "apply · scroll"; await FireSlotOnce(Scroll, ct); }
                if (Potion.Enabled && !string.IsNullOrWhiteSpace(Potion.SelectedItem) && combatSlot != Potion)
                { Phase = "apply · potion"; await FireSlotOnce(Potion, ct); }
            }
            finally
            {
                if (PauseAttacksOnRefresh && wasAttacking) _bot.Combat.StopAttacking = false;
            }
        }

        private async Task RefreshBuffs(CancellationToken ct)
        {
            Phase = "refresh · buffs";
            Status = "buffs expired";
            _lastRefreshAttempt = DateTime.Now;
            bool skillsWereRunning = false;
            if (SyncWithSkills)
            {
                try { if (IsSkillsRunning()) { skillsWereRunning = true; _bot.Skills.Pause(); await Task.Delay(200, ct); } } catch { }
            }
            try { await BuffSequence(ct); }
            finally
            {
                if (SyncWithSkills && skillsWereRunning) { try { _bot.Skills.Resume(); } catch { } }
            }
        }

        private async Task FireSlotOnce(PotionSlot slot, CancellationToken ct, bool ignoreCooldown = false)
        {
            if (!slot.Enabled || string.IsNullOrWhiteSpace(slot.SelectedItem)) return;
            if (!ignoreCooldown && !IsExpired(slot)) return;
            if (!ignoreCooldown && SkipIfBuffActive && IsBuffActiveForItem(slot.SelectedItem))
            {
                Status = $"{slot.Label} active · skip";
                return;
            }
            // Stock floor: keep the reserve. (Manual "Use" ignores this.)
            if (!ignoreCooldown && !HasStock(slot))
            {
                Status = $"{slot.Label} · reserve reached ({slot.ReserveQty})";
                return;
            }
            try
            {
                InventoryItem? inv = null;
                try { inv = _bot.Inventory.GetItem(slot.SelectedItem); } catch { }
                if (inv == null) { Status = $"{slot.SelectedItem} not in inv"; return; }
                EquipItem(slot, inv);
                try { await Task.Delay(EquipSettleMs, ct); } catch { return; }
                for (int attempt = 1; attempt <= MaxTriggerAttempts; attempt++)
                {
                    if (TriggerSlot(slot.SkillIndex))
                    {
                        slot.LastUsed = DateTime.Now;
                        Status = $"{slot.Label} · {inv.Name}";
                        return;
                    }
                    try { await Task.Delay(TriggerRetryMs, ct); } catch { return; }
                }
                Status = $"{slot.Label} · slot busy";
            }
            catch (Exception ex) { Status = $"{slot.Label} err · {ex.Message}"; }
        }

        private void EquipItem(PotionSlot slot, InventoryItem? inv = null)
        {
            try
            {
                inv ??= _bot.Inventory.GetItem(slot.SelectedItem);
                if (inv == null) return;
                _bot.Inventory.EquipUsableItem(inv);
            }
            catch (Exception ex) { Status = "equip err · " + ex.Message; }
        }

        private bool TriggerSlot(int skillIndex)
        {
            try
            {
                var result = _bot.Flash.Call("useSkill", skillIndex);
                return string.Equals(result, "true", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private bool IsSkillsRunning()
        {
            try { return _bot.Skills?.TimerRunning ?? false; }
            catch { return false; }
        }

        // Stock floor: true if using the item now keeps quantity above the
        // reserve. Reserve 0 = always allowed.
        private bool HasStock(PotionSlot slot)
        {
            if (slot.ReserveQty <= 0) return true;
            try
            {
                var inv = _bot.Inventory.GetItem(slot.SelectedItem);
                if (inv == null) return false;
                return inv.Quantity > slot.ReserveQty;
            }
            catch { return true; }
        }

        // Boss detection: current target's max HP at/above the threshold.
        private bool IsFightingBoss()
        {
            try
            {
                var t = _bot.Player?.Target;
                return t != null && t.MaxHP >= BossHpThreshold;
            }
            catch { return false; }
        }

        private string SafeGetClassName()
        {
            try { return _bot.Player?.CurrentClass?.Name ?? string.Empty; }
            catch { return string.Empty; }
        }

        private bool IsBuffActiveForItem(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName)) return false;
            try
            {
                if (_bot.Self.HasActiveAura(itemName)) return true;
                string n = itemName;
                string[] suffixes = { " Tonic", " Elixir", " Scroll", " Potion", " Draught", " Philtre",
                                       " Brew", " Infusion", " Concoction", " Tincture", " Essence", " Blessing" };
                foreach (var suf in suffixes)
                    if (n.EndsWith(suf, StringComparison.OrdinalIgnoreCase))
                    {
                        var s = n.Substring(0, n.Length - suf.Length).Trim();
                        if (!string.IsNullOrEmpty(s) && _bot.Self.HasActiveAura(s)) return true;
                    }
                string[] prefixes = { "Potent ", "Major ", "Greater ", "Lesser ", "Unstable " };
                foreach (var pre in prefixes)
                    if (n.StartsWith(pre, StringComparison.OrdinalIgnoreCase))
                    {
                        var s = n.Substring(pre.Length).Trim();
                        if (!string.IsNullOrEmpty(s) && _bot.Self.HasActiveAura(s)) return true;
                    }
            }
            catch { }
            return false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged([CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
