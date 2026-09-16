using System.Text.RegularExpressions;

namespace Domain.Common
{
	/// <summary>
	/// Comportamiento compartido por las entidades de catálogo (bancos, categorías,
	/// subcategorías): nombre, descripción y presentación visual en la app.
	/// </summary>
	public abstract partial class CatalogEntity : AuditableEntity
	{
		public const int MaxNameLength = 100;
		public const int MaxDescriptionLength = 250;
		public const string DefaultIcon = "default_icon";
		public const string DefaultColorHex = "#000000";

		protected CatalogEntity() { }

		public string Name { get; private set; } = string.Empty;

		public string? Description { get; private set; }

		public string Icon { get; private set; } = DefaultIcon;

		public string ColorHex { get; private set; } = DefaultColorHex;

		/// <summary>Los elementos precargados por el sistema no se pueden eliminar.</summary>
		public bool IsSystemDefault { get; private set; }

		protected void SetDescriptor(string name, string? description, string? icon, string? colorHex, bool isSystemDefault)
		{
			Name = NormalizeName(name);
			Description = NormalizeDescription(description);
			Icon = string.IsNullOrWhiteSpace(icon) ? DefaultIcon : icon.Trim();
			ColorHex = NormalizeColor(colorHex);
			IsSystemDefault = isSystemDefault;
		}

		public void Rename(string name)
		{
			Name = NormalizeName(name);
			MarkUpdated();
		}

		public void UpdateDescription(string? description)
		{
			Description = NormalizeDescription(description);
			MarkUpdated();
		}

		public void UpdateAppearance(string? icon, string? colorHex)
		{
			Icon = string.IsNullOrWhiteSpace(icon) ? DefaultIcon : icon.Trim();
			ColorHex = NormalizeColor(colorHex);
			MarkUpdated();
		}

		public override void Delete()
		{
			Guard.Against(IsSystemDefault, $"'{Name}' es un elemento del sistema y no se puede eliminar.");
			base.Delete();
		}

		private static string NormalizeName(string name)
			=> Guard.AgainstExceedingLength(Guard.AgainstNullOrWhiteSpace(name), MaxNameLength);

		private static string? NormalizeDescription(string? description)
		{
			if (string.IsNullOrWhiteSpace(description))
				return null;

			return Guard.AgainstExceedingLength(description.Trim(), MaxDescriptionLength, nameof(Description));
		}

		private static string NormalizeColor(string? colorHex)
		{
			if (string.IsNullOrWhiteSpace(colorHex))
				return DefaultColorHex;

			var candidate = colorHex.Trim().ToUpperInvariant();

			Guard.Against(!HexColorPattern().IsMatch(candidate), $"'{candidate}' no es un color hexadecimal válido (formato esperado: #RRGGBB).");

			return candidate;
		}

		[GeneratedRegex("^#[0-9A-F]{6}$")]
		private static partial Regex HexColorPattern();
	}
}
