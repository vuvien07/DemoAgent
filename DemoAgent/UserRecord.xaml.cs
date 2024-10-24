using DemoAgent.Util;
using Microsoft.Identity.Client.NativeInterop;
using Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Util;
using static System.Net.WebRequestMethods;

namespace DemoAgent
{
    /// <summary>
    /// Interaction logic for UserRecord.xaml
    /// </summary>
    public partial class UserRecord : System.Windows.Controls.UserControl
    {
        private List<WavFile> files;
        private Models.Account account;
        private App app = (App)System.Windows.Application.Current;
        public UserRecord(Models.Account account)
        {
            InitializeComponent();
            this.account = account;
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedFile = lvRecordings.SelectedValue as WavFile;
            if (selectedFile != null)
            {
                try
                {
                    DialogResult dialogResult = System.Windows.Forms.MessageBox.Show($"Are you sure to delete this file at {selectedFile.Path}?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (dialogResult == DialogResult.Yes)
                    {
                        (app.currWindow as UserContainer)._recordInstance.RemoveWavFile(files, selectedFile.Path);
                        LoadFiles(null);
                    }
                }
                catch (Exception) { }
            }
        }

        private void MenuItemOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var selectedFile = lvRecordings.SelectedValue as WavFile;
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

        private void searchRecord_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadFiles(searchRecord.Text);
        }

        private void LvRecordings_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3);

            e.Handled = true;
        }

        private void LoadFiles(string? name)
        {
            string directory = System.IO.Path.Combine(Environment.CurrentDirectory, "Recording");
            if (!System.IO.File.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            files = (app.currWindow as UserContainer)._recordInstance.GetAllWaveFilesInDirectory(directory);
            if (!string.IsNullOrEmpty(name))
            {
                DateTime searchDate;
                bool isDateSearch = DateTime.TryParse(name, out searchDate);

                if (isDateSearch)
                {
                    files = files.Where(x => DateTime.Parse(x.Description).Date == searchDate.Date).ToList();
                }
                else
                {
                    files = files.Where(x => x.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
                }
            }
            lvRecordings.ItemsSource = files;
        }



        private void DecryptWavFile(object sender, RoutedEventArgs e)
        {
            var selectedFile = lvRecordings.SelectedValue as WavFile;
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
                            string decryptedFilePath = System.IO.Path.Combine(folderPath, fileName + ".wav");

                            try
                            {
                                UtilHelper.DecryptFile(path, decryptedFilePath, account.PrivateKey);
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadFiles(null);
        }
    }
}
