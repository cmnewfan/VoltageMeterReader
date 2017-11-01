using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoltageMeterReader
{
    class Log
    {
        private static Object event_object = new Object();
        private static Object error_object = new Object();
        private static String log_file
        {
            get
            {
                return Environment.CurrentDirectory + "\\Log\\" + DateTime.Now.ToString("yyyyMMdd") + "_event.log";
            }
        }
        private static String err_file
        {
            get
            {
                return Environment.CurrentDirectory + "\\Log\\" + DateTime.Now.ToString("yyyyMMdd") + "_error.log";
            }
        }

        public static void LogEvent(String content, int percent = 0)
        {
            if (percent == -1)
            {
                lock (event_object)
                {
                    File.AppendAllText(log_file, content + "\r\n");
                }
            }
            else
            {
                lock (event_object)
                {
                    File.AppendAllText(log_file, DateTime.Now.ToString() + ":" + content + "\r\n");
                }
            }
        }

        public static void LogEvent(String content)
        {
            lock (event_object)
            {
                File.AppendAllText(log_file, DateTime.Now.ToString() + ":" + content + "\r\n");
            }
        }

        public static void LogException(Exception ex)
        {
            lock (error_object)
            {
                DateTime now = DateTime.Now;
                File.AppendAllText(err_file, now.ToString() + ":" + ex.Message + "\r\n");
                if (ex.InnerException != null)
                {
                    File.AppendAllText(err_file, now.ToString() + ":" + ex.InnerException.Message + "\r\n");
                }
                File.AppendAllText(err_file, now.ToString() + ":" + ex.StackTrace + "\r\n");
            }
        }

    }
}
