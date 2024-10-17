using System.Collections.Concurrent;
using System.Text.Json;

namespace clioAgent;

public class DeployEndpointHandler(ConcurrentBag<DeployJob> bag) {
	public IResult Execute(Dictionary<string, object> commandObj) {
		#region Validatio
		commandObj.TryGetValue("Product", out object productObj);
		if(productObj == null || ((JsonElement)productObj).ValueKind != JsonValueKind.String) {
			return Results.BadRequest("Product is required");
		}
		var product = ((JsonElement)productObj).GetString();
		
		#endregion
		
		var job = new DeployJob {
			Product = product
		};
		bag.Add(job);
		// put in queue
		//return id
		#region Orchestration
		
		
		#endregion
		
		
		
		//V:\8.2.0\8.2.0.4120\BankSales_BankCustomerJourney_Lending_MarketingNet6_Softkey_ENU
		
		return Results.Ok(job.Id);
	}

}