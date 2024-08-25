using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver;
using SocketMessages.Configurations;
using SocketMessages.Views.Dto;
using System.Collections.Concurrent;

public class ChatHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> _connections = new ();
    //private static readonly ConcurrentDictionary<string, Dictionary<string, string>> _connections = new();

    private readonly DataAccessMongoDb _dataAccessMongoDb = new();

    public override async Task<Task> OnConnectedAsync()
    {
        string idGroup = "Global";
        var query = Context.GetHttpContext()?.Request.Query;
        //string username = query["Username"];
        string usernameId = query["UsernameId"]; //GUARDAR ID EN VEZ DE NOMBRE
        if (query != null && query.ContainsKey("idGroup"))
        {
            idGroup = query["idGroup"];
        }

        var users = _connections.Select(kvp => $"{kvp.Value}").ToList();

        if (!users.Contains(usernameId))
        {
            //SI EL GRUPO NO ES "GLOBAL" ACTUALIZAR USUARIOS CONECTADOS DEL MONGODB
            _connections.TryAdd(Context.ConnectionId, usernameId);
            if(idGroup != "Global")
            {
                await _dataAccessMongoDb.UpdateUsersConnectedByIdRoomAsync(idGroup, usernameId); 
            }

        }

        await Groups.AddToGroupAsync(Context.ConnectionId, idGroup);
        await Clients.Group(idGroup).SendAsync("UserJoined", usernameId);

        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        _connections.TryRemove(Context.ConnectionId, out string usernameId);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Global");

        // Notify all clients in the group that a user has left
        await Clients.Group("Global").SendAsync("UserLeft", usernameId);

        await base.OnDisconnectedAsync(exception);
    }
    
    public async Task SendMessage(string usernameId, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", usernameId, message);
    }

    public async Task<List<UserRequestDto>> GetConnectedUsers()
    {
        var tasks = _connections.Select(async user => new UserRequestDto
        {
            UserName = await _dataAccessMongoDb.GetUsernameByIdAsync(user.Value),
            UserNameId = user.Value
        });

        List<UserRequestDto> userList = (await Task.WhenAll(tasks)).ToList();

        return userList;    
    }   

    public async Task<string> GetUsernameById(string UsernameId)
    {
        return await _dataAccessMongoDb.GetUsernameByIdAsync(UsernameId);
    }
    public async Task<List<string>> GetUserConnectedByRoom(string groupId)
    {
        ObjectId _Id = _dataAccessMongoDb.ConvertStringToObjectId(groupId);
        return await _dataAccessMongoDb.GetUsersConnectedByRoom(_Id);
    }

    public async Task<List<RoomRequestDto>> GetRoomNamesCollection(string UsernameId)
    {
        //if (!ObjectId.TryParse(UsernameId, out ObjectId userId))
        //{
        //    throw new ArgumentException("Invalid ObjectId format.", nameof(UsernameId));
        //}

        return await _dataAccessMongoDb.GetRoomByUserId(UsernameId);
    }

    // ENFOQUE GRUPOS

    public async Task JoinGroup(string groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
        var usernameId = _connections[Context.ConnectionId];

        // ACTUALIZAR ROOM CON USUARIOS CONECTADOS
        await Clients.Caller.SendAsync("GroupJoined", groupId);
        await Clients.Group(groupId).SendAsync("UserJoined", usernameId);
    }

    public async Task LeaveGroup(string groupId)
    {
        // QUITAR DE USUARIO CONECTADO 
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);
        var usernameId = _connections[Context.ConnectionId];
        await Clients.Group(groupId).SendAsync("UserLeft", usernameId);
    }

    public async Task SendMessageToGroup(string groupId, string usernameId, string message)
    {
        await Clients.Group(groupId).SendAsync("ReceiveMessage", usernameId, message);
    }
}   
