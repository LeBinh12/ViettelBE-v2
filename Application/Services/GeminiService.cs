using System.Text;
using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using Domain.Abstractions;
using Microsoft.Extensions.Configuration;
using Share;

namespace Application.Services;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _url;
    private readonly IServicePackageRepository _repository;

    public GeminiService(IConfiguration configuration, HttpClient httpClient, IServicePackageRepository repository)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Gemini:ApiKey"]
                  ?? throw new ArgumentNullException("Gemini:ApiKey");

        _url =
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";
        _repository = repository;
    }

    public async Task<Result<string>> AskGeminiAsync(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return await Result<string>.FailureAsync("Vui lòng nhập câu hỏi.");

        if (userMessage.Length > 50000)
            return await Result<string>.FailureAsync("Bạn không được nhập quá 1000 ký tự.");

        var prompt = $"Hãy trả lời người dùng bằng tiếng Việt dựa trên yêu cầu sau: \"{userMessage}\". " +
                     $"Trả lời ngắn gọn, dễ hiểu.";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(_url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return await Result<string>.FailureAsync(
                    $"Gemini API error: {response.StatusCode}, {responseContent}"
                );
            }

            using var doc = JsonDocument.Parse(responseContent);

            if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0)
            {
                var contentObj = candidates[0].GetProperty("content");

                if (contentObj.TryGetProperty("parts", out var partsArray) &&
                    partsArray.GetArrayLength() > 0)
                {
                    var answer = partsArray[0].GetProperty("text").GetString();

                    return await Result<string>.SuccessAsync(answer, "Gemini trả lời thành công!");
                }
            }

            return await Result<string>.FailureAsync("Không có câu trả lời từ Gemini.");
        }
        catch (Exception ex)
        {
            return await Result<string>.FailureAsync($"Lỗi khi gọi Gemini API: {ex.Message}");
        }
    }

    public async Task<string> BuildKnowledgeBaseAsync()
    {
        var packages = await _repository.GetAllAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Dưới đây là tất cả gói dịch vụ trong hệ thống:");

        foreach (var p in packages)
        {
            sb.AppendLine($"- ID: {p.Id}");
            sb.AppendLine($"  Tên: {p.PackageName}");
            sb.AppendLine($"  Giá: {p.Price:N0} VNĐ");
            sb.AppendLine($"  Mô tả: {p.Description}");
            sb.AppendLine($"  Thời hạn: {p.DurationMonths} tháng");
            sb.AppendLine($"  Danh mục: {p.Category?.Name}");
            sb.AppendLine();
        }

        sb.AppendLine("Chỉ sử dụng dữ liệu trên để trả lời. Không được bịa hay tạo thông tin mới.");

        return sb.ToString();
    }

    public async Task<Result<string>> AskGeminiWithDataAsync(GeminiRequest request)
    {
        // 1) Lấy dữ liệu từ DB
        string knowledgeBase = await BuildKnowledgeBaseAsync();

        // 2) Tạo prompt với format đặc biệt để AI trả về gói có thể click
        string finalPrompt = $@"
Bạn là trợ lý của hệ thống gói dịch vụ VietDev.

Dưới đây là dữ liệu nội bộ của hệ thống:
{knowledgeBase}

Yêu cầu của người dùng:
{request.Prompt}

⚠️ Quy tắc trả lời:
1. Chỉ được trả lời dựa trên dữ liệu ở trên.
2. Không được bịa thêm thông tin.
3. Khi giới thiệu gói dịch vụ, PHẢI sử dụng format đặc biệt sau:
   [PACKAGE:ID|TÊN_GÓI]
   
   Ví dụ: Tôi khuyên bạn nên dùng [PACKAGE:123e4567-e89b-12d3-a456-426614174000|Gói Cơ Bản A]
   
4. Có thể đưa ra nhiều gói cùng lúc.
5. Nếu không tìm thấy thông tin trong dữ liệu, hãy trả lời: 
   ""Xin lỗi, hệ thống không có dữ liệu cho yêu cầu này.""
6. Sử dụng markdown để format text: **bold**, ### heading, * bullet points
";

        // 3) Gọi lại hàm AskGeminiAsync để dùng chung logic
        return await AskGeminiAsync(finalPrompt);
    }
}