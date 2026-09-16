using Domain.Entities.Assets;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Contracts.Assets
{
	public interface IBankRepository : IRepository<Bank>
	{
		Task<bool> ExistsWithNameAsync(string name, CancellationToken cancellationToken = default);
	}
}
