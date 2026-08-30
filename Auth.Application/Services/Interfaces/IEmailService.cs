using System;
using System.Collections.Generic;
using System.Text;

namespace Auth.Application.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string htmlBody);
    }
}
