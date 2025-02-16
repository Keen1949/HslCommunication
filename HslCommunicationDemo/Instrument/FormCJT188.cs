﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HslCommunication.Profinet;
using HslCommunication;
using System.Threading;
using System.IO.Ports;
using System.Xml.Linq;
using HslCommunication.Instrument.CJT;
using HslCommunication.Instrument.CJT.Helper;

namespace HslCommunicationDemo
{
	public partial class FormCJT188 : HslFormContent
	{
		public FormCJT188( )
		{
			InitializeComponent( );
		}

		private CJT188 cjt188 = null;

		private void FormSiemens_Load( object sender, EventArgs e )
		{
			panel2.Enabled = false;
			comboBox1.SelectedIndex = 0;

			comboBox3.DataSource = SerialPort.GetPortNames( );
			try
			{
				comboBox3.SelectedIndex = 0;
			}
			catch
			{
				comboBox3.Text = "COM3";
			}

			Language( Program.Language );
		}


		private void Language( int language )
		{
			if (language == 2)
			{
				Text = "DLT645 Read Demo";

				label1.Text = "Com:";
				label3.Text = "baudRate:";
				label22.Text = "DataBit";
				label23.Text = "StopBit";
				label24.Text = "parity";
				label_address.Text = "station";
				label_password.Text = "Pwd:";
				label_op_code.Text = "Op Code";
				button1.Text = "Connect";
				button2.Text = "Disconnect";
				button3.Text = "Active";

				label11.Text = "Address:";
				label12.Text = "length:";
				button25.Text = "Bulk Read";
				label13.Text = "Results:";
				label16.Text = "Message:";
				label14.Text = "Results:";
				button26.Text = "Read";

				groupBox3.Text = "Bulk Read test";
				groupBox4.Text = "Message reading test, hex string needs to be filled in";
				groupBox5.Text = "Special function test";

				comboBox1.DataSource = new string[] { "None", "Odd", "Even" };
			}
		}

		private void FormSiemens_FormClosing( object sender, FormClosingEventArgs e )
		{

		}
		

		#region Connect And Close



		private void button1_Click( object sender, EventArgs e )
		{
			if(!int.TryParse(textBox2.Text,out int baudRate ))
			{
				MessageBox.Show( DemoUtils.BaudRateInputWrong );
				return;
			}

			if (!int.TryParse( textBox16.Text, out int dataBits ))
			{
				MessageBox.Show( DemoUtils.DataBitsInputWrong );
				return;
			}

			if (!int.TryParse( textBox17.Text, out int stopBits ))
			{
				MessageBox.Show( DemoUtils.StopBitInputWrong );
				return;
			}

			cjt188?.Close( );
			cjt188 = new CJT188( textBox_station.Text );
			cjt188.LogNet = LogNet;
			cjt188.InstrumentType = Convert.ToByte( textBox_type.Text, 16 );
			cjt188.EnableCodeFE = checkBox_enable_Fe.Checked;


			try
			{
				cjt188.SerialPortInni( sp =>
				 {
					 sp.PortName = comboBox3.Text;
					 sp.BaudRate = baudRate;
					 sp.DataBits = dataBits;
					 sp.StopBits = stopBits == 0 ? System.IO.Ports.StopBits.None : (stopBits == 1 ? System.IO.Ports.StopBits.One : System.IO.Ports.StopBits.Two);
					 sp.Parity = comboBox1.SelectedIndex == 0 ? System.IO.Ports.Parity.None : (comboBox1.SelectedIndex == 1 ? System.IO.Ports.Parity.Odd : System.IO.Ports.Parity.Even);
				 } );
				cjt188.RtsEnable = checkBox5.Checked;
				cjt188.Open( );

				button2.Enabled = true;
				button1.Enabled = false;
				panel2.Enabled = true;

				userControlReadWriteOp1.SetReadWriteNet( cjt188, "90-1F", false );
			}
			catch (Exception ex)
			{
				MessageBox.Show( ex.Message );
			}
		}

		private void button2_Click( object sender, EventArgs e )
		{
			// 断开连接
			cjt188.Close( );
			button2.Enabled = false;
			button1.Enabled = true;
			panel2.Enabled = false;
		}
		
		#endregion

		#region 批量读取测试

