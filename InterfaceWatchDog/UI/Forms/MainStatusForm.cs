using InterfaceWatchDog.Core;
using InterfaceWatchDog.Core.Actions;
using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.UI.Forms;

public class MainStatusForm : Form
{
    private readonly WatchDogEngine _engine;
    private readonly LogWriter _log;
    private readonly AppConfig _config;

    // 상태 카드
    private StatusCard _erwekaCard = null!;
    private StatusCard _tabCard = null!;

    // PDF 상태
    private Label _pdfStatusLabel = null!;
    private Label _pdfFileCountLabel = null!;
    private Label _pdfLastFileLabel = null!;

    // 로그 테일
    private ListBox _logTail = null!;

    // 하단 상태바
    private Label _lastCheckLabel = null!;

    public MainStatusForm(WatchDogEngine engine, LogWriter log, AppConfig config)
    {
        _engine = engine;
        _log = log;
        _config = config;
        InitializeComponent();
        SubscribeEvents();
        RefreshFromEngine();
    }

    private void InitializeComponent()
    {
        Text = "InterfaceWatchDog - 상태 대시보드";
        Size = new Size(700, 580);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(600, 500);
        Font = new Font("맑은 고딕", 9f);
        BackColor = Color.FromArgb(245, 245, 245);

        // ── 헤더 ──────────────────────────────────────────────────────────────
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.FromArgb(33, 37, 43)
        };
        var headerTitle = new Label
        {
            Text = "Interface WatchDog",
            ForeColor = Color.White,
            Font = new Font("맑은 고딕", 13f, FontStyle.Bold),
            Location = new Point(16, 14),
            AutoSize = true
        };
        var headerSub = new Label
        {
            Text = "한올바이오파마 인터페이스 감시 시스템",
            ForeColor = Color.FromArgb(150, 150, 160),
            Font = new Font("맑은 고딕", 8.5f),
            Location = new Point(200, 18),
            AutoSize = true
        };
        header.Controls.AddRange([headerTitle, headerSub]);

        // ── 프로그램 상태 카드 ────────────────────────────────────────────────
        var cardPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 170,
            Padding = new Padding(12, 10, 12, 0),
            BackColor = Color.FromArgb(245, 245, 245)
        };
        _erwekaCard = new StatusCard("ERWEKA Export Manager") { Location = new Point(12, 10) };
        _tabCard = new StatusCard("TabmachineIF") { Location = new Point(350, 10) };
        cardPanel.Controls.AddRange([_erwekaCard, _tabCard]);

        // ── PDF 폴더 상태 ─────────────────────────────────────────────────────
        var pdfPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            Padding = new Padding(12, 4, 12, 4),
            BackColor = Color.White
        };
        var pdfTitle = new Label
        {
            Text = "PDF 폴더 상태",
            Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
            Location = new Point(12, 10),
            AutoSize = true,
            ForeColor = Color.FromArgb(60, 60, 60)
        };
        _pdfStatusLabel = new Label
        {
            Text = "확인 중...",
            Location = new Point(130, 10),
            Size = new Size(500, 18),
            ForeColor = Color.Gray
        };
        _pdfFileCountLabel = new Label
        {
            Text = "",
            Location = new Point(12, 34),
            Size = new Size(300, 18),
            ForeColor = Color.FromArgb(80, 80, 80)
        };
        _pdfLastFileLabel = new Label
        {
            Text = "",
            Location = new Point(12, 54),
            Size = new Size(500, 18),
            ForeColor = Color.FromArgb(80, 80, 80)
        };
        pdfPanel.Controls.AddRange([pdfTitle, _pdfStatusLabel, _pdfFileCountLabel, _pdfLastFileLabel]);

        // ── 실시간 로그 테일 ──────────────────────────────────────────────────
        var logPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 6, 12, 6) };
        var logTitle = new Label
        {
            Text = "실시간 로그",
            Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 22,
            ForeColor = Color.FromArgb(60, 60, 60)
        };
        _logTail = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 8.5f),
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(200, 200, 200),
            BorderStyle = BorderStyle.None,
            SelectionMode = SelectionMode.None,
            HorizontalScrollbar = true
        };
        logPanel.Controls.Add(_logTail);
        logPanel.Controls.Add(logTitle);

        // ── 하단 버튼 바 ──────────────────────────────────────────────────────
        var bottomBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            BackColor = Color.White,
            Padding = new Padding(8, 4, 8, 4)
        };
        _lastCheckLabel = new Label
        {
            Text = "",
            Location = new Point(10, 12),
            Size = new Size(350, 18),
            ForeColor = Color.Gray,
            Font = new Font("맑은 고딕", 8f)
        };
        var btnSettings = CreateButton("설정", 420, Color.FromArgb(100, 100, 100));
        btnSettings.Click += (_, _) => OpenSettings();

        var btnLog = CreateButton("로그 보기", 530, Color.FromArgb(33, 150, 243));
        btnLog.Click += (_, _) => new LogViewerForm(_log).Show();

        bottomBar.Controls.AddRange([_lastCheckLabel, btnSettings, btnLog]);

        Controls.Add(logPanel);
        Controls.Add(pdfPanel);
        Controls.Add(cardPanel);
        Controls.Add(header);
        Controls.Add(bottomBar);
    }

    private static Button CreateButton(string text, int x, Color color)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, 4),
            Size = new Size(100, 32),
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("맑은 고딕", 9f)
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private void SubscribeEvents()
    {
        _engine.ProgramStatusChanged += status =>
        {
            if (IsDisposed) return;
            Invoke(() => UpdateProgramStatus(status));
        };

        _engine.FileStatusChanged += fileStatus =>
        {
            if (IsDisposed) return;
            Invoke(() => UpdateFileStatus(fileStatus));
        };

        _log.LogGenerated += entry =>
        {
            if (IsDisposed) return;
            Invoke(() => AppendLog(entry));
        };
    }

    private void RefreshFromEngine()
    {
        var (erweka, tabmachine, file) = _engine.GetCurrentStatus();
        UpdateProgramStatus(erweka);
        UpdateProgramStatus(tabmachine);
        UpdateFileStatus(file);
    }

    private void UpdateProgramStatus(ProgramStatus status)
    {
        var card = status.Key == "Erweka" ? _erwekaCard : _tabCard;
        card.UpdateStatus(status);
        _lastCheckLabel.Text = $"마지막 확인: {DateTime.Now:HH:mm:ss}";
    }

    private void UpdateFileStatus(FileActivityStatus status)
    {
        if (!status.IsFolderConfigured)
        {
            _pdfStatusLabel.Text = "PDF 폴더 미설정";
            _pdfStatusLabel.ForeColor = Color.Gray;
            return;
        }

        if (status.IsBacklogWarning || status.IsIdleWarning)
        {
            _pdfStatusLabel.ForeColor = Color.FromArgb(255, 152, 0);
        }
        else
        {
            _pdfStatusLabel.ForeColor = Color.FromArgb(76, 175, 80);
        }

        _pdfStatusLabel.Text = status.StatusMessage;
        _pdfFileCountLabel.Text = $"  PDF 파일 수: {status.FileCount}개";
        _pdfLastFileLabel.Text = status.LastFileCreated.HasValue
            ? $"  마지막 생성: {status.LastFileCreated.Value:yyyy-MM-dd HH:mm:ss}"
            : "  마지막 생성: 없음";
    }

    private void AppendLog(LogEntry entry)
    {
        _logTail.Items.Add(entry.ToString());
        if (_logTail.Items.Count > 200)
            _logTail.Items.RemoveAt(0);
        _logTail.TopIndex = _logTail.Items.Count - 1;
    }

    private void OpenSettings()
    {
        var config = ConfigManager.Load();
        using var form = new SettingsForm(config);
        if (form.ShowDialog() == DialogResult.OK)
        {
            _engine.ReloadConfig(ConfigManager.Load());
            _log.Info("UI", "설정이 변경되어 감시 엔진에 적용됨");
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            base.OnFormClosing(e);
        }
    }
}

