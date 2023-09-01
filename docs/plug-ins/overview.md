<!--
  ~ Copyright 2023 MONAI Consortium
  ~
  ~ Licensed under the Apache License, Version 2.0 (the "License");
  ~ you may not use this file except in compliance with the License.
  ~ You may obtain a copy of the License at
  ~
  ~ http://www.apache.org/licenses/LICENSE-2.0
  ~
  ~ Unless required by applicable law or agreed to in writing, software
  ~ distributed under the License is distributed on an "AS IS" BASIS,
  ~ WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  ~ See the License for the specific language governing permissions and
  ~ limitations under the License.
-->

# Data Plug-ins

Data plug-ins enable manipulation of incoming data before they are saved to the storage service or outgoing data right before they are exported.

## Using Data Plug-ins

The Informatics Gateway allows you to configure data plug-ins in the following services:

-   (DIMSE) MONAI Deploy DICOM Listener: Configure each listening AE Title with zero or more data plug-ins via the
    [CLI](../setup/cli.md) or via the [Configuration API](../api/rest/config.md).
-   (DIMSE) DICOM Export: configure the `Plug-inAssemblies` with one or more data plug-ins in the [ExportRequestEvent](https://github.com/Project-MONAI/monai-deploy-messaging/blob/main/src/Messaging/Events/ExportRequestEvent.cs#L85).
-   (DICOMWeb) STOW-RS:
    -   The Virtual AE endpoints (`/dicomweb/vae/...`) can be configured similarly to the DICOM listener using the [DICOMWeb STOW API](../api/rest/dicomweb-stow.md##post-dicomwebvaeaetworkflow-idstudiesstudy-instance-uid).
    -   For the default `/dicomweb/...` endpoints, set zero or more plug-ins under `InformaticsGateway>dicomWeb>plug-ins` in the
        `appsettings.json` [configuration](../setup/schema.md) file.
-   (DICOMWeb) Export: configure the `Plug-inAssemblies` with one or more data plug-ins in the [ExportRequestEvent](https://github.com/Project-MONAI/monai-deploy-messaging/blob/main/src/Messaging/Events/ExportRequestEvent.cs#L85).

> [!Note]
> When one or more plug-ins are defined, the plug-ins are executed in the order as they are listed.

## Available Plug-ins

The following plug-ins are available:

| Name                                 | Description                                                                                                   | Fully Qualified Assembly Name                                                                                                              |
| ------------------------------------ | ------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| [DicomDeidentifier](./remote-app.md) | A plug-in that de-identifies a set of configurable DICOM tags with random data before DICOM data is exported. | `Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.DicomDeidentifier, Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution` |
| [DicomReidentifier](./remote-app.md) | A plug-in to be used together with the `DicomDeidentifier` plug-in to restore the original DICOM metadata.    | `Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution.DicomReidentifier, Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution` |


## Creating a Plug-in

To create an input data plug-in, implement the [IInputDataPlugin](xref:Monai.Deploy.InformaticsGateway.Api.PlugIns.IInputDataPlugIn) interface and 
put the [dynamic link library](https://learn.microsoft.com/en-us/troubleshoot/windows-client/deployment/dynamic-link-library) (DLL) in 
the `plug-ins/` directories. Similarly, for output data plug-ins, implement the [IOutputDataPlugin](xref:Monai.Deploy.InformaticsGateway.Api.PlugIns.IOutputDataPlugIn)
interface.

Refer to the [Configuration API](../api/rest/config.md) page to retrieve available [input](../api/rest/config.md#get-configaeplug-ins) and
[output](../api/rest/config.md#get-configdestinationplug-ins) data plug-ins.


### Database Extensions

If a plug-in requires presistent data in the database, extend the [DatabaseRegistrationBase](xref:Monai.Deploy.InformaticsGateway.Database.Api.DatabaseRegistrationBase)
class to register your database context and repositories.

Refer to the `Monai.Deploy.InformaticsGateway.PlugIns.RemoteAppExecution` plug-in as a reference.

> [!Important]
> The Informatics Gateway requires all plug-ins to extend both the Entity Framework (SQLite) and MongoDB databases.

#### Entity Framework

Implement the [IDatabaseMigrationManagerForPlugIns](xref:Monai.Deploy.InformaticsGateway.Database.Api.IDatabaseMigrationManagerForPlugIns) interface to
register your Entity Framework (EF) database context. A `connectionString` is provided to the `Configure(...)` function when you extend the
[DatabaseRegistrationBase](xref:Monai.Deploy.InformaticsGateway.Database.Api.DatabaseRegistrationBase) class, allowing you to create your
[code-first](https://learn.microsoft.com/en-us/ef/ef6/modeling/code-first/workflows/new-database) EF database context and generate your
migration code using the [dotnet ef](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) CLI tool.

The following example extends the [DatabaseRegistrationBase](xref:Monai.Deploy.InformaticsGateway.Database.Api.DatabaseRegistrationBase) class
to register a new EF database context named `RemoteAppExecutionDbContext`.

In the method, you first register the database context, then register your Migration Manager by implementing the
[IDatabaseMigrationManagerForPlugIns](xref:Monai.Deploy.InformaticsGateway.Database.Api.IDatabaseMigrationManagerForPlugIns) interface.
Lastly, you register your repository for the `RemoteAppExecutions` table.

```csharp
public class DatabaseRegistrar : DatabaseRegistrationBase
{
    public override IServiceCollection Configure(IServiceCollection services, DatabaseType databaseType, string? connectionString)
    {
        Guard.Against.Null(services, nameof(services));

        switch (databaseType)
        {
            case DatabaseType.EntityFramework:
                Guard.Against.Null(connectionString, nameof(connectionString));
                services.AddDbContext<EntityFramework.RemoteAppExecutionDbContext>(options => options.UseSqlite(connectionString), ServiceLifetime.Transient);
                services.AddScoped<IDatabaseMigrationManagerForPlugIns, EntityFramework.MigrationManager>();

                services.AddScoped(typeof(IRemoteAppExecutionRepository), typeof(EntityFramework.RemoteAppExecutionRepository));
                break;
            ...
        }

        return services;
    }
}
```

#### MongoDB

Similar to the [Entity Framework](#entity-framework) section above, for MongoDB you first register your Migration Manager by implementing
the [IDatabaseMigrationManagerForPlugIns](xref:Monai.Deploy.InformaticsGateway.Database.Api.IDatabaseMigrationManagerForPlugIns) interface
and then register your repository for the `RemoteAppExecutions` collection.

```csharp
public class DatabaseRegistrar : DatabaseRegistrationBase
{
    public override IServiceCollection Configure(IServiceCollection services, DatabaseType databaseType, string? connectionString)
    {
        Guard.Against.Null(services, nameof(services));

        switch (databaseType)
        {
            case DatabaseType.MongoDb:
                services.AddScoped<IDatabaseMigrationManagerForPlugIns, MongoDb.MigrationManager>();

                services.AddScoped(typeof(IRemoteAppExecutionRepository), typeof(MongoDb.RemoteAppExecutionRepository));
                break;
            ...
        }

        return services;
    }
}
```

In the `MigrationManager`, configure the `RemoteAppExecutions` collection.

```csharp
public class MigrationManager : IDatabaseMigrationManagerForPlugIns
{
    public IHost Migrate(IHost host)
    {
        RemoteAppExecutionConfiguration.Configure();
        return host;
    }
}
```
