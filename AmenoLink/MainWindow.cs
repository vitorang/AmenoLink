using System.Runtime.InteropServices;
using AmenoLink.Interfaces.ProgramManager;
using Microsoft.Web.WebView2.WinForms;

namespace AmenoLink;

internal partial class MainWindow : Form
{
    private readonly IProgramManager processManager;
    private WebView2? webView;

    [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public MainWindow(IProgramManager processManager)
    {
        this.processManager = processManager;
        InitializeComponent();
        EnableDarkModeTitleBar();
        InitializeWebView();
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
}
