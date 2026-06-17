using AICareerCoach.BLL.DTOs.CV;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Entities;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.services
{
    public class CvFeedbackService : ICvFeedbackService
    {
        private readonly Kernel _kernel;
        public CvFeedbackService(Kernel kernel)
        {
            _kernel = kernel;

        }

        public async Task<CvFeedbackDto> GetFeedbackAsync(string cvtext)
        {
            var prompt = $"""
                You are an expert career coach specializing in CV/resume feedback,
                analyze the CV of a user and provide detailed feedback on how to improve it.
                provide an overall summary, an overall score out of 10, List of FeedbackSuggestion  ,list of missing Keywords or missing skills.
                cv:{cvtext}
                """;

            var settings = new OpenAIPromptExecutionSettings
            {
                ResponseFormat = typeof(CvFeedbackDto)
            };
            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            history.AddUserMessage(prompt);
            var result = await chatCompletionService.GetChatMessageContentsAsync(history, settings, _kernel);
            var feedback = JsonSerializer.Deserialize<CvFeedbackDto>(result[0].Content!);
            return feedback;


        }
    }
}
