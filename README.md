# LogPack SDK for ASP.NET Core 3.2+

This repository contains the ASP.NET Core SDK to be used with LogPack. LogPack is a bundler for anything that is required to solve issues in any software. Checkout https://github.com/FeatureNinjas/logpack-vscode for more info.

# Prerequisites

- ASP.NET Core 3.2+
- FTP server to upload LogPacks to

# Installation

- Download the NuGet package and VS code extension from the Releases in this repository (we will upload those to nuget.org and the VS Code marketplace later) (Subscribe to get updates on new releases)
- Install the NuGet package in your asp.net core project and add the required configuration into the `Configure()` methods of `Startup.cs`

``` cs
// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // ...
    app.UseLogPack(new LogPackOptions()
    {
        Sinks = new LogPackSink[]
        {
            new FtpSink(
                [ftp-server-url], 
                [ftp-server-port], 
                [ftp-server-username], 
                [ftp-server-password], 
        },
        ProgramType = typeof(Program)
    });
    // ...
    app.UseRouting();
}
```

- Install the VS Code extension (install the .vsix file that you downloaded) and configure the same FTP server connection in the LogPack configuration section

# Additional Features

By default, a log pack is created and uploaded whenever the request responds with a 5xx return code. You can use include filters (even create your own) to change this. For example, to create a log pack for all return code 3xx, 4xx and 5xx, add the following lines to the `LogPackOptions` object (see above)

``` cs
Include = new IIncludeFilter[]
{
  new StatusIncludeFilter(0, 1000),
},
```
    
In order to create a custom include filter, implement the `IIncludeFilter` interface. Example:

``` cs
public class LogPackAccountIdIncludeFilter : IIncludeFilter
{
  public bool Include(HttpContext context)
  {
      if (context.Items.ContainsKey("accountId")
          && (context.Items["accountId"].ToString() == "1234"
          || context.Items["accountId"].ToString() == "5678"))
      {
          return true;
      }

      return false;
  }
}
```
    
If this include filter is added to the log pack options, then for all requests for the user with the account ID is 1234 or 5678, a log pack is created.

You could even base include filters on feature flags, e.g. by using FeatureNinjas ;).

# Roadmap

The following list contains features that we're planning to implement in the next updates. Help us prioritize by giving us feedback

- Support for NodeJS
- Add more upload sinks (e.g. OneDrive, AWS, ...)
- Offer online storage so you don't have to care about FTP or any other service 
- ...

Missing something? Create an issue or contact us directly.
