using System.Xml;

namespace mxd.Dwml.Tools
{
	internal static class XMLTools
	{
		public static XmlNode GetChildByLocalName(this XmlNode n, string local_name)
		{
			foreach (XmlNode cn in n.ChildNodes)
				if (cn.LocalName == local_name)
					return cn;

			return null;
		}

		public static string GetAttributeValue(this XmlNode n, string local_name)
		{
			if (n.Attributes == null)
				return null;

			foreach (XmlAttribute attr in n.Attributes)
				if (attr.LocalName == local_name)
					return attr.Value;

			return null; //mxd. We need to differentiate between <m /> and <m val=""/> cases!
		}

	}
}