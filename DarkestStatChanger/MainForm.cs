using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DarkestStatChanger.Models;
using DarkestStatChanger.Services;

namespace DarkestStatChanger
{
    public class MainForm : Form
    {
        private HeroInfo _heroInfo;
        private string _currentFilePath;
        private Dictionary<string, Image> _skillIcons = new Dictionary<string, Image>();
        private CampingSkillData _campingData;
        private Dictionary<string, Image> _campIcons = new Dictionary<string, Image>();
        private List<MonsterInfo> _monsters;
        private Dictionary<string, EffectInfo> _effects = new Dictionary<string, EffectInfo>(System.StringComparer.OrdinalIgnoreCase);
        private SimulatorPanel _simulatorPanel;

        // Icon number words for file matching
        private static readonly string[] IconWords = { "one", "two", "three", "four", "five", "six", "seven" };

        // Top bar
        private Button btnOpen;
        private Button btnSave;
        private Label lblFilePath;

        // Tabs
        private TabControl tabControl;

        // Resistances
        private NumericUpDown nudStun, nudPoison, nudBleed, nudDisease;
        private NumericUpDown nudMove, nudDebuff, nudDeathBlow, nudTrap;

        // Weapon / Armour / Skills
        private DataGridView dgvWeapon;
        private DataGridView dgvArmour;
        private DataGridView dgvSkills;
        private DataGridView dgvCamping;

        // Effects panel (Skills tab bottom)
        private DataGridView dgvEffects;
        private Label lblEffectsHeader;

        // Status
        private ToolStripStatusLabel statusLabel;

        public MainForm()
        {
            InitializeUI();
            LoadMonsters();
            LoadEffects();
        }

        private void InitializeUI()
        {
            // Form settings
            this.Text = "Darkest Dungeon Stat Changer";
            this.Size = new Size(1000, 720);
            this.MinimumSize = new Size(850, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Window icon
            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(icoPath))
                this.Icon = new Icon(icoPath);
            this.AllowDrop = true;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.FromArgb(220, 220, 220);
            this.Font = new Font("Segoe UI", 9.5f);

            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;

            // ─── Top Panel ───
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(8)
            };

            btnOpen = CreateButton("Open File", 10, 11, 100);
            btnOpen.Click += BtnOpen_Click;

            btnSave = CreateButton("Save", 118, 11, 80);
            btnSave.Enabled = false;
            btnSave.Click += BtnSave_Click;

            lblFilePath = new Label
            {
                Text = "  Drag && drop a .darkest file here, or click [Open File]",
                Location = new Point(210, 16),
                AutoSize = true,
                ForeColor = Color.FromArgb(160, 160, 160),
                Font = new Font("Segoe UI", 9f)
            };

            topPanel.Controls.AddRange(new Control[] { btnOpen, btnSave, lblFilePath });

            // ─── Tab Control ───
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(14, 6),
                Font = new Font("Segoe UI Semibold", 10f)
            };

            var tabRes = new TabPage("  Resistances  ");
            var tabWep = new TabPage("  Weapon  ");
            var tabArm = new TabPage("  Armour  ");
            var tabSkl = new TabPage("  Skills  ");
            var tabCamp = new TabPage("  Camping  ");
            var tabSim = new TabPage("  Simulator  ");

            foreach (var tab in new[] { tabRes, tabWep, tabArm, tabSkl, tabCamp, tabSim })
            {
                tab.BackColor = Color.FromArgb(37, 37, 38);
                tab.ForeColor = Color.FromArgb(220, 220, 220);
            }

            SetupResistancesTab(tabRes);
            SetupWeaponTab(tabWep);
            SetupArmourTab(tabArm);
            SetupSkillsTab(tabSkl);
            SetupCampingTab(tabCamp);
            SetupSimulatorTab(tabSim);

            tabControl.TabPages.AddRange(new[] { tabRes, tabWep, tabArm, tabSkl, tabCamp, tabSim });

