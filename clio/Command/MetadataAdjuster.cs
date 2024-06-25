using System;
using System.Linq;
using System.Xml.Linq;
using Terrasoft.Common;

namespace Clio.Command;

/// <summary>
///     The MetadataAdjuster class provides functionality to adjust XML metadata.
/// </summary>
public static class MetadataAdjuster
{

	#region Fields: Private

	/// <summary>
	///     A function that counts the number of 'Property' elements with 'Type' attribute value 'Edm.Stream' in the XML.
	/// </summary>
	private static readonly Func<XElement, int> Count = root => root
		.Elements().First().Elements().First()
		.Elements()
		.Where(e => e.Name.LocalName == "EntityType").Elements()
		.Count(se => se.Name.LocalName == "Property" && se.Attribute("Type")?.Value == "Edm.Stream");

	#endregion

	#region Methods: Public

	/// <summary>
	///     Adjusts the XML content by removing 'Property' elements with 'Type' attribute value 'Edm.Stream'.
	/// </summary>
	/// <param name="xmlContent">The XML content to adjust.</param>
	/// <returns>The adjusted XML content as a string.</returns>
	public static string Adjust(string xmlContent){
		XDocument xDoc = XDocument.Parse(xmlContent);
		XElement root = xDoc.Root;
		if (root is null) {
			return xmlContent;
		}
		do {
			root!.Elements().First().Elements().First().Elements()
				.Where(e => e.Name.LocalName == "EntityType")
				.Elements()
				.Where(se => se.Name.LocalName == "Property" && se.Attribute("Type")?.Value == "Edm.Stream")
				.ForEach(s => s.Remove());
		} while (Count(root) > 0);
		return xDoc.ToString();
	}

	#endregion

}