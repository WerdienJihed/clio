using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Clio.Command;
using FluentAssertions;
using NUnit.Framework;

namespace Clio.Tests;

[TestFixture]
public class MetadataAdjusterTests
{

	#region Fields: Private

	private readonly Func<XElement, int> _count = root => root
		.Elements().First().Elements().First()
		.Elements()
		.Where(e => e.Name.LocalName == "EntityType").Elements()
		.Count(se => se.Name.LocalName == "Property" && se.Attribute("Type")?.Value == "Edm.Stream");

	#endregion

	#region Methods: Public

	[TestCase("samplefiles/metadata-formated.xml")]
	[TestCase("samplefiles/metadata-raw.xml")]
	public void TestOne(string fileName){
		//Arrange
		string xmlContent = File.ReadAllText(fileName);

		//Act
		string actual = MetadataAdjuster.Adjust(xmlContent);

		//Assert
		XDocument xDoc = XDocument.Parse(actual);
		XElement root = xDoc.Root;
		root.Should().NotBeNull("reading from existing xml file");
		_count(root).Should().Be(0);
	}

	#endregion

}