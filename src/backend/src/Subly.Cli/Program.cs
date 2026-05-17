using CommandLine;
using Subly.Cli.Commands;

var result = Parser.Default.ParseArguments(args,
    typeof(SubscriptionListCommand),
    typeof(SubscriptionGetCommand),
    typeof(SubscriptionCreateCommand),
    typeof(SubscriptionUpdateStatusCommand),
    typeof(SubscriptionDeleteCommand),
    typeof(DashboardSummaryCommand),
    typeof(CategoryListCommand),
    typeof(CategoryCreateCommand))
    .MapResult(
        (SubscriptionListCommand cmd) => cmd.Execute().Result,
        (SubscriptionGetCommand cmd) => cmd.Execute().Result,
        (SubscriptionCreateCommand cmd) => cmd.Execute().Result,
        (SubscriptionUpdateStatusCommand cmd) => cmd.Execute().Result,
        (SubscriptionDeleteCommand cmd) => cmd.Execute().Result,
        (DashboardSummaryCommand cmd) => cmd.Execute().Result,
        (CategoryListCommand cmd) => cmd.Execute().Result,
        (CategoryCreateCommand cmd) => cmd.Execute().Result,
        errors => 1);

Environment.Exit(result);
