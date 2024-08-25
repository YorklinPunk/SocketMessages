using MongoDB.Bson;

namespace SocketMessages.Views.Dto
{
    public class RoomRequestDto
    {
        public string _id { get; set; }
        public string? name { get; set; }
        public string? urlImagen { get; set; }
        public List<string>? connectedUsers { get; set; }
        public List<string>? assignedUsers { get; set; }
    }
}