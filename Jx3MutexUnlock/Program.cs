using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jx3MutexUnlock
{
    class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hHandle);

        private static readonly string[] jx3MutexNames = new string[] { "A5DFEC3F", "0DF11825", "5D2D1767" };
        static void Main(string[] args)
        {
            foreach (var item in jx3MutexNames)
            {
                ReleaseMutex(item);
            }
            Console.WriteLine("Anykey to exit");
            Console.ReadKey();
        }

        private static void ReleaseMutex(string muName)
        {
            Mutex mutex = new Mutex(true, muName, out bool created);
            mutex.Close();
            mutex.Dispose();
            Console.WriteLine($"Mutex {muName} available: {created}");
            if (!created)
            {
                Console.WriteLine("Start to detect Mutex");

                StringBuilder stringbuilder = new StringBuilder();
                Regex regex = new Regex(@"JX3.*exe +pid: *(\d+) *type: Mutant *(\w+):.*BaseNamedObjects.*");

                Process process = new Process();
                process.StartInfo = new ProcessStartInfo(@".\handle64.exe", $@"/accepteula -nobanner -a ""{muName}""")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };
                process.OutputDataReceived += new DataReceivedEventHandler(
                    delegate (object sender, DataReceivedEventArgs e)
                    {
                        stringbuilder.AppendLine(e.Data);
                    });

                process.EnableRaisingEvents = true;
                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
                Console.WriteLine(stringbuilder.ToString());
                string[] stdout = stringbuilder.ToString().Replace("\r\n", "\n").Split('\n');
                foreach (var line in stdout)
                {
                    var match = regex.Match(line);
                    if (match.Success)
                    {
                        string pid = match.Groups[1].Value;
                        string hid = match.Groups[2].Value;
                        Console.WriteLine($"Killing Mutex PID:{pid} HID:{hid}");
                        Process killProcess = new Process()
                        {
                            StartInfo = new ProcessStartInfo()
                            {
                                FileName = @".\handle64.exe",
                                Arguments = $@"-nobanner -p {pid} -c {hid} -y",
                                CreateNoWindow = true
                            }
                        };
                        killProcess.Start();
                        killProcess.WaitForExit();
                    }
                }

            }
        }
    }
}
