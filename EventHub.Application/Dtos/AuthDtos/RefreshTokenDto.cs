using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Dtos.AuthDtos
{
    public class RefreshTokenDto
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }
}
