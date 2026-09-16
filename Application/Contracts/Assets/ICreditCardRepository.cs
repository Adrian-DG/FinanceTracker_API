using Domain.Entities.Assets;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Contracts.Assets
{
	public interface ICreditCardRepository : IRepository<CreditCard>
	{
		/// <summary>Carga la tarjeta con sus agrupaciones: el agregado completo.</summary>
		Task<CreditCard?> GetWithGroupsAsync(Guid id, CancellationToken cancellationToken = default);
	}
}
