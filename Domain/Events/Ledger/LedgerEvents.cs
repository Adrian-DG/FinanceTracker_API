using Domain.Common;
using Domain.Enums;
using System;

namespace Domain.Events.Ledger
{
	public sealed record TransactionRegisteredDomainEvent(
		Guid TransactionId,
		TransactionType Type,
		decimal Amount,
		DateTime TransactionDate) : IDomainEvent
	{
		public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
	}
}
