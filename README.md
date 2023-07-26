# PingTool-C-Sharp
Simple Ping Tool in C#

This is an experimental project to see how complex and how well the Free Research Preview of ChatGPT-3.5 can create a program. The original Idea was to have it write a simple script file that takes two IP addresses and pings them concurrently 200 times with the windows native ping tool in command line.

This version is a continuation of the powershell script attempt utilizing C# .NET framework and Visual Studio Community Edition as suggested by ChatGPT.

In continuing the pursuit of a ChatGPT generated application some things have become clear. OpenAI is intentionally limiting response length and thus overall bandwidth of code revision. If given a large scope project ChatGPT will flat out refuse, basically saying that's alot of work and I can't do that. If given small tasks individually it usaully has a high rate of success. I blame the bandwidth limitation for this but also liken it to if I told a single programmer they have 30 seconds to complete an entire project. 
"Prompt" clarity is vital in achieving the desired results. When an error is encountered in the build all error can be resolved by ChatGPT if given the error and/or resending all of the modified code for review. ChatGPT can at times be ambiguous about where the code revision snippet is supposed to be. To fix this you simply need to send it the full code and request it send you the full updated code back instead of just a snippet. So far ChatGPT has performed as well or better than if a program was being developed by a human outsourcing solution like Fiverr.

v1.1.0.0 Change Log<br></br>
-Does not display Please wait until after results - Fixed<br>
-App says not responding during pings - Fixed<br>
-Add number of pings field with default value of 200 - Done<br>
-Correct GUI spacing for new ping field - Fixed<br>
-Correct "results message" showing before actual results - Fixed<br>
-Added version number to title - Done<br>
-Add slow/monitor mode - Future feature as currently in flood mode for fastest results<br>
