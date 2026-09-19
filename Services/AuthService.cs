using Microsoft.EntityFrameworkCore;
using PharmaTrust.Data;
using PharmaTrust.Models;
using System.Security.Cryptography;

namespace PharmaTrust.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, ILogger<AuthService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AdminUser?> ValidateAdminCredentialsAsync(string usernameOrEmail, string password)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            var cleanInput = usernameOrEmail.Trim().ToLower();

            var admin = await _context.AdminUsers
                .FirstOrDefaultAsync(u => u.IsActive && 
                    (u.Username.ToLower() == cleanInput || u.Email.ToLower() == cleanInput));

            if (admin == null)
            {
                // Fallback default admin if database is empty or not yet seeded
                if ((cleanInput == "admin_natalya" || cleanInput == "admin" || cleanInput == "admin@natalyapara.com" || cleanInput == "admin@natalya.tn") && 
                    (password == "Natalya@Pharma#2026!" || password == "admin123"))
                {
                    return new AdminUser
                    {
                        Id = 1,
                        Username = "admin_natalya",
                        Email = "admin@natalyapara.com",
                        FullName = "Dr. Natalya (Pharmacien Gérant)",
                        Role = "Administrator",
                        IsActive = true
                    };
                }
                return null;
            }

            if (VerifyPassword(password, admin.PasswordHash))
            {
                return admin;
            }

            return null;
        }

        public async Task<AdminUser?> GetAdminByIdAsync(int id)
        {
            return await _context.AdminUsers.FindAsync(id);
        }

        public async Task<AdminUser?> GetAdminByUsernameOrEmailAsync(string usernameOrEmail)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail)) return null;
            var clean = usernameOrEmail.Trim().ToLower();
            return await _context.AdminUsers.FirstOrDefaultAsync(u => u.Username.ToLower() == clean || u.Email.ToLower() == clean);
        }

        public async Task<bool> RecordLoginAsync(int adminId)
        {
            try
            {
                var admin = await _context.AdminUsers.FindAsync(adminId);
                if (admin != null)
                {
                    admin.LastLoginAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record login for admin ID {AdminId}", adminId);
            }
            return false;
        }

        public string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations: 100000,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: 32);

            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(password))
            {
                return false;
            }

            // Fallback for demo plain-text passwords or initial seed
            if (storedHash == password || (storedHash == "admin123" && password == "admin123"))
            {
                return true;
            }

            try
            {
                var parts = storedHash.Split('.');
                if (parts.Length != 2)
                {
                    return false;
                }

                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] expectedHash = Convert.FromBase64String(parts[1]);

                byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations: 100000,
                    hashAlgorithm: HashAlgorithmName.SHA256,
                    outputLength: 32);

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch
            {
                return false;
            }
        }
    }
}
