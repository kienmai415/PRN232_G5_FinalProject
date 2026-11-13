using System.ComponentModel.DataAnnotations;

namespace SportCourtManagent_Server.DTOs.Customer
{
    public class UpdateCustomerRequest
    {
        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [MaxLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự.")]
        public string FullName { get; set; } = string.Empty;

        private string? _phone;

        [RegularExpression(@"^(0[35789]\d{8}|02\d{9})$", ErrorMessage = "Số điện thoại không hợp lệ .")]
        public string? Phone
        {
            get => _phone;
            set => _phone = string.IsNullOrWhiteSpace(value)
                ? null
                : System.Text.RegularExpressions.Regex.Replace(value, @"[\s.-]", "");
        }

        [Required(ErrorMessage = "Giới tính là bắt buộc.")]
        [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Giới tính không hợp lệ.")]
        public string Gender { get; set; } = "Other";

        [Required(ErrorMessage = "Trình độ là bắt buộc.")]
        [RegularExpression("^(Beginner|Intermediate|Advanced)$", ErrorMessage = "Trình độ không hợp lệ.")]
        public string SkillLevel { get; set; } = "Beginner";

        [Range(0, 1000000, ErrorMessage = "Điểm tích lũy không hợp lệ.")]
        public int LoyaltyPoints { get; set; }

        public int? MembershipTierId { get; set; }

        public bool IsActive { get; set; }
    }
}
