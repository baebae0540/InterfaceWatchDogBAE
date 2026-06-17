using InterfaceWatchDog.Core;
using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.UI.Forms;

public class SettingsForm : Form
{
    private AppConfig _config;
    private readonly bool _erwekaIsRunning;

    private TextBox       _erwekaProcessName = null!;
    private TextBox       _erwekaArguments   = null!;
    private NumericUpDown _erwekaCheckSec    = null!;
    private NumericUpDown _erwekaPort        = null!;
    private Label         _dbServer          = null!;
    private Label         _dbDatabase        = null!;
    private Label         _dbUserId          = null!;
    private Label         _dbStatusLabel     = null!;
    private bool          _dbTestPassed;
    private TextBox       _tabProcessName = null!;
    private TextBox       _tabExePath     = null!;
    private TextBox       _tabArguments   = null!;
    private NumericUpDown _tabMaxRetry    = null!;
    private NumericUpDown _tabCheckSec    = null!;

    private TextBox       _pdfFolder     = null!;
    private NumericUpDown _pdfMaxIdle    = null!;
    private NumericUpDown _pdfMaxBacklog = null!;
    private NumericUpDown _pdfCheckMin   = null!;

    public SettingsForm(AppConfig config, bool erwekaIsRunning = false)
    {
        _config = config;
        _erwekaIsRunning = erwekaIsRunning;
        AutoScaleMode = AutoScaleMode.Dpi;
        InitializeComponent();
        LoadValues();
    }

    private void InitializeComponent()
    {
        Text            = "InterfaceWatchDog — 설정";
        Size            = new Size(860, 640);
        MinimumSize     = new Size(760, 560);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox     = false;
        Font            = new Font("맑은 고딕", 9.5f);
        BackColor       = Color.FromArgb(245, 246, 250);

        // ── 하단 버튼 바 ─────────────────────────────────────────────────────
        var btnBar  = new Panel { Dock = DockStyle.Bottom, Height = 62, BackColor = Color.White };
        var divLine = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(210, 215, 228) };

