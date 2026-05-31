using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Metadata
{
	public abstract	class BaseEntity
	{
		public Guid Id { get; set; }
		public bool IsDeleted{ get; set; }
	}
}
