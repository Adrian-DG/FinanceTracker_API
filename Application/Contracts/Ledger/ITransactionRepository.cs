using Domain.Entities.Ledger;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Contracts.Ledger
{
	public interface ITransactionRepository : IRepository<Transaction>
	{
		Task<IReadOnlyList<Transaction>> ListByBankAccountAsync(
			Guid bankAccountId,
			DateTime fromUtc,
			DateTime toUtc,
			CancellationToken cancellationToken = default);
	}
}
