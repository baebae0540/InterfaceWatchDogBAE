using System.Drawing.Drawing2D;
using System.Drawing.Text;
using InterfaceWatchDog.Core.Actions;
using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.UI.Forms;

public class LogViewerForm : Form
{
    private readonly LogWriter _log;
    private CalPanel          _calPanel       = null!;
    private ListView          _logList        = null!;
    private HashSet<DateTime> _availableDates = [];
    private DateTime          _lastValidDate  = DateTime.Today;
    private DateTime          _calMonth;
    private DateTime          _hoverDate      = DateTime.MinValue;
    private Rectangle         _prevRect;
    private Rectangle         _nextRect;
    private Rectangle         _monthPickRect;
    private bool              _inMonthPick;
    private int               _pickYear;
    private int               _hoverMonth;   // 1-12 (month picker), 0 = none

    // ── 날짜 그리드 레이아웃 ──────────────────────────────────────────────────
    private const int CW       = 30;
    private const int CH       = 30;
    private const int PAD      = 12;
    private const int HDR_H    = 32;
    private const int GAP1     = 4;
    private const int DOW_H    = 22;
    private const int GAP2     = 2;
    private const int GRID_TOP = PAD + HDR_H + GAP1 + DOW_H + GAP2; // 72
    private const int CAL_W    = PAD * 2 + 7 * CW;                   // 234

    // ── 월 선택기 레이아웃 ────────────────────────────────────────────────────
    private const int MCW    = 52;
    private const int MCH    = 36;
    private const int MP_TOP = PAD + HDR_H + GAP1;                    // 48

    // ── 폰트 ─────────────────────────────────────────────────────────────────
    private readonly Font         _fNav = new("맑은 고딕", 13f, FontStyle.Bold);
    private readonly Font         _fHdr = new("맑은 고딕", 9.5f, FontStyle.Bold);
    private readonly Font         _fDow = new("맑은 고딕", 8f);
    private readonly Font         _fDay = new("맑은 고딕", 9.5f);
    private readonly StringFormat _sf   = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

    // ── 색상 팔레트 ───────────────────────────────────────────────────────────
    private static readonly Color CHdrText    = Color.FromArgb(26,  29,  46);
    private static readonly Color CDowText    = Color.FromArgb(140, 144, 165);
    private static readonly Color CSepLine    = Color.FromArgb(232, 234, 242);
    private static readonly Color CNavBtn     = Color.FromArgb(90,  96,  120);

    private static readonly Color CLogBg      = Color.FromArgb(219, 234, 254);  // blue-100
    private static readonly Color CLogHoverBg = Color.FromArgb(191, 219, 254);  // blue-200
    private static readonly Color CLogFg      = Color.FromArgb(29,  78,  216);  // blue-700
    private static readonly Color CSelBg      = Color.FromArgb(37,  99,  235);  // blue-600
    private static readonly Color CSelFg      = Color.White;

    private static readonly Color CTodayBg    = Color.FromArgb(220, 38,  38);   // red-600 (로그 있음)
    private static readonly Color CTodayFg    = Color.White;
    private static readonly Color CTodayNoBg  = Color.FromArgb(254, 226, 226);  // red-100 (로그 없음)
    private static readonly Color CTodayNoFg  = Color.FromArgb(185, 28,  28);   // red-700

    private static readonly Color CNoLogFg    = Color.FromArgb(195, 198, 215);
    private static readonly Color COtherFg    = Color.FromArgb(220, 222, 232);

