using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SFA.DAS.ApplyService.Domain.Entities;
using SFA.DAS.ApplyService.Session;
using SFA.DAS.ApplyService.Web.Infrastructure;
using SFA.DAS.ApplyService.Web.ViewModels;

namespace SFA.DAS.ApplyService.Web.Controllers
{
    public class UsersController : Controller
    {
        private readonly IUsersApiClient _usersApiClient;
        private readonly ISessionService _sessionService;
        private readonly ILogger<UsersController> _logger;
        public UsersController(IUsersApiClient usersApiClient, ISessionService sessionService, ILogger<UsersController> logger)
        {
            _usersApiClient = usersApiClient;
            _sessionService = sessionService;
            _logger = logger;
        }
        
        [HttpGet]
        public IActionResult CreateAccount()
        {
            var vm = new CreateAccountViewModel();
            return View(vm);
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateAccount(CreateAccountViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }
            
            var inviteSuccess = await _usersApiClient.InviteUser(vm);

            TempData["NewAccount"] = JsonConvert.SerializeObject(vm);

            return inviteSuccess ? RedirectToAction("InviteSent") : RedirectToAction("Error", "Home");
            
        }

        [HttpGet]
        public IActionResult SignIn()
        {
            return Challenge(new AuthenticationProperties() {RedirectUri = Url.Action("PostSignIn", "Users")},
                OpenIdConnectDefaults.AuthenticationScheme);
        }
        
        [HttpGet]
        public IActionResult SignOut()
        {
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }

            return SignOut(CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        public IActionResult InviteSent()
        {
            CreateAccountViewModel viewModel;
            if (TempData["NewAccount"] is null)
            {
                viewModel = new CreateAccountViewModel() {Email = "[email placeholder]"};
            }
            else
            {
                viewModel =  JsonConvert.DeserializeObject<CreateAccountViewModel>(TempData["NewAccount"].ToString());    
            }
            
            return View(viewModel);
        }

        public async Task<IActionResult> PostSignIn()
        {
            var user = await _usersApiClient.GetUserBySignInId(User.GetSignInId());
           
            if (user == null)
            {
                return RedirectToAction("NotSetUp");
            }
            
            _logger.LogInformation($"Setting LoggedInUser in Session: {user.GivenNames} {user.FamilyName}");
            
            _sessionService.Set("LoggedInUser", $"{user.GivenNames} {user.FamilyName}");

            _sessionService.Set("SignedInFromApply", User.SignedInFromApply());

            if (user.ApplyOrganisationId == null)
            {
                return RedirectToAction("Index", "OrganisationSearch");
            }
            
            return RedirectToAction("Applications", "Application");
        }

        [HttpGet("/Users/SignedOut")]
        public IActionResult SignedOut()
        {
            return View();
        }

        public IActionResult NotSetUp()
        {
            return View();
        }
    }
}