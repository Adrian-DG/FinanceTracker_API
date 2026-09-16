using Application.Common.Exceptions;
using Application.Contracts;
using Domain.Entities.Assets;
using Domain.Enums;
using FluentValidation;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.BankAccounts;

public record BankAccountResponse(
	Guid Id,
	string Name,
	AccountType Type,
	decimal CurrentBalance,
	CurrencyCode Currency,
	bool AllowsOverdraft,
	Guid BankId,
	string? BankName);

public record GetBankAccountByIdQuery(Guid Id) : IRequest<BankAccountResponse>;

public class GetBankAccountByIdValidator : AbstractValidator<GetBankAccountByIdQuery>
{
	public GetBankAccountByIdValidator()
	{
		RuleFor(x => x.Id)
			.NotEmpty();
	}
}

public class GetBankAccountByIdHandler(IUnitOfWork uow) : IRequestHandler<GetBankAccountByIdQuery, BankAccountResponse>
{
	private readonly IUnitOfWork _uow = uow;

	public async Task<BankAccountResponse> Handle(GetBankAccountByIdQuery request, CancellationToken cancellationToken)
	{
		var account = await _uow.BankAccountRepository.GetByIdAsync(request.Id, cancellationToken)
			?? throw new NotFoundException(nameof(BankAccount), request.Id);

		return new BankAccountResponse(
			account.Id,
			account.Name,
			account.Type,
			account.CurrentBalance,
			account.Currency,
			account.AllowsOverdraft,
			account.BankId,
			account.Bank?.Name);
	}
}
