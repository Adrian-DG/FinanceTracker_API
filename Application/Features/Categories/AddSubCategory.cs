using Application.Common.Exceptions;
using Application.Contracts;
using Domain.Common;
using Domain.Entities.Assets;
using FluentValidation;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Categories;

public record AddSubCategoryCommand(
	Guid CategoryId,
	string Name,
	string? Description,
	string? Icon,
	string? ColorHex
) : IRequest<Guid>;

public class AddSubCategoryValidator : AbstractValidator<AddSubCategoryCommand>
{
	public AddSubCategoryValidator()
	{
		RuleFor(x => x.CategoryId)
			.NotEmpty();
		RuleFor(x => x.Name)
			.NotEmpty()
			.MaximumLength(CatalogEntity.MaxNameLength);
		RuleFor(x => x.Description)
			.MaximumLength(CatalogEntity.MaxDescriptionLength);
	}
}

public class AddSubCategoryHandler(IUnitOfWork uow) : IRequestHandler<AddSubCategoryCommand, Guid>
{
	private readonly IUnitOfWork _uow = uow;

	public async Task<Guid> Handle(AddSubCategoryCommand request, CancellationToken cancellationToken)
	{
		// La subcategoría nace dentro del agregado: por eso se carga la categoría completa.
		var category = await _uow.CategoryRepository.GetWithSubCategoriesAsync(request.CategoryId, cancellationToken)
			?? throw new NotFoundException(nameof(Category), request.CategoryId);

		var subCategory = category.AddSubCategory(request.Name, request.Description, request.Icon, request.ColorHex);

		await _uow.SaveChangesAsync(cancellationToken);

		return subCategory.Id;
	}
}
