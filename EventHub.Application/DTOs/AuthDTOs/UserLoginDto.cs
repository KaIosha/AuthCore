using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.DTOs.AuthDTOs
{
    public class LoginDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
