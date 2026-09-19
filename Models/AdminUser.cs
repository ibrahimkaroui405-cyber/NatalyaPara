using System.ComponentModel.DataAnnotations;

namespace PharmaTrust.Models
{
    public class AdminUser
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Role { get; set; } = "Administrator";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "L'identifiant ou l'adresse e-mail est obligatoire.")]
        [Display(Name = "Identifiant ou E-mail")]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Se souvenir de moi")]
        public bool RememberMe { get; set; } = true;

        public string? ReturnUrl { get; set; }
    }
}
