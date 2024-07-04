namespace MagicPot.Backend.Attributes
{
    using System.ComponentModel.DataAnnotations;
    using System.Security.Cryptography;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Web;
    using MagicPot.Backend.Services;

    public class InitDataValidationAttribute : ValidationAttribute
    {
        public static long GetUserIdWithoutValidation(string initData)
        {
            var pairs = HttpUtility.ParseQueryString(initData);
            var usr = JsonSerializer.Deserialize<InitDataUser>(pairs["user"] ?? string.Empty, JsonSerializerOptions.Default);
            return usr?.Id ?? 0;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var initData = value?.ToString();
            if (string.IsNullOrWhiteSpace(initData))
            {
                return ValidationResult.Success;
            }

            var opt = validationContext.GetRequiredService<CachedData>();

            var hash = opt.Options.TelegramBotTokenHash;
            if (string.IsNullOrEmpty(hash))
            {
                return new ValidationResult("Failed to validate: no TokenHash configured");
            }

            var tokenHashBytes = Convert.FromHexString(hash);

            var pairs = HttpUtility.ParseQueryString(initData);
            var providedHashHex = pairs.Get("hash");

            pairs.Remove("hash");
            var data = string.Join(
                '\n',
                pairs.AllKeys.OrderBy(x => x).Select(x => x + "=" + pairs[x]));

            var actualHash = HMACSHA256.HashData(tokenHashBytes, System.Text.Encoding.UTF8.GetBytes(data));
            var actualHashHex = Convert.ToHexString(actualHash);

            if (!StringComparer.OrdinalIgnoreCase.Equals(providedHashHex, actualHashHex))
            {
                return new ValidationResult("Invalid value (validation failed)");
            }

            var usr = JsonSerializer.Deserialize<InitDataUser>(pairs["user"] ?? string.Empty, JsonSerializerOptions.Default);
            if (usr == null || usr.Id == 0)
            {
                return new ValidationResult("No 'user' found");
            }

            var authDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(pairs["auth_date"] ?? "0"));
            if (authDate.Add(opt.Options.TelegramInitDataValidity) < DateTimeOffset.UtcNow)
            {
                return new ValidationResult("Value is expired (too old)");
            }

            return ValidationResult.Success;
        }

        protected sealed class InitDataUser
        {
            [JsonPropertyName("id")]
            public long Id { get; set; }

            [JsonPropertyName("first_name")]
            public string FirstName { get; set; } = string.Empty;

            [JsonPropertyName("last_name")]
            public string LastName { get; set; } = string.Empty;

            [JsonPropertyName("username")]
            public string Username { get; set; } = string.Empty;

            [JsonPropertyName("language_code")]
            public string LanguageCode { get; set; } = string.Empty;

            [JsonPropertyName("is_premium")]
            public bool IsPremium { get; set; }

            [JsonPropertyName("allows_write_to_pm")]
            public bool AllowsWriteToPM { get; set; }
        }
    }
}
