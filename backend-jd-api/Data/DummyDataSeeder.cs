using backend_jd_api.Models;
using MongoDB.Driver;

namespace backend_jd_api.Data
{
    public static class DummyDataSeeder
    {
        public static async Task SeedDummyDataAsync(IMongoDatabase database)
        {
            var collection = database.GetCollection<JobDescription>("JobDescriptions");

            // // Check if collection is empty
            // if ((await collection.CountDocumentsAsync(FilterDefinition<JobDescription>.Empty)) > 0)
            // {
            //     return; // Data already exists
            // }

            var dummyJobs = new List<JobDescription>
            {
                new JobDescription
                {
                    UserEmail = "john.doe@example.com",
                    OriginalText = "Looking for a dynamic and energetic young salesman to join our fast-paced team. Must be willing to work long hours. Strong man needed for this challenging role. Minimum 10 years experience required.",
                    ImprovedText = "Seeking a dynamic sales professional to join our fast-paced team. The ideal candidate will be adaptable, resilient, and committed to excellence. Experience in consultative selling and relationship building is essential.",
                    FileName = "sales-position.pdf",
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    Analysis = new AnalysisResult
                    {
                        bias_score = 0.65,
                        inclusivity_score = 0.45,
                        clarity_score = 0.80,
                        Issues = new List<Issue>
                        {
                            new Issue
                            {
                                Type = "Gender Bias",
                                Text = "salesman, Strong man",
                                Severity = "high",
                                Explanation = "Uses gender-specific terms that may discourage female applicants"
                            },
                            new Issue
                            {
                                Type = "Age Discrimination",
                                Text = "young",
                                Severity = "high",
                                Explanation = "Age-specific term that may discriminate against older candidates"
                            }
                        },
                        suggestions = new List<Suggestion>
                        {
                            new Suggestion
                            {
                                Original = "salesman",
                                Improved = "sales professional",
                                rationale = "Gender-neutral term that is more inclusive",
                                Category = "Bias"
                            },
                            new Suggestion
                            {
                                Original = "Strong man needed",
                                Improved = "Strong candidate needed",
                                rationale = "Removes gender-specific language",
                                Category = "Bias"
                            }
                        },
                        seo_keywords = new List<string>
                        {
                            "sales professional",
                            "consultative selling",
                            "relationship building",
                            "sales experience",
                            "customer relationship management"
                        }
                    }
                },
                new JobDescription
                {
                    UserEmail = "jane.smith@example.com",
                    OriginalText = "Tech ninja wanted! Looking for a rockstar developer who can crush code 24/7. Must be a recent graduate from a top university. Beer pong skills a plus!",
                    ImprovedText = "Seeking an experienced software developer with strong problem-solving skills. The ideal candidate will demonstrate expertise in modern development practices and contribute to our collaborative team environment.",
                    FileName = "developer-position.docx",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    Analysis = new AnalysisResult
                    {
                        bias_score = 0.75,
                        inclusivity_score = 0.55,
                        clarity_score = 0.60,
                        Issues = new List<Issue>
                        {
                            new Issue
                            {
                                Type = "Unprofessional Language",
                                Text = "ninja, rockstar, crush code",
                                Severity = "medium",
                                Explanation = "Casual language may not be inclusive and can deter serious candidates"
                            },
                            new Issue
                            {
                                Type = "Work-Life Balance",
                                Text = "24/7",
                                Severity = "medium",
                                Explanation = "Suggests unrealistic work expectations"
                            }
                        },
                        suggestions = new List<Suggestion>
                        {
                            new Suggestion
                            {
                                Original = "Tech ninja wanted!",
                                Improved = "Software Developer Position",
                                rationale = "More professional and clear job title",
                                Category = "Clarity"
                            },
                            new Suggestion
                            {
                                Original = "crush code 24/7",
                                Improved = "write efficient, maintainable code",
                                rationale = "More professional description of job responsibilities",
                                Category = "Clarity"
                            }
                        },
                        seo_keywords = new List<string>
                        {
                            "software developer",
                            "problem-solving",
                            "modern development",
                            "collaborative team",
                            "technical expertise"
                        }
                    }
                }
            };

            await collection.InsertManyAsync(dummyJobs);
        }
    }
}
