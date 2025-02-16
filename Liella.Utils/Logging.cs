namespace Liella.Utils {
    public enum LoggingLevel {
        Fatal = 1,
        Error = 2,
        Info = 3,
        Verbose = 4
    }
    public interface ILogger {
        void Log(string category, LoggingLevel level, string message);
        void Fatal(string category, string message, Exception? ex);
        void Error(string category, string message, Exception? ex);
        void Info(string category, string message);
        void Verbose(string category, string message);
        IDisposable BeginScope(string message);
    }

    public class ConsoleLogger : ILogger {
        private struct ConsoleScope : IDisposable {
            private string m_Message;
            public ConsoleScope(string message) {
                m_Message = message;
            }
            public void Dispose() {
                var sideLength = (Console.BufferWidth - m_Message.Length - 4) / 2;
                var dupString = new string('=', sideLength);
                Console.WriteLine($"{dupString}< {m_Message} >{dupString}");
            }
        }
        public IDisposable BeginScope(string message) {
            var sideLength = (Console.BufferWidth - message.Length - 4) / 2;
            var dupString = new string('=', sideLength);
            Console.WriteLine($"{dupString}< {message} >{dupString}");
            return new ConsoleScope($"End of {message}");
        }

        public void Error(string category, string message, Exception? ex) {
            Log(category, LoggingLevel.Error, $"{message}. Exception: \n{ex?.ToString() ?? "None"}");
        }

        public void Fatal(string category, string message, Exception? ex) {
            Log(category, LoggingLevel.Fatal, $"{message}. Exception: \n{ex?.ToString() ?? "None"}");
        }

        public void Info(string category, string message) {
            Log(category, LoggingLevel.Info, message);
        }

        public void Log(string category, LoggingLevel level, string message) {
            var dateTime = DateTime.Now.ToString("yyyy/MM/dd-hh:mm:ss.fff");
            var levelColor = level switch {
                LoggingLevel.Info => ConsoleColor.Green,
                LoggingLevel.Verbose => ConsoleColor.Gray,
                LoggingLevel.Error => ConsoleColor.Red,
                LoggingLevel.Fatal => ConsoleColor.DarkRed,
                _ => ConsoleColor.Gray
            };
            Console.Write($"[{dateTime}]");

            Console.ForegroundColor = levelColor;
            Console.Write($" [{level.ToString().ToUpper()}]");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($" [{category}] ");
            Console.ResetColor();

            Console.WriteLine(message);
        }

        public void Verbose(string category, string message) {
            Log(category, LoggingLevel.Verbose, message);
        }
    }
    public class LiLogger : ILogger {
        public static LiLogger Default { get; } = new();
        protected List<ILogger> m_BackLoggers = new() { new ConsoleLogger() };
        private struct ScopeDisposable : IDisposable {
            private IEnumerable<IDisposable> m_Disposables;
            public ScopeDisposable(IEnumerable<IDisposable> disposables) {
                m_Disposables = disposables;
            }
            public void Dispose() {
                foreach(var i in m_Disposables) i.Dispose();
            }
        }
        public IDisposable BeginScope(string message) {
            var disposables = m_BackLoggers.Select(e => e.BeginScope(message)).ToArray();
            return new ScopeDisposable(disposables);
        }

        public void Log(string category, LoggingLevel level, string message) {
            foreach(var i in m_BackLoggers) i.Log(category, level, message);
        }

        public void Fatal(string category, string message, Exception? ex) {
            foreach(var i in m_BackLoggers) i.Fatal(category, message, ex);
        }

        public void Error(string category, string message, Exception? ex) {
            foreach(var i in m_BackLoggers) i.Error(category, message, ex);
        }

        public void Info(string category, string message) {
            foreach(var i in m_BackLoggers) i.Info(category, message);
        }

        public void Verbose(string category, string message) {
            foreach(var i in m_BackLoggers) i.Verbose(category, message);
        }
    }
}
