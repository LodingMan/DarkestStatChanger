using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using DarkestStatChanger.Models;

namespace DarkestStatChanger
{
    public class SimulatorPanel : UserControl
    {
        // Data
        private HeroInfo _heroInfo;
        private List<MonsterInfo> _monsters;
        private Dictionary<string, Image> _skillIcons;
        private Dictionary<string, Models.EffectInfo> _effects = new Dictionary<string, Models.EffectInfo>(StringComparer.OrdinalIgnoreCase);
        private MonsterInfo _selectedMonster;
        private int _monsterCurrentHp;
        private int _turnNumber = 1;

        // Active DoTs on current monster
        private List<Models.ActiveDot> _activeDots = new List<Models.ActiveDot>();
        // Active stun and debuffs on current monster
        private int _stunTurnsLeft = 0;
        private List<Models.ActiveDebuff> _activeDebuffs = new List<Models.ActiveDebuff>();

        // Left panel controls
        private ComboBox cmbMonsterType;
        private ComboBox cmbVariant;
        private PictureBox picMonster;
        private Label lblMonsterStats;
        private NumericUpDown nudHeroLevel;
        private FlowLayoutPanel pnlSkillIcons;

        // Right panel
        private Label lblMonsterName;
        private ProgressBar hpBar;
        private Label lblHpText;
        private RichTextBox rtbStatus;
        private RichTextBox txtLog;
        private Button btnReset;
        private Button btnNextTurn;

        // Colors
        private static readonly Color BgPanel = Color.FromArgb(37, 37, 38);
        private static readonly Color BgControl = Color.FromArgb(50, 50, 55);
        private static readonly Color Gold = Color.FromArgb(218, 185, 107);
        private static readonly Color TextLight = Color.FromArgb(220, 220, 220);
        private static readonly Color TextDim = Color.FromArgb(160, 160, 160);
        private static readonly Color HpGreen = Color.FromArgb(80, 180, 80);
        private static readonly Color HpRed = Color.FromArgb(200, 60, 60);

        public SimulatorPanel()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = BgPanel;
            this.ForeColor = TextLight;
            BuildUI();
        }

        public void SetMonsters(List<MonsterInfo> monsters)
        {
            _monsters = monsters;
            PopulateMonsterSelector();
        }

        public void SetHeroData(HeroInfo heroInfo, Dictionary<string, Image> skillIcons)
        {
            _heroInfo = heroInfo;
            _skillIcons = skillIcons;
            PopulateSkillIcons();
        }

        public void SetEffects(Dictionary<string, Models.EffectInfo> effects)
        {
            _effects = effects ?? new Dictionary<string, Models.EffectInfo>(StringComparer.OrdinalIgnoreCase);
        }

