using Application.Contracts;
using Domain.Common;
using Domain.Entities.Assets;
using FluentValidation;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Banks;

public record CreateBankCommand(
	string Name,
	string? Description,
	string? Icon,
	string? ColorHex
) : IRequest<Guid>;

public class CreateBankValidator : AbstractValidator<CreateBankCommand>
{
	public CreateBankValidator()
	{
		RuleFor(x => x.Name)
			.NotEmpty()
			.MaximumLength(CatalogEntity.MaxNameLength);
		RuleFor(x => x.Description)
			.MaximumLength(CatalogEntity.MaxDescriptionLength);
	}
}

public class CreateBankHandler(IUnitOfWork uow) : IRequestHandler<CreateBankCommand, Guid>
{
	private readonly IUnitOfWork _uow = uow;

	public async Task<Guid> Handle(CreateBankCommand request, CancellationToken cancellationToken)
	{
		if (await _uow.BankRepository.ExistsWithNameAsync(request.Name, cancellationToken))
			throw new DomainException($"Ya existe un banco llamado '{request.Name}'.");

		var bank = Bank.Create(request.Name, request.Description, request.Icon, request.ColorHex);

		await _uow.BankRepository.AddAsync(bank, cancellationToken);
		await _uow.SaveChangesAsync(cancellationToken);

		return bank.Id;
	}
}
