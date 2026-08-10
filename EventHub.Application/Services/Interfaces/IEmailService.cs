using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string htmlBody);
    }
}
