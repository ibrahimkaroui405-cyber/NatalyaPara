using Microsoft.EntityFrameworkCore;
using PharmaTrust.Data;
using PharmaTrust.Models;
using System.Security.Cryptography;

namespace PharmaTrust.Services
{
    public class AuthService : IAuthService
    {
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private readonly AppDbContext _context;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, ILogger<AuthService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AdminAuthResult> AuthenticateAsync(string usernameOrEmail, string password)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
            {
                return new AdminAuthResult
                {
                    Succeeded = false,
                    ErrorMessage = "L'identifiant et le mot de passe sont requis."
                };
            }

            var cleanInput = usernameOrEmail.Trim().ToLower();

            var admin = await _context.AdminUsers
                .FirstOrDefaultAsync(u => u.IsActive && 
                    (u.Username.ToLower() == cleanInput || u.Email.ToLower() == cleanInput));

            if (admin == null)
            {
                return new AdminAuthResult
                {
                    Succeeded = false,
                    ErrorMessage = "Identifiants invalides. Veuillez vérifier votre nom d'utilisateur/email et mot de passe."
                };
            }

            // Check if account is locked out
            if (admin.LockoutEnd.HasValue && admin.LockoutEnd.Value > DateTime.UtcNow)
            {
                _logger.LogWarning("Connexion bloquée: Le compte '{Username}' est verrouillé jusqu'à {LockoutEnd}", admin.Username, admin.LockoutEnd.Value);
                return new AdminAuthResult
                {
                    Succeeded = false,
                    IsLockedOut = true,
                    LockoutEnd = admin.LockoutEnd.Value,
                    Admin = admin,
                    ErrorMessage = "Compte temporairement verrouillé pour des raisons de sécurité suite à plusieurs tentatives échouées."
                };
            }

            // Verify password using salted PBKDF2
            if (VerifyPassword(password, admin.PasswordHash))
            {
                // Reset failed attempts on successful login
                if (admin.AccessFailedCount > 0 || admin.LockoutEnd.HasValue)
                {
                    admin.AccessFailedCount = 0;
                    admin.LockoutEnd = null;
                }

                admin.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return new AdminAuthResult
                {
                    Succeeded = true,
                    Admin = admin
                };
            }

            // Password mismatch: Increment failed attempts counter
            admin.AccessFailedCount++;
            if (admin.AccessFailedCount >= MaxFailedAttempts)
            {
                admin.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                _logger.LogWarning("COMPTE VERROUILLÉ: {MaxFailedAttempts} tentatives échouées sur le compte '{Username}'. Verrouillé pour {DurationMinutes} minutes.", 
                    MaxFailedAttempts, admin.Username, LockoutDuration.TotalMinutes);
            }

            await _context.SaveChangesAsync();

            return new AdminAuthResult
            {
                Succeeded = false,
                IsLockedOut = admin.LockoutEnd.HasValue && admin.LockoutEnd.Value > DateTime.UtcNow,
                LockoutEnd = admin.LockoutEnd,
                ErrorMessage = "Identifiants invalides. Veuillez vérifier votre nom d'utilisateur/email et mot de passe."
            };
        }

        public async Task<AdminUser?> ValidateAdminCredentialsAsync(string usernameOrEmail, string password)
        {
            var result = await AuthenticateAsync(usernameOrEmail, password);
            return result.Succeeded ? result.Admin : null;
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
                _logger.LogError(ex, "Échec lors de l'enregistrement de connexion pour l'admin ID {AdminId}", adminId);
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
