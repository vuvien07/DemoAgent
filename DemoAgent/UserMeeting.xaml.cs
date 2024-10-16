using Microsoft.Extensions.DependencyInjection;
using Models;
using Repositories;
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

namespace DemoAgent
{
    /// <summary>
    /// Interaction logic for UserMeeting.xaml
    /// </summary>
    public partial class UserMeeting : UserControl
    {
        private Account account;
        public UserMeeting(Account account)
        {
            InitializeComponent();
            this.account = account;
            var meeting = DemoAgentContext.INSTANCE.Meetings.FirstOrDefault(x => x.StatusId == 3);
            if(meeting != null)
            {
                btMeeting.IsEnabled = false;

            }
        }
        private void btMeeting_Click(object sender, RoutedEventArgs e)
        {
           
                new NewMeeting(account).Show();
           
            
        }
        private void Load()
        {
            var meeting = MeetingDBContext.Instance.GetMeetingByCreator(account.Username) as Meeting;
            if (meeting != null)
            {
                lbCurrent.Content += $"{meeting.Name} ({meeting.TimeStart}-{meeting.TimeEnd})";
            }
            var historyMeetings = MeetingDBContext.Instance.GetHistoryMeetingsByCreator(account.Username);
            historyMeetings.Reverse();
            lvMeetings.ItemsSource = historyMeetings;
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Load();
        }
    }
}
