![.NET](https://github.com/gottscj/MongoRunner/actions/workflows/Build.yml/badge.svg) ![downloads](https://img.shields.io/nuget/dt/MongoRunner)

# MongoRunner
dotnet tool to install and run MongoDB. will by default run a single node replica set so features such as transactions are supported

builds on the the excellent [Mongo2Go](https://github.com/Mongo2Go/Mongo2Go)

# Install

```
dotnet tool install --global MongoRunner --version 2021.10.5
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




