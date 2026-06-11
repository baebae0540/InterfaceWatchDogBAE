using InterfaceWatchDog.Core;
using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.UI.Forms;

public class SettingsForm : Form
{
    private AppConfig _config;

    private TextBox       _erwekaProcessName = null!;
    private TextBox       _erwekaExePath     = null!;
    private TextBox       _erwekaArguments   = null!;
    private NumericUpDown _erwekaMaxRetry    = null!;

    private TextBox       _tabProcessName = null!;
    private TextBox       _tabExePath     = null!;
    private TextBox       _tabArguments   = null!;
    private NumericUpDown _tabMaxRetry    = null!;

    private TextBox       _pdfFolder     = null!;
    private NumericUpDown _pdfMaxIdle    = null!;
    private NumericUpDown _pdfMaxBacklog = null!;

    private NumericUpDown _processCheckSec = null!;
    private NumericUpDown _fileCheckMin    = null!;

    public SettingsForm(AppConfig config)
    {
        _config = config;
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
            FlatStyle = FlatStyle.Flat, Font = new Font("맑은 고딕", 10f), Cursor = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(195, 200, 215);
        btnCancel.Click += (_, _) => Close();

        btnFlow.Controls.Add(btnSave);
        btnFlow.Controls.Add(btnCancel);
        btnBar.Controls.AddRange([divLine, btnFlow]);

        // ── TabControl ───────────────────────────────────────────────────────
        var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("맑은 고딕", 9.5f), Padding = new Point(16, 6) };

        // 탭 1: ERWEKA — rows: text(0-1) + browse(2-3) + text(4-5) + num(6)
        var (pg1, tbl1) = MakeTab("① ERWEKA Export Manager");
        AddRowStyles(tbl1, 42, 28, 42, 28, 42, 28, 52);
        tbl1.SuspendLayout();
        _erwekaProcessName = AddTextAt(tbl1, 0, "프로세스 이름",   "확장자(.exe) 없이 입력   예) ExportManager   (비워두면 ERWEKA 감시를 사용하지 않음)");
        _erwekaExePath     = AddBrowseAt(tbl1, 2, "실행 파일 경로",  "미입력 시 감시만 수행 (재시작 불가)", isExe: true,  autoFill: _erwekaProcessName);
        _erwekaArguments   = AddTextAt(tbl1, 4, "실행 인수 (선택)", "재시작 시 함께 전달할 명령행 인수 --port 9000 --config \"C:\\config\\tab.ini\"");
        _erwekaMaxRetry    = AddNumAt(tbl1, 6, "최대 재시작 횟수",  1, 10, "회");
        tbl1.ResumeLayout(true);

        // 탭 2: TabmachineIF — 동일 구조
        var (pg2, tbl2) = MakeTab("② TabmachineIF");
        AddRowStyles(tbl2, 42, 28, 42, 28, 42, 28, 52);
        tbl2.SuspendLayout();
        _tabProcessName = AddTextAt(tbl2, 0, "프로세스 이름 *",   "확장자(.exe) 없이 입력   예) TabmachineIF");
        _tabExePath     = AddBrowseAt(tbl2, 2, "실행 파일 경로 *", "재시작에 사용할 실행 파일 경로",         isExe: true, autoFill: _tabProcessName);
        _tabArguments   = AddTextAt(tbl2, 4, "실행 인수 (선택)", "재시작 시 함께 전달할 명령행 인수 --port 9000 --config \"C:\\config\\tab.ini\"");
        _tabMaxRetry    = AddNumAt(tbl2, 6, "최대 재시작 횟수",  1, 10, "회");
        tbl2.ResumeLayout(true);

        // 탭 3: PDF 폴더 — browse(0-1) + num(2) + num(3)
        var (pg3, tbl3) = MakeTab("③ PDF 폴더 감시");
        AddRowStyles(tbl3, 42, 28, 52, 52);
        tbl3.SuspendLayout();
        _pdfFolder     = AddBrowseAt(tbl3, 0, "PDF 폴더 경로",       "ERWEKA가 PDF를 저장하는 폴더   (비워두면 PDF 폴더 감시를 사용하지 않음)", isExe: false, autoFill: null);
        _pdfMaxIdle    = AddNumAt(tbl3, 2, "신규 파일 없음 경고 기준", 1, 1440, "분");
        _pdfMaxBacklog = AddNumAt(tbl3, 3, "누적 파일 수 경고 기준",   1, 9999, "개");
        tbl3.ResumeLayout(true);

