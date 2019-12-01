using System;
using System.Globalization;
using System.IO;
using System.Net;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class LevelTraderStorage : Robot
    {
        private static WebClient client = new WebClient();

        private static string URL = "http://cdn-nfs.faireconomy.media/ff_calendar_thisweek.xml";

        [Parameter("Source Folder", Group = "Settings", DefaultValue = "C:\\Users\\marec\\Documents\\NinjaTrader 8\\bin\\MarketProfitPack\\CustomLevels")]
        public string Source { get; set; }

        [Parameter("Destination Folder", Group = "Settings", DefaultValue = "C:\\Users\\marec\\Documents\\TRADING_BACKTEST_TEST")]
        public string Destination { get; set; }

        [Parameter("Daily Update Time [UTC]", DefaultValue = "12:00", Group = "Settings")]
        public string DailyReloadTimeInput { get; set; }

        private int DailyReloadHour, DailyReloadMinute;
        private DateTime DailyReloadTime;


        protected override void OnStart()
        {
            DailyReloadHour = int.Parse(DailyReloadTimeInput.Split(new string[] 
            {
                ":"
            }, StringSplitOptions.None)[0]);
            DailyReloadMinute = int.Parse(DailyReloadTimeInput.Split(new string[] 
            {
                ":"
            }, StringSplitOptions.None)[1]);
            Timer.Start(60);
        }

        protected override void OnTimer()
        {
            base.OnTimer();
            DateTime time = Server.TimeInUtc;
            if (DailyReloadHour == time.Hour && DailyReloadMinute == time.Minute && ( time.DayOfWeek != DayOfWeek.Saturday || time.DayOfWeek != DayOfWeek.Sunday))
            {
                DirectoryInfo source = new DirectoryInfo(Source);
                DirectoryInfo destination = new DirectoryInfo(GetDestinationFolder());
                FetchCalendar();
                if (!destination.Exists)
                {
                    destination = Directory.CreateDirectory(GetDestinationFolder());
                    CopyFilesRecursively(source, destination);
                    Print("Copy levels to backup folder finished. Source: {0} Destination: {1}", source, GetDestinationFolder());
                }
                else
                {
                    Print("Skipping backup. Folder already exists. Source: {0} Destination: {1}", source, GetDestinationFolder());
                }
            }
        }


        private string GetDestinationFolder()
        {
            DateTime time = Server.TimeInUtc;
            int week = GetWeekOfYear(time);
            return Destination + "\\" + time.Year + "-" + time.Month + "-" + time.Day + " (" + week + ")\\";
        }

        private void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name));
        }

        private void FetchCalendar()
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URL);
            request.Method = "GET";
            String body = String.Empty;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                var fileStream = File.Create(Destination + "\\calendar-" + Server.Time.Year + "-" + GetWeekOfYear(Server.Time) + ".xml");
                Stream dataStream = response.GetResponseStream();
                dataStream.CopyTo(fileStream);
                dataStream.Close();
                fileStream.Close();
            }
        }

        public int GetWeekOfYear(DateTime time)
        {
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
    }
}
