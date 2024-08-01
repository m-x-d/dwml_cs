﻿using System.IO;

namespace mxd.Dwml
{
	// Wrapper node, cause we can't store strings and property nodes in the same collection...
	internal class TeXNode
	{
		private readonly string text = string.Empty;
		private readonly MLPropertiesNode pr;
		
		public TeXNode(MLPropertiesNode pr)
		{
			this.pr = pr;
		}

		public TeXNode(string text)
		{
			this.text = text;
		}

		public string GetAttributeValue(string name)
		{
			if (pr == null)
				throw new InvalidDataException($"Can't get attribute '{name}' from string '{text}'!");

			return pr.GetAttributeValue(name);
		}

		public override string ToString() => (pr != null ? pr.ToString() : text);

	}
}