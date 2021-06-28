using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TutumAdminAPI.Models;

namespace TutumAdminAPI.Controllers
{
    public class AuthController : Controller
    {
        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        private ClaimsIdentity GetIdentity()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, "AdminName"),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, "Admin")
            };
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "Token", ClaimsIdentity.DefaultNameClaimType, 
                                                                                ClaimsIdentity.DefaultRoleClaimType);
            return claimsIdentity;
        }

        //Показать форму авторизации
        public IActionResult LoginForm() => View();

        public IActionResult AccessDenied() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginForm([Bind("Login, Password")] AdminLoginModel loginModel)
        {
            var correctLogin = _config["Jwt:Login"];
            var correctPassword = _config["Jwt:Password"];

            var inputLogin = loginModel.Login;
            var inputPassword = loginModel.Password;

            if (correctLogin != inputLogin || correctPassword != inputPassword)
            {
                ModelState.AddModelError("CorrectCredentials", "Неверный логин и/или пароль");
                return View(loginModel);
            }

            var claims = new List<Claim>
                {
                    new Claim("user", "Admin"),
                    new Claim("role", "Admin")
                };

            await HttpContext.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies", "user", "role")));

            if (Url.IsLocalUrl(""))
            {
                return Redirect("");
            }
            else
            {
                return Redirect("/");
            }
        }
    }
}
