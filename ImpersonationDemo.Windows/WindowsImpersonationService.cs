using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace ImpersonationDemo
{
    public class WindowsImpersonationService : IImpersonationService
    {
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LogonUser(
            string lpszUsername,
            string lpszDomain,
            string lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            out IntPtr phToken);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hHandle);

        public IDisposable Impersonate(string domain, string username, string password)
        {
            IntPtr tokenHandle;

            if (!LogonUser(username, domain, password, 2, 0, out tokenHandle))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    $"LogonUser failed for '{username}'.");
            }

            var identity = new WindowsIdentity(tokenHandle);
            var context = identity.Impersonate();

            return new ImpersonationScope(tokenHandle, context);
        }

        public T RunAs<T>(string domain, string username, string password, Func<T> action)
        {
            IntPtr tokenHandle;

            if (!LogonUser(username, domain, password, 2, 0, out tokenHandle))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    $"LogonUser failed for '{username}'.");
            }

            var identity = new WindowsIdentity(tokenHandle);

            try
            {
                using (var context = identity.Impersonate())
                {
                    return action();
                }
            }
            finally
            {
                if (tokenHandle != IntPtr.Zero)
                {
                    CloseHandle(tokenHandle);
                }
            }
        }

        private sealed class ImpersonationScope : IDisposable
        {
            private readonly IntPtr _tokenHandle;
            private readonly IDisposable _context;

            public ImpersonationScope(IntPtr tokenHandle, IDisposable context)
            {
                _tokenHandle = tokenHandle;
                _context = context;
            }

            public void Dispose()
            {
                try
                {
                    _context?.Dispose();
                }
                finally
                {
                    if (_tokenHandle != IntPtr.Zero)
                    {
                        CloseHandle(_tokenHandle);
                    }
                }
            }
        }
    }
}
