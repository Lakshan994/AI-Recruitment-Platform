using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.Interfaces;
using RecruitmentPlatform.API.Models;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RecruitmentPlatform.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICloudStorageService _storageService;

        public DocumentsController(AppDbContext context, ICloudStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        // POST /api/documents/upload
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string documentType)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var validTypes = new[] { "Resume", "Certification", "SupportingDocument" };
            if (!validTypes.Contains(documentType))
                return BadRequest(new { message = "Invalid document type. Allowed types: Resume, Certification, SupportingDocument" });

            var userId = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{userId}_{documentType}_{Guid.NewGuid()}{fileExtension}";

            // Upload securely to S3 simulated private vault
            var fileUrl = await _storageService.UploadFileAsync(file, documentType, uniqueFileName);

            // Record in Database
            var doc = new UserDocument
            {
                UserId = userId,
                DocumentType = documentType,
                FileName = file.FileName,
                FileUrl = fileUrl
            };

            _context.UserDocuments.Add(doc);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "File uploaded securely to cloud storage.",
                document = new
                {
                    doc.Id,
                    doc.UserId,
                    doc.DocumentType,
                    doc.FileName,
                    doc.UploadedAt
                }
            });
        }

        // GET /api/documents/my
        [HttpGet("my")]
        public async Task<IActionResult> GetMyDocuments()
        {
            var userId = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var docs = await _context.UserDocuments
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            var response = docs.Select(d => new
            {
                d.Id,
                d.UserId,
                d.DocumentType,
                d.FileName,
                d.UploadedAt
            });

            return Ok(response);
        }

        // GET /api/documents/candidate/{candidateId}
        [HttpGet("candidate/{candidateId}")]
        [Authorize(Roles = "Recruiter,HiringManager,Admin")]
        public async Task<IActionResult> GetCandidateDocuments(string candidateId)
        {
            var docs = await _context.UserDocuments
                .Where(d => d.UserId == candidateId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            var response = docs.Select(d => new
            {
                d.Id,
                d.UserId,
                d.DocumentType,
                d.FileName,
                d.UploadedAt
            });

            return Ok(response);
        }

        // DELETE /api/documents/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var userId = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            var doc = await _context.UserDocuments.FindAsync(id);
            if (doc == null)
                return NotFound(new { message = "Document not found." });

            // Ensure user owns document, or is Recruiter/Admin
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (doc.UserId != userId && role != "Recruiter" && role != "Admin")
                return Forbid();

            // Delete from secure S3 vault
            await _storageService.DeleteFileAsync(doc.FileUrl);

            _context.UserDocuments.Remove(doc);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Document deleted successfully." });
        }

        // GET /api/documents/download/{id} — Direct authorized secure file download / stream
        [HttpGet("download/{id}")]
        [Authorize] // Requires login
        public async Task<IActionResult> DownloadFile(string id)
        {
            var doc = await _context.UserDocuments.FindAsync(id);
            if (doc == null)
                return NotFound(new { message = "Document not found." });

            // Authorize download (owner, or Recruiter/HiringManager/Admin)
            var userId = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);

            if (doc.UserId != userId && role != "Recruiter" && role != "HiringManager" && role != "Admin")
                return Forbid();

            // Extract simulated S3 relative path
            if (string.IsNullOrWhiteSpace(doc.FileUrl) || !doc.FileUrl.StartsWith("s3://private-recruitment-bucket/"))
                return BadRequest(new { message = "Invalid cloud file path." });

            var relativePath = doc.FileUrl.Replace("s3://private-recruitment-bucket/", "").Replace('/', Path.DirectorySeparatorChar);
            var secureStorageFolder = Path.Combine(Directory.GetCurrentDirectory(), "SecureCloudStoragePrivateBucket");
            var filePath = Path.Combine(secureStorageFolder, relativePath);

            if (!System.IO.File.Exists(filePath))
                return NotFound(new { message = "Physical file not found in secure cloud vault." });

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var contentType = "application/octet-stream";
            var ext = Path.GetExtension(filePath).ToLower();
            if (ext == ".pdf") contentType = "application/pdf";
            else if (ext == ".txt") contentType = "text/plain";
            else if (ext == ".png") contentType = "image/png";
            else if (ext == ".jpg" || ext == ".jpeg") contentType = "image/jpeg";

            return File(fileBytes, contentType, doc.FileName);
        }
    }
}
