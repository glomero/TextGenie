namespace TextGenie.Services
{
    public interface ITextProcessingService
    {
        Task<string> RewriteTextAsync(string text, string style = "professional");
        Task<string> TranslateTextAsync(string text, string targetLanguage = "English");
        Task<string> SummarizeTextAsync(string text, int maxLength = 100);
    }
} 