// ExpressionItemGroup.cs created with MonoDevelop
// User: luis at 12:31 13/05/2008

using System;
using System.Collections.Generic;

namespace MathTextLibrary.Analisys
{
	
	/// <summary>
	/// This class implements a group of <see cref="ExpressionItem"/> intances.
	/// </summary>
	public class ExpressionGroupItem : ExpressionItem
	{
		private List<ExpressionItem> childrenItems;
		
	
		
		/// <summary>
		/// <see cref="ExpressionItemGroup"/>'s constructor.
		/// </summary>
		public ExpressionGroupItem() : base()
		{
			childrenItems = new List<ExpressionItem>();
		}
		
#region Properties

		/// <value>
		/// Contains the group's children items.
		/// </value>
		public List<ExpressionItem> ChildrenItems 
		{
			get 
			{
				return childrenItems;
			}
			set 
			{
				childrenItems = value;
			}
		}

	
		
		/// <value>
		/// Contains the label shown by the item.
		/// </value>
		public override string Label {
			get { return this.ToString(); }
		}
		
		public override string Type {
			get { return "Grupo de elementos"; }
		}


		
#endregion Properties
		
#region Public methods
		
	

		
#endregion Public methods
		
#region Non-public methods
	
		protected override bool MatchSequence(ref TokenSequence sequence, 
		                                      out string output)
		{
			
			output="";
			List<string> res = new List<string>();
			foreach (ExpressionItem item in childrenItems) 
			{
				string auxOutput;
				if(item.Match(ref sequence, out auxOutput))
				{
					res.Add(auxOutput);
				}
				else
				{
					
					return false;
				}	
				
			}
			output = String.Format(FormatString, res.ToArray());
			
			return true;
		}
		
	
		
		protected override string SpecificToString ()
		{
			List<string> resList = new List<string>();
			foreach (ExpressionItem item in childrenItems) 
			{
				resList.Add(item.ToString());
			}
			
			return "{" + String.Join(" ", resList.ToArray()) + "}";
		}


		
#endregion Non-public methods
	}
}
