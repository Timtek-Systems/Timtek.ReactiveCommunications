using Machine.Specifications;

namespace Timtek.ReactiveCommunications.Specifications.Contexts;

internal class with_transaction_timeout_context
{
    protected static TransactionTimeoutContext Context;
    protected static TransactionTimeoutContextBuilder ContextBuilder;

    Establish context = () => ContextBuilder = new TransactionTimeoutContextBuilder();

    Cleanup after = () =>
    {
        Context = null;
        ContextBuilder = null;
    };
}
