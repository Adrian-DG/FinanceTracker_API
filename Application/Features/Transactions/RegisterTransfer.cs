using Application.Common.Exceptions;
using Application.Contracts;
using Domain.Entities.Assets;
using Domain.Entities.Ledger;
using FluentValidation;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Transactions;

public record RegisterTransferCommand(
	decimal Amount,
	DateTime TransactionDate,
	Guid OriginBankAccountId,
	Guid DestinationBankAccountId,
	string? Description
) : IRequest<Guid>;

public class RegisterTransferValidator : AbstractValidator<RegisterTransferCommand>
{
	public RegisterTransferValidator()
	{
		RuleFor(x => x.Amount)
			.GreaterThan(0);
		RuleFor(x => x.TransactionDate)
			.NotEmpty();
		RuleFor(x => x.OriginBankAccountId)
			.NotEmpty();
		RuleFor(x => x.DestinationBankAccountId)
			.NotEmpty()
			.NotEqual(x => x.OriginBankAccountId)
			.WithMessage("La cuenta de destino debe ser distinta de la de origen.");
		RuleFor(x => x.Description)
			.MaximumLength(Transaction.MaxDescriptionLength);
	}
}

public class RegisterTransferHandler(IUnitOfWork uow) : IRequestHandler<RegisterTransferCommand, Guid>
{
	private readonly IUnitOfWork _uow = uow;

	public async Task<Guid> Handle(RegisterTransferCommand request, CancellationToken cancellationToken)
	{
		var origin = await _uow.BankAccountRepository.GetByIdAsync(request.OriginBankAccountId, cancellationToken)
			?? throw new NotFoundException(nameof(BankAccount), request.OriginBankAccountId);

		var destination = await _uow.BankAccountRepository.GetByIdAsync(request.DestinationBankAccountId, cancellationToken)
			?? throw new NotFoundException(nameof(BankAccount), request.DestinationBankAccountId);

		// Sin tasa de cambio en el modelo, una transferencia solo tiene sentido
		// entre cuentas de la misma moneda.
		origin.EnsureCurrencyMatches(destination.Currency);

		var transaction = Transaction.RegisterTransfer(
			request.Amount,
			request.TransactionDate,
			origin.Id,
			destination.Id,
			request.Description);

		origin.Debit(request.Amount);
		destination.Credit(request.Amount);

		await _uow.TransactionRepository.AddAsync(transaction, cancellationToken);
		await _uow.SaveChangesAsync(cancellationToken);

		return transaction.Id;
	}
}
