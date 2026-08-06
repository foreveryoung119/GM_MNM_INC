using Microsoft.AspNetCore.Identity;
using InsuranceClaimsSystem.Models;

namespace InsuranceClaimsSystem.Services;

/// <summary>
/// Service interface for managing users.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="user">The user to create.</param>
    /// <param name="password">The user's password.</param>
    /// <returns>Result of the user creation operation.</returns>
    Task<IdentityResult> CreateUserAsync(ApplicationUser user, string password);

    /// <summary>
    /// Gets all users.
    /// </summary>
    /// <returns>List of all users.</returns>
    Task<List<ApplicationUser>> GetAllUsersAsync();

    /// <summary>
    /// Gets a specific user by ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The user or null if not found.</returns>
    Task<ApplicationUser?> GetUserByIdAsync(string userId);

    /// <summary>
    /// Gets a specific user by email.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <returns>The user or null if not found.</returns>
    Task<ApplicationUser?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Gets all assessors in the system.
    /// </summary>
    /// <returns>List of assessor users.</returns>
    Task<List<ApplicationUser>> GetAllAssessorsAsync();

    /// <summary>
    /// Gets all broker company officers in the system.
    /// </summary>
    /// <returns>List of broker users.</returns>
    Task<List<ApplicationUser>> GetAllBrokersAsync();

    /// <summary>
    /// Updates user information.
    /// </summary>
    /// <param name="user">The user with updated information.</param>
    /// <returns>Result of the update operation.</returns>
    Task<IdentityResult> UpdateUserAsync(ApplicationUser user);

    /// <summary>
    /// Changes a user's password.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="currentPassword">The current password.</param>
    /// <param name="newPassword">The new password.</param>
    /// <returns>Result of the password change operation.</returns>
    Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword);

    /// <summary>
    /// Resets a user's password without requiring the current password.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="newPassword">The new password.</param>
    /// <returns>Result of the password reset operation.</returns>
    Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string newPassword);

    /// <summary>
    /// Deletes a user.
    /// </summary>
    /// <param name="user">The user to delete.</param>
    /// <returns>Result of the delete operation.</returns>
    Task<IdentityResult> DeleteUserAsync(ApplicationUser user);

    /// <summary>
    /// Gets users by role.
    /// </summary>
    /// <param name="role">The role name.</param>
    /// <returns>List of users with specified role.</returns>
    Task<List<ApplicationUser>> GetUsersByRoleAsync(string role);

    /// <summary>
    /// Deactivates a user (sets IsActive to false).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>Result of the deactivation operation.</returns>
    Task<IdentityResult> DeactivateUserAsync(string userId);

    /// <summary>
    /// Activates a user (sets IsActive to true).
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>Result of the activation operation.</returns>
    Task<IdentityResult> ActivateUserAsync(string userId);
}
