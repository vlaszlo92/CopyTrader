using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;
using CopyTrader.Web.Areas.Identity.Data;
using CopyTrader.Web.Interfaces;
using CopyTrader.Web.Models;
using Newtonsoft.Json;
using System.Runtime.Intrinsics.X86;
using Microsoft.CodeAnalysis.Scripting;

namespace CopyTrader.Web.Controllers
{
    [Authorize(Roles = "superadmin")]
    public class AdminController : Controller
    {
        private readonly IApplicationUserRepository _userManager;
        private readonly ITraderGroupRepository _traderGroupManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AdminController(IApplicationUserRepository userManager,
            ITraderGroupRepository traderGroupManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _traderGroupManager = traderGroupManager;
            _signInManager = signInManager;
        }
        public async Task<IActionResult> Index()
        {
            if (TempData.TryGetValue("ViewBag", out var viewBag))
            {
                ViewBag.Script = viewBag;
            }
            var traderGroups = await _traderGroupManager.GetTraderGroupsAsync();
            var userId = _signInManager.Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userlist = await _userManager.GetAllUserAsync();
            //TODO: change this to handle roles properly.
            if (userId == "" || true)
            {
                var avm = new AdminViewModel
                {
                    UserId = userId,
                    Users = userlist,
                    TraderGroups = traderGroups
                };
                return View(avm);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTraderGroupIds(AdminViewModel model, string traderGroupsJson)
        {
            if (ModelState.IsValid)
            {  
                var invalidUsers = await _userManager.UpdateUserAsync(model.Users);
                if (invalidUsers.Any())
                {
                    var invalidUserIds = string.Join(", ", invalidUsers);
                    var script = $"alert('A következő felhasználókat nem sikerült elmenteni: \\nid: {invalidUserIds}');";
                    ViewBag.Script = script;
                }

                if (!string.IsNullOrEmpty(traderGroupsJson))
                {
                    var traderGroups = JsonConvert.DeserializeObject<List<TraderGroup>>(traderGroupsJson);
                    model.TraderGroups = traderGroups ?? await _traderGroupManager.GetTraderGroupsAsync();
                }
                else
                {
                    model.TraderGroups = await _traderGroupManager.GetTraderGroupsAsync();
                }
                TempData["ViewBag"] = ViewBag.Script;

                return RedirectToAction("Index", "Admin");
            }          
            return View("Index", model);
        }

    }
}
