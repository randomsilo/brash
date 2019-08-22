# Dot Net Commands

The dot net cli commands used to setup the project are documented here.
Please update this file with whatever commands are used to update a solution or project.

## File System

```bash
# project directories
mkdir -p ./hcli

```

## Projects

```bash

cd ./hcli
dotnet new console

dotnet add package System.Data.SQLite
dotnet add package Dapper
dotnet add package Serilog
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Newtonsoft.Json
dotnet add package CommandLineParser
dotnet add package Handlebars.Net

cd ..

```