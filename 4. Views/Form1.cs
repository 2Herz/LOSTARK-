using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using LostArkCalculator.Core;
using LostArkCalculator.Models;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace LostArkCalculator.Views
{
    public partial class Form1 : Form
    {
        private readonly IDataRepository _db;
        private readonly IMarketApi _api;
        private readonly IAssetCalculator _calc;

        private bool isLoadingChar = false;
        private Timer autoTimer;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        public Form1(IDataRepository db, IMarketApi api, IAssetCalculator calc)
        {
            InitializeComponent();
            _db = db;
            _api = api;
            _calc = calc;

            // 계산 완료 이벤트를 구독하여 화면 갱신
            _calc.OnAssetCalculated += RefreshUI;

            this.Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitializeGrid();
            InitializeChart();
            InitializeTrayIcon();

            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            foreach (var key in _db.Data.Characters.Keys) comboBox1.Items.Add(key);
            if (comboBox1.Items.Count > 0) comboBox1.SelectedIndex = 0;

            comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;
            button2.Click += Button2_Click;
            dataGridView1.CellValueChanged += DataGridView1_CellValueChanged;

            autoTimer = new Timer { Interval = 300000 };
            autoTimer.Tick += (s, ev) => { if (button1.Enabled) button1.PerformClick(); };

            if (this.Controls.ContainsKey("checkBox1") || checkBox1 != null)
                checkBox1.CheckedChanged += (s, ev) => { if (checkBox1.Checked) { autoTimer.Start(); button1.PerformClick(); } else autoTimer.Stop(); };

            this.Resize += Form1_Resize;
            RefreshUI();
        }

        // =====================================
        // UI 초기화 및 유틸리티 
        // =====================================
        private void InitializeGrid()
        {
            if (dataGridView1.Columns.Count > 0) return;
            string[] cols = { "Category|구분", "Shard|운파", "Dest|파괴석", "Guard|수호석", "Leap|돌파석", "Fusion|아비도스", "Yong|용숨", "Bing|빙숨", "Total|합계(골드)" };
            foreach (var c in cols) dataGridView1.Columns.Add(c.Split('|')[0], c.Split('|')[1]);

            dataGridView1.Rows.Add("상위단가", 0, 0, 0, 0, 0, 0, 0, "");
            dataGridView1.Rows.Add("하위단가", 0, 0, 0, 0, 0, 0, 0, "");
            dataGridView1.Rows.Add("상위재료값", 0, 0, 0, 0, 0, 0, 0, 0);
            dataGridView1.Rows.Add("하위재료값", 0, 0, 0, 0, 0, 0, 0, 0);
            dataGridView1.Rows.Add("보유수량(상위)", 0, 0, 0, 0, 0, 0, 0, "");
            dataGridView1.Rows.Add("보유수량(하위)", 0, 0, 0, 0, 0, 0, 0, "");
            dataGridView1.Rows.Add("★ 캐릭터 자산 ★", "", "", "", "", "", "", "", 0);

            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                if (i != 4 && i != 5) { dataGridView1.Rows[i].ReadOnly = true; dataGridView1.Rows[i].DefaultCellStyle.BackColor = Color.WhiteSmoke; }
                if (i >= 2) dataGridView1.Rows[i].DefaultCellStyle.Format = "N0";
            }
            dataGridView1.Columns[8].DefaultCellStyle.BackColor = Color.LightCyan;
            dataGridView1.Rows[6].DefaultCellStyle.BackColor = Color.LightYellow;
        }

        private void InitializeChart()
        {
            chart1.Series.Clear();
            chart1.Series.Add(new Series("원정대 총 자산") { ChartType = SeriesChartType.Line, BorderWidth = 3, Color = Color.CornflowerBlue, MarkerStyle = MarkerStyle.Circle, MarkerSize = 8 });
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("열기", null, (s, ev) => { this.Show(); this.WindowState = FormWindowState.Normal; });
            trayMenu.Items.Add("종료", null, (s, ev) => Application.Exit());

            trayIcon = new NotifyIcon { Text = "로스트아크 자산 계산기", Icon = SystemIcons.Application, ContextMenuStrip = trayMenu, Visible = true };
            trayIcon.DoubleClick += (s, ev) => { this.Show(); this.WindowState = FormWindowState.Normal; };
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                trayIcon.BalloonTipTitle = "로아 계산기 백그라운드 실행 중";
                trayIcon.BalloonTipText = "5분마다 시세를 자동으로 갱신합니다.";
                trayIcon.ShowBalloonTip(2000);
            }
        }

        private double GetSafeDouble(object cellValue)
        {
            if (cellValue == null || cellValue == DBNull.Value) return 0;
            string strVal = cellValue.ToString().Replace(",", "").Replace(" ", "").Trim();
            if (double.TryParse(strVal, out double result)) return result;
            return 0;
        }

        // =====================================
        // 사용자 이벤트 및 매니저 호출
        // =====================================
        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem == null) return;
            isLoadingChar = true;
            string charName = comboBox1.SelectedItem.ToString().Split(' ')[0];
            var charData = _db.Data.Characters[charName];

            for (int col = 1; col <= 7; col++)
            {
                dataGridView1.Rows[4].Cells[col].Value = charData.Materials[AppConstants.MatKeys[col]].TopQty;
                dataGridView1.Rows[5].Cells[col].Value = charData.Materials[AppConstants.MatKeys[col]].BottomQty;
            }
            isLoadingChar = false;
            _calc.CalculateCharacterAssets(charName); // 매니저에게 계산 지시
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            dataGridView1.EndEdit(); // 버그 픽스: 편집 강제 완료
            button1.Enabled = false; button1.Text = "시세 동기화 중...";
            try
            {
                await _calc.FetchMarketPricesAsync(_api);
                if (comboBox1.SelectedItem != null) _calc.CalculateCharacterAssets(comboBox1.SelectedItem.ToString().Split(' ')[0]);
                _calc.CalculateRosterHistory();

                if (!checkBox1.Checked) MessageBox.Show("시세 동기화 완료!", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { button1.Enabled = true; button1.Text = "시세 갱신 및 계산"; }
        }

        private async void Button2_Click(object sender, EventArgs e)
        {
            string mainCharName = textBox1.Text.Trim();
            if (string.IsNullOrEmpty(mainCharName) || mainCharName == "캐릭터 닉네임") { MessageBox.Show("캐릭터 이름을 입력하세요."); return; }

            button2.Enabled = false; button2.Text = "동기화 중...";
            try
            {
                var roster = await _api.GetRosterAsync(mainCharName);
                if (roster.Count == 0) { MessageBox.Show("캐릭터 정보를 찾을 수 없습니다."); return; }

                var sortedRoster = roster.OrderByDescending(c => c.NumericItemLevel).ToList();
                var newDict = new Dictionary<string, Character>();

                comboBox1.SelectedIndexChanged -= ComboBox1_SelectedIndexChanged;
                comboBox1.Items.Clear();

                foreach (var c in sortedRoster)
                {
                    string name = c.CharacterName;
                    newDict[name] = _db.Data.Characters.ContainsKey(name) ? _db.Data.Characters[name] : new Character();
                    comboBox1.Items.Add($"{name} ({c.CharacterClassName}, Lv.{c.NumericItemLevel})");
                }

                _db.Data.Characters = newDict;
                _db.Save();

                comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;
                if (comboBox1.Items.Count > 0) comboBox1.SelectedIndex = 0;

                MessageBox.Show($"총 {sortedRoster.Count}개의 캐릭터 연동 완료!", "성공", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show("오류 발생: " + ex.Message); }
            finally { button2.Enabled = true; button2.Text = "원정대 동기화"; }
        }

        private void DataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (isLoadingChar || comboBox1.SelectedItem == null) return;
            if (e.RowIndex == 4 || e.RowIndex == 5)
            {
                string charName = comboBox1.SelectedItem.ToString().Split(' ')[0];
                for (int col = 1; col <= 7; col++)
                {
                    _db.Data.Characters[charName].Materials[AppConstants.MatKeys[col]].TopQty = GetSafeDouble(dataGridView1.Rows[4].Cells[col].Value);
                    _db.Data.Characters[charName].Materials[AppConstants.MatKeys[col]].BottomQty = GetSafeDouble(dataGridView1.Rows[5].Cells[col].Value);
                }
                _db.Save();
                _calc.CalculateCharacterAssets(charName);
            }
        }

        // =====================================
        // 옵저버 패턴 응답 (UI 그리기 전담)
        // =====================================
        private void RefreshUI()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(RefreshUI)); return; }

            // 1. 단가 및 등락폭 그리기
            for (int col = 1; col <= 7; col++)
            {
                string key = AppConstants.MatKeys[col];
                if (_calc.TopPrices.ContainsKey(key)) FormatPriceCell(dataGridView1.Rows[0].Cells[col], _calc.TopPrices[key]);
                if (_calc.BotPrices.ContainsKey(key)) FormatPriceCell(dataGridView1.Rows[1].Cells[col], _calc.BotPrices[key]);
            }

            // 2. 캐릭터 자산 합산액 그리기
            if (comboBox1.SelectedItem != null)
            {
                string charName = comboBox1.SelectedItem.ToString().Split(' ')[0];
                var charData = _db.Data.Characters[charName];
                double topTotal = 0, botTotal = 0;

                for (int col = 1; col <= 7; col++)
                {
                    string key = AppConstants.MatKeys[col];
                    dataGridView1.Rows[2].Cells[col].Value = charData.Materials[key].CalculatedTopValue;
                    dataGridView1.Rows[3].Cells[col].Value = charData.Materials[key].CalculatedBotValue;
                    topTotal += charData.Materials[key].CalculatedTopValue;
                    botTotal += charData.Materials[key].CalculatedBotValue;
                }
                dataGridView1.Rows[2].Cells[8].Value = topTotal;
                dataGridView1.Rows[3].Cells[8].Value = botTotal;
                dataGridView1.Rows[6].Cells[8].Value = topTotal + botTotal;
            }

            // 3. 차트 및 원정대 총 자산 그리기
            chart1.Series[0].Points.Clear();
            double currentTotal = 0;
            foreach (var kvp in _db.Data.History)
            {
                chart1.Series[0].Points.AddXY(kvp.Key, kvp.Value);
                currentTotal = kvp.Value;
            }

            if (chart1.Titles.Count == 0) chart1.Titles.Add(new Title { Font = new Font("맑은 고딕", 12, FontStyle.Bold) });
            chart1.Titles[0].Text = $"내 원정대 총 자산: {currentTotal:N0} 골드";
            if (trayIcon != null) trayIcon.Text = $"원정대 자산: {currentTotal:N0} 골드";
        }

        private void FormatPriceCell(DataGridViewCell cell, PriceData p)
        {
            if (p.Diff > 0)
            {
                cell.Style.ForeColor = Color.Red;
                cell.Value = $"{p.Current:N2} (▲{p.Diff:N2})";
            }
            else if (p.Diff < 0)
            {
                cell.Style.ForeColor = Color.Blue;
                cell.Value = $"{p.Current:N2} (▼{Math.Abs(p.Diff):N2})";
            }
            else
            {
                cell.Style.ForeColor = Color.Black;
                cell.Value = $"{p.Current:N2}";
            }
        }
    }
}