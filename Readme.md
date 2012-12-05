# Installation and Build Dependencies
## Configuration Files

Remember that settings from dependent projects' .config may need to be manually merged into the main application's .config.

## Database Server

1. Install SQL Server.
1. Build the Server project, then deploy the SQLCMD script from its output folder. Alternatively, double-click Server.publish.xml do build and publish.

When deploying synchronisation capabilities to an existing database, the following two adjustments must be made to each table:

1. Add a timestamp column.
1. Add a trigger that will update the timestamp when a row changes.

## Web Server

1. Install the Services project as an IIS application.
1. Enable net.tcp transport.
1. Edit the .config file to ensure that the connection strings and WCF configuration are correct.

## Client

1. Install SQL Server Express on the client.
1. Create an empty database called Client.
1. Install the client application.
1. Edit the client's .config to ensure that the connection strings and WCF configuration are correct.
1. The Tables setting is a CSV list of which tables to synchronise.

## ApexSQL Diff API

ApexSQL's licensing requires certain files to be available to the application at runtime. These are copied to the output directory by post-build events in the
Services and Client applications. Beware that the WCF services will pop up a modal dialog upon the first schema synchronisation during the trial period, so someone
has to be watching out for this.

### x64

The ApexSQL assemblies are x86. WcfSvcHost runs as x64. If you're running the solution from the IDE on an x64 machine, open a Visual Studio Command Prompt with administrator privileges
at [C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE](file:///C:/Program Files %28x86%29/Microsoft Visual Studio 10.0/Common7/IDE). Then run:

    copy WcfSvcHost.exe WcfSvcHost.exe.bak
    corflags /32BIT+ WcfSvcHost.exe /Force
    sn -Vr WcfSvcHost.exe

### Synchronisation Conflicts

In the event of a conflict, the server always wins. This can probably be changed to client always wins, but the API does not allow for anything else.

Deleted rows are not synchronised.

## Diagnostics

The client and server are both configured to write WCF trace logs. The client also writes a plain-text error log. Inspect these to determine why it isn't working.


### Common problems:

1. WAS must be running on the web server, and .NET 4 correctly registered with IIS.
1. The Services application requires write permissions for the location of its configuration log.
1. Requests from the client must make their way to the database server with a valid database logon that has the necessary permissions.

## The IdRanges Table

When a client connects to the server for the first time, it requests an ID range. These are tracked in the IdRanges table, and are negative. The server uses positive IDs.
The IdRange setting in the Service's .config defines how many IDs to allocate to each client. The same range is used across all tables.

## Solution Dependencies

* The Database project uses [SQL Server Data Tools](http://msdn.microsoft.com/en-gb/data/hh297027).
* The Client project uses [Ninject](http://www.ninject.org/), which can be installed with [NuGet](http://nuget.org/).
* The Client project also uses the [Notify Property Weaver extension](http://visualstudiogallery.msdn.microsoft.com/bd351303-db8c-4771-9b22-5e51524fccd3)
  to implement INotifyPropertyChanged.
* The Client and Services projects use [ApexSQL Diff API](http://www.apexsql.com/sql_tools_diffapi.aspx).

## Schema Synchronisation

The service and client have a SchemaVersion setting. Before the client synchronises, it checks the server's schema version. If this is greater than the client's
schema, it will download a snapshot of the server's schema, and synchronise its local database. The ApexSqlTables setting in SyncClient is a comma-delimited list
of regular expressions indicating which tables to synchronise. Ensure that none of the expressions match tracking tables.