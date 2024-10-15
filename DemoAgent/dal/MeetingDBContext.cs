using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Repositories
{
    public class MeetingDBContext
    {
        public static MeetingDBContext Instance = new MeetingDBContext();
        public void AddMetting(Meeting meeting)
        {
            try
            {
                var context = new DemoAgentContext();
                context.Meetings.Add(meeting);
                context.SaveChanges();
            }
            catch (Exception ex)
            {

            }
        }

        public Meeting GetMeetingByCreator(string creator)
        {
            try
            {
                var context = new DemoAgentContext();
                var meeting = context.Meetings.FirstOrDefault(m => m.Creator == creator && m.StatusId == 3) as Meeting;
                if (meeting != null)
                {
                    return meeting;
                }
            }
            catch (Exception ex)
            {

            }
            return null;
        }



        public void updateStatusMeeting(Meeting meeting)
        {
            try
            {
                var context = new DemoAgentContext();
                var findMeeting = context.Meetings.FirstOrDefault(m => m.Id == meeting.Id);
                if (meeting != null)
                {
                    findMeeting.StatusId = meeting.StatusId;
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {

            }
        }

        public List<dynamic> GetHistoryMeetingsByCreator(string creator)
        {
            List<dynamic> list = new List<dynamic>();
            try
            {
                var context = new DemoAgentContext();
                var findMeetings = context.Meetings.Where(m => m.Creator == creator && m.StatusId == 4).Select(meeting => new
                {
                    Id = meeting.Id,
                    Creator = meeting.Creator,
                    Name = meeting.Name,
                    Description = meeting.Description,
                    TimeStart = meeting.TimeStart,
                    TimeEnd = meeting.TimeEnd,
                    status = "Done",
                }).ToList<dynamic>();
                list = findMeetings;
            }
            catch (Exception)
            {
            }
            return list;
        }

    }
}
