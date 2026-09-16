using Domain.Common;
using System;

namespace Domain.Entities.Liability
{
	/// <summary>
	/// Tasa pactada con el banco y su fecha de vigencia. El préstamo la registra en
	/// cada revisión para poder reconstruir cómo evolucionó el costo del crédito.
	/// </summary>
	public class LoanRateHistory : AuditableEntity
	{
		private LoanRateHistory() { }

		/// <summary>Tasa anual expresada como porcentaje (12.5 significa 12.5 %).</summary>
		public decimal Rate { get; private set; }

		public DateOnly EffectiveDate { get; private set; }

		public string? Note { get; private set; }

		public Guid LoanId { get; private set; }

		public Loan? Loan { get; private set; }

		internal static LoanRateHistory Create(Guid loanId, decimal rate, DateOnly effectiveDate, string? note) => new()
		{
			LoanId = Guard.AgainstEmpty(loanId),
			Rate = Guard.AgainstNegative(rate),
			EffectiveDate = effectiveDate,
			Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
		};
	}
}
