using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Metadata
{
	public interface IAuditableEntity
	{
		public DateTime CreateAtUtc { get; set; }
		public DateTime UpdateAtUtc { get; set; }
	}
}
