using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GymManagement.Models;
using GymManagement.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using GymManagement.Data;

namespace GymManagement.Controllers
{
  public class AccountController : Controller
  {
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IWebHostEnvironment _env;
    private readonly AppDbContext _dbContext;

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, IWebHostEnvironment env,AppDbContext dbContext)
    {
      _userManager = userManager;
      _signInManager = signInManager;
      _env = env;
      _dbContext = dbContext;
    }

    private async Task<IActionResult> RedirectToDashboardByRole(User user)
    {
      if (await _userManager.IsInRoleAsync(user, "Admin"))
        return RedirectToAction("Dashboard", "Admin", new { area = "Admin" });
      if (await _userManager.IsInRoleAsync(user, "Trainer"))
        return RedirectToAction("Dashboard", "Trainer");
      if (await _userManager.IsInRoleAsync(user, "Receptionist"))
        return RedirectToAction("Dashboard", "Receptionist");
      if (await _userManager.IsInRoleAsync(user, "Customer"))
        return RedirectToAction("Dashboard", "Customer");

      return RedirectToAction("Index", "Home");
    }


    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            // 1. 获取默认 gym branch（必须存在）
            var defaultBranch = await _dbContext.GymBranches.FirstOrDefaultAsync();
            if (defaultBranch == null)
            {
                ModelState.AddModelError("", "No gym branches exist in the system. Please contact admin.");
                return View(model);
            }

            // 2. 使用 normalized 值防止 UNIQUE 约束报错
            var normalizedUsername = _userManager.NormalizeName(model.Username);
            var usernameExists = await _dbContext.Users.AnyAsync(u => u.NormalizedUserName == normalizedUsername);
            if (usernameExists)
            {
                ModelState.AddModelError("Username", "This username is already taken.");
                return View(model);
            }

            var normalizedEmail = _userManager.NormalizeEmail(model.Email);
            var emailExists = await _dbContext.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            // 3. 构造 Customer 对象
            var user = new Customer
            {
                UserName = model.Username,
                Email = model.Email,
                Name = model.Name,
                JoinDate = DateTime.UtcNow,
                DOB = model.DOB,
                PhoneNumber = "",
                MembershipType = MembershipType.Monthly,
                MembershipStatus = MembershipStatus.Active,
                SubscriptionDate = DateTime.Now,
                MembershipExpiry = DateTime.Now.AddMonths(1),
                GymBranchId = defaultBranch.BranchId
            };

            // 4. 使用 Identity 创建用户（此操作会写入 AspNetUsers 表）
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // 5. 加角色
                await _userManager.AddToRoleAsync(user, "Customer");

                // 6. 登录
                user.RoleNames = await _userManager.GetRolesAsync(user);
                await _signInManager.SignInAsync(user, isPersistent: false);

