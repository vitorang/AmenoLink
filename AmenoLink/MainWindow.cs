using AmenoLink.Interfaces.ProgramManager;
using Microsoft.Web.WebView2.WinForms;

namespace AmenoLink;

internal partial class MainWindow : Form
{
    private readonly IProgramManager processManager;
    private WebView2? webView;

    public MainWindow(IProgramManager processManager)
    {
        this.processManager = processManager;
        InitializeComponent();
        InitializeWebView();
    }

    private void InitializeWebView()
    {
        webView = new WebView2
        {
            Dock = DockStyle.Fill,
            Source = new Uri("about:blank")
        };

        Controls.Add(webView);
    }
}
