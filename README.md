# Sql Deployment Executor
Executes sql scripts for production from a directory.

## Assumptions
1. Sql is stored in a flat directory.
1. Files are ordered by a filename convention in the way they should be executed.
1. Each sql script should be executable based on the same connection string.
1. Each sql script is encapsulated.
1. Any ```GO``` keywords should be removed. See below.

### The ```GO``` Keyword
SSMS contains a ```GO``` keyword which allows for a break between two independent statements in the same execution. This keyword is not, in fact, valid SQL and should be removed prior to execution in this program. It also is not best practice to include this keyword since it means that one file contains more than one unit of work. For instance, commands that are required to be the first statement should instead be split into new scripts. Use of the semicolon ";" to indicate a statement's end is encouraged. You will be alerted if the ```GO``` keyword is found and the file will not be executed. 

## Recommendations
1. Each sql script should accomplish one unit of work.
1. Each sql script should protect itself from duplicate execution.
1. Each sql script should be dynamic, using variables where possible.
1. Each sql script in the directory should act on the same database (no use keywords).
1. Each sql script should have no or minimal output.
1. Comment liberally.

## Arguments
Please see ```Arguments.cs``` for a complete list of arguments.

## Examples
```bash
SqlDeploymentExecutor.exe -d "C:\Deployments\Scripts\1.0.0" -c "Server=<SERVER>;UID=<UID>;PWD=<PWD>;Initial Catalog=<DATABASE>;" 
```