    public LogViewerForm(LogWriter log)
    {
        _log      = log;
        _calMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _pickYear = _calMonth.Year;
        AutoScaleMode = AutoScaleMode.Dpi;
        InitializeComponent();
        LoadDates();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fNav.Dispose(); _fHdr.Dispose(); _fDow.Dispose();
            _fDay.Dispose(); _sf.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        Text          = "InterfaceWatchDog — 로그 뷰어";
        Size          = new Size(1100, 652);
        MinimumSize   = new Size(900, 492);
        StartPosition = FormStartPosition.CenterScreen;
        Font          = new Font("맑은 고딕", 9.5f);
        BackColor     = Color.FromArgb(245, 246, 250);

        // ── 툴바 ─────────────────────────────────────────────────────────────
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.White, Padding = new Padding(14, 10, 14, 10) };
        toolbar.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(215, 218, 228) });

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        var btnRefresh = ToolBtn("새로고침");
        btnRefresh.Click += (_, _) => LoadDates();
        var btnFolder = ToolBtn("폴더 열기");
        btnFolder.Click += (_, _) =>
        {
            if (Directory.Exists(LogWriter.LogDirectoryPath))
                System.Diagnostics.Process.Start("explorer.exe", LogWriter.LogDirectoryPath);
        };
        flow.Controls.AddRange([btnRefresh, btnFolder]);
        toolbar.Controls.Add(flow);

        // ── 달력 패널 (owner-draw) ────────────────────────────────────────────
        _calPanel = new CalPanel { Dock = DockStyle.Left, Width = CAL_W + 1, BackColor = Color.White };
        _calPanel.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Color.FromArgb(215, 218, 228) });
        _calPanel.Paint      += OnCalPaint;
        _calPanel.MouseClick += OnCalClick;
        _calPanel.MouseMove  += OnCalMouseMove;
        _calPanel.MouseLeave += (_, _) =>
        {
            bool need = _hoverDate != DateTime.MinValue || _hoverMonth != 0;
            _hoverDate  = DateTime.MinValue;
            _hoverMonth = 0;
            _calPanel.Cursor = Cursors.Default;
            if (need) _calPanel.Invalidate();
        };

        // ── 로그 목록 ─────────────────────────────────────────────────────────
        _logList = new ListView
        {
            Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true,
            GridLines = false, HeaderStyle = ColumnHeaderStyle.Nonclickable,
            Font = new Font("Consolas", 9f),
            BackColor = Color.FromArgb(22, 24, 30), ForeColor = Color.FromArgb(195, 200, 215),
            BorderStyle = BorderStyle.None
        };
        _logList.Columns.Add("시간",   140);
        _logList.Columns.Add("레벨",    62);
        _logList.Columns.Add("소스",   170);
        _logList.Columns.Add("메시지", 620);

        Controls.Add(_logList);
        Controls.Add(_calPanel);
        Controls.Add(toolbar);

        Shown += (_, _) =>
        {
            if (_logList.Items.Count > 0)
                _logList.EnsureVisible(_logList.Items.Count - 1);
        };
    }

    // ── 페인트 ───────────────────────────────────────────────────────────────

    private void OnCalPaint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.SmoothingMode     = SmoothingMode.AntiAlias;

        int w = _calPanel.ClientSize.Width - 1;

        // 헤더 (공통)
        _prevRect      = new Rectangle(6, PAD, 28, HDR_H);
        _nextRect      = new Rectangle(w - 34, PAD, 28, HDR_H);
        _monthPickRect = new Rectangle(_prevRect.Right, PAD, _nextRect.Left - _prevRect.Right, HDR_H);

        using (var b = new SolidBrush(CNavBtn))
        {
            g.DrawString("<", _fNav, b, _prevRect, _sf);
            g.DrawString(">", _fNav, b, _nextRect, _sf);
        }

        string hdrText = _inMonthPick
            ? $"{_pickYear}년  ✕"            // 클릭하면 취소
            : $"{_calMonth:yyyy년 M월}  ▾";  // 클릭하면 월 선택기 열기
        using (var b = new SolidBrush(CHdrText))
            g.DrawString(hdrText, _fHdr, b, _monthPickRect, _sf);

        if (_inMonthPick) DrawMonthPicker(g, w);
        else              DrawDayView(g, w);
    }

    private void DrawDayView(Graphics g, int w)
    {
        int gridX = (w - 7 * CW) / 2;
        int dowY  = PAD + HDR_H + GAP1;

        // 요일 헤더
        string[] dows = ["일", "월", "화", "수", "목", "금", "토"];
        using (var b = new SolidBrush(CDowText))
        {
            for (int i = 0; i < 7; i++)
                g.DrawString(dows[i], _fDow, b, new Rectangle(gridX + i * CW, dowY, CW, DOW_H), _sf);
        }

        using (var p = new Pen(CSepLine))
            g.DrawLine(p, gridX, dowY + DOW_H + 1, gridX + 7 * CW, dowY + DOW_H + 1);

        // 날짜 셀
        var firstDay = new DateTime(_calMonth.Year, _calMonth.Month, 1);
        int startDow = (int)firstDay.DayOfWeek;
        var selDate  = _lastValidDate.Date;

        for (int i = 0; i < 42; i++)
        {
            var  date    = firstDay.AddDays(i - startDow);
            bool isCur   = date.Month == _calMonth.Month;
            bool hasLog  = _availableDates.Contains(date.Date);
            bool isSel   = isCur && hasLog && date.Date == selDate;
            bool isHov   = isCur && hasLog && !isSel && date.Date == _hoverDate;
            bool isToday = date.Date == DateTime.Today;

            int col  = i % 7;
            int row  = i / 7;
            var cell = new Rectangle(gridX + col * CW, GRID_TOP + row * CH, CW, CH);
            var dot  = new Rectangle(cell.X + 3, cell.Y + 3, CW - 6, CH - 6);

            // 배경 원
            Color? bg = isSel                      ? CSelBg
                      : isToday && isCur && hasLog  ? CTodayBg
                      : isToday && isCur            ? CTodayNoBg
                      : isHov                       ? CLogHoverBg
                      : hasLog && isCur             ? CLogBg
                                                    : (Color?)null;
            if (bg.HasValue)
            {
                using var b = new SolidBrush(bg.Value);
                g.FillEllipse(b, dot);
            }

            // 오늘 + 선택: 파란 원 바깥에 빨간 링
            if (isSel && isToday && isCur)
            {
                var ring = new Rectangle(dot.X - 2, dot.Y - 2, dot.Width + 4, dot.Height + 4);
                using var p = new Pen(CTodayBg, 1.5f);
                g.DrawEllipse(p, ring);
            }

            // 텍스트
            Color fg = isSel                      ? CSelFg
                     : isToday && isCur && hasLog  ? CTodayFg
                     : isToday && isCur            ? CTodayNoFg
                     : !isCur                      ? COtherFg
                     : hasLog                      ? CLogFg
                                                   : CNoLogFg;
            using var tb = new SolidBrush(fg);
            g.DrawString(date.Day.ToString(), _fDay, tb, cell, _sf);
        }
    }

    private void DrawMonthPicker(Graphics g, int w)
    {
        int gridX = (w - 4 * MCW) / 2;
        string[] names = ["1월","2월","3월","4월","5월","6월","7월","8월","9월","10월","11월","12월"];

        for (int m = 1; m <= 12; m++)
        {
            int col  = (m - 1) % 4;
            int row  = (m - 1) / 4;
            var cell  = new Rectangle(gridX + col * MCW, MP_TOP + row * MCH, MCW, MCH);
            var inner = new Rectangle(cell.X + 4, cell.Y + 4, cell.Width - 8, cell.Height - 8);

            bool isCurMonth = _pickYear == _calMonth.Year && m == _calMonth.Month;
            bool hasLog     = _availableDates.Any(d => d.Year == _pickYear && d.Month == m);
            bool isHov      = m == _hoverMonth;

            Color? bg = isCurMonth ? CSelBg
                      : isHov      ? CLogHoverBg
                      : hasLog     ? CLogBg
                                   : (Color?)null;
            if (bg.HasValue)
            {
                using var b    = new SolidBrush(bg.Value);
                using var path = RoundedRect(inner, 8);
                g.FillPath(b, path);
            }

            Color fg = isCurMonth ? CSelFg : hasLog ? CLogFg : CNoLogFg;
            using var tb = new SolidBrush(fg);
            g.DrawString(names[m - 1], _fDay, tb, cell, _sf);
        }
    }

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d    = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X,          r.Y,           d, d, 180, 90);
        path.AddArc(r.Right - d,  r.Y,           d, d, 270, 90);
        path.AddArc(r.Right - d,  r.Bottom - d,  d, d,   0, 90);
        path.AddArc(r.X,          r.Bottom - d,  d, d,  90, 90);
        path.CloseFigure();
        return path;
    }

    // ── 클릭 ─────────────────────────────────────────────────────────────────

    private void OnCalClick(object? sender, MouseEventArgs e)
    {
        if (_inMonthPick) { HandleMonthPickClick(e); return; }

        if (_prevRect.Contains(e.Location))      { _calMonth = _calMonth.AddMonths(-1); _calPanel.Invalidate(); return; }
        if (_nextRect.Contains(e.Location))      { _calMonth = _calMonth.AddMonths(1);  _calPanel.Invalidate(); return; }
        if (_monthPickRect.Contains(e.Location)) { _inMonthPick = true; _pickYear = _calMonth.Year; _hoverMonth = 0; _calPanel.Invalidate(); return; }

        var date = HitTestDay(e.Location);
        if (date == DateTime.MinValue) return;
        _lastValidDate = date;
        _calPanel.Invalidate();
        LoadLogs();
    }

    private void HandleMonthPickClick(MouseEventArgs e)
    {
        if (_prevRect.Contains(e.Location))      { _pickYear--; _calPanel.Invalidate(); return; }
        if (_nextRect.Contains(e.Location))      { _pickYear++; _calPanel.Invalidate(); return; }
        if (_monthPickRect.Contains(e.Location)) { _inMonthPick = false; _calPanel.Invalidate(); return; }  // 취소

        int w     = _calPanel.ClientSize.Width - 1;
        int gridX = (w - 4 * MCW) / 2;
        int relX  = e.X - gridX;
        int relY  = e.Y - MP_TOP;
        if (relX < 0 || relY < 0) return;
        int col = relX / MCW;
        int row = relY / MCH;
        if (col > 3 || row > 2) return;

        _calMonth    = new DateTime(_pickYear, row * 4 + col + 1, 1);
        _inMonthPick = false;
        _hoverMonth  = 0;
        _calPanel.Invalidate();
    }

    // ── 마우스 이동 ───────────────────────────────────────────────────────────

    private void OnCalMouseMove(object? sender, MouseEventArgs e)
    {
        bool onNav = _prevRect.Contains(e.Location) || _nextRect.Contains(e.Location);
        bool onHdr = _monthPickRect.Contains(e.Location);

        if (_inMonthPick)
        {
            int w     = _calPanel.ClientSize.Width - 1;
            int gridX = (w - 4 * MCW) / 2;
            int relX  = e.X - gridX;
            int relY  = e.Y - MP_TOP;
            int hm    = (relX >= 0 && relY >= 0 && relX / MCW <= 3 && relY / MCH <= 2)
                        ? (relY / MCH) * 4 + (relX / MCW) + 1 : 0;

            _calPanel.Cursor = (onNav || onHdr || hm > 0) ? Cursors.Hand : Cursors.Default;
            if (hm != _hoverMonth) { _hoverMonth = hm; _calPanel.Invalidate(); }
            return;
        }

        var hover = (onNav || onHdr) ? DateTime.MinValue : HitTestDay(e.Location);
        _calPanel.Cursor = (onNav || onHdr || hover != DateTime.MinValue) ? Cursors.Hand : Cursors.Default;
        if (hover != _hoverDate) { _hoverDate = hover; _calPanel.Invalidate(); }
    }

    // ── 히트 테스트 ──────────────────────────────────────────────────────────

    private DateTime HitTestDay(Point pt)
    {
        int w     = _calPanel.ClientSize.Width - 1;
        int gridX = (w - 7 * CW) / 2;
        int relX  = pt.X - gridX;
        int relY  = pt.Y - GRID_TOP;
        if (relX < 0 || relY < 0) return DateTime.MinValue;
        int col = relX / CW;
        int row = relY / CH;
        if (col > 6 || row > 5) return DateTime.MinValue;

        var firstDay = new DateTime(_calMonth.Year, _calMonth.Month, 1);
        var date     = firstDay.AddDays(row * 7 + col - (int)firstDay.DayOfWeek);

        return (date.Month == _calMonth.Month && _availableDates.Contains(date.Date))
            ? date.Date : DateTime.MinValue;
    }

    // ── 유틸 ─────────────────────────────────────────────────────────────────

    private static Button ToolBtn(string text) => new()
    {
        Text = text, Size = new Size(100, 32), FlatStyle = FlatStyle.Flat,
        Font = new Font("맑은 고딕", 9f), BackColor = Color.FromArgb(232, 235, 242),
        ForeColor = Color.FromArgb(46, 52, 72), Cursor = Cursors.Hand,
        Margin = new Padding(0, 0, 8, 0)
    };

    private void LoadDates()
    {
        _availableDates = _log.GetAvailableLogDates()
            .Select(d => DateTime.ParseExact(d, "yyyy-MM-dd", null).Date)
            .ToHashSet();

        if (_availableDates.Count > 0)
        {
            _lastValidDate = _availableDates.Max();
            _calMonth      = new DateTime(_lastValidDate.Year, _lastValidDate.Month, 1);
            _pickYear      = _calMonth.Year;
        }

        _inMonthPick = false;
        _calPanel.Invalidate();
        LoadLogs();
    }

    private void LoadLogs()
    {
        _logList.Items.Clear();

        var date = _lastValidDate.Date;
        if (!_availableDates.Contains(date)) return;

        foreach (var entry in _log.ReadLogsByDate(date.ToString("yyyy-MM-dd")))
        {
            var item = new ListViewItem(entry.Timestamp.ToString("HH:mm:ss.fff"));
            item.SubItems.Add(entry.LevelText);
            item.SubItems.Add(entry.Source);
            item.SubItems.Add(entry.Message);
            item.ForeColor = entry.LevelColor;
            _logList.Items.Add(item);
        }

        if (_logList.Items.Count > 0)
            _logList.EnsureVisible(_logList.Items.Count - 1);
    }

    // 더블버퍼 패널 — MouseMove Invalidate 시 깜빡임 방지
    private sealed class CalPanel : Panel
    {
        public CalPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint           |
                     ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();
        }
    }
}
