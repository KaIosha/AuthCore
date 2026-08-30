using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Auth.Application.Dtos.AuthDtos
{
    public class ForgetPasswordDto
    {
        [EmailAddress(ErrorMessage ="Enter a Valid Email")]
        public string Email { get; set; }
    }
}
