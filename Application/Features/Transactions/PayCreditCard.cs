using Application.Common.Exceptions;
using Application.Contracts;
using Domain.Entities.Assets;
using Domain.Entities.Ledger;
using FluentValidation;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Transactions;

public record PayCreditCardCommand(
	decimal Amount,
	DateTime TransactionDate,
	Guid BankAccountId,
	Guid CreditCardId,
	Guid? CreditCardGroupId,
	string? Description
) : IRequest<Guid>;

public class PayCreditCardValidator : AbstractValidator<PayCreditCardCommand>
{
	public PayCreditCardValidator()
	{
		RuleFor(x => x.Amount)
			.GreaterThan(0);
		RuleFor(x => x.TransactionDate)
			.NotEmpty();
		RuleFor(x => x.BankAccountId)
			.NotEmpty();
		RuleFor(x => x.CreditCardId)
			.NotEmpty();
		RuleFor(x => x.Description)
			.MaximumLength(Transaction.MaxDescriptionLength);
	}
}

public class PayCreditCardHandler(IUnitOfWork uow) : IRequestHandler<PayCreditCardCommand, Guid>
{
	private readonly IUnitOfWork _uow = uow;

	public async Task<Guid> Handle(PayCreditCardCommand request, CancellationToken cancellationToken)
	{
		var account = await _uow.BankAccountRepository.GetByIdAsync(request.BankAccountId, cancellationToken)
			?? throw new NotFoundException(nameof(BankAccount), request.BankAccountId);

		var card = await _uow.CreditCardRepository.GetWithGroupsAsync(request.CreditCardId, cancellationToken)
			?? throw new NotFoundException(nameof(CreditCard), request.CreditCardId);

		account.EnsureCurrencyMatches(card.Currency);

		var transaction = Transaction.RegisterCreditCardPayment(
			request.Amount,
			request.TransactionDate,
			account.Id,
			card.Id,
			request.CreditCardGroupId,
			request.Description);

		// El dinero sale de la cuenta y reduce la deuda de la tarjeta: dos agregados,
		// una sola transacción de base de datos.
		account.Debit(request.Amount);
		card.RegisterPayment(request.Amount, request.CreditCardGroupId);

		await _uow.TransactionRepository.AddAsync(transaction, cancellationToken);
		await _uow.SaveChangesAsync(cancellationToken);

		return transaction.Id;
	}
}
