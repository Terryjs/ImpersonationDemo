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

            return new ImpersonationScope(context, tokenHandle);
        }

        public T RunAs<T>(string domain, string username, string password, Func<T> action)
        {
            using (Impersonate(domain, username, password))
            {
                return action();
            }
        }

        private sealed class ImpersonationScope : IDisposable
        {
            private readonly object _context;
            private readonly IntPtr _tokenHandle;

            public ImpersonationScope(object context, IntPtr tokenHandle)
            {
                _context = context;
                _tokenHandle = tokenHandle;
            }

            public void Dispose()
            {
                try
                {
                    _context?.GetType().GetMethod("Undo")?.Invoke(_context, null);
                }
                catch { }
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
