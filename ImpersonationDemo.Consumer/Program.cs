using System;
using System.IO;
using System.Reflection;
using System.Security.Principal;

namespace ImpersonationDemo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (!OperatingSystem.IsWindows())
            {
                Console.WriteLine("This demo must run on Windows.");
                return;
            }

            try
            {
                var impersonationService = LoadWindowsService();

                var domain = "YOUR_DOMAIN";
                var username = "YOUR_USERNAME";
                var password = "YOUR_PASSWORD";

                var currentUser = impersonationService.RunAs(
                    domain,
                    username,
                    password,
                    () => WindowsIdentity.GetCurrent().Name);

                Console.WriteLine($"Current user inside impersonation: {currentUser}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Impersonation failed:");
                Console.WriteLine(ex);
            }
        }

        private static IImpersonationService LoadWindowsService()
        {
            var dllPath = Path.Combine(AppContext.BaseDirectory, "ImpersonationDemo.Windows.dll");

            if (!File.Exists(dllPath))
            {
                throw new FileNotFoundException(
                    "Windows implementation DLL not found. Build the Windows project and ensure it was copied to the consumer output folder.",
                    dllPath);
            }

            var assembly = Assembly.LoadFrom(dllPath);
            var type = assembly.GetType("ImpersonationDemo.WindowsImpersonationService");

            if (type == null)
            {
                throw new InvalidOperationException("Could not find WindowsImpersonationService in the loaded assembly.");
            }

            var instance = Activator.CreateInstance(type);
            if (instance == null)
            {
                throw new InvalidOperationException("Could not create an instance of WindowsImpersonationService.");
            }

            return (IImpersonationService)instance;
        }
    }
}
