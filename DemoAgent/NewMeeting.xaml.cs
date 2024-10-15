using DemoAgent.Util;
using HotelManagement.Util;
using Models;
using Repositories;
using Services;
using System;
using System.Collections.Generic;
using System.Windows;
using Util;

namespace DemoAgent
{
    public partial class NewMeeting : Window
    {
        private readonly Account account;
        private readonly RecordService recordService;

        public NewMeeting(Account account)
        {
            InitializeComponent();
            this.account = account;

            recordService = RecordService.Instance;
            recordService.InitializeService(account);

            EventUtil.PrintNotice -= MessageBoxUtil.PrintMessageBox;
            EventUtil.PrintNotice += MessageBoxUtil.PrintMessageBox;
        }

        private void btSaveMeeting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime? endDate = UtilHelper.TryParseDate(dtTimeEnd.Text);
                DateTime? endTime = timePickerEnd.SelectedTime;

                if (endDate != null && endTime != null)
                {
                    DateTime endDateTime = endDate.Value.Date + endTime.Value.TimeOfDay;

                    Meeting newMeeting = new()
                    {
                        Name = txMeeting.Text,
                        Description = txDescription.Text,
                        TimeEnd = endDateTime
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
                            EventUtil.printNotice("You are not allowed to create a meeting because there is a record to process!", MessageUtil.ERROR);
                        }
                    }
                }
                else
                {
                    EventUtil.printNotice("Invalid end time or date!", MessageUtil.ERROR);
                }
            }
            catch (Exception ex)
            {
                EventUtil.printNotice($"An error occurred! {ex.Message}", MessageUtil.ERROR);
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

            DateTime? endTime = newMeeting.TimeEnd;
            if (endTime is null)
            {
                errors.Add("Invalid end date!");
            }
            else if (endTime < DateTime.Now)
            {
                errors.Add("End time is past compared to the current time!");
            }

            if (errors.Count > 0)
            {
                EventUtil.printNotice(string.Join(Environment.NewLine, errors), MessageUtil.ERROR);
            }

            return errors.Count == 0;
        }

        public void CreateNewMeeting(Meeting newMeeting)
        {
            try
            {
                Meeting existingMeeting = MeetingDBContext.Instance.GetMeetingByCreator(account.Username);
                if (existingMeeting != null)
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

                MeetingDBContext.Instance.AddMetting(meeting);
            }
            catch (Exception ex)
            {
                EventUtil.printNotice("An error occurred! Please try again.", MessageUtil.ERROR);
                throw new Exception(ex.Message, ex);
            }
        }
    }
}
