using MongoDB.Bson;
using MongoDB.Driver;
using SocketMessages.Views.Dto;

namespace SocketMessages.Configurations
{
    public class DataAccessMongoDb
    {
        private readonly ConnectionStringDBMongo _connectionStringDB = new();

        public async Task<(bool IsActive, ObjectId UserId)> GetUserIdMongo(string username)
        {
            var usersCollection = await _connectionStringDB.GetCollection("users");

            var filter = Builders<BsonDocument>.Filter.Eq("nickname", username);
            var projection = Builders<BsonDocument>.Projection.Include("_id").Include("online");
            var userDocument = await usersCollection.Find(filter).Project(projection).FirstOrDefaultAsync()
                                ?? throw new Exception("User not found.");
            ObjectId userId = userDocument["_id"].AsObjectId;
            bool isActive = userDocument["online"].AsBoolean;

            return (isActive, userId);
        }

        public async Task<string> GetRoomNameMongo(string groupId)
        {
            ObjectId _Id = ConvertStringToObjectId(groupId);            

            var roomsCollection = await _connectionStringDB.GetCollection("rooms");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", _Id);
            var projection = Builders<BsonDocument>.Projection.Include("name");
            var userDocument = await roomsCollection.Find(filter).Project(projection).FirstOrDefaultAsync()
                                ?? throw new Exception("Group not found.");

            var groupname = userDocument["name"].AsString;

            return groupname;
        }
        public async Task<List<RoomRequestDto>> GetRoomByUserId(string userId)
        {
            var roomsCollection = await _connectionStringDB.GetCollection("rooms");
            var filter = Builders<BsonDocument>.Filter.AnyEq("assignedUsers", userId);
            var projection = Builders<BsonDocument>.Projection
                .Include("_id")
                .Include("name")
                .Include("urlImagen")
                .Include("connectedUsers")
                .Include("assignedUsers");

            var documents = await roomsCollection.Find(filter).Project(projection).ToListAsync();

            var rooms = documents.Select(doc => new RoomRequestDto
            {
                _id = doc["_id"].AsObjectId.ToString(),
                name = doc["name"].BsonType == BsonType.String ? doc["name"].AsString : doc["name"].ToString(),
                urlImagen = doc.Contains("urlImagen") && doc["urlImagen"].BsonType == BsonType.String ?
                    doc["urlImagen"].AsString :
                    null,

                connectedUsers = doc.Contains("connectedUsers") && doc["connectedUsers"].BsonType == BsonType.Array ?
                         doc["connectedUsers"].AsBsonArray.Select(user => user.AsString).ToList() :
                         new List<string>(),

                assignedUsers = doc.Contains("assignedUsers") && doc["assignedUsers"].BsonType == BsonType.Array ?
                        doc["assignedUsers"].AsBsonArray.Select(user => user.AsString).ToList() :
                        new List<string>()
            }).ToList();

            return rooms;
        }

        public async Task ChangeStateUser(string userId, bool state)
        {
            ObjectId _Id = ConvertStringToObjectId(userId);

            var usersCollection = await _connectionStringDB.GetCollection("users");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", _Id);
            var changes = Builders<BsonDocument>.Update.Set(x => x["online"], state);
            await usersCollection.UpdateOneAsync(filter, changes);
        }

        public async Task UpdateUsersConnectedByIdRoomAsync(string groupId, string userId, bool addUser = true)
        {
            ObjectId _IdGroup = ConvertStringToObjectId(groupId);
            ObjectId _IdUser = ConvertStringToObjectId(userId);

            var roomsCollection = await _connectionStringDB.GetCollection("rooms");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", _IdGroup);
            var group = await roomsCollection.Find(filter).FirstOrDefaultAsync() ?? throw new Exception("El grupo no existe.");

            var connectedUsers = group.GetValue("connectedUsers", new BsonArray()).AsBsonArray;
            bool userExists = connectedUsers.Contains(_IdUser);

            if ((addUser || userExists) && (!addUser || !userExists))
            {
                var update = addUser
                    ? Builders<BsonDocument>.Update.AddToSet("connectedUsers", _IdUser)
                    : Builders<BsonDocument>.Update.Pull("connectedUsers", _IdUser);
                await roomsCollection.UpdateOneAsync(filter, update);
            }
            else
            {
                throw new Exception("El usuario" + (addUser ? "ya" : "no") + " está en la lista de usuarios conectados.");
            }
        }

        public async Task<string> GetUsernameByIdAsync(string userId)
        {
            ObjectId _Id = ConvertStringToObjectId(userId);

            var users = await _connectionStringDB.GetCollection("users");
            var filter = Builders<BsonDocument>.Filter.In("_id", new[] { _Id });
            var projection = Builders<BsonDocument>.Projection.Include("nickname").Exclude("_id");
            string name = users.Find(filter)
                            .Project(projection)
                            .FirstOrDefault()["nickname"]
                            .ToString();

            return name;
        }

        public async Task<List<string>> GetUsersConnectedByRoom(ObjectId groupId)
        {
            var rooms = await _connectionStringDB.GetCollection("rooms");

            var filter = Builders<BsonDocument>.Filter.Eq("_id", groupId);
            var projection = Builders<BsonDocument>.Projection.Include("connectedUsers").Exclude("_id");
            var users = rooms.Find(filter).Project(projection).ToList();

            List<string> usersConnected = new();

            var connectedUsersArray = users[0]["connectedUsers"].AsBsonArray;

            foreach (var user in connectedUsersArray)
            {
                string name = await GetUsernameByIdAsync(user.AsString);
                usersConnected.Add(name);
            }

            return usersConnected;
        }

        public ObjectId ConvertStringToObjectId(string id)
        {
            if (!ObjectId.TryParse(id, out ObjectId _Id))
            {
                throw new ArgumentException("Invalid ObjectId format.", nameof(id));
            }
            return _Id;
        }
    }
}