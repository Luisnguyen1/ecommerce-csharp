using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Core.Services;

public class AuthService(IRepository<AppUser> users) : IAuthService
{
    private readonly PasswordHasher<AppUser> _hasher = new();

    public async Task<Result<AppUser>> RegisterAsync(string email, string fullName, string password, CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result<AppUser>.Fail("Enter a valid email address.");
        if (string.IsNullOrWhiteSpace(fullName))
            return Result<AppUser>.Fail("Enter your full name.");
        if (password.Length < 8)
            return Result<AppUser>.Fail("Password must be at least 8 characters.");

        var existing = (await users.GetAllAsync(ct)).Any(u => u.Email == email);
        if (existing)
            return Result<AppUser>.Fail("An account with this email already exists.");

        var user = new AppUser
        {
            Email = email,
            FullName = fullName.Trim(),
            Role = Roles.Customer
        };
        user.PasswordHash = _hasher.HashPassword(user, password);
        await users.AddAsync(user, ct);
        return Result<AppUser>.Ok(user);
    }

    public async Task<AppUser?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();
        var user = (await users.GetAllAsync(ct)).FirstOrDefault(u => u.Email == email);
        if (user is null) return null;

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Success ? user : null;
    }

    public Task<AppUser?> GetByIdAsync(string id, CancellationToken ct = default) => users.GetByIdAsync(id, ct);

    public Task<IReadOnlyList<AppUser>> GetAllAsync(CancellationToken ct = default) => users.GetAllAsync(ct);
}
