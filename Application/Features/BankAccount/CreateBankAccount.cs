using Application.Contracts;
using Domain.Enums;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.BankAccount;

public record CreateBankAccountCommand(
	string Name,
	decimal CurrentBalance,
	CurrencyCode Currency,
	Guid BankId
) : IRequest<Guid>;

public class  CreateBankAccountValidator : AbstractValidator<CreateBankAccountCommand>
{
	public CreateBankAccountValidator()
	{
		RuleFor(x => x.Name)
			.NotEmpty()
			.MaximumLength(100);
		RuleFor(x => x.CurrentBalance)
			.GreaterThanOrEqualTo(0);
		RuleFor(x => x.Currency)
			.IsInEnum();
		RuleFor(x => x.BankId)
			.NotEmpty();
	}
}

public class CreateBankAccountHandler(IUnitOfWork uow) : IRequestHandler<CreateBankAccountCommand, Guid>
{
	private readonly IUnitOfWork _uow = uow;
	public Task<Guid> Handle(CreateBankAccountCommand request, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}