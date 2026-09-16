using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities.Assets
{
	/// <summary>
	/// Clasificación de primer nivel de los movimientos. Es la raíz del agregado
	/// que contiene sus subcategorías.
	/// </summary>
	public class Category : CatalogEntity
	{
		private readonly List<SubCategory> _subCategories = new();

		private Category() { }

		public IReadOnlyCollection<SubCategory> SubCategories => _subCategories.AsReadOnly();

		public static Category Create(
			string name,
			string? description = null,
			string? icon = null,
			string? colorHex = null,
			bool isSystemDefault = false)
		{
			var category = new Category();
			category.SetDescriptor(name, description, icon, colorHex, isSystemDefault);

			return category;
		}

		public SubCategory AddSubCategory(
			string name,
			string? description = null,
			string? icon = null,
			string? colorHex = null,
			bool isSystemDefault = false)
		{
			var normalized = Guard.AgainstNullOrWhiteSpace(name).ToUpperInvariant();

			Guard.Against(
				_subCategories.Any(x => !x.IsDeleted && x.Name.ToUpperInvariant() == normalized),
				$"La categoría '{Name}' ya tiene una subcategoría llamada '{name}'.");

			var subCategory = SubCategory.Create(Id, name, description, icon ?? Icon, colorHex ?? ColorHex, isSystemDefault);
			_subCategories.Add(subCategory);
			MarkUpdated();

			return subCategory;
		}

		public void RemoveSubCategory(Guid subCategoryId)
		{
			var subCategory = _subCategories.SingleOrDefault(x => x.Id == subCategoryId)
				?? throw new DomainException($"La subcategoría '{subCategoryId}' no pertenece a la categoría '{Name}'.");

			subCategory.Delete();
			MarkUpdated();
		}
	}
}
