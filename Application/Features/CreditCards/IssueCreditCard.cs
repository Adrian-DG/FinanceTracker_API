using Application.Common.Exceptions;
using Application.Contracts;
using Domain.Entities.Assets;
using Domain.Enums;
using FluentValidation;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.CreditCards;

public record IssueCreditCardCommand(
	string Name,
	string? Alias,
	decimal CreditLimit,
	CurrencyCode Currency,
	int CutOffDay,
	int DueDay,
	DateOnly ExpirationDate,
	Guid BankId,
	decimal InitialBalance
) : IRequest<Guid>;

public class IssueCreditCardValidator : AbstractValidator<IssueCreditCardCommand>
{
	public IssueCreditCardValidator()
	{
		RuleFor(x => x.Name)
			.NotEmpty()
			.MaximumLength(CreditCard.MaxNameLength);
		RuleFor(x => x.CreditLimit)
			.GreaterThan(0);
		RuleFor(x => x.Currency)
			.IsInEnum();
		// Se limita a 28 para que el corte y el vencimiento existan en todos los meses.
		RuleFor(x => x.CutOffDay)
			.InclusiveBetween(1, 28);
		RuleFor(x => x.DueDay)
			.InclusiveBetween(1, 28);
		RuleFor(x => x.BankId)
			.NotEmpty();
		RuleFor(x => x.InitialBalance)
			.GreaterThanOrEqualTo(0);
	}
}

public class IssueCreditCardHandler(IUnitOfWork uow) : IRequestHandler<IssueCreditCardCommand, Guid>
{
	private readonly IUnitOfWork _uow = uow;

	public async Task<Guid> Handle(IssueCreditCardCommand request, CancellationToken cancellationToken)
	{
		if (!await _uow.BankRepository.ExistsAsync(request.BankId, cancellationToken))
			throw new NotFoundException(nameof(Bank), request.BankId);

		var card = CreditCard.Issue(
			request.Name,
			request.CreditLimit,
			request.Currency,
			request.CutOffDay,
			request.DueDay,
			request.ExpirationDate,
			request.BankId,
			request.Alias,
			request.InitialBalance);

		await _uow.CreditCardRepository.AddAsync(card, cancellationToken);
		await _uow.SaveChangesAsync(cancellationToken);

		return card.Id;
	}
}
