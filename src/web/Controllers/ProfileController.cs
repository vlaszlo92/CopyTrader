using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CopyTrader.Web.Areas.Identity.Data;
using CopyTrader.Web.Interfaces;
using CopyTrader.Web.Models;

namespace CopyTrader.Web.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IApplicationUserRepository _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _appDbContext;
        private readonly IUserAPIKeyRepository _userAPIKeyRepository;
        private readonly IUserBalanceRepository _userBalanceRepository;

        public ProfileController(IApplicationUserRepository userManager,
                                   SignInManager<ApplicationUser> signInManager,
                                   ApplicationDbContext appDbContext,
                                   IUserAPIKeyRepository userAPIKeyRepository,
                                   IUserBalanceRepository userBalanceRepository)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _appDbContext = appDbContext;
            _userAPIKeyRepository = userAPIKeyRepository;
            _userBalanceRepository = userBalanceRepository;
        }
        public async Task<IActionResult> Index()
        {
            var userId = _signInManager.Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                var apisasync = await _userAPIKeyRepository.GetUserAPIKeysByUserIdAsync(userId);
                var userAsync = await _userManager.GetApplicationUserAsync(userId);
                var vm = new UserProfileViewModel
                {
                    FirstName = userAsync.FirstName,
                    LastName = userAsync.LastName,
                    Email = User.FindFirstValue(ClaimTypes.Email),
                    APIs = apisasync,
                };
                return View(vm);
            }
            return View();
        }

        public async Task<IActionResult> AddAPIKey()
        {
            //var userId = _signInManager.Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            //if (userId != null)
            //{
            //    var isAPIValid = await _userAPIKeyRepository.IsUserAPIExistAsync(userId);
            //    if (isAPIValid)
            //    {
            //        return RedirectToAction("ModifyAPIKey", "Profile");                    
            //    }
            //}
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddAPIKey(UserAPIKeyViewModel userAPIViewModel)
        {
            if (ModelState.IsValid)
            {
                var userAPI = new UserAPI
                {
                    UserId = _signInManager.Context.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    APIKey = userAPIViewModel.Key,
                    Secure = userAPIViewModel.Secure,
                    FullName = userAPIViewModel.FullName,
                    BinanceUserId = userAPIViewModel.BinanceUserId,
                    IsActive = true
                };
                var isStored = await _userAPIKeyRepository.AddUserAPIKeyAsync(userAPI);
                if (isStored)
                {
                    userAPI.Id = await _userAPIKeyRepository.GetAPIIdByAPIKeyAsync(userAPI.APIKey);
                    await _userBalanceRepository.AddUserBalanceAsync(userAPI.Id, userAPIViewModel.Balance, userAPIViewModel.CurrencyType);
                    return RedirectToAction("Index", "Profile");
                }

                ModelState.AddModelError(string.Empty, "API Kulcs hozzáadása sikertelen.");
            }
            return View();
        }

        public IActionResult ModifyAPIKey()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ModifyAPIKey(UserAPIKeyViewModel userAPIViewModel)
        {
            if (ModelState.IsValid)
            {
                var userAPI = new UserAPI
                {
                    UserId = _signInManager.Context.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    APIKey = userAPIViewModel.Key,
                    Secure = userAPIViewModel.Secure,
                    FullName = userAPIViewModel.FullName,
                    BinanceUserId = userAPIViewModel.BinanceUserId
                };
                var isModified = await _userAPIKeyRepository.ModifyUserAPIKeyAsync(userAPI);
                if (isModified)
                {
                    //TODO: change the balances too.
                    return RedirectToAction("Index", "Profile");
                }

                ModelState.AddModelError(string.Empty, "API Kulcs módosítása sikertelen.");
            }
            return View();
        }
    }
}
