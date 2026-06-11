using InterfaceWatchDog.Core;
using InterfaceWatchDog.Core.Actions;
using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.UI.Forms;

public class MainStatusForm : Form
{
    private readonly WatchDogEngine _engine;
    private readonly LogWriter      _log;
    private readonly AppConfig      _config;

    private StatusCard _erwekaCard = null!;
    private StatusCard _tabCard    = null!;

    private Panel  _pdfStatusBadge   = null!;
    private Label  _pdfStatusText    = null!;
    private Label  _pdfCountVal      = null!;
    private Label  _pdfLastVal       = null!;
    private Label  _pdfFolderVal     = null!;

    private ListBox _logList       = null!;
    private Label   _lastCheckLbl  = null!;

    public MainStatusForm(WatchDogEngine engine, LogWriter log, AppConfig config)
    {
        _engine = engine;
        _log    = log;
        _config = config;
        AutoScaleMode = AutoScaleMode.Dpi;
        InitializeComponent();
        SubscribeEvents();
        RefreshFromEngine();
    }

    // =========================================================================
    private void InitializeComponent()
    {
        Text            = "InterfaceWatchDog — 상태 대시보드";
        Size            = new Size(980, 782);
        MinimumSize     = new Size(860, 642);
        StartPosition   = FormStartPosition.CenterScreen;
        Font            = new Font("맑은 고딕", 9.5f);
        BackColor       = Color.FromArgb(242, 244, 248);

        // ── 헤더 ─────────────────────────────────────────────────────────────
        var header = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 60,
            BackColor = Color.FromArgb(28, 32, 40)
        };
        var hTitle = new Label
        {
            Text      = "Interface WatchDog",
            ForeColor = Color.White,
            Font      = new Font("맑은 고딕", 14f, FontStyle.Bold),
            Location  = new Point(18, 16),
            AutoSize  = true
        };
        var hSub = new Label
        {
            Text      = "인터페이스 프로그램 감시 시스템",
            ForeColor = Color.FromArgb(140, 148, 165),
            Font      = new Font("맑은 고딕", 9f),
            Location  = new Point(210, 22),
            AutoSize  = true
        };
        header.Controls.AddRange([hTitle, hSub]);

