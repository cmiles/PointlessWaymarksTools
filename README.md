# Pointless Waymarks Tools

This repository pulls together reusable tools with a focus on WPF Desktop Applications. These tools were first built for the [Pointless Waymarks Project](https://github.com/cmiles/PointlessWaymarksProject).

**WPFCommon**

This library has several controls and ideas that I like for building small to medium WPF applications:
  - StatusControl - For Desktop Apps I want to have an easy, generic, way to run tasks in the background, block the UI, show cancellation buttons, stream text progress information, display messages and show in-app toast. The StatusControl UserControl can be added to a control/page/window and bound to a StatusControlContext to handle this scenario. This is a compromise - for the most beautiful UI interactions you probably don't want to block/cover your UI and this single control won't help you produce an infinite variety of perfect intricate UI interactions, but what it does provide is an 80-90% good-enough solution that can be applied quickly, easily and consistently to run tasks in the background, show progress and show basic UI notifications.
  - ThreadSwitcher - based on Raymond Chen's [C++/WinRT envy: Bringing thread switching tasks to C# (WPF and WinForms edition)](https://devblogs.microsoft.com/oldnewthing/20190329-00/?p=102373) -  this adds the ability to write 'await ThreadSwitcher.ResumeForegroundAsync();' and 'await ThreadSwitcher.ResumeBackgroundAsync();' to get onto/off of the UI Thread. While the MVVM pattern and Bindings can help you write code that doesn't have to be aware of which thread you are on I have found it impractical to produce a user friendly/focused WPF GUI code without needing to be able to switch to and away from the main UI thread. I have found using the ThreadSwitcer a pleasant and productive pattern in GUI code.
  - WindowScreenShot - It turns out that it is quite nice to get screen shots of your app's window but also quite tricky to get this to happen correctly in all situations... The ScreenShot control in this project is based on code from [Capturing screenshots using C# and p/invoke](https://www.cyotek.com/blog/capturing-screenshots-using-csharp-and-p-invoke).

**ExcelInteropExtensions**

A very useful approach to getting user data from Excel is reading directly from the Excel application. This can be messy (give the user any message/help you want but you will still spend time explaining that reading from Excel isn't working because they have a cell open for editing...) - but especially for power users it can avoid confusion over what data is saved vs on screen and can reduce repetitive steps like saving/picking/dragging/etc. This is not my first .NET journey into Excel interop code and if you are exploring this approach I encourage you to look at and/or reuse this code. It is very heavily based on [Automate multiple Excel instances - CodeProject](https://www.codeproject.com/articles/1157395/automate-multiple-excel-instances) by James Faix. Faix's code, and other code in this vein, all ultimately link back to [Andrew Whitechapel : Getting the Application Object in a Shimmed Automation Add-in (archived link)](https://web.archive.org/web/20130518152056/http://blogs.officezealot.com/whitechapel/archive/2005/04/10/4514.aspx) (the source I used in the mid-2000s when I first started to do .NET Excel Interop!).

## Tools and Libraries

I am incredibly grateful to the all the people and projects that make it possible to rapidly build useful, open, low/no-cost software. Below is a mostly-up-to-date-and-largely-comprehensive list of tools/packages/libraries/etc. that are used to build the this project:

**Tools:**
 - [Visual Studio IDE](https://visualstudio.microsoft.com/), [.NET Core (Linux, macOS, and Windows)](https://dotnet.microsoft.com/download/dotnet-core)
 - [ReSharper: The Visual Studio Extension for .NET Developers by JetBrains](https://www.jetbrains.com/resharper/)
 - [GitHub Copilot · Your AI pair programmer · GitHub](https://github.com/features/copilot)
 - [Metalama: A Framework for Clean & Concise Code in C#](https://www.postsharp.net/metalama)
 - [Xavalon/XamlStyler: Visual Studio extension to help format your XAML source code](https://github.com/Xavalon/XamlStyler)
 - [PowerShell](https://github.com/PowerShell/PowerShell)
 - [tareqimbasher/NetPad: A cross-platform C# editor and playground.](https://github.com/tareqimbasher/NetPad)
 - [dotnet-script/dotnet-script: Run C# scripts from the .NET CLI.](https://github.com/dotnet-script/dotnet-script)
 - [AutoHotkey](https://www.autohotkey.com/)
 - [Beyond Compare](https://www.scootersoftware.com/)
 - [Compact-Log-Format-Viewer: A cross platform tool to read & query JSON aka CLEF log files created by Serilog](https://github.com/warrenbuckley/Compact-Log-Format-Viewer)
 - [DB Browser for SQLite](https://sqlitebrowser.org/)
 - [ExifTool by Phil Harvey](https://exiftool.org/) and [Oliver Betz | ExifTool Windows installer and portable package](https://oliverbetz.de/pages/Artikel/ExifTool-for-Windows)
 - [Fork - a fast and friendly git client for Mac and Windows](https://git-fork.com/)
 - [grepWin: A powerful and fast search tool using regular expressions](https://github.com/stefankueng/grepWin)
 - [Inno Setup](https://jrsoftware.org/isinfo.php)
 - [Greenfish Icon Editor Pro](http://greenfishsoftware.org/gfie.php)
 - [Notepad++](https://notepad-plus-plus.org/)
 - [RegexBuddy: Learn, Create, Understand, Test, Use and Save Regular Expression](https://www.regexbuddy.com/)

**Core Technologies:**
 - [dotnet/core: Home repository for .NET Core](https://github.com/dotnet/core)
 - [dotnet/wpf: WPF is a .NET Core UI framework for building Windows desktop applications.](https://github.com/dotnet/wpf). MIT License.

**Images:**
 - [drewnoakes/metadata-extractor-dotnet: Extracts Exif, IPTC, XMP, ICC and other metadata from image, video and audio files](https://github.com/drewnoakes/metadata-extractor-dotnet) - Used to read the metadata in Photographs - there are a number of ways to get this data but it is amazing to have a single go to library to work with that already handles a number of the (many, many, many...) issues. Apache License, Version 2.0.
 - [saucecontrol/PhotoSauce: MagicScaler high-performance, high-quality image processing pipeline for .NET](https://github.com/saucecontrol/PhotoSauce) - Fast high quality Image Resizing. If you personally care about image quality image resizing becomes a complicated topic very quickly and I think the results from this library are excellent. Ms-Pl.
 - [Pictogrammers - Open-source iconography for designers and developers](https://pictogrammers.com/)
 - [ElinamLLC/SharpVectors: SharpVectors - SVG# Reloaded: SVG DOM and Rendering in C# for the .Net.](https://github.com/ElinamLLC/SharpVectors) - support for using SVG in WPF applications including Markup Extensions and Controls. BSD 3-Clause License.

**Excel:**
 - [Automate multiple Excel instances - CodeProject](https://www.codeproject.com/Articles/1157395/Automate-multiple-Excel-instances) - James Faix's excellent code for getting references to running Excel instances was pulled into this project, converted for style and upgraded to .NET Core. The basic approach in this article comes from a 2005 post by Andrew Whitechapel titled 'Getting the Application Object in a Shimmed Automation Add-in' - http://blogs.officezealot.com/whitechapel/archive/2005/04/10/4514.aspx. The post by Andrew Whitechapel is now only available thru the Wayback Machine - [Andrew Whitechapel : Getting the Application Object in a Shimmed Automation Add-in](https://web.archive.org/web/20130518152056/http://blogs.officezealot.com/whitechapel/archive/2005/04/10/4514.aspx).
 - [ClosedXML](https://github.com/ClosedXML/ClosedXML) - A great way to read and write Excel Files - I have years of experience with this library and it is both excellent and well maintained. MIT License.

**Maps/GIS:**
 - [sealbro/dotnet.garmin.connect: Unofficial garmin connect client](https://github.com/sealbro/dotnet.garmin.connect). MIT License.
 - [GeoNames Web Service](https://www.geonames.org/export/web-services.html) - [GeoNames](https://www.geonames.org/) is a great resource for place name lookup and offers both offline information downloads and a Web API which can be used for limited use for free.
 - [mattjohnsonpint/GeoTimeZone: Provides an IANA time zone identifier from latitude and longitude coordinates.](https://github.com/mattjohnsonpint/GeoTimeZone) - Great in combination with spatial data for determining times (offline!). MIT License.
 - [Leaflet - a JavaScript library for interactive maps](https://leafletjs.com/) - [On GitHub](https://github.com/Leaflet/Leaflet). BSD-2-Clause License.
   - [elmarquis/Leaflet.GestureHandling: Brings the basic functionality of Google Maps Gesture Handling into Leaflet. Prevents users from getting trapped on the map when scrolling a long page.](https://github.com/elmarquis/Leaflet.GestureHandling). MIT License.
   - [domoritz/leaflet-locatecontrol: A leaflet control to geolocate the user.](https://github.com/domoritz/leaflet-locatecontrol). MIT License.
   - [GitHub - koddas/Leaflet.awesome-svg-markers: Colorful, iconic & retina-proof SVG markers for Leaflet, based on Leaflet.awesome-markers](https://github.com/koddas/Leaflet.awesome-svg-markers/tree/master). MIT License.
 - [NetTopologySuite/NetTopologySuite: A .NET GIS solution](https://github.com/NetTopologySuite/NetTopologySuite). [NetTopologySuite License](https://github.com/NetTopologySuite/NetTopologySuite/blob/develop/License.md) - Nuget Package listed as BSD-3-Clause.
 - [NetTopologySuite/NetTopologySuite.IO.GPX: GPX I/O for NetTopologySuite](https://github.com/NetTopologySuite/NetTopologySuite.IO.GPX). BSD-3-Clause License.
 - [NetTopologySuite/NetTopologySuite.IO.GeoJSON: GeoJSON IO module for NTS.](https://github.com/NetTopologySuite/NetTopologySuite.IO.GeoJSON). BSD-3-Clause License.
 - [Open Topo Data](https://www.opentopodata.org/) - Provides an open and free Elevation API and offers both a public service and the code to host the service yourself (including scripts/information to get the needed source data) - [GitHub: ajnisbet/opentopodata: Open alternative to the Google Elevation API!](https://github.com/ajnisbet/opentopodata). (Code) MIT License.

**Wpf:**
 - [punker76/gong-wpf-dragdrop: The GongSolutions.WPF.DragDrop library is a drag'n'drop framework for WPF](https://github.com/punker76/gong-wpf-dragdrop). BSD-3-Clause License.
 - [hexinnovation/MathConverter: An All-in-One XAML Converter](https://github.com/hexinnovation/MathConverter) - A welcome addition to binding options in WPF that allows specifying math operations as a ConverterParameter to modify bindings without writing (another) custom converter.
 - [ookii-dialogs/ookii-dialogs-wpf: Awesome dialogs for Windows Desktop applications built with Microsoft .NET (WPF)](https://github.com/ookii-dialogs/ookii-dialogs-wpf) - easy access to several nice dialogs. [License of Ookii.Dialogs.Wpf.NETCore 2.1.0](https://www.nuget.org/packages/Ookii.Dialogs.Wpf.NETCore/2.1.0/License).
 - [SimpleScreenShotCapture](https://github.com/cyotek/SimpleScreenshotCapture) and [Capturing screenshots using C# and p/invoke](https://www.cyotek.com/blog/capturing-screenshots-using-csharp-and-p-invoke)- An example project and blog post with information on and code for capturing screen and window shots using native methods. Used this as the basis for a WPF/[WpfScreenHelper](https://github.com/micdenny/WpfScreenHelper) version - the advantage over traditional WPF based window image methods is that XamlIsland type controls can be captured. Creative Commons Attribution 4.0 International License.
 - [TinyIpc](https://github.com/steamcore/TinyIpc) - Windows Desktop Inter-process Communication wrapped up into a super simple to use interface for C#. After trying a number of things over the years I think this technology wrapped into a great C# library is absolutely a key piece of .NET Windows desktop development that provides a reasonable way for your apps to communicate with each other 'locally'. MIT License.
 - [WindowsCommunityToolkit](https://github.com/CommunityToolkit/WindowsCommunityToolkit) - [Microsoft.Toolkit.Mvvm](https://www.nuget.org/packages/Microsoft.Toolkit.Mvvm/) - The Mvvm Toolkit provides a number of good tools including SourceGenerators that can implement IPropertyNotificationChanged! MIT License.
 - [micdenny/WpfScreenHelper: Porting of Windows Forms Screen helper for Windows Presentation Foundation (WPF). It avoids dependencies on Windows Forms libraries when developing in WPF.](https://github.com/micdenny/WpfScreenHelper) - help with some details of keeping windows in visible screen space without referencing WinForms. MIT License.

**Html:**
 - [Flurl](https://flurl.dev/) - [GitHub - tmenier/Flurl: Fluent URL builder and testable HTTP client for .NET](https://github.com/tmenier/Flurl). MIT License.
 - [lunet-io/markdig: A fast, powerful, CommonMark compliant, extensible Markdown processor for .NET](https://github.com/lunet-io/markdig) and [Kryptos-FR/markdig.wpf: A WPF library for lunet-io/markdig https://github.com/lunet-io/markdig](https://github.com/Kryptos-FR/markdig.wpf) - Used to process Commonmark Markdown both inside the application and for HTML generation. BSD 2-Clause Simplified License and MIT License.
 - [Pure](https://purecss.io/) - Used in the reporting output for simple styling. GitHub: [pure-css/pure: A set of small, responsive CSS modules that you can use in every web project.](https://github.com/pure-css/pure/). BSD and MIT Licenses.
 - [sakura: a minimal classless css framework / theme](https://oxal.org/projects/sakura/) - Minimal Classless Css. GitHub: [oxalorg/sakura: a minimal css framework/theme.](https://github.com/oxalorg/sakura). MIT License.

**Data Transfer:**
 - [aws/aws-sdk-net: The official AWS SDK for .NET](https://github.com/aws/aws-sdk-net/) - For Amazon S3 file management. After years of using this library I appreciate that it is constantly updated! Apache License 2.0.

**General:**
 - [replaysMike/AnyClone: A CSharp library that can deep clone any object using only reflection.](https://github.com/replaysMike/AnyClone). MIT License.
 - [zzzprojects/System.Linq.Dynamic.Core: The .NET Standard / .NET Core version from the System Linq Dynamic functionality.](https://github.com/zzzprojects/System.Linq.Dynamic.Core) - In addition to interesting dynamic query building via strings this library has a very easy collection of methods to build types at runtime. Apache-2.0 license.
 - [kzu/GitInfo: Git and SemVer Info from MSBuild, C# and VB](https://github.com/kzu/GitInfo) - Git version information. MIT License.
 - [thomasgalliker/ObjectDumper: ObjectDumper is a utility which aims to serialize C# objects to string for debugging and logging purposes.](https://github.com/thomasgalliker/ObjectDumper) - A quick way to convert objects to human readable strings/formats. Apache License, Version 2.0.
 - [mcintyre321/OneOf: Easy to use F#-like \~discriminated\~ unions for C# with exhaustive compile time matching](https://github.com/mcintyre321/OneOf). MIT License.
 - [App-vNext/Polly: Polly is a .NET resilience and transient-fault-handling library that allows developers to express policies such as Retry, Circuit Breaker, Timeout, Bulkhead Isolation, and Fallback in a fluent and thread-safe manner. From version 6.0.1, Polly targets .NET Standard 1.1 and 2.0+.](https://github.com/App-vNext/Polly) - Great library for handling retry logic in .NET. New BSD License.
 - [serilog/serilog: Simple .NET logging with fully-structured events](https://github.com/serilog/serilog). Easy full featured logging. Apache-2.0 License.
   - [RehanSaeed/Serilog.Exceptions: Log exception details and custom properties that are not output in Exception.ToString().](https://github.com/RehanSaeed/Serilog.Exceptions) MIT License.
   - [serilog/serilog-formatting-compact: Compact JSON event format for Serilog](https://github.com/serilog/serilog-formatting-compact). Apache-2.0 License.
   - [serilog/serilog-sinks-console: Write log events to System.Console as text or JSON, with ANSI theme support](https://github.com/serilog/serilog-sinks-console). Apache-2.0 License.
   - [serilog-contrib/Serilog.Sinks.DelegatingText: A Serilog sink to emit formatted log events to a delegate.](https://github.com/serilog-contrib/Serilog.Sinks.DelegatingText). Apache License, Version 2.0.
   - [pm4net/serilog-enrichers-callerinfo: A simple Serilog enricher to add information about the calling method.](https://github.com/pm4net/serilog-enrichers-callerinfo) - Apache-2.0 license.
   - [serilog/serilog-enrichers-environment: Enrich Serilog log events with properties from System.Environment.](https://github.com/serilog/serilog-enrichers-environment) - MIT License.
   - [serilog-contrib/serilog-enrichers-globallogcontext: A Serilog Enricher for adding properties to all log events in your app](https://github.com/serilog-contrib/serilog-enrichers-globallogcontext) - Apache 2.0 License.
 - [HamedFathi/SimMetricsCore: A text similarity metric library, e.g. from edit distance's (Levenshtein, Gotoh, Jaro, etc) to other metrics, (e.g Soundex, Chapman). This library is compiled based on the .NET standard with a lot of useful extension methods.](https://github.com/HamedFathi/SimMetricsCore)
 - [replaysMike/TypeSupport: A CSharp library that makes it easier to work with Types dynamically.](https://github.com/replaysMike/TypeSupport) - When working with generic and dynamic types I appreciate some of the extension methods provided by this library to handle details like .IsNumericType that often seem to descend into endless edge cases when you try to write it yourself. GPL-3.0 License.
 - [Tyrrrz/CliWrap: Library for running command-line processes](https://github.com/Tyrrrz/CliWrap) - Improved options for running command line processes from .NET. MIT License.
 - [dotnet/reactive: LINQ for IAsyncEnumerable (System.Linq.Async)](https://github.com/dotnet/reactive): implements standard LINQ operators for IAsyncEnumerable. MIT License.