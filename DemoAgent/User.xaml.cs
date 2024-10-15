using HotelManagement.Util;
using Services;
using System;
using System.Windows;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Util;
using DemoAgent.Util;

namespace DemoAgent
{
    /// <summary>
    /// Interaction logic for User.xaml
    /// </summary>
    public partial class User : Window
    {
        public string publicKey { get; set; }
        public string privateKey { get; set; }
        private string selectedFilePath;


        public User(string publicKey, string privateKey)
        {
            InitializeComponent();
            this.publicKey = publicKey;
            this.privateKey = privateKey;
            EventUtil.PrintNotice -= MessageBoxUtil.PrintMessageBox;
            EventUtil.PrintNotice += MessageBoxUtil.PrintMessageBox;
        }



        private void ButtonOpenFile_Click1(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog
            {
                Multiselect = true,
                Filter = "WAV Files (*.wav)|*.wav",
                Title = "Chọn file WAV"
            };

            string filePath = openFileDialog.FileName;
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                selectedFilePath = openFileDialog.FileName;

                labelEncodeFileName.Content = $"File đã chọn: {selectedFilePath}";
                buttonEncrypt.Visibility = Visibility.Visible;

            }
        }

        private void ButtonOpenFile_Click2(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "CPN Files (*.cpn)|*.cpn", // Chỉnh sửa bộ lọc để chọn tệp .cpn
                Title = "Chọn file giải mã"
            };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                selectedFilePath = openFileDialog.FileName;

                labelDecodeFileName.Content = $"File đã chọn: {selectedFilePath}";
                buttonDecrypt.Visibility = Visibility.Visible;
            }
        }


        private void ButtonEncrypt_Click(object sender, RoutedEventArgs e)
        {
                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                {
                    if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string folderPath = folderDialog.SelectedPath;
                        string originalFileName = System.IO.Path.GetFileNameWithoutExtension(selectedFilePath);
                        // file theo thoi gian
                        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                        string encryptedFileName = $"{originalFileName}_{timestamp}.cpn";
                        string encryptedFilePath = System.IO.Path.Combine(folderPath, encryptedFileName);

                    // Mã hóa tệp WAV thành tệp CPN
                    //encryptionService.EncryptWavFile(selectedFilePath, encryptedFilePath, publicKey);
                    }
                }
        }


        private void ButtonDecrypt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                {
                    if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string folderPath = folderDialog.SelectedPath;
                        string originalFileName = System.IO.Path.GetFileNameWithoutExtension(selectedFilePath);

                        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                        string decryptedFileName = $"{originalFileName}_{timestamp}.wav";

                        string decryptedFilePath = System.IO.Path.Combine(folderPath, decryptedFileName);

                        // Giải mã tệp CPN thành tệp WAV
                        UtilHelper.DecryptFile(selectedFilePath, decryptedFilePath, privateKey);

                        // Hiển thị thông báo sau khi giải mã thành công
                        System.Windows.MessageBox.Show("Tệp đã được giải mã thành công và lưu tại " + decryptedFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxUtil.PrintMessageBox("An error occured!", MessageUtil.ERROR);
            }
        }

        private void NavigateToRecordAudio(object sender, RoutedEventArgs e)
        {
            //Record record = new Record();
            //this.Close();
        }
    }
}
