using Domain.Common;
using System;

namespace Domain.Entities.Assets
{
	/// <summary>
	/// Clasificación de segundo nivel. Solo existe dentro de una categoría:
	/// se crea a través de <see cref="Category.AddSubCategory"/>.
	/// </summary>
	public class SubCategory : CatalogEntity
	{
		private SubCategory() { }

		public Guid CategoryId { get; private set; }

		public Category? Category { get; private set; }

		internal static SubCategory Create(
			Guid categoryId,
			string name,
			string? description,
			string? icon,
			string? colorHex,
			bool isSystemDefault)
		{
			var subCategory = new SubCategory { CategoryId = Guard.AgainstEmpty(categoryId) };
			subCategory.SetDescriptor(name, description, icon, colorHex, isSystemDefault);

			return subCategory;
		}
	}
}