        private void BuildUI()
        {
            // ──── LEFT PANEL (fixed width) ────
            var leftPanel = new Panel
            {
                Dock = DockStyle.Left, Width = 280,
                AutoScroll = true, Padding = new Padding(12),
                BackColor = BgPanel
            };

            int y = 8;

            var lblMon = MakeLabel("Monster:", 8, y); y += 22;
            cmbMonsterType = new ComboBox
            {
                Location = new Point(8, y), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BgControl, ForeColor = TextLight,
                Font = new Font("Segoe UI", 9.5f)
            };
            cmbMonsterType.SelectedIndexChanged += CmbMonsterType_Changed;
            y += 30;

            var lblVar = MakeLabel("Variant:", 8, y); y += 22;
            cmbVariant = new ComboBox
            {
                Location = new Point(8, y), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = BgControl, ForeColor = TextLight,
                Font = new Font("Segoe UI", 9.5f)
            };
            cmbVariant.SelectedIndexChanged += CmbVariant_Changed;
            y += 34;

            picMonster = new PictureBox
            {
                Location = new Point(8, y), Size = new Size(250, 140),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(25, 25, 28),
                BorderStyle = BorderStyle.FixedSingle
            };
            y += 148;

            lblMonsterStats = new Label
            {
                Location = new Point(8, y), Size = new Size(250, 110),
                ForeColor = TextDim, Font = new Font("Consolas", 9f),
                Text = "No monster selected"
            };
            y += 118;

            var sep1 = new Label { Location = new Point(8, y), Size = new Size(250, 2), BackColor = Color.FromArgb(60, 60, 65) };
            y += 10;

            var lblHL = MakeLabel("Hero Level (0-4):", 8, y); y += 22;
            nudHeroLevel = MakeNud(8, y, 0, 4, 0);
            nudHeroLevel.ValueChanged += (s, e) => PopulateSkillIcons();
            y += 36;

            var sep2 = new Label { Location = new Point(8, y), Size = new Size(250, 2), BackColor = Color.FromArgb(60, 60, 65) };
            y += 10;

            var lblSkills = MakeLabel("Click skill to attack:", 8, y);
            lblSkills.ForeColor = Gold;
            y += 22;

            pnlSkillIcons = new FlowLayoutPanel
            {
                Location = new Point(4, y), Size = new Size(264, 130),
                BackColor = Color.Transparent, WrapContents = true
            };

            leftPanel.Controls.AddRange(new Control[]
            {
                lblMon, cmbMonsterType, lblVar, cmbVariant,
                picMonster, lblMonsterStats,
                sep1, lblHL, nudHeroLevel,
                sep2, lblSkills, pnlSkillIcons
            });

            // ──── Divider ────
            var divider = new Panel { Dock = DockStyle.Left, Width = 2, BackColor = Color.FromArgb(50, 50, 55) };

            // ──── RIGHT PANEL ────
            var rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(28, 28, 30), Padding = new Padding(20) };

            lblMonsterName = new Label
            {
                Dock = DockStyle.Top, Height = 36,
                Font = new Font("Segoe UI Semibold", 16f),
                ForeColor = Gold, TextAlign = ContentAlignment.MiddleLeft,
                Text = "   Select a monster"
            };

            hpBar = new ProgressBar
            {
                Dock = DockStyle.Top, Height = 28,
                Minimum = 0, Maximum = 100, Value = 100,
                Style = ProgressBarStyle.Continuous
            };

            lblHpText = new Label
            {
                Dock = DockStyle.Top, Height = 24,
                Font = new Font("Consolas", 11f, FontStyle.Bold),
                ForeColor = HpGreen, TextAlign = ContentAlignment.MiddleCenter,
                Text = ""
            };

