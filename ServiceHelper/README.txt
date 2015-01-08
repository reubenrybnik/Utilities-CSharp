This project is designed to make installing and locally debugging a .NET-based looping Windows service easier in .NET 3.5 and above.

Features include:
 - Command-line install and uninstall without InstallUtil.exe
 - Default install options located in app.config
 - Command-line overrides for all install options
 - An optional interactive console-based install experience with defaults provided
 - A watchdog on the service loop thread that fires an event when a service loop takes longer than expected
 - Options to not start as a service but instead run as a console application

Other useful/interesting pieces of code include:
 - A set of methods for securely reading passwords from the console (Utils.cs)
 - A design pattern for impersonating a Windows user given a user name and password (WindowsService.cs)
 - A set of methods that can be called at or near safe stopping points to allow the service to be gracefully stopped in the middle of a loop (WindowsServiceImplementation.cs)
 - A "reusable" thread implementation that allows for thread aborts if necessary (ReusableThread.cs)
 - Some cleverness around exception handling/not handling to help improve the context of debugging "unhandled" exceptions (the program will stop execution at the exception instead of jumping to a catch block without needing to enable breaking on thrown exceptions) (ReusableThread.cs, WindowsService.cs)
 - A way to override Console.In for testing that requires console input (WindowsServiceTests.cs)

While I can't claim credit for every idea manifested in this project, the code was all written by me personally from scratch and most of the features originated solely in my head. I embarked upon this project to provide a reusable framework which lessens a few of the pain points that I encountered when working on infrastructure systems that used Windows services.

!!IMPORTANT!!
This project is still being implemented. Expect significant bugs, missing functionality, and missing comments in the current version.