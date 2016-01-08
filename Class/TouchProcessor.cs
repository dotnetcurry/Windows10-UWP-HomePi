using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace HomePi
{
	public sealed class PointerEventArgs : EventArgs
	{
		public Point Position { get; internal set; }
		public int Pressure { get; internal set; }
	}
	public sealed class TouchProcessor : DependencyObject
	{
		private ITouchDevice device;
        private CancellationTokenSource threadCancelSource = new CancellationTokenSource();
		private bool penPressed;

		public TouchProcessor(ITouchDevice device)
		{
            if (device == null)
                throw new ArgumentNullException(nameof(device));
			this.device = device;
		}

		private bool HasListeners
		{
			get { return (_PointerMoved != null) && (_PointerDown != null) && (_PointerUp != null); }
		}

		private event EventHandler<PointerEventArgs> _PointerDown;
		public event EventHandler<PointerEventArgs> PointerDown
		{
			add
			{
				if (!HasListeners)
					StartProcessor();
				_PointerDown += value;

			}
			remove
			{
				_PointerDown -= value;
				if (!HasListeners)
					StopProcessor();
			}
		}

		private event EventHandler<PointerEventArgs> _PointerUp;
		public event EventHandler<PointerEventArgs> PointerUp
		{
			add
			{
				if (!HasListeners)
					StartProcessor();
				_PointerUp += value;

			}
			remove
			{
				_PointerUp -= value;
				if (!HasListeners)
					StopProcessor();
			}
		}

		private event EventHandler<PointerEventArgs> _PointerMoved;
        public event EventHandler<PointerEventArgs> PointerMoved
		{
			add
			{
				if (!HasListeners)
					StartProcessor();
				_PointerMoved += value;

			}
			remove
			{
				_PointerMoved -= value;
				if (!HasListeners)
					StopProcessor();
			}
		}

		private async void TouchProcessorLoop(CancellationToken cancellationToken)
		{
			while(!cancellationToken.IsCancellationRequested)
			{
				ReadTouchState();
				await Task.Delay(10);
			}
		}

		private void ReadTouchState()
		{
			device.ReadTouchpoints();

			int pressure = device.Pressure;
			if (pressure > 5)
			{
				if (!penPressed)
				{
					penPressed = true;
					var _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
					{
						_PointerDown?.Invoke(this, new PointerEventArgs() { Position = device.TouchPosition, Pressure = pressure });
					});
				}
				else
				{
					var _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
					{
						_PointerMoved?.Invoke(this, new PointerEventArgs() { Position = device.TouchPosition, Pressure = pressure });
					});
				}
			}
			else if (pressure < 2 && penPressed == true)
			{
				penPressed = false;
				var _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
				{
					_PointerUp?.Invoke(this, new PointerEventArgs() { Position = device.TouchPosition });
				});
			}
		}

		private void StartProcessor()
		{
			threadCancelSource = new CancellationTokenSource();
			Task.Run(() => TouchProcessorLoop(threadCancelSource.Token));
		}

		private void StopProcessor()
		{
			if(threadCancelSource != null)
			{
				threadCancelSource.Cancel();
				threadCancelSource = null;
            }
		}
	}
}
