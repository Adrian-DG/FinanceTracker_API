using Domain.Enums;
using Domain.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities.Assets
{
	public class CreditCardGroup : BaseEntity, ISyncEntity
	{
		public required string Name { get; set; }
		public CurrencyCode Currency { get; set; }
		public Guid CardId { get; set; }
		public virtual CreditCard? Card { get; set; }
		public SyncStatus SyncStatus { get; set; }
	}
}
