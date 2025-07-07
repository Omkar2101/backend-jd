using MongoDB.Driver;
using backend_jd_api.Models;
using backend_jd_api.Config;

namespace backend_jd_api.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<JobDescription> _jobs;

        public MongoDbContext(AppSettings settings)
        {
            var client = new MongoClient(settings.Database.ConnectionString);
            _database = client.GetDatabase(settings.Database.DatabaseName);
            _jobs = _database.GetCollection<JobDescription>(settings.Database.CollectionName);
        }

        public IMongoCollection<JobDescription> Jobs => _jobs;
        public IMongoDatabase Database => _database;

        // Simple CRUD operations
        public async Task<JobDescription> CreateJobAsync(JobDescription job)
        {
            await _jobs.InsertOneAsync(job);
            return job;
        }

        public async Task<JobDescription?> GetJobAsync(string id)
        {
            return await _jobs.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<JobDescription>> GetAllJobsAsync(int skip = 0, int limit = 20)
        {
            return await _jobs
                .Find(_ => true)
                .Skip(skip)
                .Limit(limit)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<JobDescription> UpdateJobAsync(JobDescription job)
        {
            await _jobs.ReplaceOneAsync(x => x.Id == job.Id, job);
            return job;
        }

        public async Task<bool> DeleteJobAsync(string id)
        {
            var result = await _jobs.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<List<JobDescription>> GetJobsByUserEmailAsync(string email)
        {
            return await _jobs
                .Find(x => x.UserEmail == email)
                .SortByDescending(x => x.CreatedAt)
                .ToListAsync();
        }
    }
}