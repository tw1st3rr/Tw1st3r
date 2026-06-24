// Tw1st3r Plugin UI — clean, simple, single-window two-panel layout.
// One accent color (sky blue), flat cards, no tabs, everything visible at once.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Tw1st3r
{
    public class Tw1st3rWindow : Window
    {
        // --- Palette: matches Skua's default "Skua" theme. ---------------------
        //   Primary  = #7d9aa9 (slate blue-grey)   Text on primary = #000000
        //   Surface  = Material dark.
        private static readonly Color BgC      = Color.FromRgb(0x12, 0x12, 0x12);  // Material dark surface
        private static readonly Color PanelC   = Color.FromRgb(0x1e, 0x1e, 0x1e);  // elevated surface
        private static readonly Color EdgeC    = Color.FromRgb(0x33, 0x33, 0x33);
        private static readonly Color AccentC  = Color.FromRgb(0x7d, 0x9a, 0xa9);  // Skua primary (slate)
        private static readonly Color OnAccent = Color.FromRgb(0x00, 0x00, 0x00);  // text on primary
        private static readonly Color FgC      = Color.FromRgb(0xf0, 0xf0, 0xf0);
        private static readonly Color MutedC   = Color.FromRgb(0x9e, 0x9e, 0x9e);
        private static readonly Color GreenC   = Color.FromRgb(0x4c, 0xaf, 0x50);  // Material green
        private static readonly Color RedC     = Color.FromRgb(0xf4, 0x43, 0x36);  // Material red

        private static readonly Brush Bg     = new SolidColorBrush(BgC);
        private static readonly Brush Panel  = new SolidColorBrush(PanelC);
        private static readonly Brush Edge   = new SolidColorBrush(EdgeC);
        private static readonly Brush Accent = new SolidColorBrush(AccentC);
        private static readonly Brush Fg     = new SolidColorBrush(FgC);
        private static readonly Brush Muted  = new SolidColorBrush(MutedC);
        private static readonly Brush Green  = new SolidColorBrush(GreenC);
        private static readonly Brush Red    = new SolidColorBrush(RedC);
        private static readonly Brush Input  = new SolidColorBrush(Color.FromRgb(0x2a, 0x2e, 0x36));

        private static readonly FontFamily UiFont = new FontFamily("Segoe UI");

        private readonly Tw1st3rManager _mgr;

        public Tw1st3rWindow(Tw1st3rManager mgr)
        {
            _mgr = mgr;
            Title = "Tw1st3r Plugin";
            Width = 660;
            Height = 560;
            MinWidth = 600;
            MinHeight = 480;
            Background = Bg;
            Foreground = Fg;
            FontFamily = UiFont;
            DataContext = mgr;

            var root = new DockPanel { LastChildFill = true, Margin = new Thickness(12) };

            // ── Header ────────────────────────────────────────────────────
            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
            var brand = new TextBlock
            {
                Text = "TW1ST3R",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Accent
            };
            DockPanel.SetDock(brand, Dock.Left);
            header.Children.Add(brand);

            var status = new TextBlock
            {
                Foreground = Muted,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Right,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            status.SetBinding(TextBlock.TextProperty, new Binding(nameof(Tw1st3rManager.Status)));
            header.Children.Add(status);
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);

            // ── Action bar (bottom) ───────────────────────────────────────
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
            actions.Children.Add(Btn("Start", Green, BgC, (_, __) => _mgr.Start()));
            actions.Children.Add(Btn("Stop", Red, BgC, (_, __) => _mgr.Stop()));
            actions.Children.Add(Btn("Refresh", Accent, OnAccent, (_, __) => _mgr.RefreshInventory()));
            var phase = new TextBlock
            {
                Foreground = Accent,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };
            phase.SetBinding(TextBlock.TextProperty, new Binding(nameof(Tw1st3rManager.Phase)));
            actions.Children.Add(phase);
            DockPanel.SetDock(actions, Dock.Bottom);
            root.Children.Add(actions);

            // ── Body: two columns ─────────────────────────────────────────
            var body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var left = BuildLeftPanel();
            Grid.SetColumn(left, 0);
            body.Children.Add(left);

            var right = BuildRightPanel();
            Grid.SetColumn(right, 1);
            body.Children.Add(right);

            root.Children.Add(body);
            Content = root;
        }

        // ===================== LEFT: slots + settings ==========================
        private UIElement BuildLeftPanel()
        {
            var card = Card(new Thickness(0, 0, 6, 0));
            var stack = (StackPanel)card.Child!;

            stack.Children.Add(SectionTitle("CONSUMABLES"));

            foreach (var slot in _mgr.Slots)
                stack.Children.Add(SlotRow(slot));

            // Combat consumable picker
            var combatRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            combatRow.Children.Add(new TextBlock
            {
                Text = "Combat:",
                Foreground = Muted,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            });
            var combatCombo = new ComboBox { Width = 90, ItemsSource = _mgr.CombatChoices };
            combatCombo.SetBinding(ComboBox.SelectedItemProperty,
                new Binding(nameof(Tw1st3rManager.CombatChoice)) { Source = _mgr, Mode = BindingMode.TwoWay });
            Skin(combatCombo);
            combatRow.Children.Add(combatCombo);
            stack.Children.Add(combatRow);

            // Divider
            stack.Children.Add(Divider());

            // Settings (compact checkboxes)
            stack.Children.Add(SectionTitle("OPTIONS"));

            var hpRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            hpRow.Children.Add(Check("Use HP %", () => _mgr.UseHpThreshold, v => _mgr.UseHpThreshold = v, 90));
            var hpBox = new TextBox { Width = 44, Text = _mgr.HpThresholdPercent.ToString(), VerticalAlignment = VerticalAlignment.Center };
            Skin(hpBox);
            hpBox.LostFocus += (s, e) => { if (int.TryParse(hpBox.Text, out var v)) _mgr.HpThresholdPercent = v; hpBox.Text = _mgr.HpThresholdPercent.ToString(); };
            hpRow.Children.Add(hpBox);
            stack.Children.Add(hpRow);

            stack.Children.Add(Check("Potion (Loop / ignore HP only)", () => _mgr.PotionSpamMode, v => _mgr.PotionSpamMode = v));
            stack.Children.Add(Check("Scroll (Loop)", () => _mgr.ScrollSpamMode, v => _mgr.ScrollSpamMode = v));
            stack.Children.Add(Check("Sync with Skills tab", () => _mgr.SyncWithSkills, v => _mgr.SyncWithSkills = v));
            stack.Children.Add(Check("Pause attacks while refreshing", () => _mgr.PauseAttacksOnRefresh, v => _mgr.PauseAttacksOnRefresh = v));
            stack.Children.Add(Check("Skip if buff already active", () => _mgr.SkipIfBuffActive, v => _mgr.SkipIfBuffActive = v));

            // --- Potion saver -------------------------------------------------
            stack.Children.Add(Divider());
            stack.Children.Add(SectionTitle("POTION SAVER"));

            var bossRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            bossRow.Children.Add(Check("Boss only (skip trash)", () => _mgr.BossOnly, v => _mgr.BossOnly = v, 150));
            bossRow.Children.Add(new TextBlock { Text = "HP≥", Foreground = Muted, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 4, 0) });
            var bossBox = new TextBox { Width = 70, Text = _mgr.BossHpThreshold.ToString(), VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Only fire when the target's max HP is at or above this number" };
            Skin(bossBox);
            bossBox.LostFocus += (s, e) => { if (int.TryParse(bossBox.Text, out var v)) _mgr.BossHpThreshold = v; bossBox.Text = _mgr.BossHpThreshold.ToString(); };
            bossRow.Children.Add(bossBox);
            stack.Children.Add(bossRow);

            stack.Children.Add(new TextBlock
            {
                Text = "Set a 'keep' number per slot above to never use your last N.",
                Foreground = Muted, FontSize = 10, FontStyle = FontStyles.Italic,
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 2)
            });

            return new ScrollViewer { Content = card, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private UIElement SlotRow(PotionSlot slot)
        {
            // A self-contained mini-card per slot: line 1 = enable + item + Use,
            // line 2 = clearly labelled "every Xs" and "keep N" controls.
            var card = new Border
            {
                Background = Input,
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(7, 5, 7, 5),
                Margin = new Thickness(0, 0, 0, 5),
            };
            var outer = new StackPanel();

            // --- line 1: [checkbox label] [item dropdown ............] [Use] ---
            var row1 = new DockPanel { LastChildFill = true };

            var chk = new CheckBox { Foreground = Fg, VerticalAlignment = VerticalAlignment.Center, Width = 64 };
            chk.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(PotionSlot.Enabled)) { Source = slot, Mode = BindingMode.TwoWay });
            chk.Content = new TextBlock { Text = slot.Label, Foreground = Fg, FontSize = 11, FontWeight = FontWeights.SemiBold };
            DockPanel.SetDock(chk, Dock.Left);
            row1.Children.Add(chk);

            var useBtn = SmallBtn("Use", (_, __) => _mgr.UseSlotNow(slot));
            useBtn.Margin = new Thickness(6, 0, 0, 0);
            DockPanel.SetDock(useBtn, Dock.Right);
            row1.Children.Add(useBtn);

            var combo = new ComboBox
            {
                IsEditable = slot.IsCustom,
                ItemsSource = slot.Options,
                VerticalAlignment = VerticalAlignment.Center
            };
            combo.SetBinding(slot.IsCustom ? ComboBox.TextProperty : ComboBox.SelectedItemProperty,
                new Binding(nameof(PotionSlot.SelectedItem)) { Source = slot, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.LostFocus });
            Skin(combo);
            row1.Children.Add(combo);
            outer.Children.Add(row1);

            // --- line 2: every [cd]s    keep [reserve] ------------------------
            var row2 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            row2.Children.Add(MiniLabel2("every"));
            var cdBox = new TextBox { Width = 46, VerticalAlignment = VerticalAlignment.Center, ToolTip = "How often to use this, in seconds" };
            Skin(cdBox);
            cdBox.SetBinding(TextBox.TextProperty,
                new Binding(nameof(PotionSlot.CooldownSeconds)) { Source = slot, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.LostFocus });
            row2.Children.Add(cdBox);
            row2.Children.Add(MiniLabel2("sec"));

            row2.Children.Add(new TextBlock { Width = 16 });  // spacer
            row2.Children.Add(MiniLabel2("keep"));
            var keepBox = new TextBox { Width = 50, VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Stop using once you only have this many left (0 = use them all)" };
            Skin(keepBox);
            keepBox.SetBinding(TextBox.TextProperty,
                new Binding(nameof(PotionSlot.ReserveQty)) { Source = slot, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.LostFocus });
            row2.Children.Add(keepBox);
            row2.Children.Add(MiniLabel2("in reserve"));
            outer.Children.Add(row2);

            card.Child = outer;
            return card;
        }

        private static TextBlock MiniLabel2(string s) => new TextBlock
        {
            Text = s,
            Foreground = Muted,
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(3, 0, 4, 0)
        };

        // ===================== RIGHT: class guide ==============================
        private UIElement BuildRightPanel()
        {
            var card = Card(new Thickness(6, 0, 0, 0));
            var stack = (StackPanel)card.Child!;

            stack.Children.Add(SectionTitle("CLASS GUIDE"));

            var cls = new TextBlock { FontSize = 16, FontWeight = FontWeights.Bold, Foreground = Fg, TextWrapping = TextWrapping.Wrap };
            cls.SetBinding(TextBlock.TextProperty, new Binding(nameof(Tw1st3rManager.CurrentClassDisplay)));
            stack.Children.Add(cls);

            var role = new TextBlock { FontSize = 11, FontStyle = FontStyles.Italic, Foreground = Accent, Margin = new Thickness(0, 0, 0, 4) };
            role.SetBinding(TextBlock.TextProperty, new Binding(nameof(Tw1st3rManager.ClassRoleDisplay)));
            stack.Children.Add(role);

            var desc = new TextBlock { FontSize = 11, Foreground = Muted, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) };
            desc.SetBinding(TextBlock.TextProperty, new Binding(nameof(Tw1st3rManager.ClassDescriptionDisplay)));
            stack.Children.Add(desc);

            // Build picker
            var buildRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            buildRow.Children.Add(new TextBlock { Text = "Build:", Foreground = Muted, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
            var buildCombo = new ComboBox { Width = 150 };
            buildCombo.SetBinding(ComboBox.ItemsSourceProperty, new Binding(nameof(Tw1st3rManager.AvailableBuilds)));
            buildCombo.SetBinding(ComboBox.SelectedItemProperty, new Binding(nameof(Tw1st3rManager.SelectedBuild)) { Mode = BindingMode.TwoWay });
            Skin(buildCombo);
            buildRow.Children.Add(buildCombo);
            stack.Children.Add(buildRow);

            stack.Children.Add(MiniLabel("ENHANCEMENTS"));
            var enh = new TextBlock { FontFamily = new FontFamily("Consolas"), FontSize = 11, Foreground = Fg, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) };
            enh.SetBinding(TextBlock.TextProperty, new Binding(nameof(Tw1st3rManager.RecommendationDisplay)));
            stack.Children.Add(enh);

            stack.Children.Add(MiniLabel("POTIONS"));
            var pot = new TextBlock { FontSize = 11, Foreground = Fg, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 6) };
            pot.SetBinding(TextBlock.TextProperty, new Binding(nameof(Tw1st3rManager.ClassPotionsDisplay)));
            stack.Children.Add(pot);

            stack.Children.Add(MiniLabel("TIPS"));
            var tips = new TextBlock { FontSize = 11, Foreground = Muted, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) };
            tips.SetBinding(TextBlock.TextProperty, new Binding(nameof(Tw1st3rManager.ClassTipsDisplay)));
            stack.Children.Add(tips);

            var btns = new StackPanel { Orientation = Orientation.Horizontal };
            btns.Children.Add(Btn("Apply", Accent, OnAccent, (_, __) => _mgr.ApplyRecommendation()));
            btns.Children.Add(SmallBtn("Reload", (_, __) => _mgr.ReloadRecommendations()));
            stack.Children.Add(btns);

            return new ScrollViewer { Content = card, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        // ===================== HELPERS =========================================
        private static Border Card(Thickness margin) => new Border
        {
            Background = Panel,
            BorderBrush = Edge,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = margin,
            Child = new StackPanel()
        };

        private static TextBlock SectionTitle(string s) => new TextBlock
        {
            Text = s,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = Accent,
            Margin = new Thickness(0, 0, 0, 8)
        };

        private static TextBlock MiniLabel(string s) => new TextBlock
        {
            Text = s,
            FontSize = 9,
            Foreground = Muted,
            Margin = new Thickness(0, 0, 0, 2)
        };

        private static UIElement Divider() => new Border
        {
            Height = 1,
            Background = Edge,
            Margin = new Thickness(0, 12, 0, 12)
        };

        private static CheckBox Check(string text, Func<bool> getter, Action<bool> setter, double? width = null)
        {
            var cb = new CheckBox
            {
                Content = new TextBlock { Text = text, Foreground = Fg, FontSize = 11 },
                IsChecked = getter(),
                Margin = new Thickness(0, 2, 0, 2),
                VerticalAlignment = VerticalAlignment.Center
            };
            if (width.HasValue) cb.Width = width.Value;
            cb.Checked += (_, __) => setter(true);
            cb.Unchecked += (_, __) => setter(false);
            return cb;
        }

        private static Button Btn(string text, Brush bg, Color fg, RoutedEventHandler click)
        {
            var b = new Button
            {
                Content = text,
                Background = bg,
                Foreground = new SolidColorBrush(fg),
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(0, 0, 6, 0),
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            b.Click += click;
            return b;
        }

        private static Button SmallBtn(string text, RoutedEventHandler click)
        {
            var b = new Button
            {
                Content = text,
                Background = Input,
                Foreground = Fg,
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 0, 0),
                BorderBrush = Edge,
                BorderThickness = new Thickness(1),
                FontSize = 11,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            b.Click += click;
            return b;
        }

        private static void Skin(Control c)
        {
            c.Background = Input;
            c.Foreground = Fg;
            c.BorderBrush = Edge;
            c.BorderThickness = new Thickness(1);
            c.FontSize = 11;
            if (c is TextBox tb) { tb.CaretBrush = Accent; tb.Padding = new Thickness(4, 2, 4, 2); }
            if (c is ComboBox cb) cb.Padding = new Thickness(6, 3, 6, 3);
        }
    }
}
