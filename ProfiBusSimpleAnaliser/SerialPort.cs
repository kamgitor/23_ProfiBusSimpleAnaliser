using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Controls;
using System.IO.Ports;

using System.Threading;				// Watki Thread
// using System.Windows.Threading;

// using System.Timers;				// Timery


namespace ProfiBusSimpleAnaliser
{
	delegate void rx_funct_process();

	// enum STATUS { IDLE, FRAME_OK, TIMEOUT };

	class SerialPortGeneric
	{
		protected ComboBox combobox;
		protected SerialPort serial;
		protected rx_funct_process rx_user_process;
		protected System.Timers.Timer timer;
		protected Semaphore semafor = null;

		// private STATUS rx_status = STATUS.IDLE;

		// rx data
		protected Queue<byte> rx_buffer = null;		// Kolejka odbiorcza
		// private rx_ext_funct rx_funct;

		// ***************************************************************************
		public SerialPortGeneric(ComboBox combo)
		{
			combobox = combo;
			serial = new SerialPort();
			rx_buffer = new Queue<byte>();

			InitSerialComboBox();

			// rx_funct = funct;

			serial.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(rx_handler_funct);

		}	// SerialPortGeneric


		// ***************************************************************************
		/// Analizowanie ramki wysylanej, w generic - przezroczyste
		protected virtual byte[] AnaliseTxFrame(byte[] buf)				// tylko metode virtual mozna przyslonic (override)
		{
			return buf;		// nic nie robi

		}	// AnaliseTxFrame


		// ***************************************************************************
		/// Analizowanie wamki odebranej
		/// @return : 1 - frame ok, 0 - error
		protected virtual bool AnaliseRxFrame(byte[] buf)			// tylko metode virtual mozna przyslonic (override)
		{
			return true;

		}	// AnaliseRxFrame


		// ***************************************************************************
		// public virtual bool ReceiveFrame(int timeout, byte[] buf, int size)
		// ret true - frame received, false - timeout
		public bool ReceiveFrame(int timeout, out byte [] rx_buf, out int rx_size)
		{
			rx_buf = null;
			rx_size = 0;

			rx_buffer.Clear();

			timer = new System.Timers.Timer();
			timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimerEvent);
			timer.Interval = timeout;
			timer.Enabled = true;

			semafor = new Semaphore(0, 1);		// arg 1 - ile miejsce jest wolnych, arg 2 - ile jest max miejsc - (0,1) - WaitOne - czeka az bedzie realise
			semafor.WaitOne();

			// if (rx_status == STATUS.FRAME_OK)
			if (rx_buffer.Count != 0)
			{
				rx_buf = rx_buffer.ToArray();
				rx_size = rx_buffer.Count;
				if (AnaliseRxFrame(rx_buf) == true)
					return true;
				else
					return false;		// crc error
			}
			else
				return false;

		}	// ReceiveFrame


		// ***************************************************************************
		// Wewnetrzny Event odbioru Rx
		// protected void rx_handler_funct(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
		protected virtual void rx_handler_funct(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)	// tylko metode virtual mozna przyslonic (override)
		{
			int rx_size = serial.BytesToRead;
			byte[] buffer = new byte[rx_size];

			int iReaded = serial.Read(buffer, 0, rx_size);

			for (int i = 0; i < rx_size; i++)
			{
				rx_buffer.Enqueue(buffer[i]);
			}

			// rx_funct(buffer, rx_size);

		}	// rx_handler_funct
		

		// ***************************************************************************
		// Event doliczenie timera
		void OnTimerEvent(object sender, System.Timers.ElapsedEventArgs e)
		{
			timer.Enabled = false;				// wylaczenie timera
			// rx_status = STATUS.TIMEOUT;
			semafor.Release();

		}	// ReceiveFrame


		// ***************************************************************************
		// Funkcja wewnetrza odpalana w osobnym watku - wywoluje funkcje usera, bo nie dalo sie delegata odpalic jako watek
		void RxSystemProcess()
		{
			rx_user_process();

		}	// RxSystemProcess


		// ***************************************************************************
		// Odpalenie procesu odbierania/wysylania
		public void TxRxStartProcess(rx_funct_process rx_usr_proc)
		{
			rx_user_process = rx_usr_proc;

			// Thread thr = new Thread(rx_usr_proc);		// To nie chce sie kompilowac
			Thread thd = new Thread(RxSystemProcess);
			thd.Start();

		}	// TxRxStartProcess


		// ***************************************************************************
		public void SendFrame(byte[] buf)	// bool wait)
		{
			buf = AnaliseTxFrame(buf);
			serial.Write(buf, 0, buf.Length);
			// int timer = 0;

			/*
			// To nie dziala
			while (serial.BytesToWrite != 0)
			{
				++timer;
				Thread.Sleep(1);
			}
			 * */

		}	// SendFrame


		// ***************************************************************************
		// get, set
		public int BaudRate
		{
			get
			{
				return serial.BaudRate;
			}

			set
			{
				serial.BaudRate = value;
			}
		}

		// ***************************************************************************
		// Wsadzenie istniejacych portow do comboboxa
		protected void InitSerialComboBox()
		{
			string [] portNames = SerialPort.GetPortNames();

			combobox.Items.Clear();

			for (int i = 0; i < portNames.Length; i++)
				combobox.Items.Add(portNames[i]);

		}	// InitSerialPorts


		// ***************************************************************************
		// Przygotowuje port do odpalenia ret false - problem
		public bool PreparePort()
		{
			if (serial.IsOpen)
				serial.Close();

			if (combobox.SelectedIndex != -1)
			{
				serial.PortName = (string)combobox.Items[combobox.SelectedIndex];
				serial.Open();

				return true;
			}
			else
				return false;		// not selected

		}	// GetComboboxPos


		/*

		Wojtek uzywal
		IsOpen
		Close
		Write(buffer, 0, length);
		 
			int iReadedBytes = serialPort1.BytesToRead;
		int iReaded = serialPort1.Read(buffer, 0, iReadedBytes);
		
		BaudRate = 19200;
		DataReceived
		 
		 
		 */

	}
}
