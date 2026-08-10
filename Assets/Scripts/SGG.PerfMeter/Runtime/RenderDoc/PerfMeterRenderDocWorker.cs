using System;
using System.Threading.Tasks;

namespace SGG.PerfMeter
{
	internal interface IPerfMeterRenderDocWorkerOperation<out T>
	{
		bool IsCompleted { get; }
		T GetResult();
	}

	internal interface IPerfMeterRenderDocWorkerScheduler
	{
		IPerfMeterRenderDocWorkerOperation<T> Start<T>(Func<T> operation);
	}

	internal sealed class PerfMeterRenderDocTaskWorkerScheduler : IPerfMeterRenderDocWorkerScheduler
	{
		public IPerfMeterRenderDocWorkerOperation<T> Start<T>(Func<T> operation)
		{
			if (operation == null)
			{
				throw new ArgumentNullException(nameof(operation));
			}

			return new TaskWorkerOperation<T>(Task.Run(operation));
		}

		private sealed class TaskWorkerOperation<T> : IPerfMeterRenderDocWorkerOperation<T>
		{
			private readonly Task<T> _task;

			internal TaskWorkerOperation(Task<T> task)
			{
				_task = task;
			}

			public bool IsCompleted => _task.IsCompleted;

			public T GetResult()
			{
				return _task.GetAwaiter().GetResult();
			}
		}
	}
}
