var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("subly-postgres-data")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(pgAdmin => pgAdmin.WithLifetime(ContainerLifetime.Persistent));

var sublyDb = postgres.AddDatabase("sublydb");

var api = builder.AddProject<Projects.Subly_Api>("api")
    .WithReference(sublyDb)
    .WaitFor(sublyDb);

builder.AddViteApp("frontend", "../../../frontend")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
