namespace Timelapse
{
    using System;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Threading;

    using log4net;
    using log4net.Config;

    /// <summary>
    /// Program for creating timelapses using VLC.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The timer
        /// </summary>
        private static Timer timer;

        /// <summary>
        /// The log
        /// </summary>
        private static ILog log;

        /// <summary>
        /// Gets the interval.
        /// </summary>
        /// <value>The interval.</value>
        private static int Interval
        {
            get
            {
                return int.Parse(ConfigurationManager.AppSettings["Interval"]);
            }
        }

        /// <summary>
        /// Gets the capture time.
        /// </summary>
        /// <value>The capture time.</value>
        private static int CaptureTime
        {
            get
            {
                return int.Parse(ConfigurationManager.AppSettings["CaptureTime"]);
            }
        }

        /// <summary>
        /// Gets the stream frame rate.
        /// </summary>
        /// <value>The stream frame rate.</value>
        private static int StreamFrameRate
        {
            get
            {
                return int.Parse(ConfigurationManager.AppSettings["StreamFrameRate"]);
            }
        }

        /// <summary>
        /// Gets the timeout.
        /// </summary>
        /// <value>The timeout.</value>
        private static int Timeout
        {
            get
            {
                return int.Parse(ConfigurationManager.AppSettings["Timeout"]);
            }
        }

        /// <summary>
        /// Gets the VLC arguments.
        /// </summary>
        /// <value>The VLC arguments.</value>
        private static string Arguments
        {
            get
            {
                return ConfigurationManager.AppSettings["Arguments"];
            }
        }

        /// <summary>
        /// Gets the VLC path.
        /// </summary>
        /// <value>The VLC path.</value>
        private static string Path
        {
            get
            {
                return ConfigurationManager.AppSettings["Path"];
            }
        }

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            Console.WriteLine("###############");
            Console.WriteLine("## Timelapse ##");
            Console.WriteLine("###############");

            Console.WriteLine();
            log.Info("Starting application");

            Console.WriteLine();
            log.DebugFormat("VLC Path: '{0}'", Path);

            Console.WriteLine();
            Console.WriteLine("Press ESC to stop");

            timer = new Timer(OnTimerIntervalElapsed, null, 0, Interval);

            // Run until user presses ESC
            while (true)
            {
                var cki = Console.ReadKey(true);

                if (cki.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine();
                    log.Info("Exiting application");

                    Environment.Exit(-1);
                }
            }
        }

        /// <summary>
        /// Handles the <see cref="E:TimerIntervalElapsed" /> event.
        /// </summary>
        /// <param name="state">The state.</param>
        private static void OnTimerIntervalElapsed(object state)
        {
            Console.WriteLine();
            log.DebugFormat("Starting capture");
            Console.WriteLine();

            CaptureImage(null);

            Console.WriteLine();
            log.DebugFormat("Capture finished");
            Console.WriteLine("-----------------");
        }

        /// <summary>
        /// Captures the image.
        /// </summary>
        /// <param name="state">The state.</param>
        private static void CaptureImage(object state)
        {
            var date = DateTime.Now;

            // Create folder
            var dir = string.Format("Images\\{0}", date.ToString("yyyyMMdd"));

            log.DebugFormat("Creating directory '{0}'", dir);
            Directory.CreateDirectory(dir);

            // Run VLC to capture image, abort if the process takes longer than the specified timeout
            var start = new ProcessStartInfo
            {
                Arguments = string.Format(Arguments, date.ToString("yyyyMMdd"), date.ToString("HHmmss"), StreamFrameRate * CaptureTime * 0.6, CaptureTime),
                FileName = Path,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            Console.WriteLine();
            log.DebugFormat("VLC Arguments: {0}", start.Arguments);
            Console.WriteLine();

            // Run the external process & wait for it to finish
            try
            {
                log.DebugFormat("Running VLC");

                var proc = Process.Start(start);
                proc.OutputDataReceived += (sender, args) => log.Debug(args.Data);
                proc.ErrorDataReceived += (sender, args) => log.Error(args.Data);
                
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                var completed = proc.WaitForExit(Timeout);

                if (!completed)
                {
                    log.WarnFormat("VLC did not quit before the alloted limit of {0}s, aborting VLC thread", CaptureTime);

                    proc.Close();

                    if (!proc.HasExited)
                    {
                        proc.Kill();
                    }
                }

                log.DebugFormat("VLC processing completed");
            }
            catch (ThreadAbortException)
            {
                // Do nothing
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }           
        }
    }
}
