using System;
using System.Collections.Generic;
using System.Text;

namespace Auth.Application.Dtos.AuthDtos
{
    public class RefreshTokenDto
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }
}