        var btnFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Right, AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 8, 14, 0)
        };
        var btnSave = new Button
        {
            Text = "저장", Size = new Size(110, 36),
            BackColor = Color.FromArgb(33, 120, 220), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = new Font("맑은 고딕", 10f, FontStyle.Bold), Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += BtnSave_Click;

        var btnCancel = new Button
        {
            Text = "취소", Size = new Size(100, 36),
            BackColor = Color.FromArgb(228, 231, 238), ForeColor = Color.FromArgb(50, 55, 70),
            FlatStyle = FlatStyle.Flat, Font = new Font("맑은 고딕", 10f), Cursor = Cursors.Hand,
            CausesValidation = false
        };
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(195, 200, 215);
        btnCancel.Click += (_, _) => Close();

        btnFlow.Controls.Add(btnSave);
        btnFlow.Controls.Add(btnCancel);
        btnBar.Controls.AddRange([divLine, btnFlow]);

        // ── TabControl ───────────────────────────────────────────────────────
        var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("맑은 고딕", 9.5f), Padding = new Point(16, 6) };

        // 탭 1: ERWEKA — rows: text(0-1) + text(2-3) + num(4) + num(5) + ro(6) + ro(7) + btn+status(8)
        var (pg1, tbl1) = MakeTab("① ERWEKA Export Manager");
        AddRowStyles(tbl1, 42, 28, 42, 28, 52, 52, 186);
        tbl1.SuspendLayout();
        _erwekaProcessName = AddTextWithButtonAt(tbl1, 0, "프로세스 이름",
            "확장자(.exe) 없이 입력 — \"가져오기\"로 실행 중인 프로그램에서 자동 입력",
            "가져오기", (_, _) => PickRunningProgram(_erwekaProcessName, null!, _erwekaArguments, _erwekaPort, this));
        _erwekaArguments   = AddTextAt(tbl1, 2, "프로세스 구분 문자열", "동일 이름의 다른 프로세스와 구분하기 위한 명령행 포함 문자열 (예: Export Manager)");
        _erwekaPort        = AddNumAt(tbl1, 4, "TCP 포트 감시 (0=미사용)", 0, 65535, "포트");
        _erwekaCheckSec    = AddNumAt(tbl1, 5, "프로세스 체크 주기", 10, 300, "초");
        AddDbInfoCard(tbl1, 6);
        tbl1.ResumeLayout(true);

        // 탭 2: TabmachineIF — 동일 구조
        var (pg2, tbl2) = MakeTab("② TabmachineIF");
        AddRowStyles(tbl2, 42, 28, 42, 28, 42, 28, 52, 52);
        tbl2.SuspendLayout();
        _tabProcessName = AddTextWithButtonAt(tbl2, 0, "프로세스 이름 *",
            "확장자(.exe) 없이 입력 — \"가져오기\"로 실행 중인 프로그램에서 자동 입력",
            "가져오기", (_, _) => PickRunningProgram(_tabProcessName, _tabExePath, _tabArguments, owner: this));
        _tabExePath     = AddBrowseAt(tbl2, 2, "실행 파일 경로",   "미입력 시 감시만 수행 (재시작 불가)", isExe: true, autoFill: _tabProcessName);
        _tabArguments   = AddTextAt(tbl2, 4, "실행 인수 (선택)", "재시작 시 함께 전달할 명령행 인수");
        _tabMaxRetry    = AddNumAt(tbl2, 6, "최대 재시작 횟수",  1, 10, "회");
        _tabCheckSec    = AddNumAt(tbl2, 7, "프로세스 체크 주기", 10, 300, "초");
        tbl2.ResumeLayout(true);

        // 탭 3: PDF 폴더 — browse(0-1) + num(2) + num(3) + num(4)
        var (pg3, tbl3) = MakeTab("③ PDF 폴더 감시");
        AddRowStyles(tbl3, 42, 28, 52, 52, 52);
        tbl3.SuspendLayout();
        _pdfFolder     = AddBrowseAt(tbl3, 0, "PDF 폴더 경로",       "ERWEKA가 PDF를 저장하는 폴더   (비워두면 PDF 폴더 감시를 사용하지 않음)", isExe: false, autoFill: null);
        _pdfMaxIdle    = AddNumAt(tbl3, 2, "신규 파일 없음 경고 기준", 1, 1440, "분");
        _pdfMaxBacklog = AddNumAt(tbl3, 3, "누적 파일 수 경고 기준",   1, 9999, "개");
        _pdfCheckMin   = AddNumAt(tbl3, 4, "파일 활동 체크 주기",    1,  60, "분");
        tbl3.ResumeLayout(true);

        if (_config.PdfFolder.Visible)
        {
            pg3.Enabled = _erwekaIsRunning;
            tabs.TabPages.AddRange([pg1, pg2, pg3]);
        }
        else
        {
            tabs.TabPages.AddRange([pg1, pg2]);
        }
        Controls.Add(tabs);
        Controls.Add(btnBar);
    }

    // =========================================================================
    // 탭 + 내부 그리드 생성 (행 없이 열만 정의)
    // 3열: [레이블 170px] | [입력 Percent] | [버튼/단위 110px]
    // =========================================================================
    private static (TabPage page, TableLayoutPanel grid) MakeTab(string title)
    {
        var page = new TabPage(title)
        {
            BackColor = Color.White,
            Padding   = new Padding(14, 12, 14, 12)
        };
        var grid = new TableLayoutPanel
        {
            Dock            = DockStyle.Fill,
            ColumnCount     = 3,
            RowCount        = 0,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            BackColor       = Color.White
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
        page.Controls.Add(grid);
        return (page, grid);
    }

    // 모든 행 높이를 한 번에 정의 (컨트롤 추가 전 반드시 호출)
    // 마지막에 Percent=100% 여백 행을 추가해 Dock=Fill 컨트롤이 마지막 실제 행을
    // 벗어나 늘어나는 WinForms TLP 동작을 방지
    private static void AddRowStyles(TableLayoutPanel grid, params float[] heights)
    {
        foreach (var h in heights)
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // 여백 흡수 행
        grid.RowCount = heights.Length + 1;
    }

    // =========================================================================
    // 텍스트 입력 행: row=입력행, row+1=힌트행
    // =========================================================================
    private static TextBox AddTextAt(TableLayoutPanel grid, int row, string label, string hint)
    {
        grid.Controls.Add(MakeLbl(label), 0, row);

        var txt = new TextBox
        {
            Dock   = DockStyle.Fill,
            Font   = new Font("맑은 고딕", 9.5f),
            Margin = new Padding(0, 6, 6, 6)
        };
        grid.Controls.Add(txt, 1, row);
        grid.SetColumnSpan(txt, 2);

        var hintLbl = MakeHint(hint);
        grid.Controls.Add(hintLbl, 1, row + 1);
        grid.SetColumnSpan(hintLbl, 2);

        return txt;
    }

    // =========================================================================
    // 텍스트 입력 + 커스텀 버튼 행: row=입력행, row+1=힌트행
    // =========================================================================
    private static TextBox AddTextWithButtonAt(TableLayoutPanel grid, int row, string label, string hint,
        string buttonText, EventHandler onClick)
    {
        grid.Controls.Add(MakeLbl(label), 0, row);

        var txt = new TextBox
        {
            Dock   = DockStyle.Fill,
            Font   = new Font("맑은 고딕", 9.5f),
            Margin = new Padding(0, 6, 4, 6)
        };
        grid.Controls.Add(txt, 1, row);

        var btn = new Button
        {
            Dock        = DockStyle.Fill,
            Text        = buttonText,
            FlatStyle   = FlatStyle.Flat,
            Font        = new Font("맑은 고딕", 8.5f),
            BackColor   = Color.FromArgb(210, 215, 228),
            ForeColor   = Color.FromArgb(30, 36, 60),
            Cursor      = Cursors.Hand,
            Margin      = new Padding(4, 4, 0, 4),
            MinimumSize = new Size(90, 0)
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(150, 160, 190);
        btn.FlatAppearance.BorderSize  = 1;
        btn.Click += onClick;
        grid.Controls.Add(btn, 2, row);

        var hintLbl = MakeHint(hint);
        grid.Controls.Add(hintLbl, 1, row + 1);
        grid.SetColumnSpan(hintLbl, 2);

        return txt;
    }

    // 실행 중인 프로그램 창을 선택해 프로세스 이름/실행 파일 경로/실행 인수(및 TCP 포트)를 자동으로 채운다.
    private static void PickRunningProgram(TextBox processName, TextBox? exePath, TextBox arguments,
        NumericUpDown? port = null, IWin32Window? owner = null)
    {
        using var picker = new ProcessPickerForm();
        if (picker.ShowDialog(owner) != DialogResult.OK || picker.Selected == null) return;

        var info = RunningProgramFinder.GetLaunchInfo(picker.Selected.Pid);
        if (info == null)
        {
            MessageBox.Show(owner, "선택한 프로그램의 정보를 가져올 수 없습니다.",
                "InterfaceWatchDog", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        processName.Text = info.ProcessName;
        if (exePath != null) exePath.Text = info.ExecutablePath;
        arguments.Text   = info.Arguments;

        if (port != null && info.ListeningPorts.Count > 0)
        {
            var p = info.ListeningPorts[0];
            if (p >= port.Minimum && p <= port.Maximum)
                port.Value = p;
        }
    }

    private void AddDbInfoCard(TableLayoutPanel grid, int row)
    {
        var card = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.FromArgb(248, 249, 253),
            Margin    = new Padding(0, 10, 0, 6),
            Padding   = new Padding(18, 14, 18, 12)
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(210, 216, 230), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var inner = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 5,
            BackColor   = Color.Transparent
        };
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
        inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
        inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var title = new Label
        {
            Text      = "DB 연결 정보",
            Dock      = DockStyle.Fill,
            Font      = new Font("맑은 고딕", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(60, 68, 90),
            TextAlign = ContentAlignment.MiddleLeft
        };
        inner.Controls.Add(title, 0, 0);
        inner.SetColumnSpan(title, 2);

        Label MakeKey(string t) => new()
        {
            Text = t, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("맑은 고딕", 8.5f), ForeColor = Color.FromArgb(120, 128, 148)
        };

        Label MakeVal() => new()
        {
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("맑은 고딕", 9f), ForeColor = Color.FromArgb(46, 52, 72)
        };

        _dbServer   = MakeVal();
        _dbDatabase = MakeVal();
        _dbUserId   = MakeVal();

        inner.Controls.Add(MakeKey("Server"),   0, 1);
        inner.Controls.Add(_dbServer,           1, 1);
        inner.Controls.Add(MakeKey("Database"), 0, 2);
        inner.Controls.Add(_dbDatabase,         1, 2);
        inner.Controls.Add(MakeKey("User ID"),  0, 3);
        inner.Controls.Add(_dbUserId,           1, 3);

        // ── 연결 테스트 버튼 + 상태 뱃지 ─────────────────────────────────────
        var btnRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false, BackColor = Color.Transparent,
            Margin = new Padding(0), Padding = new Padding(0, 4, 0, 0)
        };

        var btn = new Button
        {
            Text = "연결 테스트", Width = 130, Height = 34,
            FlatStyle = FlatStyle.Flat, Font = new Font("맑은 고딕", 9.5f),
            BackColor = Color.FromArgb(33, 120, 220), ForeColor = Color.White,
            Cursor = Cursors.Hand, Margin = new Padding(0)
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += BtnDbTest_Click;

        _dbStatusLabel = new Label
        {
            AutoSize  = true,
            Font      = new Font("맑은 고딕", 9f, FontStyle.Bold),
            Margin    = new Padding(12, 7, 0, 0)
        };
        if (_config.DbConnectionVerified)
        {
            _dbStatusLabel.Text      = "● 연결됨";
            _dbStatusLabel.ForeColor = Color.FromArgb(34, 160, 60);
        }

        btnRow.Controls.Add(btn);
        btnRow.Controls.Add(_dbStatusLabel);
        inner.Controls.Add(btnRow, 0, 4);
        inner.SetColumnSpan(btnRow, 2);

        card.Controls.Add(inner);
        grid.Controls.Add(card, 0, row);
        grid.SetColumnSpan(card, 3);
    }

    private async void BtnDbTest_Click(object? sender, EventArgs e)
    {
        var dbConfig = ConfigManager.LoadAlarmDb();

        _dbServer.Text   = dbConfig.Server;
        _dbDatabase.Text = dbConfig.Database;
        _dbUserId.Text   = dbConfig.UserId;

        if (!dbConfig.IsConfigured)
        {
            _dbTestPassed = false;
            _dbStatusLabel.Text      = "● 미설정";
            _dbStatusLabel.ForeColor = Color.FromArgb(180, 140, 20);
            MessageBox.Show(this,
                $"dbconfig.json이 설정되지 않았습니다.\n경로: {ConfigManager.DbConfigFilePath}",
                "연결 테스트", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            await using var conn = new Microsoft.Data.SqlClient.SqlConnection(dbConfig.ConnectionString);
            await conn.OpenAsync();
            _dbTestPassed = true;
            _dbStatusLabel.Text      = "● 연결됨";
            _dbStatusLabel.ForeColor = Color.FromArgb(34, 160, 60);
        }
        catch (Exception ex)
        {
            _dbTestPassed = false;
            _dbStatusLabel.Text      = "● 연결 실패";
            _dbStatusLabel.ForeColor = Color.FromArgb(220, 50, 50);
            MessageBox.Show(this, $"DB 연결 실패:\n{ex.Message}",
                "연결 테스트", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // =========================================================================
    // 찾아보기 행: row=입력행, row+1=힌트행
    // =========================================================================
    private TextBox AddBrowseAt(TableLayoutPanel grid, int row, string label, string hint,
        bool isExe, TextBox? autoFill)
    {
        grid.Controls.Add(MakeLbl(label), 0, row);

        var txt = new TextBox
        {
            Dock   = DockStyle.Fill,
            Font   = new Font("맑은 고딕", 9.5f),
            Margin = new Padding(0, 6, 4, 6)
        };
        grid.Controls.Add(txt, 1, row);

        var btn = new Button
        {
            Dock        = DockStyle.Fill,
            Text        = "찾아보기",
            FlatStyle   = FlatStyle.Flat,
            Font        = new Font("맑은 고딕", 8.5f),
            BackColor   = Color.FromArgb(210, 215, 228),
            ForeColor   = Color.FromArgb(30, 36, 60),
            Cursor      = Cursors.Hand,
            Margin      = new Padding(4, 4, 0, 4),
            MinimumSize = new Size(90, 0)
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(150, 160, 190);
        btn.FlatAppearance.BorderSize  = 1;
        grid.Controls.Add(btn, 2, row);

        var hintLbl = MakeHint(hint);
        grid.Controls.Add(hintLbl, 1, row + 1);
        grid.SetColumnSpan(hintLbl, 2);

        if (isExe)
            btn.Click += (_, _) =>
            {
                using var dlg = new OpenFileDialog { Filter = "실행 파일 (*.exe)|*.exe" };
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    txt.Text = dlg.FileName;
                    if (autoFill != null && string.IsNullOrWhiteSpace(autoFill.Text))
                        autoFill.Text = Path.GetFileNameWithoutExtension(dlg.FileName);
                }
            };
        else
            btn.Click += (_, _) =>
            {
                using var dlg = new FolderBrowserDialog { Description = "폴더 선택" };
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    txt.Text = dlg.SelectedPath;
            };

        return txt;
    }

    // =========================================================================
    // 숫자 입력 행: 레이블(col0) | NumericUpDown(col1) | 단위(col2)
    // =========================================================================
    private static NumericUpDown AddNumAt(TableLayoutPanel grid, int row, string label,
        decimal min, decimal max, string suffix)
    {
        grid.Controls.Add(MakeLbl(label), 0, row);

        var numPanel = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            Margin        = new Padding(0, 10, 0, 10)
        };
        var num = new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Font    = new Font("맑은 고딕", 9.5f),
            Width   = 110,
            Margin  = new Padding(0)
        };

        num.Validating += (_, e) =>
        {
            if (decimal.TryParse(num.Text, out var typed) && (typed < num.Minimum || typed > num.Maximum))
            {
                MessageBox.Show(
                    $"{label}은(는) {num.Minimum}~{num.Maximum}{suffix} 범위로 입력해야 합니다.",
                    "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                num.Select(0, num.Text.Length);
            }
        };

        var sfx = new Label
        {
            Text      = suffix,
            AutoSize  = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Font      = new Font("맑은 고딕", 9.5f),
            ForeColor = Color.FromArgb(70, 80, 100),
            Margin    = new Padding(6, 5, 0, 0)
        };

        numPanel.Controls.Add(num);
        numPanel.Controls.Add(sfx);
        grid.Controls.Add(numPanel, 1, row);
        grid.SetColumnSpan(numPanel, 2);

        return num;
    }

    // =========================================================================
    // 공통 헬퍼
    // =========================================================================
    private static Label MakeLbl(string text) => new()
    {
        Text      = text,
        Dock      = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Font      = new Font("맑은 고딕", 9.5f),
        ForeColor = Color.FromArgb(46, 52, 72),
        Padding   = new Padding(4, 0, 0, 0)
    };

    private static Label MakeHint(string text) => new()
    {
        Text      = text,
        Dock      = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Font      = new Font("맑은 고딕", 8.5f),
        ForeColor = Color.FromArgb(148, 155, 172),
        Margin    = new Padding(4, 4, 0, 4)
    };

    // =========================================================================
    // 값 로드 / 저장
    // =========================================================================
    private void LoadValues()
    {
        _erwekaProcessName.Text  = _config.Erweka.ProcessName;
        _erwekaArguments.Text    = _config.Erweka.Arguments;
        _erwekaCheckSec.Value    = Math.Clamp(_config.Erweka.ProcessCheckSeconds, 10, 300);
        _erwekaPort.Value        = Math.Clamp(_config.Erweka.Port, 0, 65535);

        var dbConfig = ConfigManager.LoadAlarmDb();
        _dbServer.Text   = dbConfig.Server;
        _dbDatabase.Text = dbConfig.Database;
        _dbUserId.Text   = dbConfig.UserId;
        _dbTestPassed    = _config.DbConnectionVerified;

        _tabProcessName.Text     = _config.TabmachineIF.ProcessName;
        _tabExePath.Text         = _config.TabmachineIF.ExecutablePath;
        _tabArguments.Text       = _config.TabmachineIF.Arguments;
        _tabMaxRetry.Value       = Math.Clamp(_config.TabmachineIF.MaxRestartAttempts, 1, 10);
        _tabCheckSec.Value       = Math.Clamp(_config.TabmachineIF.ProcessCheckSeconds, 10, 300);

        _pdfFolder.Text          = _config.PdfFolder.Path;
        _pdfMaxIdle.Value        = Math.Clamp(_config.PdfFolder.MaxIdleMinutes, 1, 1440);
        _pdfMaxBacklog.Value     = Math.Clamp(_config.PdfFolder.MaxBacklogCount, 1, 9999);
        _pdfCheckMin.Value       = Math.Clamp(_config.PdfFolder.FileActivityCheckMinutes, 1, 60);
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_tabProcessName.Text))
        {
            MessageBox.Show(this, "TabmachineIF의 프로세스 이름은 필수입니다.",
                "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var latestDb = ConfigManager.LoadAlarmDb();
        if (_dbTestPassed && !latestDb.IsConfigured)
        {
            _dbTestPassed = false;
            _dbServer.Text   = latestDb.Server;
            _dbDatabase.Text = latestDb.Database;
            _dbUserId.Text   = latestDb.UserId;
            _dbStatusLabel.Text      = "● 설정 누락";
            _dbStatusLabel.ForeColor = Color.FromArgb(220, 50, 50);
            MessageBox.Show(this,
                $"dbconfig.json의 필수 값이 누락되어 DB 연결 상태가 초기화되었습니다.\n경로: {ConfigManager.DbConfigFilePath}",
                "DB 설정 변경 감지", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!_dbTestPassed)
        {
            var result = MessageBox.Show(this,
                "DB 연결 테스트가 완료되지 않았습니다.\n테스트 없이 저장하면 에러 발생 시 알람이 DB에 기록되지 않습니다.\n\n그래도 저장하시겠습니까?",
                "DB 연결 미확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.No)
                return;
        }

        _config.Erweka.ProcessName          = _erwekaProcessName.Text.Trim();
        _config.Erweka.Arguments            = _erwekaArguments.Text.Trim();
        _config.Erweka.ProcessCheckSeconds  = (int)_erwekaCheckSec.Value;
        _config.Erweka.Port                 = (int)_erwekaPort.Value;

        _config.DbConnectionVerified = _dbTestPassed;

        _config.TabmachineIF.ProcessName          = _tabProcessName.Text.Trim();
        _config.TabmachineIF.ExecutablePath       = _tabExePath.Text.Trim();
        _config.TabmachineIF.Arguments            = _tabArguments.Text.Trim();
        _config.TabmachineIF.MaxRestartAttempts   = (int)_tabMaxRetry.Value;
        _config.TabmachineIF.ProcessCheckSeconds  = (int)_tabCheckSec.Value;

        if (_config.PdfFolder.Visible)
        {
            _config.PdfFolder.Path                    = _pdfFolder.Text.Trim();
            _config.PdfFolder.MaxIdleMinutes          = (int)_pdfMaxIdle.Value;
            _config.PdfFolder.MaxBacklogCount         = (int)_pdfMaxBacklog.Value;
            _config.PdfFolder.FileActivityCheckMinutes = (int)_pdfCheckMin.Value;
        }

        ConfigManager.Save(_config);
        DialogResult = DialogResult.OK;
        Close();
    }
}
