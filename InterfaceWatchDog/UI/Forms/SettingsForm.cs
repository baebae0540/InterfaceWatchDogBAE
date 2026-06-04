using InterfaceWatchDog.Core;
using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.UI.Forms;

public class SettingsForm : Form
{
    private AppConfig _config;

    // ERWEKA
    private TextBox _erwekaProcessName = null!;
    private TextBox _erwekaExePath = null!;
    private TextBox _erwekaArguments = null!;
    private NumericUpDown _erwekaMaxRetry = null!;

    // TabmachineIF
    private TextBox _tabProcessName = null!;
    private TextBox _tabExePath = null!;
    private TextBox _tabArguments = null!;
    private NumericUpDown _tabMaxRetry = null!;

    // PDF 폴더
    private TextBox _pdfFolder = null!;
    private NumericUpDown _pdfMaxIdle = null!;
    private NumericUpDown _pdfMaxBacklog = null!;

    // 감시 주기
    private NumericUpDown _processCheckSec = null!;
    private NumericUpDown _fileCheckMin = null!;

    public SettingsForm(AppConfig config)
    {
        _config = config;
        InitializeComponent();
        LoadToControls();
    }

    private void InitializeComponent()
    {
        Text = "InterfaceWatchDog - 설정";
        Size = new Size(560, 620);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("맑은 고딕", 9f);
        BackColor = Color.White;

        var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 12, 16, 8), AutoScroll = true };

        int y = 0;

        // ── ERWEKA ──────────────────────────────────────────────────────────
        var erwekaGroup = CreateGroup("ERWEKA Export Manager", y, 185);
        y += 195;

        _erwekaProcessName = AddRow(erwekaGroup, "프로세스 이름 *", 30, hint: "확장자(.exe) 제외");
        _erwekaExePath = AddRowWithBrowse(erwekaGroup, "실행 파일 경로", 72, isExe: true);
        _erwekaArguments = AddRow(erwekaGroup, "실행 인수 (선택)", 114);
        _erwekaMaxRetry = AddNumericRow(erwekaGroup, "최대 재시작 횟수", 148, 1, 10);

        // ── TabmachineIF ────────────────────────────────────────────────────
        var tabGroup = CreateGroup("TabmachineIF", y, 185);
        y += 195;

        _tabProcessName = AddRow(tabGroup, "프로세스 이름 *", 30, hint: "확장자(.exe) 제외");
        _tabExePath = AddRowWithBrowse(tabGroup, "실행 파일 경로 *", 72, isExe: true);
        _tabArguments = AddRow(tabGroup, "실행 인수 (선택)", 114);
        _tabMaxRetry = AddNumericRow(tabGroup, "최대 재시작 횟수", 148, 1, 10);

        // ── PDF 폴더 ─────────────────────────────────────────────────────────
        var pdfGroup = CreateGroup("PDF 폴더 감시", y, 140);
        y += 150;

        _pdfFolder = AddRowWithBrowse(pdfGroup, "PDF 폴더 경로 *", 30, isExe: false);
        _pdfMaxIdle = AddNumericRow(pdfGroup, "신규 파일 없음 경고 기준 (분)", 72, 5, 1440);
        _pdfMaxBacklog = AddNumericRow(pdfGroup, "미처리 파일 누적 경고 (개)", 106, 1, 9999);

        // ── 감시 주기 ────────────────────────────────────────────────────────
        var intervalGroup = CreateGroup("감시 주기", y, 90);
        y += 100;

        _processCheckSec = AddNumericRow(intervalGroup, "프로세스 체크 주기 (초)", 30, 10, 300);
        _fileCheckMin = AddNumericRow(intervalGroup, "파일 활동 체크 주기 (분)", 62, 1, 60);

        // ── 버튼 ─────────────────────────────────────────────────────────────
        var btnPanel = new Panel { Width = 500, Height = 42, Location = new Point(0, y + 4) };
        var btnSave = new Button
        {
            Text = "저장",
            Size = new Size(100, 34),
            Location = new Point(290, 0),
            BackColor = Color.FromArgb(33, 150, 243),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("맑은 고딕", 9f, FontStyle.Bold)
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += BtnSave_Click;

        var btnCancel = new Button
        {
            Text = "취소",
            Size = new Size(100, 34),
            Location = new Point(400, 0),
            BackColor = Color.FromArgb(240, 240, 240),
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.Click += (_, _) => Close();

        btnPanel.Controls.AddRange([btnSave, btnCancel]);

        mainPanel.Controls.AddRange([erwekaGroup, tabGroup, pdfGroup, intervalGroup, btnPanel]);
        Controls.Add(mainPanel);
    }

    private static GroupBox CreateGroup(string title, int y, int height)
    {
        return new GroupBox
        {
            Text = title,
            Location = new Point(0, y),
            Size = new Size(510, height),
            Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(33, 150, 243)
        };
    }

    private static TextBox AddRow(GroupBox group, string label, int y, string hint = "")
    {
        var lbl = new Label
        {
            Text = label,
            Location = new Point(12, y + 3),
            Size = new Size(170, 18),
            Font = new Font("맑은 고딕", 9f),
            ForeColor = Color.FromArgb(60, 60, 60)
        };
        var txt = new TextBox
        {
            Location = new Point(188, y),
            Size = new Size(290, 22),
            Font = new Font("맑은 고딕", 9f)
        };
        if (!string.IsNullOrEmpty(hint))
        {
            var hintLbl = new Label
            {
                Text = hint,
                Location = new Point(188, y + 22),
                Size = new Size(290, 14),
                Font = new Font("맑은 고딕", 7.5f),
                ForeColor = Color.Gray
            };
            group.Controls.Add(hintLbl);
        }
        group.Controls.AddRange([lbl, txt]);
        return txt;
    }

    private TextBox AddRowWithBrowse(GroupBox group, string label, int y, bool isExe)
    {
        var lbl = new Label
        {
            Text = label,
            Location = new Point(12, y + 3),
            Size = new Size(170, 18),
            Font = new Font("맑은 고딕", 9f),
            ForeColor = Color.FromArgb(60, 60, 60)
        };
        var txt = new TextBox
        {
            Location = new Point(188, y),
            Size = new Size(240, 22),
            Font = new Font("맑은 고딕", 9f)
        };
        var btn = new Button
        {
            Text = "...",
            Location = new Point(432, y - 1),
            Size = new Size(46, 24),
            FlatStyle = FlatStyle.Flat
        };

        if (isExe)
        {
            btn.Click += (_, _) =>
            {
                using var dlg = new OpenFileDialog
                {
                    Filter = "실행 파일 (*.exe)|*.exe",
                    Title = "실행 파일 선택"
                };
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txt.Text = dlg.FileName;
                    // 프로세스 이름 자동 완성 (같은 그룹 내 ProcessName 박스 찾기)
                    AutoFillProcessName(group, dlg.FileName);
                }
            };
        }
        else
        {
            btn.Click += (_, _) =>
            {
                using var dlg = new FolderBrowserDialog { Description = "PDF 폴더 선택" };
                if (dlg.ShowDialog() == DialogResult.OK)
                    txt.Text = dlg.SelectedPath;
            };
        }

        group.Controls.AddRange([lbl, txt, btn]);
        return txt;
    }

    private static void AutoFillProcessName(GroupBox group, string exePath)
    {
        // 같은 GroupBox 안에 있는 첫 번째 TextBox (프로세스 이름 필드)가 비어있으면 자동 채움
        var firstTxt = group.Controls.OfType<TextBox>().FirstOrDefault();
        if (firstTxt != null && string.IsNullOrWhiteSpace(firstTxt.Text))
            firstTxt.Text = Path.GetFileNameWithoutExtension(exePath);
    }

    private static NumericUpDown AddNumericRow(GroupBox group, string label, int y, decimal min, decimal max)
    {
        var lbl = new Label
        {
            Text = label,
            Location = new Point(12, y + 3),
            Size = new Size(220, 18),
            Font = new Font("맑은 고딕", 9f),
            ForeColor = Color.FromArgb(60, 60, 60)
        };
        var num = new NumericUpDown
        {
            Location = new Point(238, y),
            Size = new Size(80, 22),
            Minimum = min,
            Maximum = max,
            Font = new Font("맑은 고딕", 9f)
        };
        group.Controls.AddRange([lbl, num]);
        return num;
    }

    private void LoadToControls()
    {
        _erwekaProcessName.Text = _config.Erweka.ProcessName;
        _erwekaExePath.Text = _config.Erweka.ExecutablePath;
        _erwekaArguments.Text = _config.Erweka.Arguments;
        _erwekaMaxRetry.Value = Math.Clamp(_config.Erweka.MaxRestartAttempts, 1, 10);

        _tabProcessName.Text = _config.TabmachineIF.ProcessName;
        _tabExePath.Text = _config.TabmachineIF.ExecutablePath;
        _tabArguments.Text = _config.TabmachineIF.Arguments;
        _tabMaxRetry.Value = Math.Clamp(_config.TabmachineIF.MaxRestartAttempts, 1, 10);

        _pdfFolder.Text = _config.PdfFolder.Path;
        _pdfMaxIdle.Value = Math.Clamp(_config.PdfFolder.MaxIdleMinutes, 5, 1440);
        _pdfMaxBacklog.Value = Math.Clamp(_config.PdfFolder.MaxBacklogCount, 1, 9999);

        _processCheckSec.Value = Math.Clamp(_config.Intervals.ProcessCheckSeconds, 10, 300);
        _fileCheckMin.Value = Math.Clamp(_config.Intervals.FileActivityCheckMinutes, 1, 60);
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_erwekaProcessName.Text) ||
            string.IsNullOrWhiteSpace(_tabProcessName.Text))
        {
            MessageBox.Show("ERWEKA와 TabmachineIF의 프로세스 이름은 필수입니다.",
                "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _config.Erweka.ProcessName = _erwekaProcessName.Text.Trim();
        _config.Erweka.ExecutablePath = _erwekaExePath.Text.Trim();
        _config.Erweka.Arguments = _erwekaArguments.Text.Trim();
        _config.Erweka.MaxRestartAttempts = (int)_erwekaMaxRetry.Value;

        _config.TabmachineIF.ProcessName = _tabProcessName.Text.Trim();
        _config.TabmachineIF.ExecutablePath = _tabExePath.Text.Trim();
        _config.TabmachineIF.Arguments = _tabArguments.Text.Trim();
        _config.TabmachineIF.MaxRestartAttempts = (int)_tabMaxRetry.Value;

        _config.PdfFolder.Path = _pdfFolder.Text.Trim();
        _config.PdfFolder.MaxIdleMinutes = (int)_pdfMaxIdle.Value;
        _config.PdfFolder.MaxBacklogCount = (int)_pdfMaxBacklog.Value;

        _config.Intervals.ProcessCheckSeconds = (int)_processCheckSec.Value;
        _config.Intervals.FileActivityCheckMinutes = (int)_fileCheckMin.Value;

        ConfigManager.Save(_config);

        DialogResult = DialogResult.OK;
        Close();
    }
}
