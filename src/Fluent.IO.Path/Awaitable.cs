using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Fluent.IO
{
    public class Awaitable<T> : INotifyCompletion where T : notnull
    {
        private Task _task;
        public Func<T> ValueFactory { get; }

        private T _valueCache;

        public Awaitable(T value, Task task)
        {
            _task = task;
            _valueCache = value;
            ValueFactory = () => _valueCache;
        }

        public Awaitable(T value) : this(value, Task.FromResult(true)) { }

        public Awaitable(Func<T> valueFactory, Task task)
        {
            _task = task;
            ValueFactory = valueFactory;
#nullable disable
            _valueCache = default;
#nullable enable
        }

        public Awaitable(Func<T> valueFactory) : this(valueFactory, Task.FromResult(true)) { }

        public Awaitable<T> GetAwaiter() => this;

        public bool IsCompleted => _task?.IsCompleted ?? true;

        void INotifyCompletion.OnCompleted(Action continuation)
        {
            if (_task.IsCompleted)
            {
                continuation();
                return;
            }
            Task antecedent = _task;
            _task = Task.Run(async () =>
            {
                await antecedent;
                continuation();
            });
        }

        public T GetResult() => _valueCache = ValueFactory();

        public static implicit operator T(Awaitable<T> awaitable) => awaitable.ValueFactory();
    }
}
