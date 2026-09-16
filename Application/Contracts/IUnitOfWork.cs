using Application.Contracts.Assets;
using Application.Contracts.Ledger;
using Application.Contracts.Liability;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Contracts
{
	/// <summary>
	/// Una sola transacción por caso de uso: los handlers modifican varios agregados
	/// (cuenta + movimiento, préstamo + movimiento) y confirman todo junto.
	/// </summary>
	public interface IUnitOfWork
	{
		IBankRepository BankRepository { get; }

		IBankAccountRepository BankAccountRepository { get; }

		ICreditCardRepository CreditCardRepository { get; }

		ICategoryRepository CategoryRepository { get; }

		ITransactionRepository TransactionRepository { get; }

		ILoanRepository LoanRepository { get; }

		Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
	}
}
