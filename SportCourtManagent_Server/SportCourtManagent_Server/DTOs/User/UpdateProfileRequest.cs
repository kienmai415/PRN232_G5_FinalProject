using System;
using System.ComponentModel.DataAnnotations;

namespace SportCourtManagent_Server.DTOs.User
{
    public class UpdateProfileRequest
    {
        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự.")]
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

        [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Giới tính phải là Male, Female hoặc Other.")]
        public string? Gender { get; set; }

        [StringLength(500, ErrorMessage = "URL ảnh đại diện quá dài.")]
        public string? AvatarUrl { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        public string? SkillLevel { get; set; }
    }
}
