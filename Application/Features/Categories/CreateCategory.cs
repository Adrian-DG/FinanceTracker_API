using Application.Contracts;
using Domain.Common;
using Domain.Entities.Assets;
using FluentValidation;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Categories;

public record CreateCategoryCommand(
	string Name,
	string? Description,
	string? Icon,
	string? ColorHex
) : IRequest<Guid>;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
	public CreateCategoryValidator()
	{
		RuleFor(x => x.Name)
			.NotEmpty()
			.MaximumLength(CatalogEntity.MaxNameLength);
		RuleFor(x => x.Description)
			.MaximumLength(CatalogEntity.MaxDescriptionLength);
	}
}

public class CreateCategoryHandler(IUnitOfWork uow) : IRequestHandler<CreateCategoryCommand, Guid>
{
	private readonly IUnitOfWork _uow = uow;

	public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
	{
		var category = Category.Create(request.Name, request.Description, request.Icon, request.ColorHex);

		await _uow.CategoryRepository.AddAsync(category, cancellationToken);
		await _uow.SaveChangesAsync(cancellationToken);

		return category.Id;
	}
}
