﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BLL.ViewModels;
using DAL;
using Infrastructure.Notifications;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;

namespace Itis_bet.Controllers
{
    [AllowAnonymous]
    public class RegLogController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly INotificator<bool> _notify;
        private readonly Database _db;

        public RegLogController(UserManager<User> manager, Database db, SignInManager<User> signInManager,
            INotificator<bool> notify)
        {
            _signInManager = signInManager;
            _notify = notify;
            _userManager = manager;
            _db = db;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
                return Redirect("/");

            return View(new Tuple<LoginViewModel, RegisterViewModel>(
                new LoginViewModel(), new RegisterViewModel()));
        }

        [HttpPost]
        public async Task<IActionResult> Reg(RegisterViewModel regVM)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(regVM.Email);

                if (user != null)
                {
                    ModelState.AddModelError(string.Empty, "User already exist");
                    return InvalidRegisterRequest(regVM);
                }

                await _userManager.CreateAsync(CreateUser(regVM.Login, regVM.Email), regVM.Password);
                await _notify.AboutRegistrationAsync(RegistrationReason.Succeeded, regVM.Email);

                return await Log(new LoginViewModel {Email = regVM.Email, Password = regVM.Password});
            }

            return InvalidRegisterRequest(regVM);
        }

        [HttpPost]
        public async Task<IActionResult> Log(LoginViewModel logVM)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(logVM.Email);

                if (user == null)
                    return RedirectToAction("Reg", "RegLog");

                var res = await SignIn(user.UserName, logVM.Password, logVM.Remember);

                if (res.Succeeded)
                    return RedirectToAction("Index", "Account");
                else
                    ModelState.AddModelError(string.Empty, "Incorrect login or password.");
            }

            return InvalidLoginRequest(logVM);
        }

        [HttpPost]
        public async Task<IActionResult> LogOut()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult GetRegPartial() =>
            PartialView("RegPartial", new RegisterViewModel());

        [HttpGet]
        public IActionResult GetLogPartial() =>
            PartialView("LogPartial", new LoginViewModel());

        private IActionResult InvalidLoginRequest(LoginViewModel logVM) =>
            View("Index", new Tuple<LoginViewModel, RegisterViewModel>(logVM, null));

        private IActionResult InvalidRegisterRequest(RegisterViewModel regVM) =>
            View("Index", new Tuple<LoginViewModel, RegisterViewModel>(null, regVM));

        private async Task<Microsoft.AspNetCore.Identity.SignInResult> SignIn(string userName, string password,
            bool remember) =>
            await _signInManager.PasswordSignInAsync(userName, password, remember, false);

        private User CreateUser(string name, string email) => new User {Email = email, UserName = name};

        const int VK_CLIENT_ID = 7782410;
        const string VK_CLIENT_SECRET = "8YEm5WQQytMQs5PyaeQl";

        [HttpGet]
        public IActionResult VkAuth()
        {
            return Redirect(
                $"https://oauth.vk.com/authorize?client_id={VK_CLIENT_ID}&display=page&redirect_uri=https://localhost:5001/reglog/vk&scope=email&response_type=code&v=5.131");
        }

        [HttpGet]
        public IActionResult Vk(string code)
        {
            string url =
                $"https://oauth.vk.com/access_token?client_id={VK_CLIENT_ID}&client_secret={VK_CLIENT_SECRET}&redirect_uri=https://localhost:5001&code={code}";


            var response = GET(url);
            JObject result = JObject.Parse(response);

            var user = _db.Users.Where(u => u.Email.Equals(result.GetValue("email").ToString()));


            return Redirect("/");
        }

        private static string GET(string Url)
        {
            System.Net.WebRequest req = System.Net.WebRequest.Create(Url);
            System.Net.WebResponse resp = req.GetResponse();
            System.IO.Stream stream = resp.GetResponseStream();
            System.IO.StreamReader sr = new System.IO.StreamReader(stream);
            string Out = sr.ReadToEnd();
            sr.Close();
            return Out;
        }
    }
}
