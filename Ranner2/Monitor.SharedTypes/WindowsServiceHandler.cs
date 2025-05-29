using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitor.SharedTypes
{
    public static class WindowsServiceHandler
    {
        public static void Stop(string windowsServiceName)
        {
            var p = new Process();
            p.StartInfo =
            (new ProcessStartInfo()
            {
                FileName = "sc",
                Arguments = $"stop \"{windowsServiceName}\"",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            });            

            p.Start();

            var output = p.StandardOutput.ReadToEnd();
            var error = p.StandardError.ReadToEnd();

            p.WaitForExit();

            Console.WriteLine(output);
            Console.WriteLine(error);

        }
        public static void Start(string windowsServiceName)
        {
            var p = new Process();
            p.StartInfo =
            (new ProcessStartInfo()
            {
                FileName = "sc",
                Arguments = $"start \"{windowsServiceName}\"",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            });

            p.Start();

            var output = p.StandardOutput.ReadToEnd();
            var error = p.StandardError.ReadToEnd();

            p.WaitForExit();

            Console.WriteLine(output);
            Console.WriteLine(error);

        }

        public static void Delete(string windowsServiceName)
        {
            var p = new Process();
            p.StartInfo =
            (new ProcessStartInfo()
            {
                FileName = "sc",
                Arguments = $"delete \"{windowsServiceName}\"",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            });

            p.Start();

            var output = p.StandardOutput.ReadToEnd();
            var error = p.StandardError.ReadToEnd();

            p.WaitForExit();

            Console.WriteLine(output);
            Console.WriteLine(error);

        }

        public static void Create(string windowsServiceName, string fileName)
        {            
            var p = new Process();
            p.StartInfo =
            (new ProcessStartInfo()
            {
                FileName = "sc",
                Arguments = $"create \"{windowsServiceName}\" binPath= \"{fileName}\" start= auto",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            });

            Console.WriteLine(p.StartInfo.Arguments);

            p.Start();

            var output = p.StandardOutput.ReadToEnd();
            var error = p.StandardError.ReadToEnd();

            p.WaitForExit();

            Console.WriteLine(output);
            Console.WriteLine(error);

        }
    }
}