        // 탭 4: 감시 주기 — num(0) + num(1)
        var (pg4, tbl4) = MakeTab("④ 감시 주기");
        AddRowStyles(tbl4, 52, 52);
        tbl4.SuspendLayout();
        _processCheckSec = AddNumAt(tbl4, 0, "프로세스 체크 주기",    10, 300, "초");
        _fileCheckMin    = AddNumAt(tbl4, 1, "파일 활동 체크 주기",    1,  60, "분");
        tbl4.ResumeLayout(true);

        tabs.TabPages.AddRange([pg1, pg2, pg3, pg4]);
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
                if (dlg.ShowDialog() == DialogResult.OK)
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
                if (dlg.ShowDialog() == DialogResult.OK)
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

        // NumericUpDown을 Panel로 감싸 Dock=Fill → Dock=Left 방식으로 고정 너비 유지
        var numPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 10) };
        var num = new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Font    = new Font("맑은 고딕", 9.5f),
            Width   = 110,
            Dock    = DockStyle.Left
        };
        numPanel.Controls.Add(num);
        grid.Controls.Add(numPanel, 1, row);

        var sfx = new Label
        {
            Text      = suffix,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font      = new Font("맑은 고딕", 9.5f),
            ForeColor = Color.FromArgb(70, 80, 100)
        };
        grid.Controls.Add(sfx, 2, row);

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
        _erwekaExePath.Text      = _config.Erweka.ExecutablePath;
        _erwekaArguments.Text    = _config.Erweka.Arguments;
        _erwekaMaxRetry.Value    = Math.Clamp(_config.Erweka.MaxRestartAttempts, 1, 10);

        _tabProcessName.Text     = _config.TabmachineIF.ProcessName;
        _tabExePath.Text         = _config.TabmachineIF.ExecutablePath;
        _tabArguments.Text       = _config.TabmachineIF.Arguments;
        _tabMaxRetry.Value       = Math.Clamp(_config.TabmachineIF.MaxRestartAttempts, 1, 10);

        _pdfFolder.Text          = _config.PdfFolder.Path;
        _pdfMaxIdle.Value        = Math.Clamp(_config.PdfFolder.MaxIdleMinutes, 1, 1440);
        _pdfMaxBacklog.Value     = Math.Clamp(_config.PdfFolder.MaxBacklogCount, 1, 9999);

        _processCheckSec.Value   = Math.Clamp(_config.Intervals.ProcessCheckSeconds, 10, 300);
        _fileCheckMin.Value      = Math.Clamp(_config.Intervals.FileActivityCheckMinutes, 1, 60);
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_tabProcessName.Text))
        {
            MessageBox.Show("TabmachineIF의 프로세스 이름은 필수입니다.",
                "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _config.Erweka.ProcessName        = _erwekaProcessName.Text.Trim();
        _config.Erweka.ExecutablePath     = _erwekaExePath.Text.Trim();
        _config.Erweka.Arguments          = _erwekaArguments.Text.Trim();
        _config.Erweka.MaxRestartAttempts = (int)_erwekaMaxRetry.Value;

        _config.TabmachineIF.ProcessName        = _tabProcessName.Text.Trim();
        _config.TabmachineIF.ExecutablePath     = _tabExePath.Text.Trim();
        _config.TabmachineIF.Arguments          = _tabArguments.Text.Trim();
        _config.TabmachineIF.MaxRestartAttempts = (int)_tabMaxRetry.Value;

        _config.PdfFolder.Path            = _pdfFolder.Text.Trim();
        _config.PdfFolder.MaxIdleMinutes  = (int)_pdfMaxIdle.Value;
        _config.PdfFolder.MaxBacklogCount = (int)_pdfMaxBacklog.Value;

        _config.Intervals.ProcessCheckSeconds      = (int)_processCheckSec.Value;
        _config.Intervals.FileActivityCheckMinutes = (int)_fileCheckMin.Value;

        ConfigManager.Save(_config);
        DialogResult = DialogResult.OK;
        Close();
    }
}
