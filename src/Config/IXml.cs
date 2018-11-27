using System.Xml.Linq;

namespace GridEx.HistoryServerClient.Config
{
	interface IXml
	{
		void Load(XElement xElement);

		XElement GetAsXElement();
	}
}
