using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

namespace TakeCareOfUs.Services
{
    public class NameValidationService
    {
        private readonly string apiKey;
        private readonly HashSet<string> fakeNamePatterns = new HashSet<string>
        {
            "test", "abc", "123", "asdf", "qwerty", "name", "fake", "none", "null", "user", "admin"
        };

        private static readonly HttpClient client = new HttpClient();

        public NameValidationService()
        {
            // إضافة تهيئة رأس المصادقة لمرة واحدة عند إنشاء الكائن
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        public bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            name = name.Trim().ToLower();

            if (name.Length < 3 || name.Distinct().Count() <= 2)
                return false;

            foreach (var pattern in fakeNamePatterns)
            {
                if (name.Contains(pattern))
                    return false;
            }

            if (!Regex.IsMatch(name, @"^[a-zA-Z\s]+$"))
                return false;

            return true;
        }

        public async Task<(int score, string explanation, string description)> CheckNameAsync(string name)
        {
            var instruction = @"
You are a professional name validation expert. 
Your task is to evaluate whether a given name is likely to be a real human name or an artificial, fake, or suspicious one.

You must return a JSON object with exactly these three fields:
- score: an integer from 0 to 100, representing how realistic the name appears.
- explanation: a short, direct sentence (1–2 lines) that summarizes your judgment.
- description: a clear, detailed explanation that justifies the score. It should include analysis of structure, capitalization, use of known name patterns, cultural appropriateness, and any red flags (e.g., random words, tech terms, fake generators, etc).

Guidelines:
- Be specific, logical, and concise.
- Avoid vague or generic responses.
- The explanation should be user-facing and plain-language.
- The description should be technical, analytical, and well-structured.

Example:
{
  ""score"": 22,
  ""explanation"": ""The name uses generic fantasy words and does not resemble a real personal name."",
  ""description"": ""The input 'Land of Magic' lacks typical name structure (no first/last name). Words like 'Land' and 'Magic' are not used as names in real contexts. There’s no cultural or linguistic alignment, and the capitalization mimics titles rather than names. It resembles fictional or game-related content.""
}
";

            var requestData = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "system", content = instruction },
                    new { role = "user", content = $"Name: {name}" }
                },
                max_tokens = 300 // رفع العدد لتفصيل أفضل
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
                return (-1, "API request failed", "");

            var responseString = await response.Content.ReadAsStringAsync();

            Console.WriteLine("=== GPT RAW RESPONSE ===");
            Console.WriteLine(responseString);

            try
            {
                dynamic fullResponse = JsonConvert.DeserializeObject(responseString);
                string messageContent = fullResponse.choices[0].message.content;

                // تنظيف النص لأخذ فقط JSON إن وجد ضمن الرسالة
                int start = messageContent.IndexOf('{');
                int end = messageContent.LastIndexOf('}');
                if (start >= 0 && end >= 0 && end > start)
                    messageContent = messageContent.Substring(start, end - start + 1);

                var result = JsonConvert.DeserializeObject<NameEvaluationResult>(messageContent);

                if (result == null)
                    return (-1, "Failed to parse JSON from response", "");

                return (result.score, result.explanation, result.description);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Parsing error: " + ex.Message);
                return (-1, "Error parsing GPT response", "");
            }
        }

        private class NameEvaluationResult
        {
            public int score { get; set; }
            public string explanation { get; set; }
            public string description { get; set; }
        }
    }
}
