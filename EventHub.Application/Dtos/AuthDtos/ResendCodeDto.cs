using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace EventHub.Application.Dtos.AuthDtos
{
    public class ResendCodeDto
    {
        [EmailAddress(ErrorMessage ="Invalid email address.")]
        public string Email { get; set; }
    }
}
