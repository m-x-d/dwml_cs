﻿using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace mxd.Dwml
{
	// Base class for handling tags and methods. Named 'Tag2Method' in Python version.
	internal abstract partial class MLNodeBase
	{
		protected struct NodeInfo
		{
			public string Tag;
			public TeXNode Node;
		}
		
		protected abstract TeXNode ProcessTag(string tag, XmlNode elm);

		protected TeXNode CallMethod(XmlNode elm, string tag = null)
		{
			if (tag == null)
				tag = elm.LocalName;

			return ProcessTag(tag, elm);
		}

		protected List<NodeInfo> ProcessChildrenList(XmlNode elm, HashSet<string> include = null)
		{
			var result = new List<NodeInfo>();
			
			foreach (XmlNode e in elm.ChildNodes)
			{
				if (!e.NamespaceURI.Contains(OMML_NS))
					continue;

				var tag = e.LocalName;
				if (include != null && !include.Contains(tag))
					continue;

				var tag_elm = CallMethod(e, tag);
				if (tag_elm == null)
				{
					tag_elm = ProcessUnknown(e, tag);
					if (tag_elm == null)
						continue;
				}

				result.Add(new NodeInfo { Tag = tag, Node = tag_elm });
			}

			return result;
		}

		protected Dictionary<string, TeXNode> ProcessChildrenDict(XmlNode elm, HashSet<string> include = null)
		{
			var latexChars = new Dictionary<string, TeXNode>();
			
			foreach (var info in ProcessChildrenList(elm, include))
				latexChars[info.Tag] = info.Node;

			return latexChars;
		}

		protected string ProcessChildren(XmlNode elm, HashSet<string> include = null) => string.Join("", ProcessChildrenList(elm, include).Select(t => t.Node));

		protected virtual TeXNode ProcessUnknown(XmlNode elm, string tag) => null;

	}

}