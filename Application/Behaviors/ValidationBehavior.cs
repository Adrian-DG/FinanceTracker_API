using FluentValidation;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Behaviors
{
	/// <summary>
	/// Ejecuta los validadores del request antes de llegar al handler. Es la frontera
	/// de la aplicación: filtra lo que está mal formado para que el dominio solo tenga
	/// que preocuparse por sus propias invariantes.
	/// </summary>
	public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
		: IPipelineBehavior<TRequest, TResponse>
		where TRequest : notnull
	{
		private readonly IEnumerable<IValidator<TRequest>> _validators = validators;

		public async Task<TResponse> Handle(
			TRequest request,
			RequestHandlerDelegate<TResponse> next,
			CancellationToken cancellationToken)
		{
			if (!_validators.Any())
				return await next(cancellationToken);

			var context = new ValidationContext<TRequest>(request);

			var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));

			var failures = results
				.SelectMany(result => result.Errors)
				.Where(failure => failure is not null)
				.ToList();

			if (failures.Count > 0)
				throw new ValidationException(failures);

			return await next(cancellationToken);
		}
	}
}
