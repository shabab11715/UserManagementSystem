using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Task4.Data;
using Task4.Helpers;
using Task4.Models;
using Task4App.Services;

namespace Task4.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly EmailSender _email;

        public AuthController(ApplicationDbContext db, EmailSender email)
        {
            _db = db;
            _email = email;
        }

        // GET: /Auth/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Auth/Register
        [HttpPost]
        public async Task<IActionResult> Register(string email, string password)
        {
            email = (email ?? "").Trim().ToLower();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Email and password are required.";
                return View();
            }

            var user = new User
            {
                Email = email,
                PasswordHash = PasswordHasher.Hash(password),
                IsBlocked = false,
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = null
            };

            var token = GenerateToken();
            user.EmailVerificationToken = token;
            user.EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);
            user.VerificationEmailLastSentAt = DateTime.UtcNow;

            try
            {
                _db.Users.Add(user);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is PostgresException pgEx &&
                    pgEx.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    ViewBag.Error = "An account with this email already exists.";
                    return View();
                }

                throw;
            }

            var verifyUrl = Url.Action(
                action: "VerifyEmail",
                controller: "Auth",
                values: new { token },
                protocol: Request.Scheme
            );

            await _email.SendAsync(
                user.Email,
                "Verify your email",
                $@"
                    <p>Thanks for registering.</p>
                    <p>Please verify your email by clicking this link:</p>
                    <p><a href=""{verifyUrl}"">Verify Email</a></p>
                    <p>This link expires in 24 hours.</p>
                "
            );

            return RedirectToAction("Login", new { reason = "unverified_sent" });
        }

        // GET: /Auth/VerifyEmail?token=...
        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string token)
        {
            token = (token ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
            if (user == null)
                return RedirectToAction("Login");

            if (user.EmailVerificationTokenExpiresAt == null || user.EmailVerificationTokenExpiresAt < DateTime.UtcNow)
                return RedirectToAction("Login");

            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiresAt = null;

            await _db.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login(string? reason = null)
        {
            ViewBag.Info = null;

            if (reason == "blocked")
                ViewBag.Info = "Your account is blocked. Please contact admin.";
            else if (reason == "unverified")
                ViewBag.Info = "Your email is not verified. Please verify first.";
            else if (reason == "unverified_sent")
                ViewBag.Info = "Verification email sent. Please check your inbox.";
            else if (reason == "resent")
                ViewBag.Info = "Verification email resent. Please check your inbox.";
            else if (reason == "wait")
                ViewBag.Info = "Please wait 1 minute before requesting another email.";
            else if (reason == "reset_sent")
                ViewBag.Info = "Password reset email sent. Please check your inbox.";
            else if (reason == "reset_wait")
                ViewBag.Info = "Please wait 1 minute before requesting another reset email.";
            else if (reason == "reset_done")
                ViewBag.Info = "Password updated. You can login now.";

            return View();
        }

        // POST: /Auth/Login
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            email = (email ?? "").Trim().ToLower();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Email and password are required.";
                return View();
            }

            var hash = PasswordHasher.Hash(password);
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == hash);

            if (user == null)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            if (user.IsBlocked)
            {
                HttpContext.Session.Remove("UserId");
                return RedirectToAction("Login", new { reason = "blocked" });
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            HttpContext.Session.SetInt32("UserId", user.Id);

            return RedirectToAction("Index", "Home");
        }


        // GET: /Auth/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("UserId");
            return RedirectToAction("Login");
        }

        // GET: /Auth/ResendVerification
        [HttpGet]
        public IActionResult ResendVerification()
        {
            return View();
        }

        // POST: /Auth/ResendVerification
        [HttpPost]
        public async Task<IActionResult> ResendVerification(string email)
        {
            email = (email ?? "").Trim().ToLower();
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Email is required.";
                return View();
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "No account found with this email.";
                return View();
            }

            if (user.IsEmailVerified)
            {
                ViewBag.Error = "This account is already verified. Please login.";
                return View();
            }

            if (user.VerificationEmailLastSentAt != null &&
                DateTime.UtcNow < user.VerificationEmailLastSentAt.Value.AddMinutes(1))
            {
                return RedirectToAction("Login", new { reason = "wait" });
            }

            var token = GenerateToken();
            user.EmailVerificationToken = token;
            user.EmailVerificationTokenExpiresAt = DateTime.UtcNow.AddHours(24);
            user.VerificationEmailLastSentAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var verifyUrl = Url.Action("VerifyEmail", "Auth", new { token }, Request.Scheme);

            await _email.SendAsync(
                user.Email,
                "Verify your email",
                $@"
                    <p>Please verify your email by clicking the link below:</p>
                    <p><a href=""{verifyUrl}"">Verify Email</a></p>
                    <p>This link expires in 24 hours.</p>
                "
            );

            return RedirectToAction("Login", new { reason = "resent" });
        }

        // GET: /Auth/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Auth/ForgotPassword
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            email = (email ?? "").Trim().ToLower();
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Email is required.";
                return View();
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "No account found with this email.";
                return View();
            }

            if (user.PasswordResetLastSentAt != null &&
                DateTime.UtcNow < user.PasswordResetLastSentAt.Value.AddMinutes(1))
            {
                return RedirectToAction("Login", new { reason = "reset_wait" });
            }

            var token = GenerateToken();
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(30);
            user.PasswordResetLastSentAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var resetUrl = Url.Action("ResetPassword", "Auth", new { token }, Request.Scheme);

            await _email.SendAsync(
                user.Email,
                "Reset your password",
                $@"
                    <p>You requested a password reset.</p>
                    <p>Click this link to reset your password:</p>
                    <p><a href=""{resetUrl}"">Reset Password</a></p>
                    <p>This link expires in 30 minutes.</p>
                "
            );

            return RedirectToAction("Login", new { reason = "reset_sent" });
        }

        // GET: /Auth/ResetPassword?token=...
        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            ViewBag.Token = token ?? "";
            return View();
        }

        // POST: /Auth/ResetPassword
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string token, string newPassword)
        {
            token = (token ?? "").Trim();
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
            {
                ViewBag.Error = "Token and new password are required.";
                ViewBag.Token = token;
                return View();
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token);
            if (user == null)
            {
                ViewBag.Error = "Invalid reset token.";
                ViewBag.Token = token;
                return View();
            }

            if (user.PasswordResetTokenExpiresAt == null || user.PasswordResetTokenExpiresAt < DateTime.UtcNow)
            {
                ViewBag.Error = "Reset token expired.";
                ViewBag.Token = token;
                return View();
            }

            user.PasswordHash = PasswordHasher.Hash(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiresAt = null;

            await _db.SaveChangesAsync();

            return RedirectToAction("Login", new { reason = "reset_done" });
        }

        private static string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Base64UrlEncode(bytes);
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}