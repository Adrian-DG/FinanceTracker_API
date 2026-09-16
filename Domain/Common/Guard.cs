using System;
using System.Runtime.CompilerServices;

namespace Domain.Common
{
	/// <summary>
	/// Validaciones de invariantes para usar dentro de las entidades.
	/// A diferencia de FluentValidation (que protege la frontera de la aplicación),
	/// estas garantías viven en el dominio y siempre se ejecutan.
	/// </summary>
	public static class Guard
	{
		public static string AgainstNullOrWhiteSpace(string? value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
		{
			if (string.IsNullOrWhiteSpace(value))
				throw new DomainException($"'{parameterName}' es obligatorio.");

			return value.Trim();
		}

		public static string AgainstExceedingLength(string value, int maxLength, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
		{
			if (value.Length > maxLength)
				throw new DomainException($"'{parameterName}' no puede exceder {maxLength} caracteres.");

			return value;
		}

		public static decimal AgainstNegative(decimal value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
		{
			if (value < 0)
				throw new DomainException($"'{parameterName}' no puede ser negativo.");

			return value;
		}

		public static decimal AgainstNonPositive(decimal value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
		{
			if (value <= 0)
				throw new DomainException($"'{parameterName}' debe ser mayor que cero.");

			return value;
		}

		public static int AgainstNonPositive(int value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
		{
			if (value <= 0)
				throw new DomainException($"'{parameterName}' debe ser mayor que cero.");

			return value;
		}

		public static Guid AgainstEmpty(Guid value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
		{
			if (value == Guid.Empty)
				throw new DomainException($"'{parameterName}' es obligatorio.");

			return value;
		}

		public static int AgainstOutOfRange(int value, int min, int max, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
		{
			if (value < min || value > max)
				throw new DomainException($"'{parameterName}' debe estar entre {min} y {max}.");

			return value;
		}

		public static void Against(bool condition, string message)
		{
			if (condition)
				throw new DomainException(message);
		}
	}
}