                return RedirectToAction("Dashboard", "Customer");
            }

            // 7. 出错处理
            foreach (var error in result.Errors)
            {
                if (error.Code.Contains("Password"))
                    ModelState.AddModelError("Password", error.Description);
                else if (error.Code.Contains("Email"))
                    ModelState.AddModelError("Email", error.Description);
                else if (error.Code.Contains("UserName"))
                    ModelState.AddModelError("Username", error.Description);
                else
                    ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Login(string returnUrl = "")
    {
      var model = new LoginViewModel { ReturnUrl = returnUrl };
      return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
      if (!ModelState.IsValid) return View(model);

      var result = await _signInManager.PasswordSignInAsync(
          model.Username, model.Password, model.RememberMe, lockoutOnFailure: false);

      if (result.Succeeded)
      {
        var signedInUser = await _userManager.FindByNameAsync(model.Username);
        if (signedInUser != null)
        {
          signedInUser.RoleNames = await _userManager.GetRolesAsync(signedInUser);
          return await RedirectToDashboardByRole(signedInUser);
        }
      }

      ModelState.AddModelError(string.Empty, "Invalid username or password.");
      return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
      await _signInManager.SignOutAsync();
      return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
      var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
      var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
      return Challenge(properties, provider);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");

        if (remoteError != null)
        {
            ModelState.AddModelError("", $"Error from external provider: {remoteError}");
            return RedirectToAction(nameof(Login));
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            ModelState.AddModelError("", "External login info is null.");
            return RedirectToAction(nameof(Login));
        }

        var loginResult = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false);

        if (loginResult.Succeeded)
        {
            var existingUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (existingUser != null)
            {
                return await RedirectToDashboardByRole(existingUser);
            }

            return Redirect(returnUrl);
        }

        var email = info.Principal?.FindFirstValue(ClaimTypes.Email);
        var fullName = info.Principal?.FindFirstValue(ClaimTypes.Name) ?? email ?? "Google User";

        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError("", "Google account missing email claim.");
            return RedirectToAction(nameof(Login));
        }

        var defaultBranch = await _dbContext.GymBranches.FirstOrDefaultAsync();

        var user = new Customer
        {
            UserName = email,
            Email = email,
            Name = fullName,
            JoinDate = DateTime.UtcNow,
            PhoneNumber = "",
            MembershipType = MembershipType.Monthly,
            MembershipStatus = MembershipStatus.Active,
            SubscriptionDate = DateTime.Now,
            MembershipExpiry = DateTime.Now.AddMonths(1),
            GymBranchId = defaultBranch?.BranchId ?? 0
        };

        var createResult = await _userManager.CreateAsync(user);
        if (createResult.Succeeded)
        {
            await _userManager.AddLoginAsync(user, info);
            await _userManager.AddToRoleAsync(user, "Customer");
            user.RoleNames = await _userManager.GetRolesAsync(user);
            await _signInManager.SignInAsync(user, isPersistent: false);

            return await RedirectToDashboardByRole(user);
        }

        foreach (var error in createResult.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        return RedirectToAction(nameof(Login));
    }


    public IActionResult AccessDenied() => View();

    [HttpGet]
    public IActionResult ChangePassword()
    {
      var model = new ChangePasswordViewModel
      {
        Username = User.Identity?.Name ?? ""
      };
      return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
      if (ModelState.IsValid)
      {
        var user = await _userManager.FindByNameAsync(model.Username);
        if (user == null)
        {
          ModelState.AddModelError("", "User not found.");
          return View(model);
        }

        var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
        if (result.Succeeded)
        {
          TempData["message"] = "Password changed successfully.";
          return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
          ModelState.AddModelError("", error.Description);
      }

      return View(model);
    }
  [HttpPost]
  [Authorize(Roles = "Receptionist")]
public async Task<IActionResult> ToggleAvailability(bool isAvailable)
{
    var user = await _userManager.GetUserAsync(User);
    if (user is not Receptionist receptionist) return Unauthorized();

    receptionist.IsAvailable = isAvailable;
    await _userManager.UpdateAsync(receptionist);

    // 刷新当前登录状态，才能立刻反映在右上角
    await _signInManager.RefreshSignInAsync(receptionist);

    TempData["Toast"] = $"Availability set to {(isAvailable ? "Available" : "Not Available")}";
    return RedirectToAction("ViewProfile");
}





    [Authorize]
    [HttpGet]
    public async Task<IActionResult> ViewProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        var vm = new EditProfileViewModel
        {
            UserName = user.UserName ?? "",
            Name = user.Name,
            Email = user.Email ?? string.Empty,
            DOB = user.DOB,
            ProfileImageUrl = string.IsNullOrEmpty(user.ProfileImageName)
                ? "/uploads/profile/default.png"
                : "/uploads/profile/" + user.ProfileImageName,
            RoleNames = roles
        };

        // 👇 根据角色填充额外字段
        if (roles.Contains("Customer") && user is Customer customer)
        {
            vm.MembershipStatus = customer.MembershipStatus;
            vm.SubscriptionDate = customer.SubscriptionDate;
        }

        if (roles.Contains("Receptionist") && user is Receptionist receptionist)
        {
            vm.Notes = receptionist.Notes;
            vm.IsAvailable = receptionist.IsAvailable;
        }

        if (roles.Contains("Trainer") && user is Trainer trainer)
        {
            vm.Bio = trainer.Bio;
            vm.Specialization = trainer.Specialization;
        }

        return View(vm);
    }



    [Authorize]
