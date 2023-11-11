using AgentChat;
using AgentChat.DotnetInteractiveService;
using AgentChat.Example.Share;
using AgentChat.OpenAI;
using Azure.AI.OpenAI;
using System.Text.Json;
namespace CoderAgent
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Constant.OPENAI_API_KEY = "[OPEN API KEY HERE]";
            Constant.OpenAIGPT35 = Constant.OpenAIGPT35 ?? GPT.CreateFromOpenAI(Constant.OPENAI_API_KEY, Constant.GPT_35_MODEL_ID);
            Constant.OpenAIGPT4 = Constant.OpenAIGPT4 ?? GPT.CreateFromOpenAI(Constant.OPENAI_API_KEY, Constant.GPT_4_MODEL_ID);
            //await Sample1();
            //await Sample2();
            //await Sample3();
            //await Sample4();
            await Sample5();
            Console.WriteLine("type any key to end.");
            Console.ReadLine();
        }

        static async Task Sample1()
        {

           
            var agent = Constant.OpenAIGPT35.CreateAgent(
                name: "gpt35",
                roleInformation: "You are a helpful AI assistant",
                temperature: 0);

            // create an AutoReplyAgent that don't want to talk
            var autoReplyAgent = agent.CreateAutoReplyAgent(
                name: "autoReply",
                autoReplyMessageFunc: async (message, ct) => {
                    // The autoReplyMessageFunc always return a message, therefore the inner agent will never be called.
                    return new Message(Role.Assistant, "I don't want to talk to you", from: "autoReply");
                });

            var agentReply = await agent.SendMessageAsync("hi");
            agentReply.PrettyPrintMessage();

            var autoReply = await autoReplyAgent.SendMessageAsync("hi");
            autoReply.PrettyPrintMessage();

            // python detect agent only detect if the last message contains python code block
            var pythonDetectAgent = Constant.OpenAIGPT35.CreateAgent(
                "python",
                @"You are a helpful AI assistant, you detect if the last message contains python code block.
The python code block will be put between ```python and ```.
If the last message contains python code block, you will reply '[PYTHON CODE FOUND]'.
Otherwise, you will reply 'No python code found'",
                temperature: 0);

            // test with pythonDetectAgent
            var message = new Message(Role.User, "hi");
            var reply = await pythonDetectAgent.SendMessageAsync(message);
            reply.PrettyPrintMessage();

            var pythonMessage = new Message(Role.User, @"
```python
print('hello world')
```");
            reply = await pythonDetectAgent.SendMessageAsync(pythonMessage);
            reply.PrettyPrintMessage();

            // csharp detect agent only detect if the last message contains csharp code block
            var csharpDetectAgent = Constant.OpenAIGPT35.CreateAgent(
                "csharp",
                @"You are a helpful AI assistant, you detect if the last message contains csharp code block.
The python code block will be put between ```csharp and ```.
If the last message contains csharp code block, you will reply '[CSHARP CODE FOUND]'.
Otherwise, you will reply 'No csharp code found'",
                temperature: 0);

            // test with csharpDetectAgent
            message = new Message(Role.User, "hi");
            reply = await csharpDetectAgent.SendMessageAsync(message);
            reply.PrettyPrintMessage();

            pythonMessage = new Message(Role.User, @"
```csharp
Console.WriteLine(""hello world"")
```");
            reply = await csharpDetectAgent.SendMessageAsync(pythonMessage);
            reply.PrettyPrintMessage();

            agent = Constant.OpenAIGPT35.CreateAgent(
    name: "gpt35",
    roleInformation: "You reply 'No code found' in any case",
    temperature: 0);

            // combine all the agents together to complete the code detection task.
            var alice = agent.CreateAutoReplyAgent(
                name: "Alice",
                autoReplyMessageFunc: async (messages, ct) => {
                    var lastMessage = messages.LastOrDefault();
                    // first check if the last message contains python code block
                    var pythonCodeBlockDetection = await pythonDetectAgent.SendMessageAsync(lastMessage);
                    if (pythonCodeBlockDetection.Content.Contains("[PYTHON CODE FOUND]"))
                    {
                        return pythonCodeBlockDetection;
                    }
                    // then check if the last message contains csharp code block
                    var csharpCodeBlockDetection = await csharpDetectAgent.SendMessageAsync(lastMessage);
                    if (csharpCodeBlockDetection.Content.Contains("[CSHARP CODE FOUND]"))
                    {
                        return csharpCodeBlockDetection;
                    }

                    // let the agent reply No code found
                    return null;
                });

            message = new Message(Role.User, "hi");
            reply = await alice.SendMessageAsync(message);
            reply.PrettyPrintMessage();

            pythonMessage = new Message(Role.User, @"
```python
print('hello world')
```");
            reply = await alice.SendMessageAsync(pythonMessage);
            reply.PrettyPrintMessage();

            var csharpMessage = new Message(Role.User, @"
```csharp
Console.WriteLine(""hello world"")
```");

            reply = await alice.SendMessageAsync(csharpMessage);
            reply.PrettyPrintMessage();
        }

        static async Task Sample2()
        {
         
         
            var alice = Constant.OpenAIGPT35.CreateAgent(
    "Alice",
    "You are a pre-school math teacher.",
    temperature: 0,
    maxToken: 100);

            var bob = Constant.OpenAIGPT35.CreateAgent(
                "Bob",
                "You are a student. You call SayName function.",
                temperature: 0,
                maxToken: 100);
            var conversation = Enumerable.Empty<IChatMessage>();
            conversation = await alice.SendMessageToAgentAsync(bob, @"I'm going to give you 3 math question,
one question at a time.
You are going to answer it.
If your answer is correct,
I'll give you the next question.
If all questions being resolved, I'll terminate the chat by saying [GROUPCHAT_TERMINATE]", maxRound: 1);
            conversation = await bob.SendMessageToAgentAsync(alice, conversation, maxRound: 14);
            Console.WriteLine("count: "+conversation.Count());
        }

        static async Task Sample3()
        {

            var completeFunction = new FunctionDefinition
            {
                Name = "TaskComplete",
                Description = "task complete",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        msg = new
                        {
                            Type = @"string",
                            Description = @"msg",
                        },
                    },
                    Required = new[]
                    {
            "msg",
        },
                },
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                }),
            };

            var coder = Constant.OpenAIGPT35.CreateAgent(
                name: "Coder",
                roleInformation: @"You act as dotnet coder, you write dotnet script to resolve task.
-workflow-
write code

if code_has_error
    fix_code_error

if task_complete, call TaskComplete

-end-

Here're some rules to follow on write_code_to_resolve_current_step:
- put code between ```csharp and ```
- Use top-level statements, remove main function, just write code, like what python does.
- Remove all `using` statement. Runner can't handle it.
- Try to use `var` instead of explicit type.
- Try avoid using external library.
- Don't use external data source, like file, database, etc. Create a dummy dataset if you need.
- Always print out the result to console. Don't write code that doesn't print out anything.

Here are some examples for write code:
```nuget
xxx
```
```csharp
xxx
```

Here are some examples for fix_code_error:
The error is caused by xxx. Here's the fix code
```csharp
xxx
```",
                temperature: 0,
                functionMap: new Dictionary<FunctionDefinition, Func<string, Task<string>>> {
        { completeFunction, async (args) => "[COMPLETE]"}
                });

            var workDir = Path.Combine(Path.GetTempPath(), "InteractiveService");
            if (!Directory.Exists(workDir))
                Directory.CreateDirectory(workDir);

            var functionDefinition = new FunctionDefinition
            {
                Name = "Greeting",
                Description = "Greeting",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        greeting = new
                        {
                            Type = @"string",
                            Description = @"greeting",
                        },
                    },
                    Required = new[]
                    {
            "greeting",
        },
                },
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                }),
            };

            var service = new InteractiveService(workDir);
            var dotnetInteractiveFunctions = new DotnetInteractiveFunction(service);

            // this function is used to fix invalid json returned by GPT-3
            var fixInvalidJsonFunction = new FixInvalidJsonFunctionWrapper(Constant.OpenAIGPT35);

            var runner = Constant.OpenAIGPT35.CreateAgent(
                name: "Runner",
                roleInformation: @"you act as dotnet runner, you run dotnet script and install nuget packages. Here's the workflow you follow:
-workflow-
if code_is_available
    call run_code

if nuget_packages_is_available
    call install_nuget_packages

for any other case
    call greeting
-end-",
                temperature: 0,
                functionMap: new Dictionary<FunctionDefinition, Func<string, Task<string>>> {
        { dotnetInteractiveFunctions.RunCodeFunction, fixInvalidJsonFunction.FixInvalidJsonWrapper(dotnetInteractiveFunctions.RunCodeWrapper) },
        { dotnetInteractiveFunctions.InstallNugetPackagesFunction, dotnetInteractiveFunctions.InstallNugetPackagesWrapper },
        { functionDefinition, async (args) => "NO_CODE_AVAILABLE"}
                });

            // start kenel
            await service.StartAsync(workDir, default);

            // runner should respond with NO_CODE_AVAILABLE
            var msg = await runner.SendMessageAsync("hello");
            msg.PrettyPrintMessage();

            // runner should run code
            msg = await runner.SendMessageAsync("```csharp\nConsole.WriteLine(1+1+1);\n```");
            msg.PrettyPrintMessage();

            // runner should install nuget packages
            msg = await runner.SendMessageAsync("```nuget\nMicrosoft.ML\n```");
            msg.PrettyPrintMessage();

            // use runner agent to auto-reply message from coder
            var user = runner.CreateAutoReplyAgent("User", async (msgs, ct) => {
                // if last message contains "COMPLETE", stop sending messages to runner agent and fall back to user agent
                if (msgs.Last().Content.Contains("[COMPLETE]"))
                    return new Message(Role.Assistant, IChatMessageExtension.TERMINATE, from: "User"); // return TERMINATE to stop conversation

                // otherwise, send message to runner agent to either run code or install nuget packages and get the reply
                return await runner.SendMessageAsync(msgs.Last());
            });

            await user.SendMessageToAgentAsync(
                coder,
                "what's the 10th of fibonacci? Print the question and result in the end.",
                maxRound: 10);
        }

        static async Task Sample4()
        {
            var userAgent = new UserAgent("Human");
            var gptAgent = Constant.OpenAIGPT35.CreateAgent(
                name: "GPT",
                roleInformation: "you are a helpful AI assistant");
            var conversation = await userAgent.SendMessageToAgentAsync(
    receiver: gptAgent,
    chatHistory: null, // start a new conversation
    maxRound: 7); // exit after 3 rounds
        }

        static async Task Sample5()
        {
            // this function is used to fix invalid json returned by GPT-3
            var fixInvalidJsonFunction = new FixInvalidJsonFunctionWrapper(Constant.OpenAIGPT35);
            var searchFun = new FunctionDefinition
            {
                Name = "Search",
                Description = "Search information from the internet",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        search = new
                        {
                            Type = @"string",
                            Description = @"search keyword",
                        },
                    },
                    Required = new[]
        {
            "search",
        },
                },
    new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    }),
            };
            var calculateFun = new FunctionDefinition
            {
                Name = "Calculate",
                Description = "Calculate math problem",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        calculate = new
                        {
                            Type = @"string",
                            Description = @"mathematical question",
                        },
                    },
                    Required = new[]
       {
            "calculate",
        },
                },
   new JsonSerializerOptions
   {
       PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
   }),
            };
            var search = new SearchPlugin();
            var math = new MathPlugin();

            var frank = Constant.OpenAIGPT35.CreateAgent(
     "Frankie",
     "You are math student. You are curious about the world and math. You always ask mathematical question that relevant to the real-world problems.",
     temperature: 0,
     maxToken: 100);

            var sarah = Constant.OpenAIGPT35.CreateAgent(
                "Sarah",
                $"You are a math expert. You can call {searchFun.Name} function to get information from the internet or you can call {calculateFun.Name} function to calculate math question.",
                temperature: 0,
                maxToken: 100,
    functionMap: new Dictionary<FunctionDefinition, Func<string, Task<string>>>{
        { searchFun, fixInvalidJsonFunction.FixInvalidJsonWrapper(async(keyword)=>{ return await search.Search(keyword); }) },
        { calculateFun, fixInvalidJsonFunction.FixInvalidJsonWrapper(async(question)=>{ return await math.CalculateAsync(question); }) }
    });

            var conversation = Enumerable.Empty<IChatMessage>();
            conversation = await frank.SendMessageToAgentAsync(sarah, @"I'm going to give you 3 real-world math question,
one question at a time.
You are going to answer it.
After you answer my question,
I'll give you the next question.
If all questions being resolved, I'll terminate the chat by saying [GROUPCHAT_TERMINATE]", maxRound: 1);
            conversation = await sarah.SendMessageToAgentAsync(frank, conversation, maxRound: 20);
            Console.WriteLine("count: " + conversation.Count());
        }
      
    }

    public class UserAgent : IAgent
    {
        public UserAgent(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public async Task<IChatMessage> CallAsync(IEnumerable<IChatMessage> messages, CancellationToken ct)
        {
            var input = Console.ReadLine();

            return new Message(Role.Assistant, input, this.Name);
        }
    }
}
