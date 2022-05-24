using Amazon.AspNetCore.Identity.Cognito;
using Amazon.Extensions.CognitoAuthentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAPICodePack.Shell;
using System.Net;
using WebAdvert.Web.Models.Accounts;

namespace WebAdvert.Web.Controllers
{
    public class AccountsController : Controller
    {
        private readonly SignInManager<CognitoUser> _signInManager;
        private readonly UserManager<CognitoUser> _userManager;
        private readonly CognitoUserPool _cognitoUserPool;

        public AccountsController(SignInManager<CognitoUser> signInManager, 
                        UserManager<CognitoUser> userManager, 
                        CognitoUserPool cognitoUserPool)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _cognitoUserPool = cognitoUserPool;
        }

        [HttpGet]
        public async Task<IActionResult> SignUp()
        {
            var model = new SignUp();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SignUp(SignUp model)
        {
            if (ModelState.IsValid)
            {
                var user = _cognitoUserPool.GetUser(model.Email);
                if (user.Status != null)
                {
                    ModelState.AddModelError("UserExists", "User with this email already exists");
                    return View(model);
                }

                user.Attributes.Add(CognitoAttribute.Name.AttributeName, model.Email);
                var createdUser = await _userManager.CreateAsync(user, model.Password).ConfigureAwait(false);
                if (createdUser.Succeeded)
                {
                    return RedirectToAction("ConfirmAccount");
                }
                else
                {
                    var findUser = await _userManager.FindByEmailAsync(model.Email).ConfigureAwait(false);
                    if (findUser != null && findUser.Status == "UNCONFIRMED")
                    {
                        findUser.ResendConfirmationCodeAsync().Wait();
                        return RedirectToAction("ConfirmAccount");
                    }
                }
                if(createdUser.Errors != null)
                {
                    foreach(var error in createdUser.Errors)
                    {
                        ModelState.AddModelError(error.Code, error.Description);
                    }
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmAccount(ConfirmAccount model)
        {
            return View(model);
        }

        [HttpPost]
        [ActionName("ConfirmAccount")]
        public async Task<IActionResult> Confirm(ConfirmAccount model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email).ConfigureAwait(false);
                if(user == null)
                {
                    ModelState.AddModelError("NotFound", "A user with the given email does not found");
                    return View(model);
                }
                var result = await (_userManager as CognitoUserManager<CognitoUser>).ConfirmSignUpAsync(user, model.ConfirmationCode, true).ConfigureAwait(false);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    foreach(var error in result.Errors)
                    {
                        ModelState.AddModelError(error.Code, error.Description);
                    }
                    return View(model);
                }
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ForgotPassword()
        {
            var model = new ForgotPassword();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPassword model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email).ConfigureAwait(false);
                if(user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    ModelState.AddModelError("NotFound", "A user with the given email does not found");
                    return View(model);
                }
                user.ForgotPasswordAsync().Wait();
                return RedirectToAction("ConfirmNewPassword");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmNewPassword()
        {
            var model = new ConfirmNewPassword();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmNewPassword(ConfirmNewPassword model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email).ConfigureAwait(false);
                if(user == null)
                {
                    ModelState.AddModelError("NotFound", "A user with the given email does not found");
                    return View(model);
                }

                await user.ConfirmForgotPasswordAsync(model.ConfirmationCode, model.NewPassword);
                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }

        public async Task<IActionResult> SignIn()
        {
            var model = new SignIn();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SignIn(SignIn model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false).ConfigureAwait(false);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("LoginError", "Email and password do not match");
            }
            return View(model);
        }

        public async Task<IActionResult> Download()
        {
            //WebClient myWebClient = new WebClient();
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Request");
            var res = client.GetAsync("https://api.github.com/repos/m-krishnachaitanya/covid-19-api/zipball/master").Result;
            var reqBytes = await res.Content.ReadAsByteArrayAsync();
            //myWebClient.Headers.Add("User-Agent", "Request");
            //var filepath = KnownFolders.Downloads.Path + "/template.zip";
            //myWebClient.DownloadFileAsync(new Uri("https://api.github.com/repos/m-krishnachaitanya/covid-19-api/zipball/master"), filepath);
            return File(reqBytes, "application/zip", "template.zip", true);
        }
    }
}
