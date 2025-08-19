using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TakeCareOfUs.Models;
using TakeCareOfUs.Services;
using TakeCareOfUs.ViewModels;

namespace TakeCareOfUs.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Users> signInManager;
        private readonly UserManager<Users> userManager;
        private readonly NameValidationService nameValidationService;

        public AccountController(
            SignInManager<Users> signInManager,
            UserManager<Users> userManager,
            NameValidationService nameValidationService)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.nameValidationService = nameValidationService;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Email or password is incorrect.");
                    return View(model);
                }
            }
            return View(model);
        }

        public IActionResult Register()
        {
            return View();
        }

   [HttpPost]
public async Task<IActionResult> Register(RegisterViewModel model)
{
    if (ModelState.IsValid)
    {
        // استدعاء الدالة التي ترجع 3 قيم: score, explanation, description
        (int score, string explanation, string description) = await nameValidationService.CheckNameAsync(model.Name);

        // التحقق من صحة الاسم بناءً على النتيجة
        if (score < 50)
        {
            ModelState.AddModelError("", $"❗ Name not valid ({score}/100): {explanation} — {description}");
            return View(model);
        }

        // إنشاء مستخدم جديد إذا كان الاسم صالحًا
        Users users = new Users
        {
            FullName = model.Name,
            Email = model.Email,
            UserName = model.Email,
        };

        var result = await userManager.CreateAsync(users, model.Password);

        if (result.Succeeded)
        {
            return RedirectToAction("Login", "Account");
        }
        else
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }
    }

    return View(model);
}



        public IActionResult ChangePassword(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("VerifyEmail", "Account");
            }
            return View(new ChangePasswordViewModel { Email = username });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var result = await userManager.RemovePasswordAsync(user);
                    if (result.Succeeded)
                    {
                        result = await userManager.AddPasswordAsync(user, model.NewPassword);
                        return RedirectToAction("Login", "Account");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        return View(model);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Email not found!");
                    return View(model);
                }
            }
            else
            {
                ModelState.AddModelError("", "Something went wrong. Try again.");
                return View(model);
            }
        }

        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
