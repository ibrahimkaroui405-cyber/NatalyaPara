using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PharmaTrust.Models;

namespace PharmaTrust.Services
{
    public class TelegramService : ITelegramService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<TelegramService> _logger;
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private readonly string _settingsFilePath;
        private static readonly object _lock = new object();

        public TelegramService(
            IWebHostEnvironment env, 
            IConfiguration config, 
            ILogger<TelegramService> logger)
        {
            _env = env;
            _config = config;
            _logger = logger;
            var appDataDir = Path.Combine(_env.ContentRootPath, "App_Data");
            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }
            _settingsFilePath = Path.Combine(appDataDir, "telegram_settings.json");
        }

        public TelegramSettings GetSettings()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_settingsFilePath))
                    {
                        var json = File.ReadAllText(_settingsFilePath);
                        var settings = JsonSerializer.Deserialize<TelegramSettings>(json);
                        if (settings != null)
                        {
                            return settings;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erreur lors de la lecture des paramètres Telegram depuis {File}", _settingsFilePath);
                }

                // Fallback to IConfiguration or defaults
                var defaultSettings = new TelegramSettings
                {
                    IsEnabled = _config.GetValue<bool>("Telegram:Enabled", true),
                    BotToken = _config.GetValue<string>("Telegram:BotToken") ?? string.Empty,
                    ChatId = _config.GetValue<string>("Telegram:ChatId") ?? string.Empty,
                    OwnerWhatsApp = _config.GetValue<string>("Telegram:OwnerWhatsApp") ?? "21621040266"
                };

                return defaultSettings;
            }
        }

        public void SaveSettings(TelegramSettings settings)
        {
            lock (_lock)
            {
                try
                {
                    var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(_settingsFilePath, json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la sauvegarde des paramètres Telegram dans {File}", _settingsFilePath);
                }
            }
        }

        public async Task<bool> SendOrderNotificationAsync(Order order)
        {
            var settings = GetSettings();
            if (!settings.IsEnabled)
            {
                _logger.LogInformation("Notifications Telegram désactivées.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(settings.BotToken) || string.IsNullOrWhiteSpace(settings.ChatId))
            {
                _logger.LogWarning("Impossible d'envoyer la notification Telegram : BotToken ou ChatId non renseigné.");
                return false;
            }

            var sb = new StringBuilder();
            sb.AppendLine("🌿 <b>NOUVELLE COMMANDE — NATALYA PARA</b> 🌿");
            sb.AppendLine();
            sb.AppendLine($"📦 <b>N° Commande :</b> <code>{order.OrderNumber}</code>");
            sb.AppendLine($"👤 <b>Client :</b> {order.CustomerName}");
            sb.AppendLine($"📞 <b>Téléphone :</b> {order.Phone}");
            sb.AppendLine($"📍 <b>Gouvernorat :</b> {order.City}");
            sb.AppendLine($"🏠 <b>Adresse :</b> {order.Address}");
            if (!string.IsNullOrWhiteSpace(order.DeliveryNotes))
            {
                sb.AppendLine($"📝 <b>Note livraison :</b> {order.DeliveryNotes}");
            }
            sb.AppendLine();
            sb.AppendLine("🛒 <b>Articles commandés :</b>");
            if (order.Items != null && order.Items.Count > 0)
            {
                foreach (var item in order.Items)
                {
                    sb.AppendLine($" • <b>{item.Quantity}x</b> {item.ProductName} — <b>{(item.UnitPrice * item.Quantity):F2} DT</b>");
                }
            }
            sb.AppendLine();
            sb.AppendLine($"🚚 <b>Frais de livraison :</b> {order.ShippingFee:F2} DT");
            sb.AppendLine($"💰 <b>TOTAL À ENCAISSER :</b> <b>{order.TotalAmount:F2} DT</b>");
            sb.AppendLine("💵 <b>Règlement :</b> Espèces à la livraison");
            sb.AppendLine($"⏰ <i>Passée le {DateTime.Now:dd/MM/yyyy à HH:mm}</i>");

            var messageText = sb.ToString();

            try
            {
                var url = $"https://api.telegram.org/bot{settings.BotToken.Trim()}/sendMessage";
                var payload = new
                {
                    chat_id = settings.ChatId.Trim(),
                    text = messageText,
                    parse_mode = "HTML",
                    disable_web_page_preview = true
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    settings.LastNotificationSentAt = DateTime.Now;
                    settings.LastNotificationStatus = $"Succès pour {order.OrderNumber}";
                    SaveSettings(settings);
                    _logger.LogInformation("Notification Telegram envoyée avec succès pour la commande {OrderNumber}", order.OrderNumber);
                    return true;
                }
                else
                {
                    settings.LastNotificationStatus = $"Erreur Telegram ({response.StatusCode}): {responseBody}";
                    SaveSettings(settings);
                    _logger.LogError("Échec de l'envoi Telegram : {Response}", responseBody);
                    return false;
                }
            }
            catch (Exception ex)
            {
                settings.LastNotificationStatus = $"Exception: {ex.Message}";
                SaveSettings(settings);
                _logger.LogError(ex, "Exception lors de l'envoi de la notification Telegram pour {OrderNumber}", order.OrderNumber);
                return false;
            }
        }

        public async Task<(bool Success, string Message)> SendTestMessageAsync(string botToken, string chatId)
        {
            if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
            {
                return (false, "Veuillez renseigner le Bot Token et le Chat ID.");
            }

            try
            {
                var url = $"https://api.telegram.org/bot{botToken.Trim()}/sendMessage";
                var text = $"✅ <b>TEST DE NOTIFICATION RÉUSSI !</b>\n\nVotre boutique <b>Natalya Parapharmacie</b> est maintenant connectée à Telegram.\nVous recevrez automatiquement toutes les nouvelles commandes avec leur total et adresse de livraison.\n\n<i>Envoyé le {DateTime.Now:dd/MM/yyyy HH:mm:ss}</i>";

                var payload = new
                {
                    chat_id = chatId.Trim(),
                    text = text,
                    parse_mode = "HTML"
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Message test envoyé avec succès sur votre Telegram !");
                }

                return (false, $"Erreur Telegram ({response.StatusCode}) : {responseBody}");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur de connexion : {ex.Message}");
            }
        }

        public string BuildWhatsAppUrl(Order order)
        {
            var settings = GetSettings();
            var phone = !string.IsNullOrWhiteSpace(settings.OwnerWhatsApp) ? settings.OwnerWhatsApp : "21621040266";

            var sb = new StringBuilder();
            sb.AppendLine("🌿 *NOUVELLE COMMANDE — NATALYA PARA* 🌿");
            sb.AppendLine($"📦 *Commande :* #{order.OrderNumber}");
            sb.AppendLine($"👤 *Client :* {order.CustomerName}");
            sb.AppendLine($"📞 *Tél :* {order.Phone}");
            sb.AppendLine($"📍 *Livraison :* {order.City} ({order.Address})");
            sb.AppendLine();
            sb.AppendLine("🛒 *Détails des articles :*");
            if (order.Items != null && order.Items.Count > 0)
            {
                foreach (var it in order.Items)
                {
                    sb.AppendLine($" • {it.Quantity}x {it.ProductName} ({ (it.UnitPrice * it.Quantity):F2} DT)");
                }
            }
            sb.AppendLine();
            sb.AppendLine($"🚚 *Frais de livraison :* {order.ShippingFee:F2} DT");
            sb.AppendLine($"💰 *TOTAL TTC :* {order.TotalAmount:F2} DT");
            sb.AppendLine("💵 *Paiement :* Espèces à la livraison");

            var encoded = Uri.EscapeDataString(sb.ToString());
            return $"https://api.whatsapp.com/send?phone={phone}&text={encoded}";
        }
    }
}
