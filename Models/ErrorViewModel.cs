namespace PharmaTrust.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public int StatusCode { get; set; } = 500;

        public string Title { get; set; } = "Une erreur inattendue est survenue";

        public string Description { get; set; } = "Une interruption temporaire a eu lieu. Notre équipe en officine a été alertée pour rétablir la situation au plus vite.";
    }
}
