using Domain.Entities.Assets;
using Domain.Enums;
using Domain.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities.Liability
{
	public class Loan : BaseEntity, ISyncEntity, IAuditableEntity
	{
		public required string Concept { get; set; }
		public decimal InitialAmmount { get; set; }
		public decimal RemainingAmmount { get; private set; }
		public int TotalTermMonths { get; set; }
		public decimal CurrentMontlyFee { get; set; }

		public Guid BankId { get; set; }
		public virtual Bank? Bank { get; set; }

		public SyncStatus SyncStatus { get; set; }
		public DateTime CreateAtUtc { get; set; }
		public DateTime UpdateAtUtc { get; set; }

		// Methods 

		public void UpdateRemainingAmmount(decimal amount)
		{
			RemainingAmmount = amount;
		}

		public void ApplyPayment(decimal paymentAmount)
		{
			if (paymentAmount > RemainingAmmount)
				throw new InvalidOperationException("El pago no puede ser mayor al monto restante");

			RemainingAmmount -= paymentAmount;
		}
	}
}
