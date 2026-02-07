using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Task4.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Email { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public bool IsBlocked { get; set; }

        public bool IsEmailVerified { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public string? EmailVerificationToken { get; set; }
        public DateTime? EmailVerificationTokenExpiresAt { get; set; }
        public DateTime? VerificationEmailLastSentAt { get; set; }

        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiresAt { get; set; }
        public DateTime? PasswordResetLastSentAt { get; set; }

        [NotMapped]
        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Email)) return "";
                var at = Email.IndexOf('@');
                return at > 0 ? Email.Substring(0, at) : Email;
            }
        }
    }
}
