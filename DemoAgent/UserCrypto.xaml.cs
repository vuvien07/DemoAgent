using DemoAgent.Util;
using HotelManagement.Util;
using Models;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Util;

namespace DemoAgent
{
    /// <summary>
    /// Interaction logic for UserCrypto.xaml
    /// </summary>
    public partial class UserCrypto : UserControl
    {
        public Account account;

        public UserCrypto(Account account)
        {
            InitializeComponent();
            this.account = account;
            EventUtil.PrintNotice -= MessageBoxUtil.PrintMessageBox;
            EventUtil.PrintNotice += MessageBoxUtil.PrintMessageBox;
        }

        private void ButtonOpenFile_Click1(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "WAV Files (*.wav)|*.wav",
                Title = "Chọn file WAV",
                Multiselect = true // Cho phép chọn nhiều file
            };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                listBoxEncodeFiles.Items.Clear();
                foreach (string filePath in openFileDialog.FileNames)
                {
                    listBoxEncodeFiles.Items.Add(filePath);
                }
                buttonEncrypt.Visibility = Visibility.Visible;
            }
        }

        private void ButtonOpenFile_Click2(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "CNP Files (*.cnp)|*.cnp",
                Title = "Chọn file CNP",
                Multiselect = true // Cho phép chọn nhiều file
            };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)

            {
                listBoxDecodeFiles.Items.Clear();
                foreach (string filePath in openFileDialog.FileNames)
                {
                    listBoxDecodeFiles.Items.Add(filePath);
                }
                buttonDecrypt.Visibility = Visibility.Visible;
            }
        }

        private void ButtonEncrypt_Click(object sender, RoutedEventArgs e)
        {

            var fileNames = new List<string>();
            foreach (string filePath in listBoxEncodeFiles.Items)
            {
                // Lấy tên file từ đường dẫn đầy đủ
                string fileName = System.IO.Path.GetFileName(filePath);
                fileNames.Add(fileName);
            }

            using (System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string folderPath = folderDialog.SelectedPath;

                    foreach (string filePath in listBoxEncodeFiles.Items)
                    {
                        // Lấy tên file mà không có phần mở rộng
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                        fileName = fileName + "_" + timestamp + account.Username;
                        string encryptedFilePath = System.IO.Path.Combine(folderPath, fileName + ".cnp");
                        try
                        {
                            UtilHelper.EncryptFile(filePath, encryptedFilePath, account.PublicKey);
                            EventUtil.printNotice("Tệp đã được mã hóa thành công và lưu tại " + encryptedFilePath, MessageUtil.SUCCESS);
                        }
                        catch (Exception ex)
                        {
                            EventUtil.printNotice("An error occured " + encryptedFilePath, MessageUtil.ERROR);
                        }
                    }
                }
            }

        }

        private void ButtonDecrypt_Click(object sender, RoutedEventArgs e)
        {
            var fileNames = new List<string>();
            foreach (string filePath in listBoxEncodeFiles.Items)
            {
                // Lấy tên file từ đường dẫn đầy đủ
                string fileName = System.IO.Path.GetFileName(filePath);
                fileNames.Add(fileName);
            }
            using (System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string folderPath = folderDialog.SelectedPath;

                    foreach (string filePath in listBoxDecodeFiles.Items)
                    {
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);

                        string decryptedFilePath = System.IO.Path.Combine(folderPath, fileName + ".wav");
                        try
                        {
                            UtilHelper.DecryptFile(filePath, decryptedFilePath, account.PrivateKey);
                            EventUtil.printNotice("Tệp đã được giải mã thành công và lưu tại " + decryptedFilePath, MessageUtil.SUCCESS);
                        }
                        catch (Exception ex)
                        {
                            EventUtil.printNotice("An error occured " + decryptedFilePath, MessageUtil.ERROR);
                        }
                    }
                }
            }
        }
    }
}
