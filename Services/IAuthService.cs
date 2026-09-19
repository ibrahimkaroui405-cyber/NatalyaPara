using PharmaTrust.Models;

namespace PharmaTrust.Services
{
    public interface IAuthService
    {
        Task<AdminUser?> ValidateAdminCredentialsAsync(string usernameOrEmail, string password);
        Task<AdminUser?> GetAdminByIdAsync(int id);
        Task<AdminUser?> GetAdminByUsernameOrEmailAsync(string usernameOrEmail);
        Task<bool> RecordLoginAsync(int adminId);
        string HashPassword(string password);
        bool VerifyPassword(string password, string storedHash);
    }
}