            // Sync UI → model when switching to Simulator tab
            tabControl.SelectedIndexChanged += (s, e) =>
            {
                if (tabControl.SelectedIndex == 5 && _heroInfo != null) // Simulator tab
                {
                    UpdateModelFromUI();
                    _simulatorPanel.SetHeroData(_heroInfo, _skillIcons);
                }
            };

            // ─── Status Strip ───
            var statusStrip = new StatusStrip { BackColor = Color.FromArgb(45, 45, 48) };
            statusLabel = new ToolStripStatusLabel("Ready")
            {
                ForeColor = Color.FromArgb(160, 160, 160)
            };
            statusStrip.Items.Add(statusLabel);

            // Add controls (order matters for Dock)
            this.Controls.Add(tabControl);
            this.Controls.Add(topPanel);
            this.Controls.Add(statusStrip);
        }

        // ═══════════════════════════════════════════════════════
        //  Resistances Tab
        // ═══════════════════════════════════════════════════════

        private void SetupResistancesTab(TabPage tab)
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 2,
                Padding = new Padding(15)
            };

            for (int c = 0; c < 4; c++)
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            for (int r = 0; r < 2; r++)
                table.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            var names = new[] { "Stun", "Poison", "Bleed", "Disease", "Move", "Debuff", "Death Blow", "Trap" };
            var nuds = new NumericUpDown[8];

            for (int i = 0; i < 8; i++)
            {
                var grp = new GroupBox
                {
                    Text = names[i] + " (%)",
                    ForeColor = Color.FromArgb(218, 185, 107),
                    Dock = DockStyle.Fill,
                    Margin = new Padding(8),
                    Padding = new Padding(10, 18, 10, 10),
                    Font = new Font("Segoe UI Semibold", 10f)
                };

                nuds[i] = new NumericUpDown
                {
                    Minimum = -200,
                    Maximum = 300,
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 18f, FontStyle.Bold),
                    BackColor = Color.FromArgb(50, 50, 55),
                    ForeColor = Color.White,
                    TextAlign = HorizontalAlignment.Center
                };

                grp.Controls.Add(nuds[i]);
                table.Controls.Add(grp, i % 4, i / 4);
            }

            nudStun = nuds[0]; nudPoison = nuds[1]; nudBleed = nuds[2]; nudDisease = nuds[3];
            nudMove = nuds[4]; nudDebuff = nuds[5]; nudDeathBlow = nuds[6]; nudTrap = nuds[7];

            tab.Controls.Add(table);
        }

        // ═══════════════════════════════════════════════════════
        //  Weapon Tab
        // ═══════════════════════════════════════════════════════

        private void SetupWeaponTab(TabPage tab)
        {
            dgvWeapon = CreateStyledGrid();
            dgvWeapon.Columns.AddRange(new DataGridViewColumn[]
            {
                Col("Level", 60, true),
                Col("Name", 160),
                Col("ATK %", 70),
                Col("DMG Min", 75),
                Col("DMG Max", 75),
                Col("Crit %", 70),
                Col("SPD", 60),
            });
            tab.Controls.Add(dgvWeapon);
        }

        // ═══════════════════════════════════════════════════════
        //  Armour Tab
        // ═══════════════════════════════════════════════════════

        private void SetupArmourTab(TabPage tab)
        {
            dgvArmour = CreateStyledGrid();
            dgvArmour.Columns.AddRange(new DataGridViewColumn[]
            {
                Col("Level", 60, true),
                Col("Name", 170),
                Col("DEF %", 70),
                Col("PROT", 70),
                Col("HP", 70),
                Col("SPD", 60),
            });
            tab.Controls.Add(dgvArmour);
        }

        // ═══════════════════════════════════════════════════════
        //  Skills Tab
        // ═══════════════════════════════════════════════════════

        private void SetupSkillsTab(TabPage tab)
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300,
                Panel1MinSize = 120,
                Panel2MinSize = 100,
                BackColor = Color.FromArgb(37, 37, 38),
                SplitterWidth = 5
            };

            // ── Top: Skills grid ──
            dgvSkills = CreateStyledGrid();
            dgvSkills.RowTemplate.Height = 40;

            var iconCol = new DataGridViewImageColumn
            {
                HeaderText = "Icon",
                Width = 48, MinimumWidth = 48,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ImageLayout = DataGridViewImageCellLayout.Zoom,
                DefaultCellStyle = { NullValue = null, Padding = new Padding(4) }
            };
            dgvSkills.Columns.Add(iconCol);
            dgvSkills.Columns.AddRange(new DataGridViewColumn[]
            {
                Col("Type", 110, true),
                Col("ID", 130, true),
                Col("Lv", 35, true),
                Col("ACC", 75),
                Col("DMG", 75),
                Col("Crit", 75),
                Col("Launch", 70),
                Col("Target", 70),
                Col("Effects", 200, true),
            });
            dgvSkills.SelectionChanged += DgvSkills_SelectionChanged;
            split.Panel1.Controls.Add(dgvSkills);

            // ── Bottom: Effects panel ──
            var headerBar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(4, 3, 4, 3)
            };
            headerBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            headerBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            headerBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            headerBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            lblEffectsHeader = new Label
            {
                Text = "Effects  ←  select a skill row",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(218, 185, 107),
                Font = new Font("Segoe UI Semibold", 9.5f),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var btnLoadEff = MakeHeaderButton("Load Effects File");
            btnLoadEff.Click += BtnLoadEffectsFile_Click;

            var btnSaveEff = MakeHeaderButton("Save Effects");
            btnSaveEff.Click += BtnSaveEffects_Click;

            headerBar.Controls.Add(lblEffectsHeader, 0, 0);
            headerBar.Controls.Add(btnSaveEff, 1, 0);
            headerBar.Controls.Add(btnLoadEff, 2, 0);

            // Effects DataGridView — 3 generic columns, fully dynamic
            dgvEffects = CreateStyledGrid();
            dgvEffects.Dock = DockStyle.Fill;
            dgvEffects.Columns.AddRange(new DataGridViewColumn[]
            {
                Col("Effect",    200, true),
                Col("Parameter", 180, true),
                Col("Value",     160),
            });

            split.Panel2.Controls.Add(dgvEffects);
            split.Panel2.Controls.Add(headerBar);

            tab.Controls.Add(split);
        }

        private Button MakeHeaderButton(string text)
        {
            return new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 3, 3, 3),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(62, 62, 66),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.5f),
                FlatAppearance =
                {
                    BorderColor = Color.FromArgb(80, 80, 85),
                    MouseOverBackColor = Color.FromArgb(80, 80, 90)
                }
            };
        }

        // ═══════════════════════════════════════════════════════
        //  Camping Tab
        // ═══════════════════════════════════════════════════════

        private void SetupCampingTab(TabPage tab)
        {
            dgvCamping = CreateStyledGrid();
            dgvCamping.RowTemplate.Height = 40;

            var iconCol = new DataGridViewImageColumn
            {
                HeaderText = "Icon",
                Width = 48,
                MinimumWidth = 48,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ImageLayout = DataGridViewImageCellLayout.Zoom,
                DefaultCellStyle = { NullValue = null, Padding = new Padding(4) }
            };

            dgvCamping.Columns.Add(iconCol);
            dgvCamping.Columns.AddRange(new DataGridViewColumn[]
            {
                Col("ID", 150, true),
                Col("Cost", 60),
                Col("Use Limit", 80),
                Col("Effects", 400, true),
            });
            tab.Controls.Add(dgvCamping);
        }

        private void LoadCampIcons()
        {
            foreach (var img in _campIcons.Values) img.Dispose();
            _campIcons.Clear();

            if (_campingData == null) return;

            foreach (var kvp in _campingData.IconPaths)
            {
                try
                {
                    using (var stream = new FileStream(kvp.Value, FileMode.Open, FileAccess.Read))
                    {
                        _campIcons[kvp.Key] = Image.FromStream(stream);
                    }
                }
                catch { /* skip */ }
            }
        }

        // ═══════════════════════════════════════════════════════
        //  Simulator Tab
        // ═══════════════════════════════════════════════════════

        private void SetupSimulatorTab(TabPage tab)
        {
            _simulatorPanel = new SimulatorPanel();
            tab.Controls.Add(_simulatorPanel);
        }

        private void LoadMonsters()
        {
            _monsters = new List<MonsterInfo>();

            // Look for 'monsters' folder next to the exe
            var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string[] searchDirs = new[]
            {
                Path.Combine(exeDir, "monsters"),
                Path.Combine(exeDir, "..", "..", "..", "monsters"),     // bin/Release/net472 → project root
                Path.Combine(exeDir, "..", "..", "..", "..", "monsters"), // project root → workspace root
            };

            foreach (var dir in searchDirs)
            {
                var full = Path.GetFullPath(dir);
                if (Directory.Exists(full))
                {
                    _monsters = MonsterParser.LoadAll(full);
                    _simulatorPanel?.SetMonsters(_monsters);
                    statusLabel.Text = $"Ready  |  Monsters loaded: {_monsters.Count}";
                    return;
                }
            }
        }

        private void LoadEffects()
        {
            var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var effectsDir = Path.Combine(exeDir, "effects");

            if (Directory.Exists(effectsDir))
            {
                _effects = EffectsParser.LoadAll(effectsDir);
                _simulatorPanel?.SetEffects(_effects);
                statusLabel.Text += $"  |  Effects: {_effects.Count}";
            }
        }

        // ═══════════════════════════════════════════════════════

        private void LoadFile(string path)
        {
            try
            {
                _heroInfo = DarkestFileParser.Parse(path);
                _currentFilePath = path;
                _campingData = CampingSkillParser.Parse(path);
                LoadSkillIcons(path);
                PopulateUI();
                btnSave.Enabled = true;
                lblFilePath.Text = "  " + path;
                statusLabel.Text = $"Loaded: {Path.GetFileName(path)}  |  " +
                                   $"Weapons: {_heroInfo.Weapons.Count}  Armours: {_heroInfo.Armours.Count}  " +
                                   $"Skills: {_heroInfo.CombatSkills.Count}  " +
                                   $"Monsters: {_monsters.Count}";

                // Feed hero data to simulator
                _simulatorPanel.SetHeroData(_heroInfo, _skillIcons);
            }
            catch (Exception ex)
            {
                var code = (ex is DscException dsc) ? dsc.Code : "E900";
                MessageBox.Show($"[{code}] Failed to parse file:\n\n{ex.Message}", $"Error ({code})",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSkillIcons(string darkestFilePath)
        {
            // Dispose previous icons
            foreach (var img in _skillIcons.Values) img.Dispose();
            _skillIcons.Clear();

            // Search for ability PNGs in the same directory as the .darkest file
            var dir = Path.GetDirectoryName(darkestFilePath);
            if (dir == null) return;

            var abilityFiles = Directory.GetFiles(dir, "*.ability.*.png")
                .OrderBy(f => f)
                .ToArray();

            // Get unique skill IDs in order of first appearance (combat_skill only)
            var uniqueSkillIds = _heroInfo.CombatSkills
                .Where(s => s.SkillType == "combat_skill")
                .Select(s => s.Id)
                .Distinct()
                .ToList();

            // Map: icon files named ability.one/ability.two or ability.1/ability.2
            foreach (var iconFile in abilityFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(iconFile).ToLower();
                var parts = fileName.Split('.');
                var lastPart = parts.Length >= 3 ? parts[parts.Length - 1] : null;
                if (lastPart == null) continue;

                int wordIndex = -1;
                // Try word format: one, two, three, ...
                int wi = Array.IndexOf(IconWords, lastPart);
                if (wi >= 0) wordIndex = wi;
                // Try numeric format: 1, 2, 3, ... (convert to 0-based index)
                else if (int.TryParse(lastPart, out int n) && n >= 1)
                    wordIndex = n - 1;

                if (wordIndex >= 0 && wordIndex < uniqueSkillIds.Count)
                {
                    string skillId = uniqueSkillIds[wordIndex];
                    if (!_skillIcons.ContainsKey(skillId))
                    {
                        try
                        {
                            using (var stream = new FileStream(iconFile, FileMode.Open, FileAccess.Read))
                                _skillIcons[skillId] = Image.FromStream(stream);
                        }
                        catch { /* skip unloadable icons */ }
                    }
                }
            }
        }

        private void PopulateUI()
        {
            // Resistances
            if (_heroInfo.Resistances != null)
            {
                nudStun.Value = Clamp(_heroInfo.Resistances.Stun);
                nudPoison.Value = Clamp(_heroInfo.Resistances.Poison);
                nudBleed.Value = Clamp(_heroInfo.Resistances.Bleed);
                nudDisease.Value = Clamp(_heroInfo.Resistances.Disease);
                nudMove.Value = Clamp(_heroInfo.Resistances.Move);
                nudDebuff.Value = Clamp(_heroInfo.Resistances.Debuff);
                nudDeathBlow.Value = Clamp(_heroInfo.Resistances.DeathBlow);
                nudTrap.Value = Clamp(_heroInfo.Resistances.Trap);
            }

            // Weapons
            dgvWeapon.Rows.Clear();
            for (int i = 0; i < _heroInfo.Weapons.Count; i++)
            {
                var w = _heroInfo.Weapons[i];
                dgvWeapon.Rows.Add(i, w.Name, w.Atk, w.DmgMin, w.DmgMax, w.Crit, w.Spd);
            }

            // Armours
            dgvArmour.Rows.Clear();
            for (int i = 0; i < _heroInfo.Armours.Count; i++)
            {
                var a = _heroInfo.Armours[i];
                dgvArmour.Rows.Add(i, a.Name, a.Def, a.Prot, a.Hp, a.Spd);
            }

            // Skills
            dgvSkills.Rows.Clear();
            foreach (var s in _heroInfo.CombatSkills)
            {
                Image icon = null;
                _skillIcons.TryGetValue(s.Id, out icon);

                // Build effects summary from quoted effect names
                var effectNames = s.Properties
                    .Where(p => p.Key.Equals("effect", StringComparison.OrdinalIgnoreCase))
                    .SelectMany(p => System.Text.RegularExpressions.Regex
                        .Matches(p.Value, @"""([^""]+)""")
                        .Cast<System.Text.RegularExpressions.Match>()
                        .Select(m => m.Groups[1].Value))
                    .ToList();
                var effectsSummary = string.Join(", ", effectNames);

                int rowIdx = dgvSkills.Rows.Add(icon, s.SkillType, s.Id, s.Level, s.Atk, s.Dmg, s.Crit, s.Launch, s.Target, effectsSummary);
                dgvSkills.Rows[rowIdx].Height = 40;
            }

            // Camping Skills
            dgvCamping.Rows.Clear();
            LoadCampIcons();
            if (_campingData != null)
            {
                foreach (var cs in _campingData.Skills)
                {
                    Image icon = null;
                    _campIcons.TryGetValue(cs.Id, out icon);

                    // Build effects summary
                    var effectsSummary = string.Join(", ",
                        cs.Effects.Select(e =>
                        {
                            var label = !string.IsNullOrEmpty(e.SubType) ? e.SubType : e.Type;
                            return $"{label}: {e.Amount}";
                        }));

                    int ri = dgvCamping.Rows.Add(icon, cs.Id, cs.Cost, cs.UseLimit, effectsSummary);
                    dgvCamping.Rows[ri].Height = 40;
                }
            }
        }

        private void UpdateModelFromUI()
        {
            // Resistances
            if (_heroInfo.Resistances != null)
            {
                _heroInfo.Resistances.Stun = (int)nudStun.Value;
                _heroInfo.Resistances.Poison = (int)nudPoison.Value;
                _heroInfo.Resistances.Bleed = (int)nudBleed.Value;
                _heroInfo.Resistances.Disease = (int)nudDisease.Value;
                _heroInfo.Resistances.Move = (int)nudMove.Value;
                _heroInfo.Resistances.Debuff = (int)nudDebuff.Value;
                _heroInfo.Resistances.DeathBlow = (int)nudDeathBlow.Value;
                _heroInfo.Resistances.Trap = (int)nudTrap.Value;
            }

            // Weapons
            for (int i = 0; i < dgvWeapon.Rows.Count && i < _heroInfo.Weapons.Count; i++)
            {
                var row = dgvWeapon.Rows[i];
                var w = _heroInfo.Weapons[i];
                w.Name = row.Cells[1].Value?.ToString() ?? w.Name;
                w.Atk = ToInt(row.Cells[2].Value, w.Atk);
                w.DmgMin = ToInt(row.Cells[3].Value, w.DmgMin);
                w.DmgMax = ToInt(row.Cells[4].Value, w.DmgMax);
                w.Crit = ToInt(row.Cells[5].Value, w.Crit);
                w.Spd = ToInt(row.Cells[6].Value, w.Spd);
            }

            // Armours
            for (int i = 0; i < dgvArmour.Rows.Count && i < _heroInfo.Armours.Count; i++)
            {
                var row = dgvArmour.Rows[i];
                var a = _heroInfo.Armours[i];
                a.Name = row.Cells[1].Value?.ToString() ?? a.Name;
                a.Def = ToInt(row.Cells[2].Value, a.Def);
                a.Prot = ToInt(row.Cells[3].Value, a.Prot);
                a.Hp = ToInt(row.Cells[4].Value, a.Hp);
                a.Spd = ToInt(row.Cells[5].Value, a.Spd);
            }

            // Skills (cell indices shifted +1 due to icon column)
            for (int i = 0; i < dgvSkills.Rows.Count && i < _heroInfo.CombatSkills.Count; i++)
            {
                var row = dgvSkills.Rows[i];
                var s = _heroInfo.CombatSkills[i];
                s.Atk = row.Cells[4].Value?.ToString() ?? s.Atk;
                s.Dmg = row.Cells[5].Value?.ToString() ?? s.Dmg;
                s.Crit = row.Cells[6].Value?.ToString() ?? s.Crit;
                s.Launch = row.Cells[7].Value?.ToString() ?? s.Launch;
                s.Target = row.Cells[8].Value?.ToString() ?? s.Target;
            }

            // Camping Skills
            if (_campingData != null)
            {
                for (int i = 0; i < dgvCamping.Rows.Count && i < _campingData.Skills.Count; i++)
                {
                    var row = dgvCamping.Rows[i];
                    var cs = _campingData.Skills[i];
                    cs.Cost = ToInt(row.Cells[2].Value, cs.Cost);
                    cs.UseLimit = ToInt(row.Cells[3].Value, cs.UseLimit);
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        //  Event Handlers
        // ═══════════════════════════════════════════════════════

        private void DgvSkills_SelectionChanged(object sender, EventArgs e)
        {
            if (_heroInfo == null || dgvSkills.CurrentRow == null) return;
            int idx = dgvSkills.CurrentRow.Index;
            if (idx >= 0 && idx < _heroInfo.CombatSkills.Count)
                PopulateEffectsPanel(_heroInfo.CombatSkills[idx]);
        }

        private static readonly Color ColParamBalance  = Color.FromArgb(220, 220, 220);
        private static readonly Color ColParamFlag     = Color.FromArgb(110, 110, 120);
        private static readonly Color ColParamMissing  = Color.FromArgb(180, 100, 100);

        // Parameters considered balance-relevant: shown bright
        private static readonly System.Collections.Generic.HashSet<string> BalanceKeys =
            new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "chance","dotPoison","dotBleed","duration",
                "damage_low_multiply","damage_high_multiply",
                "damage_low_add","damage_high_add",
                "crit_chance_add","attack_rating_add","defense_rating_add",
                "speed_rating_add","protection_rating_add",
                "heal","healstress","dotHpHeal","health_damage",
                "health_damage_blocks","stun","pull","push",
                "buff_amount",
            };

        private void PopulateEffectsPanel(CombatSkill skill)
        {
            dgvEffects.Rows.Clear();

            var effectNames = skill.Properties
                .Where(p => p.Key.Equals("effect", StringComparison.OrdinalIgnoreCase))
                .SelectMany(p => System.Text.RegularExpressions.Regex
                    .Matches(p.Value, @"""([^""]+)""")
                    .Cast<System.Text.RegularExpressions.Match>()
                    .Select(m => m.Groups[1].Value))
                .ToList();

            if (effectNames.Count == 0)
            {
                lblEffectsHeader.Text = $"Effects: {skill.Id}  Lv.{skill.Level}  — (no effects)";
                return;
            }

            lblEffectsHeader.Text = $"Effects: {skill.Id}  Lv.{skill.Level}";

            bool anyMissing = false;
            foreach (var effName in effectNames)
            {
                EffectInfo eff;
                if (!_effects.TryGetValue(effName, out eff))
                {
                    anyMissing = true;
                    int ri = dgvEffects.Rows.Add(effName, "⚠ not loaded", "");
                    dgvEffects.Rows[ri].DefaultCellStyle.ForeColor = ColParamMissing;
                    continue;
                }

                // One row per parameter (skip 'name' — it's already in column 0)
                bool firstRow = true;
                foreach (var param in eff.RawParams)
                {
                    if (param.Key.Equals("name", StringComparison.OrdinalIgnoreCase)) continue;

                    string displayName = firstRow ? effName : "";
                    firstRow = false;

                    int ri = dgvEffects.Rows.Add(displayName, param.Key, param.Value);

                    bool isBalance = BalanceKeys.Contains(param.Key);
                    dgvEffects.Rows[ri].Cells[0].Style.ForeColor = Color.FromArgb(218, 185, 107);
                    dgvEffects.Rows[ri].Cells[1].Style.ForeColor = isBalance
                        ? ColParamBalance : ColParamFlag;
                    dgvEffects.Rows[ri].Cells[2].Style.ForeColor = isBalance
                        ? ColParamBalance : ColParamFlag;
                }
            }

            if (anyMissing)
                lblEffectsHeader.Text += "  ⚠ Load the mod's effects file";
        }

        private void BtnLoadEffectsFile_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Load mod effects file";
                ofd.Filter = "Effects Files (*.effects.darkest)|*.effects.darkest|All Darkest Files (*.darkest)|*.darkest";
                if (ofd.ShowDialog() != DialogResult.OK) return;

                EffectsParser.LoadFile(ofd.FileName, _effects);
                _simulatorPanel?.SetEffects(_effects);

                int total = _effects.Count;
                statusLabel.Text = $"Effects loaded: {total}  |  {Path.GetFileName(ofd.FileName)}";

                // Refresh effects panel if a skill is selected
                if (_heroInfo != null && dgvSkills.CurrentRow != null)
                {
                    int idx = dgvSkills.CurrentRow.Index;
                    if (idx >= 0 && idx < _heroInfo.CombatSkills.Count)
                        PopulateEffectsPanel(_heroInfo.CombatSkills[idx]);
                }
            }
        }

        private void BtnSaveEffects_Click(object sender, EventArgs e)
        {
            // Walk the grid: track current effect by last non-empty Effect column value
            string currentEffName = null;
            EffectInfo currentEff = null;

            foreach (DataGridViewRow row in dgvEffects.Rows)
            {
                string effCol   = row.Cells[0].Value?.ToString();
                string paramKey = row.Cells[1].Value?.ToString();
                string paramVal = row.Cells[2].Value?.ToString() ?? "";

                // Effect column is only filled on the first row of each effect group
                if (!string.IsNullOrEmpty(effCol))
                {
                    currentEffName = effCol;
                    _effects.TryGetValue(currentEffName, out currentEff);
                }

                if (currentEff == null || string.IsNullOrEmpty(paramKey)) continue;
                if (paramKey == "⚠ not loaded") continue;

                var rawParam = currentEff.RawParams
                    .FirstOrDefault(p => p.Key.Equals(paramKey, StringComparison.OrdinalIgnoreCase));
                if (rawParam != null)
                    rawParam.Value = paramVal;
            }

            var toSave = _effects.Values.Where(ef => !string.IsNullOrEmpty(ef.SourceFile));
            int saved;
            try
            {
                saved = EffectsFileWriter.Save(toSave);
            }
            catch (Exception ex)
            {
                var code = (ex is DscException dsc) ? dsc.Code : "E900";
                MessageBox.Show($"[{code}] Failed to save effects:\n\n{ex.Message}", $"Error ({code})",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (saved == 0)
            {
                MessageBox.Show(
                    "No mod effects file loaded.\nLoad a *.effects.darkest file first.",
                    "Nothing to save", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                statusLabel.Text = $"Effects saved ({saved} file(s), .bak backup created)";
                MessageBox.Show($"Saved {saved} effects file(s).\nBackup created as .bak",
                    "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnOpen_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Open .darkest file";
                ofd.Filter = "Darkest Files (*.darkest)|*.darkest|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                    LoadFile(ofd.FileName);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_heroInfo == null || string.IsNullOrEmpty(_currentFilePath)) return;

            try
            {
                UpdateModelFromUI();
                DarkestFileWriter.Save(_heroInfo, _currentFilePath);
                if (_campingData != null && !string.IsNullOrEmpty(_campingData.JsonFilePath))
                    CampingSkillParser.Save(_campingData);
                statusLabel.Text = $"Saved: {Path.GetFileName(_currentFilePath)}  (backup: .bak)";
                MessageBox.Show("File saved successfully!\nBackup created as .bak",
                    "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                var code = (ex is DscException dsc) ? dsc.Code : "E900";
                MessageBox.Show($"[{code}] Failed to save:\n\n{ex.Message}", $"Error ({code})",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                var file = files[0];
                if (file.EndsWith(".darkest", StringComparison.OrdinalIgnoreCase))
                    LoadFile(file);
                else
                    MessageBox.Show("Please drop a .darkest file.", "Invalid File",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ═══════════════════════════════════════════════════════
        //  Helpers
        // ═══════════════════════════════════════════════════════

        private Button CreateButton(string text, int x, int y, int width)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(62, 62, 66),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                FlatAppearance =
                {
                    BorderColor = Color.FromArgb(80, 80, 85),
                    MouseOverBackColor = Color.FromArgb(80, 80, 90),
                    MouseDownBackColor = Color.FromArgb(100, 100, 110)
                }
            };
        }

        private DataGridView CreateStyledGrid()
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.FromArgb(37, 37, 38),
                GridColor = Color.FromArgb(55, 55, 60),
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                ColumnHeadersHeight = 36,
                RowTemplate = { Height = 30 },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(45, 45, 48),
                    ForeColor = Color.White,
                    SelectionBackColor = Color.FromArgb(75, 75, 85),
                    SelectionForeColor = Color.White,
                    Font = new Font("Consolas", 10.5f),
                    Padding = new Padding(4, 2, 4, 2)
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(55, 55, 60),
                    ForeColor = Color.FromArgb(218, 185, 107),
                    Font = new Font("Segoe UI Semibold", 10f),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(40, 40, 43)
                }
            };
            return dgv;
        }

        private DataGridViewTextBoxColumn Col(string header, int width, bool readOnly = false)
        {
            return new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                Width = width,
                MinimumWidth = 40,
                ReadOnly = readOnly,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
        }

        private static decimal Clamp(int val)
        {
            return Math.Max(-200, Math.Min(300, val));
        }

        private static int ToInt(object val, int fallback)
        {
            if (val == null) return fallback;
            return int.TryParse(val.ToString(), out int result) ? result : fallback;
        }

        private static double ToDouble(object val, double fallback)
        {
            if (val == null) return fallback;
            return double.TryParse(val.ToString(),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double result) ? result : fallback;
        }
    }
}
