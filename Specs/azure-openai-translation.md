# Azure OpenAI Translation Service

## Overview

The `AzureOpenAITranslationService` is an enhanced translation service that uses Azure OpenAI (GPT-4) to provide context-aware, high-quality subtitle translations with automatic OCR artifact cleanup.

## Key Features

### 1. Context-Aware Translation
- Maintains a chat history of previous subtitles (up to last 20 messages)
- Uses previous context to understand narrative flow and maintain consistency
- Only translates the current subtitle, but leverages history for better accuracy

### 2. Session Management
- Automatically creates a new translation session when the foreground app changes
- Each streaming app (Netflix, HBO, etc.) gets its own isolated session
- Prevents context mixing between different shows or apps

### 3. OCR Artifact Cleanup
The system prompt instructs the LLM to automatically clean common OCR errors:
- Random special characters (|, ~, `, etc.)
- Broken words or extra spaces
- Misrecognized characters (0 vs O, 1 vs I/l, etc.)
- Doubled letters or punctuation

### 4. Consistent Entity Translation
The LLM is instructed to track and maintain consistent translations for:
- Character names (translate or keep original based on convention)
- Place names
- Product/brand names (usually kept original)
- Technical terms

## Implementation Details

### Service Architecture

```
AzureOpenAITranslationService
├── ITranslationService interface
├── Session Management
│   ├── Track current foreground app
│   ├── Reset session on app change
│   └── Maintain chat history per session
└── Azure OpenAI Integration
    ├── System prompt engineering
    ├── Chat completion API
    └── Response processing
```

### Chat History Management

The service maintains a rolling history of the last 20 messages (10 user-assistant exchanges):
- Each subtitle adds a user message and an assistant response
- When history exceeds 20 messages, the oldest messages are removed
- History is cleared when the foreground app changes

### System Prompt

The system prompt is carefully crafted to:
1. Define the role as a subtitle translator
2. Specify OCR cleanup requirements
3. Emphasize using context without including old text in output
4. Define rules for entity translation consistency
5. Preserve tone and style of dialogue

## Configuration

Add the following settings to your Azure configuration:

```
AzureOpenAIEndpoint: https://your-resource.openai.azure.com/
AzureOpenAIKey: your-api-key-here
AzureOpenAIDeployment: gpt-4o
```

## Usage

The service is automatically used by `WorkflowOrchestrator` when configured. No code changes needed in the workflow.

### Switching Between Services

To switch back to traditional Azure Translator:
1. Open `MauiProgram.cs`
2. Change the registration:
   ```csharp
   // From:
   builder.Services.AddSingleton<ITranslationService, AzureOpenAITranslationService>();
   
   // To:
   builder.Services.AddSingleton<ITranslationService, AzureTranslatorService>();
   ```

## Cost Considerations

Azure OpenAI is more expensive than Azure Translator but provides superior quality:

- **Azure Translator**: ~$10/million characters
- **Azure OpenAI (GPT-4)**: ~$30-60/million tokens (~750K words)

For typical subtitle usage (2 hours/day):
- Azure Translator: ~$0.20/month
- Azure OpenAI: ~$6-12/month

The improved translation quality and context awareness typically justify the additional cost for accessibility applications.

## Limitations

1. **Latency**: Azure OpenAI is slower than Azure Translator (~1-3 seconds vs ~200ms)
2. **Rate Limits**: Azure OpenAI has lower rate limits
3. **Availability**: Requires approval for Azure OpenAI access
4. **Cost**: Higher cost per translation

## Benefits Over Traditional Translation

1. **Context Awareness**: Understands narrative and character relationships
2. **OCR Cleanup**: Automatically fixes common OCR errors
3. **Entity Consistency**: Maintains consistent character/place names
4. **Natural Language**: Better handling of idioms and colloquialisms
5. **Adaptive**: Can adjust translation style based on context

## Future Enhancements

Possible improvements:
- Fine-tune prompt based on content type (drama, comedy, documentary)
- Add genre detection to adjust translation style
- Implement caching for frequently seen phrases
- Add confidence scoring for translations
- Support for multiple target languages in single session

## Example Flow

```
1. User watches Netflix
2. Subtitle appears: "H3llo, my nam3 is John"
3. Service detects Netflix is foreground app
4. Adds to chat history with system prompt
5. LLM receives:
   - System: "Clean OCR and translate..."
   - User: "Translate this subtitle: 'H3llo, my nam3 is John'"
   - History: [previous subtitles for context]
6. LLM returns: "Bună, mă numesc John"
   - Cleaned OCR artifacts (3 → e)
   - Proper Romanian translation
7. Result is spoken via TTS
8. Exchange added to history

Later subtitle: "Nice to see you again, John"
9. LLM uses history to maintain "John" (not "Ioan")
10. Returns consistent translation
```

## Testing

To test the service:
1. Configure Azure OpenAI credentials in Settings
2. Navigate to Debug page
3. Enter test text with OCR artifacts (e.g., "H3ll0 W0rld|")
4. Click "Test Translation"
5. Verify OCR cleanup and translation quality

## Troubleshooting

### "Translation service not configured"
- Verify all three settings are filled: endpoint, key, deployment name
- Check endpoint URL format: `https://your-resource.openai.azure.com/`

### Translation is slow
- Normal for Azure OpenAI (1-3 seconds)
- Consider switching to Azure Translator for faster response

### Translation quality issues
- Check the system prompt in `BuildSystemPrompt()`
- Verify GPT-4 or GPT-4o deployment (GPT-3.5 has lower quality)
- Ensure chat history isn't too large (max 20 messages)

### Session not resetting
- Check `ForegroundAppDetector` is working correctly
- Verify app package name detection
- Check logs for "Foreground app changed" messages
