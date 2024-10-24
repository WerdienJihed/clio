using System.ComponentModel.DataAnnotations;
using clioAgent.Handlers;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace clioAgent.Tests;

public class Tests {

	[SetUp]
	public void Setup(){ }

	[TestCase("A","B","C",1)]
	[TestCase("A","B","C",-1)]
	[TestCase("A","B","C",1024)]
	[TestCase("A","B","C",65_536)]
	public void DeployIISDbHandlerArgs_Validates_WithError(string zipFolderPath, string siteFolderName, string siteName, int sitePort){
		var v = new DeploySiteHandlerArgsValidator();
		var record = new DeploySiteHandlerArgs(zipFolderPath, siteFolderName, siteName, sitePort);
		var result = v.Validate("DeploySiteHandlerArgs", record);
		result.Failures.Should().HaveCount(1);
		result.Failures.FirstOrDefault().Should().StartWith("SitePort");
		
	}
	
	

}