		private void button25_Click( object sender, EventArgs e )
		{
			try
			{
				OperateResult<byte[]> read = cjt188.Read( textBox6.Text, ushort.Parse( textBox9.Text ) );
				if (read.IsSuccess)
				{
					for (int i = 0; i < read.Content.Length; i++)
					{
						read.Content[i] = (byte)(read.Content[i] - 0x33);
					}
					textBox10.Text = "Result：" + HslCommunication.BasicFramework.SoftBasic.ByteToHexString( read.Content, ' ' );
				}
				else
				{
					MessageBox.Show( "Read Failed：" + read.ToMessageShowString( ) );
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show( "Read Failed：" + ex.Message );
			}
		}

		#endregion

		#region 报文读取测试


		private void button26_Click( object sender, EventArgs e )
		{
			OperateResult<byte[]> read = cjt188.ReadFromCoreServer( HslCommunication.BasicFramework.SoftBasic.HexStringToBytes( textBox13.Text ) );
			if (read.IsSuccess)
			{
				textBox11.Text = "Result：" + HslCommunication.BasicFramework.SoftBasic.ByteToHexString( read.Content );
			}
			else
			{
				MessageBox.Show( "Read Failed：" + read.ToMessageShowString( ) );
			}
		}


		#endregion

		private void button3_Click( object sender, EventArgs e )
		{
			OperateResult active = cjt188.ActiveDeveice( );
			if (active.IsSuccess)
			{
				MessageBox.Show( "Send Active Code Success" );
			}
			else
			{
				MessageBox.Show( "Active Code failed:" + active.Message );
			}
		}

		private void button4_Click( object sender, EventArgs e )
		{
			OperateResult<string> read = cjt188.ReadAddress( );
			if (read.IsSuccess)
			{
				textBox_station.Text = read.Content;
				textBox12.Text = $"[{DateTime.Now:HH:mm:ss}] Address:{read.Content}";
			}
			else
			{
				MessageBox.Show( "Read failed: " + read.Message );
			}
		}

		private void button6_Click( object sender, EventArgs e )
		{
			// 广播当前时间
			//OperateResult read = dLT645.BroadcastTime( DateTime.Now );
			//if (read.IsSuccess)
			//{
			//    textBox12.Text = $"[{DateTime.Now:HH:mm:ss}] BroadcastTime Success";
			//}
			//else
			//{
			//    MessageBox.Show( "Read failed: " + read.Message );
			//}
		}

		private void button5_Click( object sender, EventArgs e )
		{
			// 写通信地址
			OperateResult read = cjt188.WriteAddress( textBox1.Text );
			if (read.IsSuccess)
			{
				textBox12.Text = $"[{DateTime.Now:HH:mm:ss}] Write Success";
			}
			else
			{
				MessageBox.Show( "Read failed: " + read.Message );
			}
		}

		private void button7_Click( object sender, EventArgs e )
		{
			OperateResult<string[]> read = cjt188.ReadStringArray( textBox1.Text );
			if (read.IsSuccess)
			{
				textBox12.Text = $"[{DateTime.Now:HH:mm:ss}] Read Result: {Environment.NewLine}";
				foreach (string item in read.Content)
				{
					textBox12.AppendText( item );
					textBox12.AppendText( Environment.NewLine );
				}
			}
			else
			{
				MessageBox.Show( "Read failed: " + read.Message );
			}
		}

		public override void SaveXmlParameter( XElement element )
		{
			element.SetAttributeValue( DemoDeviceList.XmlCom, comboBox3.Text );
			element.SetAttributeValue( DemoDeviceList.XmlBaudRate, textBox2.Text );
			element.SetAttributeValue( DemoDeviceList.XmlDataBits, textBox16.Text );
			element.SetAttributeValue( DemoDeviceList.XmlStopBit, textBox17.Text );
			element.SetAttributeValue( DemoDeviceList.XmlParity, comboBox1.SelectedIndex );
			element.SetAttributeValue( DemoDeviceList.XmlStation, textBox_station.Text );
			element.SetAttributeValue( DemoDeviceList.XmlRtsEnable, checkBox5.Checked );
		}

		public override void LoadXmlParameter( XElement element )
		{
			base.LoadXmlParameter( element );
			comboBox3.Text = element.Attribute( DemoDeviceList.XmlCom ).Value;
			textBox2.Text = element.Attribute( DemoDeviceList.XmlBaudRate ).Value;
			textBox16.Text = element.Attribute( DemoDeviceList.XmlDataBits ).Value;
			textBox17.Text = element.Attribute( DemoDeviceList.XmlStopBit ).Value;
			comboBox1.SelectedIndex = int.Parse( element.Attribute( DemoDeviceList.XmlParity ).Value );
			textBox_station.Text = element.Attribute( DemoDeviceList.XmlStation ).Value;
			checkBox5.Checked = bool.Parse( element.Attribute( DemoDeviceList.XmlRtsEnable ).Value );
		}

		private void userControlHead1_SaveConnectEvent_1( object sender, EventArgs e )
		{
			userControlHead1_SaveConnectEvent( sender, e );
		}

	}
}
