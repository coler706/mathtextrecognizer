// ExpressionSubexpressionItem.cs created with MonoDevelop
// User: luis at 14:46 13/05/2008

using System;
using System.Collections.Generic;

namespace MathTextLibrary.Analisys
{
	
	/// <summary>
	/// This class implements an expression item that represents the use
	/// of another expression in the parent expression.
	/// </summary>
	public class ExpressionRuleCallItem : ExpressionItem
	{
		string expressionName;
		
		
		/// <summary>
		/// <see cref="ExpressionSubexpressionItem"/>'s constructor.
		/// </summary>
		public ExpressionRuleCallItem() : base()
		{
		}
		


#region Properties
		
		/// <value>
		/// Contains the name of the subexpression to be used.
		/// </value>
		public string RuleName 
		{
			get 
			{
				return expressionName;
			}
			set 
			{
				expressionName = value;
			}
		}
		
		/// <value>
		/// Contains the label shown by the item.
		/// </value>
		public override string Label {
			get { return ToString(); }
		}
		
		public override string Type {
			get { return "Llamada a regla"; }
		}


		
#endregion Properties

#region Public methods

		
#endregion Public methods
		
#region Non-public methods
		
		protected override bool MatchSequence (ref TokenSequence sequence, out string output)
		{
			// The actual matching is done by the rule.
			SyntacticalRule ruleCalled = 
				SyntacticalRulesLibrary.Instance[expressionName];
			
			bool res =  ruleCalled.Match(ref sequence, out output);
			if(res)
			{
				output = String.Format(formatString, output);
			}
			
			return res;
		}

		protected override string SpecificToString ()
		{
			return this.expressionName;
		}


		
#endregion Non-public methods
	}
}
