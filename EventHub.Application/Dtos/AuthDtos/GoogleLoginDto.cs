using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Dtos.AuthDtos
{
    public class GoogleLoginDto
    {
        public string IdToken { get; set; } = string.Empty;
    }
}
