using System;

namespace ImpersonationDemo
{
    public interface IImpersonationService
    {
        IDisposable Impersonate(string domain, string username, string password);
        T RunAs<T>(string domain, string username, string password, Func<T> action);
    }
}