[HttpGet]
public async Task<IActionResult> EditProfile()
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return NotFound();

    var roles = await _userManager.GetRolesAsync(user);

    var model = new EditProfileViewModel
    {
        Name = user.Name,
        Email = user.Email,
        DOB = user.DOB,
        ProfileImageUrl = string.IsNullOrEmpty(user.ProfileImageName)
            ? "/uploads/profile/default.png"
            : "/uploads/profile/" + user.ProfileImageName,
        RoleNames = roles
    };

    if (roles.Contains("Receptionist") && user is Receptionist receptionist)
    {
        model.Notes = receptionist.Notes;
        model.IsAvailable = receptionist.IsAvailable;
    }

    if (roles.Contains("Trainer") && user is Trainer trainer)
    {
        model.Specialization = trainer.Specialization;
        model.Bio = trainer.Bio;
    }

    return View(model);
}


    [Authorize]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> EditProfile(EditProfileViewModel model)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return NotFound();

    var roles = await _userManager.GetRolesAsync(user);
    model.RoleNames = roles;

    if (!ModelState.IsValid)
    {
        model.ProfileImageUrl = string.IsNullOrEmpty(user.ProfileImageName)
            ? "/uploads/profile/default.png"
            : "/uploads/profile/" + user.ProfileImageName;
        return View(model);
    }

    if (!string.Equals(model.Email, user.Email, StringComparison.OrdinalIgnoreCase))
    {
        var exists = await _userManager.FindByEmailAsync(model.Email);
        if (exists != null && exists.Id != user.Id)
        {
            ModelState.AddModelError("Email", "This email is already taken.");
            model.ProfileImageUrl = string.IsNullOrEmpty(user.ProfileImageName)
                ? "/uploads/profile/default.png"
                : "/uploads/profile/" + user.ProfileImageName;
            return View(model);
        }
    }

    user.Name = model.Name;
    user.Email = model.Email;
    user.DOB = model.DOB;

    // 更新 Receptionist 字段
    if (roles.Contains("Receptionist") && user is Receptionist receptionist)
    {
        receptionist.Notes = model.Notes;
    }

    // 更新 Trainer 字段
    if (roles.Contains("Trainer") && user is Trainer trainer)
    {
        trainer.Specialization = model.Specialization;
        trainer.Bio = model.Bio;
    }

    if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
    {
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp" };
        var contentType = model.ProfileImageFile.ContentType.ToLower();

        if (!allowedTypes.Contains(contentType))
        {
            ModelState.AddModelError("", "Only image files (JPG, PNG, GIF, BMP, WEBP) are allowed.");
            model.ProfileImageUrl = string.IsNullOrEmpty(user.ProfileImageName)
                ? "/uploads/profile/default.png"
                : "/uploads/profile/" + user.ProfileImageName;
            return View(model);
        }

        var folder = Path.Combine(_env.WebRootPath, "uploads", "profile");
        Directory.CreateDirectory(folder);

        if (!string.IsNullOrEmpty(user.ProfileImageName) && user.ProfileImageName != "default.png")
        {
            var oldPath = Path.Combine(folder, user.ProfileImageName);
            if (System.IO.File.Exists(oldPath))
                System.IO.File.Delete(oldPath);
        }

        var uniqueFile = Guid.NewGuid().ToString() + Path.GetExtension(model.ProfileImageFile.FileName);
        var filePath = Path.Combine(folder, uniqueFile);

        using var stream = new FileStream(filePath, FileMode.Create);
        await model.ProfileImageFile.CopyToAsync(stream);

        user.ProfileImageName = uniqueFile;
    }

    if (!string.IsNullOrWhiteSpace(model.Password))
    {
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            model.ProfileImageUrl = string.IsNullOrEmpty(user.ProfileImageName)
                ? "/uploads/profile/default.png"
                : "/uploads/profile/" + user.ProfileImageName;
            return View(model);
        }
    }

    await _userManager.UpdateAsync(user);
    return RedirectToAction("ViewProfile");
}

  }
}
