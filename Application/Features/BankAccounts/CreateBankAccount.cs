using Application.Common.Exceptions;
using Application.Contracts;
using Domain.Entities.Assets;
using Domain.Enums;
using FluentValidation;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.BankAccounts;

public record CreateBankAccountCommand(
	string Name,
	AccountType Type,
	decimal CurrentBalance,
	CurrencyCode Currency,
	Guid BankId
) : IRequest<Guid>;

public class CreateBankAccountValidator : AbstractValidator<CreateBankAccountCommand>
{
	public CreateBankAccountValidator()
	{
		RuleFor(x => x.Name)
			.NotEmpty()
			.MaximumLength(BankAccount.MaxNameLength);
		RuleFor(x => x.Type)
			.IsInEnum();
		RuleFor(x => x.Currency)
			.IsInEnum();
		RuleFor(x => x.BankId)
			.NotEmpty();
		// El saldo inicial negativo no se rechaza aquí: solo la cuenta corriente
		// admite sobregiro y esa regla pertenece al dominio.
	}
}

public class CreateBankAccountHandler(IUnitOfWork uow) : IRequestHandler<CreateBankAccountCommand, Guid>
{
	private readonly IUnitOfWork _uow = uow;

	public async Task<Guid> Handle(CreateBankAccountCommand request, CancellationToken cancellationToken)
	{
		if (!await _uow.BankRepository.ExistsAsync(request.BankId, cancellationToken))
			throw new NotFoundException(nameof(Bank), request.BankId);

		var account = BankAccount.Open(
			request.Name,
			request.Type,
			request.CurrentBalance,
			request.Currency,
			request.BankId);

		await _uow.BankAccountRepository.AddAsync(account, cancellationToken);
		await _uow.SaveChangesAsync(cancellationToken);

		return account.Id;
	}
}
