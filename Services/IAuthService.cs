using PharmaTrust.Models;

namespace PharmaTrust.Services
{
    public class AdminAuthResult
    {
        public bool Succeeded { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public AdminUser? Admin { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public interface IAuthService
    {
        Task<AdminAuthResult> AuthenticateAsync(string usernameOrEmail, string password);
        Task<AdminUser?> ValidateAdminCredentialsAsync(string usernameOrEmail, string password);
        Task<AdminUser?> GetAdminByIdAsync(int id);
        Task<AdminUser?> GetAdminByUsernameOrEmailAsync(string usernameOrEmail);
        Task<bool> RecordLoginAsync(int adminId);
        string HashPassword(string password);
        bool VerifyPassword(string password, string storedHash);
    }
}
