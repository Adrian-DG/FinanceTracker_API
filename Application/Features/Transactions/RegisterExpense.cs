using Application.Common.Exceptions;
using Application.Contracts;
using Domain.Entities.Assets;
using Domain.Entities.Ledger;
using FluentValidation;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Transactions;

/// <summary>
/// Un gasto se paga con una cuenta o con una tarjeta, nunca con ambas: con cuenta
/// descuenta el saldo, con tarjeta aumenta la deuda y consume crédito disponible.
/// </summary>
public record RegisterExpenseCommand(
	decimal Amount,
	DateTime TransactionDate,
	Guid CategoryId,
	Guid? SubCategoryId,
	Guid? BankAccountId,
	Guid? CreditCardId,
	Guid? CreditCardGroupId,
	string? Description
) : IRequest<Guid>;

public class RegisterExpenseValidator : AbstractValidator<RegisterExpenseCommand>
{
	public RegisterExpenseValidator()
	{
		RuleFor(x => x.Amount)
			.GreaterThan(0);
		RuleFor(x => x.TransactionDate)
			.NotEmpty();
		RuleFor(x => x.CategoryId)
			.NotEmpty();
		RuleFor(x => x.Description)
			.MaximumLength(Transaction.MaxDescriptionLength);
		RuleFor(x => x)
			.Must(x => x.BankAccountId.HasValue ^ x.CreditCardId.HasValue)
			.WithMessage("Indique la cuenta o la tarjeta con la que se pagó el gasto, pero no ambas.");
		RuleFor(x => x.CreditCardGroupId)
			.Must((command, _) => command.CreditCardId.HasValue)
			.When(x => x.CreditCardGroupId.HasValue)
			.WithMessage("La agrupación de consumo requiere indicar la tarjeta.");
	}
}

public class RegisterExpenseHandler(IUnitOfWork uow) : IRequestHandler<RegisterExpenseCommand, Guid>
{
	private readonly IUnitOfWork _uow = uow;

	public async Task<Guid> Handle(RegisterExpenseCommand request, CancellationToken cancellationToken)
	{
		await RegisterIncomeHandler.EnsureClassificationExistsAsync(_uow, request.CategoryId, request.SubCategoryId, cancellationToken);

		var transaction = request.CreditCardId.HasValue
			? await ChargeToCardAsync(request, cancellationToken)
			: await ChargeToAccountAsync(request, cancellationToken);

		await _uow.TransactionRepository.AddAsync(transaction, cancellationToken);
		await _uow.SaveChangesAsync(cancellationToken);

		return transaction.Id;
	}

	private async Task<Transaction> ChargeToAccountAsync(RegisterExpenseCommand request, CancellationToken cancellationToken)
	{
		var accountId = request.BankAccountId!.Value;

		var account = await _uow.BankAccountRepository.GetByIdAsync(accountId, cancellationToken)
			?? throw new NotFoundException(nameof(BankAccount), accountId);

		var transaction = Transaction.RegisterExpenseFromAccount(
			request.Amount,
			request.TransactionDate,
			account.Id,
			request.CategoryId,
			request.SubCategoryId,
			request.Description);

		account.Debit(request.Amount);

		return transaction;
	}

	private async Task<Transaction> ChargeToCardAsync(RegisterExpenseCommand request, CancellationToken cancellationToken)
	{
		var cardId = request.CreditCardId!.Value;

		var card = await _uow.CreditCardRepository.GetWithGroupsAsync(cardId, cancellationToken)
			?? throw new NotFoundException(nameof(CreditCard), cardId);

		var transaction = Transaction.RegisterExpenseOnCreditCard(
			request.Amount,
			request.TransactionDate,
			card.Id,
			request.CategoryId,
			request.CreditCardGroupId,
			request.SubCategoryId,
			request.Description);

		card.Charge(request.Amount, DateOnly.FromDateTime(request.TransactionDate), request.CreditCardGroupId);

		return transaction;
	}
}
