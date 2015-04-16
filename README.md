# Configuratively

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

*Queries* allow you to specify cross cutting hierachies against your model. The uriTemplate is actually a dynamic route which lets you override configuration values using a *last entity* windows. Given the route `/test/web` any settings in _web_ will override settings with the same name in _test_. Using these uriTemplates you can fit your deployment models without physically changing the repository.

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
