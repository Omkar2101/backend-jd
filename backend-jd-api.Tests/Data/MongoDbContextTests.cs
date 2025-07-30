using Xunit;
using Moq;
using MongoDB.Driver;
using backend_jd_api.Data;
using backend_jd_api.Models;
using backend_jd_api.Config;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Threading;

namespace backend_jd_api.Tests.Data
{
    public class MongoDbContextTests
    {
        private readonly Mock<IMongoCollection<JobDescription>> _mockCollection;
        private readonly AppSettings _testSettings;

        public MongoDbContextTests()
        {
            _mockCollection = new Mock<IMongoCollection<JobDescription>>();
            _testSettings = new AppSettings
            {
                Database = new DatabaseConfig
                {
                    ConnectionString = "mongodb://localhost:27017",
                    DatabaseName = "TestJobAnalyzerDB",
                    CollectionName = "TestJobDescriptions"
                }
            };
        }

        [Fact]
        public void Constructor_ShouldCreateValidSettings()
        {
            Assert.NotNull(_testSettings);
            Assert.NotNull(_testSettings.Database);
            Assert.Equal("mongodb://localhost:27017", _testSettings.Database.ConnectionString);
            Assert.Equal("TestJobAnalyzerDB", _testSettings.Database.DatabaseName);
            Assert.Equal("TestJobDescriptions", _testSettings.Database.CollectionName);
        }

        [Fact]
        public async Task CreateJobAsync_ShouldInsertJob()
        {
            var testJob = CreateTestJob();
            var context = CreateTestContext();

            _mockCollection.Setup(c => c.InsertOneAsync(testJob, null, default))
                .Returns(Task.CompletedTask);

            var result = await context.CreateJobAsync(testJob);

            Assert.Equal(testJob, result);
            _mockCollection.Verify(c => c.InsertOneAsync(testJob, null, default), Times.Once);
        }

        [Fact]
        public async Task GetJobAsync_ShouldReturnJob_WhenExists()
        {
            var jobId = ObjectId.GenerateNewId().ToString();
            var expectedJob = CreateTestJob();
            expectedJob.Id = jobId;

            var mockCursor = new Mock<IAsyncCursor<JobDescription>>();
            mockCursor.Setup(_ => _.Current).Returns(new List<JobDescription> { expectedJob });
            mockCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockCollection.Setup(c => c.FindSync(
                It.IsAny<FilterDefinition<JobDescription>>(),
                It.IsAny<FindOptions<JobDescription, JobDescription>>(),
                It.IsAny<CancellationToken>()))
                .Returns(mockCursor.Object);

            var context = CreateTestContext();

            var result = await context.GetJobAsync(jobId);

            Assert.NotNull(result);
            Assert.Equal(jobId, result.Id);
        }

