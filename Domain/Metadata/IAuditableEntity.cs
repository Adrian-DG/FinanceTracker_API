using System;

namespace Domain.Metadata
{
	public interface IAuditableEntity
	{
		DateTime CreatedAtUtc { get; }
		DateTime UpdatedAtUtc { get; }
	}
}
