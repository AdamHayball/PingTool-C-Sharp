# PingTool-C-Sharp
Simple Ping Tool in C#

This is an experimental project to see how complex and how well the Free Research Preview of ChatGPT-3.5 can create a program. The original Idea was to have it write a simple script file that takes two IP addresses and pings them concurrently 200 times with the windows native ping tool in command line.

This version is a continuation of the powershell script attempt utilizing C# .NET framework and Visual Studio Community Edition as suggested by ChatGPT.

In continuing the pursuit of a ChatGPT generated application some things have become clear. OpenAI is intentionally limiting response length and thus overall bandwidth of code revision, "Prompt" clarity is vital in achieving the desired results. When an error is encountered in the build all error can be resolved by ChatGPT if given the error and/or resending all of the modified code for review. ChatGPT can at times be ambiguous about where the code revision snippet is supposed to be. To fix this you simply need to send it the full code and request it send you the full or the entire changed code back instead of just a snippet. 

v1.1.0.0 Change Log
-Does not display Please wait until after results - Fixed
-App says not responding during pings - Fixed
-Add number of pings field with default value of 200 - Done
-Correct GUI spacing for new ping field - Fixed
-Correct "results message" showing before actual results - Fixed
-Added version number to title - Done
-Add slow/monitor mode - Future feature as currently in flood mode for fastest results
