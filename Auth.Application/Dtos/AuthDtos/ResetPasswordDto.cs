namespace Auth.Application.Dtos.AuthDtos
{
    public class ResetPasswordDto
    {
        public string Code { get; set; }
        public string Email { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
