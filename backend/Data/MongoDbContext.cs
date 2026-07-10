using MongoDB.Driver;
using RecruitmentPlatform.API.Models;

namespace RecruitmentPlatform.API.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(MongoDbSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            _database = client.GetDatabase(settings.DatabaseName);

            // Create unique index on User.Email
            var emailIndex = new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true });
            Users.Indexes.CreateOne(emailIndex);
        }

        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
        public IMongoCollection<JobPosting> JobPostings => _database.GetCollection<JobPosting>("JobPostings");
        public IMongoCollection<Application> Applications => _database.GetCollection<Application>("Applications");
    }
}
