using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Models;

namespace ECommerce.Core.Services;

public class ReviewService(IRepository<Review> reviews) : IReviewService
{
    public async Task<IReadOnlyList<Review>> GetForProductAsync(string productId, CancellationToken ct = default) =>
        (await reviews.GetAllAsync(ct))
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

    public async Task<IReadOnlyList<Review>> GetByUserAsync(string userId, CancellationToken ct = default) =>
        (await reviews.GetAllAsync(ct))
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

    public async Task<Result> UpsertAsync(string userId, string userName, string productId, int rating, string title, string body, CancellationToken ct = default)
    {
        if (rating is < 1 or > 5) return Result.Fail("Rating must be between 1 and 5 stars.");
        if (string.IsNullOrWhiteSpace(body)) return Result.Fail("Please write a few words about the product.");
        if (body.Length > 2000) return Result.Fail("Review is too long (2000 characters max).");

        var existing = (await reviews.GetAllAsync(ct))
            .FirstOrDefault(r => r.UserId == userId && r.ProductId == productId);

        if (existing is not null)
        {
            existing.Rating = rating;
            existing.Title = title.Trim();
            existing.Body = body.Trim();
            await reviews.UpdateAsync(existing, ct);
        }
        else
        {
            await reviews.AddAsync(new Review
            {
                ProductId = productId,
                UserId = userId,
                UserName = userName,
                Rating = rating,
                Title = title.Trim(),
                Body = body.Trim()
            }, ct);
        }
        return Result.Ok();
    }

    public async Task<IReadOnlyDictionary<string, RatingSummary>> GetSummariesAsync(CancellationToken ct = default)
    {
        var grouped = (await reviews.GetAllAsync(ct))
            .GroupBy(r => r.ProductId)
            .ToDictionary(
                g => g.Key,
                g => new RatingSummary { Average = Math.Round(g.Average(r => r.Rating), 1), Count = g.Count() });
        return grouped;
    }
}
