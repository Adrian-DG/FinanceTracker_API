using Application.Contracts.Assets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Contracts
{
	public interface IUnitOfWork
	{
		IBankAccountRepository BankAccountRepository { get; }

		Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
	}
}
