# Configuratively 

[![Build status](https://ci.appveyor.com/api/projects/status/mgcx695ybu7fr8ey?svg=true)](https://ci.appveyor.com/project/perceptile/configuratively)

Configuratively is a file based database for storing your environment and application configuration. It is easy to get started and supports a variety of use cases to fit your project.

* Files are stored as JSON
* RESTful API and Command-Line interfaces for retreiving configuration.
* Mapped queries to model hierarchies to override configuration values.

Configuratively is written in C# and runs on the .NET platform but with an aim to work with [coreclr](https://github.com/dotnet/coreclr) for cross-platform availability.

## Getting Started

Both the web service and command-line interfaces are bundled within the same application for simplicity. To start using Configuratively:

1. Download the [latest release](https://github.com/perceptile/Configuratively/releases/latest).
2. Use the [example](https://github.com/perceptile/Configuratively/tree/master/_testRepo/classic-scenario) to create your own configuration repository.
3. Run the `configuratively.exe` without parameters to host the webserver which defaults to `http://localhost:9000`.
4. Use the `export` argument from the command-line to compile your configuration to a JSON file.
 
*When using the web server you can change the host uri and configuration folder location within `configuratively.exe.config`*.

## A Simple Example

Configuratively has two main components: JSON files which live within a file system hierarchy and a mapping file which tells the executable how to read it. We will start by modelling a simple domain using the mapping file which you can store anywhere but we will store at the root of the filesystem.

### ~/simple-mapping.cfg
```json
{
    "entities": [
        { "name": "environments", "regex": "^environments" },
        { "name": "packages", "regex": "^packages" }
    ],
    "queries": [
        { "uriTemplate": "{environments}/{packages}" }
    ]
}
```

*Entities* relate to the domain model of your configuration, and they map a virtual name to a physical location using a regex expression. This gives you flexiblity about how you want to structure your model. Configuratively will automatically allow you to reach entities based on this configuration using convention based routes. For example an environment config located at `/environments/test.json` will be accessible via the route `/environments/test`. We will cover routes later on.

*Queries* allow you to specify cross cutting hierachies against your model. The uriTemplate is actually a dynamic route which lets you override configuration values using a *last entity* wins. Given the route `/test/web` any settings in _web_ will override settings with the same name in _test_. Using these uriTemplates you can fit your deployment models without physically changing the repository.

Now lets add configuration files for a test environment, a web package and a service package.

### ~/environments/test.json
```json
{
	"name": "test",
    "settings": {
        "debug":  false
    }
}
```

### ~/packages/web.json
```json
{
	"name": "webservice",
	"settings" : {
		"debug" : true 
	}
}
```

### ~/packages/service.json
```json
{
  "name" : "service",
  "settings" : { }
}
```

*name* is a human readable identifier for each entity and should be unique within the entity folder it lives in. This is the name you will use in the route when accessing the configuration. The debug setting is described in both the test and web configuration, lets try out some routes.

```powershell
> $result = Get-Configuration -repositoryPath C:\simplerepo -routes '/environments/test'
> $result.settings.debug
false

> $result = Get-Configuration -repositoryPath C:\simplerepo -routes '/test/service'
> $result.settings.debug
false

> $result = Get-Configuration -repositoryPath C:\simplerepo -routes '/test/web'
> $result.settings.debug
true
```

The service package does not override the debug value and so inherits it from its environment parent, whilst the web package has overridden it. 

## Linking configuration files

You can specify multiple parents for a configuration file to build complex domains and normalise your repository data. 

### ~/packages/service.json
```json
{
  "links" : [ "templates/packages/default.json" ],
  "name" : "service",
  "settings" : { }
}
```

### ~/template/packages/default.json
```json
{
  "name" : "service",
  "settings" : { "debug" : true }
}
```

Now calling the route `/environment/service` will return a debug value of true. We could add default as a link to the web.json and reduce the duplication within there too.

## Hosting your configuration as a service

We use the wonderful [Nancy](http://nancyfx.org/) framework to service configuration using HTTP. In addition we use [topshelf](http://topshelf-project.com/) as a container which will allow you to run it either as a console application or as a windows service. The command line reference for the executable can be found on their [wiki](http://docs.topshelf-project.com/en/latest/overview/commandline.html). We'll cover the basics here.

#### Running from command line

    configuratively.exe run 

#### Running as a windows service with default credentials

    configuratively.exe install
    
#### Running from command line under a specific user

    configuratively.exe run -username safeuser -password safepassword
    
You can configure the location of the configuration repository, mapping file and http endpoint by editing `configuratively.exe.config`. 

```xml
<appSettings>
  <add key="hostUri" value="http://localhost:9000" />
  <add key="repoPath" value="C:\classic-scenario"/>
  <add key="mappingFile" value="simple-mapping.cfg" />
</appSettings>
```

Once the service is running you get the JSON configuration over HTTP. The endpoints covering the simple example are:

    GET http://localhost:9000/environments/test
    
    GET http://localhost:9000/packages/service
    
    GET http://localhost:9000/test/web

## Generating configuration files using the Command-Line 

If you want to pre-generate the configuration files to support a custom method of delivery to your applications you can do that too. The same `configuratively.exe` has a built in command-line generator and comes bundled with a powershell module.

Routes are described using a comma separated list in the form of `route1=output1.json,route2=output2.json` and you can generate as many as you like. When generating a lot of configuration its better to send multiple routes in at the same time to save on the executables start-up time.

    > Save-Configuration -repositoryPath C:\repository -routes "test/web=web.json,environments/test=test.json"
    web.json
    test.json
    
## Contributing

We use and develop Configuratively for our own use, but we would love to help make it work for you too.

* Submit a pull request for a new feature or bugfix.
* Submit a failing test for a bug.
* Make a feature request by raising an issue.
* Let us know about any bugs.
* Tell us how you're using Configuratively!

You can reach us [@naeemkhedarun](https://twitter.com/naeemkhedarun) and [@James_Dawson](https://twitter.com/James_Dawson).
