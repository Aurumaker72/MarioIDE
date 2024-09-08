using System;
using Caliburn.Micro;

namespace Gemini.Framework.Results
{
    public class LambdaResult : IResult
    {
        private readonly Action<CoroutineExecutionContext> _lambda;

        public LambdaResult(Action<CoroutineExecutionContext> lambda)
        {
            _lambda = lambda;
        }

        public void Execute(CoroutineExecutionContext context)
        {
            _lambda(context);

            EventHandler<ResultCompletionEventArgs> completedHandler = Completed;
            if (completedHandler != null)
                completedHandler(this, new ResultCompletionEventArgs());
        }

        public event EventHandler<ResultCompletionEventArgs> Completed;
    }
}