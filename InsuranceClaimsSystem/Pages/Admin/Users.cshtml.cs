using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using InsuranceClaimsSystem.Models;
using InsuranceClaimsSystem.Services;

namespace InsuranceClaimsSystem.Pages.Admin;

[Authorize(Roles = "Admin")]
public class UsersModel : PageModel
{
    private const string SeedAdminEmail = "admin@gmsugar.local";
    private readonly IUserService _userService;

    public UsersModel(IUserService userService)
    {
        _userService = userService;
    }

    public List<ApplicationUser> Users { get; set; } = new();

    [BindProperty]
    [ValidateNever]
    public NewUserInput NewUser { get; set; } = new();

    [BindProperty]
    [ValidateNever]
    public EditUserInput EditUser { get; set; } = new();

    [BindProperty]
    [ValidateNever]
    public ResetPasswordInput ResetPassword { get; set; } = new();

    public string? SelectedEditUserId { get; set; }
    public string? SelectedResetPasswordUserId { get; set; }

    public string[] AvailableRoles { get; } = new[] { "Admin", "Insurance Officer", "Assessor", "Broker Company Officer", "Lawyer" };

    public async Task OnGetAsync(string? editUserId, string? resetPasswordUserId)
    {
        if (!IsSeedAdminUser())
        {
            Response.Redirect("/Account/AccessDenied");
            return;
        }

        Users = await _userService.GetAllUsersAsync();

        if (!string.IsNullOrWhiteSpace(editUserId))
        {
            var user = await _userService.GetUserByIdAsync(editUserId);
            if (user != null)
            {
                SelectedEditUserId = editUserId;
                EditUser = new EditUserInput
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FullName = user.FullName,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    CompanyName = user.CompanyName,
                    Department = user.Department
                };
            }
        }

        if (!string.IsNullOrWhiteSpace(resetPasswordUserId))
        {
            var user = await _userService.GetUserByIdAsync(resetPasswordUserId);
            if (user != null)
            {
                SelectedResetPasswordUserId = resetPasswordUserId;
                ResetPassword = new ResetPasswordInput
                {
                    UserId = user.Id
                };
            }
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!IsSeedAdminUser())
        {
            return Forbid();
        }

        ModelState.Clear();

        if (!TryValidateModel(NewUser, nameof(NewUser)))
        {
            Users = await _userService.GetAllUsersAsync();
            return Page();
        }

        var user = new ApplicationUser
        {
            Email = NewUser.Email,
            UserName = NewUser.Email,
            FullName = NewUser.FullName,
            Role = NewUser.Role,
            EmailConfirmed = true,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CompanyName = NewUser.CompanyName,
            Department = NewUser.Department
        };

        var result = await _userService.CreateUserAsync(user, NewUser.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            Users = await _userService.GetAllUsersAsync();
            return Page();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        if (!IsSeedAdminUser())
        {
            return Forbid();
        }

        ModelState.Clear();

        if (!TryValidateModel(EditUser, nameof(EditUser)))
        {
            Users = await _userService.GetAllUsersAsync();
            SelectedEditUserId = EditUser.Id;
            return Page();
        }

        var user = await _userService.GetUserByIdAsync(EditUser.Id);
        if (user == null)
        {
            return NotFound();
        }

        user.Email = EditUser.Email;
        user.UserName = EditUser.Email;
        user.FullName = EditUser.FullName;
        user.Role = EditUser.Role;
        user.IsActive = EditUser.IsActive;
        user.CompanyName = EditUser.CompanyName;
        user.Department = EditUser.Department;

        var result = await _userService.UpdateUserAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            Users = await _userService.GetAllUsersAsync();
            SelectedEditUserId = EditUser.Id;
            return Page();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResetPasswordAsync()
    {
        if (!IsSeedAdminUser())
        {
            return Forbid();
        }

        ModelState.Clear();

        if (!TryValidateModel(ResetPassword, nameof(ResetPassword)))
        {
            Users = await _userService.GetAllUsersAsync();
            SelectedResetPasswordUserId = ResetPassword.UserId;
            return Page();
        }

        var user = await _userService.GetUserByIdAsync(ResetPassword.UserId);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userService.ResetPasswordAsync(user, ResetPassword.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            Users = await _userService.GetAllUsersAsync();
            SelectedResetPasswordUserId = ResetPassword.UserId;
            return Page();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string userId)
    {
        if (!IsSeedAdminUser())
        {
            return Forbid();
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userService.DeleteUserAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            Users = await _userService.GetAllUsersAsync();
            return Page();
        }

        return RedirectToPage();
    }

    private bool IsSeedAdminUser()
    {
        return string.Equals(User.Identity?.Name, SeedAdminEmail, StringComparison.OrdinalIgnoreCase);
    }

    public class NewUserInput
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Insurance Officer";

        public string CompanyName { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public class EditUserInput
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Insurance Officer";

        public bool IsActive { get; set; } = true;

        public string CompanyName { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;
    }

    public class ResetPasswordInput
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
