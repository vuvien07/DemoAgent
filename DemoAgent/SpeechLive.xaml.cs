using DemoAgent.Util;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Util;


namespace DemoAgent
{
    /// <summary>
    /// Interaction logic for SpeechLive.xaml
    /// </summary>
    public partial class SpeechLive : System.Windows.Controls.UserControl
    {
        private Models.Account account;
        private List<TextFile> files;

        public SpeechLive(Models.Account account)
        {
            InitializeComponent();
            this.account = account;
        }

        public class TextFile
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string Size { get; set; }
            public string Description { get; set; }
            public string Path { get; set; }
        }
        public List<TextFile> GetAllTextFilesInDirectory(string path)
        {
            List<TextFile> textFiles = new List<TextFile>();
            string[] files = Directory.GetFiles(path);
            try
            {
                foreach (string file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    if (fileInfo.Exists && fileInfo.Extension.Equals(".cnp", StringComparison.OrdinalIgnoreCase))
                    {
                        textFiles.Add(new TextFile
                        {
                            Name = fileInfo.Name,
                            Type = fileInfo.Extension,
                            Size = FormatFileSize(fileInfo.Length),
                            Description = fileInfo.CreationTime.ToString(),
                            Path = fileInfo.FullName
                        });
                    }
                }
            }
            catch (Exception e)
            {
                e.GetBaseException();
            }
            textFiles.Reverse();
            return textFiles;
        }
        public string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Load(null);
        }
        public void Load(string? name)
        {
            string directory = System.IO.Path.Combine(Environment.CurrentDirectory, "Transcription");
            if (!System.IO.File.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            List<TextFile> listTxt = GetAllTextFilesInDirectory(directory);
            if (!string.IsNullOrEmpty(name))
            {
                DateTime searchDate;
                bool isDateSearch = DateTime.TryParse(name, out searchDate);

                if (isDateSearch)
                {
                    listTxt = listTxt.Where(x => DateTime.Parse(x.Description).Date == searchDate.Date).ToList();
                }
                else
                {
                    listTxt = listTxt.Where(x => x.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
                }
            }
            lvTrans.ItemsSource = listTxt;
        }
        private void MenuItemOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var selectedFile = lvTrans.SelectedValue as TextFile;
            if (selectedFile != null)
            {
                try
                {
                    // Mở thư mục chứa file đã chọn
                    string directoryPath = System.IO.Path.GetDirectoryName(selectedFile.Path);
                    if (Directory.Exists(directoryPath))
                    {
                        var processStartInfo = new ProcessStartInfo
                        {
                            FileName = directoryPath,
                            UseShellExecute = true
                        };
                        Process.Start(processStartInfo);
                    }
                    else
                    {
                        EventUtil.printNotice("Thư mục không tồn tại!", MessageUtil.ERROR);
                    }
                }
                catch (Exception)
                {
                    EventUtil.printNotice("Đã xảy ra lỗi!", MessageUtil.ERROR);
                }
            }
        }
        private void MenuItemDeleteText_Click(object sender, RoutedEventArgs e)
        {
            var selectedFile = lvTrans.SelectedValue as TextFile;

            if (selectedFile != null)
            {
                try
                {
                    MessageBoxResult dialogResult = System.Windows.MessageBox.Show(
                        $"Are you sure to delete this file at {selectedFile.Path}?",
                        "Confirm",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        try
                        {
                            if (System.IO.File.Exists(selectedFile.Path))
                            {
                                string directory = System.IO.Path.Combine(Environment.CurrentDirectory, "Transcription");
                                if (!System.IO.File.Exists(directory))
                                {
                                    Directory.CreateDirectory(directory);
                                }
                                files = GetAllTextFilesInDirectory(directory);
                                System.IO.File.Delete(selectedFile.Path);
                                TextFile text = files.FirstOrDefault(f => f.Path == selectedFile.Path);
                                if (text != null)
                                {
                                    files.Remove(text);
                                }
                            }
                            Load(null);
                            EventUtil.printNotice($"Remove file with path {selectedFile.Path} successfully!", MessageUtil.SUCCESS);
                        }
                        catch (Exception ex)
                        {
                            EventUtil.printNotice("An error occurred: " + ex.Message, MessageUtil.ERROR);
                        }
                    }
                }
                catch (Exception ex)
                {
                    EventUtil.printNotice("An unexpected error occurred: " + ex.Message, MessageUtil.ERROR);
                }
            }
        }

        private void DecryptTextFile(object sender, RoutedEventArgs e)
        {
            var selectedFile = lvTrans.SelectedValue as TextFile;
            if (selectedFile != null)
            {
                string path = selectedFile.Path;
                if (path != null && System.IO.File.Exists(path))
                {
                    using (System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog())
                    {
                        if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            string folderPath = folderDialog.SelectedPath;
                            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                            string decryptedFilePath = System.IO.Path.Combine(folderPath, fileName + ".text");
                            try
                            {
                                UtilHelper.DecryptFile(path, decryptedFilePath, account.PrivateKey);
                                string decryptedContent = System.IO.File.ReadAllText(decryptedFilePath);

                                ResultTextBox.Text = decryptedContent;
                                EventUtil.printNotice($"The file has been successfully decrypted and saved at {folderPath}", MessageUtil.SUCCESS);
                            }
                            catch (Exception ex)
                            {
                                EventUtil.printNotice($"Error decrypting file: {ex.Message}", MessageUtil.ERROR);
                            }
                        }
                    }
                }
            }
        }

        private void searchTrans_TextChanged(object sender, TextChangedEventArgs e)
        {
            Load(searchTrans.Text);
        }
    }
}