using Domain.Metadata;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Contracts
{
	/// <summary>
	/// Acceso básico a una raíz de agregado. Las consultas específicas viven en el
	/// repositorio de cada agregado; aquí solo lo que aplica a todos.
	/// El borrado es lógico y lo decide la entidad, por eso no hay un Remove.
	/// </summary>
	public interface IRepository<TEntity> where TEntity : BaseEntity
	{
		Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

		Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default);

		Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

		Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
	}
}
