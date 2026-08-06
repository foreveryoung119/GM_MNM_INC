using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InsuranceClaimsSystem.Data;
using InsuranceClaimsSystem.Models;

namespace InsuranceClaimsSystem.Services;

/// <summary>
/// Service for managing users.
/// </summary>
public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    /// <param name="roleManager">The role manager.</param>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public UserService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context, ILogger<UserService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password)
    {
        try
        {
            user.CreatedDate = DateTime.UtcNow;
            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                _logger.LogError($"Error creating user {user.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return result;
            }

            if (!string.IsNullOrWhiteSpace(user.Role) && await _roleManager.RoleExistsAsync(user.Role))
            {
                var roleResult = await _userManager.AddToRoleAsync(user, user.Role);
                if (!roleResult.Succeeded)
                {
                    _logger.LogError($"Error assigning role {user.Role} to user {user.Email}: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                    return roleResult;
                }
            }

            _logger.LogInformation($"User {user.Email} created successfully with role {user.Role}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception creating user: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ApplicationUser>> GetAllUsersAsync()
    {
        try
        {
            return await _context.Users
                .Where(u => u.IsActive || !u.IsActive)
                .OrderByDescending(u => u.CreatedDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving all users: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        try
        {
            return await _userManager.FindByIdAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving user {userId}: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        try
        {
            return await _userManager.FindByEmailAsync(email);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving user by email: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ApplicationUser>> GetAllAssessorsAsync()
    {
        try
        {
            return await _context.Users
                .Where(u => u.Role == "Assessor" && u.IsActive)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving assessors: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ApplicationUser>> GetAllBrokersAsync()
    {
        try
        {
            return await _context.Users
                .Where(u => u.Role == "Broker Company Officer" && u.IsActive)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving broker users: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> UpdateUserAsync(ApplicationUser user)
    {
        try
        {
            var existingUser = await _userManager.FindByIdAsync(user.Id);
            if (existingUser == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });
            }

            existingUser.Email = user.Email;
            existingUser.UserName = user.Email;
            existingUser.FullName = user.FullName;
            existingUser.Role = user.Role;
            existingUser.IsActive = user.IsActive;
            existingUser.Department = user.Department;
            existingUser.CompanyName = user.CompanyName;
            existingUser.ModifiedDate = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(existingUser);
            if (!result.Succeeded)
            {
                _logger.LogError($"Error updating user {user.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return result;
            }

            if (!string.IsNullOrWhiteSpace(user.Role) && await _roleManager.RoleExistsAsync(user.Role))
            {
                var currentRoles = await _userManager.GetRolesAsync(existingUser);
                if (!currentRoles.Contains(user.Role))
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(existingUser, currentRoles);
                    if (!removeResult.Succeeded)
                    {
                        _logger.LogError($"Error removing old roles for user {user.Email}: {string.Join(", ", removeResult.Errors.Select(e => e.Description))}");
                        return removeResult;
                    }

                    var addResult = await _userManager.AddToRoleAsync(existingUser, user.Role);
                    if (!addResult.Succeeded)
                    {
                        _logger.LogError($"Error assigning new role {user.Role} to user {user.Email}: {string.Join(", ", addResult.Errors.Select(e => e.Description))}");
                        return addResult;
                    }
                }
            }

            _logger.LogInformation($"User {user.Email} updated successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception updating user: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string newPassword)
    {
        try
        {
            var existingUser = await _userManager.FindByIdAsync(user.Id);
            if (existingUser == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
            var result = await _userManager.ResetPasswordAsync(existingUser, resetToken, newPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation($"Password reset for user {existingUser.Email}");
            }
            else
            {
                _logger.LogError($"Error resetting password for {existingUser.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception resetting password: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword)
    {
        try
        {
            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation($"Password changed for user {user.Email}");
            }
            else
            {
                _logger.LogError($"Error changing password for {user.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception changing password: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> DeleteUserAsync(ApplicationUser user)
    {
        try
        {
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation($"User {user.Email} deleted successfully");
            }
            else
            {
                _logger.LogError($"Error deleting user {user.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception deleting user: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ApplicationUser>> GetUsersByRoleAsync(string role)
    {
        try
        {
            return await _context.Users
                .Where(u => u.Role == role && u.IsActive)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving users by role: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> DeactivateUserAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            user.IsActive = false;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation($"User {user.Email} deactivated");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception deactivating user: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> ActivateUserAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            user.IsActive = true;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                _logger.LogInformation($"User {user.Email} activated");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception activating user: {ex.Message}");
            throw;
        }
    }
}
