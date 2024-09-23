namespace clio.E2E;

[SetUpFixture]
public class OneTime
{

	[OneTimeSetUp]
	public void OneTimeSetUp(){}

	[OneTimeTearDown]
	public void OneTimeTearDown(){}

}