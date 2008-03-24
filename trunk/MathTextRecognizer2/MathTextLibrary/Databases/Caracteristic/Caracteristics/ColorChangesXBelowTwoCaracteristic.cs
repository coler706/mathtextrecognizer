using System;

using MathTextLibrary.Databases.Caracteristic.Caracteristics.Helpers;

namespace MathTextLibrary.Databases.Caracteristic.Caracteristics
{
	/// <summary>
	/// Esta caracteristica determina si el numero de cambios blanco-negro
	/// en el eje X de la imagen es menor estricto que 2.
	/// </summary>
	/// <seealso cref="MathTextLibrary.Caracteristics.Helpers.CountColorChangesHelper"/>
	public class ColorChangesXBelowTwoCaracteristic:IBinaryCaracteristic
	{
		public ColorChangesXBelowTwoCaracteristic()
		{
			priority=160;
		}

		public override bool Apply(MathTextBitmap image)
		{
			if(CountColorChangesHelper.NumColorChangesRow(image,
			                                              image.ProcessedImage.GetLength(0)/2) < 2) 
			{
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}