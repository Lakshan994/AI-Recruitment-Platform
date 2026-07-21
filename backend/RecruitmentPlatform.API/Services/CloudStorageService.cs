using Microsoft.AspNetCore.Http;
using RecruitmentPlatform.API.Interfaces;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RecruitmentPlatform.API.Services
{
    public class CloudStorageService : ICloudStorageService
    {
        private readonly string _secureStorageFolder;
        private readonly string _encryptionKey = "RecruitmentSecureStorageKeyExample!";

        public CloudStorageService()
        {
            // Simulated private cloud storage directory, completely outside of wwwroot for absolute security!
            _secureStorageFolder = Path.Combine(Directory.GetCurrentDirectory(), "SecureCloudStoragePrivateBucket");
            if (!Directory.Exists(_secureStorageFolder))
            {
                Directory.CreateDirectory(_secureStorageFolder);
            }
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folder, string fileName)
        {
            var bucketFolder = Path.Combine(_secureStorageFolder, folder);
            if (!Directory.Exists(bucketFolder))
            {
                Directory.CreateDirectory(bucketFolder);
            }

            var filePath = Path.Combine(bucketFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return simulated "s3://private-recruitment-bucket/{folder}/{fileName}" cloud uri
            return $"s3://private-recruitment-bucket/{folder}/{fileName}";
        }

        public Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl) || !fileUrl.StartsWith("s3://private-recruitment-bucket/"))
                return Task.CompletedTask;

            var relativePath = fileUrl.Replace("s3://private-recruitment-bucket/", "").Replace('/', Path.DirectorySeparatorChar);
            var filePath = Path.Combine(_secureStorageFolder, relativePath);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return Task.CompletedTask;
        }

        public string GeneratePresignedUrl(string fileUrl, int expirationMinutes = 15)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return string.Empty;

            var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes).Ticks;
            var payload = $"{fileUrl}|{expiresAt}";

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_encryptionKey)))
            {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var signature = Convert.ToBase64String(hashBytes)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .TrimEnd('='); // URL-safe Base64

                // Return token payload
                return $"{fileUrl}?expires={expiresAt}&sig={signature}";
            }
        }

        public bool ValidatePresignedToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            try
            {
                // Parse fileUrl, expires, sig from url path or query params
                var parts = token.Split(new[] { '?' }, 2);
                if (parts.Length < 2) return false;

                var fileUrl = parts[0];
                var queryParams = parts[1].Split('&');
                long expiresTicks = 0;
                string sig = string.Empty;

                foreach (var q in queryParams)
                {
                    var kv = q.Split('=', 2);
                    if (kv.Length < 2) continue;
                    if (kv[0] == "expires") expiresTicks = long.Parse(kv[1]);
                    if (kv[0] == "sig") sig = kv[1];
                }

                if (expiresTicks == 0 || string.IsNullOrEmpty(sig))
                    return false;

                // Check expiration
                if (DateTime.UtcNow.Ticks > expiresTicks)
                    return false;

                // Recalculate signature and match
                var payload = $"{fileUrl}|{expiresTicks}";
                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_encryptionKey)))
                {
                    var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                    var calculatedSig = Convert.ToBase64String(hashBytes)
                        .Replace("+", "-")
                        .Replace("/", "_")
                        .TrimEnd('=');

                    return string.Equals(sig, calculatedSig, StringComparison.Ordinal);
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
