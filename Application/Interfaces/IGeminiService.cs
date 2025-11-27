using Application.DTOs;
using Nethereum.Contracts.QueryHandlers.MultiCall;
using Share;

namespace Application.Interfaces;

public interface IGeminiService
{
    Task<Result<string>> AskGeminiAsync(string prompt);
    Task<Result<string>> AskGeminiWithDataAsync(GeminiRequest request);
}