using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyManagement.Constants;
using PropertyManagement.Models;

namespace PropertyManagement.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel(
    UserManager<AspNetUser> userManager,
    SignInManager<AspNetUser> signInManager,
    ILogger<RegisterModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Choose an account type.")]
        [Display(Name = "Account Type")]
        public string? Role { get; set; }
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        if (Input.Role is not null && !AppRoles.All.Contains(Input.Role))
        {
            ModelState.AddModelError("Input.Role", "Choose a valid account type.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new AspNetUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            FirstName = Input.FirstName,
            LastName = Input.LastName
        };

        var createResult = await userManager.CreateAsync(user, Input.Password);

        if (!createResult.Succeeded)
        {
            AddErrors(createResult);
            return Page();
        }

        var roleResult = await userManager.AddToRoleAsync(user, Input.Role!);

        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            AddErrors(roleResult);
            return Page();
        }

        logger.LogInformation("User {Email} registered as {Role}.", user.Email, Input.Role);

        await signInManager.SignInAsync(user, isPersistent: false);

        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~/"));
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}