        [Fact]
        public async Task GetJobAsync_ShouldReturnNull_WhenNotExists()
        {
            var mockCursor = new Mock<IAsyncCursor<JobDescription>>();
            mockCursor.Setup(_ => _.Current).Returns(new List<JobDescription>());
            mockCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockCollection.Setup(c => c.FindSync(
                It.IsAny<FilterDefinition<JobDescription>>(),
                It.IsAny<FindOptions<JobDescription, JobDescription>>(),
                It.IsAny<CancellationToken>()))
                .Returns(mockCursor.Object);

            var context = CreateTestContext();

            var result = await context.GetJobAsync("nonexistentid");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllJobsAsync_ShouldReturnList()
        {
            var testJobs = new List<JobDescription> { CreateTestJob(), CreateTestJob() };

            var mockCursor = new Mock<IAsyncCursor<JobDescription>>();
            mockCursor.Setup(_ => _.Current).Returns(testJobs);
            mockCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockCollection.Setup(c => c.FindSync(
                It.IsAny<FilterDefinition<JobDescription>>(),
                It.IsAny<FindOptions<JobDescription, JobDescription>>(),
                It.IsAny<CancellationToken>()))
                .Returns(mockCursor.Object);

            var context = CreateTestContext();

            var result = await context.GetAllJobsAsync();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task UpdateJobAsync_ShouldReplaceJob()
        {
            var testJob = CreateTestJob();
            var mockReplaceResult = new Mock<ReplaceOneResult>();

            _mockCollection.Setup(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<JobDescription>>(),
                testJob,
                It.IsAny<ReplaceOptions>(),
                default))
                .ReturnsAsync(mockReplaceResult.Object);

            var context = CreateTestContext();

            var result = await context.UpdateJobAsync(testJob);

            Assert.Equal(testJob, result);
            _mockCollection.Verify(c => c.ReplaceOneAsync(
                It.IsAny<FilterDefinition<JobDescription>>(),
                testJob,
                It.IsAny<ReplaceOptions>(),
                default), Times.Once);
        }

        [Fact]
        public async Task DeleteJobAsync_ShouldReturnTrue_WhenDeleted()
        {
            var mockDeleteResult = new Mock<DeleteResult>();
            mockDeleteResult.Setup(r => r.DeletedCount).Returns(1);

            _mockCollection.Setup(c => c.DeleteOneAsync(
                It.IsAny<FilterDefinition<JobDescription>>(),
                default))
                .ReturnsAsync(mockDeleteResult.Object);

            var context = CreateTestContext();

            var result = await context.DeleteJobAsync("someid");

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteJobAsync_ShouldReturnFalse_WhenNotDeleted()
        {
            var mockDeleteResult = new Mock<DeleteResult>();
            mockDeleteResult.Setup(r => r.DeletedCount).Returns(0);

            _mockCollection.Setup(c => c.DeleteOneAsync(
                It.IsAny<FilterDefinition<JobDescription>>(),
                default))
                .ReturnsAsync(mockDeleteResult.Object);

            var context = CreateTestContext();

            var result = await context.DeleteJobAsync("someid");

            Assert.False(result);
        }

        [Fact]
        public async Task GetJobsByUserEmailAsync_ShouldReturnUserJobs()
        {
            var userEmail = "test@example.com";
            var userJobs = new List<JobDescription> { CreateTestJob(), CreateTestJob() };

            var mockCursor = new Mock<IAsyncCursor<JobDescription>>();
            mockCursor.Setup(_ => _.Current).Returns(userJobs);
            mockCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockCollection.Setup(c => c.FindSync(
                It.IsAny<FilterDefinition<JobDescription>>(),
                It.IsAny<FindOptions<JobDescription, JobDescription>>(),
                It.IsAny<CancellationToken>()))
                .Returns(mockCursor.Object);

            var context = CreateTestContext();

            var result = await context.GetJobsByUserEmailAsync(userEmail);

            Assert.NotNull(result);
            Assert.All(result, job => Assert.Equal(userEmail, job.UserEmail));
        }

        private JobDescription CreateTestJob()
        {
            return new JobDescription
            {
                Id = ObjectId.GenerateNewId().ToString(),
                UserEmail = "test@example.com",
                OriginalText = "Test job description",
                ImprovedText = "Improved test job description",
                FileName = "test.txt",
                CreatedAt = DateTime.UtcNow
            };
        }

        private TestableMongoDbContext CreateTestContext()
        {
            return new TestableMongoDbContext(_mockCollection.Object, null, _testSettings);
        }
    }

    public class TestableMongoDbContext : MongoDbContext
    {
        private readonly IMongoCollection<JobDescription> _testCollection;

        public TestableMongoDbContext(IMongoCollection<JobDescription> collection, IMongoDatabase database, AppSettings settings)
            : base(settings)
        {
            _testCollection = collection;
        }

        public new async Task<JobDescription> CreateJobAsync(JobDescription job)
        {
            await _testCollection.InsertOneAsync(job);
            return job;
        }

        public new async Task<JobDescription?> GetJobAsync(string id)
        {
            var filter = Builders<JobDescription>.Filter.Eq(j => j.Id, id);
            using var cursor = _testCollection.FindSync(filter);
            if (cursor.MoveNext())
            {
                var batch = cursor.Current;
                foreach (var job in batch)
                {
                    return job;
                }
            }
            return null;
        }

        public new async Task<List<JobDescription>> GetAllJobsAsync(int skip = 0, int limit = 20)
        {
            var filter = Builders<JobDescription>.Filter.Empty;
            using var cursor = _testCollection.FindSync(filter);
            var results = new List<JobDescription>();
            while (cursor.MoveNext())
            {
                results.AddRange(cursor.Current);
            }
            return results;
        }

        public new async Task<JobDescription> UpdateJobAsync(JobDescription job)
        {
            await _testCollection.ReplaceOneAsync(
                Builders<JobDescription>.Filter.Eq(j => j.Id, job.Id),
                job);
            return job;
        }

        public new async Task<bool> DeleteJobAsync(string id)
        {
            var result = await _testCollection.DeleteOneAsync(
                Builders<JobDescription>.Filter.Eq(j => j.Id, id));
            return result.DeletedCount > 0;
        }

        public new async Task<List<JobDescription>> GetJobsByUserEmailAsync(string email)
        {
            var filter = Builders<JobDescription>.Filter.Eq(j => j.UserEmail, email);
            using var cursor = _testCollection.FindSync(filter);
            var results = new List<JobDescription>();
            while (cursor.MoveNext())
            {
                results.AddRange(cursor.Current);
            }
            return results;
        }
    }
}
