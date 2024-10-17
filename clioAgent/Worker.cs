using System.Collections.Concurrent;

namespace clioAgent;

public class Worker {

	private readonly ConcurrentBag<DeployJob> _bag;

	public Worker(ConcurrentBag<DeployJob> bag){
		_bag = bag;
	}
	
	public async Task RunAsync(CancellationToken stoppingToken){
		
		while(!stoppingToken.IsCancellationRequested){

			Console.
				WriteLine($"Worker running at: {DateTimeOffset.Now} - count {_bag.Count}");

			await Task.Delay(1000, stoppingToken);
			
		}
	}

}