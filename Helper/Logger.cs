using FastDevTool.ViewMode;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;


namespace FastDevTool.Helper {
    internal static class Logger {
        private enum LogLevel {
            DEBUG,
            INFO,
            WARN,
            ERROR,
            FATAL
        };
        private static List<string> _logLevel = new List<string>() {
            "debug",
            "info",
            "warn",
            "error",
            "fatal"
        };
        public static string LogText { get => mv.Log; private set { mv.Log = value; } }
        private static MainWindowViewMode mv;
        private static readonly ReaderWriterLockSlim rwl = new();
        private static void WriteLine(
            LogLevel logLevel,
            string msg,
            string caller,
            //string file,
            int line) 
        {
            string fmtmsg = string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}][{1}]: {2} [{3}, {4}]\n",
                DateTime.Now, logLevel, msg, caller, line);

            rwl.EnterWriteLock();
            LogText += fmtmsg;
            rwl.ExitWriteLock();
        }

        public static void Debug(
            String format = "",
            [CallerMemberName] string caller = "",
            //[CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            params object?[] args) => WriteLine(LogLevel.DEBUG, string.Format(format, args), caller, line);

        public static void Info(
            string format = "",
            [CallerMemberName] string caller = "",
            //[CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            params object?[] args) => WriteLine(LogLevel.INFO, string.Format(format, args), caller, line);

        public static void Warn(
            String format = "",
            [CallerMemberName] string caller = "",
            //[CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            params object?[] args) => WriteLine(LogLevel.WARN, string.Format(format, args), caller, line);

        public static void Error(
            String format = "",
            [CallerMemberName] string caller = "",
            //[CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            params object?[] args) => WriteLine(LogLevel.ERROR, string.Format(format, args), caller, line);

        public static void Fatal(
            String format = "",
            [CallerMemberName] string caller = "",
            //[CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            params object?[] args) => WriteLine(LogLevel.FATAL, string.Format(format, args), caller, line);


        public static void iniLog(MainWindowViewMode mwwm) {
            mv = mwwm;
        }

    }
}

