using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RegistrGmailFB.DAL;
using RegistrGmailFB.Models;
using RegistrGmailFB.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace RegistrGmailFB.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _rolemanager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, UserManager<AppUser> userManager, RoleManager<IdentityRole> rolemanager, SignInManager<AppUser> signInManager, IConfiguration config, AppDbContext context)
        {
            _logger = logger;
            _userManager = userManager;
            _rolemanager = rolemanager;
            _signInManager = signInManager;
            _config = config;
            _context = context;
        }

        public IActionResult Index()
        {
         
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Registr()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registr(RegistrVM registrVM)
        {
            if (!ModelState.IsValid) return View();
            AppUser user = new AppUser
            {
                Fullname = registrVM.Fullname,
                UserName = registrVM.UserName,
                Email = registrVM.Email
            };
            IdentityResult result= await _userManager.CreateAsync(user, registrVM.Password);

            if (!result.Succeeded)
            {
                foreach (var item in result.Errors)
                {
                    ModelState.AddModelError("", item.Description);
                }
                return View(registrVM);
            }

            return RedirectToAction("login");
        }
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM loginVM)
        {
            if (!ModelState.IsValid) return View();

            AppUser appUser = await _userManager.FindByEmailAsync(loginVM.Email);
            if (appUser == null)
            {
                ModelState.AddModelError("", "Email ve ya sifre yanlishdir");
                return View(loginVM);
            }
            SignInResult result = await _signInManager.PasswordSignInAsync(appUser, loginVM.Password, true, true);

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "account bloklandi");
                return View(loginVM);
            }
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Email ve ya sifre yanlishdir");
                return View(loginVM);
            }
            await _signInManager.SignInAsync(appUser, true);
            return RedirectToAction("index");
        }
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index");
        }

        public IActionResult GoogleLogin(string ReturnUrl)
        {
            string redirectUrl = Url.Action("ExternalResponse", new { ReturnUrl = ReturnUrl });

            AuthenticationProperties properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            //Bağlantı kurulacak harici platformun hangisi olduğunu belirtiyor ve bağlantı özelliklerini elde ediyoruz.
            return new ChallengeResult("Google", properties);
            //ChallengeResult; kimlik doğrulamak için gerekli olan tüm özellikleri kapsayan AuthenticationProperties nesnesini alır ve ayarlar.
        }

        public async Task<IActionResult> ExternalResponse(string ReturnUrl = "/")
        {
            ExternalLoginInfo loginInfo = await _signInManager.GetExternalLoginInfoAsync();
            //Kullanıcıyla ilgili dış kaynaktan gelen tüm bilgileri taşıyan nesnedir.
            //Bu nesnesnin 'LoginProvider' propertysinin değerine göz atarsanız eğer hangi dış kaynaktan geliniyorsa onun bilgisinin yazdığını göreceksiniz.
            if (loginInfo == null)
                return RedirectToAction("Login");
            else
            {
                Microsoft.AspNetCore.Identity.SignInResult loginResult = await _signInManager.ExternalLoginSignInAsync(loginInfo.LoginProvider, loginInfo.ProviderKey, true);
                //Giriş yapıyoruz.
                if (loginResult.Succeeded)
                    return Redirect(ReturnUrl);
                else
                {
                    //Eğer ki akış bu bloğa girerse ilgili kullanıcı uygulamamıza kayıt olmadığından dolayı girişi başarısız demektir.
                    //O halde kayıt işlemini yapıp, ardından giriş yaptırmamız gerekmektedir.
                    AppUser user = new AppUser
                    {
                        Email = loginInfo.Principal.FindFirst(ClaimTypes.Email).Value,
                        UserName = loginInfo.Principal.FindFirst(ClaimTypes.GivenName).Value,
                        Fullname = loginInfo.Principal.FindFirst(ClaimTypes.Name).Value

                    };
                    //Dış kaynaktan gelen Claimleri uygun eşlendikleri propertylere atıyoruz.
                    IdentityResult createResult = await _userManager.CreateAsync(user);
                    //Kullanıcı kaydını yapıyoruz.
                    if (createResult.Succeeded)
                    {
                        //Eğer kayıt başarılıysa ilgili kullanıcı bilgilerini AspNetUserLogins tablosuna kaydetmemiz gerekmektedir ki
                        //bir sonraki dış kaynak login talebinde Identity mimarisi ilgili kullanıcının hangi dış kaynaktan geldiğini anlayabilsin.
                        IdentityResult addLoginResult = await _userManager.AddLoginAsync(user, loginInfo);
                        //Kullanıcı bilgileri dış kaynaktan gelen bilgileriyle AspNetUserLogins tablosunda eşleştirilmek suretiyle kaydedilmiştir.
                        if (addLoginResult.Succeeded)
                        {
                            await _signInManager.SignInAsync(user, true);
                            //await _signInManager.ExternalLoginSignInAsync(loginInfo.LoginProvider, loginInfo.ProviderKey, true);
                            return Redirect(ReturnUrl);
                        }
                    }

                }
            }
            return Redirect(ReturnUrl);
        }
    }
}