        // ── 프로그램 상태 카드 (2열) ─────────────────────────────────────────
        var cardOuter = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 230,
            Padding   = new Padding(14, 12, 14, 0),
            BackColor = Color.FromArgb(242, 244, 248)
        };
        var cardLayout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 1,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        cardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _erwekaCard = new StatusCard("ERWEKA Export Manager") { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 6, 0) };
        _tabCard    = new StatusCard("TabmachineIF")           { Dock = DockStyle.Fill, Margin = new Padding(6, 0, 0, 0) };

        cardLayout.Controls.Add(_erwekaCard, 0, 0);
        cardLayout.Controls.Add(_tabCard,    1, 0);
        cardOuter.Controls.Add(cardLayout);

        // ── PDF 폴더 상태 패널 ────────────────────────────────────────────────
        var pdfOuter = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 170,                              // 높이 확대 (텍스트 잘림 방지)
            Padding   = new Padding(16, 10, 16, 10),
            BackColor = Color.White
        };
        var pdfDivider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(220, 224, 232) };

        var pdfLayout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 4,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        pdfLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175));  // 열 너비 확대
        pdfLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 4; i++)
            pdfLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));     // 행 높이 확대 (텍스트 잘림 방지)

        Label MakePdfLabel(string t) => new Label
        {
            Text      = t,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font      = new Font("맑은 고딕", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(80, 90, 110)
        };
        Label MakePdfVal(string t = "") => new Label
        {
            Text      = t,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font      = new Font("맑은 고딕", 9f),
            ForeColor = Color.FromArgb(50, 55, 70)
        };

        // 행 0: 섹션 타이틀 + 상태 뱃지
        var pdfTitleLbl = MakePdfLabel("PDF 폴더 감시");
        pdfTitleLbl.Font = new Font("맑은 고딕", 9.5f, FontStyle.Bold);
        pdfTitleLbl.ForeColor = Color.FromArgb(40, 50, 70);

        _pdfStatusBadge = new Panel
        {
            Dock      = DockStyle.Fill,
            Padding   = new Padding(0, 4, 0, 4)
        };
        _pdfStatusText = new Label
        {
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font      = new Font("맑은 고딕", 9f),
            ForeColor = Color.Gray
        };
        _pdfStatusBadge.Controls.Add(_pdfStatusText);

        // 행 1–3: 상세 정보
        _pdfFolderVal = MakePdfVal();
        _pdfCountVal  = MakePdfVal();
        _pdfLastVal   = MakePdfVal();

        pdfLayout.Controls.Add(pdfTitleLbl,     0, 0);
        pdfLayout.Controls.Add(_pdfStatusBadge, 1, 0);
        pdfLayout.Controls.Add(MakePdfLabel("폴더 경로"),    0, 1);
        pdfLayout.Controls.Add(_pdfFolderVal,               1, 1);
        pdfLayout.Controls.Add(MakePdfLabel("현재 파일 수"), 0, 2);
        pdfLayout.Controls.Add(_pdfCountVal,                1, 2);
        pdfLayout.Controls.Add(MakePdfLabel("최신 파일"),   0, 3);
        pdfLayout.Controls.Add(_pdfLastVal,                 1, 3);

        pdfOuter.Controls.Add(pdfLayout);
        pdfOuter.Controls.Add(pdfDivider);

        // ── 실시간 로그 ───────────────────────────────────────────────────────
        var logOuter = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 8, 14, 6) };
        var logDivider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(220, 224, 232) };
        var logTitle = new Label
        {
            Text      = "실시간 로그",
            Dock      = DockStyle.Top,
            Height    = 26,
            Font      = new Font("맑은 고딕", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(40, 50, 70)
        };
        _logList = new ListBox
        {
            Dock               = DockStyle.Fill,
            Font               = new Font("Consolas", 9f),
            BackColor          = Color.FromArgb(24, 26, 32),
            ForeColor          = Color.FromArgb(195, 200, 215),
            BorderStyle        = BorderStyle.None,
            SelectionMode      = SelectionMode.None,
            HorizontalScrollbar = true,
            ItemHeight         = 18
        };
        logOuter.Controls.Add(_logList);
        logOuter.Controls.Add(logTitle);
        logOuter.Controls.Add(logDivider);

        // ── 하단 바 ───────────────────────────────────────────────────────────
        var bottomBar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 50,
            BackColor = Color.White,
            Padding   = new Padding(14, 7, 14, 7)
        };
        var bottomDivider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(220, 224, 232) };

        _lastCheckLbl = new Label
        {
            Text      = "",
            Dock      = DockStyle.Left,
            Width     = 260,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(130, 138, 155),
            Font      = new Font("맑은 고딕", 8.5f)
        };

        var btnRight = new FlowLayoutPanel
        {
            Dock          = DockStyle.Right,
            AutoSize      = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            Padding       = new Padding(0)
        };

        var btnLog = MakeBtn("로그 보기", Color.FromArgb(33, 150, 243));
        btnLog.Click += (_, _) => new LogViewerForm(_log).Show();

        var btnSettings = MakeBtn("설정", Color.FromArgb(80, 90, 110));
        btnSettings.Click += (_, _) => OpenSettings();

        btnRight.Controls.Add(btnLog);
        btnRight.Controls.Add(btnSettings);
        bottomBar.Controls.AddRange([bottomDivider, _lastCheckLbl, btnRight]);

        // ── 조립 ─────────────────────────────────────────────────────────────
        Controls.Add(logOuter);
        Controls.Add(pdfOuter);
        Controls.Add(cardOuter);
        Controls.Add(header);
        Controls.Add(bottomBar);
    }

    private static Button MakeBtn(string text, Color bg)
    {
        var b = new Button
        {
            Text      = text,
            Size      = new Size(100, 34),
            Margin    = new Padding(6, 0, 0, 0),
            BackColor = bg,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("맑은 고딕", 9f),
            Cursor    = Cursors.Hand
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    // =========================================================================
    private void SubscribeEvents()
    {
        _engine.ProgramStatusChanged += s =>
        {
            if (IsDisposed) return;
            try { Invoke(() => UpdateCard(s)); } catch { }
        };
        _engine.FileStatusChanged += fs =>
        {
            if (IsDisposed) return;
            try { Invoke(() => UpdatePdf(fs)); } catch { }
        };
        _log.LogGenerated += e =>
        {
            if (IsDisposed) return;
            try { Invoke(() => AppendLog(e)); } catch { }
        };
    }

    private void RefreshFromEngine()
    {
        var (e, t, f) = _engine.GetCurrentStatus();
        UpdateCard(e);
        UpdateCard(t);
        UpdatePdf(f);
    }

    private void UpdateCard(ProgramStatus s)
    {
        (s.Key == "Erweka" ? _erwekaCard : _tabCard).Update(s);
        _lastCheckLbl.Text = $"마지막 확인: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
    }

    private void UpdatePdf(FileActivityStatus s)
    {
        _pdfFolderVal.Text = _config.PdfFolder.Path;

        if (!s.IsFolderConfigured)
        {
            _pdfStatusText.Text      = "폴더 미설정";
            _pdfStatusText.ForeColor = Color.FromArgb(150, 158, 175);
            _pdfCountVal.Text        = "-";
            _pdfLastVal.Text         = "-";
            return;
        }

        if (s.IsBacklogWarning || s.IsIdleWarning)
        {
            _pdfStatusText.ForeColor = Color.FromArgb(230, 120, 20);
            _pdfStatusText.Font      = new Font("맑은 고딕", 9f, FontStyle.Bold);
        }
        else
        {
            _pdfStatusText.ForeColor = Color.FromArgb(40, 160, 80);
            _pdfStatusText.Font      = new Font("맑은 고딕", 9f);
        }

        _pdfStatusText.Text = s.StatusMessage;
        _pdfCountVal.Text   = $"{s.FileCount}개";
        _pdfLastVal.Text    = s.LastFileCreated.HasValue
            ? s.LastFileCreated.Value.ToString("yyyy-MM-dd HH:mm:ss")
            : "없음";
    }

    private void AppendLog(LogEntry entry)
    {
        _logList.Items.Add(entry.ToString());
        if (_logList.Items.Count > 300)
            _logList.Items.RemoveAt(0);
        _logList.TopIndex = _logList.Items.Count - 1;
    }

    private void OpenSettings()
    {
        var config = ConfigManager.Load();
        using var form = new SettingsForm(config);
        if (form.ShowDialog() == DialogResult.OK)
        {
            var newCfg = ConfigManager.Load();
            _engine.ReloadConfig(newCfg);
            _pdfFolderVal.Text = newCfg.PdfFolder.Path;
            _log.Info("UI", "설정 변경 — 감시 엔진 재적용됨");
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        { e.Cancel = true; Hide(); }
        else
            base.OnFormClosing(e);
    }
}

// =============================================================================
// 상태 카드 (DockStyle.Fill 지원)
// =============================================================================
internal class StatusCard : Panel
{
    private readonly Label _name;
    private readonly Label _dot;
    private readonly Label _statusText;
    private readonly Label _restartLbl;
    private readonly Label _lastSeenLbl;
    private readonly Label _msgLbl;

    public StatusCard(string programName)
    {
        BackColor   = Color.White;
        BorderStyle = BorderStyle.FixedSingle;
        Padding     = new Padding(16, 12, 16, 12);

        _name = new Label
        {
            Text         = programName,
            Dock         = DockStyle.Top,
            Height       = 24,
            Font         = new Font("맑은 고딕", 10f, FontStyle.Bold),
            ForeColor    = Color.FromArgb(40, 48, 65),
            AutoEllipsis = true
        };

        // 상태 행: ● 상태명
        var statusRow = new Panel { Dock = DockStyle.Top, Height = 44 };
        _dot = new Label
        {
            Text      = "●",
            Font      = new Font("맑은 고딕", 20f),
            ForeColor = Color.FromArgb(160, 168, 185),
            Location  = new Point(0, 6),
            Size      = new Size(34, 34)
        };
        _statusText = new Label
        {
            Text      = "확인 중",
            Font      = new Font("맑은 고딕", 13f, FontStyle.Bold),
            ForeColor = Color.FromArgb(160, 168, 185),
            Location  = new Point(38, 10),
            AutoSize  = true
        };
        statusRow.Controls.AddRange([_dot, _statusText]);

        var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(230, 232, 238) };

        var infoPanel = new Panel { Dock = DockStyle.Fill };

        _restartLbl = MakeInfo("재시작: 0회");
        _lastSeenLbl = MakeInfo("마지막 감지: —");
        _msgLbl      = new Label
        {
            Dock      = DockStyle.Fill,
            Font      = new Font("맑은 고딕", 8.5f),
            ForeColor = Color.FromArgb(130, 138, 155),
            TextAlign = ContentAlignment.TopLeft
        };

        infoPanel.Controls.Add(_msgLbl);
        infoPanel.Controls.Add(_lastSeenLbl);
        infoPanel.Controls.Add(_restartLbl);

        // 쌓는 순서: Fill은 마지막에
        Controls.Add(infoPanel);
        Controls.Add(divider);
        Controls.Add(statusRow);
        Controls.Add(_name);
    }

    private static Label MakeInfo(string t) => new Label
    {
        Text      = t,
        Dock      = DockStyle.Top,
        Height    = 22,
        Font      = new Font("맑은 고딕", 8.5f),
        ForeColor = Color.FromArgb(100, 108, 125),
        TextAlign = ContentAlignment.MiddleLeft
    };

    public void Update(ProgramStatus s)
    {
        _dot.ForeColor        = s.StatusColor;
        _statusText.Text      = s.StatusText;
        _statusText.ForeColor = s.StatusColor;
        _restartLbl.Text      = $"재시작: {s.RestartCount}회";
        _lastSeenLbl.Text     = s.LastSeenAlive.HasValue
            ? $"마지막 감지: {s.LastSeenAlive.Value:HH:mm:ss}"
            : "마지막 감지: —";
        _msgLbl.Text          = s.StatusMessage;
    }
}
