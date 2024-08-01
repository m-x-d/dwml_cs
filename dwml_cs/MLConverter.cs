using System.Xml;

namespace mxd.Dwml
{
	public static class MLConverter
	{
		public static string Convert(XmlNode oMath) => new MLMathNode(oMath).Text;
	}
}