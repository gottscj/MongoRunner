[![Build](https://github.com/gottscj/MongoRunner/actions/workflows/Build.yml/badge.svg)](https://github.com/gottscj/MongoRunner/actions/workflows/Build.yml) [![Nuget downloads](https://img.shields.io/nuget/dt/MongoRunner)](https://www.nuget.org/packages/MongoRunner) [![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/gottscj/MongoRunner/blob/main/LICENSE)

# MongoRunner
dotnet tool to install and run MongoDB. will by default run a single node replica set so features such as transactions are supported

builds on the the excellent [Mongo2Go](https://github.com/Mongo2Go/Mongo2Go)

# Install

```
dotnet tool install --global MongoRunner --version 2022.01.25
```
# Arguments
```
 -v: verbose
 
 -p: port start mongo on
 
 -d: data directory to store mongo data
```
# Usage

- With no arguments
```
MongoRunner
```

- With arguments
```
MongoRunner -p 30000 -d c:\data
```




