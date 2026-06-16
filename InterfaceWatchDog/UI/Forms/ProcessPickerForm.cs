using InterfaceWatchDog.Core;

namespace InterfaceWatchDog.UI.Forms;

// 현재 화면에 보이는 프로그램 창 목록에서 사용자가 직접 선택하도록 하는 대화상자.
// (사용자는 "javaw" 같은 실제 프로세스 이름이 아니라, 화면에 보이는 창 제목만 알고 있음)
public class ProcessPickerForm : Form
{
    private readonly ListBox _list;
    private readonly List<RunningProgramFinder.WindowInfo> _windows;

    public RunningProgramFinder.WindowInfo? Selected { get; private set; }

    public ProcessPickerForm()
    {
        Text            = "실행 중인 프로그램 선택";
        Size            = new Size(440, 440);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        Font            = new Font("맑은 고딕", 9.5f);

        _windows = RunningProgramFinder.GetVisibleWindows();

        var hint = new Label
        {
            Dock      = DockStyle.Top,
            Height    = 32,
            Text      = "감시할 프로그램의 창을 선택하세요.",
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(10, 0, 0, 0),
            ForeColor = Color.FromArgb(70, 80, 100)
        };

        _list = new ListBox { Dock = DockStyle.Fill, Font = new Font("맑은 고딕", 9.5f) };
        foreach (var w in _windows) _list.Items.Add(w.Title);
        _list.DoubleClick += (_, _) => Accept();

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10)
        };

        var btnCancel = new Button { Text = "취소", Size = new Size(90, 32) };
        btnCancel.Click += (_, _) => Close();

        var btnOk = new Button { Text = "선택", Size = new Size(90, 32) };
        btnOk.Click += (_, _) => Accept();

        btnPanel.Controls.Add(btnCancel);
        btnPanel.Controls.Add(btnOk);

        Controls.Add(_list);
        Controls.Add(btnPanel);
        Controls.Add(hint);
    }

    private void Accept()
    {
        if (_list.SelectedIndex < 0)
        {
            MessageBox.Show("프로그램을 선택하세요.", "InterfaceWatchDog", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Selected = _windows[_list.SelectedIndex];
        DialogResult = DialogResult.OK;
        Close();
    }
}
