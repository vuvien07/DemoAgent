using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using Util;
using System.Windows;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Windows.Threading;
using Services;
using DemoAgent;
using Microsoft.Extensions.DependencyInjection;

namespace HotelManagement.Util
{
    public class MessageBoxUtil
    {
        public static void PrintMessageBox(string message, string title)
        {
            switch (title)
            {
                case "Success":
                    System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                case "Error":
                    System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }
    }
}
