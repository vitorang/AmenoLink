using System.Runtime.InteropServices;
using AmenoLink.Interfaces.Configurations;
using AmenoLink.Interfaces.ProgramManager;
using Microsoft.Web.WebView2.WinForms;

namespace AmenoLink;

internal partial class MainWindow : Form
{
    private readonly IProgramManager processManager;
    private readonly IConfigurationManager configurationManager;
    private WebView2? webView;
    private NotifyIcon? trayIcon;
    private ContextMenuStrip? trayMenu;
    private bool isExiting;
    private bool isInitialVisibleSet;

    [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public MainWindow(IProgramManager processManager, IConfigurationManager configurationManager)
    {
        this.processManager = processManager;
        this.configurationManager = configurationManager;
        InitializeComponent();
        EnableDarkModeTitleBar();
        InitializeWebView();
        InitializeTrayIcon();
    }

    protected override void SetVisibleCore(bool value)
    {
        if (!isInitialVisibleSet)
        {
            isInitialVisibleSet = true;
            if (configurationManager.General.StartMinimizedToTray)
            {
                value = false;
                ShowInTaskbar = false;
                if (!IsHandleCreated)
                    CreateHandle();
            }
        }
        base.SetVisibleCore(value);
    }

    private void EnableDarkModeTitleBar()
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            int useDarkMode = 1;
            DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
        }
    }

    private void InitializeWebView()
    {
        webView = new WebView2
        {
            Dock = DockStyle.Fill,
            Source = new Uri("http://localhost:13545/ameno-ui/")
        };

        Controls.Add(webView);
    }

    private void InitializeTrayIcon()
    {
        var openMenuItem = new ToolStripMenuItem("Exibir/Ocultar", null, (sender, e) => ToggleWindowVisibility());
        var exitMenuItem = new ToolStripMenuItem("Sair", null, (sender, e) => ExitApplication());

        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add(openMenuItem);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add(exitMenuItem);

        trayIcon = new NotifyIcon
        {
            Text = "AmenoLink",
            Icon = Icon ?? Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application,
            ContextMenuStrip = trayMenu,
            Visible = true
        };

        trayIcon.MouseClick += (sender, e) =>
        {
            if (e.Button == MouseButtons.Left)
                ToggleWindowVisibility();
        };
    }

    private void ToggleWindowVisibility()
    {
        if (Visible && WindowState != FormWindowState.Minimized)
            HideToTray();
        else
            RestoreFromTray();
    }

    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;
    }

    private void RestoreFromTray()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        isExiting = true;
        trayIcon?.Dispose();
        Application.Exit();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized)
            HideToTray();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!isExiting && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        trayIcon?.Dispose();
        base.OnFormClosing(e);
    }
}