// ─── 상태 카드 컨트롤 ─────────────────────────────────────────────────────────

internal class StatusCard : Panel
{
    private readonly Label _titleLabel;
    private readonly Label _statusDot;
    private readonly Label _statusLabel;
    private readonly Label _restartCountLabel;
    private readonly Label _lastSeenLabel;
    private readonly Label _messageLabel;

    public StatusCard(string programName)
    {
        Size = new Size(316, 148);
        BackColor = Color.White;
        BorderStyle = BorderStyle.FixedSingle;

        _titleLabel = new Label
        {
            Text = programName,
            Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
            Location = new Point(12, 12),
            Size = new Size(250, 18),
            ForeColor = Color.FromArgb(40, 40, 40)
        };

        _statusDot = new Label
        {
            Text = "●",
            Font = new Font("맑은 고딕", 16f),
            Location = new Point(12, 34),
            Size = new Size(28, 28),
            ForeColor = Color.Gray
        };

        _statusLabel = new Label
        {
            Text = "확인 중",
            Font = new Font("맑은 고딕", 11f, FontStyle.Bold),
            Location = new Point(44, 38),
            AutoSize = true,
            ForeColor = Color.Gray
        };

        _restartCountLabel = new Label
        {
            Text = "재시작: 0회",
            Location = new Point(12, 72),
            Size = new Size(280, 16),
            ForeColor = Color.FromArgb(100, 100, 100),
            Font = new Font("맑은 고딕", 8.5f)
        };

        _lastSeenLabel = new Label
        {
            Text = "마지막 확인: -",
            Location = new Point(12, 92),
            Size = new Size(280, 16),
            ForeColor = Color.FromArgb(100, 100, 100),
            Font = new Font("맑은 고딕", 8.5f)
        };

        _messageLabel = new Label
        {
            Text = "",
            Location = new Point(12, 112),
            Size = new Size(280, 28),
            ForeColor = Color.FromArgb(120, 120, 120),
            Font = new Font("맑은 고딕", 8f)
        };

        Controls.AddRange([_titleLabel, _statusDot, _statusLabel,
                           _restartCountLabel, _lastSeenLabel, _messageLabel]);
    }

    public void UpdateStatus(ProgramStatus status)
    {
        _statusDot.ForeColor = status.StatusColor;
        _statusLabel.Text = status.StatusText;
        _statusLabel.ForeColor = status.StatusColor;
        _restartCountLabel.Text = $"재시작: {status.RestartCount}회";
        _lastSeenLabel.Text = status.LastSeenAlive.HasValue
            ? $"마지막 감지: {status.LastSeenAlive.Value:HH:mm:ss}"
            : "마지막 감지: -";
        _messageLabel.Text = status.StatusMessage;
    }
}
