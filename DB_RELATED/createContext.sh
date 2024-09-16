#!/bin/bash

dotnet ef dbcontext scaffold "Name=DefaultConnection" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models/Entities --force
