using CommandLine;
using Subly.Cli.Commands;

var result = await Parser.Default.ParseArguments(args,
    typeof(McpToolsCommand),
    typeof(AuthRegisterCommand),
    typeof(AuthLoginCommand),
    typeof(SubscriptionListCommand),
    typeof(SubscriptionGetCommand),
    typeof(SubscriptionCreateCommand),
    typeof(SubscriptionSuggestLogosCommand),
    typeof(SubscriptionUpdateCommand),
    typeof(SubscriptionUpdateCategoryCommand),
    typeof(SubscriptionUpdateStatusCommand),
    typeof(SubscriptionDeleteCommand),
    typeof(DashboardSummaryCommand),
    typeof(CategoryListCommand),
    typeof(CategoryCreateCommand),
    typeof(DatabaseResetCommand),
    typeof(CategoryRenameCommand),
    typeof(CategoryDeleteCommand))
    .MapResult(command => command switch
    {
        McpToolsCommand cmd => cmd.Execute(),
        AuthRegisterCommand cmd => cmd.Execute(),
        AuthLoginCommand cmd => cmd.Execute(),
        SubscriptionListCommand cmd => cmd.Execute(),
        SubscriptionGetCommand cmd => cmd.Execute(),
        SubscriptionCreateCommand cmd => cmd.Execute(),
        SubscriptionSuggestLogosCommand cmd => cmd.Execute(),
        SubscriptionUpdateCommand cmd => cmd.Execute(),
        SubscriptionUpdateCategoryCommand cmd => cmd.Execute(),
        SubscriptionUpdateStatusCommand cmd => cmd.Execute(),
        SubscriptionDeleteCommand cmd => cmd.Execute(),
        DashboardSummaryCommand cmd => cmd.Execute(),
        CategoryListCommand cmd => cmd.Execute(),
        CategoryCreateCommand cmd => cmd.Execute(),
        DatabaseResetCommand cmd => cmd.Execute(),
        CategoryRenameCommand cmd => cmd.Execute(),
        CategoryDeleteCommand cmd => cmd.Execute(),
        _ => Task.FromResult(1),
    }, errors => Task.FromResult(1));

Environment.Exit(result);
