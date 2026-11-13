using System.ComponentModel.DataAnnotations;

namespace SportCourtManagent_Server.DTOs.Customer
{
    public class CreateCustomerRequest
    {
        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [MaxLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [MaxLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
        public string Email { get; set; } = string.Empty;

        private string? _phone;

        [RegularExpression(@"^(0[35789]\d{8}|02\d{9})$", ErrorMessage = "Số điện thoại không hợp lệ .")]
        public string? Phone
        {
            get => _phone;
            set => _phone = string.IsNullOrWhiteSpace(value)
                ? null
                : System.Text.RegularExpressions.Regex.Replace(value, @"[\s.-]", "");
        }

        [MaxLength(100, ErrorMessage = "Mật khẩu quá dài.")]
        public string? Password { get; set; } // If empty, defaults to Customer@123

        [Required(ErrorMessage = "Giới tính là bắt buộc.")]
        [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Giới tính không hợp lệ.")]
        public string Gender { get; set; } = "Other";

        [Required(ErrorMessage = "Trình độ là bắt buộc.")]
        [RegularExpression("^(Beginner|Intermediate|Advanced)$", ErrorMessage = "Trình độ không hợp lệ.")]
        public string SkillLevel { get; set; } = "Beginner";

        public int LoyaltyPoints { get; set; } = 0;

        public int? MembershipTierId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
