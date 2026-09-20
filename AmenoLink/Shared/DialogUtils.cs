namespace AmenoLink.Shared;

internal static class DialogUtils
{
    public static string? ShowOpenFileDialog(string filter, string title, string? currentPath = null)
    {
        string? selectedFile = null;

        var thread = new Thread(() =>
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = filter,
                Title = title
            };

            if (!string.IsNullOrWhiteSpace(currentPath))
            {
                string? directory = Path.GetDirectoryName(currentPath);
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                    openFileDialog.InitialDirectory = directory;
                else if (Directory.Exists(currentPath))
                    openFileDialog.InitialDirectory = currentPath;
            }

            if (openFileDialog.ShowDialog() == DialogResult.OK)
                selectedFile = openFileDialog.FileName?.Replace('\\', '/');
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        return selectedFile;
    }

    public static string? ShowFolderBrowserDialog(string title, string? currentPath = null)
    {
        string? selectedFolder = null;

        var thread = new Thread(() =>
        {
            using var folderBrowserDialog = new FolderBrowserDialog
            {
                Description = title,
                UseDescriptionForTitle = true
            };

            if (!string.IsNullOrWhiteSpace(currentPath) && Directory.Exists(currentPath))
                folderBrowserDialog.InitialDirectory = currentPath;

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                selectedFolder = folderBrowserDialog.SelectedPath?.Replace('\\', '/');
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        return selectedFolder;
    }

    public static void OpenInBrowser(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
}
