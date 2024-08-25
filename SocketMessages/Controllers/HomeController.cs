using Microsoft.AspNetCore.Mvc;
using SocketMessages.Configurations;

namespace SocketMessages.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LoginAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                ModelState.AddModelError("username", "username is required");
                return View();
            }
            DataAccessMongoDb dataAccessMongoDb = new();
            var (isActive, UsernameId) = await dataAccessMongoDb.GetUserIdMongo(username);

            if (isActive)
            {
                HttpContext.Session.SetString("Username", username);
                HttpContext.Session.SetString("UsernameId", UsernameId.ToString());
                //await dataAccessMongoDb.ChangeStateUser(username, true);
                await dataAccessMongoDb.ChangeStateUser(UsernameId.ToString(), true);
            }
            else
            {
                ModelState.AddModelError("username", "El usuario ya esta logeado");                
                return View();
            }
            
            //TempData["Username"] = username;
            //TempData["UsernameId"] = await dataAccessMongoDb.GetUserIdMongo(username);
            return RedirectToAction("ChatGeneral");
        }

        public Task<IActionResult> ChatGeneralAsync()
        {
            var username = HttpContext.Session.GetString("Username");
            var usernameId = HttpContext.Session.GetString("UsernameId");

            if (string.IsNullOrEmpty(username))
            {
                return Task.FromResult<IActionResult>(RedirectToAction("Index"));
            }

            ViewBag.Username = username;
            ViewBag.UsernameId = usernameId;
            return Task.FromResult<IActionResult>(View());
        }

        public async Task<IActionResult> GroupChatAsync(string groupid)
        {
            DataAccessMongoDb dataAccessMongoDb = new();
            string groupname = await dataAccessMongoDb.GetRoomNameMongo(groupid);

            HttpContext.Session.SetString("GroupName", groupname);
            HttpContext.Session.SetString("GroupId", groupid);

            var usernameId = HttpContext.Session.GetString("UsernameId");
            if (string.IsNullOrEmpty(usernameId))
            {
                return RedirectToAction("Index");
            }
            ViewBag.UsernameId = usernameId;
            ViewBag.GroupName = groupname;
            ViewBag.GroupId = groupid;
            return View();
        }
    }
}
