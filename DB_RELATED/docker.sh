#!/bin/bash

docker pull mcr.microsoft.com/mssql/server:latest

docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=U$m1e$k@' -p 1433:1433 --name sql_server_container -d mcr.microsoft.com/mssql/server:latest
