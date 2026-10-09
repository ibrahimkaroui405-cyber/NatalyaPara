using System;

namespace PharmaTrust.Models
{
    public class TelegramSettings
    {
        public bool IsEnabled { get; set; } = true;
        public string BotToken { get; set; } = string.Empty;
        public string ChatId { get; set; } = string.Empty;
        public string OwnerWhatsApp { get; set; } = "21621040266";
        public DateTime? LastNotificationSentAt { get; set; }
        public string? LastNotificationStatus { get; set; }
    }
}
