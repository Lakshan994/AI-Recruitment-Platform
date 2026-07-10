using Microsoft.AspNetCore.Mvc;
using RecruitmentPlatform.API.Models;
using System.Text.Json;
using System.Text;

namespace RecruitmentPlatform.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ChatController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> PostMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message cannot be empty.");
            }

            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Gemini API Key is not configured.");
                return StatusCode(500, "Chatbot is currently unavailable.");
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            var systemPrompt = "You are a helpful and professional AI assistant for 'TalentAI', an AI-powered recruitment platform. " +
                "Your purpose is to help users (candidates, recruiters, and hiring managers) understand how to use the platform. " +
                "Keep your answers concise, friendly, and directly relevant to the platform. " +
                "Key features of TalentAI include: " +
                "1. For Candidates: Finding jobs, applying with AI-matched scoring, viewing profile insights. " +
                "2. For Recruiters: Posting jobs, reviewing AI-screened applications, shortlisting candidates. " +
                "3. For Hiring Managers: Viewing hiring dashboards, making final hiring decisions. " +
                "4. For Admins: Managing users and platform analytics. " +
                "Do not hallucinate features. Always be polite and direct.";

            var geminiRequest = new GeminiRequest
            {
                SystemInstruction = new GeminiSystemInstruction
                {
                    Parts = new List<GeminiPart> { new GeminiPart { Text = systemPrompt } }
                },
                Contents = new List<GeminiContent>
                {
                    new GeminiContent
                    {
                        Role = "user",
                        Parts = new List<GeminiPart> { new GeminiPart { Text = request.Message } }
                    }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(geminiRequest), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(url, jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Error from Gemini API: {StatusCode} - {Error}", response.StatusCode, error);
                    return StatusCode(500, "Error generating response from AI.");
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseString);

                var replyText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "I'm sorry, I couldn't understand that.";

                return Ok(new ChatResponse { Reply = replyText });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while calling Gemini API.");
                return StatusCode(500, "Internal server error while processing your request.");
            }
        }
    }
}
