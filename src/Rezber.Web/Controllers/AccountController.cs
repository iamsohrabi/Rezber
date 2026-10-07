using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rezber.Domain.Identity;
using Rezber.Services.Features;
using Rezber.Web.Models.ViewModels;

namespace Rezber.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly IUserService _userService;
    private readonly IApiTokenManagementService _tokenManagement;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IUserService userService,
        IApiTokenManagementService tokenManagement,
        ILogger<AccountController> logger)
    {
        _userService = userService;
        _tokenManagement = tokenManagement;
        _logger = logger;
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "PackageBrowser");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var result = await _userService.SignInAsync(model.Email, model.Password, model.RememberMe);
        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} signed in", model.Email);
            return RedirectToLocal(returnUrl);
        }

        ModelState.AddModelError(string.Empty, result.Error!);
        return View(model);
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "PackageBrowser");

        return View(new RegisterViewModel());
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _userService.RegisterAsync(
            model.DisplayName, model.Email, model.JobTitle, model.Password);

        if (result.Succeeded)
            return RedirectToAction("Index", "PackageBrowser");

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error);

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _userService.SignOutAsync();
        _logger.LogInformation("User signed out");
        return RedirectToAction("Index", "PackageBrowser");
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage));
            return RedirectToAction("Index", "Dashboard");
        }

        var result = await _userService.ChangePasswordAsync(model.CurrentPassword, model.NewPassword, ct);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error ?? "Password could not be changed.";
            return RedirectToAction("Index", "Dashboard");
        }

        TempData["Success"] = "Password changed successfully.";
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    [HttpGet, Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _tokenManagement.GetActiveProfileAsync();
        if (user == null) return NotFound();

        ViewBag.Roles = new[] { user.Role };
        return View(user);
    }

    [HttpGet, Authorize]
    public async Task<IActionResult> CliTokens(CancellationToken ct)
    {
        var result = await _tokenManagement.GetAccountTokensAsync(ct);
        if (result.Status == PackageWorkflowStatus.Unauthorized)
            return Unauthorized();

        return View(new CliTokensViewModel
        {
            Tokens = result.Value!.Tokens
        });
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCliToken(string? name, CancellationToken ct)
    {
        var result = await _tokenManagement.CreateAccountTokenAsync(name, ct);
        if (result.Status == PackageWorkflowStatus.Unauthorized)
            return Unauthorized();

        var model = new CliTokensViewModel
        {
            Tokens = result.Value!.Tokens,
            NewlyCreatedToken = result.Value.NewlyCreatedToken
        };
        return View("CliTokens", model);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeCliToken(Guid id, CancellationToken ct)
    {
        var result = await _tokenManagement.RevokeAccountTokenAsync(id, ct);
        if (result.Status == PackageWorkflowStatus.Unauthorized)
            return Unauthorized();

        return RedirectToAction(nameof(CliTokens));
    }

    private IActionResult RedirectToLocal(string? returnUrl)
        => Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction("Index", "PackageBrowser");
}
