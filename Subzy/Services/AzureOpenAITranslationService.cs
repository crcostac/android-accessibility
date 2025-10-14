using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using Subzy.Services.Interfaces;
using System.Collections.Generic;

namespace Subzy.Services;

/// <summary>
/// Translation service implementation using Azure OpenAI with chat history for context.
/// Maintains a session per foreground app to provide consistent translations.
/// </summary>
public class AzureOpenAITranslationService : ITranslationService
{
    private readonly ILoggingService _logger;
    private readonly SettingsService _settingsService;
    private readonly ForegroundAppDetector _appDetector;
    private AzureOpenAIClient? _client;
    private ChatClient? _chatClient;
    
    // Session management
    private string? _currentAppPackage;
    private readonly List<ChatMessage> _chatHistory;
    private const int MaxHistoryMessages = 20; // Keep last 10 exchanges (user + assistant)
    
    public bool IsConfigured { get; private set; }

    public AzureOpenAITranslationService(
        ILoggingService logger, 
        SettingsService settingsService,
        ForegroundAppDetector appDetector)
    {
        _logger = logger;
        _settingsService = settingsService;
        _appDetector = appDetector;
        _chatHistory = new List<ChatMessage>();
        InitializeClient();
    }

    private void InitializeClient()
    {
        try
        {
            var settings = _settingsService.LoadSettings();
            
            if (string.IsNullOrWhiteSpace(settings.AzureOpenAIKey) || 
                string.IsNullOrWhiteSpace(settings.AzureOpenAIEndpoint) ||
                string.IsNullOrWhiteSpace(settings.AzureOpenAIDeployment))
            {
                _logger.Warning("Azure OpenAI not fully configured");
                IsConfigured = false;
                return;
            }

            var credential = new AzureKeyCredential(settings.AzureOpenAIKey);
            _client = new AzureOpenAIClient(new Uri(settings.AzureOpenAIEndpoint), credential);
            _chatClient = _client.GetChatClient(settings.AzureOpenAIDeployment);
            
            IsConfigured = true;
            _logger.Info("Azure OpenAI translation service initialized");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to initialize Azure OpenAI", ex);
            IsConfigured = false;
        }
    }

    public async Task<(string translatedText, string detectedLanguage)> TranslateAsync(
        string text,
        string targetLanguage,
        string? sourceLanguage = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (string.Empty, string.Empty);
        }

        if (!IsConfigured)
        {
            _logger.Warning("Translation service not configured");
            return (text, "unknown");
        }

        try
        {
            // Check if foreground app changed - reset session if so
            var currentApp = _appDetector.GetForegroundAppPackageName();
            if (currentApp != _currentAppPackage)
            {
                _logger.Info($"Foreground app changed from '{_currentAppPackage}' to '{currentApp}' - resetting translation session");
                ResetSession();
                _currentAppPackage = currentApp;
            }

            // Build system prompt
            var systemPrompt = BuildSystemPrompt(targetLanguage);
            
            // Build chat messages
            var messages = new List<ChatMessage>();
            messages.Add(ChatMessage.CreateSystemMessage(systemPrompt));
            
            // Add chat history for context
            messages.AddRange(_chatHistory);
            
            // Add current user message
            var userMessage = $"Translate this subtitle text: \"{text}\"";
            messages.Add(ChatMessage.CreateUserMessage(userMessage));

            // Call Azure OpenAI
            var chatCompletion = await _chatClient!.CompleteChatAsync(messages);
            
            var translatedText = chatCompletion.Value.Content[0].Text;
            
            // Update chat history (keep only recent exchanges)
            _chatHistory.Add(ChatMessage.CreateUserMessage(userMessage));
            _chatHistory.Add(ChatMessage.CreateAssistantMessage(translatedText));
            
            // Trim history if too long
            while (_chatHistory.Count > MaxHistoryMessages)
            {
                _chatHistory.RemoveAt(0);
            }
            
            _logger.Info($"Translated text using Azure OpenAI with context (history: {_chatHistory.Count} messages)");
            
            // Detect source language (simplified - could be enhanced with language detection)
            var detectedLang = sourceLanguage ?? "auto-detected";
            
            return (translatedText, detectedLang);
        }
        catch (Exception ex)
        {
            _logger.Error("Azure OpenAI translation failed", ex);
            return (text, "error");
        }
    }

    private string BuildSystemPrompt(string targetLanguage)
    {
        var targetLangName = GetLanguageName(targetLanguage);
        
        return $@"You are a specialized subtitle translator for streaming content. Your task is to translate subtitles to {targetLangName}.

IMPORTANT RULES:
1. ONLY translate the current subtitle text provided in the user message. Do NOT include previous subtitles in your response.
2. The text comes directly from OCR and may contain artifacts. Clean up common OCR errors such as:
   - Random special characters (|, ~, `, etc.)
   - Broken words or extra spaces
   - Misrecognized characters (0 vs O, 1 vs I/l, etc.)
   - Doubled letters or punctuation

3. Use the chat history (previous subtitles) ONLY for context to:
   - Understand the narrative flow
   - Maintain consistent translation of character names
   - Maintain consistent translation of place names
   - Maintain consistent translation of product/brand names
   - Understand pronouns and references

4. Track and maintain a consistent mapping for:
   - Character names (translate or keep original based on common practice)
   - Place names (translate or keep original based on common practice)
   - Product/brand names (usually keep original)
   - Technical terms

5. Keep the translation natural and appropriate for spoken dialogue.
6. Preserve the tone and style of the original (formal, casual, emotional, etc.).
7. Your response should contain ONLY the translated text of the current subtitle, nothing else.
8. Do not add explanations, notes, or metadata.

Remember: Previous messages provide context, but you must ONLY translate and return the current subtitle text.";
    }

    private string GetLanguageName(string languageCode)
    {
        return languageCode.ToLower() switch
        {
            "ro" => "Romanian",
            "en" => "English",
            "es" => "Spanish",
            "fr" => "French",
            "de" => "German",
            "it" => "Italian",
            "pt" => "Portuguese",
            _ => languageCode.ToUpper()
        };
    }

    /// <summary>
    /// Resets the chat session (called when foreground app changes).
    /// </summary>
    public void ResetSession()
    {
        _chatHistory.Clear();
        _currentAppPackage = null;
        _logger.Info("Translation session reset");
    }

    /// <summary>
    /// Reinitializes the translation client with updated settings.
    /// Use this after changing API keys to apply the new credentials.
    /// </summary>
    public void Reinitialize()
    {
        _logger.Info("Reinitializing Azure OpenAI translation service");
        ResetSession();
        InitializeClient();
    }
}
