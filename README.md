# PingTool-C-Sharp
## Simple Ping Tool in C#

This project serves as an experiment, designed to gauge the complexity and proficiency of ChatGPT-3.5's Free Research Preview in creating programs. Initially, the idea was to have the model generate a basic script that would perform concurrent pinging of two IP addresses, executing this task 200 times using the native Windows ping tool in the command line.

The current version represents a continuation of the effort, building upon the initial attempt using a powershell script. Now, the project is evolving to leverage the C# .NET framework and Visual Studio Community Edition, guided by the insights from ChatGPT.

Throughout this pursuit of a ChatGPT-generated application, certain insights have emerged. OpenAI deliberately enforces limits on response length, affecting the overall capacity for code revision. When presented with a substantial project, ChatGPT may decline, essentially communicating that the task is extensive and beyond its capability. However, when provided with smaller, individual tasks, its success rate tends to be higher. This limitation can be likened to expecting a single programmer to complete an entire project within a mere 30 seconds.

The clarity of the initial "prompt" significantly influences the achievement of desired outcomes. In cases where build errors arise, ChatGPT can resolve these issues when provided with the error details and the modified code for review. Occasionally, ChatGPT may be ambiguous regarding the appropriate location for inserting a code revision snippet. To address this, it's recommended to share the entire code and request a complete updated version in response, rather than just a snippet. So far, ChatGPT has demonstrated performance on par with, or even surpassing, human outsourcing solutions like Fiverr when it comes to program development.

In some instances, human intervention was necessary to help ChatGPT identify conflicts within the code and their resolutions. In the current stage of development, ChatGPT plays more of a supportive role than that of the primary coder.

Revisions can prove challenging, as ChatGPT may disregard existing solutions in longer projects and introduce conflicting code, leading to redundancy issues that require human intervention to correct.

When attempting to introduce significant functional changes, ChatGPT may struggle to understand why it can't seamlessly add new methods and achieve the desired functionality.

While endeavoring to incorporate IPV6 support, it became evident that a comprehensive code overhaul, along with the integration of new libraries and extensive debugging, would be necessary.

The outcomes generated by ChatGPT can appear inconsistent, as its style and approach may vary frequently. Utilizing a new chat session can yield either positive or negative results, depending on the logical state of the language model at that moment.

Barring any major bugs or feature changes, the development of this program is nearing completion, as the program is now fully functional and in regular use.

As an additional point, I must acknowledge that I haven't yet attained mastery in C#; however, I've managed to reach this advanced stage of development by harnessing a powerful large language model to generate code based on my textual inputs. The journey involved numerous iterations and revisions to arrive at a release candidate. It's noteworthy that achieving this progress is remarkable, especially considering the nascent stage of development of large language models at this juncture. Deliberately opting for C# despite my limited familiarity with the programming language added an extra layer of challenge and reduced the potential for manual code influence.

By the way, I had ChatGPT revise the README – quite amusing! XD

## Version 1.3.3
- Fixed an issue where pressing the "Reset" button during ongoing pinging would not properly cancel tasks and clear results. Now, the "Reset" button effectively cancels ongoing pinging tasks, clears displayed results, and resets the chart display.

This release marks the introduction of the first Release Candidate (RC). In this RC, the focus has been on enhancing the user experience and ensuring smooth functionality. To make the application as portable and lightweight as possible, a compiled binary of the application, along with its associated dependencies, will be provided in a compressed file format. This approach enables users to conveniently utilize the tool across various platforms without the need for extensive installation procedures.

## Version 1.3.2
- Added GitHub Link
- Enhanced UI Spacing
- Removed Window Resizing
- Fixed bug preventing single IP results when pinging the gateway only

**Known Issue:** Pressing reset while pinging does not cancel and clear results; it only resets the chart.

## Version 1.3.1
- Fixed Pressing the cancel button after reset displays previous results

## Version 1.3.0
- Added graph IP legend
- Increased thickness of graph result lines
- Fixed issue of app crashing when "Run Ping" button is pressed twice
- Introduced set packet size option
- Streamlined .csproj file post .NET upgrade
- Titlebar version is no longer static
- Improved UI spacing

## Version 1.2.0
- Added slow/monitor mode
- Added cancel button
- Added charting graph
- Introduced output logging
- Fixed IPs not pinging concurrently
### New System Requirements
- Windows 7 or higher
- Dual-core or better CPU
- .NET upgrade from 4.7.2 to 6.0

## Version 1.1.1
- Added icon generated by Bing/Create Powered by Dall-E using prompt "icon for ping tool"
- Included both single and multi-image .ICO of AI-generated icon files in resources
- Changed Assembly name to remove ".NET" from compiled .EXE

## Version 1.1.0
- Fixed: Does not display "Please wait" until after results
- Fixed: App not responding during pings
- Added number of pings field with a default value of 200
- Fixed GUI spacing for the new ping field
- Fixed "results message" showing before actual results
- Added version number to the title
- Currently in flood mode for fastest results and testing
