using MongoDB.Bson;
using MongoDB.Driver;

namespace SocketMessages.Configurations
{
    public class ConnectionStringDBMongo
    {
        private readonly string connectionString;
        private readonly string databaseName = "ChatSocket";
        //private readonly string collectionName = "room";

        public ConnectionStringDBMongo()
        {
            var host = GlobalConfiguration.Configuration?["ConnectionStringsMongo:host"];
            //var port = GlobalConfiguration.Configuration?["ConnectionStringsMongo:port"];
            var user = GlobalConfiguration.Configuration?["ConnectionStringsMongo:user"];
            var pass = GlobalConfiguration.Configuration?["ConnectionStringsMongo:pass"];
            //var authSource = GlobalConfiguration.Configuration?["ConnectionStringsMongo:authSource"];

            //connectionString = $"mongodb://{user}:{pass}@{host}:{port}/?authSource={authSource}";
            connectionString = $"mongodb+srv://{user}:{pass}@{host}";
            //mongodb+srv://yorklin:123@bdualabee.qvtb1zs.mongodb.net/
        }

        public Task<IMongoCollection<BsonDocument>> GetCollection(string nameCollection)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(nameCollection);

            return Task.FromResult(collection);
        }
    }    
}