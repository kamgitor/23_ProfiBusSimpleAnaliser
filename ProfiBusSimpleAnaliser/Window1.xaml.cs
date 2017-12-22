using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Collections.ObjectModel;
using System.Threading;				// Watki Thread
using System.Windows.Threading;


namespace ProfiBusSimpleAnaliser
{

	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
		ProfiBus KamSerial;

		int BAUDRATE = 230400;		// 250000 orig

		UInt16 CRC_ERROR_POS = 0;
		UInt16 FRAMING_ERROR_POS = 1;
		UInt16 UNDEFINED_ERROR_POS = 2;
		UInt16 EXCEPTION_POS = 3;


		public ObservableCollection<Ramka_ListItem> ListaRamek = new ObservableCollection<Ramka_ListItem>();
		// public ObservableCollection<Ramka_ListItem> ListaRamek;		// {get; set;}	<-	widze ze tego nie musi byc


		// ***************************************************************
		/// Dodaje do listy wpis - blad
		void AddErrItemToList(string name)
		{
			Ramka_ListItem item = new Ramka_ListItem();
			item.info = name;
			item.odbiorca = 0;
			item.nadawca = 0;
			item.typ = 0;
			item.ilosc = 0;

			ListaRamek.Add(item);

		}	// AddErrItemToList


		// ***************************************************************
		private void ResetFrameList()
		{
			if (ListaRamek.Count != 0)
				ListaRamek.Clear();

			AddErrItemToList("Crc Error");
			AddErrItemToList("Framing Error");
			AddErrItemToList("Undefined Error");
			AddErrItemToList("Exception");

		}	// ResetFrameList


		// ***************************************************************
		public Window1()
		{
			// ListaRamek = new ObservableCollection<Ramka_ListItem>();

			InitializeComponent();
			AppInit();

		}	// Window1


		// ***************************************************************
		public void AppInit()
		{
			// KamSerial = new SerialPortGeneric(comboBoxPorts);
			KamSerial = new ProfiBus(comboBoxPorts);

			KamSerial.ProfiFrameOk += new delegate_FrameOk(KamSerial_ProfiFrameOk);
			KamSerial.ProfiFrameErr += new delegate_FrameErr(KamSerial_ProfiFrameErr);

			listView1.ItemsSource = ListaRamek;

			ResetFrameList();

		}	// AppInit


		// ***************************************************************
		/// Zaznaczenie checkboxa
		private void EnableTransmisionCheckbox_Checked(object sender, RoutedEventArgs e)
		{
			if (EnableTransmisionCheckbox.IsChecked.Value == true)
			{
				if (comboBoxPorts.SelectedIndex != -1)
				{
					KamSerial.PreparePort();
					KamSerial.BaudRate = BAUDRATE;
					
					// KamSerial.TxRxStartProcess(ReadParamsProcess);
				}
				else
				{
					MessageBox.Show("Wybierz port szeregowy", "Informacja");
					EnableTransmisionCheckbox.IsChecked = false;
				}
			}
			else
			{

			}

		}	// AppInit


		// ***************************************************************
		// dodanie zdarznie na liscie ze wystapil wyjatek
		void AddFrameException()
		{
			this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
			{
				ListaRamek[EXCEPTION_POS].ilosc++;
				listView1.Items.Refresh();
			});
		
		}	// AddFrameException


		// ***************************************************************
		private void TestFrameAddModife(byte odb, byte nad, byte typ)
		{
			for (UInt16 i = 1; i < ListaRamek.Count; i++)
			{
				if (ListaRamek[i].odbiorca == odb)
					if (ListaRamek[i].nadawca == nad)
						if (ListaRamek[i].typ == typ)		// frame exist
						{
							ListaRamek[i].ilosc++;
							listView1.Items.Refresh();
							return;
						}
			}

			// frame not exist
			Ramka_ListItem item = new Ramka_ListItem();

			item.info = "Ok";
			item.odbiorca = odb;
			item.nadawca = nad;
			item.typ = typ;
			item.ilosc = 1;

			ListaRamek.Add(item);

		}	// TestIsFrameExist


		// ***************************************************************
		/// Event zlapania gotowej ramki profibusa
		private void KamSerial_ProfiFrameOk(Queue<byte> rx_data)
		{
			this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
			{
				try
				{
					TestFrameAddModife(rx_data.ElementAt(0), rx_data.ElementAt(1), rx_data.ElementAt(2));
				}
				catch
				{
					AddFrameException();
				}
			});

			/*
			// To tez dziala
			this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
			{
				ListaRamek.Add(new Ramka_ListItem
				{
					odbiorca = rx_data.ElementAt(0),
					nadawca = rx_data.ElementAt(1),
					typ = rx_data.ElementAt(2),
					ilosc = 0,
				}
				);
			});
			*/

		}	// KamSerial_ProfiFrameOk


		// ***************************************************************
		/// Event bledu ramki
		private void KamSerial_ProfiFrameErr(RX_RET_STATE err)
		{

			this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate()
			{
				if (err == RX_RET_STATE.CRC_ERROR)
				{
					ListaRamek[CRC_ERROR_POS].ilosc++;
				}
				else if (err == RX_RET_STATE.FRAMING_ERROR)
				{
					ListaRamek[FRAMING_ERROR_POS].ilosc++;
				}
				else
					ListaRamek[UNDEFINED_ERROR_POS].ilosc++;

				listView1.Items.Refresh();
			});


		}	// KamSerial_ProfiFrameErr


		// ***************************************************************
		// Kasowanie listy ramek
		private void button_clear_Click(object sender, RoutedEventArgs e)
		{
			ResetFrameList();

		}	// KamSerial_ProfiFrameErr


	}


	// ***************************************************************
	// klasa pozycji na liscie ramek
	public class Ramka_ListItem
	{
		public string info { get; set; }
		public byte odbiorca { get; set; }		// zeby dzialalo koniecznie musi byc get i set
		public byte nadawca { get; set; }
		public byte typ { get; set; }
		public uint ilosc { get; set; }
	
	}	// Ramka_ListItem


}
