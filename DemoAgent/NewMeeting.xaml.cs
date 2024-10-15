using DemoAgent.Util;
using HotelManagement.Util;
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
using System.Windows.Shapes;
using Util;

namespace DemoAgent
{
    /// <summary>
    /// Interaction logic for NewMetting.xaml
    /// </summary>
    public partial class NewMeeting : Window
    {
        private Account account;
        private readonly RecordService? recordService;

        public NewMeeting(Account account)
        {
            InitializeComponent();
            this.account = account;
            if(recordService == null)
            {
                recordService = RecordService.Instance;
                recordService.InitializeService(account);
            }
            EventUtil.PrintNotice -= MessageBoxUtil.PrintMessageBox;
            EventUtil.PrintNotice += MessageBoxUtil.PrintMessageBox;
        }

        private void btSaveMeeting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Meeting newMeeting = new()
                {
                    Name = txMeeting.Text,
                    Description = txDescription.Text,
                    TimeEnd = (DateTime)UtilHelper.TryParseDate(dtTimeEnd.Text)
                };

                if (ValidateMeetingInfo(newMeeting))
                {
                    if (!recordService.IsRecording())
                    {
                        CreateNewMeeting(newMeeting);
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window != this)
                            {
                                window.Closing -= App.Closing_Window;
                                window.Close();
                            }
                        }
                        var userContainer = new UserContainer(account);
                        userContainer.RaiseEvent();
                        this.Close();
                    }
                    else
                    {
                        EventUtil.printNotice("You are not allowed to create meeting because there is a record to process!", MessageUtil.ERROR);
                    }
                }
            }catch(Exception ex)
            {
                EventUtil.printNotice($"An error occured! {ex.Message}", MessageUtil.ERROR);
            }
        }

        private bool ValidateMeetingInfo(Meeting newMeeting)
        {
            List<string> errors = new List<string>();
            if (string.IsNullOrWhiteSpace(newMeeting.Name))
            {
                errors.Add("Title must not be null or white space!");
            }
            if (string.IsNullOrWhiteSpace(newMeeting.Description))
            {
                errors.Add("Description must not be null or white space!");
            }
            else
            {
                DateTime? endTime = newMeeting.TimeEnd;
                if (endTime is null)
                {
                    errors.Add("Invalid end date!");
                }
                else if (endTime < DateTime.Now)
                {
                    errors.Add("End time is past compared to current time!");
                }
            }
            if (errors.Count > 0)
            {
                EventUtil.printNotice(string.Join(Environment.NewLine, errors), MessageUtil.ERROR);
            }
            return errors.Count == 0;
        }

        public void CreateNewMeeting(Meeting newMeeting)
        {
            Meeting findMeeting = MeetingDBContext.Instance.GetMeetingByCreator(account.Username);
            if (findMeeting != null)
            {
                EventUtil.printNotice("A meeting created by this user already exists.", MessageUtil.ERROR);
                throw new InvalidOperationException("A meeting created by this user already exists.");
            }
            Meeting meeting = new()
            {
                Name = newMeeting.Name,
                Description = newMeeting.Description,
                TimeStart = DateTime.Now,
                TimeEnd = newMeeting.TimeEnd,
                Creator = account.Username,
                StatusId = 3
            };
            try
            {
                MeetingDBContext.Instance.AddMetting(meeting);
                EventUtil.printNotice("Create new meeting succesful!", MessageUtil.SUCCESS);
            }
            catch (Exception ex)
            {
                EventUtil.printNotice("An error occured! Please try again", MessageUtil.ERROR);
                throw new Exception(ex.Message, ex);
            }
        }
    }
}
