using System.ComponentModel;

namespace Domain.Enums
{
	/// <summary>Qué hacer con el cuadro de amortización después de un abono a capital.</summary>
	public enum ExtraordinaryRecalculationStrategy
	{
		[Description("Se mantiene la cuota mensual y el cuadro se recalcula sobre el nuevo saldo, por lo que el préstamo se salda en menos cuotas.")]
		REDUCE_TERM = 1,

		[Description("Se mantiene la cantidad de cuotas pendientes y se recalcula el monto de la cuota sobre el nuevo saldo, por lo que la mensualidad baja.")]
		REDUCE_INSTALLMENT = 2
	}
}
