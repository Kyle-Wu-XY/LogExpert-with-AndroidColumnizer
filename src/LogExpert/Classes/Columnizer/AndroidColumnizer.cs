using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace LogExpert
{
    using System;
    using System.Text.RegularExpressions;
    using static LogExpert.TimeFormatDeterminer;

    public class AndroidColumnizer : ILogLineColumnizer, IColumnizerPriority
    {
        #region ILogLineColumnizer implementation
        private readonly Regex lineRegex = new Regex(@"(?<date>\d+-\d+) (?<time>\d{2}:\d{2}:\d{2}\.\d{3})\s+(?<pid>\d+)\s+(?<tid>\d+)\s+(?<level>[I|V|W|E|D|F])\s+(?<tag>.*?): (?<txt>.*)");
        protected int timeOffset = 0;
        private const int LOG_MAX_LENGTH = 10*1024;
        private TimeFormatDeterminer _timeFormatDeterminer = new TimeFormatDeterminer();

        public bool IsTimeshiftImplemented()
        {
            return true;
        }

        public void SetTimeOffset(int msecOffset)
        {
            this.timeOffset = msecOffset;
        }

        public int GetTimeOffset()
        {
            return this.timeOffset;
        }


        public DateTime GetTimestamp(ILogLineColumnizerCallback callback, ILogLine line)
        {
            IColumnizedLogLine cols = SplitLine(callback, line);
            if (cols == null || cols.ColumnValues  == null || cols.ColumnValues.Length < 2)
            {
                return DateTime.MinValue;
            }
            if (cols.ColumnValues[0].FullValue.Length == 0 || cols.ColumnValues[1].FullValue.Length == 0)
            {
                return DateTime.MinValue;
            }
            FormatInfo formatInfo = _timeFormatDeterminer.DetermineDateTimeFormatInfo(line.FullLine);
            if (formatInfo == null)
            {
                return DateTime.MinValue;
            }

            try
            {
                DateTime dateTime = DateTime.ParseExact(
                    cols.ColumnValues[0].FullValue + " " + cols.ColumnValues[1].FullValue, formatInfo.DateTimeFormat,
                    formatInfo.CultureInfo);
                return dateTime;
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }


        public void PushValue(ILogLineColumnizerCallback callback, int column, string value, string oldValue)
        {
            if (column == 1)
            {
                try
                {
                    FormatInfo formatInfo = _timeFormatDeterminer.DetermineTimeFormatInfo(oldValue);
                    if (formatInfo == null)
                    {
                        return;
                    }
                    DateTime newDateTime = DateTime.ParseExact(value, formatInfo.TimeFormat, formatInfo.CultureInfo);
                    DateTime oldDateTime = DateTime.ParseExact(oldValue, formatInfo.TimeFormat, formatInfo.CultureInfo);
                    long mSecsOld = oldDateTime.Ticks / TimeSpan.TicksPerMillisecond;
                    long mSecsNew = newDateTime.Ticks / TimeSpan.TicksPerMillisecond;
                    this.timeOffset = (int) (mSecsNew - mSecsOld);
                }
                catch (FormatException)
                {
                }
            }
        }

        public string GetName()
        {
            return "Android Columnizer";
        }

        public string GetDescription()
        {
            return "Splits every line into 7 fields: Date, Time, PID, TID, Level, Tag and the rest of the log message";
        }

        public int GetColumnCount()
        {
            return 7;
        }

        public string[] GetColumnNames()
        {
            return new string[] {"date", "time", "pid", "tid", "level", "tag", "txt"};
        }

        public IColumnizedLogLine SplitLine(ILogLineColumnizerCallback callback, ILogLine line)
        {
            ColumnizedLogLine cLogLine = new ColumnizedLogLine
            {
                LogLine = line
            };

            Column[] columns = new Column[7]
            {
                new Column {FullValue = "", Parent = cLogLine},
                new Column {FullValue = "", Parent = cLogLine},
                new Column {FullValue = "", Parent = cLogLine},
                new Column {FullValue = "", Parent = cLogLine},
                new Column {FullValue = "", Parent = cLogLine},
                new Column {FullValue = "", Parent = cLogLine},
                new Column {FullValue = "", Parent = cLogLine}
            };

            cLogLine.ColumnValues = columns.Select(a => a as IColumn).ToArray();

            string temp = line.FullLine;
            if (temp.Length > LOG_MAX_LENGTH )
            {
                // spam
                temp = temp.Substring(0, LOG_MAX_LENGTH);
                columns[3].FullValue = temp;
                return cLogLine;
            }
            // 0      1             2     3   4   5                  6
            // 12-14 15:40:35.103  1923  1923 E CarEvSettingAdapter: onChangeEvent beanId is empty!

            if (this.lineRegex.IsMatch(temp))
            {
                Match match = this.lineRegex.Match(temp);
                GroupCollection groups = match.Groups;
                if (groups.Count == 8)
                {
                    columns[0].FullValue = groups["date"].Value;
                    columns[1].FullValue = groups["time"].Value;
                    columns[2].FullValue = groups["pid"].Value;
                    columns[3].FullValue = groups["tid"].Value;
                    columns[4].FullValue = groups["level"].Value;
                    columns[5].FullValue = groups["tag"].Value;
                    columns[6].FullValue = groups["txt"].Value;
                }
            }

            return cLogLine;
        }

        public Priority GetPriority(string fileName, IEnumerable<ILogLine> samples)
        {
            Priority result = Priority.NotSupport;

            int timeStampCount = 0;
            foreach (var line in samples)
            {
                if (line == null || string.IsNullOrEmpty(line.FullLine))
                {
                    continue;
                }
                var timeDeterminer = new TimeFormatDeterminer();
                if (null != timeDeterminer.DetermineDateTimeFormatInfo(line.FullLine))
                {
                    timeStampCount++;
                }
                else
                {
                    timeStampCount--;
                }
            }

            if (timeStampCount > 0)
            {
                result = Priority.WellSupport;
            }

            return result;
        }

        #endregion
    }
}
