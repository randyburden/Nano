<h1 align="center">
    <img src="https://cloud.githubusercontent.com/assets/458529/20018410/929f790c-a295-11e6-8afa-13306be5c727.png" alt="Nano Logo" />
</h1>

| Windows             | Linux             | NuGet                    | Azure Demo Website       |
| ------------------- | ------------------|--------------------------|--------------------------|
| [![windows][1]][2]  | [![linux][3]][4]  | [![NuGet Version][5]][6] | [![Azure Status][7]][8]  |

Nano is a .NET cross-platform micro web framework for building web-based HTTP services and websites.

Features
---

 - Rapidly create web APIs by writing static methods
 - Run self-hosted or within an ASP.NET website
 - Add it as a [C# single file drop in][9] or referenced as a [NuGet Package][6]
   - The Api Explorer is distributed as a [single file drop in][10]
 - Easy setup and low ceremony so that you can focus on business logic and not infrastructure and framework hassles
 - Api Explorer webpage saves you massive time!
   - Enables exploring and invoking available endpoints and operations
   - Displays XML source code documentation
   - Enables rapid testing and doesn't require QA analysts to use external tools to invoke your web service
   - Can serve as a "free" auto-generated UI that can be given to internal users
   - Includes a built-in C# and JavaScript proxy generator which enables clients to generate a proxy to call your API
 - Easy to use extension points for intercepting requests, responses, or errors in order to implement logging, authentication, authorization, etc.
 - Allows developers to simply write static method and have them automatically be exposed as JSON endpoints at a convention based URL. As fast as you can write a method is as fast as you can have an API.
 - Ability to go low level by using the NanoContext class in the instance that you need to control content type, return blobs, handles headers or cookies, etc.

Demo
---

The Nano demo website and Api Explorer is hosted on Azure and can be viewed [here][8].

There are also four different demo projects included demonstrating how to use Nano as a self-hosed console application, a self-hosted Windows service, a self-hosted Topshelf managed Windows service, or within an ASP.NET application.

Getting Started
---

Follow this five minute introduction to create your first self-hosted Nano application.

**Step One** - Create a new solution and a C# console application project called "NanoIsAwesome"

**Step Two** - Install Newtonsoft.Json from Nuget

**Step Three** - Download the single-file drop-ins for both Nano [nano.cs][9] and the Api Explorer [index.html][10] and add them to your project structure which should look like this:

```
> NanoIsAwesome
  > Properties
  > References
  > www
    > ApiExplorer
        index.html
    App.config
    HelloWorldApi.cs
    Nano.cs
    packages.config
    Program.cs
```

**Step Four** - Create a new class in the root of your project named "HelloWorldApi". Then just copypasta the code below:

```csharp
namespace NanoIsAwesome
{
    class HelloWorldApi
    {
        public static string HelloWorld()
        {
            return "Hello World!";
        }
    }
}
```

**Step Five** - Update your Program.cs Main method to configure and start Nano. Just copy the code below. Feel free to read the comments if you’re curious about what each piece does:

```csharp
static void Main(string[] args)
{
    // Create a new configuration instance for Nano
    var config = new Nano.Web.Core.NanoConfiguration();

    // This exposes all public static methods in the HelloWorldApi class as JSON endpoints
    config.AddMethods<HelloWorldApi>();

    // Map the www directory so it can be served. This allows us to access the Api Explorer
    // by browsing to the ApiExplorer sub-directory
    if (Debugger.IsAttached)
    {
        // If we're debugging, we need to map the directory as it appears in the project
        config.AddDirectory("/", "../../www", returnHttp404WhenFileWasNotFound: true);
    }
    else
    {
        // If we're not debugging, we can map the directory as it will appear in the deployed code
        config.AddDirectory("/", "www", returnHttp404WhenFileWasNotFound: true);
    }

    // Start the Nano server using the config and tell it to run at the given URL
    using (var server = Nano.Web.Core.Host.HttpListener.HttpListenerNanoServer.Start(config, "http://localhost:4545"))
    {
        System.Console.WriteLine("Press any key to exit.");
        System.Console.ReadKey();
    }
}
```

**Step Six** - Start the debugger and navigate to http://localhost:4545/ApiExplorer/ to use the built-in testing endpoint or http://localhost:4545/Api/HelloWorldApi/HelloWorld to hit the endpoint directly.

And that’s it. You're successfully using Nano to serve a website and an API endpoint. Visit the [Getting Started Guide][11], the [Nano Wiki][12], or browse the source code, unit tests, and demo projects to learn more.

Api Explorer
---

The Api Explorer page is an integral part of the Nano experience as it allows you to interact with any of your web service endpoints with ease. Here is a quick list of features:
 - Built-in C# and JavaScript proxy generator
 - Displays method (operation) names, URLs, data types, and method and parameter XML comments
 - Ability to search by API classes or methods
 - Ability to invoke methods
 - Displays API results along with HTTP status code and execution time
 - Displays sample JSON input for methods that take complex objects

![Api Explorer Screenshot][17]

The Nano demo website and Api Explorer is hosted on Azure and can be viewed [here][8].

Contributors
---

Thank you to all of the contributors who have helped shaped Nano in big and small ways:
 - Randy Burden - https://github.com/randyburden
 - David Whitlark - https://github.com/dwhitlark
 - Austen Jeane - https://github.com/aus10
 - Jordan Beacham - https://github.com/jbeacham
 - Dominique Harris - https://github.com/revnique
 - Brian Schwarzkopf - https://github.com/bsschwa
 - Robert Rudduck - https://github.com/rrudduck
 - Michael Rudduck - https://github.com/mrudduck
 - Rahul Rumalla - https://github.com/xizmark
 - Andrew McClellen - https://github.com/acmc
 - Taylor Wolfe - https://github.com/wolfester
 - Jack Farrington - https://github.com/jackfarrington
 - Brennon Meiners - https://github.com/BrennonEH
 - Chris Cha - https://github.com/christophercha
 - Rolando Rojas - https://github.com/rorojas
 - Vishal Tahiliani - https://github.com/vishtahil

Thank You
---

Thanks to the following companies for providing free resources to help this maintain this project:
 - [AppVeyor][2] - Continuous integration build server for running our Windows tests
 - [Travis CI][4] - Continuous integration build server for running our Linux (Mono) tests
 - [Microsoft Azure][8] - Web hosting

License
----

Nano is open source licensed under the terms of the [MIT License][13] and available for free. See this projects [LICENSE][14] file for more information.

Acknowledgments
---

Nano made use of substantial portions and/or was heavily influenced by the following open source software:

 - Nancy: https://github.com/NancyFx/Nancy

        The MIT License
        Copyright (c) 2010 Andreas Håkansson, Steven Robbins and contributors
        License available at: https://github.com/NancyFx/Nancy/blob/master/license.txt

 - Katana Project: http://katanaproject.codeplex.com/

        Apache License
        Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
        License available at: http://katanaproject.codeplex.com/SourceControl/latest#LICENSE.txt

 - DynamicDictionary: https://github.com/randyburden/DynamicDictionary

        The MIT License
        Copyright (c) 2014 Randy Burden ( http://randyburden.com ) All rights reserved.
        License available at: https://github.com/randyburden/DynamicDictionary/blob/master/LICENSE

 - JSON.NET: https://github.com/JamesNK/Newtonsoft.Json

        The MIT License
        Copyright (c) 2007 James Newton-King
        License available at: https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md


Links
----

* [Website][16]
* [Getting Started Guide][11]
* [Issue Tracker][15]
* [Source Code][16]
* [Demo Website][8]

[1]: https://ci.appveyor.com/api/projects/status/github/ambitenergylabs/nano?svg=true
[2]: https://ci.appveyor.com/project/AmbitEnergyLabs/nano
[3]: https://travis-ci.org/AmbitEnergyLabs/Nano.svg
[4]: https://travis-ci.org/AmbitEnergyLabs/Nano
[5]: https://img.shields.io/nuget/v/Nano.svg
[6]: https://www.nuget.org/packages/Nano
[7]: https://img.shields.io/badge/azure%20demo%20website-up-brightgreen.svg
[8]: http://nanodemoaspnet.azurewebsites.net/
[9]: https://raw.githubusercontent.com/AmbitEnergyLabs/Nano/master/src/Nano/Nano.cs
[10]: https://github.com/AmbitEnergyLabs/Nano/raw/master/src/Nano.Demo.SelfHost.WindowsService/www/ApiExplorer/index.html
[11]: https://github.com/AmbitEnergyLabs/Nano/wiki/Getting-Started-Guide
[12]: https://github.com/AmbitEnergyLabs/Nano/wiki
[13]: https://opensource.org/licenses/mit-license.php
[14]: https://github.com/AmbitEnergyLabs/Nano/blob/master/LICENSE
[15]: https://github.com/AmbitEnergyLabs/Nano/issues
[16]: https://github.com/AmbitEnergyLabs/Nano
[17]: https://cloud.githubusercontent.com/assets/458529/20028013/2df95942-a2f3-11e6-9c65-8a7f1c7f0bff.png