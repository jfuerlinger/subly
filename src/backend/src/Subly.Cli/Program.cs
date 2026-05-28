using CommandLine;
using Subly.Cli.Commands;

var result = await Parser.Default.ParseArguments(args,
    typeof(SubscriptionListCommand),
    typeof(SubscriptionGetCommand),
    typeof(SubscriptionCreateCommand),
    typeof(SubscriptionUpdateStatusCommand),
    typeof(SubscriptionDeleteCommand),
    typeof(DashboardSummaryCommand),
    typeof(CategoryListCommand),
    typeof(CategoryCreateCommand),
    typeof(DatabaseResetCommand))
    .MapResult(
        async (SubscriptionListCommand cmd) => await cmd.Execute(),
        async (SubscriptionGetCommand cmd) => await cmd.Execute(),
        async (SubscriptionCreateCommand cmd) => await cmd.Execute(),
        async (SubscriptionUpdateStatusCommand cmd) => await cmd.Execute(),
        async (SubscriptionDeleteCommand cmd) => await cmd.Execute(),
        async (DashboardSummaryCommand cmd) => await cmd.Execute(),
        async (CategoryListCommand cmd) => await cmd.Execute(),
        async (CategoryCreateCommand cmd) => await cmd.Execute(),
        async (DatabaseResetCommand cmd) => await cmd.Execute(),
        errors => Task.FromResult(1));

Environment.Exit(result);
