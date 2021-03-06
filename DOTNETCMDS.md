# Dot Net Commands

The dot net cli commands used to setup the project are documented here.
Please update this file with whatever commands are used to update a solution or project.

## File System

```bash
# project directories
mkdir -p ./brashcli
mkdir -p ./Brash

```

## Projects

```bash

cd ./brashcli
dotnet new console

dotnet add package System.Data.SQLite
dotnet add package Dapper
dotnet add package Serilog
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Newtonsoft.Json
dotnet add package CommandLineParser
cd ..

```


```bash
cd Brash
dotnet new classlib
dotnet add package System.Data.SQLite
dotnet add package Dapper
dotnet add package Serilog
cd ..

```


```bash
cd BrashTest
dotnet new xunit

dotnet add package Microsoft.NET.Test.Sdk
dotnet add package xunit.runner.visualstudio
dotnet add package System.Data.SQLite
dotnet add package Dapper
dotnet add package Serilog
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File

cd ..
```