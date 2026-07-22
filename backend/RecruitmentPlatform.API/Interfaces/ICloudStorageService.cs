using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace RecruitmentPlatform.API.Interfaces
{
	public interface ICloudStorageService
	{
		/// <summary>
		/// Uploads a file to simulated secure cloud storage bucket and returns a unique key/url.
		/// </summary>
		Task<string> UploadFileAsync(IFormFile file, string folder, string fileName);

		/// <summary>
		/// Deletes a file from simulated secure cloud storage.
		/// </summary>
		Task DeleteFileAsync(string fileUrl);

		/// <summary>
		/// Generates a secure, temporary, signed download link.
		/// </summary>
		string GeneratePresignedUrl(string fileUrl, int expirationMinutes = 15);

		/// <summary>
		/// Validates a presigned token/url signature.
		/// </summary>
		bool ValidatePresignedToken(string token);
	}
}
