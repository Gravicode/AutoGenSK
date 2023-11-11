using Google.Apis.Logging;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgentChat.Example.Share;

namespace CoderAgent
{
    public class SearchPlugin
    {
        IKernel kernel;
        public SearchPlugin()
        {
            kernel = new KernelBuilder()
      
       .WithOpenAIChatCompletionService(
           modelId: Constant.GPT_35_MODEL_ID,
           apiKey: Constant.OPENAI_API_KEY)
       .Build();

            // Load Bing plugin
            string bingApiKey = Constant.BingKey;
            if (bingApiKey == null)
            {
                Console.WriteLine("Bing credentials not found. Skipping example.");
            }
            else
            {
                var bingConnector = new BingConnector(bingApiKey);
                var bing = new WebSearchEnginePlugin(bingConnector);
                kernel.ImportFunctions(bing, "bing");              
            }
        }

        public async Task<string> Search(string question)
        {
            var function = kernel.Functions.GetFunction("bing", "search");
            var result = await kernel.RunAsync(question, function);

            Console.WriteLine(question);
            Console.WriteLine($"----BING----");
            return result.GetValue<string>().Trim();
        }
    }
}
