using Domain.Entities.Assets;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Contracts.Assets
{
	public interface IBankAccountRepository : IRepository<BankAccount>
	{
		Task<IReadOnlyList<BankAccount>> ListByBankAsync(Guid bankId, CancellationToken cancellationToken = default);

		Task<bool> ExistsWithNameAsync(Guid bankId, string name, CancellationToken cancellationToken = default);
	}
}
