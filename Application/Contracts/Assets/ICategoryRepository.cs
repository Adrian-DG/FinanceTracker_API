using Domain.Entities.Assets;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Contracts.Assets
{
	public interface ICategoryRepository : IRepository<Category>
	{
		Task<Category?> GetWithSubCategoriesAsync(Guid id, CancellationToken cancellationToken = default);

		Task<bool> SubCategoryBelongsToCategoryAsync(Guid categoryId, Guid subCategoryId, CancellationToken cancellationToken = default);
	}
}
