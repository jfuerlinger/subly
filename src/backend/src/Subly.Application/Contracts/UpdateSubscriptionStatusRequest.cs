using Subly.Domain.Models;

namespace Subly.Application.Contracts;

public sealed record UpdateSubscriptionStatusRequest(SubscriptionStatus Status);
