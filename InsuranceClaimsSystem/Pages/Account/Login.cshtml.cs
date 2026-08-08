using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using InsuranceClaimsSystem.Models;

namespace InsuranceClaimsSystem.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public LoginModel(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string ErrorMessage { get; set; } = string.Empty;

    public string ReturnUrl { get; set; } = string.Empty;

    public void OnGet(string? returnUrl = null)
    {
        if (User?.Identity?.IsAuthenticated ?? false)
        {
            if (User.IsInRole("Broker Company Officer"))
            {
                Response.Redirect("/Claims");
                return;
            }

            Response.Redirect("/");
            return;
        }

        ReturnUrl = returnUrl ?? Url.Content("~/");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var signedInUser = await _signInManager.UserManager.FindByEmailAsync(Input.Email);
            if (signedInUser != null && await _signInManager.UserManager.IsInRoleAsync(signedInUser, "Broker Company Officer"))
            {
                return RedirectToPage("/Claims/Index");
            }

            return RedirectToPage("/Index");
        }

        if (result.IsLockedOut)
        {
            ErrorMessage = "Account locked out. Please try again later.";
            return Page();
        }

        ErrorMessage = "Invalid login attempt.";
        return Page();
    }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}
