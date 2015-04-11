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
