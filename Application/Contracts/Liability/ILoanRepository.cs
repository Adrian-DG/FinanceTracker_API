using Domain.Entities.Liability;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Contracts.Liability
{
	public interface ILoanRepository : IRepository<Loan>
	{
		/// <summary>
		/// Carga el préstamo con su cuadro de amortización y sus pagos. Las operaciones
		/// del agregado recalculan el cuadro, así que necesitan el grafo completo.
		/// </summary>
		Task<Loan?> GetWithScheduleAsync(Guid id, CancellationToken cancellationToken = default);
	}
}
