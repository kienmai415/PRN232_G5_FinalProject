using System.ComponentModel.DataAnnotations;

namespace SportCourtManagent_Server.DTOs.User
{
    public class UpdateUserByAdminRequest
    {
        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
        public string Email { get; set; } = string.Empty;

        private string _phone = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [RegularExpression(@"^(0[35789]\d{8}|02\d{9})$", ErrorMessage = "Số điện thoại không hợp lệ .")]
        public string Phone
        {
            get => _phone;
            set => _phone = string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : System.Text.RegularExpressions.Regex.Replace(value, @"[\s.-]", "");
        }

        [Required(ErrorMessage = "Vai trò là bắt buộc.")]
        public string Role { get; set; } = string.Empty;

        [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Giới tính phải là Male, Female hoặc Other.")]
        public string Gender { get; set; } = "Other";

        public string SkillLevel { get; set; } = "Beginner";

        public bool IsActive { get; set; }
    }
}
