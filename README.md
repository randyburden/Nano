Nano
====

<a href="https://www.nuget.org/packages/Nano"><img src="https://img.shields.io/nuget/v/Nano.svg" alt="NuGet Version" /></a> <a href="https://www.nuget.org/packages/Nano"><img src="https://img.shields.io/nuget/dt/Nano.svg" alt="NuGet Download Count" /></a>

Nano is a micro web framework for building web-based HTTP services for .NET.

 - Rapidly create web apis by writing static methods
 - Low ceremony so that you can focus on business logic and not infrastructure and framework hassles
 - Auto generated web interfaces for invoking your web apis
 - Self-host in an executable or integrate into a new or existing ASP.NET application with ease
 - It can be used as a [C# single file drop in](https://raw.githubusercontent.com/AmbitEnergyLabs/Nano/master/src/Nano/Nano.cs) or referenced as a [NuGet Package](https://www.nuget.org/packages/Nano).

*Note: This is currently an alpha level project.*

Project Background
---

**Opinions**

Nano is a framework for building web apis and is optimized for developer productivity. With any framework ever written, it comes with set of opinionated ways of building software. The first opinion is that developers shouldn't have to worry about the transport protocol of HTTP for the majority of cases. We feel that in general developers just want to invoke a method to do work or get data and be on their merry way when both writing or consuming web-based services and apis. They don't want to have to worry about ports, tcp, serialization, connection timeouts, reading api specs, etc. So Nano is built with that in mind and at it's core allows developers to just write static methods and have the framework handle all of the HTTP stuff.

Project Features
---

Nano brings some very valuable features that rival many other web api frameworks including:

 - Extension points/events/hooks for intercepting the request, response, or errors in order to implement logging, authentication, authorization, etc.
 - Auto-generated web interface which can serve as documentation, a test playground, a "free" UI that you can give to users to invoke methods on your service, code proxy generation allowing your users to bypass NuGet and instead get proxy libraries directly from the source, etc.
 - Ability for api creators to have their static methods automatically be exposed as JSON endpoints at a convention based url.. 'as fast as you can write a method is as fast as you can have an api'.
 - Ability to go low level by using the NanoContext class in the rare instance that you need to control content type, return blobs, handles headers or cookies, etc.

License
----

MIT
