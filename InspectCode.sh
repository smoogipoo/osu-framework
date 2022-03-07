#!/bin/bash

dotnet tool restore
dotnet CodeFileSanity
dotnet jb inspectcode "osu-framework.Desktop.slnf" --no-build --output="inspectcodereport.xml" --verbosity=WARN --debug --loglevel=TRACE
dotnet nvika parsereport "inspectcodereport.xml" --treatwarningsaserrors
