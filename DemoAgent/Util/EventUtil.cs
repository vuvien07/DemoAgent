using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoAgent.Util
{
    public class EventUtil
    {
        public static event Action<string, string>? PrintNotice;

        public static void printNotice(string title, string type)
        {
            PrintNotice?.Invoke(title, type);
        }
    }
}