            btnReset = new Button
            {
                Dock = DockStyle.Top, Height = 30,
                Text = "↻ Reset HP", FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(55, 55, 60), ForeColor = TextLight,
                Font = new Font("Segoe UI", 9.5f), Cursor = Cursors.Hand
            };
            btnReset.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 75);
            btnReset.Click += (s, e) => ResetMonsterHp();

            btnNextTurn = new Button
            {
                Dock = DockStyle.Top, Height = 30,
                Text = "⏩ Next Turn", FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 60, 80), ForeColor = Color.FromArgb(120, 200, 255),
                Font = new Font("Segoe UI", 9.5f), Cursor = Cursors.Hand
            };
            btnNextTurn.FlatAppearance.BorderColor = Color.FromArgb(60, 100, 140);
            btnNextTurn.Click += (s, e) => NextTurn();

            rtbStatus = new RichTextBox
            {
                Dock = DockStyle.Top, Height = 22,
                BackColor = Color.FromArgb(28, 28, 30),
                ForeColor = Color.FromArgb(100, 100, 110),
                Font = new Font("Consolas", 8.5f),
                ReadOnly = true, BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.None,
                WordWrap = false
            };

            var sepR = new Label { Dock = DockStyle.Top, Height = 6, BackColor = Color.Transparent };

            txtLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(28, 28, 30),
                ForeColor = TextDim,
                Font = new Font("Consolas", 10f),
                ReadOnly = true, BorderStyle = BorderStyle.None,
                Text = ""
            };

            rightPanel.Controls.Add(txtLog);
            rightPanel.Controls.Add(sepR);
            rightPanel.Controls.Add(btnNextTurn);
            rightPanel.Controls.Add(btnReset);
            rightPanel.Controls.Add(rtbStatus);
            rightPanel.Controls.Add(lblHpText);
            rightPanel.Controls.Add(hpBar);
            rightPanel.Controls.Add(lblMonsterName);

            this.Controls.Add(rightPanel);
            this.Controls.Add(divider);
            this.Controls.Add(leftPanel);
        }

        // ═══════════════════════════════════════
        //  Monster Selection
        // ═══════════════════════════════════════

        private void PopulateMonsterSelector()
        {
            cmbMonsterType.Items.Clear();
            if (_monsters == null || _monsters.Count == 0) return;

            var uniqueNames = _monsters.Select(m => m.Id).Distinct().OrderBy(n => n).ToList();
            foreach (var name in uniqueNames)
            {
                var displayName = _monsters.First(m => m.Id == name).DisplayName;
                cmbMonsterType.Items.Add(new ComboItem(name, displayName));
            }

            if (cmbMonsterType.Items.Count > 0)
                cmbMonsterType.SelectedIndex = 0;
        }

        private void CmbMonsterType_Changed(object sender, EventArgs e)
        {
            var sel = cmbMonsterType.SelectedItem as ComboItem;
            if (sel == null || _monsters == null) return;

            var variants = _monsters.Where(m => m.Id == sel.Value).ToList();
            cmbVariant.Items.Clear();
            foreach (var v in variants)
                cmbVariant.Items.Add(new ComboItem(v.VariantSuffix, v.VariantSuffix));

            if (cmbVariant.Items.Count > 0)
                cmbVariant.SelectedIndex = 0;
        }

        private void CmbVariant_Changed(object sender, EventArgs e)
        {
            var monSel = cmbMonsterType.SelectedItem as ComboItem;
            var varSel = cmbVariant.SelectedItem as ComboItem;
            if (monSel == null || varSel == null) return;

            _selectedMonster = _monsters.FirstOrDefault(m =>
                m.Id == monSel.Value && m.VariantSuffix == varSel.Value);

            UpdateMonsterDisplay();
            ResetMonsterHp();
        }

        private void UpdateMonsterDisplay()
        {
            if (_selectedMonster == null)
            {
                lblMonsterStats.Text = "No monster selected";
                picMonster.Image = null;
                return;
            }

            var m = _selectedMonster;
            lblMonsterStats.Text =
                $"HP: {m.Hp}   Dodge: {m.Dodge}%   PROT: {m.Prot * 100:F0}%\n" +
                $"SPD: {m.Spd}   Type: {m.EnemyType}\n" +
                $"Stun: {m.StunResist}%  Blight: {m.PoisonResist}%\n" +
                $"Bleed: {m.BleedResist}%  Debuff: {m.DebuffResist}%";

            if (!string.IsNullOrEmpty(m.ImagePath) && File.Exists(m.ImagePath))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(m.ImagePath);
                    var ms = new System.IO.MemoryStream(bytes);
                    picMonster.Image = Image.FromStream(ms);
                }
                catch { picMonster.Image = null; }
            }
            else
            {
                picMonster.Image = null;
            }
        }

        private void ResetMonsterHp()
        {
            if (_selectedMonster == null) return;
            _monsterCurrentHp = _selectedMonster.Hp;
            _activeDots.Clear();
            _activeDebuffs.Clear();
            _stunTurnsLeft = 0;
            _turnNumber = 1;
            UpdateHpDisplay();
            UpdateDoTStatus();
            txtLog.Clear();
            lblMonsterName.Text = $"   {_selectedMonster.DisplayName} ({_selectedMonster.VariantSuffix})";
            SetSkillButtonsEnabled(true);
        }

        private void UpdateHpDisplay()
        {
            if (_selectedMonster == null) return;
            int maxHp = _selectedMonster.Hp;
            int hp = Math.Max(0, _monsterCurrentHp);
            double pct = maxHp > 0 ? (double)hp / maxHp * 100 : 0;

            hpBar.Value = (int)Math.Max(0, Math.Min(100, pct));
            lblHpText.Text = $"HP: {hp} / {maxHp}";
            lblHpText.ForeColor = pct > 50 ? HpGreen : pct > 25 ? Gold : HpRed;
        }

        // ═══════════════════════════════════════
        //  Skill Icons
        // ═══════════════════════════════════════

        private void PopulateSkillIcons()
        {
            pnlSkillIcons.Controls.Clear();
            if (_heroInfo == null || _skillIcons == null) return;

            int heroLv = (int)nudHeroLevel.Value;

            var uniqueSkills = _heroInfo.CombatSkills
                .Where(s => s.SkillType == "combat_skill")
                .Select(s => s.Id)
                .Distinct()
                .ToList();

            foreach (var skillId in uniqueSkills)
            {
                var skill = _heroInfo.CombatSkills.FirstOrDefault(s =>
                    s.Id == skillId && s.Level == heroLv && s.SkillType == "combat_skill")
                    ?? _heroInfo.CombatSkills.FirstOrDefault(s =>
                        s.Id == skillId && s.SkillType == "combat_skill");
                if (skill == null) continue;

                Image icon = null;
                _skillIcons.TryGetValue(skillId, out icon);

                var btn = new Button
                {
                    Size = new Size(56, 56),
                    Margin = new Padding(4),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(45, 45, 50),
                    Cursor = Cursors.Hand,
                    Tag = skill,
                    Image = icon != null ? new Bitmap(icon, 44, 44) : null,
                    ImageAlign = ContentAlignment.MiddleCenter
                };
                btn.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 75);
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 80);
                btn.FlatAppearance.MouseDownBackColor = Gold;

                var tip = new ToolTip();
                tip.SetToolTip(btn, $"{skillId} (Lv.{skill.Level})");

                btn.Click += SkillBtn_Click;
                pnlSkillIcons.Controls.Add(btn);
            }
        }

        private void SkillBtn_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var skill = btn?.Tag as CombatSkill;
            if (skill == null || _selectedMonster == null || _heroInfo == null)
                return;

            SimulateCombat(skill);
        }

        // ═══════════════════════════════════════
        //  Combat Simulation — HP + DoT
        // ═══════════════════════════════════════

        private void SimulateCombat(CombatSkill skill)
        {
            if (_monsterCurrentHp <= 0) return;
            int heroLv = (int)nudHeroLevel.Value;
            var m = _selectedMonster;

            var weapon = _heroInfo.Weapons.Count > heroLv
                ? _heroInfo.Weapons[heroLv]
                : _heroInfo.Weapons.LastOrDefault();
            if (weapon == null) return;

            double skillAtk    = ParsePct(skill.Atk);
            double skillDmgMod = ParsePct(skill.Dmg);
            double skillCrit   = ParsePct(skill.Crit);

            // ── Hit check ── (ACC - Dodge + 5)%, clamped [5%, 100%]
            double rawHitChance = (weapon.Atk + skillAtk) - m.Dodge + 5;
            double hitChance = Math.Max(5, Math.Min(100, rawHitChance));
            var rng = new Random();
            bool isHit = rng.NextDouble() * 100 < hitChance;

            AppendLog($"─── Turn {_turnNumber}: {skill.Id} ───", Color.FromArgb(100, 100, 110));

            if (!isHit)
            {
                AppendLog($"  ✕ MISS  (Hit%: {hitChance:F0}%)", Color.FromArgb(150, 100, 100));
                SetSkillButtonsEnabled(false);  // Miss still consumes the turn
                return;
            }

            // ── Damage ──
            double dmgMod = 1.0 + (skillDmgMod / 100.0);
            int rawMin  = (int)Math.Floor(weapon.DmgMin * dmgMod);
            int rawMax  = (int)Math.Ceiling(weapon.DmgMax * dmgMod);
            int rawDmg  = rng.Next(Math.Max(0, rawMin), Math.Max(1, rawMax + 1));
            int finalDmg = (int)Math.Max(0, Math.Round(rawDmg * (1.0 - m.Prot)));

            double totalCrit = weapon.Crit + skillCrit;
            bool isCrit = rng.NextDouble() * 100 < totalCrit;
            if (isCrit) finalDmg = (int)Math.Round(finalDmg * 1.5);

            int oldHp = _monsterCurrentHp;
            _monsterCurrentHp = Math.Max(0, _monsterCurrentHp - finalDmg);
            UpdateHpDisplay();

            string critTag = isCrit ? " ★CRIT" : "";
            var dmgColor = isCrit ? Color.FromArgb(255, 200, 80) : Color.FromArgb(220, 100, 100);
            AppendLog($"  ⚔ -{finalDmg} DMG{critTag}   HP: {oldHp} → {_monsterCurrentHp}", dmgColor);

            // ── Effects → DoT ──
            // Effect names can contain spaces (e.g. "PD Single Blight 1"), so extract quoted strings
            var skillEffects = skill.Properties
                .Where(p => p.Key.Equals("effect", StringComparison.OrdinalIgnoreCase))
                .SelectMany(p => Regex.Matches(p.Value, @"""([^""]+)""")
                    .Cast<Match>().Select(m => m.Groups[1].Value))
                .ToList();

            foreach (var effName in skillEffects)
            {
                Models.EffectInfo eff;
                if (!_effects.TryGetValue(effName, out eff)) continue;

                // Only DoTs targeting the enemy
                bool isEnemyTarget = eff.Target == null ||
                    eff.Target.Equals("target", StringComparison.OrdinalIgnoreCase);
                if (!isEnemyTarget) continue;

                if (eff.DotPoison > 0 || eff.DotBleed > 0)
                {
                    string kind = eff.DotPoison > 0 ? "Blight" : "Bleed";
                    int dmgPT   = eff.DotPoison > 0 ? eff.DotPoison : eff.DotBleed;
                    double resist = kind == "Blight" ? m.PoisonResist : m.BleedResist;

                    // Application chance: base chance - monster resistance
                    double applyChance = eff.Chance - resist;
                    bool applied = rng.NextDouble() * 100 < applyChance;

                    if (applied)
                    {
                        // DoTs stack independently — each has its own timer (DD1 rules)
                        _activeDots.Add(new Models.ActiveDot
                        {
                            EffectName = effName,
                            Kind = kind,
                            DmgPerTurn = dmgPT,
                            TurnsLeft  = eff.Duration > 0 ? eff.Duration : 3
                        });
                        AppendLog($"  ☣ {kind} applied: {dmgPT}/turn × {eff.Duration}t  (chance {applyChance:F0}%)",
                            kind == "Blight" ? Color.FromArgb(140, 220, 100) : Color.FromArgb(220, 80, 80));
                    }
                    else
                    {
                        AppendLog($"  ○ {kind} resisted  ({eff.Chance:F0}% - {resist}% res = {applyChance:F0}%)",
                            Color.FromArgb(110, 110, 120));
                    }
                }

                if (eff.IsStun)
                {
                    double applyChance = eff.Chance - m.StunResist;
                    bool applied = rng.NextDouble() * 100 < applyChance;
                    if (applied)
                    {
                        int dur = eff.Duration > 0 ? eff.Duration : 1;
                        _stunTurnsLeft = Math.Max(_stunTurnsLeft, dur);
                        AppendLog($"  ★ STUN applied! ({dur}t)  (chance {applyChance:F0}%)",
                            Color.FromArgb(255, 220, 80));
                    }
                    else
                    {
                        AppendLog($"  ○ Stun resisted  ({eff.Chance:F0}% - {m.StunResist}% res = {applyChance:F0}%)",
                            Color.FromArgb(110, 110, 120));
                    }
                }

                if (eff.IsDebuff)
                {
                    double applyChance = eff.Chance - m.DebuffResist;
                    bool applied = rng.NextDouble() * 100 < applyChance;
                    if (applied)
                    {
                        int dur = eff.Duration > 0 ? eff.Duration : 3;
                        string desc = BuildDebuffDescription(eff);
                        _activeDebuffs.Add(new Models.ActiveDebuff
                        {
                            EffectName = effName,
                            Description = desc,
                            TurnsLeft = dur
                        });
                        AppendLog($"  ⬇ Debuff applied: {desc} ({dur}t)  (chance {applyChance:F0}%)",
                            Color.FromArgb(180, 130, 255));
                    }
                    else
                    {
                        AppendLog($"  ○ Debuff resisted  ({eff.Chance:F0}% - {m.DebuffResist}% res = {applyChance:F0}%)",
                            Color.FromArgb(110, 110, 120));
                    }
                }
            }

            UpdateDoTStatus();
            SetSkillButtonsEnabled(false);  // lock until Next Turn

            if (_monsterCurrentHp <= 0)
            {
                AppendLog($"  ☠ {m.DisplayName} DEFEATED!", Color.FromArgb(255, 80, 80));
                SetSkillButtonsEnabled(false);
            }
        }

        private void SetSkillButtonsEnabled(bool enabled)
        {
            pnlSkillIcons.Enabled = enabled;
            pnlSkillIcons.BackColor = enabled
                ? Color.Transparent
                : Color.FromArgb(20, 20, 20);
        }

        private void NextTurn()
        {
            if (_selectedMonster == null || _monsterCurrentHp <= 0) return;
            _turnNumber++;
            AppendLog($"─── Turn {_turnNumber}: Status phase ───", Color.FromArgb(100, 100, 110));

            bool anyStatus = _activeDots.Count > 0 || _stunTurnsLeft > 0 || _activeDebuffs.Count > 0;
            if (!anyStatus)
            {
                AppendLog("  (no active status effects)", Color.FromArgb(100, 100, 110));
                SetSkillButtonsEnabled(true);
                return;
            }

            // DoT ticks
            var dotsToRemove = new List<Models.ActiveDot>();
            foreach (var dot in _activeDots)
            {
                int oldHp = _monsterCurrentHp;
                _monsterCurrentHp = Math.Max(0, _monsterCurrentHp - dot.DmgPerTurn);
                dot.TurnsLeft--;

                var dotColor = dot.Kind == "Blight"
                    ? Color.FromArgb(140, 220, 100)
                    : Color.FromArgb(220, 80, 80);
                string tickTag = dot.TurnsLeft <= 0 ? " [last tick]" : $" [{dot.TurnsLeft}t left]";
                AppendLog($"  ☣ {dot.Kind} -{dot.DmgPerTurn}   HP: {oldHp} → {_monsterCurrentHp}{tickTag}", dotColor);

                if (dot.TurnsLeft <= 0) dotsToRemove.Add(dot);
            }
            foreach (var d in dotsToRemove) _activeDots.Remove(d);

            // Stun countdown
            if (_stunTurnsLeft > 0)
            {
                _stunTurnsLeft--;
                if (_stunTurnsLeft <= 0)
                    AppendLog("  ★ Stun expired.", Color.FromArgb(160, 140, 80));
                else
                    AppendLog($"  ★ Stunned ({_stunTurnsLeft}t remaining)", Color.FromArgb(255, 220, 80));
            }

            // Debuff countdown
            var debuffsToRemove = new List<Models.ActiveDebuff>();
            foreach (var debuff in _activeDebuffs)
            {
                debuff.TurnsLeft--;
                if (debuff.TurnsLeft <= 0)
                {
                    debuffsToRemove.Add(debuff);
                    AppendLog($"  ⬇ Debuff expired: {debuff.Description}", Color.FromArgb(120, 100, 160));
                }
            }
            foreach (var d in debuffsToRemove) _activeDebuffs.Remove(d);

            UpdateHpDisplay();
            UpdateDoTStatus();
            SetSkillButtonsEnabled(true);

            if (_monsterCurrentHp <= 0)
            {
                AppendLog($"  ☠ {_selectedMonster.DisplayName} DEFEATED!", Color.FromArgb(255, 80, 80));
                SetSkillButtonsEnabled(false);
            }
        }

        private void UpdateDoTStatus()
        {
            if (rtbStatus == null) return;
            rtbStatus.Clear();

            bool anyStatus = _activeDots.Count > 0 || _stunTurnsLeft > 0 || _activeDebuffs.Count > 0;
            if (!anyStatus)
            {
                AppendStatus("  Status: none", Color.FromArgb(80, 80, 90));
                return;
            }

            bool first = true;
            void Sep() { if (!first) AppendStatus("  |  ", Color.FromArgb(60, 60, 70)); first = false; }

            foreach (var d in _activeDots)
            {
                Sep();
                var c = d.Kind == "Blight"
                    ? Color.FromArgb(140, 220, 100)
                    : Color.FromArgb(220, 80, 80);
                AppendStatus($"☣{d.Kind} {d.DmgPerTurn}/t({d.TurnsLeft}t)", c);
            }

            if (_stunTurnsLeft > 0)
            {
                Sep();
                AppendStatus($"★STUN({_stunTurnsLeft}t)", Color.FromArgb(255, 220, 80));
            }

            foreach (var d in _activeDebuffs)
            {
                Sep();
                AppendStatus($"⬇{d.Description}({d.TurnsLeft}t)", Color.FromArgb(180, 130, 255));
            }
        }

        private void AppendStatus(string text, Color color)
        {
            rtbStatus.SelectionStart = rtbStatus.TextLength;
            rtbStatus.SelectionColor = color;
            rtbStatus.AppendText(text);
        }

        private string BuildDebuffDescription(Models.EffectInfo eff)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(eff.DebuffIds))
                parts.Add($"[{eff.DebuffIds}]");
            if (eff.SpeedRatingAdd != 0)
                parts.Add($"SPD{eff.SpeedRatingAdd:+0;-0}");
            if (eff.AttackRatingAdd != 0)
                parts.Add($"ACC{eff.AttackRatingAdd:+0;-0}%");
            if (eff.DefenseRatingAdd != 0)
                parts.Add($"DOD{eff.DefenseRatingAdd:+0;-0}%");
            if (eff.DmgLowMultiply != 0 || eff.DmgHighMultiply != 0)
                parts.Add($"DMG{eff.DmgLowMultiply:+0;-0}%/{eff.DmgHighMultiply:+0;-0}%");
            return parts.Count > 0 ? string.Join(" ", parts) : "Debuff";
        }

        // ═══════════════════════════════════════
        //  Helpers
        // ═══════════════════════════════════════

        private double ParsePct(string val)
        {
            if (string.IsNullOrEmpty(val)) return 0;
            val = val.Replace("%", "").Trim();
            double.TryParse(val, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double r);
            return r;
        }

        private void AppendLog(string text, Color color)
        {
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionColor = color;
            txtLog.SelectionFont = new Font("Consolas", 10.5f);
            txtLog.AppendText(text + "\n");
            txtLog.ScrollToCaret();
        }

        private Label MakeLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text, Location = new Point(x, y), AutoSize = true,
                ForeColor = Gold, Font = new Font("Segoe UI Semibold", 9.5f)
            };
        }

        private NumericUpDown MakeNud(int x, int y, int min, int max, int val)
        {
            return new NumericUpDown
            {
                Location = new Point(x, y), Width = 80, Minimum = min, Maximum = max, Value = val,
                BackColor = BgControl, ForeColor = Color.White,
                Font = new Font("Consolas", 12f, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center
            };
        }

        private class ComboItem
        {
            public string Value { get; }
            public string Display { get; }
            public ComboItem(string value, string display) { Value = value; Display = display; }
            public override string ToString() => Display;
        }
    }
}
