namespace FatQueue.Messenger.Core.Expressions
{
    public class ExpressionExecutor
    {
        private readonly ISerializer _serializer;
        private readonly IJobActivator _jobActivator;

        public ExpressionExecutor(ISerializer serializer, IJobActivator jobActivator)
        {
            _serializer = serializer;
            _jobActivator = jobActivator;
        }

        public void Execute(string expression)
        {
            var messageActionContainer = (MessageActionContainer)_serializer.Deserialize(expression, typeof(MessageActionContainer));
            var messageAction = messageActionContainer.Deserialize(_serializer);
            messageAction.Perform(_jobActivator);
        }
    }
}
