using System.Threading.Tasks;
using PharmaTrust.Models;

namespace PharmaTrust.Services
{
    public interface ITelegramService
    {
        TelegramSettings GetSettings();
        void SaveSettings(TelegramSettings settings);
        Task<bool> SendOrderNotificationAsync(Order order);
        Task<(bool Success, string Message)> SendTestMessageAsync(string botToken, string chatId);
        string BuildWhatsAppUrl(Order order);
    }
}
