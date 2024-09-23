using System.Text;
using Allure.Net.Commons;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using clio.E2E.Common;
using clio.E2E.CommonSteps;

namespace clio.E2E.Commands;

[AllureSuite("Command Tests")]
[AllureTag("Command Test")]
[AllureNUnit]
[TestFixture(Category = "Command")]
public abstract class BaseCommandTestFixture {

	#region Setup/Teardown

	[SetUp]
	protected virtual async Task Setup(){
		AllureApi.AddEpic(
			string.IsNullOrWhiteSpace(Epic) ? CreateEpicNameFromCommandName(CommandUnderTest) : Epic);

		string storyText = string.IsNullOrWhiteSpace(Story) ? CreateStoryNameFromCommandName(CommandUnderTest) : Story;
		AllureApi.AddStory(storyText);
		AllureApi.AddTags(CommandUnderTest.ToString());
	}

	[TearDown]
	protected virtual async Task TearDown(){ }

	#endregion

	#region Fields: Protected

	protected readonly IEnvironment Environment;

	#endregion

	#region Constructors: Protected

	protected BaseCommandTestFixture(){
		Environment = Common.Environment.GetInstance();
	}

	#endregion

	#region Properties: Private

	/// <summary>
	///  Function to create an epic name from the command name.
	/// </summary>
	private static Func<CommandName, string> CreateEpicNameFromCommandName =>
		commandName => {
			string commandNameStr = char.ToUpper(commandName.ToString()[0]) + commandName.ToString()[1..];
			return $"{commandNameStr} command";
		};

	/// <summary>
	///  Function to create a story name from the command name.
	/// </summary>
	private static Func<CommandName, string> CreateStoryNameFromCommandName =>
		commandName => {
			string commandNameStr = char.ToUpper(commandName.ToString()[0]) + commandName.ToString()[1..];
			return $"{commandNameStr} general tests";
		};

	/// <summary>
	///  Wraps the given text in a paragraph HTML tag.
	/// </summary>
	private static Func<string, string> WrapParagraph =>
		text => $"""
				<p>
					{text}
				</p>
				""";

	/// <summary>
	///  Creates a hyperlink to the README file.
	/// </summary>
	private Func<Uri, string> CreateReadmeHyperlink =>
		_ => $"""
			<b>
				<a href='{ReadmeUrl}' target='_blank'>README</a>
			</b>
			""";

	#endregion

	#region Properties: Protected

	/// <summary>
	///  REQUIRED: Command under test, this is part of the Allure report
	/// </summary>
	protected abstract CommandName CommandUnderTest { get; }

	/// <summary>
	///  REQUIRED: Link to readme URL, this is part of the Allure report
	/// </summary>
	protected abstract Uri ReadmeUrl { get; }

	protected virtual string Epic => string.Empty;

	protected virtual string Story => string.Empty;
	

	#endregion

	#region Methods: Private

	/// <summary>
	///  Appends additional test description to the existing Allure test description.
	/// </summary>
	/// <param name="description">The additional description to append.</param>
	private void AppendTestDescription(string description){
		if (string.IsNullOrWhiteSpace(description)) {
			return;
		}
		StringBuilder sb = new();
		sb.AppendLine("<p>");
		sb.AppendLine($"{description}");
		sb.AppendLine("</p>");
		AllureApi.SetDescriptionHtml(sb.ToString());
	}

	/// <summary>
	///  Appends a warning message to the existing Allure test description.
	/// </summary>
	private string BuildWarningMessage(string warning){
		if (string.IsNullOrWhiteSpace(warning)) {
			return string.Empty;
		}
		StringBuilder sb = new();
		sb
			.AppendLine("<p>")
			.AppendLine($"<b style='color:red;'>WARNING: </b>{warning}")
			.AppendLine("</p>");
		return sb.ToString();
	}

	#endregion

	#region Methods: Protected

	protected void SetTestDescription(string warning = ""){
		AllureLifecycle.Instance.UpdateTestCase(testResult => {
			testResult.descriptionHtml = testResult.description;
			testResult.descriptionHtml
				+= WrapParagraph($"For more information, see the {CreateReadmeHyperlink(ReadmeUrl)}");
			testResult.descriptionHtml += WrapParagraph(BuildWarningMessage(warning));
		});
	}

	#endregion

}